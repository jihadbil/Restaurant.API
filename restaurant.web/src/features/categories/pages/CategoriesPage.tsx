import React, { useState, useEffect, useCallback } from 'react';
import {
  Layers,
  Plus,
  Search,
  Printer,
  Edit2,
  Trash2,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import Modal from '../../../components/ui/Modal';
import ConfirmDialog from '../../../components/ui/ConfirmDialog';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import apiClient from '../../../services/apiClient';
import type { CategoryDto, PrintStationDto } from '../../../types/api';
import toast from 'react-hot-toast';
import { useAuth } from '../../../hooks/useAuth';

export const CategoriesPage: React.FC = () => {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [printStations, setPrintStations] = useState<PrintStationDto[]>([]);
  const { hasPermission } = useAuth();
  const canManage = hasPermission('Permission.Categories.Manage');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(null);
  const [formName, setFormName] = useState<string>('');
  const [selectedStationIds, setSelectedStationIds] = useState<number[]>([]);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Delete State
  const [deleteTarget, setDeleteTarget] = useState<CategoryDto | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  // Load Categories & Print Stations
  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [catsRes, stationsRes] = await Promise.all([
        apiClient.get<CategoryDto[]>('/api/categories'),
        apiClient.get<PrintStationDto[]>('/api/printstations'),
      ]);

      setPrintStations(stationsRes || []);

      // Preserve the complete routing set when editing a category.
      const enrichedCategories = await Promise.all(
        (catsRes || []).map(async (cat) => {
          const linkedStations = await apiClient.get<PrintStationDto[]>(
            `/api/CategoryPrintStations/category/${cat.id}`
          );
          return { ...cat, printStationIds: linkedStations.map(station => station.id),
            printStationName: linkedStations.map(station => station.name).join('، ') };
        })
      );

      setCategories(enrichedCategories);
    } catch {
      setCategories([]);
      toast.error('فشل تحميل قائمة التصنيفات ومحطات الطباعة.');
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
    setEditingCategory(null);
    setFormName('');
    setSelectedStationIds([]);
    setFormError(null);
    setIsModalOpen(true);
  };

  // Open Edit Modal
  const handleOpenEdit = (cat: CategoryDto) => {
    setEditingCategory(cat);
    setFormName(cat.name);
    setSelectedStationIds(cat.printStationIds ?? []);
    setFormError(null);
    setIsModalOpen(true);
  };

  // Handle Form Submit
  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName.trim()) {
      setFormError('يرجى إدخال اسم التصنيف.');
      return;
    }

    setFormError(null);
    setIsSubmitting(true);
    try {
      let savedCategoryId = editingCategory?.id;

      if (editingCategory) {
        // Update Category
        await apiClient.put(`/api/categories/${editingCategory.id}`, {
          id: editingCategory.id,
          name: formName.trim(),
        });
      } else {
        // Create Category
        const created = await apiClient.post<CategoryDto>('/api/categories', {
          name: formName.trim(),
        });
        savedCategoryId = created.id;
        // A retry after a routing failure must edit this category instead of creating another.
        setEditingCategory(created);
      }

      // Replace the complete set in a single server transaction.
      if (savedCategoryId) {
        await apiClient.put(`/api/CategoryPrintStations/category/${savedCategoryId}`, {
          printStationIds: selectedStationIds,
        });
      }

      toast.success(editingCategory ? 'تم تحديث التصنيف بنجاح!' : 'تم إنشاء التصنيف الجديد بنجاح!');
      setIsModalOpen(false);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'حدث خطأ أثناء حفظ التصنيف.';
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
      await apiClient.delete(`/api/categories/${deleteTarget.id}`);
      toast.success(`تم حذف التصنيف "${deleteTarget.name}" بنجاح.`);
      setDeleteTarget(null);
      loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل حذف التصنيف. قد يحتوي على منتجات مرتبطة به.';
      toast.error(msg);
    } finally {
      setIsDeleting(false);
    }
  };

  // Filter categories by search
  const filteredCategories = categories.filter((cat) =>
    cat.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

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
            تصنيفات المأكولات والمشروبات
          </h2>
          <p style={{ color: 'var(--color-text-muted)', fontSize: '0.875rem', marginTop: '4px' }}>
            إدارة أقسام قائمة الطعام وتوجيه طباعة تذاكر التحضير لمحطات المطبخ والبار
          </p>
        </div>

        <Button
          variant="primary"
          size="lg"
          disabled={!canManage} onClick={handleOpenCreate}
          icon={<Plus size={20} />}
        >
          إضافة تصنيف جديد
        </Button>
      </div>

      {/* Filter & Search Bar */}
      <div
        style={{
          backgroundColor: 'var(--color-surface)',
          padding: '16px 20px',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '16px',
        }}
      >
        <div style={{ maxWidth: '400px', width: '100%' }}>
          <Input
            placeholder="البحث عن تصنيف..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            icon={<Search size={18} />}
          />
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--color-text-muted)', fontSize: '0.875rem' }}>
          <span>إجمالي التصنيفات:</span>
          <span style={{ fontWeight: 700, color: 'var(--color-primary)' }}>{categories.length}</span>
        </div>
      </div>

      {/* Categories Grid / Table */}
      {isLoading ? (
        <div style={{ padding: '60px', textAlign: 'center' }}>
          <LoadingSpinner size="lg" />
        </div>
      ) : filteredCategories.length === 0 ? (
        <EmptyState
          title="لم يتم العثور على أي تصنيفات"
          description={searchQuery ? 'لا توجد نتائج تطابق بحثك الحالي.' : 'ابدأ بإضافة أول تصنيف لقائمة طعامك.'}
          icon={<Layers size={36} />}
          actionText={searchQuery ? undefined : 'إضافة تصنيف جديد'}
          onAction={canManage ? handleOpenCreate : undefined}
        />
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))',
            gap: '16px',
          }}
        >
          {filteredCategories.map((cat) => (
            <div
              key={cat.id}
              style={{
                backgroundColor: 'var(--color-surface)',
                borderRadius: 'var(--radius-lg)',
                border: '1px solid var(--color-border)',
                padding: '20px',
                boxShadow: 'var(--shadow-card)',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between',
                gap: '14px',
                transition: 'all var(--transition-fast)',
              }}
            >
              <div>
                {/* Header: Name & Icon */}
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '8px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <div
                      style={{
                        width: '40px',
                        height: '40px',
                        borderRadius: '10px',
                        backgroundColor: 'var(--color-primary-bg)',
                        color: 'var(--color-primary)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      <Layers size={22} />
                    </div>
                    <h3 style={{ fontSize: '1.1875rem', fontWeight: 700, margin: 0 }}>
                      {cat.name}
                    </h3>
                  </div>
                </div>

                {/* Print Station Routing Badge */}
                <div style={{ marginTop: '12px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <Printer size={16} style={{ color: 'var(--color-text-muted)' }} />
                  <span style={{ fontSize: '0.8125rem', color: 'var(--color-text-muted)' }}>محطة الطباعة:</span>
                  {cat.printStationName ? (
                    <span
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '4px',
                        padding: '3px 10px',
                        borderRadius: 'var(--radius-full)',
                        backgroundColor: 'var(--color-info-bg)',
                        color: 'var(--color-info-text)',
                        fontSize: '0.8125rem',
                        fontWeight: 600,
                      }}
                    >
                      <CheckCircle2 size={14} />
                      {cat.printStationName}
                    </span>
                  ) : (
                    <span
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        padding: '3px 10px',
                        borderRadius: 'var(--radius-full)',
                        backgroundColor: 'var(--color-surface-active)',
                        color: 'var(--color-text-muted)',
                        fontSize: '0.8125rem',
                      }}
                    >
                      غير مربوطة بمحطة
                    </span>
                  )}
                </div>
              </div>

              {/* Bottom Actions */}
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'flex-end',
                  gap: '8px',
                  borderTop: '1px solid var(--color-border-light)',
                  paddingTop: '12px',
                }}
              >
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={!canManage} onClick={() => handleOpenEdit(cat)}
                  icon={<Edit2 size={15} />}
                >
                  تعديل
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  disabled={!canManage} onClick={() => setDeleteTarget(cat)}
                  icon={<Trash2 size={15} />}
                >
                  حذف
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create / Edit Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingCategory ? 'تعديل التصنيف' : 'إضافة تصنيف جديد'}
        maxWidth="500px"
      >
        <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
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
              <AlertCircle size={18} />
              <span>{formError}</span>
            </div>
          )}

          <Input
            label="اسم التصنيف *"
            placeholder="مثال: بيتزا، مشويات، مشروبات باردة..."
            value={formName}
            onChange={(e) => setFormName(e.target.value)}
            autoFocus
            required
          />

          {/* Multiple stations are a single routing configuration. */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <fieldset style={{ border: '1px solid var(--color-border)', borderRadius: 'var(--radius-md)', padding: '12px' }}>
              <legend>محطات طباعة التصنيف (اختياري)</legend>
              {printStations.map(station => (
                <label key={station.id} style={{ display: 'flex', gap: '8px', padding: '6px' }}>
                  <input type="checkbox" checked={selectedStationIds.includes(station.id)}
                    onChange={event => setSelectedStationIds(previous => event.target.checked
                      ? [...previous, station.id] : previous.filter(id => id !== station.id))} />
                  {station.name}
                </label>
              ))}
              <button type="button" onClick={() => setSelectedStationIds([])}>
                بدون محطة طباعة مخصصة
              </button>
            </fieldset>
            <span style={{ fontSize: '0.75rem', color: 'var(--color-text-muted)' }}>
              اختر محطة أو أكثر. ستُرسل تذاكر الأصناف التابعة للتصنيف إلى جميع المحطات المحددة.
            </span>
          </div>

          <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '14px' }}>
            <Button variant="secondary" onClick={() => setIsModalOpen(false)} disabled={isSubmitting}>
              إلغاء
            </Button>
            <Button variant="primary" type="submit" isLoading={isSubmitting}>
              {editingCategory ? 'حفظ التعديلات' : 'إضافة التصنيف'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDeleteConfirm}
        title="حذف التصنيف"
        message={`هل أنت متأكد من رغبتك في حذف التصنيف "${deleteTarget?.name}"؟ لا يمكن التراجع عن هذا الإجراء.`}
        confirmText="نعم، احذف التصنيف"
        variant="danger"
        isLoading={isDeleting}
      />
    </div>
  );
};

export default CategoriesPage;
