import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  Wallet,
  ArrowDownLeft,
  ArrowUpRight,
  Plus,
  Minus,
  Calendar,
  DollarSign,
  Clock,
  RotateCcw,
  CheckCircle2,
  AlertCircle,
  FileText,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import Modal from '../../../components/ui/Modal';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import ShiftCloseModal from '../components/ShiftCloseModal';
import apiClient, { ApiError } from '../../../services/apiClient';
import { useAuth } from '../../../hooks/useAuth';
import { localDate } from '../../../utils/localDate';
import type { CashboxDto, CashDrawerEntryDto, CashboxBalanceDto, CashShiftDto } from '../../../types/api';
import { CashDrawerEntryType } from '../../../types/api';
import toast from 'react-hot-toast';

export const TreasuryPage: React.FC = () => {
  const [cashboxes, setCashboxes] = useState<CashboxDto[]>([]);
  const [entries, setEntries] = useState<CashDrawerEntryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [shifts, setShifts] = useState<CashShiftDto[]>([]);
  const { user } = useAuth();
  type ManualPayload = { amount: number; entryType: CashDrawerEntryType; notes: string; cashboxId: number; clientRequestId: string };
  const pendingKey = `pending-manual:${user?.id}`;
  const manualRequest = useRef<ManualPayload | null>(null);
  const [page, setPage] = useState(1);
  const [periodSummary, setPeriodSummary] = useState({ totalCount: 0, totalInflow: 0, totalOutflow: 0, totalSales: 0 });
  const requestSequence = useRef(0);
  const pageSize = 100;

  // Filters
  const [selectedCashboxId, setSelectedCashboxId] = useState<number | 'all'>('all');
  const [selectedTypeFilter, setSelectedTypeFilter] = useState<string>('all');
  const [dateFrom, setDateFrom] = useState<string>(() => localDate());
  const [dateTo, setDateTo] = useState<string>(() => localDate());

  // Manual Entry Modal State
  const [isManualModalOpen, setIsManualModalOpen] = useState<boolean>(false);
  const [manualEntryType, setManualEntryType] = useState<CashDrawerEntryType>(CashDrawerEntryType.Inflow);
  const [manualAmount, setManualAmount] = useState<string>('');
  const [manualNotes, setManualNotes] = useState<string>('');
  const [manualCashboxId, setManualCashboxId] = useState<number | ''>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [hasPendingManual, setHasPendingManual] = useState(false);

  // Shift Close Modal State
  const [isShiftCloseOpen, setIsShiftCloseOpen] = useState<boolean>(false);

  // Load Data
  const loadData = useCallback(async () => {
    const requestId = ++requestSequence.current;
    setIsLoading(true);
    try {
      const [cbRes, entriesRes, shiftRes] = await Promise.all([
        apiClient.get<CashboxDto[]>('/api/cashboxes'),
        apiClient.get<{ items: CashDrawerEntryDto[]; totalCount: number; totalInflow: number; totalOutflow: number; totalSales: number }>('/api/cashdrawerentries/paged', {
          from: dateFrom || undefined, to: dateTo || undefined, page, pageSize,
          cashboxId: selectedCashboxId === 'all' ? undefined : selectedCashboxId,
          entryType: selectedTypeFilter === 'all' ? undefined : Number(selectedTypeFilter),
        }),
        apiClient.get<CashShiftDto[]>('/api/cashshifts'),
      ]);

      const enrichedCashboxes = await Promise.all(
        (cbRes || []).map(async (cb) => {
          const balRes = await apiClient.get<CashboxBalanceDto>(`/api/cashboxes/${cb.id}/balance`);
          return { ...cb, currentBalance: balRes.currentBalance };
        })
      );

      if (requestId !== requestSequence.current) return;
      setCashboxes(enrichedCashboxes);
      setEntries(entriesRes.items);
      setPeriodSummary(entriesRes);
      setShifts(shiftRes);
      setLoadError(null);
      setManualCashboxId(previous => previous || enrichedCashboxes.find(c => c.isActive)?.id || '');
    } catch (error) {
      if (requestId !== requestSequence.current) return;
      const message = error instanceof Error ? error.message : 'فشل تحميل بيانات الخزينة وحركات الصندوق.';
      setLoadError(message);
      toast.error(message);
    } finally {
      if (requestId === requestSequence.current) setIsLoading(false);
    }
  }, [dateFrom, dateTo, page, selectedCashboxId, selectedTypeFilter]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void loadData(); }, 0);
    return () => window.clearTimeout(timer);
  }, [loadData]);

  // Open Manual Entry
  const handleOpenManual = (type: CashDrawerEntryType) => {
    setManualEntryType(type);
    setManualAmount('');
    setManualNotes('');
    setFormError(null);
    const saved = localStorage.getItem(pendingKey);
    try { manualRequest.current = saved ? JSON.parse(saved) as ManualPayload : null; }
    catch { setFormError('تعذر قراءة بيانات الحركة المعلقة. راجع سجل الخزينة قبل إعادة إدخالها.'); setIsManualModalOpen(true); return; }
    setHasPendingManual(Boolean(manualRequest.current));
    if (manualRequest.current) {
      setManualAmount(String(manualRequest.current.amount)); setManualNotes(manualRequest.current.notes);
      setManualEntryType(manualRequest.current.entryType); setManualCashboxId(manualRequest.current.cashboxId);
      setFormError('توجد حركة سابقة تحتاج تأكيداً. الحفظ سيستعيد نتيجتها دون إنشاء حركة جديدة.');
      setIsManualModalOpen(true); return;
    }
    const box = cashboxes.find(c => c.isActive && c.id === selectedCashboxId) || cashboxes.find(c => c.isActive);
    setManualCashboxId(box?.id || '');
    setIsManualModalOpen(true);
  };

  const completeManual = async (saved: CashDrawerEntryDto) => {
      localStorage.removeItem(pendingKey); manualRequest.current = null; setHasPendingManual(false);

      toast.success(
        saved.entryType === CashDrawerEntryType.Inflow
          ? `تم إيداع مبلغ ${saved.amount.toFixed(2)} د.ل بنجاح!`
          : `تم تسجيل سحب مبلغ ${saved.amount.toFixed(2)} د.ل بنجاح!`
      );
      setIsManualModalOpen(false);
      await loadData();
  };

  // Submit Manual Entry
  const handleManualSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isSubmitting || loadError || isLoading) return;
    const amountNum = parseFloat(manualAmount);
    if (!amountNum || amountNum <= 0) {
      setFormError('يرجى إدخال مبلغ صحيح أكبر من صفر.');
      return;
    }

    if (!manualCashboxId) {
      setFormError('يرجى تحديد الخزينة.');
      return;
    }

    setFormError(null);
    setIsSubmitting(true);
    try {
      const payload = {
        amount: amountNum,
        entryType: manualEntryType,
        notes: manualNotes.trim() || (manualEntryType === CashDrawerEntryType.Inflow ? 'إيداع نقدي يدوي' : 'سحب نقدي يدوي'),
        cashboxId: Number(manualCashboxId),
      };
      let recovered: CashDrawerEntryDto | null = null;
      if (manualRequest.current) {
        recovered = await apiClient.get<CashDrawerEntryDto>(`/api/cashdrawerentries/byrequest/${encodeURIComponent(manualRequest.current.clientRequestId)}`)
          .catch((error: unknown) => { if (error instanceof ApiError && error.status === 404) return null; throw error; });
      } else {
        const request = { ...payload, clientRequestId: crypto.randomUUID() };
        localStorage.setItem(pendingKey, JSON.stringify(request));
        manualRequest.current = request;
        setHasPendingManual(true);
      }
      const saved = recovered ?? await apiClient.post<CashDrawerEntryDto>('/api/cashdrawerentries', manualRequest.current);
      await completeManual(saved);
    } catch (err: unknown) {
      if (err instanceof ApiError && [400, 403, 409].includes(err.status ?? 0)) {
        if (manualRequest.current) {
          try {
            const saved = await apiClient.get<CashDrawerEntryDto>(`/api/cashdrawerentries/byrequest/${encodeURIComponent(manualRequest.current.clientRequestId)}`);
            await completeManual(saved); return;
          } catch (recoveryError) {
            if (!(recoveryError instanceof ApiError && recoveryError.status === 404)) {
              setFormError('تعذر تأكيد نتيجة الحركة. احتُفظ بها للاستعادة دون تكرار.'); return;
            }
          }
        }
        localStorage.removeItem(pendingKey); manualRequest.current = null; setHasPendingManual(false);
      }
      const msg = err instanceof Error ? err.message : 'فشل تسجيل حركة الخزينة.';
      setFormError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Filtered Entries
  const filteredEntries = useMemo(() => {
    return entries.filter((entry) => {
      // 1. Cashbox Filter
      if (selectedCashboxId !== 'all' && entry.cashboxId !== selectedCashboxId) {
        return false;
      }
      // 2. Type Filter
      if (selectedTypeFilter !== 'all') {
        if (entry.entryType !== Number(selectedTypeFilter)) return false;
      }
      // 3. Date Filter
      if (entry.date) {
        const entryDateStr = entry.date.split('T')[0];
        if (dateFrom && entryDateStr < dateFrom) return false;
        if (dateTo && entryDateStr > dateTo) return false;
      }
      return true;
    });
  }, [entries, selectedCashboxId, selectedTypeFilter, dateFrom, dateTo]);

  // Aggregate Stats
  const stats = useMemo(() => {
    const inflows = periodSummary.totalInflow;
    const outflows = periodSummary.totalOutflow;
    const sales = periodSummary.totalSales;

    const activeBalance = cashboxes.filter(c => c.isActive && (selectedCashboxId === 'all' || c.id === selectedCashboxId)).reduce(
      (sum, cb) => sum + (cb.currentBalance ?? cb.initialBalance ?? 0),
      0
    );

    return { inflows, outflows, sales, activeBalance };
  }, [periodSummary, cashboxes, selectedCashboxId]);

  const getEntryBadge = (type: CashDrawerEntryType) => {
    switch (type) {
      case CashDrawerEntryType.SalePayment:
        return { text: 'مبيعات طلب', bg: 'var(--color-success-bg)', color: 'var(--color-success-text)', isPositive: true };
      case CashDrawerEntryType.Inflow:
        return { text: 'إيداع يدوي', bg: 'var(--color-info-bg)', color: 'var(--color-info-text)', isPositive: true };
      case CashDrawerEntryType.Outflow:
        return { text: 'سحب يدوي', bg: 'var(--color-warning-bg)', color: 'var(--color-warning-text)', isPositive: false };
      case CashDrawerEntryType.Refund:
        return { text: 'رد مبيعات', bg: 'var(--color-danger-bg)', color: 'var(--color-danger-text)', isPositive: false };
      case CashDrawerEntryType.TransferIn:
        return { text: 'استلام توريد وردية', bg: 'var(--color-info-bg)', color: 'var(--color-info-text)', isPositive: true };
      case CashDrawerEntryType.TransferOut:
        return { text: 'توريد وردية', bg: 'var(--color-warning-bg)', color: 'var(--color-warning-text)', isPositive: false };
      case CashDrawerEntryType.CashShortage:
        return { text: 'عجز وردية', bg: 'var(--color-danger-bg)', color: 'var(--color-danger-text)', isPositive: false };
      case CashDrawerEntryType.CashSurplus:
        return { text: 'زيادة وردية', bg: 'var(--color-success-bg)', color: 'var(--color-success-text)', isPositive: true };
      default:
        return { text: 'حركة عامة', bg: 'var(--color-surface-active)', color: 'var(--color-text-muted)', isPositive: true };
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {loadError && <p role="alert" style={{ color: 'var(--color-danger-text)' }}>تعذر تحديث البيانات؛ الأرصدة والحركات المعروضة قديمة. {loadError}</p>}
      {/* Top Header & Actions */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '16px',
        }}
      >
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>
            حركات الخزينة والصندوق
          </h2>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem', marginTop: '4px' }}>
            متابعة السيولة النقدية، الإيداعات والسحوبات اليدوية، وتقفيل الورديات
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <Button
            variant="success"
            size="md"
            onClick={() => handleOpenManual(CashDrawerEntryType.Inflow)}
            icon={<Plus size={16} />}
          >
            إيداع نقدي (+)
          </Button>

          <Button
            variant="danger"
            size="md"
            onClick={() => handleOpenManual(CashDrawerEntryType.Outflow)}
            icon={<Minus size={16} />}
          >
            سحب نقدي (-)
          </Button>

          <Button
            variant="warning"
            size="md"
            onClick={() => setIsShiftCloseOpen(true)}
            icon={<Clock size={16} />}
          >
            تقفيل الوردية (F12)
          </Button>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <Button variant="secondary" disabled={page <= 1 || isLoading} onClick={() => setPage(value => value - 1)}>السابق</Button>
        <span>صفحة {page} — {periodSummary.totalCount} حركة</span>
        <Button variant="secondary" disabled={page * pageSize >= periodSummary.totalCount || isLoading} onClick={() => setPage(value => value + 1)}>التالي</Button>
      </div>
      {/* KPI Cards: Current Balance, Inflows, Outflows, Sales */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '16px',
        }}
      >
        {/* Total Cashbox Balance */}
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            padding: '20px',
            boxShadow: 'var(--shadow-card)',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>
              الرصيد الإجمالي الحالي
            </span>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                backgroundColor: 'var(--color-primary-bg)',
                color: 'var(--color-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Wallet size={20} />
            </div>
          </div>
          <div style={{ fontSize: '1.625rem', fontWeight: 800, color: 'var(--color-primary)' }}>
            {stats.activeBalance.toFixed(2)} <span style={{ fontSize: '1rem' }}>د.ل</span>
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
            إجمالي السيولة في خزائن النظام
          </span>
        </div>

        {/* Total Sales in Filter Period */}
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            padding: '20px',
            boxShadow: 'var(--shadow-card)',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>
              مبيعات الفترة المحددة
            </span>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                backgroundColor: 'var(--color-success-bg)',
                color: 'var(--color-success-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <CheckCircle2 size={20} />
            </div>
          </div>
          <div style={{ fontSize: '1.625rem', fontWeight: 800, color: 'var(--color-success-text)' }}>
            +{stats.sales.toFixed(2)} <span style={{ fontSize: '1rem' }}>د.ل</span>
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
            إجمالي المقبوضات من فواتير الطلبات
          </span>
        </div>

        {/* Manual Inflows */}
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            padding: '20px',
            boxShadow: 'var(--shadow-card)',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>
              إيداعات يدوية
            </span>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                backgroundColor: 'var(--color-info-bg)',
                color: 'var(--color-info-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ArrowDownLeft size={20} />
            </div>
          </div>
          <div style={{ fontSize: '1.625rem', fontWeight: 800, color: 'var(--color-info-text)' }}>
            +{stats.inflows.toFixed(2)} <span style={{ fontSize: '1rem' }}>د.ل</span>
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
            تغذية نقدية وسندات قبض
          </span>
        </div>

        {/* Outflows */}
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            padding: '20px',
            boxShadow: 'var(--shadow-card)',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '0.875rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>
              إجمالي السحوبات
            </span>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                backgroundColor: 'var(--color-danger-bg)',
                color: 'var(--color-danger-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ArrowUpRight size={20} />
            </div>
          </div>
          <div style={{ fontSize: '1.625rem', fontWeight: 800, color: 'var(--color-danger-text)' }}>
            -{stats.outflows.toFixed(2)} <span style={{ fontSize: '1rem' }}>د.ل</span>
          </div>
          <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
            مصروفات ونثريات وردود مبيعات
          </span>
        </div>
      </div>

      {/* Filter Controls Toolbar */}
      <div
        style={{
          backgroundColor: 'var(--color-surface)',
          padding: '16px 20px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '14px',
        }}
      >
        {/* Cashbox Select */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)', whiteSpace: 'nowrap' }}>
            الخزينة:
          </span>
          <select
            value={selectedCashboxId}
            onChange={(e) => { setPage(1); setSelectedCashboxId(e.target.value === 'all' ? 'all' : Number(e.target.value)); }}
            style={{
              padding: '8px 14px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
              backgroundColor: 'var(--color-surface)',
              color: 'var(--color-text-primary)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          >
            <option value="all">جميع الخزائن</option>
            {cashboxes.map((cb) => (
              <option key={cb.id} value={cb.id}>
                {cb.name} ({(cb.currentBalance ?? cb.initialBalance ?? 0).toFixed(2)} د.ل)
              </option>
            ))}
          </select>
        </div>

        {/* Type Select */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)', whiteSpace: 'nowrap' }}>
            نوع الحركة:
          </span>
          <select
            value={selectedTypeFilter}
            onChange={(e) => { setPage(1); setSelectedTypeFilter(e.target.value); }}
            style={{
              padding: '8px 14px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
              backgroundColor: 'var(--color-surface)',
              color: 'var(--color-text-primary)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          >
            <option value="all">جميع الحركات</option>
            <option value={CashDrawerEntryType.SalePayment}>مبيعات طلب</option>
            <option value={CashDrawerEntryType.Inflow}>إيداع يدوي</option>
            <option value={CashDrawerEntryType.Outflow}>سحب يدوي</option>
            <option value={CashDrawerEntryType.Refund}>رد مبيعات</option>
            <option value={CashDrawerEntryType.TransferIn}>استلام توريد وردية</option>
            <option value={CashDrawerEntryType.TransferOut}>توريد وردية</option>
            <option value={CashDrawerEntryType.CashShortage}>عجز وردية</option>
            <option value={CashDrawerEntryType.CashSurplus}>زيادة وردية</option>
          </select>
        </div>

        {/* Date Filter */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Calendar size={16} style={{ color: 'var(--color-text-muted)' }} />
          <input
            type="date"
            value={dateFrom}
            onChange={(e) => { setPage(1); setDateFrom(e.target.value); }}
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--color-border)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          />
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>إلى</span>
          <input
            type="date"
            value={dateTo}
            onChange={(e) => { setPage(1); setDateTo(e.target.value); }}
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--color-border)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          />
        </div>

        <Button
          variant="secondary"
          size="sm"
          onClick={loadData}
          isLoading={isLoading}
          icon={<RotateCcw size={14} />}
        >
          تحديث
        </Button>
      </div>

      {/* Transactions History Table */}
      {isLoading ? (
        <div style={{ padding: '60px', textAlign: 'center' }}>
          <LoadingSpinner size="lg" />
        </div>
      ) : filteredEntries.length === 0 ? (
        <EmptyState
          title="لا توجد حركات مسجلة"
          description="لم يتم تسجيل أي حركات نقدية في الخزينة ضمن الفترة أو الفلاتر المحددة."
          icon={<Wallet size={36} />}
        />
      ) : (
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            overflow: 'hidden',
            boxShadow: 'var(--shadow-card)',
          }}
        >
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right' }}>
            <thead>
              <tr style={{ backgroundColor: 'var(--color-surface-active)', borderBottom: '1px solid var(--color-border)' }}>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>نوع الحركة</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>المبلغ</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الخزينة</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>التاريخ والوقت</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>المستخدم / الكاشير</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>السبب والملاحظات</th>
              </tr>
            </thead>
            <tbody>
              {filteredEntries.map((entry) => {
                const badge = getEntryBadge(entry.entryType);
                const dateObj = new Date(entry.date);
                const dateStr = dateObj.toLocaleDateString('ar-LY', { dateStyle: 'short' });
                const timeStr = dateObj.toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' });

                return (
                  <tr
                    key={entry.id}
                    style={{
                      borderBottom: '1px solid var(--color-border-light)',
                      transition: 'background-color var(--transition-fast)',
                    }}
                  >
                    {/* Entry Type */}
                    <td style={{ padding: '12px 16px' }}>
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '6px',
                          padding: '4px 10px',
                          borderRadius: 'var(--radius-full)',
                          fontSize: '0.8125rem',
                          fontWeight: 700,
                          backgroundColor: badge.bg,
                          color: badge.color,
                        }}
                      >
                        {badge.isPositive ? <ArrowDownLeft size={14} /> : <ArrowUpRight size={14} />}
                        {badge.text}
                      </span>
                    </td>

                    {/* Amount */}
                    <td
                      style={{
                        padding: '12px 16px',
                        fontSize: '1.0625rem',
                        fontWeight: 800,
                        color: badge.isPositive ? 'var(--color-success-text)' : 'var(--color-danger-text)',
                      }}
                    >
                      {badge.isPositive ? '+' : '-'}{entry.amount.toFixed(2)} د.ل
                      {!entry.affectsCash && <div style={{ fontSize: '0.75rem' }}>دفع إلكتروني — لا يؤثر على النقد</div>}
                    </td>

                    {/* Cashbox */}
                    <td style={{ padding: '12px 16px', fontSize: '0.875rem', color: 'var(--color-text-secondary)', fontWeight: 600 }}>
                      {entry.cashboxName || 'الرئيسية'}
                    </td>

                    {/* Date */}
                    <td style={{ padding: '12px 16px', fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>
                      <div>{dateStr}</div>
                      <div style={{ fontSize: '0.75rem' }}>{timeStr}</div>
                    </td>

                    {/* User */}
                    <td style={{ padding: '12px 16px', fontSize: '0.875rem' }}>
                      {entry.userName || 'مستخدم'}
                    </td>

                    {/* Notes / Reason */}
                    <td style={{ padding: '12px 16px', fontSize: '0.875rem', color: 'var(--color-text-secondary)' }}>
                      {entry.notes || '-'}
                      {entry.orderId && (
                        <span style={{ fontSize: '0.75rem', color: 'var(--color-primary)', marginRight: '6px' }}>
                          (فاتورة #{entry.orderId})
                        </span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Manual Inflow / Outflow Modal */}
      <Modal
        isOpen={isManualModalOpen}
        onClose={() => setIsManualModalOpen(false)}
        title={manualEntryType === CashDrawerEntryType.Inflow ? 'إيداع نقدي يدوي في الخزينة' : 'سحب نقدي يدوي من الخزينة'}
        maxWidth="480px"
      >
        <form onSubmit={handleManualSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {formError && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '10px 14px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-danger-bg)',
                color: 'var(--color-danger-text)',
                fontSize: '0.875rem',
              }}
            >
              <AlertCircle size={18} />
              <span>{formError}</span>
            </div>
          )}

          {/* Cashbox Selection */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.875rem', fontWeight: 600 }}>الخزينة المستهدفة *</label>
            <select
              disabled={hasPendingManual} value={manualCashboxId}
              onChange={(e) => setManualCashboxId(Number(e.target.value))}
              style={{
                padding: '10px 14px',
                borderRadius: 'var(--radius-md)',
                border: '1.5px solid var(--color-border)',
                backgroundColor: 'var(--color-surface)',
                color: 'var(--color-text-primary)',
                fontSize: '0.9375rem',
                fontFamily: 'inherit',
              }}
              required
            >
              {cashboxes.filter(cb => cb.isActive).map((cb) => (
                <option key={cb.id} value={cb.id}>
                  {cb.name} (رصيدها: {(cb.currentBalance ?? cb.initialBalance ?? 0).toFixed(2)} د.ل)
                </option>
              ))}
            </select>
          </div>

          {/* Amount */}
          <Input
            label="المبلغ النقدي (د.ل) *"
            type="number"
            step="0.01"
            min="0.01"
            placeholder="0.00"
            disabled={hasPendingManual} value={manualAmount}
            onChange={(e) => setManualAmount(e.target.value)}
            icon={<DollarSign size={18} />}
            autoFocus
            required
          />

          {/* Reason / Notes */}
          <Input
            label="السبب أو البيان *"
            placeholder={
              manualEntryType === CashDrawerEntryType.Inflow
                ? 'مثال: تغذية نقدية لبدء الوردية، توريد رأس مال...'
                : 'مثال: مشتريات خضار طازجة، مصاريف نثرية...'
            }
            disabled={hasPendingManual} value={manualNotes}
            onChange={(e) => setManualNotes(e.target.value)}
            icon={<FileText size={18} />}
            required
            maxLength={500}
          />

          {/* Submit Buttons */}
          <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '10px' }}>
            <Button variant="secondary" onClick={() => setIsManualModalOpen(false)} disabled={isSubmitting}>
              إلغاء
            </Button>
            <Button
              variant={manualEntryType === CashDrawerEntryType.Inflow ? 'primary' : 'danger'}
              type="submit"
              disabled={!!loadError || isLoading || isSubmitting}
              isLoading={isSubmitting}
            >
              {manualEntryType === CashDrawerEntryType.Inflow ? 'تأكيد الإيداع' : 'تأكيد السحب'}
            </Button>
          </div>
        </form>
      </Modal>

      {shifts.length > 0 && (
        <div style={{ overflowX: 'auto' }}>
          <h3>سجل إغلاق الورديات</h3>
          <table style={{ width: '100%', textAlign: 'right', borderCollapse: 'collapse' }}>
            <thead><tr><th>الوردية</th><th>الدرج</th><th>الخزينة المستلمة</th><th>المورّد</th><th>العجز / الزيادة</th><th>الكاشير</th><th>الإغلاق</th></tr></thead>
            <tbody>{shifts.filter(s => (selectedCashboxId === 'all' || s.sourceCashboxId === selectedCashboxId) && (!dateFrom || s.closedAt.split('T')[0] >= dateFrom) && (!dateTo || s.closedAt.split('T')[0] <= dateTo)).map(s => (
              <tr key={s.id}><td>#{s.id}</td><td>{s.sourceCashboxName}</td><td>{s.targetCashboxName}</td><td>{s.actualCash.toFixed(2)} د.ل</td><td>{s.difference?.toFixed(2)} د.ل</td><td>{s.userName}</td><td>{new Date(s.closedAt).toLocaleString('ar-LY')}</td></tr>
            ))}</tbody>
          </table>
        </div>
      )}

      {/* Global Shift Close Modal */}
      <ShiftCloseModal
        isOpen={isShiftCloseOpen}
        onClose={() => {
          setIsShiftCloseOpen(false);
          loadData();
        }}
      />
    </div>
  );
};

export default TreasuryPage;
