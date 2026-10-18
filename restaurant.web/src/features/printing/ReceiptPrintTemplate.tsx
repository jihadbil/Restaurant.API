import React from 'react';
import type { OrderDto } from '../../types/api';
import { OrderType } from '../../types/api';

interface ReceiptPrintTemplateProps {
  order: OrderDto;
  restaurantName: string;
  paymentMethodName?: string;
}

export const ReceiptPrintTemplate: React.FC<ReceiptPrintTemplateProps> = ({
  order,
  restaurantName,
  paymentMethodName,
}) => {
  const getOrderTypeText = (type?: OrderType) => {
    switch (type) {
      case OrderType.Indoor:
        return 'داخلي (طاولة)';
      case OrderType.Outdoor:
        return 'سفري (خارجي)';
      case OrderType.Delivery:
        return 'توصيل منزلي';
      default:
        return 'طلب عام';
    }
  };

  const resolvedPaymentMethod = paymentMethodName || order.cashDrawerEntries?.find(entry => entry.entryType === 1)?.paymentMethodName || 'غير محدد';
  const formattedDate = new Date(order.date).toLocaleString('ar-LY', {
    dateStyle: 'short',
    timeStyle: 'short',
  });

  return (
    <div
      className="receipt-print-area"
      style={{
        width: '78mm',
        maxWidth: '100%',
        margin: '0 auto',
        padding: '8px',
        backgroundColor: '#ffffff',
        color: '#000000',
        fontFamily: "'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif",
        fontSize: '12px',
        direction: 'rtl',
        textAlign: 'right',
        lineHeight: 1.4,
      }}
    >
      {/* Restaurant Header */}
      <div style={{ textAlign: 'center', marginBottom: '10px' }}>
        <h2 style={{ fontSize: '18px', fontWeight: 800, margin: '0 0 4px 0' }}>
          {restaurantName}
        </h2>
        <div style={{ fontSize: '11px', color: '#444' }}>فاتورة ضريبية مبسطة</div>
      </div>

      <div style={{ borderBottom: '1px dashed #000', margin: '8px 0' }} />

      {/* Order Info */}
      <div style={{ fontSize: '11.5px', display: 'flex', flexDirection: 'column', gap: '3px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>رقم الفاتورة / الطلب:</span>
          <span style={{ fontWeight: 800, fontSize: '13px' }}>#{order.orderNumber}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>التاريخ والوقت:</span>
          <span>{formattedDate}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>نوع الطلب:</span>
          <span style={{ fontWeight: 700 }}>{getOrderTypeText(order.orderType)}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>الكاشير:</span>
          <span>{order.userName || 'كاشير'}</span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>طريقة الدفع:</span>
          <span>{resolvedPaymentMethod}</span>
        </div>
      </div>

      <div style={{ borderBottom: '1px dashed #000', margin: '8px 0' }} />

      {/* Items Table */}
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '11px' }}>
        <thead>
          <tr style={{ borderBottom: '1px solid #000' }}>
            <th style={{ textAlign: 'right', padding: '4px 0' }}>الصنف</th>
            <th style={{ textAlign: 'center', padding: '4px 0' }}>الكمية</th>
            <th style={{ textAlign: 'left', padding: '4px 0' }}>الإجمالي</th>
          </tr>
        </thead>
        <tbody>
          {(order.orderItems || []).map((item, idx) => {
            const qty = item.quantity || 1;
            const price = item.unitSalePrice ?? (item.total ? item.total / qty : 0);
            const lineTotal = item.total ?? item.lineTotal ?? (qty * price);

            return (
              <React.Fragment key={idx}>
                <tr style={{ verticalAlign: 'top' }}>
                  <td style={{ padding: '4px 0', fontWeight: 600 }}>
                    {item.productName || (item as unknown as { product?: { name?: string } }).product?.name || 'صنف'}
                    {item.notes && item.notes !== 'لا يوجد ملاحظات' && (
                      <div style={{ fontSize: '9.5px', color: '#444', fontStyle: 'italic' }}>
                        ({item.notes})
                      </div>
                    )}
                  </td>
                  <td style={{ textAlign: 'center', padding: '4px 0' }}>
                    {qty} × {price.toFixed(2)}
                  </td>
                  <td style={{ textAlign: 'left', padding: '4px 0', fontWeight: 700 }}>
                    {lineTotal.toFixed(2)}
                  </td>
                </tr>
              </React.Fragment>
            );
          })}
        </tbody>
      </table>

      <div style={{ borderBottom: '1px dashed #000', margin: '8px 0' }} />

      {/* Totals */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '3px', fontSize: '12px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <span>المجموع الفرعي:</span>
          <span>{(((order.total || 0) + (order.discount || 0))).toFixed(2)} د.ل</span>
        </div>
        {(order.discount || 0) > 0 && (
          <div style={{ display: 'flex', justifyContent: 'space-between', color: '#dc2626' }}>
            <span>الخصم:</span>
            <span>-{(order.discount || 0).toFixed(2)} د.ل</span>
          </div>
        )}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            fontSize: '15px',
            fontWeight: 800,
            borderTop: '1px solid #000',
            paddingTop: '6px',
            marginTop: '4px',
          }}
        >
          <span>الصافي المطلوب:</span>
          <span>{(order.total || 0).toFixed(2)} د.ل</span>
        </div>
      </div>

      <div style={{ borderBottom: '1px dashed #000', margin: '10px 0' }} />

      {/* Footer */}
      <div style={{ textAlign: 'center', fontSize: '11px', marginTop: '6px' }}>
        <div style={{ fontWeight: 700 }}>شكراً لزيارتكم ونتمنى لكم وجبة شهية!</div>
        <div style={{ fontSize: '9.5px', color: '#666', marginTop: '3px' }}>يرجى الاحتفاظ بالفاتورة لخدمتكم</div>
      </div>
    </div>
  );
};

export default ReceiptPrintTemplate;
