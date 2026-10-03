import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  TrendingUp,
  DollarSign,
  ShoppingBag,
  Percent,
  RotateCcw,
  Award,
  CreditCard,
  UtensilsCrossed,
  Printer,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import apiClient from '../../../services/apiClient';
import type { ComprehensiveReportDto } from '../../../types/api';
import toast from 'react-hot-toast';

export const DashboardPage: React.FC = () => {
  const [data, setData] = useState<ComprehensiveReportDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Date filters
  const [startDate, setStartDate] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0];
  });
  const [endDate, setEndDate] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [preset, setPreset] = useState<'today' | '7days' | '30days' | 'thisMonth'>('30days');

  // Quick preset handler
  const handlePresetChange = (p: 'today' | '7days' | '30days' | 'thisMonth') => {
    setPreset(p);
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];

    if (p === 'today') {
      setStartDate(todayStr);
      setEndDate(todayStr);
    } else if (p === '7days') {
      const d = new Date();
      d.setDate(d.getDate() - 7);
      setStartDate(d.toISOString().split('T')[0]);
      setEndDate(todayStr);
    } else if (p === '30days') {
      const d = new Date();
      d.setDate(d.getDate() - 30);
      setStartDate(d.toISOString().split('T')[0]);
      setEndDate(todayStr);
    } else if (p === 'thisMonth') {
      const d = new Date(today.getFullYear(), today.getMonth(), 1);
      setStartDate(d.toISOString().split('T')[0]);
      setEndDate(todayStr);
    }
  };

  const loadDashboard = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await apiClient.get<ComprehensiveReportDto>('/api/reports/comprehensive', {
        startDate,
        endDate,
      });
      setData(res);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل تحميل مؤشرات لوحة القيادة';
      toast.error(msg);
    } finally {
      setIsLoading(false);
    }
  }, [startDate, endDate]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  // Max sold for progress bar
  const maxSold = useMemo(() => {
    if (!data?.bestSellingProducts?.length) return 1;
    return Math.max(...data.bestSellingProducts.map((p) => p.quantitySold), 1);
  }, [data]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Top Header & Filter Controls */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '16px',
          background: 'var(--color-surface)',
          padding: '20px 24px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
        }}
      >
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, margin: 0, color: 'var(--color-text-main)' }}>
            لوحة القيادة والمؤشرات العامة
          </h1>
          <p style={{ margin: '4px 0 0 0', fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
            نظرة شاملة على أداء المبيعات، الأرباح، والأصناف الأكثر طلباً
          </p>
        </div>

        {/* Date Filters & Presets */}
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '10px' }}>
          {/* Preset Buttons */}
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
            <div style={{ position: 'relative' }}>
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
            </div>
            <span style={{ color: 'var(--color-text-muted)', fontSize: '0.85rem' }}>إلى</span>
            <div>
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
          </div>

          <Button
            variant="secondary"
            size="md"
            icon={<RotateCcw size={16} />}
            onClick={loadDashboard}
            title="تحديث البيانات"
          >
            تحديث
          </Button>

          <Button
            variant="ghost"
            size="md"
            icon={<Printer size={16} />}
            onClick={() => window.print()}
            title="طباعة التقرير"
          >
            طباعة
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div style={{ padding: '60px 0', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
          <LoadingSpinner size="lg" />
          <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>جاري احتساب مؤشرات لوحة القيادة...</span>
        </div>
      ) : !data ? (
        <EmptyState
          title="لا تتوفر بيانات"
          description="لم يتم العثور على أي حركات بيع خلال الفترة الزمنية المحددة."
        />
      ) : (
        <>
          {/* Main 4 KPI Metric Cards */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
              gap: '16px',
            }}
          >
            {/* 1. Total Sales */}
            <div
              style={{
                background: 'linear-gradient(135deg, rgba(56, 224, 123, 0.08) 0%, var(--color-surface) 100%)',
                padding: '20px',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid rgba(56, 224, 123, 0.3)',
                display: 'flex',
                alignItems: 'center',
                gap: '16px',
              }}
            >
              <div
                style={{
                  width: '52px',
                  height: '52px',
                  borderRadius: 'var(--radius-md)',
                  background: 'rgba(56, 224, 123, 0.15)',
                  color: 'var(--color-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <DollarSign size={28} />
              </div>
              <div>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>
                  إجمالي المبيعات
                </span>
                <div
                  style={{
                    fontSize: '1.5rem',
                    fontWeight: 800,
                    color: 'var(--color-text-main)',
                    marginTop: '2px',
                    fontFamily: 'monospace',
                  }}
                >
                  {(data.totalSales || 0).toFixed(2)}{' '}
                  <span style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--color-primary)' }}>د.ل</span>
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                  صافي العمليات المكتملة
                </div>
              </div>
            </div>

            {/* 2. Total Profit */}
            <div
              style={{
                background: 'linear-gradient(135deg, rgba(14, 165, 233, 0.08) 0%, var(--color-surface) 100%)',
                padding: '20px',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid rgba(14, 165, 233, 0.3)',
                display: 'flex',
                alignItems: 'center',
                gap: '16px',
              }}
            >
              <div
                style={{
                  width: '52px',
                  height: '52px',
                  borderRadius: 'var(--radius-md)',
                  background: 'rgba(14, 165, 233, 0.15)',
                  color: '#0ea5e9',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <TrendingUp size={28} />
              </div>
              <div>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>
                  إجمالي صافي الربح
                </span>
                <div
                  style={{
                    fontSize: '1.5rem',
                    fontWeight: 800,
                    color: 'var(--color-text-main)',
                    marginTop: '2px',
                    fontFamily: 'monospace',
                  }}
                >
                  {(data.totalProfit || 0).toFixed(2)}{' '}
                  <span style={{ fontSize: '0.9rem', fontWeight: 600, color: '#0ea5e9' }}>د.ل</span>
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                  هامش الربح:{' '}
                  {data.totalSales > 0 ? (((data.totalProfit || 0) / data.totalSales) * 100).toFixed(1) : 0}%
                </div>
              </div>
            </div>

            {/* 3. Total Orders */}
            <div
              style={{
                background: 'linear-gradient(135deg, rgba(245, 158, 11, 0.08) 0%, var(--color-surface) 100%)',
                padding: '20px',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                display: 'flex',
                alignItems: 'center',
                gap: '16px',
              }}
            >
              <div
                style={{
                  width: '52px',
                  height: '52px',
                  borderRadius: 'var(--radius-md)',
                  background: 'rgba(245, 158, 11, 0.15)',
                  color: '#f59e0b',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <ShoppingBag size={28} />
              </div>
              <div>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>
                  عدد الطلبات المكتملة
                </span>
                <div
                  style={{
                    fontSize: '1.5rem',
                    fontWeight: 800,
                    color: 'var(--color-text-main)',
                    marginTop: '2px',
                    fontFamily: 'monospace',
                  }}
                >
                  {data.totalOrdersCount || 0}{' '}
                  <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>طلب</span>
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                  ملغاة: {data.cancelledOrdersCount || 0}
                </div>
              </div>
            </div>

            {/* 4. Average Order Value */}
            <div
              style={{
                background: 'linear-gradient(135deg, rgba(168, 85, 247, 0.08) 0%, var(--color-surface) 100%)',
                padding: '20px',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid rgba(168, 85, 247, 0.3)',
                display: 'flex',
                alignItems: 'center',
                gap: '16px',
              }}
            >
              <div
                style={{
                  width: '52px',
                  height: '52px',
                  borderRadius: 'var(--radius-md)',
                  background: 'rgba(168, 85, 247, 0.15)',
                  color: '#a855f7',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <Percent size={28} />
              </div>
              <div>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>
                  متوسط قيمة الطلب (AOV)
                </span>
                <div
                  style={{
                    fontSize: '1.5rem',
                    fontWeight: 800,
                    color: 'var(--color-text-main)',
                    marginTop: '2px',
                    fontFamily: 'monospace',
                  }}
                >
                  {(data.averageOrderValue || 0).toFixed(2)}{' '}
                  <span style={{ fontSize: '0.9rem', fontWeight: 600, color: '#a855f7' }}>د.ل</span>
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                  إجمالي الخصومات: {(data.totalDiscount || 0).toFixed(2)} د.ل
                </div>
              </div>
            </div>
          </div>

          {/* Grid Layout: Best Selling Products & Sales Breakdown */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: '20px' }}>
            {/* Left Card: Best Selling Products (Progress Bars) */}
            <div
              style={{
                background: 'var(--color-surface)',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid var(--color-border)',
                padding: '24px',
                display: 'flex',
                flexDirection: 'column',
                gap: '20px',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div
                    style={{
                      width: '36px',
                      height: '36px',
                      borderRadius: 'var(--radius-md)',
                      background: 'rgba(56, 224, 123, 0.15)',
                      color: 'var(--color-primary)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Award size={20} />
                  </div>
                  <div>
                    <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700 }}>الأصناف الأكثر طلباً ومبيعاً</h3>
                    <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                      مرتبة حسب حجم الكمية المباعة
                    </span>
                  </div>
                </div>
                <span
                  style={{
                    fontSize: '0.8rem',
                    fontWeight: 700,
                    padding: '4px 10px',
                    background: 'var(--color-bg-base)',
                    borderRadius: 'var(--radius-full)',
                    color: 'var(--color-text-muted)',
                  }}
                >
                  {data.bestSellingProducts?.length || 0} صنف
                </span>
              </div>

              {/* Products List */}
              <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                {!data.bestSellingProducts?.length ? (
                  <p style={{ textAlign: 'center', color: 'var(--color-text-muted)', padding: '20px 0' }}>
                    لا توجد بيانات للأصناف في هذه الفترة
                  </p>
                ) : (
                  data.bestSellingProducts.map((prod, idx) => {
                    const pct = Math.min(100, Math.round((prod.quantitySold / maxSold) * 100));
                    return (
                      <div
                        key={prod.productId || idx}
                        style={{
                          display: 'flex',
                          flexDirection: 'column',
                          gap: '6px',
                          padding: '10px 12px',
                          background: 'var(--color-bg-base)',
                          borderRadius: 'var(--radius-md)',
                          border: '1px solid var(--color-border)',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <span
                              style={{
                                width: '22px',
                                height: '22px',
                                borderRadius: '50%',
                                background:
                                  idx === 0
                                    ? '#f59e0b'
                                    : idx === 1
                                    ? '#94a3b8'
                                    : idx === 2
                                    ? '#b45309'
                                    : 'var(--color-border)',
                                color: idx < 3 ? '#ffffff' : 'var(--color-text-muted)',
                                fontSize: '0.75rem',
                                fontWeight: 800,
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                              }}
                            >
                              {idx + 1}
                            </span>
                            <span style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--color-text-main)' }}>
                              {prod.name}
                            </span>
                            {prod.barCode && (
                              <span
                                style={{
                                  fontSize: '0.7rem',
                                  color: 'var(--color-text-muted)',
                                  background: 'var(--color-surface)',
                                  padding: '2px 6px',
                                  borderRadius: '4px',
                                  fontFamily: 'monospace',
                                }}
                              >
                                {prod.barCode}
                              </span>
                            )}
                          </div>
                          <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px' }}>
                            <span style={{ fontWeight: 800, fontSize: '0.95rem', color: 'var(--color-primary)' }}>
                              {prod.quantitySold}{' '}
                              <span style={{ fontSize: '0.75rem', fontWeight: 500, color: 'var(--color-text-muted)' }}>
                                قطعة
                              </span>
                            </span>
                            <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                              ({(prod.totalRevenue || 0).toFixed(2)} د.ل)
                            </span>
                          </div>
                        </div>

                        {/* Visual Progress Bar */}
                        <div
                          style={{
                            width: '100%',
                            height: '6px',
                            background: 'var(--color-surface)',
                            borderRadius: '3px',
                            overflow: 'hidden',
                          }}
                        >
                          <div
                            style={{
                              width: `${pct}%`,
                              height: '100%',
                              background:
                                idx === 0
                                  ? 'linear-gradient(90deg, #f59e0b, #fbbf24)'
                                  : 'linear-gradient(90deg, var(--color-primary), #34d399)',
                              borderRadius: '3px',
                              transition: 'width 0.4s ease',
                            }}
                          />
                        </div>

                        {/* Secondary stats: profit */}
                        <div
                          style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            fontSize: '0.75rem',
                            color: 'var(--color-text-muted)',
                          }}
                        >
                          <span>التكلفة: {(prod.totalCost || 0).toFixed(2)} د.ل</span>
                          <span style={{ color: '#0ea5e9', fontWeight: 600 }}>
                            الربح: {(prod.totalProfit || 0).toFixed(2)} د.ل
                          </span>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>

            {/* Right Card: Breakdown by Payment Method & Categories */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              {/* Payment Methods Breakdown */}
              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  padding: '24px',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '16px',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div
                    style={{
                      width: '36px',
                      height: '36px',
                      borderRadius: 'var(--radius-md)',
                      background: 'rgba(14, 165, 233, 0.15)',
                      color: '#0ea5e9',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <CreditCard size={20} />
                  </div>
                  <div>
                    <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700 }}>المبيعات حسب وسائل الدفع</h3>
                    <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                      توزيع المدفوعات والسيولة
                    </span>
                  </div>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {!data.salesByPaymentMethod?.length ? (
                    <p style={{ textAlign: 'center', color: 'var(--color-text-muted)', padding: '16px 0' }}>
                      لا توجد مدفوعات مسجلة في هذه الفترة
                    </p>
                  ) : (
                    data.salesByPaymentMethod.map((pm, i) => {
                      const share =
                        data.totalSales > 0 ? Math.round((pm.totalSales / data.totalSales) * 100) : 0;
                      return (
                        <div
                          key={pm.paymentMethodId || i}
                          style={{
                            padding: '12px 14px',
                            background: 'var(--color-bg-base)',
                            borderRadius: 'var(--radius-md)',
                            border: '1px solid var(--color-border)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                          }}
                        >
                          <div>
                            <div style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--color-text-main)' }}>
                              {pm.paymentMethodName || pm.name || 'طريقة دفع'}
                            </div>
                            <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '2px' }}>
                              {pm.totalOrders || pm.ordersCount || 0} طلب ({share}%)
                            </div>
                          </div>
                          <div style={{ textAlign: 'left' }}>
                            <div
                              style={{
                                fontWeight: 800,
                                fontSize: '1rem',
                                color: 'var(--color-text-main)',
                                fontFamily: 'monospace',
                              }}
                            >
                              {(pm.totalSales || 0).toFixed(2)} د.ل
                            </div>
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>

              {/* Best Selling Categories Breakdown */}
              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  padding: '24px',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '16px',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div
                    style={{
                      width: '36px',
                      height: '36px',
                      borderRadius: 'var(--radius-md)',
                      background: 'rgba(245, 158, 11, 0.15)',
                      color: '#f59e0b',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <UtensilsCrossed size={20} />
                  </div>
                  <div>
                    <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700 }}>أداء تصنيفات المنيو</h3>
                    <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                      الأعلى إيراداً بين التصنيفات
                    </span>
                  </div>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {!data.bestSellingCategories?.length ? (
                    <p style={{ textAlign: 'center', color: 'var(--color-text-muted)', padding: '16px 0' }}>
                      لا توجد بيانات للتصنيفات في هذه الفترة
                    </p>
                  ) : (
                    data.bestSellingCategories.slice(0, 5).map((cat, i) => (
                      <div
                        key={cat.categoryId || i}
                        style={{
                          padding: '12px 14px',
                          background: 'var(--color-bg-base)',
                          borderRadius: 'var(--radius-md)',
                          border: '1px solid var(--color-border)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                        }}
                      >
                        <div>
                          <div style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--color-text-main)' }}>
                            {cat.name}
                          </div>
                          <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '2px' }}>
                            {cat.quantitySold} قطعة مباعة
                          </div>
                        </div>
                        <div style={{ textAlign: 'left' }}>
                          <div
                            style={{
                              fontWeight: 800,
                              fontSize: '1rem',
                              color: 'var(--color-text-main)',
                              fontFamily: 'monospace',
                            }}
                          >
                            {(cat.totalRevenue || 0).toFixed(2)} د.ل
                          </div>
                          <div style={{ fontSize: '0.75rem', color: '#0ea5e9' }}>
                            ربح: {(cat.totalProfit || 0).toFixed(2)} د.ل
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default DashboardPage;
