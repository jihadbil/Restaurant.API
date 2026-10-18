import type { OrderDto } from '../types/api';
import { OrderType, PrinterType, type PrinterDto, type PrintStationDto, type ProductDto } from '../types/api';
import apiClient from './apiClient';
import { getPrintTransport, sendAgentDocuments } from './localPrintAgent';
import { rasterizePrintHtml } from './printRaster';
import { getCurrentRestaurant } from './restaurantService';
import { escapeHtml, openPrintWindow, printHtml, showPrintDocument } from './printDocument';

type PrintMode = 'agent' | 'dialog' | 'document';

async function restaurantNameForPrint(name?: string, popup?: Window | null): Promise<string> {
  try { return name?.trim() || (await getCurrentRestaurant()).name; }
  catch (error) { popup?.close(); throw error; }
}

export interface ReportPrintData {
  title: string;
  subtitle?: string;
  periodText: string;
  kpis?: { label: string; value: string; color?: string }[];
  headers: string[];
  rows: (string | number)[][];
  totals?: (string | number)[];
  restaurantName?: string;
  generatedBy?: string;
}

const getOrderTypeText = (type?: OrderType) => {
  switch (type) {
    case OrderType.Indoor:
      return 'محلي (داخل المطعم)';
    case OrderType.Outdoor:
      return 'سفري (استلام خارجي)';
    case OrderType.Delivery:
      return 'توصيل منزلي';
    default:
      return 'طلب عام';
  }
};

/**
 * Prints a standalone document or submits it to the serialized print queue.
 */
function printViaIframe(htmlContent: string, title = 'طباعة', popup?: Window | null): Promise<void> {
  if (popup) { showPrintDocument(popup, htmlContent); return Promise.resolve(); }
  return printHtml(htmlContent, title);
}

async function configuredPrinters(type: PrinterType): Promise<PrinterDto[]> {
  const printers = (await apiClient.get<PrinterDto[]>('/api/printers')).filter(printer => printer.printerType === type);
  if (!printers.length || printers.some(printer => !printer.printerName.trim())) {
    throw new Error('لم تُضبط طابعة لهذا النوع من المستندات. أضفها من الإعدادات ← الطابعات والأجهزة.');
  }
  return printers;
}

function kitchenTicketHtml(order: OrderDto, stationName: string): string {
    const timeStr = new Date(order.date || Date.now()).toLocaleTimeString('ar-LY', {
      hour: '2-digit',
      minute: '2-digit',
    });

    const items = order.orderItems || [];
    const itemsHtml = items
      .map((item) => {
        const pName = item.productName || (item as unknown as { product?: { name?: string } }).product?.name || 'صنف';
        const noteHtml =
          item.notes && item.notes !== 'لا يوجد ملاحظات'
            ? `<div style="font-size: 13px; font-weight: bold; color: #b91c1c; margin-top: 3px; background: #fee2e2; padding: 2px 6px; border-radius: 4px;">* ${escapeHtml(item.notes)}</div>`
            : '';

        return `
          <div style="border-bottom: 1.5px dotted #666; padding: 6px 0;">
            <div style="display: flex; justify-content: space-between; align-items: baseline; font-size: 17px; font-weight: 800;">
              <span>${escapeHtml(pName)}</span>
              <span style="font-size: 22px; font-weight: 900; background: #000; color: #fff; padding: 1px 8px; border-radius: 4px;">&times; ${item.quantity}</span>
            </div>
            ${noteHtml}
          </div>
        `;
      })
      .join('');

    const orderNotesHtml =
      order.notes && order.notes !== 'لا يوجد ملاحظات'
        ? `
        <div style="margin-top: 10px; padding: 8px; background: #fef3c7; border: 2px solid #b45309; border-radius: 6px; font-size: 13px; font-weight: bold; color: #92400e;">
          ملاحظة الطلب: ${escapeHtml(order.notes)}
        </div>
      `
        : '';

    const html = `
      <!DOCTYPE html>
      <html dir="rtl" lang="ar">
      <head>
        <meta charset="utf-8">
        <title>مطبخ #${order.orderNumber}</title>
        <style>
          @page {
            size: auto;
            margin: 3mm;
          }
          * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
          }
          body {
            font-family: 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif;
            color: #000;
            background: #fff;
            width: 74mm;
            margin: 0 auto;
            line-height: 1.3;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
          }
          .header {
            text-align: center;
            border-bottom: 3px solid #000;
            padding-bottom: 6px;
            margin-bottom: 8px;
          }
          .order-num-box {
            text-align: center;
            padding: 6px 0;
            margin-bottom: 6px;
            background: #f4f4f5;
            border-radius: 6px;
            border: 2px solid #000;
          }
          .order-num {
            font-size: 42px;
            font-weight: 900;
            line-height: 1;
          }
          .badge-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 4px 0;
            border-bottom: 2px solid #000;
            margin-bottom: 8px;
          }
          .badge {
            font-size: 14px;
            font-weight: 800;
            padding: 3px 8px;
            border: 1.5px solid #000;
            border-radius: 4px;
          }
        </style>
      </head>
      <body>
        <div class="header">
          <h2 style="font-size: 18px; font-weight: 800;">${escapeHtml(stationName)}</h2>
        </div>

        <div class="order-num-box">
          <div style="font-size: 13px; font-weight: 700;">رقم الطلب</div>
          <div class="order-num">#${order.orderNumber}</div>
        </div>

        <div class="badge-row">
          <span class="badge">${getOrderTypeText(order.orderType)}</span>
          <span style="font-size: 13px; font-weight: 700;">الوقت: ${timeStr}</span>
        </div>

        <div>
          ${itemsHtml}
        </div>

        ${orderNotesHtml}

        <div style="text-align: center; margin-top: 12px; font-size: 11px; border-top: 2px solid #000; padding-top: 4px;">
          كاشير: ${escapeHtml(order.userName || 'نقطة البيع')} &bull; تاريخ: ${new Date(order.date || Date.now()).toLocaleDateString('ar-LY')}
        </div>
      </body>
      </html>
    `;

    return html;
}

export const printService = {
  /**
   * Prints the Customer Receipt (فاتورة الزبون)
   */
  printReceipt: async (
    order: OrderDto,
    restaurantName?: string,
    paymentMethodName?: string,
    mode: PrintMode = getPrintTransport()
  ): Promise<void> => {
    const popup = mode === 'document' ? openPrintWindow('فاتورة المبيعات') : null;
    restaurantName = await restaurantNameForPrint(restaurantName, popup);
    paymentMethodName = paymentMethodName || order.cashDrawerEntries?.find(entry => entry.entryType === 1)?.paymentMethodName || 'غير محدد';
    const formattedDate = new Date(order.date || Date.now()).toLocaleString('ar-LY', {
      dateStyle: 'short',
      timeStyle: 'short',
    });

    const items = order.orderItems || [];
    const itemsRowsHtml = items
      .map((item) => {
        const qty = item.quantity || 1;
        const price = item.unitSalePrice ?? (item.total ? item.total / qty : 0);
        const lineTotal = item.total ?? item.lineTotal ?? qty * price;
        const pName = item.productName || (item as unknown as { product?: { name?: string } }).product?.name || 'صنف';
        const noteHtml =
          item.notes && item.notes !== 'لا يوجد ملاحظات'
            ? `<div style="font-size: 10px; color: #555; font-style: italic;">(${escapeHtml(item.notes)})</div>`
            : '';

        return `
          <tr style="border-bottom: 1px dotted #ccc;">
            <td style="padding: 5px 0; text-align: right; vertical-align: top;">
              <strong style="font-size: 12px;">${escapeHtml(pName)}</strong>
              ${noteHtml}
            </td>
            <td style="padding: 5px 2px; text-align: center; font-size: 11.5px; white-space: nowrap;">
              ${qty} &times; ${price.toFixed(2)}
            </td>
            <td style="padding: 5px 0; text-align: left; font-size: 12px; font-weight: bold; white-space: nowrap;">
              ${lineTotal.toFixed(2)}
            </td>
          </tr>
        `;
      })
      .join('');

    const subTotal = (order.total || 0) + (order.discount || 0);
    const discountHtml =
      (order.discount || 0) > 0
        ? `
        <div style="display: flex; justify-content: space-between; color: #b91c1c; font-size: 11.5px;">
          <span>الخصم:</span>
          <span>-${(order.discount || 0).toFixed(2)} د.ل</span>
        </div>
      `
        : '';

    const html = `
      <!DOCTYPE html>
      <html dir="rtl" lang="ar">
      <head>
        <meta charset="utf-8">
        <title>فاتورة #${order.orderNumber}</title>
        <style>
          @page {
            size: auto;
            margin: 3mm;
          }
          * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
          }
          body {
            font-family: 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif;
            font-size: 12px;
            color: #000;
            background: #fff;
            width: 74mm;
            margin: 0 auto;
            line-height: 1.35;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
          }
          .header {
            text-align: center;
            margin-bottom: 8px;
            padding-bottom: 6px;
            border-bottom: 2px solid #000;
          }
          .header h1 {
            font-size: 18px;
            font-weight: 800;
            margin-bottom: 2px;
          }
          .divider {
            border-bottom: 1px dashed #000;
            margin: 6px 0;
          }
          .meta-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 3px;
            font-size: 11px;
          }
          .meta-row strong {
            font-size: 12px;
          }
          table {
            width: 100%;
            border-collapse: collapse;
            margin: 6px 0;
          }
          th {
            border-bottom: 1.5px solid #000;
            padding: 4px 0;
            font-size: 11.5px;
          }
          .total-box {
            margin-top: 6px;
            padding-top: 6px;
            border-top: 2px solid #000;
            display: flex;
            justify-content: space-between;
            font-size: 16px;
            font-weight: 900;
          }
          .footer {
            text-align: center;
            margin-top: 10px;
            font-size: 10.5px;
            color: #333;
          }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>${escapeHtml(restaurantName)}</h1>
          <div style="font-size: 11px;">فاتورة ضريبية مبسطة</div>
        </div>

        <div class="meta-row">
          <span>رقم الطلب / الفاتورة:</span>
          <strong>#${order.orderNumber}</strong>
        </div>
        <div class="meta-row">
          <span>التاريخ والوقت:</span>
          <span>${formattedDate}</span>
        </div>
        <div class="meta-row">
          <span>نوع الطلب:</span>
          <strong>${getOrderTypeText(order.orderType)}</strong>
        </div>
        <div class="meta-row">
          <span>الكاشير:</span>
          <span>${escapeHtml(order.userName || 'كاشير')}</span>
        </div>
        <div class="meta-row">
          <span>طريقة الدفع:</span>
          <span>${escapeHtml(paymentMethodName)}</span>
        </div>

        <div class="divider"></div>

        <table>
          <thead>
            <tr>
              <th style="text-align: right;">الصنف</th>
              <th style="text-align: center;">الكمية</th>
              <th style="text-align: left;">الإجمالي</th>
            </tr>
          </thead>
          <tbody>
            ${itemsRowsHtml}
          </tbody>
        </table>

        <div class="divider"></div>

        <div style="display: flex; flex-direction: column; gap: 3px; font-size: 11.5px;">
          <div style="display: flex; justify-content: space-between;">
            <span>المجموع الفرعي:</span>
            <span>${subTotal.toFixed(2)} د.ل</span>
          </div>
          ${discountHtml}
        </div>

        <div class="total-box">
          <span>الصافي المطلوب:</span>
          <span>${(order.total || 0).toFixed(2)} د.ل</span>
        </div>

        <div class="divider"></div>

        <div class="footer">
          <div style="font-weight: bold; margin-bottom: 2px;">شكراً لزيارتكم ونتمنى لكم وجبة شهية!</div>
          <div style="font-size: 9.5px; color: #555;">يرجى الاحتفاظ بالفاتورة لخدمتكم</div>
        </div>
      </body>
      </html>
    `;

    if (mode === 'agent') {
      await sendAgentDocuments(`receipt:${order.id}`, async () => {
        const documents = [];
        for (const printer of await configuredPrinters(PrinterType.Receipt)) {
          documents.push(...await rasterizePrintHtml(html, printer.printerName, `Receipt_${order.orderNumber}`, 'receipt'));
        }
        return documents;
      });
    } else await printViaIframe(html, `فاتورة_${order.orderNumber}`, popup);
  },

  /**
   * Prints the Kitchen Ticket (تذكرة المطبخ والتحضير)
   * Designed with high legibility, large fonts, prominent order numbers, and notes.
   */
  printKitchenTicket: async (
    order: OrderDto,
    stationName = 'محطة المطبخ والتحضير',
    mode: PrintMode = getPrintTransport()
  ): Promise<void> => {
    if (mode !== 'agent') {
      const popup = mode === 'document' ? openPrintWindow('تذكرة المطبخ') : null;
      await printViaIframe(kitchenTicketHtml(order, stationName), `مطبخ_${order.orderNumber}`, popup);
      return;
    }
    await sendAgentDocuments(`kitchen:${order.id}`, async () => {
      const printers = await configuredPrinters(PrinterType.Kitchen);
      const groups = new Map<number, { station: PrintStationDto; items: OrderDto['orderItems'] }>();
      const categories = new Map<number, PrintStationDto[]>();
      const products = new Map<number, ProductDto>();
      for (const item of order.orderItems || []) {
        let categoryId = item.categoryId;
        if (!categoryId) {
          let product = products.get(item.productId);
          if (!product) {
            product = await apiClient.get<ProductDto>(`/api/products/${item.productId}`);
            products.set(item.productId, product);
          }
          categoryId = product.categoryId;
        }
        let stations = categories.get(categoryId);
        if (!stations) {
          stations = await apiClient.get<PrintStationDto[]>(`/api/categoryprintstations/category/${categoryId}`);
          categories.set(categoryId, stations);
        }
        if (!stations.length) throw new Error(`الصنف «${item.productName}» غير مرتبط بمحطة طباعة. اربط تصنيفه بمحطة من إعدادات التصنيفات.`);
        for (const station of stations) {
          if (!printers.some(printer => printer.printStationId === station.id)) {
            throw new Error(`لا توجد طابعة مطبخ لمحطة «${station.name}». راجع إعدادات الطابعات.`);
          }
          const group = groups.get(station.id) || { station, items: [] };
          group.items.push(item);
          groups.set(station.id, group);
        }
      }
      const documents = [];
      for (const { station, items } of groups.values()) {
        for (const printer of printers.filter(printer => printer.printStationId === station.id)) {
          documents.push(...await rasterizePrintHtml(kitchenTicketHtml({ ...order, orderItems: items }, station.name),
            printer.printerName, `Kitchen_${order.orderNumber}_${station.name}`, 'receipt'));
        }
      }
      return documents;
    });
  },

  printOrder: async (
    order: OrderDto,
    restaurantName?: string,
    paymentMethodName?: string
  ): Promise<void> => {
    const errors: string[] = [];
    for (const print of [
      () => printService.printReceipt(order, restaurantName, paymentMethodName),
      () => printService.printKitchenTicket(order),
    ]) {
      try { await print(); }
      catch (cause) { errors.push(cause instanceof Error ? cause.message : 'تعذرت الطباعة.'); }
    }
    if (errors.length) throw new Error(errors.join('\n'));
  },

  /**
   * Prints Executive Financial & Analytical Reports in A4 Format
   */
  printReport: async (data: ReportPrintData, mode: PrintMode = getPrintTransport()): Promise<void> => {
    const popup = mode === 'document' ? openPrintWindow(data.title) : null;
    const restaurantName = await restaurantNameForPrint(data.restaurantName, popup);
    const datePrinted = new Date().toLocaleString('ar-LY', {
      dateStyle: 'full',
      timeStyle: 'short',
    });

    const kpiCardsHtml = (data.kpis || [])
      .map(
        (kpi) => `
        <div style="flex: 1; min-width: 130px; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 10px 14px;">
          <div style="font-size: 11px; color: #64748b; font-weight: 600;">${escapeHtml(kpi.label)}</div>
          <div style="font-size: 18px; font-weight: 800; color: ${kpi.color || '#0f172a'}; margin-top: 3px;">${escapeHtml(kpi.value)}</div>
        </div>
      `
      )
      .join('');

    const headersHtml = data.headers
      .map(
        (h) => `
        <th style="padding: 8px 10px; background: #0f172a; color: #ffffff; font-size: 12px; font-weight: 700; border: 1px solid #0f172a; text-align: right;">${escapeHtml(h)}</th>
      `
      )
      .join('');

    const rowsHtml = data.rows
      .map(
        (row, idx) => `
        <tr style="background: ${idx % 2 === 0 ? '#ffffff' : '#f8fafc'};">
          ${row
            .map(
              (cell) => `
            <td style="padding: 7px 10px; border: 1px solid #e2e8f0; font-size: 11.5px; text-align: right;">${escapeHtml(cell)}</td>
          `
            )
            .join('')}
        </tr>
      `
      )
      .join('');

    const totalsHtml = data.totals
      ? `
        <tr style="background: #e2e8f0; font-weight: 800;">
          ${data.totals
            .map(
              (t) => `
            <td style="padding: 8px 10px; border: 1px solid #cbd5e1; font-size: 12px; text-align: right;">${escapeHtml(t)}</td>
          `
            )
            .join('')}
        </tr>
      `
      : '';

    const html = `
      <!DOCTYPE html>
      <html dir="rtl" lang="ar">
      <head>
        <meta charset="utf-8">
        <title>${escapeHtml(data.title)}</title>
        <style>
          @page {
            size: A4 portrait;
            margin: 12mm 15mm;
          }
          * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
          }
          body {
            font-family: 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif;
            color: #0f172a;
            background: #ffffff;
            line-height: 1.4;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
          }
          .report-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            border-bottom: 2px solid #0f172a;
            padding-bottom: 12px;
            margin-bottom: 16px;
          }
          .title-area h1 {
            font-size: 22px;
            font-weight: 800;
            color: #0f172a;
          }
          .title-area p {
            font-size: 12px;
            color: #64748b;
            margin-top: 3px;
          }
          .meta-area {
            text-align: left;
            font-size: 11px;
            color: #475569;
          }
          .kpis-container {
            display: flex;
            flex-wrap: wrap;
            gap: 12px;
            margin-bottom: 18px;
          }
          table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
          }
          .footer-sign {
            display: flex;
            justify-content: space-between;
            margin-top: 36px;
            padding-top: 14px;
            border-top: 1px dashed #94a3b8;
            font-size: 12px;
            color: #475569;
          }
        </style>
      </head>
      <body>
        <div class="report-header">
          <div class="title-area">
            <h1>${escapeHtml(restaurantName)}</h1>
            <p style="font-size: 14px; font-weight: 700; color: #166534; margin-top: 2px;">${escapeHtml(data.title)}</p>
            ${data.subtitle ? `<p>${escapeHtml(data.subtitle)}</p>` : ''}
            <div style="font-size: 12px; font-weight: 600; color: #334155; margin-top: 4px;">الفترة الزمنية: ${escapeHtml(data.periodText)}</div>
          </div>
          <div class="meta-area">
            <div><strong>تاريخ الاستخراج:</strong> ${datePrinted}</div>
            <div><strong>المستخدم:</strong> ${escapeHtml(data.generatedBy || 'المدير المالي')}</div>
            <div style="margin-top: 4px; font-size: 10.5px; color: #94a3b8;">نظام إدارة المطاعم والمبيعات</div>
          </div>
        </div>

        ${data.kpis && data.kpis.length > 0 ? `<div class="kpis-container">${kpiCardsHtml}</div>` : ''}

        <table>
          <thead>
            <tr>${headersHtml}</tr>
          </thead>
          <tbody>
            ${rowsHtml}
            ${totalsHtml}
          </tbody>
        </table>

        <div class="footer-sign">
          <div>
            <div style="font-weight: 700; margin-bottom: 25px;">إعداد / المحاسب المسؤول:</div>
            <div>................................................</div>
          </div>
          <div>
            <div style="font-weight: 700; margin-bottom: 25px;">اعتماد / المدير العام:</div>
            <div>................................................</div>
          </div>
        </div>
      </body>
      </html>
    `;

    if (mode === 'agent') {
      await sendAgentDocuments(`report:${JSON.stringify(data)}`, async () => {
        const documents = [];
        for (const printer of await configuredPrinters(PrinterType.Report)) {
          documents.push(...await rasterizePrintHtml(html, printer.printerName, data.title, 'report'));
        }
        return documents;
      });
    } else await printViaIframe(html, data.title, popup);
  },
};

export default printService;
