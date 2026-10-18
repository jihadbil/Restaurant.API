// All agent endpoints are mocked; these tests never send a physical print job.
const { test, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE_PATH || 'playwright');
const base = process.env.RESTAURANT_TEST_URL || 'http://127.0.0.1:5193';
let browser;
before(async () => { browser = await chromium.launch({ channel: process.env.PRINT_TEST_BROWSER || 'msedge', headless: true }); });
after(async () => { await browser?.close(); });

const order = { id: 17, orderNumber: 1234, total: 30, discount: 0, date: '2026-10-16T12:00:00', orderType: 0,
  userName: 'الكاشير', notes: 'بدون ملح', orderItems: [
    { productId: 1, categoryId: 11, productName: 'وجبة الشواية', quantity: 1, unitSalePrice: 20, total: 20 },
    { productId: 2, categoryId: 12, productName: 'عصير الليمون', quantity: 2, unitSalePrice: 5, total: 10 },
  ] };
const configured = [
  { id: 1, name: 'الكاشير', printerName: 'Receipt-80', printerType: 0, printStationId: 1 },
  { id: 2, name: 'الشواية', printerName: 'Grill-80', printerType: 1, printStationId: 2 },
  { id: 3, name: 'المشروبات', printerName: 'Drinks-80', printerType: 1, printStationId: 3 },
  { id: 4, name: 'التقارير', printerName: 'Office-A4', printerType: 2, printStationId: 1 },
];

async function fixture() {
  const context = await browser.newContext();
  const control = { posts: [], fail: null, unmapped: false };
  await context.addInitScript(() => {
    localStorage.setItem('token', 'secret-restaurant-jwt');
    localStorage.setItem('currentUser', JSON.stringify({ id: 'u', userName: 'test', roles: ['Admin'], restaurantId: 1 }));
    window.__dialogs = 0;
    window.print = () => { window.__dialogs++; };
  });
  await context.route('**/api/**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.port === '5055') {
      assert.equal(request.headers().authorization, undefined, 'restaurant credentials stay on the restaurant API');
      if (url.pathname === '/api/print') {
        const job = request.postDataJSON();
        control.posts.push(job);
        if (control.fail && await control.fail(route, job)) return;
        await route.fulfill({ json: { success: true, status: 'Submitted', completedCopies: 1, jobId: job.jobId } });
      } else if (url.pathname === '/api/health') {
        await route.fulfill({ json: { status: 'Healthy', spoolerRunning: true, printerCount: 4 } });
      } else if (url.pathname === '/api/print/test') {
        control.posts.push({ testPrinter: url.searchParams.get('printerName') });
        await route.fulfill({ json: { success: true, status: 'Submitted' } });
      } else await route.fulfill({ json: configured.map(printer => ({ name: printer.printerName, status: 'Ready' })) });
      return;
    }
    let json = [];
    if (url.pathname === '/api/restaurants/current') json = { id: 1, name: 'مطعم النخيل & العائلة' };
    if (url.pathname === '/api/printers') json = configured;
    if (url.pathname === '/api/categoryprintstations/category/11') json = control.unmapped ? [] : [{ id: 2, name: 'الشواية' }];
    if (url.pathname === '/api/categoryprintstations/category/12') json = [{ id: 3, name: 'المشروبات' }];
    await route.fulfill({ json });
  });
  const page = await context.newPage();
  await page.goto(base + '/settings');
  await page.waitForLoadState('networkidle');
  return { context, page, control };
}

test('receipt uses the configured agent printer and a nonempty raster with Arabic layout', async () => {
  const { context, page, control } = await fixture();
  try {
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printReceipt(order), order);
    assert.equal(control.posts.length, 1);
    const job = control.posts[0];
    assert.equal(job.printerName, 'Receipt-80');
    assert.equal(job.systemId, 'restaurant');
    assert.equal(job.documentType, 4);
    assert.equal(job.page.paperWidthMm, 80);
    const png = Buffer.from(job.contentBase64, 'base64');
    assert.equal(png.subarray(1, 4).toString(), 'PNG');
    assert.ok(png.length > 10000);
    const ink = await page.evaluate(async content => {
      const image = new Image(); image.src = 'data:image/png;base64,' + content; await image.decode();
      const canvas = document.createElement('canvas'); canvas.width = image.width; canvas.height = image.height;
      const ctx = canvas.getContext('2d'); ctx.drawImage(image, 0, 0);
      const pixels = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
      let dark = 0; for (let i = 0; i < pixels.length; i += 4) if (pixels[i] < 128) dark++;
      return dark;
    }, job.contentBase64);
    assert.ok(ink > 1000, 'image contains printed content, not a blank page');
    assert.equal(await page.evaluate(() => window.__dialogs), 0);
    assert.equal(await page.locator('iframe').count(), 0);
    require('node:fs').mkdirSync('obj/print-verification', { recursive: true });
    require('node:fs').writeFileSync('obj/print-verification/receipt.png', png);
  } finally { await context.close(); }
});

test('kitchen routes each category to its station and no unassigned items are silently printed', async () => {
  const { context, page, control } = await fixture();
  try {
    // Capture the HTML before rasterizing to verify that unrelated items are excluded.
    await page.evaluate(() => {
      window.__html = [];
      const append = document.body.appendChild.bind(document.body);
      document.body.appendChild = element => { if (element.tagName === 'IFRAME') window.__html.push(element.srcdoc); return append(element); };
    });
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printKitchenTicket(order), order);
    assert.deepEqual(control.posts.map(job => job.printerName), ['Grill-80', 'Drinks-80']);
    const html = await page.evaluate(() => window.__html);
    assert.match(html[0], /وجبة الشواية/); assert.doesNotMatch(html[0], /عصير الليمون/);
    assert.match(html[1], /عصير الليمون/); assert.doesNotMatch(html[1], /وجبة الشواية/);
    control.unmapped = true;
    const error = await page.evaluate(async order => {
      try { await (await import('/src/services/printService.ts')).default.printKitchenTicket(order); }
      catch (error) { return error.message; }
    }, order);
    assert.match(error, /غير مرتبط بمحطة/);
    assert.equal(control.posts.length, 2);
  } finally { await context.close(); }
});

test('lost response resumes original job ID and bytes after reload, while intentional reprint gets a fresh ID', async () => {
  const { context, page, control } = await fixture();
  try {
    control.fail = async route => { await route.abort('connectionreset'); return true; };
    const error = await page.evaluate(async order => {
      try { await (await import('/src/services/printService.ts')).default.printReceipt(order); }
      catch (error) { return error.message; }
    }, order);
    assert.match(error, /رقم المهمة/);
    const first = control.posts[0];
    control.fail = null;
    await page.reload();
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printReceipt(order), order);
    assert.equal(control.posts[1].jobId, first.jobId);
    assert.equal(control.posts[1].contentBase64, first.contentBase64);
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printReceipt(order), order);
    assert.notEqual(control.posts[2].jobId, first.jobId);
  } finally { await context.close(); }
});

test('partial kitchen failure preserves successful destinations when resuming', async () => {
  const { context, page, control } = await fixture();
  try {
    control.fail = async (route, job) => {
      if (job.printerName !== 'Drinks-80') return false;
      await route.abort('connectionreset'); return true;
    };
    await page.evaluate(async order => {
      try { await (await import('/src/services/printService.ts')).default.printKitchenTicket(order); } catch { /* expected */ }
    }, order);
    assert.deepEqual(control.posts.map(job => job.printerName), ['Grill-80', 'Drinks-80']);
    control.fail = null;
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printKitchenTicket(order), order);
    assert.deepEqual(control.posts.map(job => job.printerName), ['Grill-80', 'Drinks-80', 'Drinks-80']);
    assert.equal(control.posts[1].jobId, control.posts[2].jobId);
  } finally { await context.close(); }
});

test('A4 reports paginate into readable images and use the report printer', async () => {
  const { context, page, control } = await fixture();
  try {
    await page.evaluate(async () => {
      await (await import('/src/services/printService.ts')).default.printReport({ title: 'تقرير المبيعات', periodText: 'اليوم',
        headers: ['الصنف', 'القيمة'], rows: Array.from({ length: 100 }, (_, i) => [`صنف ${i + 1}`, i]) });
    });
    assert.ok(control.posts.length > 1);
    assert.ok(control.posts.every(job => job.printerName === 'Office-A4' && job.page.paperName === 'A4'));
    assert.equal(new Set(control.posts.map(job => job.jobId)).size, control.posts.length);
  } finally { await context.close(); }
});

test('printer settings discover Windows names, check health, and test the selected device', async () => {
  const { context, page, control } = await fixture();
  try {
    await page.getByRole('button', { name: 'الطابعات والأجهزة', exact: true }).click();
    await page.getByRole('button', { name: 'حفظ العنوان وفحص الاتصال', exact: true }).click();
    await page.getByRole('status').filter({ hasText: 'الخدمة متصلة' }).waitFor();
    await page.locator('button[title="اختبار الطباعة"]').first().click();
    await page.getByText('أُرسلت صفحة الاختبار إلى «Receipt-80».', { exact: true }).waitFor();
    assert.deepEqual(control.posts, [{ testPrinter: 'Receipt-80' }]);
    await page.getByRole('button', { name: 'إضافة طابعة جديدة', exact: true }).click();
    assert.equal(await page.locator('#windows-printers option').count(), 4);
  } finally { await context.close(); }
});

test('a definite pre-submission failure permits corrected settings on the next attempt', async () => {
  const { context, page, control } = await fixture();
  try {
    control.fail = async (route, job) => {
      await route.fulfill({ status: 503, json: { success: false, status: 'Failed', completedCopies: 0,
        jobId: job.jobId, message: 'الطابعة غير موجودة' } });
      return true;
    };
    const error = await page.evaluate(async order => {
      try { await (await import('/src/services/printService.ts')).default.printReceipt(order); }
      catch (error) { return error.message; }
    }, order);
    assert.equal(error, 'الطابعة غير موجودة');
    control.fail = null;
    await page.evaluate(async order => (await import('/src/services/printService.ts')).default.printReceipt(order), order);
    assert.notEqual(control.posts[0].jobId, control.posts[1].jobId);
  } finally { await context.close(); }
});

test('unknown spooler outcome retains the job, exposes tracking, and does not report success', async () => {
  const { context, page, control } = await fixture();
  try {
    control.fail = async (route, job) => {
      await route.fulfill({ status: 503, json: { success: false, status: 'Unknown', completedCopies: 0,
        jobId: job.jobId, message: 'تعذر تأكيد الإرسال' } });
      return true;
    };
    const attempt = async () => page.evaluate(async order => {
      try { await (await import('/src/services/printService.ts')).default.printReceipt(order); return null; }
      catch (error) { return error.message; }
    }, order);
    assert.match(await attempt(), /تعذر تأكيد الإرسال/);
    assert.match(await attempt(), /تعذر تأكيد الإرسال/);
    assert.equal(control.posts[0].jobId, control.posts[1].jobId);
    const batches = await page.evaluate(async () => (await import('/src/services/localPrintAgent.ts')).getPendingPrintBatches());
    assert.equal(batches.length, 1);
    assert.equal(batches[0].jobs[0].submitted, false);
  } finally { await context.close(); }
});

test('58mm receipt setting changes content and requested paper dimensions', async () => {
  const { context, page, control } = await fixture();
  try {
    await page.evaluate(async order => {
      (await import('/src/services/localPrintAgent.ts')).setReceiptPaper('Receipt-80', '58');
      await (await import('/src/services/printService.ts')).default.printReceipt(order);
    }, order);
    assert.equal(control.posts[0].page.paperWidthMm, 58);
    const png = Buffer.from(control.posts[0].contentBase64, 'base64');
    assert.equal(png.readUInt32BE(16), Math.ceil(52 * 96 / 25.4) * 3);
  } finally { await context.close(); }
});
