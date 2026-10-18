import React, { type ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import LoadingSpinner from '../ui/LoadingSpinner';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredPermission?: string;
  requireAdmin?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredPermission,
  requireAdmin = false,
}) => {
  const { isAuthenticated, isLoading, hasPermission, isAdmin } = useAuth();

  if (isLoading) {
    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: 'var(--color-background)',
        }}
      >
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requireAdmin && !isAdmin) {
    return <Navigate to="/pos" replace />;
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return (
      <div
        style={{
          padding: '40px 20px',
          textAlign: 'center',
          backgroundColor: 'var(--color-surface)',
          borderRadius: 'var(--radius-lg)',
          margin: '40px auto',
          maxWidth: '500px',
          boxShadow: 'var(--shadow-md)',
        }}
      >
        <h2 style={{ color: 'var(--color-danger)', marginBottom: '10px' }}>غير مصرح لك بالوصول</h2>
        <p style={{ color: 'var(--color-text-muted)', marginBottom: '20px' }}>
          لا تمتلك الصلاحيات الكافية لعرض هذه الشاشة. يرجى مراجعة مدير النظام.
        </p>
      </div>
    );
  }

  return <>{children}</>;
};

export default ProtectedRoute;
