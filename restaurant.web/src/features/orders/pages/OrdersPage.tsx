import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Receipt,
  Search,
  Printer,
  Trash2,
  Eye,
  RotateCcw,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import Modal from '../../../components/ui/Modal';
import StatusBadge from '../../../components/ui/StatusBadge';
import ConfirmDialog from '../../../components/ui/ConfirmDialog';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import ReceiptPrintTemplate from '../../printing/ReceiptPrintTemplate';
import KitchenTicketPrintTemplate from '../../printing/KitchenTicketPrintTemplate';
import printService from '../../../services/printService';
import { useAuth } from '../../../context/AuthContext';
import apiClient from '../../../services/apiClient';
import type { OrderDto } from '../../../types/api';
import { OrderStatus, OrderType } from '../../../types/api';
import toast from 'react-hot-toast';

export const OrdersPage: React.FC = () => {
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [dateTo, setDateTo] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [isFilterByDate, setIsFilterByDate] = useState<boolean>(true);

  // Details Modal
  const [selectedOrder, setSelectedOrder] = useState<OrderDto | null>(null);

  // Print Target State
  const [printReceiptTarget, setPrintReceiptTarget] = useState<OrderDto | null>(null);
  const [printKitchenTarget, setPrintKitchenTarget] = useState<OrderDto | null>(null);

  // Delete / Void State
  const [deleteTarget, setDeleteTarget] = useState<OrderDto | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  const { hasPermission } = useAuth();
  const canVoidOrder = hasPermission('Permission.Orders.VoidOrder');

  // Load Orders
  const loadOrders = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await apiClient.get<OrderDto[]>('/api/orders');
      setOrders(res || []);
    } catch {
      toast.error('فشل تحميل قائمة الطلبات والفواتير.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  // Status Change Handler
  const handleStatusChange = async (orderId: number, newStatus: OrderStatus) => {
    try {
      const order = orders.find((o) => o.id === orderId);
      if (!order) return;

      await apiClient.put(`/api/orders/${orderId}`, {
        id: orderId,
        orderStatus: newStatus,
        orderType: order.orderType,
        notes: order.notes,
        discount: order.discount,
      });

      toast.success('تم تحديث حالة الطلب بنجاح!');
      setOrders((prev) =>
        prev.map((o) => (o.id === orderId ? { ...o, orderStatus: newStatus } : o))
      );
      if (selectedOrder && selectedOrder.id === orderId) {
        setSelectedOrder((prev) => (prev ? { ...prev, orderStatus: newStatus } : null));
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل تغيير حالة الطلب.';
      toast.error(msg);
    }
  };

  // Void / Cancel Order
  const handleVoidConfirm = async () => {
    if (!deleteTarget) return;

    setIsDeleting(true);
    try {
      await apiClient.delete(`/api/orders/${deleteTarget.id}`);
      toast.success(`تم إلغاء وحذف الفاتورة #${deleteTarget.orderNumber} بنجاح.`);
      setDeleteTarget(null);
      if (selectedOrder?.id === deleteTarget.id) {
        setSelectedOrder(null);
      }
      loadOrders();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل إلغاء الطلب.';
      toast.error(msg);
    } finally {
      setIsDeleting(false);
    }
  };

  // Printing Trigger
  const handleReprintReceipt = (order: OrderDto) => {
    setPrintReceiptTarget(order);
    setTimeout(() => {
      printService.printReceipt(order);
    }, 100);
  };

  const handleReprintKitchen = (order: OrderDto) => {
    setPrintKitchenTarget(order);
    setTimeout(() => {
      window.print();
    }, 100);
  };

  // Filtered Orders
  const filteredOrders = useMemo(() => {
    return orders.filter((order) => {
      // 1. Status Filter
      if (statusFilter !== 'all') {
        const targetStatus = Number(statusFilter);
        if (order.orderStatus !== targetStatus) return false;
      }

      // 2. Date Filter
      if (isFilterByDate && order.date) {
        const orderDateStr = order.date.split('T')[0];
        if (dateFrom && orderDateStr < dateFrom) return false;
        if (dateTo && orderDateStr > dateTo) return false;
      }

      // 3. Search Query (OrderNumber, Customer/User, or Notes)
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase().trim();
        const matchNumber = String(order.orderNumber).includes(q);
        const matchUser = order.userName ? order.userName.toLowerCase().includes(q) : false;
        const matchNotes = order.notes ? order.notes.toLowerCase().includes(q) : false;
        return matchNumber || matchUser || matchNotes;
      }

      return true;
    });
  }, [orders, statusFilter, isFilterByDate, dateFrom, dateTo, searchQuery]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Page Title & Stats */}
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
            إدارة الطلبات وسجل الفواتير
          </h2>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem', marginTop: '4px' }}>
            متابعة حالة الطلبات اللحظية، إعادة الطباعة، واستعراض تفاصيل المبيعات
          </p>
        </div>

        <Button
          variant="secondary"
          size="md"
          onClick={loadOrders}
          isLoading={isLoading}
          icon={<RotateCcw size={16} />}
        >
          تحديث القائمة
        </Button>
      </div>

      {/* Control Filter Toolbar */}
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
        {/* Search */}
        <div style={{ maxWidth: '300px', width: '100%' }}>
          <Input
            placeholder="بحث برقم الطلب أو الكاشير..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            icon={<Search size={18} />}
          />
        </div>

        {/* Status Filter */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>الحالة:</span>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
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
            <option value="all">جميع الحالات</option>
            <option value={OrderStatus.Preparing}>قيد التحضير</option>
            <option value={OrderStatus.Ready}>جاهزة</option>
            <option value={OrderStatus.Delivered}>تم التسليم</option>
            <option value={OrderStatus.Cancelled}>ملغاة</option>
          </select>
        </div>

        {/* Date Filter */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <input
            type="checkbox"
            id="filterDate"
            checked={isFilterByDate}
            onChange={(e) => setIsFilterByDate(e.target.checked)}
            style={{ width: '16px', height: '16px', accentColor: 'var(--color-primary)', cursor: 'pointer' }}
          />
          <label htmlFor="filterDate" style={{ fontSize: '0.875rem', cursor: 'pointer', fontWeight: 600 }}>
            تصفية بالتاريخ:
          </label>
          <input
            type="date"
            value={dateFrom}
            onChange={(e) => setDateFrom(e.target.value)}
            disabled={!isFilterByDate}
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--color-border)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
              backgroundColor: isFilterByDate ? 'var(--color-surface)' : 'var(--color-surface-active)',
            }}
          />
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)' }}>إلى</span>
          <input
            type="date"
            value={dateTo}
            onChange={(e) => setDateTo(e.target.value)}
            disabled={!isFilterByDate}
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--color-border)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
              backgroundColor: isFilterByDate ? 'var(--color-surface)' : 'var(--color-surface-active)',
            }}
          />
        </div>
      </div>

      {/* Orders Table */}
      {isLoading ? (
        <div style={{ padding: '60px', textAlign: 'center' }}>
          <LoadingSpinner size="lg" />
        </div>
      ) : filteredOrders.length === 0 ? (
        <EmptyState
          title="لم يتم العثور على أي طلبات"
          description={searchQuery || statusFilter !== 'all' ? 'لا توجد فواتير تطابق الفلاتر المحددة.' : 'لم يتم تسجيل أي طلبات بعد.'}
          icon={<Receipt size={36} />}
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
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>رقم الطلب</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>التاريخ والوقت</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>نوع الطلب</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الكاشير</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الإجمالي الصافي</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>حالة الطلب</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredOrders.map((order) => {
                const dateObj = new Date(order.date || Date.now());
                const timeStr = dateObj.toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' });
                const dateStr = dateObj.toLocaleDateString('ar-LY', { dateStyle: 'short' });

                return (
                  <tr
                    key={order.id}
                    style={{
                      borderBottom: '1px solid var(--color-border-light)',
                      transition: 'background-color var(--transition-fast)',
                    }}
                  >
                    <td style={{ padding: '14px 16px', fontWeight: 800 }}>
                      #{order.orderNumber}
                    </td>

                    <td style={{ padding: '14px 16px', fontSize: '0.875rem', color: 'var(--color-text-secondary)' }}>
                      <div>{dateStr}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>{timeStr}</div>
                    </td>

                    <td style={{ padding: '14px 16px', fontSize: '0.875rem' }}>
                      {order.orderType === OrderType.Indoor ? (
                        <span style={{ fontWeight: 600, color: 'var(--color-success-text)' }}>داخلي</span>
                      ) : order.orderType === OrderType.Outdoor ? (
                        <span style={{ fontWeight: 600, color: 'var(--color-warning-text)' }}>سفري</span>
                      ) : (
                        <span style={{ fontWeight: 600, color: 'var(--color-danger-text)' }}>توصيل</span>
                      )}
                    </td>

                    <td style={{ padding: '14px 16px', fontSize: '0.875rem', color: 'var(--color-text-secondary)' }}>
                      {order.userName || 'كاشير'}
                    </td>

                    <td style={{ padding: '14px 16px', fontWeight: 800, color: 'var(--color-success-text)', fontSize: '1rem' }}>
                      {(order.total || 0).toFixed(2)} د.ل
                    </td>

                    <td style={{ padding: '14px 16px' }}>
                      <StatusBadge status={order.orderStatus ?? OrderStatus.Preparing} />
                    </td>

                    <td style={{ padding: '14px 16px', textAlign: 'left' }}>
                      <div style={{ display: 'inline-flex', gap: '6px' }}>
                        {/* View Details */}
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => setSelectedOrder(order)}
                          title="عرض تفاصيل الفاتورة"
                          icon={<Eye size={15} />}
                        />

                        {/* Print Receipt */}
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => handleReprintReceipt(order)}
                          title="إعادة طباعة الفاتورة"
                          icon={<Printer size={15} />}
                        />

                        {/* Void / Delete Order */}
                        {canVoidOrder && (
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => setDeleteTarget(order)}
                            title="إلغاء وحذف الفاتورة"
                            icon={<Trash2 size={15} />}
                          />
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Order Details Drawer / Modal */}
      {selectedOrder && (
        <Modal
          isOpen={Boolean(selectedOrder)}
          onClose={() => setSelectedOrder(null)}
          title={`تفاصيل الفاتورة #${selectedOrder.orderNumber}`}
          maxWidth="640px"
        >
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            {/* Order Header Summary */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(3, 1fr)',
                gap: '12px',
                padding: '14px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-surface-active)',
              }}
            >
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>حالة الطلب:</span>
                <div style={{ marginTop: '4px' }}>
                  <StatusBadge status={selectedOrder.orderStatus ?? OrderStatus.Preparing} />
                </div>
              </div>

              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>نوع الطلب:</span>
                <div style={{ fontWeight: 700, marginTop: '4px' }}>
                  {selectedOrder.orderType === OrderType.Indoor ? 'داخلي' : selectedOrder.orderType === OrderType.Outdoor ? 'سفري' : 'توصيل'}
                </div>
              </div>

              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>الكاشير المسؤول:</span>
                <div style={{ fontWeight: 700, marginTop: '4px' }}>{selectedOrder.userName || 'كاشير'}</div>
              </div>
            </div>

            {/* Quick Status Transition Buttons */}
            <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
              <span style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--color-text-muted)' }}>
                تحديث الحالة:
              </span>
              <Button
                variant={selectedOrder.orderStatus === OrderStatus.Preparing ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => handleStatusChange(selectedOrder.id, OrderStatus.Preparing)}
              >
                قيد التحضير
              </Button>
              <Button
                variant={selectedOrder.orderStatus === OrderStatus.Ready ? 'primary' : 'secondary'}
                size="sm"
                onClick={() => handleStatusChange(selectedOrder.id, OrderStatus.Ready)}
              >
                جاهزة للتسليم
              </Button>
              <Button
                variant={selectedOrder.orderStatus === OrderStatus.Delivered ? 'success' : 'secondary'}
                size="sm"
                onClick={() => handleStatusChange(selectedOrder.id, OrderStatus.Delivered)}
              >
                تم التسليم
              </Button>
            </div>

            {/* Order Items Table */}
            <div>
              <h4 style={{ fontSize: '0.9375rem', fontWeight: 700, marginBottom: '8px' }}>الأصناف والوجبات:</h4>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
                <thead>
                  <tr style={{ borderBottom: '1.5px solid var(--color-border)', backgroundColor: 'var(--color-surface-active)' }}>
                    <th style={{ padding: '8px 12px', textAlign: 'right' }}>الصنف</th>
                    <th style={{ padding: '8px 12px', textAlign: 'center' }}>الكمية</th>
                    <th style={{ padding: '8px 12px', textAlign: 'left' }}>السعر</th>
                    <th style={{ padding: '8px 12px', textAlign: 'left' }}>الإجمالي</th>
                  </tr>
                </thead>
                <tbody>
                  {(selectedOrder.orderItems || []).map((item, idx) => {
                    const itemTotal = item.total ?? item.lineTotal ?? (item.quantity * (item.unitSalePrice || 0));
                    return (
                      <tr key={idx} style={{ borderBottom: '1px solid var(--color-border-light)' }}>
                        <td style={{ padding: '8px 12px', fontWeight: 600 }}>
                          {item.productName || 'صنف'}
                          {item.notes && item.notes !== 'لا يوجد ملاحظات' && (
                            <div style={{ fontSize: '0.75rem', color: 'var(--color-danger-text)', fontStyle: 'italic' }}>
                              * {item.notes}
                            </div>
                          )}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'center', fontWeight: 700 }}>
                          {item.quantity}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'left' }}>
                          {(item.unitSalePrice || 0).toFixed(2)} د.ل
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 700 }}>
                          {itemTotal.toFixed(2)} د.ل
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Financial Summary */}
            <div
              style={{
                padding: '12px 16px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-surface-active)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <div>
                <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)' }}>المجموع الفرعي: </span>
                <span style={{ fontWeight: 600 }}>{((selectedOrder.total || 0) + (selectedOrder.discount || 0)).toFixed(2)} د.ل</span>
                {(selectedOrder.discount || 0) > 0 && (
                  <span style={{ color: 'var(--color-danger)', marginRight: '12px', fontSize: '0.8125rem' }}>
                    (الخصم: -{(selectedOrder.discount || 0).toFixed(2)} د.ل)
                  </span>
                )}
              </div>
              <div style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--color-success-text)' }}>
                الصافي: {(selectedOrder.total || 0).toFixed(2)} د.ل
              </div>
            </div>

            {/* Actions: Print Receipt & Kitchen Ticket */}
            <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '10px' }}>
              <Button
                variant="secondary"
                size="md"
                onClick={() => handleReprintKitchen(selectedOrder)}
                icon={<Printer size={16} />}
              >
                طباعة تذكرة المطبخ
              </Button>
              <Button
                variant="primary"
                size="md"
                onClick={() => handleReprintReceipt(selectedOrder)}
                icon={<Printer size={16} />}
              >
                طباعة الفاتورة
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleVoidConfirm}
        title="إلغاء وحذف الفاتورة"
        message={`هل أنت متأكد من رغبتك في إلغاء الفاتورة #${deleteTarget?.orderNumber}؟ سيتم رد المبالغ المدفوعة للخزينة.`}
        confirmText="نعم، احذف الفاتورة"
        variant="danger"
        isLoading={isDeleting}
      />

      {/* Hidden Thermal Print Areas for Reprinting */}
      {printReceiptTarget && (
        <div style={{ display: 'none' }} className="print-only">
          <ReceiptPrintTemplate order={printReceiptTarget} />
        </div>
      )}
      {printKitchenTarget && (
        <div style={{ display: 'none' }} className="print-only">
          <KitchenTicketPrintTemplate order={printKitchenTarget} />
        </div>
      )}
    </div>
  );
};

export default OrdersPage;
