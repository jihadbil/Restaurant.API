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

const dashboardReport = sales => ({ totalSales: sales, totalCost: 1, totalProfit: sales - 1,
  grossSales: sales, totalRefunds: 0, totalOrdersCount: 1, cancelledOrdersCount: 0, totalDiscount: 0,
  dailySales: [], salesByPaymentMethod: [], bestSellingProducts: [], bestSellingCategories: [],
  orderStatusSummary: [], orderTypeSummary: [] });

test('dashboard ignores a late response for a previously selected period', { timeout: 20000 }, async () => {
  let delayed;
  let calls = 0;
  const { page, context } = await fixture('/dashboard', async (route, url) => {
    if (url.pathname === '/api/reports/dashboard') {
      calls++;
      if (calls === 2) { delayed = route; return true; }
      await route.fulfill({ json: dashboardReport(calls === 1 ? 111 : 22) }); return true;
    }
  });
  try {
    const oldRequest = page.waitForRequest(r => r.url().includes('/api/reports/dashboard'));
    await page.getByRole('button', { name: '٧ أيام', exact: true }).click(); await oldRequest;
    await page.getByRole('button', { name: 'اليوم', exact: true }).click();
    await page.getByText('22.00', { exact: false }).first().waitFor();
    const oldResponse = page.waitForResponse(r => r.url() === delayed.request().url());
    await delayed.fulfill({ json: dashboardReport(777) }); await oldResponse;
    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    assert.equal(await page.getByText('777.00', { exact: false }).count(), 0);
    assert.ok(await page.getByText('22.00', { exact: false }).count());
    assert.equal(await page.getByTitle('طباعة التقرير', { exact: true }).isEnabled(), true);
  } finally { await context.close(); }
});

test('dashboard hides old figures and disables print when the current request fails', { timeout: 20000 }, async () => {
  let calls = 0;
  const { page, context } = await fixture('/dashboard', async (route, url) => {
    if (url.pathname === '/api/reports/dashboard') {
      if (++calls === 1) await route.fulfill({ json: dashboardReport(111) });
      else await route.fulfill({ status: 503, json: { message: 'Dashboard period unavailable' } });
      return true;
    }
  });
  try {
    await page.getByRole('button', { name: 'اليوم', exact: true }).click();
    await page.getByText('Dashboard period unavailable', { exact: true }).waitFor();
    assert.equal(await page.getByText('111.00', { exact: false }).count(), 0);
    assert.equal(await page.getByTitle('طباعة التقرير', { exact: true }).isDisabled(), true);
  } finally { await context.close(); }
});

test('product price edits retain the existing description and allow an explicit clear', { timeout: 20000 }, async () => {
  let product = { id: 1, name: 'وجبة الاختبار', description: 'وصف محفوظ', categoryId: 1,
    salePrice: 10, costPrice: 4, isAvailable: true };
  const payloads = [];
  const { page, context } = await fixture('/products', async (route, url) => {
    if (url.pathname === '/api/products' && route.request().method() === 'GET') {
      await route.fulfill({ json: [product] }); return true;
    }
    if (url.pathname === '/api/products/1' && route.request().method() === 'PUT') {
      const payload = route.request().postDataJSON(); payloads.push(payload); product = { ...product, ...payload };
      await route.fulfill({ status: 204 }); return true;
    }
  });
  try {
    await page.getByRole('button', { name: 'تعديل', exact: true }).click();
    assert.equal(await page.getByLabel('وصف المنتج (اختياري)', { exact: true }).inputValue(), 'وصف محفوظ');
    await page.locator('input[type=number]').first().fill('12');
    const firstSave = page.waitForResponse(r => r.url().endsWith('/api/products/1') && r.request().method() === 'PUT');
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).click();
    await firstSave;
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).waitFor({ state: 'hidden' });
    assert.equal(payloads[0].description, 'وصف محفوظ'); assert.equal(payloads[0].salePrice, 12);
    await page.getByRole('button', { name: 'تعديل', exact: true }).click();
    await page.getByLabel('وصف المنتج (اختياري)', { exact: true }).fill('');
    const secondSave = page.waitForResponse(r => r.url().endsWith('/api/products/1') && r.request().method() === 'PUT');
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).click();
    await secondSave; assert.equal(payloads[1].description, null);
  } finally { await context.close(); }
});

test('category routing shows every station and clears the entire set atomically', { timeout: 20000 }, async () => {
  const stations = [{ id: 10, name: 'محطة ألف' }, { id: 11, name: 'محطة باء' }];
  let links = [...stations];
  const { page, context, requests } = await fixture('/categories', async (route, url) => {
    if (url.pathname.toLowerCase() === '/api/printstations') { await route.fulfill({ json: stations }); return true; }
    if (url.pathname.toLowerCase() === '/api/categoryprintstations/category/1') {
      if (route.request().method() === 'PUT') {
        links = stations.filter(s => route.request().postDataJSON().printStationIds.includes(s.id));
        await route.fulfill({ status: 204 });
      } else await route.fulfill({ json: links });
      return true;
    }
    if (url.pathname === '/api/categories/1') { await route.fulfill({ status: 204 }); return true; }
  });
  try {
    await page.getByRole('button', { name: 'تعديل', exact: true }).click();
    for (const station of stations) assert.equal(await page.getByRole('checkbox', { name: station.name, exact: true }).isChecked(), true);
    await page.getByRole('button', { name: 'بدون محطة طباعة مخصصة', exact: true }).click();
    const replaced = page.waitForResponse(r => r.url().toLowerCase().endsWith('/api/categoryprintstations/category/1') && r.request().method() === 'PUT');
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).click();
    await replaced; assert.deepEqual(links, []);
    assert.equal(requests.filter(r => r.method === 'PUT' && r.url.pathname.toLowerCase().startsWith('/api/categoryprintstations/')).length, 1);
    assert.equal(requests.some(r => r.url.pathname.includes('/unlink/') || r.url.pathname.endsWith('/link')), false);
  } finally { await context.close(); }
});

test('failed category route replacement issues no unlink and retains the old route', { timeout: 20000 }, async () => {
  const stations = [{ id: 10, name: 'محطة ألف' }, { id: 11, name: 'محطة باء' }];
  let attempted;
  const { page, context, requests } = await fixture('/categories', async (route, url) => {
    if (url.pathname === '/api/printstations') { await route.fulfill({ json: stations }); return true; }
    if (url.pathname.toLowerCase() === '/api/categoryprintstations/category/1') {
      if (route.request().method() === 'PUT') {
        attempted = route.request().postDataJSON();
        await route.fulfill({ status: 503, json: { message: 'Routing replacement unavailable' } });
      } else await route.fulfill({ json: [stations[0]] });
      return true;
    }
    if (url.pathname === '/api/categories/1') { await route.fulfill({ status: 204 }); return true; }
  });
  try {
    await page.getByRole('button', { name: 'تعديل', exact: true }).click();
    await page.getByRole('checkbox', { name: 'محطة ألف', exact: true }).uncheck();
    await page.getByRole('checkbox', { name: 'محطة باء', exact: true }).check();
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).click();
    await page.getByText('Routing replacement unavailable', { exact: true }).waitFor();
    assert.deepEqual(attempted.printStationIds, [11]);
    assert.equal(requests.some(r => r.method === 'DELETE' || r.url.pathname.endsWith('/link')), false);
    await page.getByRole('button', { name: 'إلغاء', exact: true }).click();
    await page.getByRole('button', { name: 'تعديل', exact: true }).click();
    assert.equal(await page.getByRole('checkbox', { name: 'محطة ألف', exact: true }).isChecked(), true);
  } finally { await context.close(); }
});

test('retrying category routing after creation does not create a duplicate category', { timeout: 20000 }, async () => {
  let creates = 0, replacements = 0;
  const { page, context } = await fixture('/categories', async (route, url) => {
    if (url.pathname === '/api/categories' && route.request().method() === 'POST') {
      creates++; await route.fulfill({ json: { id: 9, name: 'تصنيف جديد' } }); return true;
    }
    if (url.pathname === '/api/categories/9') { await route.fulfill({ status: 204 }); return true; }
    if (url.pathname.toLowerCase() === '/api/categoryprintstations/category/9' && route.request().method() === 'PUT') {
      if (++replacements === 1) await route.fulfill({ status: 503, json: { message: 'Retry routing only' } });
      else await route.fulfill({ status: 204 });
      return true;
    }
  });
  try {
    await page.getByRole('button', { name: 'إضافة تصنيف جديد', exact: true }).click();
    await page.getByLabel('اسم التصنيف *', { exact: true }).fill('تصنيف جديد');
    await page.getByRole('button', { name: 'إضافة التصنيف', exact: true }).click();
    await page.getByText('Retry routing only', { exact: true }).waitFor();
    const replaced = page.waitForResponse(r => r.url().toLowerCase().endsWith('/api/categoryprintstations/category/9') && r.request().method() === 'PUT');
    await page.getByRole('button', { name: 'حفظ التعديلات', exact: true }).click();
    await replaced; assert.equal(creates, 1); assert.equal(replacements, 2);
  } finally { await context.close(); }
});
