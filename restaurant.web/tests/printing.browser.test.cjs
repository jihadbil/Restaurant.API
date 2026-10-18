// Run against a local Vite server. Requests are mocked; no restaurant data or printer is changed.
// PLAYWRIGHT_MODULE_PATH can point to the bundled runtime, or install Playwright locally.
const { test, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE_PATH || 'playwright');
const baseUrl = process.env.RESTAURANT_TEST_URL || 'http://127.0.0.1:5193';
let browser;
before(async () => { browser = await chromium.launch({ channel: process.env.PRINT_TEST_BROWSER || 'msedge', headless: true }); });
after(async () => { await browser?.close(); });

async function fixture(path = '/reports') {
  const context = await browser.newContext({ timezoneId: 'Africa/Tripoli' });
  const control = { name: 'مطعم النخيل & العائلة <الحقيقي>', fail: false };
  const order = {
    id: 1, orderNumber: 1234, total: 20, discount: 0, cost: 8, profit: 12,
    date: new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Tripoli' }).format(new Date()) + 'T12:00:00', orderStatus: 0, orderType: 0, userId: 'u', userName: 'الكاشير',
    orderItems: [{ id: 1, productId: 1, productName: 'وجبة & سلطة', quantity: 2, unitSalePrice: 10, total: 20 }],
    cashDrawerEntries: [{ id: 1, entryType: 1, paymentMethodName: 'بطاقة', amount: 20 }],
  };
  await context.route('**/api/**', async route => {
    const pathname = new URL(route.request().url()).pathname.toLowerCase();
    if (pathname === '/api/restaurants/current') {
      await route.fulfill({ status: control.fail ? 404 : 200, contentType: 'application/json',
        body: JSON.stringify(control.fail ? { message: 'المطعم غير محدد' } : { id: 1, name: control.name }) });
    } else if (pathname === '/api/orders/paged') {
      await route.fulfill({ json: { items: [order], totalCount: 1, page: 1, pageSize: 50 } });
    } else if (pathname === '/api/orders') {
      await route.fulfill({ json: [order] });
    } else if (pathname.startsWith('/api/orders/')) {
      await route.fulfill({ json: order });
    } else {
      await route.fulfill({ json: [] });
    }
  });
  await context.addInitScript(() => {
    localStorage.setItem('restaurant:print-transport', 'dialog');
    localStorage.setItem('token', 'test-token');
    localStorage.setItem('currentUser', JSON.stringify({ id: 'u', userName: 'الكاشير', roles: ['Admin'], restaurantId: 1 }));
    window.__printed = [];
    window.__autoComplete = false;
    window.print = () => {
      if (window.frameElement) {
        window.parent.__printed.push({ title: document.title, text: document.body.textContent,
          heading: document.querySelector('h1')?.textContent });
        if (window.parent.__autoComplete) setTimeout(() => window.dispatchEvent(new Event('afterprint')), 20);
      }
    };
  });
  const page = await context.newPage();
  await page.goto(`${baseUrl}${path}`);
  await page.waitForLoadState('networkidle');
  return { context, page, control, order };
}

test('print documents stay alive and a second print waits for afterprint', { timeout: 20000 }, async () => {
  const { context, page } = await fixture();
  try {
    await page.evaluate(async () => {
      const printing = await import('/src/services/printDocument.ts');
      window.__done = false;
      window.__jobs = Promise.all([
        printing.printHtml('<!doctype html><html><head><title>first</title></head><body><h1>first</h1></body></html>', 'first'),
        printing.printHtml('<!doctype html><html><head><title>second</title></head><body><h1>second</h1></body></html>', 'second'),
      ]).then(() => { window.__done = true; });
    });
    await page.waitForFunction(() => window.__printed.length === 1);
    await new Promise(done => setTimeout(done, 1800));
    assert.equal(await page.locator('iframe[id^="app-print-frame-"]').count(), 1);
    assert.equal(await page.evaluate(() => window.__printed.length), 1);
    assert.equal(await page.evaluate(() => window.__done), false);
    await page.evaluate(() => document.querySelector('iframe[id^="app-print-frame-"]').contentWindow.dispatchEvent(new Event('afterprint')));
    await page.waitForFunction(() => window.__printed.length === 2);
    assert.deepEqual(await page.evaluate(() => window.__printed.map(p => p.heading)), ['first', 'second']);
    await page.evaluate(() => document.querySelector('iframe[id^="app-print-frame-"]').contentWindow.dispatchEvent(new Event('afterprint')));
    await page.waitForFunction(() => window.__done);
    assert.equal(await page.locator('iframe[id^="app-print-frame-"]').count(), 0);
  } finally { await context.close(); }
});

test('receipt printing resolves the real name and payment method, including markup characters', { timeout: 15000 }, async () => {
  const { context, page, order, control } = await fixture();
  try {
    await page.evaluate(async order => {
      window.__autoComplete = true;
      const printing = (await import('/src/services/printService.ts')).default;
      await printing.printReceipt(order);
    }, order);
    const printed = await page.evaluate(() => window.__printed[0]);
    assert.equal(printed.heading, control.name);
    assert.match(printed.text, /بطاقة/);
    assert.match(printed.text, /وجبة & سلطة/);
    assert.doesNotMatch(printed.text, /مطعم الوجبة اللذيذة/);
    assert.equal(await page.locator('iframe[id^="app-print-frame-"]').count(), 0);
  } finally { await context.close(); }
});

test('report UI opens a complete standalone document for the Windows print dialog', { timeout: 15000 }, async () => {
  const { context, page, control } = await fixture();
  try {
    const popupPromise = page.waitForEvent('popup');
    await page.getByRole('button', { name: 'فتح صفحة الطباعة', exact: true }).click();
    const popup = await popupPromise;
    await popup.getByRole('heading', { name: control.name, exact: true }).waitFor();
    assert.match(await popup.locator('body').innerText(), /Ctrl\+Shift\+P/);
    assert.equal(await popup.getByRole('button', { name: 'طباعة', exact: true }).count(), 1);
    const pdf = await popup.pdf({ preferCSSPageSize: true, printBackground: true });
    assert.ok(pdf.length > 1000, 'Native Edge PDF rendering contains a document');
    await popup.close();
  } finally { await context.close(); }
});

test('missing restaurant metadata stops printing rather than inventing a name', { timeout: 15000 }, async () => {
  const { context, page, order, control } = await fixture();
  try {
    control.fail = true;
    const error = await page.evaluate(async order => {
      const printing = (await import('/src/services/printService.ts')).default;
      try { await printing.printReceipt(order); return null; } catch (error) { return error.message; }
    }, order);
    assert.equal(error, 'المطعم غير محدد');
    assert.equal(await page.locator('iframe[id^="app-print-frame-"]').count(), 0);
  } finally { await context.close(); }
});

test('CSV report export includes the current configured restaurant name', { timeout: 15000 }, async () => {
  const { context, page, control } = await fixture();
  try {
    control.name = 'اسم المطعم الجديد';
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: 'تصدير CSV', exact: true }).click();
    const download = await downloadPromise;
    const stream = await download.createReadStream();
    const chunks = [];
    for await (const chunk of stream) chunks.push(chunk);
    assert.match(Buffer.concat(chunks).toString('utf8'), /"المطعم","اسم المطعم الجديد"/);
  } finally { await context.close(); }
});

test('receipt preview shows the configured name and print-all waits between documents', { timeout: 20000 }, async () => {
  const { context, page, control } = await fixture('/orders');
  try {
    await page.locator('button[title="عرض تفاصيل الفاتورة"]').first().click();
    await page.getByRole('button', { name: 'معاينة الطباعة', exact: true }).click();
    await page.getByRole('heading', { name: control.name, exact: true }).waitFor();
    await page.getByRole('button', { name: 'طباعة الكل', exact: true }).click();
    await page.waitForFunction(() => window.__printed.length === 1);
    assert.equal(await page.evaluate(() => window.__printed[0].heading), control.name);
    assert.equal(await page.getByRole('button', { name: 'طباعة الكل', exact: true }).isEnabled(), false);
    await page.evaluate(() => document.querySelector('iframe[id^="app-print-frame-"]').contentWindow.dispatchEvent(new Event('afterprint')));
    await page.waitForFunction(() => window.__printed.length === 2);
    assert.match(await page.evaluate(() => window.__printed[1].title), /مطبخ/);
    await page.evaluate(() => document.querySelector('iframe[id^="app-print-frame-"]').contentWindow.dispatchEvent(new Event('afterprint')));
    await page.getByRole('button', { name: 'طباعة الكل', exact: true }).waitFor();
    await page.waitForFunction(() => document.querySelectorAll('iframe[id^="app-print-frame-"]').length === 0);
  } finally { await context.close(); }
});

test('saving restaurant metadata refreshes the name shown in reports', { timeout: 15000 }, async () => {
  const { context, page, control } = await fixture();
  try {
    await page.getByText(control.name, { exact: true }).waitFor();
    control.name = 'اسم المطعم بعد التعديل';
    await page.evaluate(() => window.dispatchEvent(new Event('restaurant:updated')));
    await page.getByText(control.name, { exact: true }).waitFor();
    assert.equal(await page.getByText('مطعم النخيل & العائلة <الحقيقي>', { exact: true }).count(), 0);
  } finally { await context.close(); }
});
