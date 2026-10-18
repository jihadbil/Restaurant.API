import React, { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  UtensilsCrossed,
  Receipt,
  Package,
  Layers,
  Wallet,
  BarChart3,
  LayoutDashboard,
  Settings,
  LogOut,
  ChevronRight,
  ChevronLeft,
  CircleDollarSign,
  Clock,
  User as UserIcon,
} from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import ShiftCloseModal from '../../features/treasury/components/ShiftCloseModal';

interface NavItem {
  to: string;
  label: string;
  hotkey: string;
  icon: React.ReactNode;
  permission?: string;
  adminOnly?: boolean;
}

export const MainLayout: React.FC = () => {
  const [isCollapsed, setIsCollapsed] = useState<boolean>(() => {
    return localStorage.getItem('sidebar_collapsed') === 'true';
  });
  const [isShiftModalOpen, setIsShiftModalOpen] = useState<boolean>(false);

  const { user, logout, hasPermission, isAdmin } = useAuth();
  const canCloseShift = isAdmin || hasPermission('Permission.NewOrder') || hasPermission('Permission.Treasury');
  const location = useLocation();
  const navigate = useNavigate();

  // Toggle sidebar
  const toggleSidebar = () => {
    setIsCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem('sidebar_collapsed', String(next));
      return next;
    });
  };

  // Global hotkeys (F1 - F12)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger hotkeys if typing in input/textarea
      const target = e.target as HTMLElement;
      if (['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
        if (e.key === 'F12' && canCloseShift) {
          e.preventDefault();
          setIsShiftModalOpen(true);
        }
        return;
      }

      switch (e.key) {
        case 'F1':
          e.preventDefault();
          navigate('/pos');
          break;
        case 'F2':
          e.preventDefault();
          navigate('/orders');
          break;
        case 'F3':
          e.preventDefault();
          navigate('/products');
          break;
        case 'F4':
          e.preventDefault();
          navigate('/treasury');
          break;
        case 'F5':
          e.preventDefault();
          navigate('/reports');
          break;
        case 'F12':
          e.preventDefault();
          if (canCloseShift) setIsShiftModalOpen(true);
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [navigate, canCloseShift]);

  const navItems: NavItem[] = [
    {
      to: '/pos',
      label: 'نقطة البيع (طلب جديد)',
      hotkey: 'F1',
      icon: <UtensilsCrossed size={20} />,
      permission: 'Permission.NewOrder',
    },
    {
      to: '/orders',
      label: 'إدارة الطلبات والفواتير',
      hotkey: 'F2',
      icon: <Receipt size={20} />,
      permission: 'Permission.Orders',
    },
    {
      to: '/products',
      label: 'كتالوج المنتجات',
      hotkey: 'F3',
      icon: <Package size={20} />,
      permission: 'Permission.Products',
    },
    {
      to: '/categories',
      label: 'التصنيفات والمحطات',
      hotkey: '',
      icon: <Layers size={20} />,
      permission: 'Permission.Categories',
    },
    {
      to: '/treasury',
      label: 'حركات الخزينة',
      hotkey: 'F4',
      icon: <Wallet size={20} />,
      permission: 'Permission.Treasury',
    },
    {
      to: '/reports',
      label: 'التقارير التحليلية',
      hotkey: 'F5',
      icon: <BarChart3 size={20} />,
      permission: 'Permission.Reports',
    },
    {
      to: '/dashboard',
      label: 'لوحة القيادة والمؤشرات',
      hotkey: '',
      icon: <LayoutDashboard size={20} />,
      permission: 'Permission.Dashboard',
    },
    {
      to: '/settings',
      label: 'إعدادات النظام',
      hotkey: '',
      icon: <Settings size={20} />,
      permission: 'Permission.Settings',
      adminOnly: true,
    },
  ];

  // Filter items based on user permissions
  const visibleNavItems = navItems.filter((item) => {
    if (isAdmin) return true;
    if (item.adminOnly && !isAdmin) return false;
    if (item.permission && !hasPermission(item.permission)) return false;
    return true;
  });

  const getPageTitle = (): string => {
    const current = navItems.find((item) => location.pathname.startsWith(item.to));
    return current ? current.label : 'نظام إدارة المطاعم';
  };

  return (
    <div style={{ display: 'flex', minHeight: '100vh', width: '100%', backgroundColor: 'var(--color-background)' }}>
      {/* Sidebar (Right side in RTL) */}
      <aside
        style={{
          width: isCollapsed ? 'var(--sidebar-collapsed-width)' : 'var(--sidebar-width)',
          backgroundColor: 'var(--color-surface)',
          borderLeft: '1px solid var(--color-border)',
          display: 'flex',
          flexDirection: 'column',
          transition: 'width var(--transition-normal)',
          position: 'sticky',
          top: 0,
          height: '100vh',
          zIndex: 100,
          flexShrink: 0,
        }}
      >
        {/* Brand Logo & Collapse Button */}
        <div
          style={{
            height: 'var(--header-height)',
            padding: '0 16px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: isCollapsed ? 'center' : 'space-between',
            borderBottom: '1px solid var(--color-border)',
          }}
        >
          {!isCollapsed && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
              <div
                style={{
                  width: '38px',
                  height: '38px',
                  borderRadius: '10px',
                  backgroundColor: 'var(--color-primary-bg)',
                  color: 'var(--color-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  boxShadow: 'var(--shadow-sm)',
                }}
              >
                <UtensilsCrossed size={22} />
              </div>
              <div>
                <h2 style={{ fontSize: '1.0625rem', fontWeight: 800, margin: 0, lineHeight: 1.2 }}>Emerald POS</h2>
                <span style={{ fontSize: '0.6875rem', color: 'var(--color-text-muted)' }}>إدارة المطاعم v2.0</span>
              </div>
            </div>
          )}
          {isCollapsed && (
            <div
              style={{
                width: '38px',
                height: '38px',
                borderRadius: '10px',
                backgroundColor: 'var(--color-primary-bg)',
                color: 'var(--color-primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <UtensilsCrossed size={20} />
            </div>
          )}
          <button
            onClick={toggleSidebar}
            title={isCollapsed ? 'توسيع القائمة' : 'طي القائمة'}
            style={{
              padding: '6px',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--color-text-muted)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {isCollapsed ? <ChevronLeft size={18} /> : <ChevronRight size={18} />}
          </button>
        </div>

        {/* Navigation Menu */}
        <nav
          style={{
            flex: 1,
            padding: '16px 10px',
            display: 'flex',
            flexDirection: 'column',
            gap: '6px',
            overflowY: 'auto',
          }}
        >
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              title={isCollapsed ? item.label : undefined}
              style={({ isActive }) => ({
                display: 'flex',
                alignItems: 'center',
                justifyContent: isCollapsed ? 'center' : 'space-between',
                padding: isCollapsed ? '12px' : '10px 14px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: isActive ? 'var(--color-primary-bg)' : 'transparent',
                color: isActive ? 'var(--color-success-text)' : 'var(--color-text-secondary)',
                fontWeight: isActive ? 700 : 500,
                fontSize: '0.9375rem',
                transition: 'all var(--transition-fast)',
                borderRight: isActive ? '3.5px solid var(--color-primary)' : '3.5px solid transparent',
              })}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <span style={{ display: 'flex' }}>{item.icon}</span>
                {!isCollapsed && <span>{item.label}</span>}
              </div>
              {!isCollapsed && item.hotkey && (
                <span
                  style={{
                    fontSize: '0.6875rem',
                    padding: '2px 6px',
                    borderRadius: '4px',
                    backgroundColor: 'var(--color-surface-active)',
                    color: 'var(--color-text-muted)',
                    fontWeight: 600,
                  }}
                >
                  {item.hotkey}
                </span>
              )}
            </NavLink>
          ))}
        </nav>

        {/* User Card & Logout in Sidebar footer */}
        <div
          style={{
            padding: '12px',
            borderTop: '1px solid var(--color-border)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: isCollapsed ? 'center' : 'space-between',
          }}
        >
          {!isCollapsed && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', overflow: 'hidden' }}>
              <div
                style={{
                  width: '36px',
                  height: '36px',
                  borderRadius: '50%',
                  backgroundColor: 'var(--color-primary-light)',
                  color: 'var(--color-success-text)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontWeight: 700,
                  flexShrink: 0,
                }}
              >
                <UserIcon size={18} />
              </div>
              <div style={{ overflow: 'hidden' }}>
                <div
                  style={{
                    fontSize: '0.875rem',
                    fontWeight: 700,
                    whiteSpace: 'nowrap',
                    textOverflow: 'ellipsis',
                    overflow: 'hidden',
                  }}
                >
                  {user?.userName || 'مستخدم'}
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
                  {isAdmin ? 'مدير النظام' : 'كاشير'}
                </div>
              </div>
            </div>
          )}
          <button
            onClick={logout}
            title="تسجيل الخروج"
            style={{
              padding: '8px',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--color-danger)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <LogOut size={18} />
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        {/* Top Header */}
        <header
          style={{
            height: 'var(--header-height)',
            backgroundColor: 'var(--color-surface)',
            borderBottom: '1px solid var(--color-border)',
            padding: '0 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            position: 'sticky',
            top: 0,
            zIndex: 90,
          }}
        >
          {/* Current Page Title */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <h1 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>{getPageTitle()}</h1>
          </div>

          {/* Quick Header Actions */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
            {/* Cashbox Status Badge */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '6px 14px',
                borderRadius: 'var(--radius-full)',
                backgroundColor: 'var(--color-primary-bg)',
                color: 'var(--color-success-text)',
                fontSize: '0.8125rem',
                fontWeight: 600,
              }}
            >
              <CircleDollarSign size={16} />
              <span>نقدية نقطة البيع تُسجل في درجها</span>
            </div>

            {/* Quick Shift Close Button (F12) */}
            <button
              onClick={() => setIsShiftModalOpen(true)}
              disabled={!canCloseShift}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '8px 16px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-warning-bg)',
                border: '1px solid var(--color-warning-border)',
                color: 'var(--color-warning-text)',
                fontSize: '0.875rem',
                fontWeight: 700,
                cursor: 'pointer',
                transition: 'all var(--transition-fast)',
              }}
            >
              <Clock size={16} />
              <span>تقفيل الوردية</span>
              <span
                style={{
                  fontSize: '0.6875rem',
                  padding: '1px 5px',
                  borderRadius: '3px',
                  backgroundColor: 'rgba(217, 119, 6, 0.15)',
                }}
              >
                F12
              </span>
            </button>
          </div>
        </header>

        {/* Page Content View */}
        <main style={{ flex: 1, padding: '24px', overflowY: 'auto' }}>
          <Outlet />
        </main>
      </div>

      {/* Global Shift Close Modal (Triggered by Header or F12) */}
      <ShiftCloseModal
        isOpen={isShiftModalOpen}
        onClose={() => setIsShiftModalOpen(false)}
      />
    </div>
  );
};

export default MainLayout;
