import React, { useState, useEffect, useCallback } from 'react';
import { Clock, Calculator, AlertTriangle, CheckCircle2, DollarSign } from 'lucide-react';
import Modal from '../../../components/ui/Modal';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useAuth } from '../../../context/AuthContext';
import apiClient from '../../../services/apiClient';
import type { CashboxDto, CashDrawerEntryDto, CashboxBalanceDto } from '../../../types/api';
import toast from 'react-hot-toast';

interface ShiftCloseModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const ShiftCloseModal: React.FC<ShiftCloseModalProps> = ({ isOpen, onClose }) => {
  const [cashbox, setCashbox] = useState<CashboxDto | null>(null);
  const [expectedCash, setExpectedCash] = useState<number>(0);
  const [actualCash, setActualCash] = useState<string>('');
  const [totalSales, setTotalSales] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const { hasPermission } = useAuth();
  const canViewTotals = hasPermission('Permission.Shift.ViewTotals');

  const loadShiftData = useCallback(async () => {
    setIsLoading(true);
    try {
      // 1. Get default or active cashbox
      const cashboxes = await apiClient.get<CashboxDto[]>('/api/cashboxes');
      if (cashboxes && cashboxes.length > 0) {
        const active = cashboxes[0];
        setCashbox(active);
        try {
          const bal = await apiClient.get<CashboxBalanceDto>(`/api/cashboxes/${active.id}/balance`);
          setExpectedCash(bal.currentBalance ?? active.initialBalance ?? 0);
        } catch {
          setExpectedCash(active.currentBalance ?? active.initialBalance ?? 0);
        }

        // 2. Fetch today's sales and entries for this cashbox
        const today = new Date().toISOString().split('T')[0];
        const entries = await apiClient.get<CashDrawerEntryDto[]>('/api/cashdrawerentries', {
          cashboxId: active.id,
          fromDate: today,
        });

        if (entries) {
          const salesTotal = entries
            .filter((e) => e.entryType === 1) // SalePayment
            .reduce((sum, e) => sum + e.amount, 0);
          setTotalSales(salesTotal);
        }
      }
    } catch {
      toast.error('فشل جلب بيانات الوردية الحالية للخزينة.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isOpen) {
      loadShiftData();
      setActualCash('');
    }
  }, [isOpen, loadShiftData]);

  const parsedActual = parseFloat(actualCash);
  const hasActual = !isNaN(parsedActual);
  const difference = hasActual ? parsedActual - expectedCash : null;

  const handleCloseShift = async () => {
    if (!hasActual) {
      toast.error('يرجى إدخال المبلغ النقدي الفعلي في الدرج.');
      return;
    }

    if (!cashbox) return;

    setIsSubmitting(true);
    try {
      // Record closing shift cash drawer entry
      await apiClient.post('/api/cashdrawerentries', {
        amount: parsedActual,
        entryType: 3, // Outflow (or closing balance adjustment)
        notes: `إغلاق وردية - النقد الفعلي: ${parsedActual} د.ل - الفارق: ${difference?.toFixed(2)} د.ل`,
        cashboxId: cashbox.id,
      });

      toast.success('تم تقفيل الوردية وتسجيل المطابقة بنجاح!');
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل إغلاق الوردية.';
      toast.error(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="تقفيل ومطابقة الوردية (Shift Close)" maxWidth="560px">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {/* Cashbox info banner */}
        <div
          style={{
            padding: '12px 16px',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'var(--color-primary-bg)',
            color: 'var(--color-success-text)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            fontWeight: 600,
            fontSize: '0.9375rem',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Clock size={18} />
            <span>الخزينة: {cashbox?.name || 'الرئيسية'}</span>
          </div>
          <span>وردية اليوم ({new Date().toLocaleDateString('ar-LY')})</span>
        </div>

        {/* Expected Summary (Permission Guarded) */}
        {canViewTotals ? (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <div
              style={{
                padding: '16px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-surface)',
                border: '1px solid var(--color-border)',
              }}
            >
              <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)' }}>مبيعات الوردية الكلية</span>
              <div style={{ fontSize: '1.375rem', fontWeight: 700, color: 'var(--color-text-primary)', marginTop: '4px' }}>
                {totalSales.toFixed(2)} <span style={{ fontSize: '0.875rem' }}>د.ل</span>
              </div>
            </div>

            <div
              style={{
                padding: '16px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-surface)',
                border: '1px solid var(--color-border)',
              }}
            >
              <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)' }}>النقد المتوقع في الدرج</span>
              <div style={{ fontSize: '1.375rem', fontWeight: 700, color: 'var(--color-success-text)', marginTop: '4px' }}>
                {expectedCash.toFixed(2)} <span style={{ fontSize: '0.875rem' }}>د.ل</span>
              </div>
            </div>
          </div>
        ) : (
          <div
            style={{
              padding: '12px',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--color-info-bg)',
              color: 'var(--color-info-text)',
              fontSize: '0.8125rem',
            }}
          >
            نظام الإغلاق الأعمى نشط: يرجى عد النقد المتواجد في الدرج وإدخاله دون الاطلاع على الإجماليات المسبقة.
          </div>
        )}

        {/* Actual Cash Input */}
        <div>
          <Input
            label="النقد الفعلي في الدرج (بعد العد اليدوي) *"
            type="number"
            step="0.01"
            placeholder="0.00"
            value={actualCash}
            onChange={(e) => setActualCash(e.target.value)}
            icon={<DollarSign size={18} />}
            autoFocus
          />
        </div>

        {/* Difference Indicator (If Actual entered and canViewTotals) */}
        {hasActual && canViewTotals && difference !== null && (
          <div
            style={{
              padding: '14px 18px',
              borderRadius: 'var(--radius-md)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              backgroundColor:
                difference === 0
                  ? 'var(--color-success-bg)'
                  : difference < 0
                  ? 'var(--color-danger-bg)'
                  : 'var(--color-info-bg)',
              color:
                difference === 0
                  ? 'var(--color-success-text)'
                  : difference < 0
                  ? 'var(--color-danger-text)'
                  : 'var(--color-info-text)',
              border: `1px solid ${
                difference === 0
                  ? 'var(--color-success-border)'
                  : difference < 0
                  ? 'var(--color-danger-border)'
                  : 'var(--color-info-border)'
              }`,
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 600 }}>
              {difference === 0 ? (
                <CheckCircle2 size={20} />
              ) : (
                <AlertTriangle size={20} />
              )}
              <span>
                {difference === 0
                  ? 'النقد متطابق تماماً!'
                  : difference < 0
                  ? 'يوجد عجز في الخزينة'
                  : 'توجد زيادة في الخزينة'}
              </span>
            </div>
            <div style={{ fontSize: '1.25rem', fontWeight: 800 }}>
              {Math.abs(difference).toFixed(2)} د.ل
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end', marginTop: '10px' }}>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            إلغاء
          </Button>
          <Button
            variant="warning"
            onClick={handleCloseShift}
            isLoading={isSubmitting || isLoading}
            icon={<Calculator size={18} />}
          >
            تأكيد وتقفيل الوردية
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ShiftCloseModal;
