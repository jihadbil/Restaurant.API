import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  UtensilsCrossed,
  Search,
  Plus,
  Minus,
  Trash2,
  Edit3,
  CreditCard,
  Banknote,
  RotateCcw,
  ShoppingBag,
} from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import ConfirmDialog from '../../../components/ui/ConfirmDialog';
import LoadingSpinner from '../../../components/ui/LoadingSpinner';
import EmptyState from '../../../components/ui/EmptyState';
import ExcludeAddonsModal from '../components/ExcludeAddonsModal';
import ReceiptPrintTemplate from '../../printing/ReceiptPrintTemplate';
import printService from '../../../services/printService';
import { useAuth } from '../../../hooks/useAuth';
import apiClient from '../../../services/apiClient';
import type {
  ProductDto,
  CategoryDto,
  PaymentMethodDto,
  AddonDto,
  OrderCheckoutCreateDto,
  OrderDto,
  CashboxDto,
} from '../../../types/api';
import { OrderType, OrderStatus } from '../../../types/api';
import { getFullImageUrl } from '../../../utils/imageUtils';
import toast from 'react-hot-toast';

export interface CartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  unitCostPrice: number;
  imageUrl?: string | null;
  quantity: number;
  notes: string;
  lineTotal: number;
}

export const PosPage: React.FC = () => {
  // Master Data
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethodDto[]>([]);
  const [addons, setAddons] = useState<AddonDto[]>([]);
  const [cashboxes, setCashboxes] = useState<CashboxDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Filters & State
  const [selectedCategory, setSelectedCategory] = useState<number | 'all'>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Cart State
  const [cartItems, setCartItems] = useState<CartItem[]>([]);
  const [orderType, setOrderType] = useState<OrderType>(OrderType.Indoor);
  const [orderNumber, setOrderNumber] = useState<number>(() => Math.floor(1000 + Math.random() * 90000));
  const [discount, setDiscount] = useState<string>('0');
  const [orderNotes, setOrderNotes] = useState<string>('لا يوجد ملاحظات');
  const [isClearCartConfirmOpen, setIsClearCartConfirmOpen] = useState<boolean>(false);

  // Addon Customization Modal State
  const [editingCartItemIndex, setEditingCartItemIndex] = useState<number | null>(null);

  // Printing & Checkout State
  const [isCheckingOut, setIsCheckingOut] = useState<boolean>(false);
  const [lastPrintedOrder, setLastPrintedOrder] = useState<OrderDto | null>(null);

  const { user, hasPermission } = useAuth();
  const canApplyDiscount = hasPermission('Permission.POS.ApplyDiscount');

  // Load Data
  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [catsRes, prodsRes, pmsRes, addonsRes, cashboxesRes] = await Promise.all([
        apiClient.get<CategoryDto[]>('/api/categories').catch(() => []),
        apiClient.get<ProductDto[]>('/api/products').catch(() => []),
        apiClient.get<PaymentMethodDto[]>('/api/paymentmethods').catch(() => []),
        apiClient.get<AddonDto[]>('/api/addons').catch(() => []),
        apiClient.get<CashboxDto[]>('/api/cashboxes').catch(() => []),
      ]);

      setCategories(catsRes || []);
      setProducts(prodsRes || []);
      setPaymentMethods(pmsRes || []);
      setAddons(addonsRes || []);
      setCashboxes(cashboxesRes || []);
    } catch {
      toast.error('حدث خطأ أثناء تحميل بيانات نقطة البيع.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Keyboard shortcut Esc to clear cart
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && cartItems.length > 0) {
        setIsClearCartConfirmOpen(true);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [cartItems.length]);

  // Cart operations
  const addToCart = (product: ProductDto) => {
    if (product.isAvailable === false) {
      toast.error('هذا الصنف غير متوفر حالياً.');
      return;
    }

    setCartItems((prev) => {
      const existingIndex = prev.findIndex((item) => item.productId === product.id);
      if (existingIndex >= 0) {
        const updated = [...prev];
        const item = updated[existingIndex];
        const newQty = item.quantity + 1;
        updated[existingIndex] = {
          ...item,
          quantity: newQty,
          lineTotal: newQty * item.unitPrice,
        };
        return updated;
      } else {
        return [
          ...prev,
          {
            productId: product.id,
            productName: product.name,
            unitPrice: product.salePrice,
            unitCostPrice: product.costPrice,
            imageUrl: product.imageUrl,
            quantity: 1,
            notes: 'لا يوجد ملاحظات',
            lineTotal: product.salePrice,
          },
        ];
      }
    });
  };

  const increaseQuantity = (index: number) => {
    setCartItems((prev) => {
      const updated = [...prev];
      const item = updated[index];
      const newQty = item.quantity + 1;
      updated[index] = { ...item, quantity: newQty, lineTotal: newQty * item.unitPrice };
      return updated;
    });
  };

  const decreaseQuantity = (index: number) => {
    setCartItems((prev) => {
      const updated = [...prev];
      const item = updated[index];
      if (item.quantity > 1) {
        const newQty = item.quantity - 1;
        updated[index] = { ...item, quantity: newQty, lineTotal: newQty * item.unitPrice };
        return updated;
      } else {
        return updated.filter((_, i) => i !== index);
      }
    });
  };

  const removeFromCart = (index: number) => {
    setCartItems((prev) => prev.filter((_, i) => i !== index));
  };

  const clearCart = () => {
    setCartItems([]);
    setDiscount('0');
    setOrderNotes('لا يوجد ملاحظات');
    setOrderNumber(Math.floor(1000 + Math.random() * 90000));
    setIsClearCartConfirmOpen(false);
  };

  // Calculations
  const subTotal = useMemo(() => {
    return cartItems.reduce((sum, item) => sum + item.lineTotal, 0);
  }, [cartItems]);

  const discountNum = parseFloat(discount) || 0;
  const netTotal = Math.max(0, subTotal - discountNum);

  // Filter products by category & search
  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      if (selectedCategory !== 'all' && p.categoryId !== selectedCategory) {
        return false;
      }
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase().trim();
        const bCode = p.barcode || p.barCode || '';
        return p.name.toLowerCase().includes(q) || bCode.toLowerCase().includes(q);
      }
      return true;
    });
  }, [products, selectedCategory, searchQuery]);

  // Checkout execution
  const handleCheckout = async (paymentMethodId: number) => {
    if (cartItems.length === 0) {
      toast.error('السلة فارغة. يرجى إضافة أصناف أولاً.');
      return;
    }

    if (!user?.id) {
      toast.error('انتهت جلسة تسجيل الدخول. يرجى إعادة الدخول.');
      return;
    }

    const defaultCashbox = cashboxes[0];
    if (!defaultCashbox) {
      toast.error('لم يتم العثور على خزينة نشطة في النظام. يرجى تهيئة الخزائن أولاً.');
      return;
    }

    setIsCheckingOut(true);
    try {
      const clientRequestId = `pos_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;

      const checkoutPayload: OrderCheckoutCreateDto = {
        order: {
          orderNumber,
          discount: discountNum,
          orderStatus: OrderStatus.Preparing,
          orderType,
          notes: orderNotes,
          userId: user.id,
          orderItems: cartItems.map((item) => ({
            productId: item.productId,
            quantity: item.quantity,
            unitSalePrice: item.unitPrice,
            unitCostPrice: item.unitCostPrice,
            unitDiscount: 0,
            notes: item.notes,
          })),
        },
        cashboxId: defaultCashbox.id,
        paymentMethodId,
        clientRequestId,
      };

      const createdOrder = await apiClient.post<OrderDto>('/api/orders/checkout', checkoutPayload);

      toast.success(`تم حفظ الطلب بنجاح! رقم الطلب: #${createdOrder.orderNumber || orderNumber}`);
      setLastPrintedOrder(createdOrder);

      // Trigger automatic print
      setTimeout(() => {
        printService.printOrder(createdOrder);
      }, 150);

      // Reset cart
      clearCart();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'فشل تسجيل الطلب وإنهاء البيع.';
      toast.error(msg);
    } finally {
      setIsCheckingOut(false);
    }
  };

  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: '1fr 390px',
        gap: '20px',
        height: 'calc(100vh - var(--header-height) - 48px)',
        minHeight: '580px',
      }}
    >
      {/* LEFT COLUMN: Categories & Products Catalog */}
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: '14px',
          overflow: 'hidden',
        }}
      >
        {/* Categories Bar & Search */}
        <div
          style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            padding: '12px 16px',
            display: 'flex',
            flexDirection: 'column',
            gap: '12px',
          }}
        >
          {/* Search Input */}
          <div style={{ maxWidth: '420px', width: '100%' }}>
            <Input
              placeholder="ابحث عن صنف أو امسح الباركود..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              icon={<Search size={18} />}
            />
          </div>

          {/* Category Pills (Horizontal Scrollable) */}
          <div
            style={{
              display: 'flex',
              gap: '8px',
              overflowX: 'auto',
              paddingBottom: '4px',
              scrollbarWidth: 'none',
            }}
          >
            <button
              type="button"
              onClick={() => setSelectedCategory('all')}
              style={{
                padding: '8px 18px',
                borderRadius: 'var(--radius-full)',
                backgroundColor: selectedCategory === 'all' ? 'var(--color-primary)' : 'var(--color-surface-active)',
                color: selectedCategory === 'all' ? '#ffffff' : 'var(--color-text-primary)',
                fontWeight: 700,
                fontSize: '0.875rem',
                whiteSpace: 'nowrap',
                transition: 'all var(--transition-fast)',
              }}
            >
              الكل ({products.length})
            </button>
            {categories.map((cat) => (
              <button
                key={cat.id}
                type="button"
                onClick={() => setSelectedCategory(cat.id)}
                style={{
                  padding: '8px 18px',
                  borderRadius: 'var(--radius-full)',
                  backgroundColor: selectedCategory === cat.id ? 'var(--color-primary)' : 'var(--color-surface-active)',
                  color: selectedCategory === cat.id ? '#ffffff' : 'var(--color-text-primary)',
                  fontWeight: 700,
                  fontSize: '0.875rem',
                  whiteSpace: 'nowrap',
                  transition: 'all var(--transition-fast)',
                }}
              >
                {cat.name}
              </button>
            ))}
          </div>
        </div>

        {/* Products Grid */}
        <div
          style={{
            flex: 1,
            overflowY: 'auto',
            paddingRight: '4px',
          }}
        >
          {isLoading ? (
            <div style={{ padding: '60px', textAlign: 'center' }}>
              <LoadingSpinner size="lg" />
            </div>
          ) : filteredProducts.length === 0 ? (
            <EmptyState
              title="لا توجد أصناف مطابقة"
              description="لم يتم العثور على أي وجبات أو مشروبات في هذا القسم."
              icon={<UtensilsCrossed size={36} />}
            />
          ) : (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(170px, 1fr))',
                gap: '14px',
              }}
            >
              {filteredProducts.map((prod) => (
                <div
                  key={prod.id}
                  onClick={() => addToCart(prod)}
                  style={{
                    backgroundColor: 'var(--color-surface)',
                    borderRadius: 'var(--radius-lg)',
                    border: '1.5px solid var(--color-border)',
                    overflow: 'hidden',
                    display: 'flex',
                    flexDirection: 'column',
                    cursor: prod.isAvailable !== false ? 'pointer' : 'not-allowed',
                    opacity: prod.isAvailable !== false ? 1 : 0.6,
                    transition: 'all var(--transition-fast)',
                    boxShadow: 'var(--shadow-card)',
                    position: 'relative',
                  }}
                >
                  {/* Product Image */}
                  <div
                    style={{
                      height: '115px',
                      backgroundColor: 'var(--color-surface-active)',
                      position: 'relative',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <UtensilsCrossed
                      size={32}
                      style={{
                        position: 'absolute',
                        opacity: 0.25,
                        color: 'var(--color-text-muted)',
                        pointerEvents: 'none',
                      }}
                    />
                    {prod.imageUrl ? (
                      <img
                        src={getFullImageUrl(prod.imageUrl)}
                        alt={prod.name}
                        onError={(e) => {
                          (e.target as HTMLElement).style.display = 'none';
                        }}
                        style={{
                          width: '100%',
                          height: '100%',
                          objectFit: 'cover',
                          position: 'relative',
                          zIndex: 1,
                        }}
                      />
                    ) : null}

                    {prod.isAvailable === false && (
                      <div
                        style={{
                          position: 'absolute',
                          inset: 0,
                          backgroundColor: 'rgba(0, 0, 0, 0.5)',
                          color: '#ffffff',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontWeight: 700,
                          fontSize: '0.8125rem',
                          zIndex: 2,
                        }}
                      >
                        غير متوفر
                      </div>
                    )}
                  </div>

                  {/* Product Info */}
                  <div style={{ padding: '10px 12px', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                    <div
                      style={{
                        fontSize: '0.9375rem',
                        fontWeight: 700,
                        color: 'var(--color-text-primary)',
                        whiteSpace: 'nowrap',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                      }}
                    >
                      {prod.name}
                    </div>

                    <div
                      style={{
                        fontSize: '1rem',
                        fontWeight: 800,
                        color: 'var(--color-success-text)',
                        marginTop: '2px',
                      }}
                    >
                      {(prod.salePrice || 0).toFixed(2)} د.ل
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* RIGHT COLUMN: POS Cart & Checkout Panel */}
      <div
        style={{
          backgroundColor: 'var(--color-surface)',
          borderRadius: 'var(--radius-lg)',
          border: '1px solid var(--color-border)',
          display: 'flex',
          flexDirection: 'column',
          boxShadow: 'var(--shadow-md)',
          overflow: 'hidden',
        }}
      >
        {/* Cart Header */}
        <div
          style={{
            padding: '14px 16px',
            borderBottom: '1px solid var(--color-border)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            backgroundColor: 'var(--color-surface-hover)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ShoppingBag size={20} style={{ color: 'var(--color-primary)' }} />
            <h3 style={{ fontSize: '1.0625rem', fontWeight: 800, margin: 0 }}>
              طلب #{orderNumber}
            </h3>
          </div>

          {/* Clear Cart Button */}
          {cartItems.length > 0 && (
            <button
              onClick={() => setIsClearCartConfirmOpen(true)}
              title="تفريغ السلة (Esc)"
              style={{
                padding: '4px 8px',
                borderRadius: 'var(--radius-sm)',
                color: 'var(--color-danger)',
                fontSize: '0.75rem',
                fontWeight: 700,
                display: 'flex',
                alignItems: 'center',
                gap: '4px',
              }}
            >
              <RotateCcw size={13} />
              <span>مسح</span>
            </button>
          )}
        </div>

        {/* Order Type Toggle: Indoor, Outdoor, Delivery */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr 1fr',
            gap: '4px',
            padding: '8px 12px',
            backgroundColor: 'var(--color-surface-active)',
            borderBottom: '1px solid var(--color-border)',
          }}
        >
          <button
            type="button"
            onClick={() => setOrderType(OrderType.Indoor)}
            style={{
              padding: '6px',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: orderType === OrderType.Indoor ? 'var(--color-surface)' : 'transparent',
              color: orderType === OrderType.Indoor ? 'var(--color-primary)' : 'var(--color-text-muted)',
              boxShadow: orderType === OrderType.Indoor ? 'var(--shadow-sm)' : 'none',
            }}
          >
            داخلي
          </button>
          <button
            type="button"
            onClick={() => setOrderType(OrderType.Outdoor)}
            style={{
              padding: '6px',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: orderType === OrderType.Outdoor ? 'var(--color-surface)' : 'transparent',
              color: orderType === OrderType.Outdoor ? 'var(--color-primary)' : 'var(--color-text-muted)',
              boxShadow: orderType === OrderType.Outdoor ? 'var(--shadow-sm)' : 'none',
            }}
          >
            سفري
          </button>
          <button
            type="button"
            onClick={() => setOrderType(OrderType.Delivery)}
            style={{
              padding: '6px',
              borderRadius: 'var(--radius-sm)',
              fontSize: '0.8125rem',
              fontWeight: 700,
              backgroundColor: orderType === OrderType.Delivery ? 'var(--color-surface)' : 'transparent',
              color: orderType === OrderType.Delivery ? 'var(--color-primary)' : 'var(--color-text-muted)',
              boxShadow: orderType === OrderType.Delivery ? 'var(--shadow-sm)' : 'none',
            }}
          >
            توصيل
          </button>
        </div>

        {/* Cart Items List */}
        <div
          style={{
            flex: 1,
            overflowY: 'auto',
            padding: '12px 14px',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}
        >
          {cartItems.length === 0 ? (
            <div style={{ margin: 'auto', textAlign: 'center', color: 'var(--color-text-muted)', padding: '20px 0' }}>
              <ShoppingBag size={40} style={{ opacity: 0.2, margin: '0 auto 8px auto' }} />
              <div style={{ fontSize: '0.9375rem', fontWeight: 600 }}>السلة فارغة</div>
              <div style={{ fontSize: '0.75rem', marginTop: '4px' }}>اختر أصنافاً من القائمة لإضافتها للطلب</div>
            </div>
          ) : (
            cartItems.map((item, index) => (
              <div
                key={index}
                style={{
                  backgroundColor: 'var(--color-surface-active)',
                  borderRadius: 'var(--radius-md)',
                  padding: '10px 12px',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '6px',
                }}
              >
                {/* Item Line Top: Name & Total */}
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ fontWeight: 700, fontSize: '0.875rem' }}>{item.productName}</div>
                  <div style={{ fontWeight: 800, fontSize: '0.9375rem', color: 'var(--color-text-primary)' }}>
                    {(item.lineTotal || 0).toFixed(2)} د.ل
                  </div>
                </div>

                {/* Notes Tag (Clickable to customize) */}
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <button
                    type="button"
                    onClick={() => setEditingCartItemIndex(index)}
                    title="تعديل الملاحظات واستبعاد الإضافات"
                    style={{
                      fontSize: '0.75rem',
                      color: item.notes !== 'لا يوجد ملاحظات' ? 'var(--color-warning-text)' : 'var(--color-text-muted)',
                      backgroundColor: item.notes !== 'لا يوجد ملاحظات' ? 'var(--color-warning-bg)' : 'transparent',
                      padding: '2px 6px',
                      borderRadius: '4px',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '4px',
                      fontWeight: 600,
                    }}
                  >
                    <Edit3 size={11} />
                    <span>{item.notes}</span>
                  </button>

                  {/* Quantity Stepper (+ / -) */}
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <button
                      type="button"
                      onClick={() => decreaseQuantity(index)}
                      style={{
                        width: '24px',
                        height: '24px',
                        borderRadius: 'var(--radius-sm)',
                        backgroundColor: 'var(--color-surface)',
                        color: 'var(--color-text-primary)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        border: '1px solid var(--color-border)',
                      }}
                    >
                      {item.quantity === 1 ? <Trash2 size={12} style={{ color: 'var(--color-danger)' }} /> : <Minus size={12} />}
                    </button>
                    <span style={{ fontSize: '0.875rem', fontWeight: 800, minWidth: '18px', textAlign: 'center' }}>
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      onClick={() => increaseQuantity(index)}
                      style={{
                        width: '24px',
                        height: '24px',
                        borderRadius: 'var(--radius-sm)',
                        backgroundColor: 'var(--color-primary-bg)',
                        color: 'var(--color-primary)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        border: '1px solid var(--color-primary-lighter)',
                      }}
                    >
                      <Plus size={12} />
                    </button>
                    <button
                      type="button"
                      onClick={() => removeFromCart(index)}
                      title="حذف الصنف"
                      style={{
                        padding: '4px',
                        borderRadius: 'var(--radius-sm)',
                        color: 'var(--color-text-muted)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        marginRight: '4px',
                      }}
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Calculation & Checkout Footer */}
        <div
          style={{
            padding: '14px 16px',
            borderTop: '1px solid var(--color-border)',
            backgroundColor: 'var(--color-surface)',
            display: 'flex',
            flexDirection: 'column',
            gap: '10px',
          }}
        >
          {/* Subtotal */}
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', color: 'var(--color-text-secondary)' }}>
            <span>المجموع الفرعي:</span>
            <span>{(subTotal || 0).toFixed(2)} د.ل</span>
          </div>

          {/* Discount input (Permission guarded) */}
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '10px' }}>
            <span style={{ fontSize: '0.875rem', color: 'var(--color-text-secondary)', whiteSpace: 'nowrap' }}>
              الخصم (د.ل):
            </span>
            <input
              type="number"
              min="0"
              step="0.5"
              value={discount}
              onChange={(e) => setDiscount(e.target.value)}
              disabled={!canApplyDiscount || cartItems.length === 0}
              placeholder="0.00"
              style={{
                width: '90px',
                padding: '4px 8px',
                borderRadius: 'var(--radius-sm)',
                border: '1px solid var(--color-border)',
                textAlign: 'center',
                fontWeight: 700,
                fontSize: '0.875rem',
                backgroundColor: canApplyDiscount ? 'var(--color-surface)' : 'var(--color-surface-active)',
              }}
            />
          </div>

          {/* Net Total */}
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              borderTop: '1.5px dashed var(--color-border)',
              paddingTop: '8px',
            }}
          >
            <span style={{ fontSize: '1rem', fontWeight: 800 }}>الصافي النهائي:</span>
            <span style={{ fontSize: '1.375rem', fontWeight: 900, color: 'var(--color-success-text)' }}>
              {(netTotal || 0).toFixed(2)} د.ل
            </span>
          </div>

          {/* Fast Payment Buttons */}
          <div style={{ display: 'grid', gridTemplateColumns: paymentMethods.length > 1 ? '1fr 1fr' : '1fr', gap: '8px', marginTop: '6px' }}>
            {paymentMethods.length > 0 ? (
              paymentMethods.map((pm) => (
                <Button
                  key={pm.id}
                  variant="primary"
                  size="md"
                  onClick={() => handleCheckout(pm.id)}
                  disabled={cartItems.length === 0 || isCheckingOut}
                  isLoading={isCheckingOut}
                  icon={pm.name.includes('كاش') || pm.name.includes('نقدي') ? <Banknote size={16} /> : <CreditCard size={16} />}
                >
                  {pm.name}
                </Button>
              ))
            ) : (
              <Button
                variant="primary"
                size="md"
                onClick={() => handleCheckout(1)}
                disabled={cartItems.length === 0 || isCheckingOut}
                isLoading={isCheckingOut}
                icon={<Banknote size={16} />}
              >
                دفع نقدي (كاش)
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Exclude / Customize Addons Modal */}
      {editingCartItemIndex !== null && (
        <ExcludeAddonsModal
          isOpen={editingCartItemIndex !== null}
          onClose={() => setEditingCartItemIndex(null)}
          onSave={(notes) => {
            setCartItems((prev) => {
              const updated = [...prev];
              updated[editingCartItemIndex] = { ...updated[editingCartItemIndex], notes };
              return updated;
            });
          }}
          productName={cartItems[editingCartItemIndex]?.productName || ''}
          currentNotes={cartItems[editingCartItemIndex]?.notes || ''}
          availableAddons={addons}
        />
      )}

      {/* Clear Cart Confirmation Dialog */}
      <ConfirmDialog
        isOpen={isClearCartConfirmOpen}
        onClose={() => setIsClearCartConfirmOpen(false)}
        onConfirm={clearCart}
        title="تأكيد مسح السلة"
        message="هل أنت متأكد من رغبتك في تفريغ كافة العناصر في السلة وتصفير الطلب؟"
        confirmText="نعم، أفرغ السلة"
        variant="danger"
      />

      {/* Hidden printable receipt (Only displayed during window.print()) */}
      {lastPrintedOrder && (
        <div style={{ display: 'none' }} className="print-only">
          <ReceiptPrintTemplate order={lastPrintedOrder} />
        </div>
      )}
    </div>
  );
};

export default PosPage;
