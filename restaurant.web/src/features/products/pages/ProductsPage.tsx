import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Package,
  Plus,
  Search,
  LayoutGrid,
  List,
  Edit2,
  Trash2,
  Upload,
  CheckCircle2,
  XCircle,
  TrendingUp,
  Image as ImageIcon,
  DollarSign,
  Barcode,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import Modal from '../../../components/ui/Modal';
import ConfirmDialog from '../../../components/ui/ConfirmDialog';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import apiClient from '../../../services/apiClient';
import type { ProductDto, CategoryDto } from '../../../types/api';
import { getFullImageUrl } from '../../../utils/imageUtils';
import toast from 'react-hot-toast';
import { useAuth } from '../../../hooks/useAuth';

type ViewMode = 'cards' | 'table';
type SortKey = 'name' | 'priceAsc' | 'priceDesc' | 'costAsc' | 'costDesc' | 'profitDesc';

export const ProductsPage: React.FC = () => {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const { hasPermission } = useAuth();
  const canManage = hasPermission('Permission.Products.Manage');
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Filters & Sorting
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [selectedCategoryFilter, setSelectedCategoryFilter] = useState<number | 'all'>('all');
  const [sortBy, setSortBy] = useState<SortKey>('name');
  const [viewMode, setViewMode] = useState<ViewMode>('cards');

  // Modal State (Create / Edit)
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingProduct, setEditingProduct] = useState<ProductDto | null>(null);
  const [formName, setFormName] = useState<string>('');
  const [formDescription, setFormDescription] = useState('');
  const [formCategoryId, setFormCategoryId] = useState<number | ''>('');
  const [formSalePrice, setFormSalePrice] = useState<string>('0');
  const [formCostPrice, setFormCostPrice] = useState<string>('0');
  const [formBarcode, setFormBarcode] = useState<string>('');
  const [formImageUrl, setFormImageUrl] = useState<string>('');
  const [formIsAvailable, setFormIsAvailable] = useState<boolean>(true);
  const [isUploadingImage, setIsUploadingImage] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Delete State
  const [deleteTarget, setDeleteTarget] = useState<ProductDto | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  // Load Products & Categories
  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [prodsRes, catsRes] = await Promise.all([
        apiClient.get<ProductDto[]>('/api/products'),
        apiClient.get<CategoryDto[]>('/api/categories'),
      ]);
      setProducts(prodsRes || []);
      setCategories(catsRes || []);
    } catch {
      toast.error('فشل تحميل المنتجات والتصنيفات.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => { void loadData(); }, 0);
    return () => window.clearTimeout(timer);
  }, [loadData]);

  // Open Create Modal
  const handleOpenCreate = () => {
    if (!canManage) return;
    setEditingProduct(null);
    setFormName('');
    setFormDescription('');
    setFormCategoryId(categories[0]?.id || '');
    setFormSalePrice('0.00');
    setFormCostPrice('0.00');
    setFormBarcode('');
    setFormImageUrl('');
    setFormIsAvailable(true);
    setFormError(null);
    setIsModalOpen(true);
  };

  // Open Edit Modal
  const handleOpenEdit = (prod: ProductDto) => {
    setEditingProduct(prod);
    setFormName(prod.name);
    setFormDescription(prod.description ?? '');
    setFormCategoryId(prod.categoryId);
    setFormSalePrice(String(prod.salePrice));
    setFormCostPrice(String(prod.costPrice));
    setFormBarcode(prod.barcode || prod.barCode || '');
    setFormImageUrl(prod.imageUrl || '');
    setFormIsAvailable(prod.isAvailable !== false);
    setFormError(null);
    setIsModalOpen(true);
  };

  // Handle Image Upload
  const handleImageChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error('يرجى اختيار ملف صورة صالح (JPEG, PNG, WebP).');
      return;
    }

    setIsUploadingImage(true);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const res = await apiClient.upload<{ imageUrl: string }>('/api/products/upload-image', formData);
      if (res && res.imageUrl) {
        setFormImageUrl(res.imageUrl);
        toast.success('تم رفع الصورة بنجاح!');
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل رفع الصورة للخادم.';
      toast.error(msg);
    } finally {
      setIsUploadingImage(false);
    }
  };

  // Profit calculation in Form
  const salePriceNum = parseFloat(formSalePrice) || 0;
  const costPriceNum = parseFloat(formCostPrice) || 0;
  const calculatedProfit = salePriceNum - costPriceNum;
  const calculatedProfitMargin = costPriceNum > 0 ? (calculatedProfit / costPriceNum) * 100 : 0;

  // Handle Save
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName.trim()) {
      setFormError('يرجى إدخال اسم المنتج.');
      return;
    }
    if (!formCategoryId) {
      setFormError('يرجى اختيار تصنيف المنتج.');
      return;
    }

    setFormError(null);
    setIsSubmitting(true);
    try {
      const payload = {
        name: formName.trim(),
        description: formDescription.trim() || null,
        categoryId: Number(formCategoryId),
        salePrice: salePriceNum,
        costPrice: costPriceNum,
        barcode: formBarcode.trim() || null,
        barCode: formBarcode.trim() || null,
        imageUrl: formImageUrl.trim() || null,
        isAvailable: formIsAvailable,
      };

      if (editingProduct) {
        await apiClient.put(`/api/products/${editingProduct.id}`, {
          id: editingProduct.id,
          ...payload,
        });
        toast.success('تم تحديث بيانات المنتج بنجاح!');
      } else {
        await apiClient.post('/api/products', payload);
        toast.success('تمت إضافة المنتج الجديد بنجاح!');
      }

      setIsModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'حدث خطأ أثناء حفظ المنتج.';
      setFormError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle Delete
  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return;

    setIsDeleting(true);
    try {
      await apiClient.delete(`/api/products/${deleteTarget.id}`);
      toast.success(`تم حذف المنتج "${deleteTarget.name}" بنجاح.`);
      setDeleteTarget(null);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حذف المنتج.';
      toast.error(msg);
    } finally {
      setIsDeleting(false);
    }
  };

  // Filter & Sort Logic
  const filteredAndSortedProducts = useMemo(() => {
    const result = products.filter((p) => {
      // 1. Category Filter
      if (selectedCategoryFilter !== 'all' && p.categoryId !== selectedCategoryFilter) {
        return false;
      }
      // 2. Search Query (Name or Barcode)
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase().trim();
        const matchName = p.name.toLowerCase().includes(q);
        const bCode = p.barcode || p.barCode || '';
        const matchBarcode = bCode ? bCode.toLowerCase().includes(q) : false;
        return matchName || matchBarcode;
      }
      return true;
    });

    // Sorting
    result.sort((a, b) => {
      const profitA = a.profit ?? a.salePrice - a.costPrice;
      const profitB = b.profit ?? b.salePrice - b.costPrice;

      switch (sortBy) {
        case 'name':
          return a.name.localeCompare(b.name, 'ar');
        case 'priceAsc':
          return a.salePrice - b.salePrice;
        case 'priceDesc':
          return b.salePrice - a.salePrice;
        case 'costAsc':
          return a.costPrice - b.costPrice;
        case 'costDesc':
          return b.costPrice - a.costPrice;
        case 'profitDesc':
          return profitB - profitA;
        default:
          return 0;
      }
    });

    return result;
  }, [products, selectedCategoryFilter, searchQuery, sortBy]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '22px' }}>
      {/* Top Action Bar */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '16px',
        }}
      >
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>
            كتالوج المنتجات والأصناف
          </h2>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem', marginTop: '4px' }}>
            إدارة قائمة الأصناف، أسعار البيع والتكلفة، وحساب هوامش الربح
          </p>
        </div>

        <Button
          variant="primary"
          size="lg"
          disabled={!canManage} onClick={handleOpenCreate}
          icon={<Plus size={20} />}
        >
          إضافة منتج جديد
        </Button>
      </div>

      {/* Control Toolbar: Search, Category Filter, Sort, View Modes */}
      <div
        style={{
          backgroundColor: 'var(--color-surface)',
          padding: '16px 20px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '14px',
        }}
      >
        {/* Search Input */}
        <div style={{ maxWidth: '320px', width: '100%' }}>
          <Input
            placeholder="البحث بالاسم أو الباركود..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            icon={<Search size={18} />}
          />
        </div>

        {/* Category Filter Select */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)', whiteSpace: 'nowrap' }}>
            القسم:
          </span>
          <select
            value={selectedCategoryFilter}
            onChange={(e) => setSelectedCategoryFilter(e.target.value === 'all' ? 'all' : Number(e.target.value))}
            style={{
              padding: '8px 14px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
              backgroundColor: 'var(--color-surface)',
              color: 'var(--color-text-primary)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          >
            <option value="all">جميع الأقسام ({products.length})</option>
            {categories.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {cat.name}
              </option>
            ))}
          </select>
        </div>

        {/* Sort Select */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '0.875rem', color: 'var(--color-text-muted)', whiteSpace: 'nowrap' }}>
            الترتيب:
          </span>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as SortKey)}
            style={{
              padding: '8px 14px',
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--color-border)',
              backgroundColor: 'var(--color-surface)',
              color: 'var(--color-text-primary)',
              fontSize: '0.875rem',
              fontFamily: 'inherit',
            }}
          >
            <option value="name">الاسم (أ - ي)</option>
            <option value="profitDesc">الأعلى ربحاً</option>
            <option value="priceAsc">سعر البيع (الأقل للأعلى)</option>
            <option value="priceDesc">سعر البيع (الأعلى للأقل)</option>
            <option value="costAsc">سعر التكلفة (الأقل للأعلى)</option>
            <option value="costDesc">سعر التكلفة (الأعلى للأقل)</option>
          </select>
        </div>

        {/* View Mode Toggle Buttons */}
        <div
          style={{
            display: 'flex',
            backgroundColor: 'var(--color-surface-active)',
            padding: '3px',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--color-border)',
          }}
        >
          <button
            type="button"
            onClick={() => setViewMode('cards')}
            title="عرض البطاقات المصورة"
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              backgroundColor: viewMode === 'cards' ? 'var(--color-surface)' : 'transparent',
              color: viewMode === 'cards' ? 'var(--color-primary)' : 'var(--color-text-muted)',
              display: 'flex',
              alignItems: 'center',
              boxShadow: viewMode === 'cards' ? 'var(--shadow-sm)' : 'none',
            }}
          >
            <LayoutGrid size={18} />
          </button>
          <button
            type="button"
            onClick={() => setViewMode('table')}
            title="عرض الجدول التفصيلي"
            style={{
              padding: '6px 10px',
              borderRadius: 'var(--radius-sm)',
              backgroundColor: viewMode === 'table' ? 'var(--color-surface)' : 'transparent',
              color: viewMode === 'table' ? 'var(--color-primary)' : 'var(--color-text-muted)',
              display: 'flex',
              alignItems: 'center',
              boxShadow: viewMode === 'table' ? 'var(--shadow-sm)' : 'none',
            }}
          >
            <List size={18} />
          </button>
        </div>
      </div>

      {/* Content Area */}
      {isLoading ? (
        <div style={{ padding: '60px', textAlign: 'center' }}>
          <LoadingSpinner size="lg" />
        </div>
      ) : filteredAndSortedProducts.length === 0 ? (
        <EmptyState
          title="لم يتم العثور على أي منتجات"
          description={searchQuery ? 'لا توجد منتجات تطابق الفلاتر المحددة.' : 'ابدأ بإضافة أول صنف لقائمة الطعام.'}
          icon={<Package size={36} />}
          actionText={searchQuery ? undefined : 'إضافة منتج جديد'}
          onAction={canManage ? handleOpenCreate : undefined}
        />
      ) : viewMode === 'cards' ? (
        /* Visual Cards Grid View */
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))',
            gap: '18px',
          }}
        >
          {filteredAndSortedProducts.map((prod) => {
            const profit = prod.profit ?? ((prod.salePrice || 0) - (prod.costPrice || 0));
            const categoryName = prod.categoryName || categories.find((c) => c.id === prod.categoryId)?.name || 'عام';

            return (
              <div
                key={prod.id}
                style={{
                  backgroundColor: 'var(--color-surface)',
                  borderRadius: 'var(--radius-lg)',
                  border: '1px solid var(--color-border)',
                  overflow: 'hidden',
                  boxShadow: 'var(--shadow-card)',
                  display: 'flex',
                  flexDirection: 'column',
                  transition: 'all var(--transition-fast)',
                }}
              >
                {/* Product Image */}
                <div
                  style={{
                    height: '150px',
                    width: '100%',
                    backgroundColor: 'var(--color-surface-active)',
                    position: 'relative',
                    overflow: 'hidden',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  <ImageIcon size={42} style={{ opacity: 0.35, position: 'absolute' }} />
                  {prod.imageUrl ? (
                    <img
                      src={getFullImageUrl(prod.imageUrl)}
                      alt={prod.name}
                      style={{
                        width: '100%',
                        height: '100%',
                        objectFit: 'cover',
                        position: 'relative',
                        zIndex: 1,
                      }}
                      onError={(e) => {
                        (e.target as HTMLElement).style.display = 'none';
                      }}
                    />
                  ) : null}

                  {/* Availability Badge */}
                  <div style={{ position: 'absolute', top: '10px', right: '10px' }}>
                    {prod.isAvailable !== false ? (
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '4px',
                          padding: '3px 8px',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: 'rgba(16, 185, 129, 0.9)',
                          color: '#ffffff',
                          fontSize: '0.6875rem',
                          fontWeight: 700,
                        }}
                      >
                        <CheckCircle2 size={12} />
                        متاح
                      </span>
                    ) : (
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '4px',
                          padding: '3px 8px',
                          borderRadius: 'var(--radius-full)',
                          backgroundColor: 'rgba(239, 68, 68, 0.9)',
                          color: '#ffffff',
                          fontSize: '0.6875rem',
                          fontWeight: 700,
                        }}
                      >
                        <XCircle size={12} />
                        غير متوفر
                      </span>
                    )}
                  </div>
                </div>

                {/* Card Body */}
                <div style={{ padding: '16px', display: 'flex', flexDirection: 'column', flex: 1, gap: '10px' }}>
                  <div>
                    <span style={{ fontSize: '0.75rem', color: 'var(--color-primary)', fontWeight: 600 }}>
                      {categoryName}
                    </span>
                    <h3 style={{ fontSize: '1.0625rem', fontWeight: 700, margin: '2px 0 0 0' }}>
                      {prod.name}
                    </h3>
                  </div>

                  {/* Pricing Details */}
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      marginTop: 'auto',
                      backgroundColor: 'var(--color-surface-active)',
                      padding: '8px 12px',
                      borderRadius: 'var(--radius-md)',
                    }}
                  >
                    <div>
                      <span style={{ fontSize: '0.6875rem', color: 'var(--color-text-muted)' }}>سعر البيع</span>
                      <div style={{ fontSize: '1.125rem', fontWeight: 800, color: 'var(--color-success-text)' }}>
                        {(prod.salePrice || 0).toFixed(2)} د.ل
                      </div>
                    </div>

                    <div style={{ textAlign: 'left' }}>
                      <span style={{ fontSize: '0.6875rem', color: 'var(--color-text-muted)' }}>الربح</span>
                      <div style={{ fontSize: '0.875rem', fontWeight: 700, color: 'var(--color-primary)' }}>
                        {prod.canViewCosts === false ? 'محجوب' : `${(profit || 0).toFixed(2)} د.ل`}
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  <div style={{ display: 'flex', gap: '8px', paddingTop: '8px', borderTop: '1px solid var(--color-border-light)' }}>
                    <Button
                      variant="secondary"
                      size="sm"
                      disabled={!canManage} onClick={() => handleOpenEdit(prod)}
                      style={{ flex: 1 }}
                      icon={<Edit2 size={14} />}
                    >
                      تعديل
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      disabled={!canManage} onClick={() => setDeleteTarget(prod)}
                      style={{ padding: '6px 10px' }}
                      icon={<Trash2 size={14} />}
                    />
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        /* Detailed Table View */
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            overflow: 'hidden',
            boxShadow: 'var(--shadow-card)',
          }}
        >
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right' }}>
            <thead>
              <tr style={{ backgroundColor: 'var(--color-surface-active)', borderBottom: '1px solid var(--color-border)' }}>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الصورة</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>اسم المنتج</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>القسم</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>سعر البيع</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>سعر التكلفة</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الربح</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700 }}>الحالة</th>
                <th style={{ padding: '14px 16px', fontSize: '0.875rem', fontWeight: 700, textAlign: 'left' }}>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredAndSortedProducts.map((prod) => {
                const profit = prod.profit ?? prod.salePrice - prod.costPrice;
                const categoryName = prod.categoryName || categories.find((c) => c.id === prod.categoryId)?.name || 'عام';

                return (
                  <tr
                    key={prod.id}
                    style={{
                      borderBottom: '1px solid var(--color-border-light)',
                      transition: 'background-color var(--transition-fast)',
                    }}
                  >
                    <td style={{ padding: '12px 16px' }}>
                      <div
                        style={{
                          width: '44px',
                          height: '44px',
                          borderRadius: '8px',
                          backgroundColor: 'var(--color-surface-active)',
                          overflow: 'hidden',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <ImageIcon size={20} style={{ opacity: 0.35, position: 'absolute' }} />
                        {prod.imageUrl ? (
                          <img
                            src={getFullImageUrl(prod.imageUrl)}
                            alt={prod.name}
                            onError={(e) => {
                              (e.target as HTMLElement).style.display = 'none';
                            }}
                            style={{ width: '100%', height: '100%', objectFit: 'cover', position: 'relative', zIndex: 1 }}
                          />
                        ) : null}
                      </div>
                    </td>

                    <td style={{ padding: '12px 16px', fontWeight: 700 }}>
                      {prod.name}
                      {(prod.barcode || prod.barCode) && (
                        <div style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                          <Barcode size={13} /> {prod.barcode || prod.barCode}
                        </div>
                      )}
                    </td>

                    <td style={{ padding: '12px 16px', color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                      {categoryName}
                    </td>

                    <td style={{ padding: '12px 16px', fontWeight: 800, color: 'var(--color-success-text)' }}>
                      {(prod.salePrice || 0).toFixed(2)} د.ل
                    </td>

                    <td style={{ padding: '12px 16px', color: 'var(--color-text-muted)', fontSize: '0.875rem' }}>
                      {prod.canViewCosts === false ? 'محجوب' : `${(prod.costPrice || 0).toFixed(2)} د.ل`}
                    </td>

                    <td style={{ padding: '12px 16px', fontWeight: 700, color: 'var(--color-primary)' }}>
                      {prod.canViewCosts === false ? 'محجوب' : `${(profit || 0).toFixed(2)} د.ل`}
                    </td>

                    <td style={{ padding: '12px 16px' }}>
                      {prod.isAvailable !== false ? (
                        <span style={{ color: 'var(--color-success-text)', fontSize: '0.8125rem', fontWeight: 600 }}>متاح</span>
                      ) : (
                        <span style={{ color: 'var(--color-danger-text)', fontSize: '0.8125rem', fontWeight: 600 }}>غير متوفر</span>
                      )}
                    </td>

                    <td style={{ padding: '12px 16px', textAlign: 'left' }}>
                      <div style={{ display: 'inline-flex', gap: '6px' }}>
                        <Button variant="secondary" size="sm" disabled={!canManage} onClick={() => handleOpenEdit(prod)} icon={<Edit2 size={14} />} />
                        <Button variant="danger" size="sm" disabled={!canManage} onClick={() => setDeleteTarget(prod)} icon={<Trash2 size={14} />} />
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Create / Edit Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingProduct ? 'تعديل بيانات المنتج' : 'إضافة منتج جديد'}
        maxWidth="600px"
      >
        <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {formError && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '10px 14px',
                borderRadius: 'var(--radius-md)',
                backgroundColor: 'var(--color-danger-bg)',
                color: 'var(--color-danger-text)',
                fontSize: '0.875rem',
              }}
            >
              <span>{formError}</span>
            </div>
          )}

          {/* Product Name */}
          <Input
            label="اسم المنتج *"
            placeholder="مثال: بيتزا مارجريتا، شاورما دجاج، عصير ليمون..."
            value={formName}
            onChange={(e) => setFormName(e.target.value)}
            autoFocus
            required
          />

          <Input
            label="وصف المنتج (اختياري)"
            value={formDescription}
            onChange={(event) => setFormDescription(event.target.value)}
            maxLength={500}
          />

          {/* Category Dropdown */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.875rem', fontWeight: 600 }}>التصنيف (القسم) *</label>
            <select
              value={formCategoryId}
              onChange={(e) => setFormCategoryId(Number(e.target.value))}
              style={{
                padding: '10px 14px',
                borderRadius: 'var(--radius-md)',
                border: '1.5px solid var(--color-border)',
                backgroundColor: 'var(--color-surface)',
                color: 'var(--color-text-primary)',
                fontSize: '0.9375rem',
                fontFamily: 'inherit',
              }}
              required
            >
              <option value="" disabled>-- اختر تصنيف المنتج --</option>
              {categories.map((cat) => (
                <option key={cat.id} value={cat.id}>
                  {cat.name}
                </option>
              ))}
            </select>
          </div>

          {/* Pricing Row: Sale Price & Cost Price */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <Input
              label="سعر البيع (د.ل) *"
              type="number"
              step="0.01"
              min="0"
              value={formSalePrice}
              onChange={(e) => setFormSalePrice(e.target.value)}
              icon={<DollarSign size={16} />}
              required
            />
            <Input
              label="سعر التكلفة (د.ل)"
              type="number"
              step="0.01"
              min="0"
              value={formCostPrice}
              onChange={(e) => setFormCostPrice(e.target.value)}
              icon={<DollarSign size={16} />}
            />
          </div>

          {/* Live Profit Card */}
          <div
            style={{
              padding: '12px 16px',
              borderRadius: 'var(--radius-md)',
              backgroundColor: 'var(--color-primary-bg)',
              border: '1px solid var(--color-primary-lighter)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--color-success-text)', fontWeight: 700 }}>
              <TrendingUp size={20} />
              <span>صافي ربح الصنف:</span>
            </div>
            <div style={{ textAlign: 'left' }}>
              <span style={{ fontSize: '1.1875rem', fontWeight: 800, color: 'var(--color-success-text)' }}>
                {(calculatedProfit || 0).toFixed(2)} د.ل
              </span>
              <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)', marginRight: '8px' }}>
                ({(calculatedProfitMargin || 0).toFixed(1)}%)
              </span>
            </div>
          </div>

          {/* Barcode */}
          <Input
            label="الباركود أو الرمز (اختياري)"
            placeholder="مثال: 6281001234567"
            value={formBarcode}
            onChange={(e) => setFormBarcode(e.target.value)}
            icon={<Barcode size={18} />}
          />

          {/* Image Upload Row */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '0.875rem', fontWeight: 600 }}>صورة المنتج</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
              {formImageUrl && (
                <div
                  style={{
                    width: '60px',
                    height: '60px',
                    borderRadius: '8px',
                    overflow: 'hidden',
                    border: '1px solid var(--color-border)',
                    flexShrink: 0,
                  }}
                >
                  <img src={getFullImageUrl(formImageUrl)} alt="معاينة" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                </div>
              )}
              <label
                style={{
                  flex: 1,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '8px',
                  padding: '12px',
                  borderRadius: 'var(--radius-md)',
                  border: '1.5px dashed var(--color-border)',
                  backgroundColor: 'var(--color-surface-active)',
                  cursor: isUploadingImage ? 'wait' : 'pointer',
                  fontSize: '0.875rem',
                  fontWeight: 600,
                  color: 'var(--color-text-secondary)',
                }}
              >
                {isUploadingImage ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  <>
                    <Upload size={18} />
                    <span>{formImageUrl ? 'تغيير صورة الصنف' : 'رفع صورة من جهازك'}</span>
                  </>
                )}
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleImageChange}
                  disabled={isUploadingImage}
                  style={{ display: 'none' }}
                />
              </label>
            </div>
          </div>

          {/* Availability Toggle */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '6px' }}>
            <input
              type="checkbox"
              id="isAvailable"
              checked={formIsAvailable}
              onChange={(e) => setFormIsAvailable(e.target.checked)}
              style={{ width: '18px', height: '18px', accentColor: 'var(--color-primary)', cursor: 'pointer' }}
            />
            <label htmlFor="isAvailable" style={{ fontSize: '0.9375rem', fontWeight: 600, cursor: 'pointer' }}>
              الصنف متوفر للطلب في قائمة الطعام
            </label>
          </div>

          {/* Form Actions */}
          <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '16px' }}>
            <Button variant="secondary" onClick={() => setIsModalOpen(false)} disabled={isSubmitting}>
              إلغاء
            </Button>
            <Button variant="primary" type="submit" isLoading={isSubmitting}>
              {editingProduct ? 'حفظ التعديلات' : 'إضافة المنتج'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDeleteConfirm}
        title="حذف المنتج"
        message={`هل أنت متأكد من رغبتك في حذف المنتج "${deleteTarget?.name}" بالكامل من قائمة الطعام؟`}
        confirmText="نعم، احذف المنتج"
        variant="danger"
        isLoading={isDeleting}
      />
    </div>
  );
};

export default ProductsPage;
