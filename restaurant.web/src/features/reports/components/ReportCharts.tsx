import React, { useState } from 'react';
import type {
  DailySalesReportDto,
  ProductReportDto,
  CategoryReportDto,
  PaymentMethodSalesReportDto,
} from '../../../types/api';

// ==========================================
// 1. Daily Sales Trend Chart (Area / Line Chart)
// ==========================================
interface DailySalesChartProps {
  data: DailySalesReportDto[];
}

export const DailySalesChart: React.FC<DailySalesChartProps> = ({ data }) => {
  const [metric, setMetric] = useState<'sales' | 'profit' | 'orders'>('sales');
  const [hoveredIdx, setHoveredIdx] = useState<number | null>(null);

  if (!data || data.length === 0) return null;

  // Sorted chronologically
  const sorted = [...data].sort((a, b) => {
    const da = a.date ? new Date(a.date).getTime() : 0;
    const db = b.date ? new Date(b.date).getTime() : 0;
    return da - db;
  });

  const values = sorted.map((d) => {
    if (metric === 'sales') return d.totalSales || 0;
    if (metric === 'profit') return d.totalProfit || 0;
    return d.totalOrders || d.ordersCount || 0;
  });

  const maxVal = Math.max(...values, 10);
  const minVal = 0;
  const range = maxVal - minVal;

  const width = 800;
  const height = 260;
  const paddingX = 40;
  const paddingY = 30;

  const usableWidth = width - paddingX * 2;
  const usableHeight = height - paddingY * 2;

  const points = sorted.map((_, idx) => {
    const x = paddingX + (sorted.length > 1 ? (idx / (sorted.length - 1)) * usableWidth : usableWidth / 2);
    const y = height - paddingY - ((values[idx] - minVal) / range) * usableHeight;
    return { x, y, val: values[idx], d: sorted[idx] };
  });

  const pathD = points.reduce((acc, pt, i) => {
    if (i === 0) return `M ${pt.x} ${pt.y}`;
    const prev = points[i - 1];
    const cx = (prev.x + pt.x) / 2;
    return `${acc} C ${cx} ${prev.y}, ${cx} ${pt.y}, ${pt.x} ${pt.y}`;
  }, '');

  const areaD = `${pathD} L ${points[points.length - 1]?.x || 0} ${height - paddingY} L ${points[0]?.x || 0} ${height - paddingY} Z`;

  const getMetricLabel = () => {
    if (metric === 'sales') return 'إجمالي المبيعات (د.ل)';
    if (metric === 'profit') return 'صافي الربح (د.ل)';
    return 'عدد الطلبات';
  };

  return (
    <div
      style={{
        backgroundColor: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-lg)',
        padding: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: '16px',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '10px' }}>
        <div>
          <h3 style={{ fontSize: '1.05rem', fontWeight: 800, margin: 0 }}>
            الرسم البياني لحركة المؤشرات اليومية
          </h3>
          <p style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', margin: '2px 0 0 0' }}>
            تتبع مسار {getMetricLabel()} عبر الأيام المحددة
          </p>
        </div>

        {/* Metric toggles */}
        <div
          style={{
            display: 'flex',
            backgroundColor: 'var(--color-bg-base)',
            padding: '3px',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--color-border)',
            gap: '4px',
          }}
        >
          <button
            type="button"
            onClick={() => setMetric('sales')}
            style={{
              padding: '6px 14px',
              borderRadius: 'var(--radius-sm)',
              border: 'none',
              cursor: 'pointer',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: metric === 'sales' ? 'var(--color-primary)' : 'transparent',
              color: metric === 'sales' ? '#000000' : 'var(--color-text-muted)',
              transition: 'all 0.15s ease',
            }}
          >
            المبيعات
          </button>
          <button
            type="button"
            onClick={() => setMetric('profit')}
            style={{
              padding: '6px 14px',
              borderRadius: 'var(--radius-sm)',
              border: 'none',
              cursor: 'pointer',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: metric === 'profit' ? '#10b981' : 'transparent',
              color: metric === 'profit' ? '#ffffff' : 'var(--color-text-muted)',
              transition: 'all 0.15s ease',
            }}
          >
            الأرباح
          </button>
          <button
            type="button"
            onClick={() => setMetric('orders')}
            style={{
              padding: '6px 14px',
              borderRadius: 'var(--radius-sm)',
              border: 'none',
              cursor: 'pointer',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: metric === 'orders' ? '#3b82f6' : 'transparent',
              color: metric === 'orders' ? '#ffffff' : 'var(--color-text-muted)',
              transition: 'all 0.15s ease',
            }}
          >
            الطلبات
          </button>
        </div>
      </div>

      {/* SVG Canvas */}
      <div style={{ width: '100%', overflowX: 'auto', position: 'relative' }}>
        <svg
          viewBox={`0 0 ${width} ${height}`}
          style={{ width: '100%', height: 'auto', minWidth: '550px', display: 'block' }}
        >
          <defs>
            <linearGradient id="salesGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#38e07b" stopOpacity="0.4" />
              <stop offset="100%" stopColor="#38e07b" stopOpacity="0.0" />
            </linearGradient>
            <linearGradient id="profitGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#10b981" stopOpacity="0.4" />
              <stop offset="100%" stopColor="#10b981" stopOpacity="0.0" />
            </linearGradient>
            <linearGradient id="ordersGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.4" />
              <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.0" />
            </linearGradient>
          </defs>

          {/* Grid lines */}
          {[0, 0.25, 0.5, 0.75, 1].map((ratio, i) => {
            const y = height - paddingY - ratio * usableHeight;
            const val = minVal + ratio * range;
            return (
              <g key={i}>
                <line
                  x1={paddingX}
                  y1={y}
                  x2={width - paddingX}
                  y2={y}
                  stroke="var(--color-border)"
                  strokeDasharray="4 4"
                  strokeWidth="1"
                />
                <text
                  x={paddingX - 8}
                  y={y + 4}
                  fill="var(--color-text-muted)"
                  fontSize="10"
                  textAnchor="end"
                  fontFamily="inherit"
                >
                  {val >= 1000 ? `${(val / 1000).toFixed(1)}k` : val.toFixed(0)}
                </text>
              </g>
            );
          })}

          {/* Fill Area */}
          <path
            d={areaD}
            fill={metric === 'sales' ? 'url(#salesGrad)' : metric === 'profit' ? 'url(#profitGrad)' : 'url(#ordersGrad)'}
          />

          {/* Main Stroke Path */}
          <path
            d={pathD}
            fill="none"
            stroke={metric === 'sales' ? '#38e07b' : metric === 'profit' ? '#10b981' : '#3b82f6'}
            strokeWidth="3"
            strokeLinecap="round"
          />

          {/* Points & Interactive Nodes */}
          {points.map((pt, idx) => {
            const isHovered = hoveredIdx === idx;
            return (
              <g
                key={idx}
                onMouseEnter={() => setHoveredIdx(idx)}
                onMouseLeave={() => setHoveredIdx(null)}
                style={{ cursor: 'pointer' }}
              >
                <circle
                  cx={pt.x}
                  cy={pt.y}
                  r={isHovered ? 7 : 4}
                  fill={metric === 'sales' ? '#38e07b' : metric === 'profit' ? '#10b981' : '#3b82f6'}
                  stroke="#ffffff"
                  strokeWidth={isHovered ? 3 : 2}
                  style={{ transition: 'all 0.15s ease' }}
                />

                {/* X-axis date labels */}
                {idx % Math.ceil(points.length / 8) === 0 && (
                  <text
                    x={pt.x}
                    y={height - 8}
                    fill="var(--color-text-muted)"
                    fontSize="10"
                    textAnchor="middle"
                    fontFamily="inherit"
                  >
                    {pt.d.date ? new Date(pt.d.date).toLocaleDateString('ar-LY', { month: 'numeric', day: 'numeric' }) : ''}
                  </text>
                )}
              </g>
            );
          })}
        </svg>

        {/* Live Hover Tooltip */}
        {hoveredIdx !== null && points[hoveredIdx] && (
          <div
            style={{
              position: 'absolute',
              top: '12px',
              left: '50%',
              transform: 'translateX(-50%)',
              backgroundColor: 'rgba(15, 23, 42, 0.95)',
              color: '#ffffff',
              padding: '8px 16px',
              borderRadius: '8px',
              fontSize: '0.85rem',
              boxShadow: '0 4px 14px rgba(0,0,0,0.3)',
              display: 'flex',
              alignItems: 'center',
              gap: '14px',
              pointerEvents: 'none',
              zIndex: 10,
              border: '1px solid rgba(255,255,255,0.1)',
            }}
          >
            <div>
              <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>التاريخ:</span>{' '}
              <strong>{points[hoveredIdx].d.date ? new Date(points[hoveredIdx].d.date).toLocaleDateString('ar-LY') : ''}</strong>
            </div>
            <div>
              <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>المبيعات:</span>{' '}
              <strong style={{ color: '#38e07b' }}>{(points[hoveredIdx].d.totalSales || 0).toFixed(2)} د.ل</strong>
            </div>
            <div>
              <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>الربح:</span>{' '}
              <strong style={{ color: '#10b981' }}>{(points[hoveredIdx].d.totalProfit || 0).toFixed(2)} د.ل</strong>
            </div>
            <div>
              <span style={{ color: '#94a3b8', fontSize: '0.75rem' }}>الطلبات:</span>{' '}
              <strong>{points[hoveredIdx].d.totalOrders || points[hoveredIdx].d.ordersCount || 0}</strong>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

// ==========================================
// 2. Top Products Ranked Horizontal Bar Chart
// ==========================================
interface TopProductsChartProps {
  products: ProductReportDto[];
}

export const TopProductsChart: React.FC<TopProductsChartProps> = ({ products }) => {
  if (!products || products.length === 0) return null;

  const top10 = products.slice(0, 10);
  const maxRevenue = Math.max(...top10.map((p) => p.totalRevenue || 0), 1);

  return (
    <div
      style={{
        backgroundColor: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-lg)',
        padding: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: '16px',
      }}
    >
      <div>
        <h3 style={{ fontSize: '1.05rem', fontWeight: 800, margin: 0 }}>
          أفضل 10 أصناف تحقيقاً للإيرادات والمبيعات
        </h3>
        <p style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', margin: '2px 0 0 0' }}>
          ترتيب تنازلي حسب إجمالي الإيراد والكمية المباعة
        </p>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
        {top10.map((prod, idx) => {
          const rev = prod.totalRevenue || 0;
          const pct = ((rev / maxRevenue) * 100).toFixed(1);

          return (
            <div key={idx} style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span
                    style={{
                      width: '22px',
                      height: '22px',
                      borderRadius: '50%',
                      backgroundColor: idx < 3 ? 'var(--color-primary-bg)' : 'var(--color-bg-base)',
                      color: idx < 3 ? 'var(--color-primary)' : 'var(--color-text-muted)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '0.75rem',
                      fontWeight: 800,
                    }}
                  >
                    {idx + 1}
                  </span>
                  <span style={{ fontWeight: 700 }}>{prod.name}</span>
                  <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                    ({prod.quantitySold || 0} قطعة مباعة)
                  </span>
                </div>
                <div style={{ fontWeight: 800, color: 'var(--color-primary)' }}>
                  {rev.toFixed(2)} د.ل
                </div>
              </div>

              {/* Bar track */}
              <div
                style={{
                  height: '10px',
                  backgroundColor: 'var(--color-bg-base)',
                  borderRadius: '6px',
                  overflow: 'hidden',
                  border: '1px solid var(--color-border)',
                }}
              >
                <div
                  style={{
                    height: '100%',
                    width: `${pct}%`,
                    backgroundColor: idx === 0 ? 'var(--color-primary)' : idx === 1 ? '#10b981' : idx === 2 ? '#3b82f6' : '#64748b',
                    borderRadius: '6px',
                    transition: 'width 0.4s ease-out',
                  }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ==========================================
// 3. Payment Methods Donut / Distribution Chart
// ==========================================
interface PaymentMethodsChartProps {
  data: PaymentMethodSalesReportDto[];
}

export const PaymentMethodsChart: React.FC<PaymentMethodsChartProps> = ({ data }) => {
  if (!data || data.length === 0) return null;

  const totalSales = data.reduce((sum, d) => sum + (d.totalSales || 0), 0);
  const colors = ['#38e07b', '#3b82f6', '#f59e0b', '#ec4899', '#8b5cf6'];

  return (
    <div
      style={{
        backgroundColor: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-lg)',
        padding: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: '16px',
      }}
    >
      <div>
        <h3 style={{ fontSize: '1.05rem', fontWeight: 800, margin: 0 }}>
          توزيع المبيعات بحسب وسائل التحصيل
        </h3>
        <p style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', margin: '2px 0 0 0' }}>
          نسبة مساهمة كل وسيلة دفع في إجمالي التدفقات النقدية
        </p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '14px' }}>
        {data.map((item, idx) => {
          const sales = item.totalSales || 0;
          const pct = totalSales > 0 ? ((sales / totalSales) * 100).toFixed(1) : '0';
          const color = colors[idx % colors.length];

          return (
            <div
              key={idx}
              style={{
                backgroundColor: 'var(--color-bg-base)',
                border: '1px solid var(--color-border)',
                borderRadius: 'var(--radius-md)',
                padding: '14px',
                display: 'flex',
                flexDirection: 'column',
                gap: '8px',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <div style={{ width: '12px', height: '12px', borderRadius: '3px', backgroundColor: color }} />
                  <span style={{ fontWeight: 700, fontSize: '0.9rem' }}>
                    {item.paymentMethodName || item.name || 'طريقة دفع'}
                  </span>
                </div>
                <span style={{ fontSize: '0.85rem', fontWeight: 800, color }}>
                  {pct}%
                </span>
              </div>

              <div style={{ fontSize: '1.25rem', fontWeight: 800 }}>
                {sales.toFixed(2)} <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>د.ل</span>
              </div>

              <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                {item.totalOrders || item.ordersCount || 0} عملية مكتملة
              </div>

              {/* Mini progress bar */}
              <div
                style={{
                  height: '6px',
                  backgroundColor: 'var(--color-surface)',
                  borderRadius: '4px',
                  overflow: 'hidden',
                }}
              >
                <div style={{ height: '100%', width: `${pct}%`, backgroundColor: color, borderRadius: '4px' }} />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ==========================================
// 4. Category Performance Breakdown Chart
// ==========================================
interface CategoryPerformanceChartProps {
  categories: CategoryReportDto[];
}

export const CategoryPerformanceChart: React.FC<CategoryPerformanceChartProps> = ({ categories }) => {
  if (!categories || categories.length === 0) return null;

  const totalRev = categories.reduce((sum, c) => sum + (c.totalRevenue || 0), 0);

  return (
    <div
      style={{
        backgroundColor: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 'var(--radius-lg)',
        padding: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: '16px',
      }}
    >
      <div>
        <h3 style={{ fontSize: '1.05rem', fontWeight: 800, margin: 0 }}>
          مؤشرات أداء وحصص التصنيفات الغذائية
        </h3>
        <p style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', margin: '2px 0 0 0' }}>
          حجم المبيعات ونسبة كل تصنيف من الإجمالي الكلي
        </p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '14px' }}>
        {categories.map((cat, idx) => {
          const rev = cat.totalRevenue || 0;
          const profit = cat.totalProfit || 0;
          const pct = totalRev > 0 ? ((rev / totalRev) * 100).toFixed(1) : '0';

          return (
            <div
              key={idx}
              style={{
                backgroundColor: 'var(--color-bg-base)',
                border: '1px solid var(--color-border)',
                borderRadius: 'var(--radius-md)',
                padding: '16px',
                display: 'flex',
                flexDirection: 'column',
                gap: '10px',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontWeight: 800, fontSize: '0.95rem' }}>{cat.name}</span>
                <span
                  style={{
                    backgroundColor: 'var(--color-primary-bg)',
                    color: 'var(--color-primary)',
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '0.8rem',
                    fontWeight: 800,
                  }}
                >
                  {pct}%
                </span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>المبيعات:</span>
                <strong>{rev.toFixed(2)} د.ل</strong>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>صافي الربح:</span>
                <strong style={{ color: '#10b981' }}>{profit.toFixed(2)} د.ل</strong>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>الكميات المباعة:</span>
                <span>{cat.quantitySold || 0} عنصر</span>
              </div>

              {/* Progress bar */}
              <div
                style={{
                  height: '6px',
                  backgroundColor: 'var(--color-surface)',
                  borderRadius: '4px',
                  overflow: 'hidden',
                  marginTop: '4px',
                }}
              >
                <div
                  style={{
                    height: '100%',
                    width: `${pct}%`,
                    backgroundColor: 'var(--color-primary)',
                    borderRadius: '4px',
                  }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
