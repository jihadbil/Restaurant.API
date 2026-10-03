import React from 'react';
import type { OrderDto } from '../../types/api';
import { OrderType } from '../../types/api';

interface KitchenTicketPrintTemplateProps {
  order: OrderDto;
  stationName?: string;
}

export const KitchenTicketPrintTemplate: React.FC<KitchenTicketPrintTemplateProps> = ({
  order,
  stationName = 'محطة المطبخ والتحضير',
}) => {
  const getOrderTypeBadge = (type?: OrderType) => {
    switch (type) {
      case OrderType.Indoor:
        return { text: 'محلي (داخل المطعم)', bg: '#dcfce7', border: '#86efac', color: '#15803d' };
      case OrderType.Outdoor:
        return { text: 'سفري (استلام خارجي)', bg: '#fef9c3', border: '#fde047', color: '#a16207' };
      case OrderType.Delivery:
        return { text: 'توصيل منزلي', bg: '#fee2e2', border: '#fca5a5', color: '#b91c1c' };
      default:
        return { text: 'طلب عام', bg: '#f3f4f6', border: '#e5e7eb', color: '#374151' };
    }
  };

  const badge = getOrderTypeBadge(order.orderType);
  const timeStr = new Date(order.date || Date.now()).toLocaleTimeString('ar-LY', {
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div
      className="kitchen-ticket-print-area"
      style={{
        width: '78mm',
        maxWidth: '100%',
        margin: '0 auto',
        padding: '10px',
        backgroundColor: '#ffffff',
        color: '#000000',
        fontFamily: "'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif",
        direction: 'rtl',
        textAlign: 'right',
        lineHeight: 1.35,
      }}
    >
      {/* Station Header */}
      <div style={{ textAlign: 'center', marginBottom: '6px' }}>
        <h2 style={{ fontSize: '18px', fontWeight: 800, margin: 0 }}>
          {stationName}
        </h2>
      </div>

      <div style={{ borderBottom: '3px solid #000', margin: '6px 0' }} />

      {/* Huge Order Number */}
      <div style={{ textAlign: 'center', margin: '6px 0' }}>
        <div style={{ fontSize: '13px', fontWeight: 600 }}>رقم الطلب</div>
        <div style={{ fontSize: '42px', fontWeight: 900, lineHeight: 1.1 }}>
          #{order.orderNumber}
        </div>
      </div>

      {/* Order Type Badge & Time */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          margin: '8px 0',
        }}
      >
        <span
          style={{
            padding: '4px 10px',
            borderRadius: '6px',
            backgroundColor: badge.bg,
            border: `1.5px solid ${badge.border}`,
            color: badge.color,
            fontSize: '14px',
            fontWeight: 800,
          }}
        >
          {badge.text}
        </span>
        <span style={{ fontSize: '13px', fontWeight: 700 }}>
          الوقت: {timeStr}
        </span>
      </div>

      <div style={{ borderBottom: '2px solid #000', margin: '8px 0' }} />

      {/* Items List (Large & High Legibility) */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        {order.orderItems.map((item, idx) => (
          <div
            key={idx}
            style={{
              borderBottom: '1px dotted #888',
              paddingBottom: '6px',
            }}
          >
            <div
              style={{
                display: 'flex',
                alignItems: 'baseline',
                justifyContent: 'space-between',
                fontSize: '18px',
                fontWeight: 800,
              }}
            >
              <span>{item.productName}</span>
              <span style={{ fontSize: '20px', fontWeight: 900 }}>
                × {item.quantity}
              </span>
            </div>

            {/* Item Customization / Notes */}
            {item.notes && item.notes !== 'لا يوجد ملاحظات' && (
              <div
                style={{
                  fontSize: '13px',
                  fontWeight: 700,
                  color: '#dc2626',
                  fontStyle: 'italic',
                  marginTop: '2px',
                }}
              >
                * {item.notes}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* General Order Notes */}
      {order.notes && order.notes !== 'لا يوجد ملاحظات' && (
        <div
          style={{
            marginTop: '12px',
            padding: '8px',
            backgroundColor: '#fef3c7',
            border: '1.5px solid #f59e0b',
            borderRadius: '6px',
            fontSize: '13px',
            fontWeight: 700,
            color: '#b45309',
          }}
        >
          ملاحظة عامة: {order.notes}
        </div>
      )}

      <div style={{ borderBottom: '2px solid #000', margin: '10px 0' }} />
    </div>
  );
};

export default KitchenTicketPrintTemplate;
