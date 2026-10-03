import React, { type ButtonHTMLAttributes } from 'react';
import LoadingSpinner from './LoadingSpinner';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'warning' | 'ghost' | 'success';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  icon?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  icon,
  disabled,
  style,
  ...props
}) => {
  const getVariantStyles = (): React.CSSProperties => {
    switch (variant) {
      case 'primary':
        return {
          backgroundColor: 'var(--color-primary)',
          color: '#ffffff',
          fontWeight: 700,
          boxShadow: 'var(--shadow-sm)',
        };
      case 'secondary':
        return {
          backgroundColor: 'var(--color-surface)',
          color: 'var(--color-text-primary)',
          border: '1px solid var(--color-border)',
        };
      case 'danger':
        return {
          backgroundColor: 'var(--color-danger)',
          color: '#ffffff',
          fontWeight: 600,
        };
      case 'warning':
        return {
          backgroundColor: 'var(--color-warning)',
          color: '#ffffff',
          fontWeight: 600,
        };
      case 'success':
        return {
          backgroundColor: 'var(--color-success)',
          color: '#ffffff',
          fontWeight: 600,
        };
      case 'ghost':
        return {
          backgroundColor: 'transparent',
          color: 'var(--color-text-secondary)',
        };
    }
  };

  const getSizeStyles = (): React.CSSProperties => {
    switch (size) {
      case 'sm':
        return {
          padding: '6px 12px',
          fontSize: '0.8125rem',
          borderRadius: 'var(--radius-sm)',
          gap: '6px',
        };
      case 'lg':
        return {
          padding: '14px 28px',
          fontSize: '1.0625rem',
          borderRadius: 'var(--radius-lg)',
          gap: '10px',
        };
      case 'md':
      default:
        return {
          padding: '10px 18px',
          fontSize: '0.9375rem',
          borderRadius: 'var(--radius-md)',
          gap: '8px',
        };
    }
  };

  return (
    <button
      disabled={disabled || isLoading}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        cursor: disabled || isLoading ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        transition: 'all var(--transition-fast)',
        fontFamily: 'inherit',
        border: 'none',
        ...getVariantStyles(),
        ...getSizeStyles(),
        ...style,
      }}
      {...props}
    >
      {isLoading ? (
        <LoadingSpinner size={size === 'lg' ? 'md' : 'sm'} />
      ) : (
        icon && <span style={{ display: 'inline-flex' }}>{icon}</span>
      )}
      {children}
    </button>
  );
};

export default Button;
