import React, { type InputHTMLAttributes } from 'react';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

export const Input: React.FC<InputProps> = ({
  label,
  error,
  icon,
  style,
  id,
  ...props
}) => {
  const inputId = id || (label ? `input-${label.replace(/\s+/g, '-').toLowerCase()}` : undefined);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', width: '100%' }}>
      {label && (
        <label
          htmlFor={inputId}
          style={{
            fontSize: '0.875rem',
            fontWeight: 600,
            color: 'var(--color-text-primary)',
          }}
        >
          {label}
        </label>
      )}
      <div
        style={{
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          width: '100%',
        }}
      >
        {icon && (
          <span
            style={{
              position: 'absolute',
              right: '12px',
              display: 'flex',
              alignItems: 'center',
              pointerEvents: 'none',
              color: 'var(--color-text-muted)',
            }}
          >
            {icon}
          </span>
        )}
        <input
          id={inputId}
          style={{
            width: '100%',
            padding: icon ? '10px 42px 10px 14px' : '10px 14px',
            fontSize: '0.9375rem',
            borderRadius: 'var(--radius-md)',
            border: `1.5px solid ${error ? 'var(--color-danger)' : 'var(--color-border)'}`,
            backgroundColor: 'var(--color-surface)',
            color: 'var(--color-text-primary)',
            transition: 'border-color var(--transition-fast)',
            ...style,
          }}
          {...props}
        />
      </div>
      {error && (
        <span
          style={{
            fontSize: '0.8125rem',
            color: 'var(--color-danger)',
            fontWeight: 500,
          }}
        >
          {error}
        </span>
      )}
    </div>
  );
};

export default Input;
