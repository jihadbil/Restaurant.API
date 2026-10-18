import { ENV } from '../config/env';

export interface LocalPrinter {
  name: string;
  isDefault: boolean;
  isOnline: boolean;
  isReady: boolean;
  statusKnown: boolean;
  status: string;
  supportedPaperSizes: string[];
}

export interface AgentHealth {
  status: string;
  spoolerRunning: boolean;
  printerCount: number;
}

export interface AgentPage {
  paperName?: string;
  paperWidthMm?: number;
  paperHeightMm?: number;
  margins?: { leftMm: number; rightMm: number; topMm: number; bottomMm: number };
}

export interface AgentDocument {
  printerName: string;
  jobTitle: string;
  contentBase64: string;
  page: AgentPage;
}

interface JobResult {
  success: boolean;
  status: string;
  message: string;
  jobId: string;
  completedCopies: number;
}

interface StoredJob extends AgentDocument {
  jobId: string;
  systemId: string;
  documentType: number;
  copies: number;
  submitted?: boolean;
}

class AgentRequestError extends Error {
  readonly result?: Partial<JobResult>;
  constructor(message: string, result?: Partial<JobResult>) { super(message); this.result = result; }
}

const URL_KEY = 'restaurant:print-agent-url';
export type PrintTransport = 'agent' | 'dialog';
export function getPrintTransport(): PrintTransport {
  return localStorage.getItem('restaurant:print-transport') === 'dialog' ? 'dialog' : 'agent';
}
export function setPrintTransport(value: PrintTransport): void {
  localStorage.setItem('restaurant:print-transport', value);
}
export type ReceiptPaper = '80' | '58' | 'A4';
export function getReceiptPaper(printerName: string): ReceiptPaper {
  const value = localStorage.getItem(`restaurant:receipt-paper:${getPrintAgentUrl()}:${printerName}`);
  if (value === '80' || value === '58' || value === 'A4') return value;
  return printerName.toLowerCase() === 'microsoft print to pdf' ? 'A4' : '80';
}
export function setReceiptPaper(printerName: string, value: ReceiptPaper): void {
  localStorage.setItem(`restaurant:receipt-paper:${getPrintAgentUrl()}:${printerName}`, value);
}
export function getPrintAgentUrl(): string {
  return (localStorage.getItem(URL_KEY) || ENV.PRINT_SERVICE_URL).replace(/\/+$/, '');
}

export function setPrintAgentUrl(value: string): void {
  const url = new URL(value.trim());
  if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password ||
    url.search || url.hash || url.pathname !== '/') {
    throw new Error('أدخل عنوان الخدمة فقط، مثل http://127.0.0.1:5055');
  }
  localStorage.setItem(URL_KEY, url.origin);
}

// This client deliberately uses fetch: restaurant JWTs must stay on the restaurant API.
async function request<T>(baseUrl: string, path: string, body?: unknown, timeout = 8000): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method: body === undefined ? 'GET' : 'POST',
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: 'omit',
      signal: AbortSignal.timeout(timeout),
    });
  } catch {
    throw new Error(`تعذر الاتصال بخدمة الطباعة (${baseUrl}). تأكد من تشغيل PrintService والسماح للمتصفح بالوصول إلى الشبكة المحلية.`);
  }
  let data: T & { message?: string };
  try { data = await response.json(); }
  catch { throw new AgentRequestError(`استجابة غير صالحة من خدمة الطباعة (${response.status}).`); }
  if (!response.ok) throw new AgentRequestError(data.message || `خدمة الطباعة غير جاهزة (${response.status}).`, data);
  return data;
}

export const getLocalPrinters = (): Promise<LocalPrinter[]> => request(getPrintAgentUrl(), '/api/printers');
export const getAgentHealth = (): Promise<AgentHealth> => request(getPrintAgentUrl(), '/api/health');

// Store the original bytes and IDs before sending. A lost response or page reload can
// then resume the same batch without printing already submitted destinations twice.
async function journal<T>(mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  const database = await new Promise<IDBDatabase>((resolve, reject) => {
    const open = indexedDB.open('restaurant-print-journal', 1);
    open.onupgradeneeded = () => open.result.createObjectStore('batches');
    open.onsuccess = () => resolve(open.result);
    open.onerror = () => reject(new Error('تعذر فتح سجل الطباعة المحلي.'));
  });
  try {
    return await new Promise<T>((resolve, reject) => {
      const transaction = database.transaction('batches', mode);
      const operation = action(transaction.objectStore('batches'));
      transaction.oncomplete = () => resolve(operation.result);
      transaction.onabort = transaction.onerror = () => reject(new Error('تعذر حفظ سجل الطباعة المحلي. تحقق من مساحة المتصفح.'));
    });
  } finally { database.close(); }
}

let queue: Promise<void> = Promise.resolve();

export interface PendingPrintBatch {
  key: string;
  title: string;
  jobs: { jobId: string; printerName: string; submitted: boolean }[];
}

export async function getPendingPrintBatches(): Promise<PendingPrintBatch[]> {
  const keys = await journal('readonly', store => store.getAllKeys());
  const batches: PendingPrintBatch[] = [];
  for (const key of keys) {
    if (typeof key !== 'string' || !key.startsWith(`${getPrintAgentUrl()}|${ENV.PRINT_SYSTEM_ID}|`)) continue;
    const jobs = await journal<StoredJob[] | undefined>('readonly', store => store.get(key));
    if (jobs?.length) batches.push({ key, title: jobs[0].jobTitle,
      jobs: jobs.map(job => ({ jobId: job.jobId, printerName: job.printerName, submitted: !!job.submitted })) });
  }
  return batches;
}

export async function checkPendingPrintBatch(key: string): Promise<string[]> {
  const jobs = await journal<StoredJob[] | undefined>('readonly', store => store.get(key));
  if (!jobs) return [];
  const statuses: string[] = [];
  for (const job of jobs) {
    if (job.submitted) { statuses.push(`${job.printerName}: أُرسل`); continue; }
    const query = new URLSearchParams({ systemId: job.systemId });
    try {
      const result = await request<JobResult>(getPrintAgentUrl(), `/api/jobs/${encodeURIComponent(job.jobId)}?${query}`);
      if (result.success && result.status === 'Submitted') job.submitted = true;
      statuses.push(`${job.printerName}: ${result.status} — ${result.message || ''}`);
    } catch (error) { statuses.push(`${job.printerName}: ${error instanceof Error ? error.message : 'تعذر الاستعلام'}`); }
  }
  if (jobs.every(job => job.submitted)) await journal('readwrite', store => store.delete(key));
  else await journal('readwrite', store => store.put(jobs, key));
  return statuses;
}

export async function dismissPendingPrintBatch(key: string): Promise<void> {
  await journal('readwrite', store => store.delete(key));
}

export function sendAgentDocuments(operationKey: string, prepare: () => Promise<AgentDocument[]>): Promise<void> {
  const run = queue.then(async () => {
    const baseUrl = getPrintAgentUrl();
    const key = `${baseUrl}|${ENV.PRINT_SYSTEM_ID}|${operationKey}`;
    let jobs = await journal<StoredJob[] | undefined>('readonly', store => store.get(key));
    if (!jobs) {
      jobs = (await prepare()).map(document => ({ ...document, jobId: crypto.randomUUID(),
        systemId: ENV.PRINT_SYSTEM_ID, documentType: 4, copies: 1 }));
      if (!jobs.length) return;
      await journal('readwrite', store => store.put(jobs, key));
    }
    for (const job of jobs) {
      if (job.submitted) continue;
      let result: JobResult;
      try {
        // Reposting the persisted request is idempotent in PrintService, even if
        // a previous submission completed after the HTTP connection was lost.
        result = await request<JobResult>(baseUrl, '/api/print', job, 120000);
      } catch (error) {
        // A rejected request that never reached the spooler can be prepared afresh
        // on the next user action if no destination in this batch has printed.
        const failure = error instanceof AgentRequestError ? error.result : undefined;
        if (failure && ['ValidationFailed', 'Failed'].includes(failure.status || '') &&
          !failure.completedCopies && jobs.every(candidate => !candidate.submitted)) {
          await journal('readwrite', store => store.delete(key));
          throw new Error(failure.message || 'لم تُرسل المهمة. أصلح إعدادات الطابعة ثم أعد المحاولة.', { cause: error });
        }
        throw new Error(`${error instanceof Error ? error.message : 'تعذر إرسال الطباعة.'} رقم المهمة: ${job.jobId}. المحاولة التالية تتابع المهمة نفسها.`, { cause: error });
      }
      if (!result.success || result.status !== 'Submitted') {
        throw new Error(`${result.message || 'لم تؤكد الخدمة إرسال المهمة.'} (${result.status}) رقم المهمة: ${job.jobId}`);
      }
      job.submitted = true;
      await journal('readwrite', store => store.put(jobs, key));
    }
    await journal('readwrite', store => store.delete(key));
  });
  queue = run.catch(() => {});
  return run;
}

export async function testLocalPrinter(printerName: string): Promise<void> {
  const jobId = crypto.randomUUID();
  const query = new URLSearchParams({ printerName, jobId, systemId: ENV.PRINT_SYSTEM_ID });
  const result = await request<JobResult>(getPrintAgentUrl(), `/api/print/test?${query}`, {}, 120000);
  if (!result.success || result.status !== 'Submitted') throw new Error(result.message || 'تعذر إرسال صفحة الاختبار.');
}
