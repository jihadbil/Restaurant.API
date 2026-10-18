import React, { useState } from 'react';
import { Printer, Receipt, ChefHat } from 'lucide-react';
import Modal from '../../components/ui/Modal';
import Button from '../../components/ui/Button';
import ReceiptPrintTemplate from './ReceiptPrintTemplate';
import KitchenTicketPrintTemplate from './KitchenTicketPrintTemplate';
import printService from '../../services/printService';
import type { OrderDto } from '../../types/api';
import toast from 'react-hot-toast';
import { useRestaurantInfo } from '../../hooks/useRestaurantInfo';

interface PrintPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  order: OrderDto | null;
  restaurantName?: string;
  paymentMethodName?: string;
}

export const PrintPreviewModal: React.FC<PrintPreviewModalProps> = props =>
  props.isOpen && props.order ? <PrintPreviewContent {...props} /> : null;

const PrintPreviewContent: React.FC<PrintPreviewModalProps> = ({
  isOpen,
  onClose,
  order,
  restaurantName,
  paymentMethodName,
}) => {
  const [activeTab, setActiveTab] = useState<'receipt' | 'kitchen'>('receipt');
  const [isPrinting, setIsPrinting] = useState<boolean>(false);
  const { restaurant, error, isLoading } = useRestaurantInfo();
  const name = restaurantName?.trim() || restaurant?.name;

  if (!order) return null;

  const handlePrintReceipt = async () => {
    setIsPrinting(true);
    try {
      await printService.printReceipt(order, undefined, paymentMethodName);
      toast.success('اكتمل إرسال الفاتورة للطباعة.');
    } catch (cause: unknown) {
      toast.error(cause instanceof Error ? cause.message : 'حدث خطأ أثناء محاولة الطباعة.');
    } finally {
      setIsPrinting(false);
    }
  };

  const handlePrintKitchen = async () => {
    setIsPrinting(true);
    try {
      await printService.printKitchenTicket(order);
      toast.success('اكتمل إرسال تذاكر المطبخ للطباعة.');
    } catch (cause: unknown) {
      toast.error(cause instanceof Error ? cause.message : 'حدث خطأ أثناء محاولة طباعة تذكرة المطبخ.');
    } finally {
      setIsPrinting(false);
    }
  };

  const handlePrintBoth = async () => {
    setIsPrinting(true);
    try {
      await printService.printOrder(order, undefined, paymentMethodName);
      toast.success('اكتمل إرسال الفاتورة وتذاكر المطبخ للطباعة.');
    } catch (cause: unknown) {
      toast.error(cause instanceof Error ? cause.message : 'حدث خطأ أثناء طباعة الإيصالات.');
    } finally {
      setIsPrinting(false);
    }
  };

  const handleOpenPrintDocument = async () => {
    setIsPrinting(true);
    try { await printService.printReceipt(order, undefined, paymentMethodName, 'document'); }
    catch (cause: unknown) { toast.error(cause instanceof Error ? cause.message : 'تعذر فتح صفحة الطباعة.'); }
    finally { setIsPrinting(false); }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => { if (!isPrinting) onClose(); }}
      title={`معاينة الطباعة — طلب رقم #${order.orderNumber}`}
      maxWidth="580px"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {error && <p role="alert" style={{ color: 'var(--color-danger-text)' }}>{error}</p>}
        {/* Top Control Tabs */}
        <div
          style={{
            display: 'flex',
            backgroundColor: 'var(--color-bg-base)',
            padding: '4px',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--color-border)',
            gap: '6px',
          }}
        >
          <button
            type="button"
            onClick={() => setActiveTab('receipt')}
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '8px',
              padding: '10px 14px',
              borderRadius: 'var(--radius-sm)',
              border: 'none',
              cursor: 'pointer',
              fontWeight: 700,
              fontSize: '0.9rem',
              backgroundColor: activeTab === 'receipt' ? 'var(--color-surface)' : 'transparent',
              color: activeTab === 'receipt' ? 'var(--color-primary)' : 'var(--color-text-muted)',
              boxShadow: activeTab === 'receipt' ? '0 1px 3px rgba(0,0,0,0.1)' : 'none',
              transition: 'all 0.15s ease',
            }}
          >
            <Receipt size={18} />
            <span>فاتورة الزبون (Receipt)</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('kitchen')}
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '8px',
              padding: '10px 14px',
              borderRadius: 'var(--radius-sm)',
              border: 'none',
              cursor: 'pointer',
              fontWeight: 700,
              fontSize: '0.9rem',
              backgroundColor: activeTab === 'kitchen' ? 'var(--color-surface)' : 'transparent',
              color: activeTab === 'kitchen' ? 'var(--color-primary)' : 'var(--color-text-muted)',
              boxShadow: activeTab === 'kitchen' ? '0 1px 3px rgba(0,0,0,0.1)' : 'none',
              transition: 'all 0.15s ease',
            }}
          >
            <ChefHat size={18} />
            <span>تذكرة المطبخ (Kitchen Ticket)</span>
          </button>
        </div>

        {/* Paper Preview Viewport with high-fidelity paper styling */}
        <div
          style={{
            maxHeight: '440px',
            overflowY: 'auto',
            padding: '16px',
            backgroundColor: '#27272a',
            borderRadius: 'var(--radius-lg)',
            display: 'flex',
            justifyContent: 'center',
            boxShadow: 'inset 0 2px 8px rgba(0,0,0,0.3)',
          }}
        >
          <div
            style={{
              boxShadow: '0 8px 24px rgba(0,0,0,0.45)',
              borderRadius: '4px',
              overflow: 'hidden',
              transformOrigin: 'top center',
            }}
          >
            {activeTab === 'receipt' ? (
              <ReceiptPrintTemplate
                order={order}
                restaurantName={name || 'جار تحميل اسم المطعم…'}
                paymentMethodName={paymentMethodName}
              />
            ) : (
              <KitchenTicketPrintTemplate order={order} />
            )}
          </div>
        </div>

        {/* Action Buttons */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: '10px',
            paddingTop: '8px',
            borderTop: '1px solid var(--color-border)',
          }}
        >
          <Button variant="secondary" onClick={onClose} disabled={isPrinting}>
            إغلاق
          </Button>

          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            <Button variant="secondary" onClick={handleOpenPrintDocument} disabled={isPrinting || isLoading || !name}>
              فتح صفحة الطباعة
            </Button>
            <Button
              variant="secondary"
              onClick={handlePrintReceipt}
              disabled={isLoading || !name}
              isLoading={isPrinting}
              icon={<Receipt size={16} />}
            >
              طباعة الفاتورة
            </Button>

            <Button
              variant="secondary"
              onClick={handlePrintKitchen}
              isLoading={isPrinting}
              icon={<ChefHat size={16} />}
            >
              طباعة المطبخ
            </Button>

            <Button
              variant="primary"
              onClick={handlePrintBoth}
              disabled={isLoading || !name}
              isLoading={isPrinting}
              icon={<Printer size={16} />}
            >
              طباعة الكل
            </Button>
          </div>
        </div>
      </div>
    </Modal>
  );
};

export default PrintPreviewModal;
