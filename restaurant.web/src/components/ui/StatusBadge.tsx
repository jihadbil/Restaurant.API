import React from 'react';
import { OrderStatus } from '../../types/api';

interface StatusBadgeProps {
  status: OrderStatus | number;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const getBadgeConfig = () => {
    switch (status) {
      case OrderStatus.Preparing:
        return {
          text: 'قيد التحضير',
          bg: 'var(--color-warning-bg)',
          border: 'var(--color-warning-border)',
          color: 'var(--color-warning-text)',
          dot: 'var(--color-warning)',
        };
      case OrderStatus.Ready:
        return {
          text: 'جاهزة للتسليم',
          bg: 'var(--color-info-bg)',
          border: 'var(--color-info-border)',
          color: 'var(--color-info-text)',
          dot: 'var(--color-info)',
        };
      case OrderStatus.Delivered:
        return {
          text: 'تم التسليم',
          bg: 'var(--color-success-bg)',
          border: 'var(--color-success-border)',
          color: 'var(--color-success-text)',
          dot: 'var(--color-success)',
        };
      case OrderStatus.Cancelled:
        return {
          text: 'ملغاة',
          bg: 'var(--color-danger-bg)',
          border: 'var(--color-danger-border)',
          color: 'var(--color-danger-text)',
          dot: 'var(--color-danger)',
        };
      default:
        return {
          text: 'غير محدد',
          bg: 'var(--color-surface-active)',
          border: 'var(--color-border)',
          color: 'var(--color-text-muted)',
          dot: 'var(--color-text-muted)',
        };
    }
  };

  const config = getBadgeConfig();

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '6px',
        padding: '3px 10px',
        borderRadius: 'var(--radius-full)',
        fontSize: '0.8125rem',
        fontWeight: 600,
        backgroundColor: config.bg,
        border: `1px solid ${config.border}`,
        color: config.color,
        width: 'fit-content',
        whiteSpace: 'nowrap',
      }}
    >
      <span
        style={{
          width: '7px',
          height: '7px',
          borderRadius: '50%',
          backgroundColor: config.dot,
        }}
      />
      {config.text}
    </span>
  );
};

export default StatusBadge;
