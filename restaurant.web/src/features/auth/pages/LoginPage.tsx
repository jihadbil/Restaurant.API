import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UtensilsCrossed, Lock, User, AlertCircle } from 'lucide-react';
import { useAuth } from '../../../context/AuthContext';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import toast from 'react-hot-toast';

export const LoginPage: React.FC = () => {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userName.trim() || !password.trim()) {
      setError('يرجى إدخال اسم المستخدم وكلمة المرور.');
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      const response = await login({ userName, password });
      if (response.isSuccess) {
        toast.success(`مرحباً بك مجدداً، ${response.user?.userName || userName}`);
        const isAdmin = response.user?.roles?.includes('Admin') || response.user?.roles?.includes('مدير');
        if (isAdmin) {
          navigate('/dashboard');
        } else {
          navigate('/pos');
        }
      } else {
        setError(response.message || 'فشل تسجيل الدخول. تحقق من صحة البيانات.');
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('حدث خطأ غير متوقع أثناء تسجيل الدخول.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'var(--color-background)',
        padding: '20px',
      }}
    >
      <div
        style={{
          width: '100%',
          maxWidth: '440px',
          backgroundColor: 'var(--color-surface)',
          borderRadius: 'var(--radius-xl)',
          padding: '38px 32px',
          boxShadow: 'var(--shadow-xl)',
          border: '1px solid var(--color-border)',
        }}
      >
        {/* Brand Header */}
        <div style={{ textAlign: 'center', marginBottom: '30px' }}>
          <div
            style={{
              width: '68px',
              height: '68px',
              borderRadius: '20px',
              backgroundColor: 'var(--color-primary-bg)',
              color: 'var(--color-primary)',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '16px',
              boxShadow: '0 8px 16px rgba(56, 224, 123, 0.15)',
            }}
          >
            <UtensilsCrossed size={36} />
          </div>
          <h1 style={{ fontSize: '1.625rem', fontWeight: 800, marginBottom: '6px' }}>
            Emerald POS
          </h1>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem' }}>
            نظام إدارة المطاعم ونقاط البيع السريعة
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              padding: '12px 14px',
              backgroundColor: 'var(--color-danger-bg)',
              border: '1px solid var(--color-danger-border)',
              borderRadius: 'var(--radius-md)',
              color: 'var(--color-danger-text)',
              fontSize: '0.875rem',
              fontWeight: 500,
              marginBottom: '20px',
            }}
          >
            <AlertCircle size={20} style={{ flexShrink: 0 }} />
            <span>{error}</span>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
          <Input
            label="اسم المستخدم"
            placeholder="أدخل اسم المستخدم..."
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            icon={<User size={18} />}
            autoFocus
            required
          />

          <Input
            label="كلمة المرور"
            type="password"
            placeholder="أدخل كلمة المرور..."
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            icon={<Lock size={18} />}
            required
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            isLoading={isSubmitting}
            style={{ marginTop: '8px', width: '100%' }}
          >
            تسجيل الدخول للنظام
          </Button>
        </form>

        {/* Footer Note */}
        <div style={{ textAlign: 'center', marginTop: '28px', borderTop: '1px solid var(--color-border)', paddingTop: '16px' }}>
          <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)' }}>
            نظام المطاعم الاحترافي v2.0 • جميع الحقوق محفوظة
          </span>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
