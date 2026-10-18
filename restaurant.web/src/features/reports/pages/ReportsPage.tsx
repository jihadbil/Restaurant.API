import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  Calendar,
  RotateCcw,
  Printer,
  Download,
  Search,
  DollarSign,
  TrendingUp,
  ShoppingBag,
  Percent,
  XCircle,
  CreditCard,
  UtensilsCrossed,
  Layers,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import apiClient from '../../../services/apiClient';
import printService, { type ReportPrintData } from '../../../services/printService';
import { getCurrentRestaurant } from '../../../services/restaurantService';
import { useRestaurantInfo } from '../../../hooks/useRestaurantInfo';
import {
  DailySalesChart,
  TopProductsChart,
  PaymentMethodsChart,
  CategoryPerformanceChart,
} from '../components/ReportCharts';
import type {
  DailySalesReportDto,
  ProductReportDto,
  CategoryReportDto,
  PaymentMethodSalesReportDto,
  CancelledOrderDto,
} from '../../../types/api';
import toast from 'react-hot-toast';
import { localDate } from '../../../utils/localDate';
import { createCsv } from '../../../utils/csv';

type ReportTab = 'sales' | 'products' | 'categories' | 'paymentMethods' | 'cancelled';

export const ReportsPage: React.FC = () => {
  const { restaurant } = useRestaurantInfo();
  const [isPrinting, setIsPrinting] = useState(false);
  const [activeTab, setActiveTab] = useState<ReportTab>('sales');

  // Date filters (Default last 30 days)
  const [startDate, setStartDate] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return localDate(d);
  });
  const [endDate, setEndDate] = useState<string>(() => localDate());
  const [preset, setPreset] = useState<'today' | '7days' | '30days' | 'thisMonth'>('30days');

  // Search filter inside table
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Data states
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [dailySales, setDailySales] = useState<DailySalesReportDto[]>([]);
  const [products, setProducts] = useState<ProductReportDto[]>([]);
  const [categories, setCategories] = useState<CategoryReportDto[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethodSalesReportDto[]>([]);
  const [cancelledOrders, setCancelledOrders] = useState<CancelledOrderDto[]>([]);

  // Presets handler
  const handlePresetChange = (p: 'today' | '7days' | '30days' | 'thisMonth') => {
    setPreset(p);
    const today = new Date();
    const todayStr = localDate(today);

    if (p === 'today') {
      setStartDate(todayStr);
      setEndDate(todayStr);
    } else if (p === '7days') {
      const d = new Date();
      d.setDate(d.getDate() - 7);
      setStartDate(localDate(d));
      setEndDate(todayStr);
    } else if (p === '30days') {
      const d = new Date();
      d.setDate(d.getDate() - 30);
      setStartDate(localDate(d));
      setEndDate(todayStr);
    } else if (p === 'thisMonth') {
      setStartDate(`${localDate().slice(0, 7)}-01`);
      setEndDate(todayStr);
    }
  };

  const requestSequence = useRef(0);
  const [loadedKey, setLoadedKey] = useState<string | null>(null);
  const [reportError, setReportError] = useState<string | null>(null);
  const queryKey = JSON.stringify([activeTab, startDate, endDate]);
  const reportReady = !isLoading && !reportError && loadedKey === queryKey;

  // Load report data based on active tab
  const loadReportData = useCallback(async () => {
    const requestId = ++requestSequence.current;
    setIsLoading(true);
    setReportError(null);
    setLoadedKey(null);
    try {
      if (activeTab === 'sales') {
        const res = await apiClient.get<DailySalesReportDto[]>('/api/reports/daily-sales', {
          startDate,
          endDate,
        });
        if (requestId !== requestSequence.current) return;
        setDailySales(res || []);
      } else if (activeTab === 'products') {
        const res = await apiClient.get<ProductReportDto[]>('/api/reports/best-selling-products', {
          startDate,
          endDate,
          limit: 100,
        });
        if (requestId !== requestSequence.current) return;
        setProducts(res || []);
      } else if (activeTab === 'categories') {
        const res = await apiClient.get<CategoryReportDto[]>('/api/reports/best-selling-categories', {
          startDate,
          endDate,
          limit: 100,
        });
        if (requestId !== requestSequence.current) return;
        setCategories(res || []);
      } else if (activeTab === 'paymentMethods') {
        const res = await apiClient.get<PaymentMethodSalesReportDto[]>('/api/reports/payment-methods', {
          startDate,
          endDate,
        });
        if (requestId !== requestSequence.current) return;
        setPaymentMethods(res || []);
      } else if (activeTab === 'cancelled') {
        const res = await apiClient.get<CancelledOrderDto[]>('/api/reports/cancelled-orders', {
          startDate,
          endDate,
        });
        if (requestId !== requestSequence.current) return;
        setCancelledOrders(res || []);
      }
      if (requestId === requestSequence.current) setLoadedKey(queryKey);
    } catch (err: unknown) {
      if (requestId !== requestSequence.current) return;
      const msg = err instanceof Error ? err.message : 'فشل تحميل بيانات التقرير';
      setReportError(msg);
      toast.error(msg);
    } finally {
      if (requestId === requestSequence.current) setIsLoading(false);
    }
  }, [activeTab, startDate, endDate, queryKey]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void loadReportData(); }, 0);
    return () => window.clearTimeout(timer);
  }, [loadReportData]);

  // Tab definitions
  const tabs = [
    { id: 'sales' as ReportTab, label: 'المبيعات اليومية', icon: Calendar },
    { id: 'products' as ReportTab, label: 'الأصناف الأكثر مبيعاً', icon: UtensilsCrossed },
    { id: 'categories' as ReportTab, label: 'أداء التصنيفات', icon: Layers },
    { id: 'paymentMethods' as ReportTab, label: 'وسائل الدفع', icon: CreditCard },
    { id: 'cancelled' as ReportTab, label: 'الطلبات الملغاة', icon: XCircle },
  ];

  // Calculated totals for active tab
  const totals = useMemo(() => {
    if (activeTab === 'sales') {
      const orders = dailySales.reduce((sum, x) => sum + (x.totalOrders || x.ordersCount || 0), 0);
      const sales = dailySales.reduce((sum, x) => sum + (x.totalSales || 0), 0);
      const cost = dailySales.reduce((sum, x) => sum + (x.totalCost || 0), 0);
      const profit = dailySales.reduce((sum, x) => sum + (x.totalProfit || 0), 0);
      return { orders, sales, cost, profit, margin: sales > 0 ? (profit / sales) * 100 : 0 };
    }
    if (activeTab === 'products') {
      const qty = products.reduce((sum, x) => sum + (x.quantitySold || 0), 0);
      const revenue = products.reduce((sum, x) => sum + (x.totalRevenue || 0), 0);
      const cost = products.reduce((sum, x) => sum + (x.totalCost || 0), 0);
      const profit = products.reduce((sum, x) => sum + (x.totalProfit || 0), 0);
      return { qty, revenue, cost, profit, margin: revenue > 0 ? (profit / revenue) * 100 : 0 };
    }
    if (activeTab === 'categories') {
      const qty = categories.reduce((sum, x) => sum + (x.quantitySold || 0), 0);
      const revenue = categories.reduce((sum, x) => sum + (x.totalRevenue || 0), 0);
      const cost = categories.reduce((sum, x) => sum + (x.totalCost || 0), 0);
      const profit = categories.reduce((sum, x) => sum + (x.totalProfit || 0), 0);
      return { qty, revenue, cost, profit, margin: revenue > 0 ? (profit / revenue) * 100 : 0 };
    }
    if (activeTab === 'paymentMethods') {
      const orders = paymentMethods.reduce((sum, x) => sum + (x.totalOrders || x.ordersCount || 0), 0);
      const sales = paymentMethods.reduce((sum, x) => sum + (x.totalSales || 0), 0);
      return { orders, sales };
    }
    if (activeTab === 'cancelled') {
      const count = cancelledOrders.length;
      const total = cancelledOrders.reduce((sum, x) => sum + (x.total || 0), 0);
      return { count, total };
    }
    return {};
  }, [activeTab, dailySales, products, categories, paymentMethods, cancelledOrders]);

  // Export CSV
  const handleExportCSV = async () => {
    if (!reportReady) return;
    let name: string;
    try { name = (await getCurrentRestaurant()).name; }
    catch (cause: unknown) { toast.error(cause instanceof Error ? cause.message : 'تعذر تحميل اسم المطعم.'); return; }
    const rows: (string | number)[][] = [['المطعم', name], ['الفترة', `من ${startDate} إلى ${endDate}`]];
    if (activeTab === 'sales') {
      rows.push(['التاريخ', 'عدد الطلبات', 'المبيعات قبل الاسترداد', 'الاستردادات', 'صافي المبيعات', 'التكلفة', 'الربح']);
      dailySales.forEach(d => rows.push([new Date(d.date).toLocaleDateString('ar-LY'), d.totalOrders || d.ordersCount || 0,
        d.grossSales ?? d.totalSales ?? 0, d.totalRefunds ?? 0, d.totalSales ?? 0, d.totalCost ?? 0, d.totalProfit ?? 0]));
    } else if (activeTab === 'products') {
      rows.push(['الصنف', 'الباركود', 'صافي الكمية', 'صافي الإيراد', 'التكلفة', 'الربح']);
      products.forEach(x => rows.push([x.name, x.barCode || '', x.quantitySold || 0, x.totalRevenue || 0, x.totalCost || 0, x.totalProfit || 0]));
    } else if (activeTab === 'categories') {
      rows.push(['التصنيف', 'صافي الكمية', 'صافي المبيعات', 'التكلفة', 'الربح']);
      categories.forEach(x => rows.push([x.name, x.quantitySold || 0, x.totalRevenue || 0, x.totalCost || 0, x.totalProfit || 0]));
    } else if (activeTab === 'paymentMethods') {
      rows.push(['طريقة الدفع', 'عدد العمليات', 'المبيعات قبل الاسترداد', 'الاستردادات', 'صافي التحصيل']);
      paymentMethods.forEach(x => rows.push([x.paymentMethodName || x.name || '', x.totalOrders || x.ordersCount || 0,
        x.grossSales ?? x.totalSales ?? 0, x.totalRefunds ?? 0, x.totalSales || 0]));
    } else {
      rows.push(['رقم الطلب', 'تاريخ الإلغاء', 'المستخدم', 'المبلغ', 'الملاحظات']);
      cancelledOrders.forEach(x => rows.push([x.orderNumber, new Date(x.date).toLocaleString('ar-LY'), x.userName || '', x.total || 0, x.notes || x.reason || '']));
    }
    const url = URL.createObjectURL(new Blob([createCsv(rows)], { type: 'text/csv;charset=utf-8;' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `report-${activeTab}-${startDate}-to-${endDate}.csv`;
    document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    toast.success('تم تصدير ملف التقرير بنجاح');
  };

  // Executive A4 Financial Report Printing
  const handlePrintReport = async (mode: 'dialog' | 'document' = 'dialog') => {
    if (isPrinting || !reportReady) return;
    setIsPrinting(true);
    try {
    const periodText = `من ${startDate} إلى ${endDate}`;
    const print = (data: ReportPrintData) => printService.printReport(data, mode);

    if (activeTab === 'sales') {
      await print({
        title: 'تقرير حركة المبيعات والأرباح اليومية',
        periodText,
        kpis: [
          { label: 'صافي المبيعات', value: `${(totals.sales || 0).toFixed(2)} د.ل`, color: '#16a34a' },
          { label: 'إجمالي التكلفة', value: `${(totals.cost || 0).toFixed(2)} د.ل` },
          { label: 'صافي الأرباح', value: `${(totals.profit || 0).toFixed(2)} د.ل`, color: '#2563eb' },
          { label: 'عدد الطلبات', value: `${totals.orders || 0} طلب` },
          { label: 'هامش الربح', value: `${(totals.margin || 0).toFixed(1)}%` },
        ],
        headers: ['التاريخ', 'عدد الطلبات', 'المبيعات (د.ل)', 'التكلفة (د.ل)', 'الربح (د.ل)', 'هامش الربح'],
        rows: dailySales.map((d) => {
          const dateStr = d.date ? new Date(d.date).toLocaleDateString('ar-LY') : '';
          const orders = d.totalOrders || d.ordersCount || 0;
          const margin = (d.totalSales || 0) > 0 ? (((d.totalProfit || 0) / d.totalSales) * 100).toFixed(1) : '0';
          return [dateStr, orders, (d.totalSales || 0).toFixed(2), (d.totalCost || 0).toFixed(2), (d.totalProfit || 0).toFixed(2), `${margin}%`];
        }),
        totals: ['الإجمالي العام', totals.orders || 0, (totals.sales || 0).toFixed(2), (totals.cost || 0).toFixed(2), (totals.profit || 0).toFixed(2), `${(totals.margin || 0).toFixed(1)}%`],
      });
    } else if (activeTab === 'products') {
      await print({
        title: 'تقرير الأصناف الأكثر مبيعاً وتحقيقاً للإيراد',
        periodText,
        kpis: [
          { label: 'إجمالي القطع المباعة', value: `${totals.qty || 0} قطعة` },
          { label: 'إجمالي الإيرادات', value: `${(totals.revenue || 0).toFixed(2)} د.ل`, color: '#16a34a' },
          { label: 'صافي أرباح الأصناف', value: `${(totals.profit || 0).toFixed(2)} د.ل`, color: '#2563eb' },
        ],
        headers: ['الصنف', 'الباركود', 'الكمية المباعة', 'الإيراد (د.ل)', 'التكلفة (د.ل)', 'الربح (د.ل)', 'هامش الربح'],
        rows: products.map((p) => {
          const margin = (p.totalRevenue || 0) > 0 ? (((p.totalProfit || 0) / p.totalRevenue) * 100).toFixed(1) : '0';
          return [p.name, p.barCode || '-', p.quantitySold || 0, (p.totalRevenue || 0).toFixed(2), (p.totalCost || 0).toFixed(2), (p.totalProfit || 0).toFixed(2), `${margin}%`];
        }),
      });
    } else if (activeTab === 'categories') {
      await print({
        title: 'تقرير أداء ومبيعات التصنيفات الغذائية',
        periodText,
        kpis: [
          { label: 'القطع المباعة', value: `${totals.qty || 0} قطعة` },
          { label: 'صافي المبيعات', value: `${(totals.revenue || 0).toFixed(2)} د.ل`, color: '#16a34a' },
          { label: 'صافي الأرباح', value: `${(totals.profit || 0).toFixed(2)} د.ل`, color: '#2563eb' },
        ],
        headers: ['التصنيف', 'الكمية المباعة', 'صافي المبيعات (د.ل)', 'التكلفة (د.ل)', 'الربح (د.ل)', 'نسبة الهامش'],
        rows: categories.map((c) => {
          const margin = (c.totalRevenue || 0) > 0 ? (((c.totalProfit || 0) / c.totalRevenue) * 100).toFixed(1) : '0';
          return [c.name, c.quantitySold || 0, (c.totalRevenue || 0).toFixed(2), (c.totalCost || 0).toFixed(2), (c.totalProfit || 0).toFixed(2), `${margin}%`];
        }),
      });
    } else if (activeTab === 'paymentMethods') {
      const grandTotal = paymentMethods.reduce((acc, x) => acc + (x.totalSales || 0), 0);
      await print({
        title: 'تقرير إيرادات وسائل الدفع والتحصيل',
        periodText,
        kpis: [
          { label: 'صافي التحصيل', value: `${(totals.sales || 0).toFixed(2)} د.ل`, color: '#16a34a' },
          { label: 'إجمالي العمليات', value: `${totals.orders || 0} عملية` },
        ],
        headers: ['طريقة الدفع', 'عدد العمليات', 'صافي المبيعات (د.ل)', 'النسبة من الإجمالي'],
        rows: paymentMethods.map((pm) => {
          const pct = grandTotal > 0 ? (((pm.totalSales || 0) / grandTotal) * 100).toFixed(1) : '0';
          return [pm.paymentMethodName || pm.name || 'طريقة دفع', pm.totalOrders || pm.ordersCount || 0, (pm.totalSales || 0).toFixed(2), `${pct}%`];
        }),
      });
    } else if (activeTab === 'cancelled') {
      await print({
        title: 'تقرير الطلبات والفواتير الملغاة',
        periodText,
        kpis: [
          { label: 'عدد الطلبات الملغاة', value: `${totals.count || 0} طلب`, color: '#dc2626' },
          { label: 'إجمالي المبالغ الملغاة المستردة', value: `${(totals.total || 0).toFixed(2)} د.ل`, color: '#dc2626' },
        ],
        headers: ['رقم الطلب', 'التاريخ والوقت', 'المستخدم', 'المبلغ (د.ل)', 'سبب الإلغاء والملاحظات'],
        rows: cancelledOrders.map((co) => {
          const dateStr = co.date ? new Date(co.date).toLocaleString('ar-LY') : '';
          return [`#${co.orderNumber}`, dateStr, co.userName || '-', (co.total || 0).toFixed(2), co.notes || co.reason || 'لا يوجد'];
        }),
      });
    }
    } catch (cause: unknown) {
      toast.error(cause instanceof Error ? cause.message : 'تعذر فتح طباعة التقرير.');
    } finally { setIsPrinting(false); }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Top Header & Date Filter Bar */}
      <div
        style={{
          background: 'var(--color-surface)',
          padding: '20px 24px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '16px',
        }}
      >
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, margin: 0, color: 'var(--color-text-main)' }}>
            مركز التقارير والإحصائيات
          </h1>
          {restaurant && <div style={{ fontWeight: 700, marginTop: '6px' }}>{restaurant.name}</div>}
          <p style={{ margin: '4px 0 0 0', fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
            تقارير مالية وتشغيلية تفصيلية مع إمكانية التصفية، الطباعة والتصدير
          </p>
        </div>

        {/* Date Filter & Presets */}
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '10px' }}>
          {/* Quick Presets */}
          <div
            style={{
              display: 'flex',
              background: 'var(--color-bg-base)',
              padding: '4px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
              gap: '4px',
            }}
          >
            {(
              [
                { id: 'today', label: 'اليوم' },
                { id: '7days', label: '٧ أيام' },
                { id: '30days', label: '٣٠ يوماً' },
                { id: 'thisMonth', label: 'هذا الشهر' },
              ] as const
            ).map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => handlePresetChange(item.id)}
                style={{
                  border: 'none',
                  background: preset === item.id ? 'var(--color-primary)' : 'transparent',
                  color: preset === item.id ? '#ffffff' : 'var(--color-text-muted)',
                  padding: '6px 12px',
                  borderRadius: 'var(--radius-sm)',
                  fontSize: '0.8rem',
                  fontWeight: preset === item.id ? 700 : 500,
                  cursor: 'pointer',
                  transition: 'all 0.15s ease',
                }}
              >
                {item.label}
              </button>
            ))}
          </div>

          {/* Date Pickers */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <input
              type="date"
              value={startDate}
              onChange={(e) => {
                setStartDate(e.target.value);
                setPreset('30days');
              }}
              style={{
                padding: '8px 12px',
                fontSize: '0.85rem',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                background: 'var(--color-bg-base)',
                color: 'var(--color-text-main)',
                outline: 'none',
              }}
            />
            <span style={{ color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>إلى</span>
            <input
              type="date"
              value={endDate}
              onChange={(e) => {
                setEndDate(e.target.value);
                setPreset('30days');
              }}
              style={{
                padding: '8px 12px',
                fontSize: '0.85rem',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                background: 'var(--color-bg-base)',
                color: 'var(--color-text-main)',
                outline: 'none',
              }}
            />
          </div>

          <Button variant="secondary" size="md" icon={<RotateCcw size={16} />} onClick={loadReportData} title="تحديث">
            تحديث
          </Button>

          <Button variant="secondary" size="md" icon={<Download size={16} />} onClick={handleExportCSV} disabled={!reportReady} title="تصدير CSV">
            تصدير CSV
          </Button>

          <Button variant="secondary" size="md" onClick={() => handlePrintReport('document')} disabled={isPrinting || !reportReady}>
            فتح صفحة الطباعة
          </Button>
          <Button variant="primary" size="md" icon={<Printer size={16} />} onClick={() => handlePrintReport()} isLoading={isPrinting} disabled={!reportReady} title="طباعة تقرير مالي A4">
            طباعة التقرير
          </Button>
        </div>
      </div>

      {reportError && <p role="alert">تعذر تحميل التقرير للفترة المحددة: {reportError}</p>}
      {/* Navigation Tabs */}
      <div
        style={{
          display: 'flex',
          gap: '8px',
          borderBottom: '2px solid var(--color-border)',
          paddingBottom: '2px',
          overflowX: 'auto',
        }}
      >
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => {
                setActiveTab(tab.id);
                setSearchQuery('');
              }}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '10px 18px',
                background: isActive ? 'var(--color-surface)' : 'transparent',
                border: 'none',
                borderBottom: isActive ? '3px solid var(--color-primary)' : '3px solid transparent',
                borderRadius: 'var(--radius-md) var(--radius-md) 0 0',
                color: isActive ? 'var(--color-primary)' : 'var(--color-text-muted)',
                fontWeight: isActive ? 700 : 500,
                fontSize: '0.9rem',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
                whiteSpace: 'nowrap',
              }}
            >
              <Icon size={18} />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* Summary KPI Cards for Active Tab */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '14px',
        }}
      >
        {activeTab === 'sales' && (
          <>
            <SummaryCard title="عدد عمليات البيع" value={`${totals.orders || 0} طلب`} icon={<ShoppingBag size={22} />} color="#f59e0b" />
            <SummaryCard title="صافي المبيعات" value={`${(totals.sales || 0).toFixed(2)} د.ل`} icon={<DollarSign size={22} />} color="var(--color-primary)" />
            <SummaryCard title="إجمالي التكلفة" value={`${(totals.cost || 0).toFixed(2)} د.ل`} icon={<TrendingUp size={22} />} color="#94a3b8" />
            <SummaryCard title="صافي الأرباح" value={`${(totals.profit || 0).toFixed(2)} د.ل`} subtitle={`هامش الربح: ${(totals.margin || 0).toFixed(1)}%`} icon={<Percent size={22} />} color="#0ea5e9" />
          </>
        )}

        {activeTab === 'products' && (
          <>
            <SummaryCard title="إجمالي القطع المباعة" value={`${totals.qty || 0} قطعة`} icon={<ShoppingBag size={22} />} color="#f59e0b" />
            <SummaryCard title="إجمالي إيرادات الأصناف" value={`${(totals.revenue || 0).toFixed(2)} د.ل`} icon={<DollarSign size={22} />} color="var(--color-primary)" />
            <SummaryCard title="إجمالي تكلفة المواد" value={`${(totals.cost || 0).toFixed(2)} د.ل`} icon={<TrendingUp size={22} />} color="#94a3b8" />
            <SummaryCard title="صافي الأرباح المحققة" value={`${(totals.profit || 0).toFixed(2)} د.ل`} subtitle={`هامش الربح: ${(totals.margin || 0).toFixed(1)}%`} icon={<Percent size={22} />} color="#0ea5e9" />
          </>
        )}

        {activeTab === 'categories' && (
          <>
            <SummaryCard title="إجمالي القطع بالتصنيفات" value={`${totals.qty || 0} قطعة`} icon={<Layers size={22} />} color="#f59e0b" />
            <SummaryCard title="إجمالي مبيعات التصنيفات" value={`${(totals.revenue || 0).toFixed(2)} د.ل`} icon={<DollarSign size={22} />} color="var(--color-primary)" />
            <SummaryCard title="إجمالي التكلفة" value={`${(totals.cost || 0).toFixed(2)} د.ل`} icon={<TrendingUp size={22} />} color="#94a3b8" />
            <SummaryCard title="صافي الأرباح" value={`${(totals.profit || 0).toFixed(2)} د.ل`} subtitle={`هامش الربح: ${(totals.margin || 0).toFixed(1)}%`} icon={<Percent size={22} />} color="#0ea5e9" />
          </>
        )}

        {activeTab === 'paymentMethods' && (
          <>
            <SummaryCard title="إجمالي عدد العمليات" value={`${totals.orders || 0} عملية`} icon={<CreditCard size={22} />} color="#f59e0b" />
            <SummaryCard title="إجمالي المبالغ المحصلة" value={`${(totals.sales || 0).toFixed(2)} د.ل`} icon={<DollarSign size={22} />} color="var(--color-primary)" />
          </>
        )}

        {activeTab === 'cancelled' && (
          <>
            <SummaryCard title="عدد الطلبات الملغاة" value={`${totals.count || 0} طلب`} icon={<XCircle size={22} />} color="#ef4444" />
            <SummaryCard title="إجمالي قيمة الطلبات الملغاة" value={`${(totals.total || 0).toFixed(2)} د.ل`} icon={<DollarSign size={22} />} color="#ef4444" />
          </>
        )}
      </div>

      {/* Visual Analytics Charts for Active Tab */}
      {!isLoading && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {activeTab === 'sales' && <DailySalesChart data={dailySales} />}
          {activeTab === 'products' && <TopProductsChart products={products} />}
          {activeTab === 'categories' && <CategoryPerformanceChart categories={categories} />}
          {activeTab === 'paymentMethods' && <PaymentMethodsChart data={paymentMethods} />}
        </div>
      )}

      {/* Search Input Filter */}
      <div style={{ maxWidth: '360px' }}>
        <Input
          placeholder="بحث سريع في بيانات الجدول..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          icon={<Search size={16} />}
        />
      </div>

      {/* Main Report Table Container */}
      <div
        style={{
          background: 'var(--color-surface)',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          overflow: 'hidden',
        }}
      >
        {isLoading ? (
          <div style={{ padding: '60px 0', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
            <LoadingSpinner size="lg" />
            <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>جاري احتساب وتجميع بيانات التقرير...</span>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            {/* 1. Daily Sales Table */}
            {activeTab === 'sales' && (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>التاريخ</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>عدد الطلبات</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>صافي المبيعات</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي التكلفة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>صافي الربح</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>هامش الربح</th>
                  </tr>
                </thead>
                <tbody>
                  {dailySales.length === 0 ? (
                    <tr>
                      <td colSpan={6} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                        لا توجد بيانات مبيعات في الفترة المحددة
                      </td>
                    </tr>
                  ) : (
                    dailySales
                      .filter((row) => {
                        if (!searchQuery) return true;
                        const dateStr = row.date ? new Date(row.date).toLocaleDateString('ar-LY') : '';
                        return dateStr.includes(searchQuery);
                      })
                      .map((row, idx) => {
                        const margin =
                          row.totalSales > 0 ? ((row.totalProfit / row.totalSales) * 100).toFixed(1) : '0';
                        return (
                          <tr
                            key={idx}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', fontWeight: 600 }}>
                              {new Date(row.date).toLocaleDateString('ar-LY', {
                                weekday: 'short',
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric',
                              })}
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                              {row.totalOrders || row.ordersCount || 0} طلب
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-primary)' }}>
                              {(row.totalSales || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                              {(row.totalCost || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: '#0ea5e9' }}>
                              {(row.totalProfit || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '3px 8px',
                                  borderRadius: 'var(--radius-sm)',
                                  background: 'rgba(14, 165, 233, 0.1)',
                                  color: '#0ea5e9',
                                  fontSize: '0.8rem',
                                  fontWeight: 700,
                                }}
                              >
                                {margin}%
                              </span>
                            </td>
                          </tr>
                        );
                      })
                  )}
                </tbody>
              </table>
            )}

            {/* 2. Best Selling Products Table */}
            {activeTab === 'products' && (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم الصنف</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>الباركود</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>الكمية المباعة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي الإيراد</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي التكلفة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>الربح المحقق</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>هامش الربح</th>
                  </tr>
                </thead>
                <tbody>
                  {products.length === 0 ? (
                    <tr>
                      <td colSpan={8} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                        لا توجد بيانات للأصناف في الفترة المحددة
                      </td>
                    </tr>
                  ) : (
                    products
                      .filter((p) => {
                        if (!searchQuery) return true;
                        return (
                          p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          (p.barCode && p.barCode.includes(searchQuery))
                        );
                      })
                      .map((prod, idx) => {
                        const margin =
                          prod.totalRevenue > 0 ? ((prod.totalProfit / prod.totalRevenue) * 100).toFixed(1) : '0';
                        return (
                          <tr
                            key={prod.productId || idx}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontWeight: 700 }}>
                              {idx + 1}
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                              {prod.name}
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontFamily: 'monospace' }}>
                              {prod.barCode || '-'}
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-primary)' }}>
                              {prod.quantitySold} قطعة
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700 }}>
                              {(prod.totalRevenue || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                              {(prod.totalCost || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: '#0ea5e9' }}>
                              {(prod.totalProfit || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '3px 8px',
                                  borderRadius: 'var(--radius-sm)',
                                  background: 'rgba(14, 165, 233, 0.1)',
                                  color: '#0ea5e9',
                                  fontSize: '0.8rem',
                                  fontWeight: 700,
                                }}
                              >
                                {margin}%
                              </span>
                            </td>
                          </tr>
                        );
                      })
                  )}
                </tbody>
              </table>
            )}

            {/* 3. Categories Table */}
            {activeTab === 'categories' && (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم التصنيف</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>الكمية المباعة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>صافي المبيعات</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي التكلفة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>صافي الربح</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>هامش الربح</th>
                  </tr>
                </thead>
                <tbody>
                  {categories.length === 0 ? (
                    <tr>
                      <td colSpan={7} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                        لا توجد بيانات للتصنيفات في الفترة المحددة
                      </td>
                    </tr>
                  ) : (
                    categories
                      .filter((c) => !searchQuery || c.name.toLowerCase().includes(searchQuery.toLowerCase()))
                      .map((cat, idx) => {
                        const margin =
                          cat.totalRevenue > 0 ? ((cat.totalProfit / cat.totalRevenue) * 100).toFixed(1) : '0';
                        return (
                          <tr
                            key={cat.categoryId || idx}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontWeight: 700 }}>
                              {idx + 1}
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                              {cat.name}
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-primary)' }}>
                              {cat.quantitySold} قطعة
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700 }}>
                              {(cat.totalRevenue || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                              {(cat.totalCost || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: '#0ea5e9' }}>
                              {(cat.totalProfit || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '3px 8px',
                                  borderRadius: 'var(--radius-sm)',
                                  background: 'rgba(14, 165, 233, 0.1)',
                                  color: '#0ea5e9',
                                  fontSize: '0.8rem',
                                  fontWeight: 700,
                                }}
                              >
                                {margin}%
                              </span>
                            </td>
                          </tr>
                        );
                      })
                  )}
                </tbody>
              </table>
            )}

            {/* 4. Payment Methods Table */}
            {activeTab === 'paymentMethods' && (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>طريقة الدفع</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>عدد العمليات</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي المبالغ المحصلة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>النسبة من الإجمالي</th>
                  </tr>
                </thead>
                <tbody>
                  {paymentMethods.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                        لا توجد حركات دفع مسجلة في الفترة المحددة
                      </td>
                    </tr>
                  ) : (
                    paymentMethods
                      .filter((pm) => {
                        const name = pm.paymentMethodName || pm.name || '';
                        return !searchQuery || name.toLowerCase().includes(searchQuery.toLowerCase());
                      })
                      .map((pm, idx) => {
                        const totalSales = totals.sales || 1;
                        const pct = totalSales > 0 ? ((pm.totalSales / totalSales) * 100).toFixed(1) : '0';
                        return (
                          <tr
                            key={pm.paymentMethodId || idx}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontWeight: 700 }}>
                              {idx + 1}
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                              {pm.paymentMethodName || pm.name}
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                              {pm.totalOrders || pm.ordersCount || 0} عملية
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-primary)' }}>
                              {(pm.totalSales || 0).toFixed(2)} د.ل
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '3px 8px',
                                  borderRadius: 'var(--radius-sm)',
                                  background: 'rgba(56, 224, 123, 0.1)',
                                  color: 'var(--color-primary)',
                                  fontSize: '0.8rem',
                                  fontWeight: 700,
                                }}
                              >
                                {pct}%
                              </span>
                            </td>
                          </tr>
                        );
                      })
                  )}
                </tbody>
              </table>
            )}

            {/* 5. Cancelled Orders Table */}
            {activeTab === 'cancelled' && (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                <thead>
                  <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>رقم الطلب</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>التاريخ والوقت</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>المستخدم المسؤول</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>إجمالي القيمة</th>
                    <th style={{ padding: '12px 16px', fontWeight: 700 }}>سبب الإلغاء والملاحظات</th>
                  </tr>
                </thead>
                <tbody>
                  {cancelledOrders.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                        لا توجد طلبات ملغاة في الفترة المحددة
                      </td>
                    </tr>
                  ) : (
                    cancelledOrders
                      .filter((co) => {
                        if (!searchQuery) return true;
                        const query = searchQuery.toLowerCase();
                        return (
                          co.orderNumber.toString().includes(query) ||
                          (co.userName && co.userName.toLowerCase().includes(query)) ||
                          (co.notes && co.notes.toLowerCase().includes(query))
                        );
                      })
                      .map((co, idx) => (
                        <tr
                          key={co.orderId || idx}
                          style={{
                            borderBottom: '1px solid var(--color-border)',
                            background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                          }}
                        >
                          <td style={{ padding: '12px 16px', fontWeight: 800, color: 'var(--color-text-main)' }}>
                            #{co.orderNumber}
                          </td>
                          <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                            {new Date(co.date).toLocaleString('ar-LY')}
                          </td>
                          <td style={{ padding: '12px 16px', fontWeight: 600 }}>
                            {co.userName || 'غير محدد'}
                          </td>
                          <td style={{ padding: '12px 16px', fontWeight: 700, color: '#ef4444' }}>
                            {(co.total || 0).toFixed(2)} د.ل
                          </td>
                          <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                            {co.notes || co.reason || '-'}
                          </td>
                        </tr>
                      ))
                  )}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

// Reusable Summary Metric Card Component
interface SummaryCardProps {
  title: string;
  value: string;
  subtitle?: string;
  icon: React.ReactNode;
  color: string;
}

const SummaryCard: React.FC<SummaryCardProps> = ({ title, value, subtitle, icon, color }) => (
  <div
    style={{
      background: 'var(--color-surface)',
      borderRadius: 'var(--radius-lg)',
      border: '1px solid var(--color-border)',
      padding: '16px 20px',
      display: 'flex',
      alignItems: 'center',
      gap: '14px',
    }}
  >
    <div
      style={{
        width: '44px',
        height: '44px',
        borderRadius: 'var(--radius-md)',
        background: `${color}18`,
        color,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
      }}
    >
      {icon}
    </div>
    <div>
      <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>{title}</span>
      <div
        style={{
          fontSize: '1.25rem',
          fontWeight: 800,
          color: 'var(--color-text-main)',
          marginTop: '2px',
          fontFamily: 'monospace',
        }}
      >
        {value}
      </div>
      {subtitle && <div style={{ fontSize: '0.7rem', color, marginTop: '2px', fontWeight: 600 }}>{subtitle}</div>}
    </div>
  </div>
);

export default ReportsPage;
