import { useEffect, useState } from 'react';
import Button from '../../components/ui/Button';
import Input from '../../components/ui/Input';
import { getAgentHealth, getLocalPrinters, getPrintAgentUrl, getPrintTransport,
  setPrintAgentUrl, setPrintTransport, getPendingPrintBatches, checkPendingPrintBatch, dismissPendingPrintBatch,
  getReceiptPaper, setReceiptPaper, type ReceiptPaper,
  type PendingPrintBatch, type LocalPrinter, type PrintTransport } from '../../services/localPrintAgent';

export default function PrintAgentSettings({ onPrinters }: { onPrinters: (printers: LocalPrinter[]) => void }) {
  const [url, setUrl] = useState(getPrintAgentUrl);
  const [transport, setTransport] = useState(getPrintTransport);
  const [status, setStatus] = useState('لم يتم فحص الاتصال بعد.');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [pending, setPending] = useState<PendingPrintBatch[]>([]);
  const [jobStatus, setJobStatus] = useState('');
  const [devices, setDevices] = useState<LocalPrinter[]>([]);

  const check = async () => {
    setLoading(true);
    setError(false);
    try {
      setPrintAgentUrl(url);
      const health = await getAgentHealth();
      const printers = await getLocalPrinters();
      onPrinters(printers);
      setDevices(printers);
      setPending(await getPendingPrintBatches());
      setStatus(`الخدمة متصلة — ${health.printerCount} طابعة على هذا الجهاز.`);
    } catch (cause) {
      onPrinters([]);
      setDevices([]);
      setError(true);
      setStatus(cause instanceof Error ? cause.message : 'تعذر الاتصال بخدمة الطباعة.');
    } finally { setLoading(false); }
  };

  useEffect(() => {
    let active = true;
    getLocalPrinters().then(printers => { if (active) { onPrinters(printers); setDevices(printers); } }).catch(() => {});
    getPendingPrintBatches().then(batches => { if (active) setPending(batches); }).catch(() => {});
    return () => { active = false; };
  }, [onPrinters]);

  return <section style={{ padding: 16, border: '1px solid var(--color-border)', borderRadius: 12, display: 'grid', gap: 12 }}>
    <strong>خدمة الطباعة على جهاز الكاشير</strong>
    <label>
      طريقة الطباعة
      <select aria-label="طريقة الطباعة" value={transport} style={{ marginInlineStart: 12 }} onChange={event => {
        const value = event.target.value as PrintTransport;
        setTransport(value); setPrintTransport(value);
      }}>
        <option value="agent">PrintService — طباعة مباشرة</option>
        <option value="dialog">نافذة طباعة المتصفح</option>
      </select>
    </label>
    <Input label="عنوان خدمة الطباعة" value={url} onChange={event => setUrl(event.target.value)} dir="ltr" />
    <div><Button onClick={() => void check()} isLoading={loading}>حفظ العنوان وفحص الاتصال</Button></div>
    <p role={error ? 'alert' : 'status'} style={{ margin: 0, color: error ? 'var(--color-danger-text)' : 'var(--color-text-muted)' }}>{status}</p>
    <small>هذه الخيارات محفوظة لهذا المتصفح. شغّل PrintService على جهاز الطابعات، ثم اختر أسماء طابعات Windows عند إضافة الطابعة. التقارير تستخدم A4.</small>
    {devices.map(printer => <label key={`${getPrintAgentUrl()}:${printer.name}`}>
      ورق الفواتير والمطبخ — {printer.name}
      <select aria-label={`ورق ${printer.name}`} defaultValue={getReceiptPaper(printer.name)} style={{ marginInlineStart: 12 }}
        onChange={event => setReceiptPaper(printer.name, event.target.value as ReceiptPaper)}>
        <option value="80">حراري 80 مم</option>
        <option value="58">حراري 58 مم</option>
        <option value="A4">A4</option>
      </select>
    </label>)}
    {pending.length > 0 && <div>
      <strong>محاولات طباعة تحتاج متابعة</strong>
      <p>أعد الطباعة من الطلب لمتابعة المحاولة المحفوظة. افحص حالتها قبل بدء محاولة جديدة.</p>
      {pending.map(batch => <div key={batch.key} style={{ paddingBlock: 8 }}>
        <div>{batch.title}</div>
        {batch.jobs.map(job => <div key={job.jobId}><small>{job.printerName} — {job.submitted ? 'أُرسل' : 'بانتظار التأكيد'} — {job.jobId}</small></div>)}
        <Button size="sm" disabled={loading} onClick={async () => {
          setLoading(true);
          try {
            setJobStatus((await checkPendingPrintBatch(batch.key)).join('\n'));
            setPending(await getPendingPrintBatches());
          } catch (cause) { setJobStatus(cause instanceof Error ? cause.message : 'تعذر فحص المهمة.'); }
          finally { setLoading(false); }
        }}>فحص حالة المحاولة</Button>
        <Button size="sm" variant="secondary" disabled={loading} onClick={async () => {
          if (!window.confirm('قد تكون بعض الأوراق قد طُبعت بالفعل. إنهاء المتابعة يسمح بطباعتها مرة أخرى عند إعادة الطباعة. هل تريد إنهاء المتابعة؟')) return;
          setLoading(true);
          try { await dismissPendingPrintBatch(batch.key); setPending(await getPendingPrintBatches()); }
          catch (cause) { setJobStatus(cause instanceof Error ? cause.message : 'تعذر إنهاء المتابعة.'); }
          finally { setLoading(false); }
        }}>إنهاء متابعة المحاولة</Button>
      </div>)}
      {jobStatus && <p role="status" style={{ whiteSpace: 'pre-wrap' }}>{jobStatus}</p>}
    </div>}
  </section>;
}
