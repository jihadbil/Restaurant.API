import { getReceiptPaper, type AgentDocument } from './localPrintAgent';

const PX_PER_MM = 96 / 25.4;

/** Render our self-contained HTML templates to PNG, preserving Arabic shaping,
 * tables and styles without requiring a PDF executable on the cashier's PC. */
export async function rasterizePrintHtml(html: string, printerName: string, jobTitle: string,
  format: 'receipt' | 'report'): Promise<AgentDocument[]> {
  const paper = format === 'report' ? 'A4' : getReceiptPaper(printerName);
  const widthMm = format === 'report' ? 180 : paper === '58' ? 52 : 74;
  const maxHeightMm = paper === 'A4' ? 267 : 990;
  const width = Math.ceil(widthMm * PX_PER_MM);
  const frame = document.createElement('iframe');
  frame.title = 'تجهيز مستند الطباعة';
  frame.setAttribute('aria-hidden', 'true');
  Object.assign(frame.style, { position: 'fixed', left: '-10000px', width: `${width}px`, height: '1px', border: '0' });
  try {
    await new Promise<void>((resolve, reject) => {
      const timeout = window.setTimeout(() => reject(new Error('انتهت مهلة تجهيز مستند الطباعة.')), 15000);
      frame.onload = () => { window.clearTimeout(timeout); resolve(); };
      frame.srcdoc = html.replace('</head>', `<style>html,body{width:${width}px!important;margin:0!important;padding:0!important;background:white!important}body{display:flow-root}</style></head>`);
      document.body.appendChild(frame);
    });
    const doc = frame.contentDocument!;
    await doc.fonts.ready;
    const height = Math.ceil(doc.body.getBoundingClientRect().height);
    if (!height || height > 200000) throw new Error('حجم المستند غير صالح للطباعة.');
    const content = new XMLSerializer().serializeToString(doc.documentElement);
    // Prefer row boundaries so reports and long receipts do not cut text in half.
    const boundaries = Array.from(doc.querySelectorAll('tr, body > div, body > div > div'))
      .map(element => Math.floor(element.getBoundingClientRect().bottom)).sort((a, b) => a - b);
    const documents: AgentDocument[] = [];
    for (let top = 0; top < height;) {
      let bottom = Math.min(height, top + Math.floor(maxHeightMm * PX_PER_MM));
      if (bottom < height) bottom = boundaries.findLast(y => y <= bottom && y > top + 100) ?? bottom;
      const pageHeight = bottom - top;
      const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${pageHeight}"><foreignObject x="0" y="-${top}" width="${width}" height="${height}">${content}</foreignObject></svg>`;
      const image = new Image();
      image.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = width * 3;
      canvas.height = pageHeight * 3;
      const context = canvas.getContext('2d');
      if (!context) throw new Error('تعذر تجهيز صورة الطباعة.');
      context.fillStyle = '#fff';
      context.fillRect(0, 0, canvas.width, canvas.height);
      context.drawImage(image, 0, 0, canvas.width, canvas.height);
      documents.push({ printerName, jobTitle: `${jobTitle} (${documents.length + 1})`,
        contentBase64: canvas.toDataURL('image/png').split(',')[1],
        page: paper === 'A4' ? { paperName: 'A4', margins: format === 'report'
          ? { leftMm: 15, rightMm: 15, topMm: 15, bottomMm: 15 }
          : { leftMm: 68, rightMm: 68, topMm: 15, bottomMm: 15 } }
          : { paperWidthMm: paper === '58' ? 58 : 80, paperHeightMm: Math.max(20, pageHeight / PX_PER_MM + 6),
            margins: { leftMm: 3, rightMm: 3, topMm: 3, bottomMm: 3 } },
      });
      canvas.width = canvas.height = 0;
      top = bottom;
    }
    return documents;
  } finally { frame.remove(); }
}
