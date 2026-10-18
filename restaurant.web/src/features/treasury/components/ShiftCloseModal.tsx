import React, { useState, useEffect, useCallback, useRef } from 'react';
import { ShieldCheck, DollarSign, ArrowRightLeft, Printer, CheckCircle2 } from 'lucide-react';
import Modal from '../../../components/ui/Modal';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useRestaurantInfo } from '../../../hooks/useRestaurantInfo';
import apiClient from '../../../services/apiClient';
import printService from '../../../services/printService';
import type { CashboxDto, CashShiftContextDto, CashShiftDto } from '../../../types/api';
import toast from 'react-hot-toast';

interface ShiftCloseModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const ShiftCloseModal: React.FC<ShiftCloseModalProps> = (props) =>
  props.isOpen ? <ShiftCloseContent {...props} /> : null;

const ShiftCloseContent: React.FC<ShiftCloseModalProps> = ({ isOpen, onClose }) => {
  const { restaurant } = useRestaurantInfo();
  const [cashboxes, setCashboxes] = useState<CashboxDto[]>([]);
  const [sourceCashbox, setSourceCashbox] = useState<CashboxDto | null>(null);
  const [targetCashboxId, setTargetCashboxId] = useState<number | null>(null);
  const [actualCash, setActualCash] = useState<string>('');
  const [notes, setNotes] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // Closed summary state (shown after successful submission)
  const [closedSummary, setClosedSummary] = useState<CashShiftDto | null>(null);
  const [context, setContext] = useState<CashShiftContextDto | null>(null);
  const closeRequest = useRef<{ signature: string; id: string } | null>(null);



  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await apiClient.get<CashboxDto[]>('/api/cashboxes');
      if (res && res.length > 0) {
        const active = res.filter(c => c.isActive);
        setCashboxes(active);

        // 1. Identify Drawer Cashbox (source): prioritize name matching "درج" or "pos" or non-main
        const savedId = Number(localStorage.getItem('posCashboxId'));
        const drawer = active.find(c => c.isDrawer && c.id === savedId) || active.find(c => c.isDrawer);
        setSourceCashbox(drawer || null);

        // 2. Identify Main Treasury (target): prioritize "الخزينة الرئيسية" or first safe different from drawer
        const mainSafe = active.find(c => !c.isDrawer);
        setTargetCashboxId(mainSafe ? mainSafe.id : null);
      }
      else { setCashboxes([]); setSourceCashbox(null); setTargetCashboxId(null); }
    } catch {
      toast.error('فشل جلب بيانات الخزائن لتقفيل الوردية.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    if (!isOpen || !sourceCashbox) return;
    apiClient.get<CashShiftContextDto>(`/api/cashshifts/context/${sourceCashbox.id}`)
      .then(value => { if (!cancelled) setContext(value); })
      .catch((error: Error) => { if (!cancelled) toast.error(error.message); })
      .finally(() => { if (!cancelled) setIsLoading(false); });
    return () => { cancelled = true; };
  }, [isOpen, sourceCashbox]);

  useEffect(() => {
    if (!isOpen) return;
    const timer = window.setTimeout(() => { void loadData(); }, 0);
    return () => window.clearTimeout(timer);
  }, [isOpen, loadData]);

  const parsedActual = Number(actualCash);
  const hasActual = actualCash.trim() !== '' && Number.isFinite(parsedActual) && parsedActual >= 0;

  const handleCloseShift = async () => {
    if (isSubmitting || isLoading) return;
    if (!hasActual) {
      toast.error('يرجى إدخال المبلغ النقدي الفعلي في الدرج.');
      return;
    }

    if (!sourceCashbox) {
      toast.error('لم يتم تحديد خزينة الدرج.');
      return;
    }
    if (!targetCashboxId || targetCashboxId === sourceCashbox.id || !context?.canClose || context.sourceCashboxId !== sourceCashbox.id) {
      toast.error('اختر درجاً وخزينة مستلمة نشطين، وتأكد من وجود وردية قابلة للإغلاق.');
      return;
    }

    setIsSubmitting(true);
    try {
      const payload = {
        sourceCashboxId: sourceCashbox.id, targetCashboxId, actualCash: parsedActual,
        previousShiftId: context.previousShiftId, lastEntryId: context.lastEntryId, notes: notes.trim(),
      };
      const signature = JSON.stringify(payload);
      if (closeRequest.current?.signature !== signature) closeRequest.current = { signature, id: crypto.randomUUID() };
      const saved = await apiClient.post<CashShiftDto>('/api/cashshifts/close', {
        ...payload, clientRequestId: closeRequest.current.id,
      });

      toast.success('تم تقفيل الوردية وتحويل النقدية بنجاح!');

      setClosedSummary(saved);
    } catch (err: unknown) {
      const recovered = closeRequest.current && await apiClient.get<CashShiftDto>(`/api/cashshifts/byrequest/${encodeURIComponent(closeRequest.current.id)}`).catch(() => null);
      if (recovered) {
        setClosedSummary(recovered);
        toast.success('تم حفظ إغلاق الوردية وتأكيد التوريد.');
        return;
      }
      const msg = err instanceof Error ? err.message : 'فشل تقفيل الوردية.';
      toast.error(msg);
      // Refresh the optimistic boundary; preserve the counted cash for correction/retry.
      const updated = await apiClient.get<CashShiftContextDto>(`/api/cashshifts/context/${sourceCashbox.id}`).catch(() => null);
      if (updated) setContext(updated);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePrintZReport = async () => {
    if (!closedSummary) return;

    await printService.printReport({
      title: 'إيصال إغلاق الوردية وتوريد النقدية (Z-Report)',
      subtitle: `وردية الكاشير: ${closedSummary.userName}`,
      periodText: new Date(closedSummary.closedAt).toLocaleString('ar-LY'),
      restaurantName: restaurant?.name,
      generatedBy: closedSummary.userName,
      kpis: [
        { label: 'المبلغ المورّد للخزينة', value: `${closedSummary.actualCash.toFixed(2)} د.ل`, color: '#16a34a' },
        { label: 'خزينة الدرج (المصدر)', value: closedSummary.sourceCashboxName },
        { label: 'الخزينة المستلمة (الهدف)', value: closedSummary.targetCashboxName },
      ],
      headers: ['البيان', 'التفاصيل'],
      rows: [
        ['رقم إغلاق الوردية', String(closedSummary.id)],
        ['تاريخ ووقت الإغلاق', new Date(closedSummary.closedAt).toLocaleString('ar-LY')],
        ['اسم الكاشير المسؤول', closedSummary.userName],
        ['خزينة درج نقطة البيع', closedSummary.sourceCashboxName],
        ['الخزينة المستلمة للتوريد', closedSummary.targetCashboxName],
        ['المبلغ الفعلي المورّد', `${closedSummary.actualCash.toFixed(2)} د.ل`],
        ...(closedSummary.difference != null ? [['العجز / الزيادة', `${closedSummary.difference.toFixed(2)} د.ل`]] : []),
        ['ملاحظات التقفيل', closedSummary.notes || 'لا توجد ملاحظات إضافية'],
      ],
    });
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => { if (!isSubmitting) onClose(); }}
      title="تقفيل الوردية وتوريد النقدية (Blind Shift Close)"
      maxWidth="580px"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        {closedSummary ? (
          /* Success / Summary View */
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', textAlign: 'center' }}>
            <div
              style={{
                width: '64px',
                height: '64px',
                borderRadius: '50%',
                backgroundColor: 'var(--color-primary-bg)',
                color: 'var(--color-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                margin: '0 auto',
              }}
            >
              <CheckCircle2 size={36} />
            </div>

            <div>
              <h3 style={{ fontSize: '1.25rem', fontWeight: 800, margin: 0 }}>
                تم إغلاق الوردية وتوريد النقدية بنجاح!
              </h3>
              <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem', marginTop: '4px' }}>
                تم قيد سحب النقدية من درج الكاشير وإيداعها في الخزينة المستلمة
              </p>
            </div>

            <div
              style={{
                backgroundColor: 'var(--color-surface)',
                border: '1px solid var(--color-border)',
                borderRadius: 'var(--radius-md)',
                padding: '16px',
                textAlign: 'right',
                display: 'flex',
                flexDirection: 'column',
                gap: '8px',
                fontSize: '0.9rem',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>المبلغ المحصور والمورّد:</span>
                <strong style={{ fontSize: '1.1rem', color: 'var(--color-primary)' }}>
                  {closedSummary.actualCash.toFixed(2)} د.ل
                </strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>من درج:</span>
                <strong>{closedSummary.sourceCashboxName}</strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>إلى خزينة:</span>
                <strong>{closedSummary.targetCashboxName}</strong>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <span style={{ color: 'var(--color-text-muted)' }}>الوقت:</span>
                <span>{new Date(closedSummary.closedAt).toLocaleString('ar-LY')}</span>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center', marginTop: '8px' }}>
              <Button variant="secondary" onClick={onClose}>
                إغلاق النافذة
              </Button>
              <Button variant="secondary" onClick={handlePrintZReport} icon={<Printer size={18} />}>
                طباعة تقرير Z-Report
              </Button>
            </div>
          </div>
        ) : (
          /* Normal Blind Input View */
          <>
            {(!sourceCashbox || !targetCashboxId) && !isLoading && (
              <p role="alert">يجب إعداد درج نقطة بيع وخزينة مستلمة نشطين من الإعدادات.</p>
            )}
            {context && !context.canClose && <p role="status">الوردية مغلقة ولا توجد حركات جديدة.</p>}
            {/* Strict Blind Shift Security Notice */}
            <div
              style={{
                padding: '14px 16px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'rgba(56, 224, 123, 0.08)',
                border: '1px solid rgba(56, 224, 123, 0.25)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: '12px',
              }}
            >
              <ShieldCheck size={22} style={{ color: 'var(--color-primary)', flexShrink: 0, marginTop: '2px' }} />
              <div style={{ fontSize: '0.85rem', lineHeight: 1.5, color: 'var(--color-text-primary)' }}>
                <strong style={{ display: 'block', marginBottom: '2px', color: 'var(--color-primary)' }}>
                  نظام الإغلاق الأعمى (Blind Shift Close)
                </strong>
                لمنع التلاعب وضمان النزاهة المالية، تم إخفاء أرصدة النظام التقديرية. يُطلب من الكاشير عدّ النقدية الفعلية الموجودة في الدرج وإدخالها بأمانة ليتم توريدها ومطابقتها مركزياً.
              </div>
            </div>

            {/* Source & Target Treasury Mapping */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: '1fr 1fr',
                gap: '14px',
                backgroundColor: 'var(--color-surface)',
                padding: '14px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
              }}
            >
              {/* Source: Drawer */}
              <div>
                <label style={{ display: 'block', fontSize: '0.8125rem', fontWeight: 600, marginBottom: '6px' }}>
                  خزينة الدرج (المصدر)
                </label>
                <select
                  value={sourceCashbox?.id || ''}
                  disabled={isSubmitting || isLoading}
                  onChange={(e) => {
                    const id = Number(e.target.value);
                    const found = cashboxes.find((c) => c.id === id);
                    if (found && found.id !== sourceCashbox?.id) {
                      setContext(null);
                      setIsLoading(true);
                      setSourceCashbox(found);
                    }
                  }}
                  style={{
                    width: '100%',
                    padding: '8px 12px',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid var(--color-border)',
                    backgroundColor: 'var(--color-bg-base)',
                    color: 'var(--color-text-primary)',
                    fontSize: '0.875rem',
                    fontFamily: 'inherit',
                  }}
                >
                  {cashboxes.filter(cb => cb.isDrawer).map((cb) => (
                    <option key={cb.id} value={cb.id}>
                      {cb.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Target: Safe to deposit */}
              <div>
                <label style={{ display: 'block', fontSize: '0.8125rem', fontWeight: 600, marginBottom: '6px' }}>
                  تحويل وتوريد النقدية إلى:
                </label>
                <select
                  value={targetCashboxId || ''}
                  disabled={isSubmitting || isLoading}
                  onChange={(e) => setTargetCashboxId(Number(e.target.value))}
                  style={{
                    width: '100%',
                    padding: '8px 12px',
                    borderRadius: 'var(--radius-sm)',
                    border: '1px solid var(--color-border)',
                    backgroundColor: 'var(--color-bg-base)',
                    color: 'var(--color-text-primary)',
                    fontSize: '0.875rem',
                    fontFamily: 'inherit',
                  }}
                >
                  {cashboxes
                    .filter((cb) => !cb.isDrawer && cb.id !== sourceCashbox?.id)
                    .map((cb) => (
                      <option key={cb.id} value={cb.id}>
                        {cb.name} (الخزينة المستلمة)
                      </option>
                    ))}
                </select>
              </div>
            </div>

            {/* Actual Cash Input (Blind count) */}
            <div>
              <Input
                label="النقد الفعلي في الدرج (المحصور يدوياً) *"
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00 د.ل"
                value={actualCash}
                disabled={isSubmitting || isLoading}
                onChange={(e) => setActualCash(e.target.value)}
                icon={<DollarSign size={18} />}
                autoFocus
              />
              <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', marginTop: '4px' }}>
                أدخل المبلغ الإجمالي الذي قمت بعدّه في الدرج لتوريده.
              </div>
            </div>

            {/* Optional Notes */}
            <div>
              <Input
                label="ملاحظات تقفيل الوردية (اختياري)"
                placeholder="أي ملاحظات حول الوردية أو تسليم العهدة..."
                value={notes}
                disabled={isSubmitting}
                maxLength={500}
                onChange={(e) => setNotes(e.target.value)}
              />
            </div>

            {/* Action Buttons */}
            <div style={{ display: 'flex', gap: '12px', justifyContent: 'flex-end', marginTop: '6px' }}>
              <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
                إلغاء
              </Button>
              <Button
                variant="primary"
                disabled={!hasActual || !context?.canClose || context.sourceCashboxId !== sourceCashbox?.id || !targetCashboxId || isLoading || isSubmitting}
                onClick={handleCloseShift}
                isLoading={isSubmitting || isLoading}
                icon={<ArrowRightLeft size={18} />}
              >
                تأكيد التقفيل وتوريد النقدية
              </Button>
            </div>
          </>
        )}
      </div>
    </Modal>
  );
};

export default ShiftCloseModal;
