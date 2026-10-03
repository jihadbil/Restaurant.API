import React from 'react';
import Modal from './Modal';
import Button from './Button';
import { AlertTriangle, AlertCircle } from 'lucide-react';

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'primary';
  isLoading?: boolean;
}

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'تأكيد',
  cancelText = 'إلغاء',
  variant = 'danger',
  isLoading = false,
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onClose} maxWidth="420px">
      <div style={{ textAlign: 'center', padding: '10px 0' }}>
        <div
          style={{
            width: '56px',
            height: '56px',
            borderRadius: '50%',
            backgroundColor: variant === 'danger' ? 'var(--color-danger-bg)' : 'var(--color-warning-bg)',
            color: variant === 'danger' ? 'var(--color-danger)' : 'var(--color-warning-text)',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: '16px',
          }}
        >
          {variant === 'danger' ? <AlertCircle size={30} /> : <AlertTriangle size={30} />}
        </div>
        <h3 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '8px' }}>
          {title}
        </h3>
        <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.9375rem', lineHeight: 1.6 }}>
          {message}
        </p>
        <div
          style={{
            display: 'flex',
            gap: '12px',
            justifyContent: 'center',
            marginTop: '26px',
          }}
        >
          <Button
            variant={variant === 'danger' ? 'danger' : 'primary'}
            onClick={onConfirm}
            isLoading={isLoading}
            style={{ minWidth: '120px' }}
          >
            {confirmText}
          </Button>
          <Button
            variant="secondary"
            onClick={onClose}
            disabled={isLoading}
            style={{ minWidth: '100px' }}
          >
            {cancelText}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ConfirmDialog;
