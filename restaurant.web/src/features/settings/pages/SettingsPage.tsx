import React, { useState, useEffect, useCallback } from 'react';
import {
  Printer,
  Sliders,
  DollarSign,
  PlusCircle,
  CreditCard,
  Users,
  Store,
  Edit2,
  Trash2,
  Plus,
  Key,
  Shield,
  Upload,
  RotateCcw,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import Modal from '../../../components/ui/Modal';
import ConfirmDialog from '../../../components/ui/ConfirmDialog';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import apiClient from '../../../services/apiClient';
import type {
  PrintStationDto,
  PrinterDto,
  CashboxDto,
  CashboxBalanceDto,
  AddonDto,
  PaymentMethodDto,
  UserDto,
  RestaurantDto,
} from '../../../types/api';
import { PrinterType } from '../../../types/api';
import { getFullImageUrl } from '../../../utils/imageUtils';
import toast from 'react-hot-toast';

type SettingsTab =
  | 'stations'
  | 'printers'
  | 'cashboxes'
  | 'addons'
  | 'paymentMethods'
  | 'users'
  | 'restaurant';

export const SettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<SettingsTab>('stations');
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Tab 1: Stations State
  const [stations, setStations] = useState<PrintStationDto[]>([]);
  const [stationModalOpen, setStationModalOpen] = useState<boolean>(false);
  const [editingStation, setEditingStation] = useState<PrintStationDto | null>(null);
  const [stationName, setStationName] = useState<string>('');

  // Tab 2: Printers State
  const [printers, setPrinters] = useState<PrinterDto[]>([]);
  const [printerModalOpen, setPrinterModalOpen] = useState<boolean>(false);
  const [editingPrinter, setEditingPrinter] = useState<PrinterDto | null>(null);
  const [printerForm, setPrinterForm] = useState<{
    name: string;
    printerName: string;
    printerType: PrinterType;
    printStationId: number;
  }>({
    name: '',
    printerName: '',
    printerType: PrinterType.Receipt,
    printStationId: 0,
  });

  // Tab 3: Cashboxes State
  const [cashboxes, setCashboxes] = useState<CashboxDto[]>([]);
  const [cashboxModalOpen, setCashboxModalOpen] = useState<boolean>(false);
  const [editingCashbox, setEditingCashbox] = useState<CashboxDto | null>(null);
  const [cashboxForm, setCashboxForm] = useState({
    name: '',
    description: '',
    initialBalance: '0',
    isActive: true,
  });

  // Tab 4: Addons State
  const [addons, setAddons] = useState<AddonDto[]>([]);
  const [addonModalOpen, setAddonModalOpen] = useState<boolean>(false);
  const [editingAddon, setEditingAddon] = useState<AddonDto | null>(null);
  const [addonName, setAddonName] = useState<string>('');

  // Tab 5: Payment Methods State
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethodDto[]>([]);
  const [pmModalOpen, setPmModalOpen] = useState<boolean>(false);
  const [editingPm, setEditingPm] = useState<PaymentMethodDto | null>(null);
  const [pmForm, setPmForm] = useState({
    name: '',
    isTaxFree: false,
    logoUrl: '',
  });

  // Tab 6: Users & Permissions State
  const [users, setUsers] = useState<UserDto[]>([]);
  const [userModalOpen, setUserModalOpen] = useState<boolean>(false);
  const [passwordModalOpen, setPasswordModalOpen] = useState<boolean>(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [newPassword, setNewPassword] = useState<string>('');
  const [userForm, setUserForm] = useState({
    userName: '',
    email: '',
    password: '',
    phoneNumber: '',
    roles: ['Cashier'],
    permissions: [] as string[],
  });

  // Tab 7: Restaurant Info State
  const [restaurant, setRestaurant] = useState<RestaurantDto | null>(null);
  const [restForm, setRestForm] = useState({
    name: '',
    address: '',
    phoneNumber: '',
    taxNumber: '',
    logoUrl: '',
  });
  const [isSavingRest, setIsSavingRest] = useState<boolean>(false);

  // Confirm delete dialog state
  const [confirmDialog, setConfirmDialog] = useState<{
    isOpen: boolean;
    title: string;
    message: string;
    onConfirm: () => Promise<void>;
  }>({
    isOpen: false,
    title: '',
    message: '',
    onConfirm: async () => {},
  });
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  // Load active tab data
  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      if (activeTab === 'stations') {
        const res = await apiClient.get<PrintStationDto[]>('/api/printstations');
        setStations(res || []);
      } else if (activeTab === 'printers') {
        const [printersRes, stationsRes] = await Promise.all([
          apiClient.get<PrinterDto[]>('/api/printers').catch(() => []),
          apiClient.get<PrintStationDto[]>('/api/printstations').catch(() => []),
        ]);
        setPrinters(printersRes || []);
        setStations(stationsRes || []);
      } else if (activeTab === 'cashboxes') {
        const res = await apiClient.get<CashboxDto[]>('/api/cashboxes');
        const enriched = await Promise.all(
          (res || []).map(async (cb) => {
            try {
              const b = await apiClient.get<CashboxBalanceDto>(`/api/cashboxes/${cb.id}/balance`);
              return { ...cb, currentBalance: b.currentBalance };
            } catch {
              return { ...cb, currentBalance: cb.initialBalance ?? 0 };
            }
          })
        );
        setCashboxes(enriched);
      } else if (activeTab === 'addons') {
        const res = await apiClient.get<AddonDto[]>('/api/addons');
        setAddons(res || []);
      } else if (activeTab === 'paymentMethods') {
        const res = await apiClient.get<PaymentMethodDto[]>('/api/paymentmethods');
        setPaymentMethods(res || []);
      } else if (activeTab === 'users') {
        const res = await apiClient.get<UserDto[]>('/api/users');
        setUsers(res || []);
      } else if (activeTab === 'restaurant') {
        const res = await apiClient.get<RestaurantDto[]>('/api/restaurants');
        if (res && res.length > 0) {
          const r = res[0];
          setRestaurant(r);
          setRestForm({
            name: r.name || '',
            address: r.address || '',
            phoneNumber: r.phoneNumber || '',
            taxNumber: r.taxNumber || '',
            logoUrl: r.logoUrl || '',
          });
        }
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل تحميل بيانات الإعدادات';
      toast.error(msg);
    } finally {
      setIsLoading(false);
    }
  }, [activeTab]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Tab definitions
  const tabs = [
    { id: 'stations' as SettingsTab, label: 'محطات الطباعة', icon: Sliders },
    { id: 'printers' as SettingsTab, label: 'الطابعات والأجهزة', icon: Printer },
    { id: 'cashboxes' as SettingsTab, label: 'الخزائن ونقاط النقدية', icon: DollarSign },
    { id: 'addons' as SettingsTab, label: 'الإضافات والمكونات', icon: PlusCircle },
    { id: 'paymentMethods' as SettingsTab, label: 'وسائل الدفع', icon: CreditCard },
    { id: 'users' as SettingsTab, label: 'المستخدمون والصلاحيات', icon: Users },
    { id: 'restaurant' as SettingsTab, label: 'بيانات المطعم والشعار', icon: Store },
  ];

  // ==========================================
  // Tab 1: Stations Handlers
  // ==========================================
  const handleSaveStation = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stationName.trim()) {
      toast.error('يرجى إدخال اسم محطة الطباعة');
      return;
    }
    try {
      if (editingStation) {
        await apiClient.put(`/api/printstations/${editingStation.id}`, {
          id: editingStation.id,
          name: stationName.trim(),
        });
        toast.success('تم تعديل محطة الطباعة بنجاح');
      } else {
        await apiClient.post('/api/printstations', { name: stationName.trim() });
        toast.success('تمت إضافة محطة الطباعة بنجاح');
      }
      setStationModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ محطة الطباعة';
      toast.error(msg);
    }
  };

  const handleDeleteStation = (station: PrintStationDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف محطة الطباعة',
      message: `هل أنت متأكد من حذف المحطة "${station.name}"؟`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/printstations/${station.id}`);
          toast.success('تم حذف محطة الطباعة');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف محطة الطباعة';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 2: Printers Handlers
  // ==========================================
  const handleSavePrinter = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!printerForm.name.trim() || !printerForm.printerName.trim() || !printerForm.printStationId) {
      toast.error('يرجى ملء جميع الحقول المطلوبة واختيار محطة الطباعة');
      return;
    }
    try {
      if (editingPrinter) {
        await apiClient.put(`/api/printers/${editingPrinter.id}`, {
          id: editingPrinter.id,
          name: printerForm.name.trim(),
          printerName: printerForm.printerName.trim(),
          printerType: Number(printerForm.printerType),
          printStationId: Number(printerForm.printStationId),
        });
        toast.success('تم تعديل بيانات الطابعة');
      } else {
        await apiClient.post('/api/printers', {
          name: printerForm.name.trim(),
          printerName: printerForm.printerName.trim(),
          printerType: Number(printerForm.printerType),
          printStationId: Number(printerForm.printStationId),
        });
        toast.success('تمت إضافة الطابعة بنجاح');
      }
      setPrinterModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ الطابعة';
      toast.error(msg);
    }
  };

  const handleDeletePrinter = (printer: PrinterDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف الطابعة',
      message: `هل أنت متأكد من حذف الطابعة "${printer.name}"؟`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/printers/${printer.id}`);
          toast.success('تم حذف الطابعة');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف الطابعة';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 3: Cashboxes Handlers
  // ==========================================
  const handleSaveCashbox = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cashboxForm.name.trim()) {
      toast.error('اسم الخزينة مطلوب');
      return;
    }
    try {
      if (editingCashbox) {
        await apiClient.put(`/api/cashboxes/${editingCashbox.id}`, {
          id: editingCashbox.id,
          name: cashboxForm.name.trim(),
          description: cashboxForm.description.trim(),
          isActive: cashboxForm.isActive,
        });
        toast.success('تم تعديل بيانات الخزينة');
      } else {
        await apiClient.post('/api/cashboxes', {
          name: cashboxForm.name.trim(),
          description: cashboxForm.description.trim(),
          initialBalance: parseFloat(cashboxForm.initialBalance) || 0,
          isActive: cashboxForm.isActive,
        });
        toast.success('تمت إضافة الخزينة بنجاح');
      }
      setCashboxModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ الخزينة';
      toast.error(msg);
    }
  };

  const handleDeleteCashbox = (cb: CashboxDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف الخزينة',
      message: `هل تريد بالتأكيد حذف الخزينة "${cb.name}"؟ يجب ألا تحتوي على حركات مالية.`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/cashboxes/${cb.id}`);
          toast.success('تم حذف الخزينة');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف الخزينة';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 4: Addons Handlers
  // ==========================================
  const handleSaveAddon = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!addonName.trim()) {
      toast.error('اسم الإضافة مطلوب');
      return;
    }
    try {
      if (editingAddon) {
        await apiClient.put(`/api/addons/${editingAddon.id}`, {
          id: editingAddon.id,
          name: addonName.trim(),
        });
        toast.success('تم تعديل الإضافة');
      } else {
        await apiClient.post('/api/addons', { name: addonName.trim() });
        toast.success('تمت إضافة الإضافة بنجاح');
      }
      setAddonModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ الإضافة';
      toast.error(msg);
    }
  };

  const handleDeleteAddon = (addon: AddonDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف الإضافة',
      message: `هل أنت متأكد من حذف "${addon.name}"؟`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/addons/${addon.id}`);
          toast.success('تم حذف الإضافة');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف الإضافة';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 5: Payment Methods Handlers
  // ==========================================
  const handleSavePm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!pmForm.name.trim()) {
      toast.error('اسم وسيلة الدفع مطلوب');
      return;
    }
    try {
      if (editingPm) {
        await apiClient.put(`/api/paymentmethods/${editingPm.id}`, {
          id: editingPm.id,
          name: pmForm.name.trim(),
          isTaxFree: pmForm.isTaxFree,
          logoUrl: pmForm.logoUrl || null,
        });
        toast.success('تم تعديل وسيلة الدفع');
      } else {
        await apiClient.post('/api/paymentmethods', {
          name: pmForm.name.trim(),
          isTaxFree: pmForm.isTaxFree,
          logoUrl: pmForm.logoUrl || null,
        });
        toast.success('تمت إضافة وسيلة الدفع');
      }
      setPmModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ وسيلة الدفع';
      toast.error(msg);
    }
  };

  const handleDeletePm = (pm: PaymentMethodDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف طريقة الدفع',
      message: `هل أنت متأكد من حذف طريقة الدفع "${pm.name}"؟`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/paymentmethods/${pm.id}`);
          toast.success('تم حذف طريقة الدفع');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف طريقة الدفع';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 6: Users & Permissions Handlers
  // ==========================================
  const allAvailablePermissions = [
    { key: 'Dashboard', label: 'لوحة القيادة والمؤشرات' },
    { key: 'POS', label: 'شاشة نقطة البيع (POS)' },
    { key: 'POS.ApplyDiscount', label: 'منح خصومات للكاشير' },
    { key: 'POS.VoidItem', label: 'حذف صنف من الطلب' },
    { key: 'Orders', label: 'إدارة الطلبات السابقة' },
    { key: 'Orders.VoidOrder', label: 'إلغاء واسترجاع الطلب' },
    { key: 'Products', label: 'إدارة قائمة الأصناف والأسعار' },
    { key: 'Categories', label: 'إدارة التصنيفات والربط' },
    { key: 'Treasury', label: 'الخزينة وحركات الصندوق' },
    { key: 'Shift.ViewTotals', label: 'رؤية إجماليات الوردية (غير عمياء)' },
    { key: 'Reports', label: 'التقارير المالية والأرباح' },
    { key: 'Settings', label: 'إعدادات النظام والأجهزة' },
  ];

  const handleSaveUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userForm.userName.trim() || !userForm.email.trim()) {
      toast.error('اسم المستخدم والبريد الإلكتروني مطلوبان');
      return;
    }
    try {
      if (editingUser) {
        // Update User info
        await apiClient.put(`/api/users/${editingUser.id}`, {
          userName: userForm.userName.trim(),
          email: userForm.email.trim(),
          phoneNumber: userForm.phoneNumber.trim() || null,
          permissions: userForm.permissions,
        });

        // Update User Roles
        await apiClient.post(`/api/users/${editingUser.id}/roles`, {
          roles: userForm.roles,
        });

        toast.success('تم تحديث بيانات المستخدم وصلاحياته');
      } else {
        // Register new user
        if (!userForm.password || userForm.password.length < 8) {
          toast.error('كلمة المرور يجب أن لا تقل عن 8 خانات');
          return;
        }

        await apiClient.post('/api/auth/register', {
          userName: userForm.userName.trim(),
          email: userForm.email.trim(),
          password: userForm.password,
          phoneNumber: userForm.phoneNumber.trim() || null,
          permissions: userForm.permissions,
        });

        toast.success('تم إنشاء حساب المستخدم بنجاح');
      }
      setUserModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ المستخدم';
      toast.error(msg);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingUser || !newPassword || newPassword.length < 8) {
      toast.error('كلمة المرور يجب أن تتكون من 8 خانات على الأقل');
      return;
    }
    try {
      await apiClient.post(`/api/users/${editingUser.id}/reset-password`, {
        newPassword,
      });
      toast.success('تمت إعادة تعيين كلمة المرور بنجاح');
      setPasswordModalOpen(false);
      setNewPassword('');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل إعادة تعيين كلمة المرور';
      toast.error(msg);
    }
  };

  const handleDeleteUser = (user: UserDto) => {
    setConfirmDialog({
      isOpen: true,
      title: 'حذف المستخدم',
      message: `هل أنت متأكد من حذف المستخدم "${user.userName}"؟ لا يمكن التراجع عن هذا الإجراء.`,
      onConfirm: async () => {
        setIsDeleting(true);
        try {
          await apiClient.delete(`/api/users/${user.id}`);
          toast.success('تم حذف المستخدم بنجاح');
          setConfirmDialog((prev) => ({ ...prev, isOpen: false }));
          loadData();
        } catch (err: unknown) {
          const msg = err instanceof Error ? err.message : 'فشل حذف المستخدم';
          toast.error(msg);
        } finally {
          setIsDeleting(false);
        }
      },
    });
  };

  // ==========================================
  // Tab 7: Restaurant Info Handlers
  // ==========================================
  const handleSaveRestaurant = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!restForm.name.trim()) {
      toast.error('اسم المطعم مطلوب');
      return;
    }
    setIsSavingRest(true);
    try {
      if (restaurant) {
        await apiClient.put(`/api/restaurants/${restaurant.id}`, {
          id: restaurant.id,
          name: restForm.name.trim(),
          address: restForm.address.trim() || null,
          phoneNumber: restForm.phoneNumber.trim() || null,
          taxNumber: restForm.taxNumber.trim() || null,
          logoUrl: restForm.logoUrl || null,
        });
        toast.success('تم تحديث بيانات المطعم بنجاح');
      } else {
        await apiClient.post('/api/restaurants', {
          name: restForm.name.trim(),
          address: restForm.address.trim() || null,
          phoneNumber: restForm.phoneNumber.trim() || null,
          taxNumber: restForm.taxNumber.trim() || null,
          logoUrl: restForm.logoUrl || null,
        });
        toast.success('تم حفظ بيانات المطعم بنجاح');
      }
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حفظ بيانات المطعم';
      toast.error(msg);
    } finally {
      setIsSavingRest(false);
    }
  };

  const handleUploadRestLogo = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const formData = new FormData();
    formData.append('file', file);
    try {
      const res = await apiClient.upload<{ imageUrl: string }>('/api/restaurants/upload-logo', formData);
      if (res?.imageUrl) {
        setRestForm((prev) => ({ ...prev, logoUrl: res.imageUrl }));
        toast.success('تم رفع الشعار بنجاح');
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل رفع الشعار';
      toast.error(msg);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Top Header */}
      <div
        style={{
          background: 'var(--color-surface)',
          padding: '20px 24px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '16px',
        }}
      >
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, margin: 0, color: 'var(--color-text-main)' }}>
            مركز إعدادات النظام والأجهزة
          </h1>
          <p style={{ margin: '4px 0 0 0', fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
            تهيئة محطات الطباعة، الطابعات، الخزائن، المستخدمين والصلاحيات
          </p>
        </div>

        <Button variant="secondary" size="md" icon={<RotateCcw size={16} />} onClick={loadData}>
          تحديث البيانات
        </Button>
      </div>

      {/* Tabs Navigation */}
      <div
        style={{
          display: 'flex',
          gap: '8px',
          borderBottom: '2px solid var(--color-border)',
          paddingBottom: '2px',
          overflowX: 'auto',
        }}
      >
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '10px 18px',
                background: isActive ? 'var(--color-surface)' : 'transparent',
                border: 'none',
                borderBottom: isActive ? '3px solid var(--color-primary)' : '3px solid transparent',
                borderRadius: 'var(--radius-md) var(--radius-md) 0 0',
                color: isActive ? 'var(--color-primary)' : 'var(--color-text-muted)',
                fontWeight: isActive ? 700 : 500,
                fontSize: '0.9rem',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
                whiteSpace: 'nowrap',
              }}
            >
              <Icon size={18} />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* Tab Contents */}
      {isLoading ? (
        <div style={{ padding: '60px 0', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
          <LoadingSpinner size="lg" />
          <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>جاري تحميل الإعدادات...</span>
        </div>
      ) : (
        <div>
          {/* ======================================================== */}
          {/* TAB 1: Print Stations */}
          {/* ======================================================== */}
          {activeTab === 'stations' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  محطات الطباعة تحدد أين تطبع تذاكر المطبخ والأصناف بحسب التصنيف (مثال: محطة الشواية، محطة المشروبات)
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingStation(null);
                    setStationName('');
                    setStationModalOpen(true);
                  }}
                >
                  إضافة محطة طباعة
                </Button>
              </div>

              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  overflow: 'hidden',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                      <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم المحطة</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>الطابعة المرتبطة</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {stations.length === 0 ? (
                      <tr>
                        <td colSpan={4} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                          لا توجد محطات طباعة معرفة حالياً
                        </td>
                      </tr>
                    ) : (
                      stations.map((st, idx) => (
                        <tr
                          key={st.id}
                          style={{
                            borderBottom: '1px solid var(--color-border)',
                            background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                          }}
                        >
                          <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>{idx + 1}</td>
                          <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                            {st.name}
                          </td>
                          <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>
                            {st.printerName || 'غير مسند لطابعة'}
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'left' }}>
                            <div style={{ display: 'inline-flex', gap: '8px' }}>
                              <Button
                                variant="ghost"
                                size="sm"
                                icon={<Edit2 size={15} />}
                                onClick={() => {
                                  setEditingStation(st);
                                  setStationName(st.name);
                                  setStationModalOpen(true);
                                }}
                              >
                                تعديل
                              </Button>
                              <Button
                                variant="danger"
                                size="sm"
                                icon={<Trash2 size={15} />}
                                onClick={() => handleDeleteStation(st)}
                              >
                                حذف
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 2: Printers */}
          {/* ======================================================== */}
          {activeTab === 'printers' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  إدارة طابعات الإيصالات والمطبخ وربط كل طابعة بمحطة الطباعة المناسبة
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingPrinter(null);
                    setPrinterForm({
                      name: '',
                      printerName: '',
                      printerType: PrinterType.Receipt,
                      printStationId: stations[0]?.id || 0,
                    });
                    setPrinterModalOpen(true);
                  }}
                >
                  إضافة طابعة جديدة
                </Button>
              </div>

              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  overflow: 'hidden',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                      <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>الاسم المعروض</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم الطابعة بالنظام</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>النوع</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>محطة الطباعة</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {printers.length === 0 ? (
                      <tr>
                        <td colSpan={6} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                          لا توجد طابعات مضافة حالياً
                        </td>
                      </tr>
                    ) : (
                      printers.map((pr, idx) => {
                        const typeLabel =
                          pr.printerType === PrinterType.Receipt
                            ? 'إيصالات الزبائن'
                            : pr.printerType === PrinterType.Kitchen
                            ? 'أوردر المطبخ'
                            : 'تقارير عامة';
                        return (
                          <tr
                            key={pr.id}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>{idx + 1}</td>
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                              {pr.name}
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontFamily: 'monospace' }}>
                              {pr.printerName}
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '3px 10px',
                                  borderRadius: 'var(--radius-full)',
                                  fontSize: '0.8rem',
                                  fontWeight: 600,
                                  background:
                                    pr.printerType === PrinterType.Receipt
                                      ? 'rgba(56, 224, 123, 0.12)'
                                      : 'rgba(14, 165, 233, 0.12)',
                                  color:
                                    pr.printerType === PrinterType.Receipt
                                      ? 'var(--color-primary)'
                                      : '#0ea5e9',
                                }}
                              >
                                {typeLabel}
                              </span>
                            </td>
                            <td style={{ padding: '12px 16px', fontWeight: 600 }}>
                              {pr.printStationName ||
                                stations.find((s) => s.id === pr.printStationId)?.name ||
                                'غير محدد'}
                            </td>
                            <td style={{ padding: '12px 16px', textAlign: 'left' }}>
                              <div style={{ display: 'inline-flex', gap: '8px' }}>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  icon={<Printer size={15} />}
                                  onClick={() => window.print()}
                                  title="اختبار الطباعة"
                                >
                                  اختبار
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  icon={<Edit2 size={15} />}
                                  onClick={() => {
                                    setEditingPrinter(pr);
                                    setPrinterForm({
                                      name: pr.name,
                                      printerName: pr.printerName,
                                      printerType: pr.printerType,
                                      printStationId: pr.printStationId,
                                    });
                                    setPrinterModalOpen(true);
                                  }}
                                >
                                  تعديل
                                </Button>
                                <Button
                                  variant="danger"
                                  size="sm"
                                  icon={<Trash2 size={15} />}
                                  onClick={() => handleDeletePrinter(pr)}
                                >
                                  حذف
                                </Button>
                              </div>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 3: Cashboxes */}
          {/* ======================================================== */}
          {activeTab === 'cashboxes' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  نقاط الخزائن وصناديق النقدية لحساب الرصيد والإيرادات والمصروفات اليومية
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingCashbox(null);
                    setCashboxForm({ name: '', description: '', initialBalance: '0', isActive: true });
                    setCashboxModalOpen(true);
                  }}
                >
                  إضافة خزينة جديدة
                </Button>
              </div>

              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
                  gap: '16px',
                }}
              >
                {cashboxes.length === 0 ? (
                  <div style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '40px', color: 'var(--color-text-muted)' }}>
                    لا توجد خزائن معرفة في النظام
                  </div>
                ) : (
                  cashboxes.map((cb) => (
                    <div
                      key={cb.id}
                      style={{
                        background: 'var(--color-surface)',
                        borderRadius: 'var(--radius-lg)',
                        border: '1px solid var(--color-border)',
                        padding: '20px',
                        display: 'flex',
                        flexDirection: 'column',
                        justifyContent: 'space-between',
                        gap: '16px',
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div>
                          <h3 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 800, color: 'var(--color-text-main)' }}>
                            {cb.name}
                          </h3>
                          <p style={{ margin: '4px 0 0 0', fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                            {cb.description || 'لا يوجد وصف'}
                          </p>
                        </div>
                        <span
                          style={{
                            padding: '3px 10px',
                            borderRadius: 'var(--radius-full)',
                            fontSize: '0.8rem',
                            fontWeight: 600,
                            background: cb.isActive !== false ? 'rgba(56, 224, 123, 0.12)' : 'rgba(239, 68, 68, 0.12)',
                            color: cb.isActive !== false ? 'var(--color-primary)' : 'var(--color-danger)',
                          }}
                        >
                          {cb.isActive !== false ? 'نشطة' : 'معطلة'}
                        </span>
                      </div>

                      <div
                        style={{
                          background: 'var(--color-bg-base)',
                          padding: '12px 16px',
                          borderRadius: 'var(--radius-md)',
                          border: '1px solid var(--color-border)',
                          display: 'flex',
                          alignItems: 'baseline',
                          justifyContent: 'space-between',
                        }}
                      >
                        <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)', fontWeight: 600 }}>
                          الرصيد المالي الحالي
                        </span>
                        <div
                          style={{
                            fontSize: '1.35rem',
                            fontWeight: 800,
                            color: 'var(--color-primary)',
                            fontFamily: 'monospace',
                          }}
                        >
                          {(cb.currentBalance || 0).toFixed(2)}{' '}
                          <span style={{ fontSize: '0.8rem', fontWeight: 600 }}>د.ل</span>
                        </div>
                      </div>

                      <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                        <Button
                          variant="ghost"
                          size="sm"
                          icon={<Edit2 size={15} />}
                          onClick={() => {
                            setEditingCashbox(cb);
                            setCashboxForm({
                              name: cb.name,
                              description: cb.description || '',
                              initialBalance: String(cb.initialBalance || 0),
                              isActive: cb.isActive !== false,
                            });
                            setCashboxModalOpen(true);
                          }}
                        >
                          تعديل
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          icon={<Trash2 size={15} />}
                          onClick={() => handleDeleteCashbox(cb)}
                        >
                          حذف
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 4: Addons */}
          {/* ======================================================== */}
          {activeTab === 'addons' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  قائمة الإضافات والخيارات الاختيارية للأطباق (مثل: زيادة جبنة، صوص حار...)
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingAddon(null);
                    setAddonName('');
                    setAddonModalOpen(true);
                  }}
                >
                  إضافة خيار جديد
                </Button>
              </div>

              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  overflow: 'hidden',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                      <th style={{ padding: '12px 16px', fontWeight: 700, width: '60px' }}>#</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم الإضافة</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {addons.length === 0 ? (
                      <tr>
                        <td colSpan={3} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                          لا توجد إضافات مسجلة حالياً
                        </td>
                      </tr>
                    ) : (
                      addons.map((ad, idx) => (
                        <tr
                          key={ad.id}
                          style={{
                            borderBottom: '1px solid var(--color-border)',
                            background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                          }}
                        >
                          <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>{idx + 1}</td>
                          <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                            {ad.name}
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'left' }}>
                            <div style={{ display: 'inline-flex', gap: '8px' }}>
                              <Button
                                variant="ghost"
                                size="sm"
                                icon={<Edit2 size={15} />}
                                onClick={() => {
                                  setEditingAddon(ad);
                                  setAddonName(ad.name);
                                  setAddonModalOpen(true);
                                }}
                              >
                                تعديل
                              </Button>
                              <Button
                                variant="danger"
                                size="sm"
                                icon={<Trash2 size={15} />}
                                onClick={() => handleDeleteAddon(ad)}
                              >
                                حذف
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 5: Payment Methods */}
          {/* ======================================================== */}
          {activeTab === 'paymentMethods' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  طرق الدفع المعرفة (نقداً، بطاقة مصرفية، تداول، سداد...)
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingPm(null);
                    setPmForm({ name: '', isTaxFree: false, logoUrl: '' });
                    setPmModalOpen(true);
                  }}
                >
                  إضافة طريقة دفع
                </Button>
              </div>

              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
                  gap: '16px',
                }}
              >
                {paymentMethods.length === 0 ? (
                  <div style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '40px', color: 'var(--color-text-muted)' }}>
                    لا توجد طرق دفع معرفة
                  </div>
                ) : (
                  paymentMethods.map((pm) => (
                    <div
                      key={pm.id}
                      style={{
                        background: 'var(--color-surface)',
                        borderRadius: 'var(--radius-lg)',
                        border: '1px solid var(--color-border)',
                        padding: '20px',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                        <div
                          style={{
                            width: '46px',
                            height: '46px',
                            borderRadius: 'var(--radius-md)',
                            background: 'rgba(56, 224, 123, 0.12)',
                            color: 'var(--color-primary)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            overflow: 'hidden',
                          }}
                        >
                          {pm.logoUrl ? (
                            <img src={getFullImageUrl(pm.logoUrl)} alt={pm.name} style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                          ) : (
                            <CreditCard size={24} />
                          )}
                        </div>
                        <div>
                          <h4 style={{ margin: 0, fontWeight: 700, fontSize: '0.95rem' }}>{pm.name}</h4>
                          <span style={{ fontSize: '0.75rem', color: pm.isTaxFree ? '#0ea5e9' : 'var(--color-text-muted)' }}>
                            {pm.isTaxFree ? 'معفية من الضريبة' : 'خاضعة للضريبة'}
                          </span>
                        </div>
                      </div>

                      <div style={{ display: 'flex', gap: '6px' }}>
                        <Button
                          variant="ghost"
                          size="sm"
                          icon={<Edit2 size={15} />}
                          onClick={() => {
                            setEditingPm(pm);
                            setPmForm({
                              name: pm.name,
                              isTaxFree: Boolean(pm.isTaxFree),
                              logoUrl: pm.logoUrl || '',
                            });
                            setPmModalOpen(true);
                          }}
                        >
                          تعديل
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          icon={<Trash2 size={15} />}
                          onClick={() => handleDeletePm(pm)}
                        >
                          حذف
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 6: Users & Permissions */}
          {/* ======================================================== */}
          {activeTab === 'users' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: '0.9rem' }}>
                  إدارة مستخدمي النظام (كاشير، مدير، مشرف) وتعيين الأذونات الدقيقة لكل مستخدم
                </span>
                <Button
                  variant="primary"
                  size="md"
                  icon={<Plus size={16} />}
                  onClick={() => {
                    setEditingUser(null);
                    setUserForm({
                      userName: '',
                      email: '',
                      password: '',
                      phoneNumber: '',
                      roles: ['Cashier'],
                      permissions: ['POS', 'Orders'],
                    });
                    setUserModalOpen(true);
                  }}
                >
                  إضافة مستخدم جديد
                </Button>
              </div>

              <div
                style={{
                  background: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  overflow: 'hidden',
                }}
              >
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right', fontSize: '0.85rem' }}>
                  <thead>
                    <tr style={{ background: 'var(--color-bg-base)', borderBottom: '1px solid var(--color-border)' }}>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>اسم المستخدم</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>البريد الإلكتروني</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>الهاتف</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>الدور</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700 }}>الصلاحيات المفعلة</th>
                      <th style={{ padding: '12px 16px', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.length === 0 ? (
                      <tr>
                        <td colSpan={6} style={{ textAlign: 'center', padding: '36px', color: 'var(--color-text-muted)' }}>
                          لا يوجد مستخدمون مسجلون
                        </td>
                      </tr>
                    ) : (
                      users.map((u, idx) => {
                        const isAdmin = u.roles?.includes('Admin');
                        return (
                          <tr
                            key={u.id}
                            style={{
                              borderBottom: '1px solid var(--color-border)',
                              background: idx % 2 === 0 ? 'transparent' : 'rgba(255, 255, 255, 0.02)',
                            }}
                          >
                            <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-text-main)' }}>
                              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                <Shield size={16} color={isAdmin ? '#f59e0b' : 'var(--color-primary)'} />
                                <span>{u.userName}</span>
                              </div>
                            </td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)' }}>{u.email}</td>
                            <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontFamily: 'monospace' }}>
                              {u.phoneNumber || '-'}
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span
                                style={{
                                  padding: '4px 10px',
                                  borderRadius: 'var(--radius-sm)',
                                  background: isAdmin ? 'rgba(245, 158, 11, 0.15)' : 'rgba(56, 224, 123, 0.15)',
                                  color: isAdmin ? '#f59e0b' : 'var(--color-primary)',
                                  fontWeight: 700,
                                  fontSize: '0.8rem',
                                }}
                              >
                                {isAdmin ? 'مسؤول (Admin)' : 'كاشير (Cashier)'}
                              </span>
                            </td>
                            <td style={{ padding: '12px 16px' }}>
                              <span style={{ fontSize: '0.8rem', color: 'var(--color-text-muted)' }}>
                                {isAdmin
                                  ? 'صلاحيات كاملة شاملة'
                                  : `${u.permissions?.length || 0} إذن محدد`}
                              </span>
                            </td>
                            <td style={{ padding: '12px 16px', textAlign: 'left' }}>
                              <div style={{ display: 'inline-flex', gap: '8px' }}>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  icon={<Key size={15} />}
                                  onClick={() => {
                                    setEditingUser(u);
                                    setNewPassword('');
                                    setPasswordModalOpen(true);
                                  }}
                                  title="تغيير كلمة المرور"
                                >
                                  كلمة المرور
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  icon={<Edit2 size={15} />}
                                  onClick={() => {
                                    setEditingUser(u);
                                    setUserForm({
                                      userName: u.userName,
                                      email: u.email || '',
                                      password: '',
                                      phoneNumber: u.phoneNumber || '',
                                      roles: u.roles || ['Cashier'],
                                      permissions: u.permissions || [],
                                    });
                                    setUserModalOpen(true);
                                  }}
                                >
                                  تعديل
                                </Button>
                                <Button
                                  variant="danger"
                                  size="sm"
                                  icon={<Trash2 size={15} />}
                                  onClick={() => handleDeleteUser(u)}
                                >
                                  حذف
                                </Button>
                              </div>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* TAB 7: Restaurant Profile */}
          {/* ======================================================== */}
          {activeTab === 'restaurant' && (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
                gap: '24px',
              }}
            >
              {/* Form Card */}
              <div
                style={{
                  background: 'var(--color-surface)',
                  padding: '28px',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                }}
              >
                <h3 style={{ margin: '0 0 18px 0', fontSize: '1.15rem', fontWeight: 800 }}>
                  معلومات المطعم وبيانات الترويسة
                </h3>

                <form onSubmit={handleSaveRestaurant} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  <Input
                    label="اسم المطعم *"
                    value={restForm.name}
                    onChange={(e) => setRestForm({ ...restForm, name: e.target.value })}
                    required
                  />

                  <Input
                    label="العنوان"
                    value={restForm.address}
                    onChange={(e) => setRestForm({ ...restForm, address: e.target.value })}
                    placeholder="مثال: طرابلس - حي الأندلس"
                  />

                  <Input
                    label="رقم الهاتف"
                    value={restForm.phoneNumber}
                    onChange={(e) => setRestForm({ ...restForm, phoneNumber: e.target.value })}
                    placeholder="091XXXXXXX"
                  />

                  <Input
                    label="الرقم الضريبي / السجل التجاري"
                    value={restForm.taxNumber}
                    onChange={(e) => setRestForm({ ...restForm, taxNumber: e.target.value })}
                    placeholder="TR-XXXXXX"
                  />

                  {/* Logo Upload */}
                  <div>
                    <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, marginBottom: '6px' }}>
                      شعار المطعم (يظهر في الإيصالات)
                    </label>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                      {restForm.logoUrl && (
                        <img
                          src={getFullImageUrl(restForm.logoUrl)}
                          alt="شعار المطعم"
                          style={{
                            width: '64px',
                            height: '64px',
                            objectFit: 'contain',
                            borderRadius: 'var(--radius-md)',
                            border: '1px solid var(--color-border)',
                            background: '#ffffff',
                            padding: '4px',
                          }}
                        />
                      )}
                      <label
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '8px',
                          padding: '10px 16px',
                          background: 'var(--color-bg-base)',
                          borderRadius: 'var(--radius-md)',
                          border: '1px solid var(--color-border)',
                          cursor: 'pointer',
                          fontSize: '0.85rem',
                          fontWeight: 600,
                        }}
                      >
                        <Upload size={16} />
                        <span>اختر صورة الشعار...</span>
                        <input type="file" accept="image/*" onChange={handleUploadRestLogo} style={{ display: 'none' }} />
                      </label>
                    </div>
                  </div>

                  <div style={{ marginTop: '12px' }}>
                    <Button variant="primary" size="md" type="submit" isLoading={isSavingRest}>
                      حفظ بيانات المطعم
                    </Button>
                  </div>
                </form>
              </div>

              {/* Receipt Preview Card */}
              <div
                style={{
                  background: 'var(--color-surface)',
                  padding: '28px',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                }}
              >
                <h3 style={{ margin: '0 0 18px 0', fontSize: '1.15rem', fontWeight: 800, alignSelf: 'flex-start' }}>
                  معاينة ترويسة الإيصال الحراري
                </h3>

                <div
                  style={{
                    width: '280px',
                    background: '#ffffff',
                    color: '#000000',
                    padding: '24px 16px',
                    borderRadius: '8px',
                    boxShadow: 'var(--shadow-md)',
                    textAlign: 'center',
                    fontFamily: 'monospace',
                    fontSize: '0.85rem',
                    lineHeight: 1.4,
                  }}
                >
                  {restForm.logoUrl && (
                    <img
                      src={getFullImageUrl(restForm.logoUrl)}
                      alt="Logo"
                      style={{ width: '48px', height: '48px', objectFit: 'contain', margin: '0 auto 8px auto' }}
                    />
                  )}
                  <div style={{ fontSize: '1.1rem', fontWeight: 800 }}>{restForm.name || 'اسم المطعم'}</div>
                  {restForm.address && <div style={{ fontSize: '0.75rem', marginTop: '4px' }}>{restForm.address}</div>}
                  {restForm.phoneNumber && (
                    <div style={{ fontSize: '0.75rem' }}>هاتف: {restForm.phoneNumber}</div>
                  )}
                  {restForm.taxNumber && (
                    <div style={{ fontSize: '0.75rem' }}>الرقم الضريبي: {restForm.taxNumber}</div>
                  )}

                  <div style={{ borderBottom: '1px dashed #000', margin: '14px 0' }} />

                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', fontWeight: 700 }}>
                    <span>فاتورة ضريبية مبسطة</span>
                    <span>#1001</span>
                  </div>
                  <div style={{ fontSize: '0.7rem', color: '#555', textAlign: 'right', marginTop: '4px' }}>
                    {new Date().toLocaleString('ar-LY')}
                  </div>

                  <div style={{ borderBottom: '1px dashed #000', margin: '14px 0' }} />

                  <div style={{ fontSize: '0.75rem', color: '#333' }}>
                    شكراً لزيارتكم! نتشرف بخدمتكم دائماً
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ======================================================== */}
      {/* MODALS */}
      {/* ======================================================== */}

      {/* 1. Station Modal */}
      <Modal
        isOpen={stationModalOpen}
        onClose={() => setStationModalOpen(false)}
        title={editingStation ? 'تعديل محطة الطباعة' : 'إضافة محطة طباعة جديدة'}
      >
        <form onSubmit={handleSaveStation} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="اسم محطة الطباعة *"
            value={stationName}
            onChange={(e) => setStationName(e.target.value)}
            placeholder="مثال: مطبخ المشويات، البار، الفرن"
            required
            autoFocus
          />
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setStationModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ
            </Button>
          </div>
        </form>
      </Modal>

      {/* 2. Printer Modal */}
      <Modal
        isOpen={printerModalOpen}
        onClose={() => setPrinterModalOpen(false)}
        title={editingPrinter ? 'تعديل بيانات الطابعة' : 'إضافة طابعة جديدة'}
      >
        <form onSubmit={handleSavePrinter} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="الاسم التعريفي للطابعة *"
            value={printerForm.name}
            onChange={(e) => setPrinterForm({ ...printerForm, name: e.target.value })}
            placeholder="طابعة الكاشير الرئيسية"
            required
          />
          <Input
            label="اسم الطابعة في نظام التشغيل أو الشبكة *"
            value={printerForm.printerName}
            onChange={(e) => setPrinterForm({ ...printerForm, printerName: e.target.value })}
            placeholder="مثال: POS-80C أو 192.168.1.200"
            required
          />

          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, marginBottom: '6px' }}>
              نوع الطابعة *
            </label>
            <select
              value={printerForm.printerType}
              onChange={(e) => setPrinterForm({ ...printerForm, printerType: Number(e.target.value) as PrinterType })}
              style={{
                width: '100%',
                padding: '10px 12px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                background: 'var(--color-bg-base)',
                color: 'var(--color-text-main)',
                outline: 'none',
              }}
            >
              <option value={PrinterType.Receipt}>إيصالات الزبائن (فواتير الكاشير)</option>
              <option value={PrinterType.Kitchen}>تذاكر المطبخ (أوردرات التحضير)</option>
              <option value={PrinterType.Report}>تقارير عامة</option>
            </select>
          </div>

          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, marginBottom: '6px' }}>
              محطة الطباعة التابعة لها *
            </label>
            <select
              value={printerForm.printStationId}
              onChange={(e) => setPrinterForm({ ...printerForm, printStationId: Number(e.target.value) })}
              style={{
                width: '100%',
                padding: '10px 12px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                background: 'var(--color-bg-base)',
                color: 'var(--color-text-main)',
                outline: 'none',
              }}
              required
            >
              <option value={0} disabled>
                اختر محطة الطباعة...
              </option>
              {stations.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setPrinterModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ الطابعة
            </Button>
          </div>
        </form>
      </Modal>

      {/* 3. Cashbox Modal */}
      <Modal
        isOpen={cashboxModalOpen}
        onClose={() => setCashboxModalOpen(false)}
        title={editingCashbox ? 'تعديل الخزينة' : 'إضافة خزينة جديدة'}
      >
        <form onSubmit={handleSaveCashbox} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="اسم الخزينة *"
            value={cashboxForm.name}
            onChange={(e) => setCashboxForm({ ...cashboxForm, name: e.target.value })}
            placeholder="خزينة درج الكاشير 1"
            required
          />
          <Input
            label="الوصف أو المكان"
            value={cashboxForm.description}
            onChange={(e) => setCashboxForm({ ...cashboxForm, description: e.target.value })}
            placeholder="الدرج السفلي عند البوابة"
          />
          {!editingCashbox && (
            <Input
              label="الرصيد الافتتاحي (د.ل)"
              type="number"
              step="0.01"
              value={cashboxForm.initialBalance}
              onChange={(e) => setCashboxForm({ ...cashboxForm, initialBalance: e.target.value })}
            />
          )}

          <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer', marginTop: '4px' }}>
            <input
              type="checkbox"
              checked={cashboxForm.isActive}
              onChange={(e) => setCashboxForm({ ...cashboxForm, isActive: e.target.checked })}
              style={{ width: '18px', height: '18px', accentColor: 'var(--color-primary)' }}
            />
            <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>الخزينة نشطة ومتاحة للاستخدام</span>
          </label>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setCashboxModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ الخزينة
            </Button>
          </div>
        </form>
      </Modal>

      {/* 4. Addon Modal */}
      <Modal
        isOpen={addonModalOpen}
        onClose={() => setAddonModalOpen(false)}
        title={editingAddon ? 'تعديل الإضافة' : 'إضافة خيار جديد'}
      >
        <form onSubmit={handleSaveAddon} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="اسم الإضافة *"
            value={addonName}
            onChange={(e) => setAddonName(e.target.value)}
            placeholder="مثال: جبنة إضافية، كاتشب، مخلل"
            required
            autoFocus
          />
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setAddonModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ
            </Button>
          </div>
        </form>
      </Modal>

      {/* 5. Payment Method Modal */}
      <Modal
        isOpen={pmModalOpen}
        onClose={() => setPmModalOpen(false)}
        title={editingPm ? 'تعديل وسيلة الدفع' : 'إضافة وسيلة دفع جديدة'}
      >
        <form onSubmit={handleSavePm} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="اسم طريقة الدفع *"
            value={pmForm.name}
            onChange={(e) => setPmForm({ ...pmForm, name: e.target.value })}
            placeholder="مثال: بطاقة مصرفية، تداول، سداد"
            required
          />

          <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={pmForm.isTaxFree}
              onChange={(e) => setPmForm({ ...pmForm, isTaxFree: e.target.checked })}
              style={{ width: '18px', height: '18px', accentColor: 'var(--color-primary)' }}
            />
            <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>معفية من الضريبة</span>
          </label>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setPmModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ وسيلة الدفع
            </Button>
          </div>
        </form>
      </Modal>

      {/* 6. User Modal (Add/Edit User & Granular Permissions) */}
      <Modal
        isOpen={userModalOpen}
        onClose={() => setUserModalOpen(false)}
        title={editingUser ? `تعديل المستخدم: ${editingUser.userName}` : 'إضافة مستخدم جديد'}
        maxWidth="680px"
      >
        <form onSubmit={handleSaveUser} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <Input
              label="اسم المستخدم *"
              value={userForm.userName}
              onChange={(e) => setUserForm({ ...userForm, userName: e.target.value })}
              required
            />
            <Input
              label="البريد الإلكتروني *"
              type="email"
              value={userForm.email}
              onChange={(e) => setUserForm({ ...userForm, email: e.target.value })}
              required
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <Input
              label="رقم الهاتف"
              value={userForm.phoneNumber}
              onChange={(e) => setUserForm({ ...userForm, phoneNumber: e.target.value })}
            />
            {!editingUser && (
              <Input
                label="كلمة المرور (8 أحرف على الأقل) *"
                type="password"
                value={userForm.password}
                onChange={(e) => setUserForm({ ...userForm, password: e.target.value })}
                required
              />
            )}
          </div>

          {/* Role Selection */}
          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, marginBottom: '6px' }}>
              الدور الرئيسي في النظام *
            </label>
            <div style={{ display: 'flex', gap: '16px' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="role"
                  checked={userForm.roles.includes('Cashier')}
                  onChange={() => setUserForm({ ...userForm, roles: ['Cashier'] })}
                  style={{ accentColor: 'var(--color-primary)' }}
                />
                <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>كاشير (Cashier)</span>
              </label>

              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="role"
                  checked={userForm.roles.includes('Admin')}
                  onChange={() => setUserForm({ ...userForm, roles: ['Admin'] })}
                  style={{ accentColor: 'var(--color-primary)' }}
                />
                <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>مسؤول النظام (Admin)</span>
              </label>
            </div>
          </div>

          {/* Granular Permissions Checkboxes */}
          <div>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 700, marginBottom: '10px' }}>
              صلاحيات الوصول التفصيلية (الأذونات):
            </label>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
                gap: '10px',
                background: 'var(--color-bg-base)',
                padding: '14px',
                borderRadius: 'var(--radius-md)',
                border: '1px solid var(--color-border)',
                maxHeight: '220px',
                overflowY: 'auto',
              }}
            >
              {allAvailablePermissions.map((perm) => {
                const isChecked =
                  userForm.roles.includes('Admin') || userForm.permissions.includes(perm.key);
                return (
                  <label
                    key={perm.key}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '8px',
                      fontSize: '0.85rem',
                      cursor: userForm.roles.includes('Admin') ? 'default' : 'pointer',
                      opacity: userForm.roles.includes('Admin') ? 0.7 : 1,
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={isChecked}
                      disabled={userForm.roles.includes('Admin')}
                      onChange={(e) => {
                        const nextPerms = e.target.checked
                          ? [...userForm.permissions, perm.key]
                          : userForm.permissions.filter((p) => p !== perm.key);
                        setUserForm({ ...userForm, permissions: nextPerms });
                      }}
                      style={{ accentColor: 'var(--color-primary)' }}
                    />
                    <span>{perm.label}</span>
                  </label>
                );
              })}
            </div>
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setUserModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              حفظ المستخدم
            </Button>
          </div>
        </form>
      </Modal>

      {/* 7. Password Reset Modal */}
      <Modal
        isOpen={passwordModalOpen}
        onClose={() => setPasswordModalOpen(false)}
        title={`إعادة تعيين كلمة مرور: ${editingUser?.userName}`}
      >
        <form onSubmit={handleResetPassword} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <Input
            label="كلمة المرور الجديدة (8 خانات على الأقل) *"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            autoFocus
          />
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '12px' }}>
            <Button variant="secondary" size="md" type="button" onClick={() => setPasswordModalOpen(false)}>
              إلغاء
            </Button>
            <Button variant="primary" size="md" type="submit">
              تحديث كلمة المرور
            </Button>
          </div>
        </form>
      </Modal>

      {/* Confirmation Dialog */}
      <ConfirmDialog
        isOpen={confirmDialog.isOpen}
        onClose={() => setConfirmDialog((prev) => ({ ...prev, isOpen: false }))}
        onConfirm={confirmDialog.onConfirm}
        title={confirmDialog.title}
        message={confirmDialog.message}
        isLoading={isDeleting}
      />
    </div>
  );
};

export default SettingsPage;
