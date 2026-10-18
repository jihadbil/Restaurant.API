// Chromium and PDF drivers need the document while loading printer capabilities.
let queue: Promise<void> = Promise.resolve();
let sequence = 0;

export function printHtml(html: string, title: string): Promise<void> {
  const job = queue.then(() => printSingleDocument(html, title));
  queue = job.catch(() => { /* A failed print must not block later jobs. */ });
  return job;
}

function printSingleDocument(html: string, title: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const iframe = document.createElement('iframe');
    iframe.id = `app-print-frame-${++sequence}`;
    iframe.title = title;
    iframe.setAttribute('aria-hidden', 'true');
    Object.assign(iframe.style, {
      position: 'fixed', left: '-10000px', top: '0', width: '1100px', height: '800px',
      border: '0', pointerEvents: 'none',
    });
    let completed = false;
    let enteredPrint = false;
    let media: MediaQueryList | null = null;
    const finish = (error?: unknown) => {
      if (completed) return;
      completed = true;
      window.clearTimeout(prepareTimeout);
      media?.removeEventListener('change', onPrintMediaChange);
      iframe.contentWindow?.removeEventListener('afterprint', onAfterPrint);
      window.setTimeout(() => {
        iframe.remove();
        if (error) reject(error); else resolve();
      }, 0);
    };
    const onAfterPrint = () => finish();
    const onPrintMediaChange = (event: MediaQueryListEvent) => {
      if (event.matches) enteredPrint = true;
      else if (enteredPrint) finish();
    };
    const prepareTimeout = window.setTimeout(() => finish(new Error('تعذر تجهيز صفحة الطباعة. أعد المحاولة أو افتح صفحة الطباعة المستقلة.')), 15000);
    iframe.onload = async () => {
      try {
        const target = iframe.contentWindow;
        if (!target) throw new Error('تعذر فتح صفحة الطباعة.');
        await target.document.fonts.ready;
        await Promise.all(Array.from(target.document.images).map(image => image.complete ? Promise.resolve() : new Promise<void>(done => {
          image.addEventListener('load', () => done(), { once: true });
          image.addEventListener('error', () => done(), { once: true });
        })));
        target.document.body.getBoundingClientRect();
        if (completed) return;
        media = target.matchMedia('print');
        media.addEventListener('change', onPrintMediaChange);
        target.addEventListener('afterprint', onAfterPrint, { once: true });
        window.clearTimeout(prepareTimeout);
        target.focus();
        target.print();
      } catch (error) {
        finish(error instanceof Error ? error : new Error('تعذر فتح نافذة الطباعة.'));
      }
    };
    iframe.srcdoc = html;
    document.body.appendChild(iframe);
  });
}

export function openPrintWindow(title: string): Window {
  const popup = window.open('', '_blank');
  if (!popup) throw new Error('تعذر فتح صفحة الطباعة. اسمح بالنوافذ المنبثقة لهذا الموقع.');
  popup.document.title = title;
  popup.document.body.textContent = 'جار تجهيز صفحة الطباعة…';
  return popup;
}

export function showPrintDocument(popup: Window, html: string): void {
  if (popup.closed) throw new Error('أُغلقت صفحة الطباعة قبل اكتمال تجهيزها.');
  const toolbar = `<aside class="print-help" style="padding:12px;margin-bottom:12px;border:1px solid #aaa;text-align:center;font:14px Segoe UI,Arial;direction:rtl">
    <button id="print-document-button" type="button" style="padding:8px 16px">طباعة</button>
    <p style="margin-top:8px">إذا تعذرت معاينة Edge مع doPDF، اضغط Ctrl+Shift+P للطباعة عبر نافذة Windows، ثم اختر doPDF 11.</p>
  </aside>`;
  const content = html.replace('</head>', '<style>@media print { .print-help { display:none !important; } }</style></head>')
    .replace(/<body([^>]*)>/i, `<body$1>${toolbar}`);
  popup.document.open();
  popup.document.write(content);
  popup.document.close();
  popup.document.getElementById('print-document-button')?.addEventListener('click', () => popup.print());
  popup.focus();
}

export function escapeHtml(value: unknown): string {
  return String(value ?? '').replace(/[&<>"']/g, character => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
  })[character]!);
}
