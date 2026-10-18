// End-to-end UI behavior with mocked HTTP; never submits jobs to an actual printer.
const { test, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE_PATH || 'playwright');
const base = process.env.RESTAURANT_TEST_URL || 'http://127.0.0.1:5193';
let browser;
before(async () => { browser = await chromium.launch({ channel: process.env.PRINT_TEST_BROWSER || 'msedge', headless: true }); });
after(async () => { await browser?.close(); });
const today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Tripoli' }).format(new Date());
const order = { id: 1, orderNumber: 1234, total: 10, cost: 4, profit: 6, discount: 0,
  date: today + 'T12:00:00', orderStatus: 0, orderType: 0, userId: 'u', userName: 'operator',
  orderItems: [{ id: 1, productId: 1, productName: 'وجبة الاختبار', quantity: 1, unitSalePrice: 10, total: 10 }],
  cashDrawerEntries: [{ id: 1, entryType: 1, paymentMethodName: 'نقدي', amount: 10 }] };

async function fixture(path, custom, user = { id: 'u', userName: 'operator', roles: ['Admin'] }) {
  const context = await browser.newContext({ timezoneId: 'Africa/Tripoli' });
  await context.addInitScript(user => {
    localStorage.setItem('restaurant:print-transport', 'dialog');
    if (!localStorage.getItem('token')) {
      localStorage.setItem('token', 'test-token'); localStorage.setItem('currentUser', JSON.stringify(user));
    }
    window.print = () => setTimeout(() => window.dispatchEvent(new Event('afterprint')), 0);
  }, user);
  const requests = [];
  await context.route('**/api/**', async route => {
    const url = new URL(route.request().url()); requests.push({ url, method: route.request().method() });
    if (custom && await custom(route, url)) return;
    let json = [];
    switch (url.pathname.toLowerCase()) {
      case '/api/restaurants/current': json = { id: 1, name: 'مطعم الاختبار' }; break;
      case '/api/products': json = [{ id: 1, name: 'وجبة الاختبار', categoryId: 1, salePrice: 10, costPrice: 4, isAvailable: true }]; break;
      case '/api/categories': json = [{ id: 1, name: 'طعام' }]; break;
      case '/api/paymentmethods': json = [{ id: 1, name: 'نقدي', isCash: true }, { id: 2, name: 'بطاقة', isCash: false }]; break;
      case '/api/cashboxes': json = [{ id: 1, name: 'الدرج', isActive: true, isDrawer: true }]; break;
      case '/api/cashdrawerentries/paged': json = { items: [], totalCount: 0, totalInflow: 0, totalOutflow: 0, totalSales: 0 }; break;
      case '/api/orders/paged': json = { items: [order], totalCount: 100, page: Number(url.searchParams.get('page')), pageSize: 50 }; break;
      case '/api/reports/daily-sales': json = [{ date: order.date, totalOrders: 1, totalSales: 10, totalCost: 4, totalProfit: 6 }]; break;
    }
    await route.fulfill({ json });
  });
  const page = await context.newPage(); await page.goto(base + path); await page.waitForLoadState('networkidle');
  return { page, context, requests };
}
async function readDownload(download) {
  const chunks = []; for await (const chunk of await download.createReadStream()) chunks.push(chunk);
  return Buffer.concat(chunks).toString('utf8');
}

test('report export is disabled until the newly selected period has loaded', { timeout: 20000 }, async () => {
  let pending;
  const { page, context } = await fixture('/reports', async (route, url) => {
    if (url.pathname === '/api/reports/daily-sales' && url.searchParams.get('endDate') === '2026-10-14') { pending = route; return true; }
  });
  try {
    const exportButton = page.getByTitle('تصدير CSV', { exact: true });
    await exportButton.waitFor(); assert.equal(await exportButton.isEnabled(), true);
    await page.locator('input[type=date]').nth(0).fill('2026-10-01');
    const request = page.waitForRequest(r => r.url().includes('endDate=2026-10-14'));
    await page.locator('input[type=date]').nth(1).fill('2026-10-14'); await request;
    assert.equal(await exportButton.isDisabled(), true);
    assert.equal(await page.getByRole('button', { name: 'فتح صفحة الطباعة', exact: true }).isDisabled(), true);
    await pending.fulfill({ json: [] });
    await page.waitForFunction(() => !document.querySelector('[title="تصدير CSV"]').disabled);
    const download = page.waitForEvent('download'); await exportButton.click();
    const text = await readDownload(await download);
    assert.match(text, /2026-10-14/); assert.doesNotMatch(text, /10,4,6/);
  } finally { await context.close(); }
});

test('CSV escapes embedded quotes and neutralizes formula-like text', { timeout: 15000 }, async () => {
  const { page, context } = await fixture('/reports', async (route, url) => {
    if (url.pathname === '/api/reports/cancelled-orders') {
      await route.fulfill({ json: [{ ...order, notes: '=1+1', userName: 'name"with,quote' }] }); return true;
    }
  });
  try {
    await page.getByRole('button', { name: 'الطلبات الملغاة', exact: true }).click();
    await page.waitForLoadState('networkidle');
    const download = page.waitForEvent('download'); await page.getByTitle('تصدير CSV', { exact: true }).click();
    const text = await readDownload(await download);
    assert.match(text, /"'=1\+1"/); assert.match(text, /"name""with,quote"/);
  } finally { await context.close(); }
});

test('paid-order cancellation sends PUT Cancelled, and keeps the invoice', { timeout: 15000 }, async () => {
  let cancelled;
  const { page, context } = await fixture('/orders', async (route, url) => {
    if (url.pathname === '/api/orders/1') { cancelled = { method: route.request().method(), body: route.request().postDataJSON() }; await route.fulfill({ status: 204 }); return true; }
  });
  try {
    await page.getByTitle('إلغاء الفاتورة واسترداد المبلغ', { exact: true }).click();
    const response = page.waitForResponse(r => r.url().endsWith('/api/orders/1'));
    await page.getByRole('button', { name: 'نعم، ألغِ الفاتورة', exact: true }).click();
    await response;
    assert.equal(cancelled.method, 'PUT'); assert.equal(cancelled.body.orderStatus, 3);
  } finally { await context.close(); }
});

test('orders filter and paginate on the server', { timeout: 15000 }, async () => {
  const { page, context, requests } = await fixture('/orders');
  try {
    await page.getByRole('button', { name: 'التالي', exact: true }).click();
    await page.waitForResponse(r => r.url().includes('/api/orders/paged') && r.url().includes('page=2'));
    const query = requests.filter(r => r.url.pathname === '/api/orders/paged').at(-1).url.searchParams;
    assert.equal(query.get('page'), '2'); assert.equal(query.get('pageSize'), '50'); assert.equal(query.get('from'), today);
  } finally { await context.close(); }
});

test('pending checkout survives reload and recovery creates no second sale', { timeout: 20000 }, async () => {
  let saved = null, posts = 0;
  const { page, context } = await fixture('/pos', async (route, url) => {
    if (url.pathname === '/api/orders/checkout') { posts++; saved = route.request().postDataJSON(); await route.abort('failed'); return true; }
    if (url.pathname.startsWith('/api/orders/byrequest/')) { await route.fulfill({ json: order }); return true; }
  });
  try {
    await page.getByText('وجبة الاختبار', { exact: true }).click();
    await page.getByRole('button', { name: 'نقدي', exact: true }).click();
    await page.getByRole('alert').filter({ hasText: 'عملية دفع تحتاج' }).waitFor();
    assert.equal(posts, 1); assert.equal(saved.paymentMethodId, 1);
    assert.ok(await page.evaluate(() => localStorage.getItem('pending-sale:u')));
    await page.reload(); await page.waitForLoadState('networkidle');
    await page.getByRole('button', { name: 'بطاقة', exact: true }).click();
    await page.getByText('تم حفظ الطلب بنجاح!', { exact: false }).waitFor();
    assert.equal(posts, 1); assert.equal(await page.evaluate(() => localStorage.getItem('pending-sale:u')), null);
  } finally { await context.close(); }
});

test('checkout resolves a saved result before abandoning a conflict response', { timeout: 15000 }, async () => {
  let posts = 0;
  const { page, context } = await fixture('/pos', async (route, url) => {
    if (url.pathname === '/api/orders/checkout') {
      posts++; await route.fulfill({ status: 409, json: { message: 'تعارض بعد حفظ العملية' } }); return true;
    }
    if (url.pathname.startsWith('/api/orders/byrequest/')) { await route.fulfill({ json: order }); return true; }
  });
  try {
    await page.getByText('وجبة الاختبار', { exact: true }).click();
    await page.getByRole('button', { name: 'نقدي', exact: true }).click();
    await page.getByText('تم حفظ الطلب بنجاح!', { exact: false }).waitFor();
    assert.equal(posts, 1);
    assert.equal(await page.evaluate(() => localStorage.getItem('pending-sale:u')), null);
  } finally { await context.close(); }
});

test('customized and regular portions of the same product remain separate', { timeout: 20000 }, async () => {
  let payload;
  const { page, context } = await fixture('/pos', async (route, url) => {
    if (url.pathname === '/api/orders/checkout') { payload = route.request().postDataJSON(); await route.fulfill({ json: order }); return true; }
  });
  try {
    await page.getByText('وجبة الاختبار', { exact: true }).click();
    await page.getByTitle('تعديل الملاحظات واستبعاد الإضافات').click();
    await page.getByLabel('ملاحظات تحضير خاصة', { exact: true }).fill('بدون بصل');
    await page.getByRole('button', { name: 'تطبيق الملاحظة', exact: true }).click();
    await page.getByText('وجبة الاختبار', { exact: true }).first().click();
    await page.getByRole('button', { name: 'نقدي', exact: true }).click();
    await page.getByText('تم حفظ الطلب بنجاح!', { exact: false }).waitFor();
    assert.equal(payload.order.orderItems.length, 2);
    assert.deepEqual(payload.order.orderItems.map(i => i.quantity), [1, 1]);
    assert.equal(payload.order.orderItems[0].notes, 'بدون بصل');
    assert.equal(payload.order.orderItems[1].notes, 'لا يوجد ملاحظات');
  } finally { await context.close(); }
});

test('user creation submits the selected role and canonical permission keys', { timeout: 15000 }, async () => {
  let payload;
  const { page, context } = await fixture('/settings', async (route, url) => {
    if (url.pathname === '/api/auth/register') { payload = route.request().postDataJSON(); await route.fulfill({ json: { isSuccess: true } }); return true; }
  });
  try {
    await page.getByRole('button', { name: 'المستخدمون والصلاحيات', exact: true }).click();
    await page.getByRole('button', { name: 'إضافة مستخدم جديد', exact: true }).click();
    await page.getByLabel('اسم المستخدم *', { exact: true }).fill('new-manager');
    await page.getByLabel('البريد الإلكتروني *', { exact: true }).fill('manager@example.com');
    await page.getByLabel('كلمة المرور (8 أحرف على الأقل) *', { exact: true }).fill('Audit@123456');
    await page.getByRole('radio', { name: 'مسؤول النظام (Admin)', exact: true }).check();
    await page.getByRole('button', { name: 'حفظ المستخدم', exact: true }).click();
    await page.getByText('تم إنشاء حساب المستخدم بنجاح', { exact: true }).waitFor();
    assert.deepEqual(payload.roles, ['Admin']); assert.ok(payload.permissions.every(p => p.startsWith('Permission.')));
    assert.ok(payload.permissions.includes('Permission.NewOrder'));
  } finally { await context.close(); }
});

test('catalog write controls are disabled for a read-only user', { timeout: 15000 }, async () => {
  const { page, context } = await fixture('/products', null, { id: 'u', userName: 'viewer', roles: ['Cashier'], permissions: ['Permission.Products'] });
  try {
    const create = page.getByRole('button', { name: /إضافة.*صنف|إضافة.*منتج/ }).first();
    await create.waitFor(); assert.equal(await create.isDisabled(), true);
  } finally { await context.close(); }
});

test('calendar date helper preserves Tripoli midnight and month boundaries', { timeout: 15000 }, async () => {
  const { page, context } = await fixture('/reports');
  try {
    const values = await page.evaluate(async () => {
      const { localDate } = await import('/src/utils/localDate.ts');
      const now = new Date('2026-10-16T00:30:00+02:00');
      return [localDate(now), localDate(new Date(now.getFullYear(), now.getMonth(), 1))];
    });
    assert.deepEqual(values, ['2026-10-16', '2026-10-01']);
  } finally { await context.close(); }
});

test('manual cash entry recovers after reload without a second deposit', { timeout: 20000 }, async () => {
  let posts = 0, payload;
  const { page, context } = await fixture('/treasury', async (route, url) => {
    if (url.pathname === '/api/cashdrawerentries' && route.request().method() === 'POST') {
      posts++; payload = route.request().postDataJSON(); await route.abort('failed'); return true;
    }
    if (url.pathname.startsWith('/api/cashdrawerentries/byrequest/')) {
      await route.fulfill({ json: { id: 1, ...payload } }); return true;
    }
  });
  try {
    await page.getByRole('button', { name: 'إيداع نقدي (+)', exact: true }).click();
    await page.getByLabel('المبلغ النقدي (د.ل) *', { exact: true }).fill('25');
    await page.getByLabel('السبب أو البيان *', { exact: true }).fill('تغذية درج');
    await page.getByRole('button', { name: 'تأكيد الإيداع', exact: true }).click();
    await page.waitForFunction(() => !!localStorage.getItem('pending-manual:u'));
    await page.reload(); await page.waitForLoadState('networkidle');
    await page.getByRole('button', { name: 'إيداع نقدي (+)', exact: true }).click();
    assert.equal(await page.getByLabel('المبلغ النقدي (د.ل) *', { exact: true }).inputValue(), '25');
    assert.equal(await page.getByLabel('المبلغ النقدي (د.ل) *', { exact: true }).isDisabled(), true);
    await page.getByRole('button', { name: 'تأكيد الإيداع', exact: true }).click();
    await page.getByText('تم إيداع مبلغ 25.00 د.ل بنجاح!', { exact: true }).waitFor();
    assert.equal(posts, 1);
    assert.equal(await page.evaluate(() => localStorage.getItem('pending-manual:u')), null);
  } finally { await context.close(); }
});
