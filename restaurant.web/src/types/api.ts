/**
 * API Data Transfer Objects & Domain Models
 * Matches Restaurant.Models & Restaurant.Models.DTOs (.NET 10)
 */

// ==========================================
// Enums (Const Objects + Types for erasableSyntaxOnly)
// ==========================================

export const OrderStatus = {
  Preparing: 0, // قيد التحضير
  Ready: 1,     // جاهزة
  Delivered: 2, // تم التسليم
  Cancelled: 3, // ملغاة
} as const;
export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

export const OrderType = {
  Indoor: 0,   // داخل المطعم
  Outdoor: 1,  // خارج المطعم (سفري)
  Delivery: 2, // توصيل
} as const;
export type OrderType = (typeof OrderType)[keyof typeof OrderType];

export const CashDrawerEntryType = {
  SalePayment: 1, // دفع مبيعات
  Inflow: 2,      // إيداع يدوي
  Outflow: 3,     // سحب يدوي
  Refund: 4,      // رد مبيعات
  TransferIn: 5,
  TransferOut: 6,
  CashShortage: 7,
  CashSurplus: 8,
} as const;
export type CashDrawerEntryType = (typeof CashDrawerEntryType)[keyof typeof CashDrawerEntryType];

export const PrinterType = {
  Receipt: 0, // إيصالات
  Kitchen: 1, // مطبخ
  Report: 2,  // تقارير
} as const;
export type PrinterType = (typeof PrinterType)[keyof typeof PrinterType];

// ==========================================
// Authentication & User DTOs
// ==========================================

export interface UserDto {
  id: string;
  userName: string;
  email?: string;
  phoneNumber?: string | null;
  roles?: string[];
  rolesDisplay?: string;
  permissions?: string[];
  restaurantId?: number | null;
}

export interface LoginRequestDto {
  userName: string;
  password: string;
}

export interface LoginResponseDto {
  isSuccess: boolean;
  message?: string;
  token?: string;
  user?: UserDto;
}

// ==========================================
// Category & Product DTOs
// ==========================================

export interface CategoryDto {
  id: number;
  name: string;
  productsCount?: number;
  printStationId?: number | null;
  printStationName?: string | null;
  printStationIds?: number[];
}

export interface CategoryCreateDto {
  name: string;
  printStationId?: number | null;
}

export interface CategoryUpdateDto {
  id: number;
  name: string;
  printStationId?: number | null;
}

export interface ProductDto {
  canViewCosts?: boolean;
  id: number;
  name: string;
  description?: string | null;
  salePrice: number;
  costPrice: number;
  profit?: number;
  profitMargin?: number;
  categoryId: number;
  categoryName?: string;
  imageUrl?: string | null;
  isAvailable?: boolean;
  barcode?: string | null;
  barCode?: string | null;
}

export interface ProductCreateDto {
  name: string;
  description?: string | null;
  salePrice: number;
  costPrice: number;
  categoryId: number;
  imageUrl?: string | null;
  isAvailable?: boolean;
  barcode?: string | null;
  barCode?: string | null;
}

export interface ProductUpdateDto {
  id: number;
  name: string;
  description?: string | null;
  salePrice: number;
  costPrice: number;
  categoryId: number;
  imageUrl?: string | null;
  isAvailable?: boolean;
  barcode?: string | null;
  barCode?: string | null;
}

export interface AddonDto {
  id: number;
  name: string;
  price: number;
  isAvailable?: boolean;
}

// ==========================================
// Order & Cart DTOs
// ==========================================

export interface OrderItemDto {
  id?: number;
  categoryId?: number;
  productId: number;
  productName: string;
  quantity: number;
  unitSalePrice: number;
  unitCostPrice?: number;
  unitDiscount?: number;
  lineTotal?: number;
  total?: number;
  notes?: string;
}

export interface OrderItemCreateDto {
  productId: number;
  quantity: number;
  unitSalePrice: number;
  unitCostPrice: number;
  unitDiscount: number;
  notes?: string;
}

export interface CashDrawerEntrySummaryDto {
  paymentMethodName?: string | null;
  id: number;
  amount: number;
  entryType: CashDrawerEntryType;
  date: string;
}

export interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

export interface OrderDto {
  id: number;
  orderNumber: number;
  total: number;
  cost: number;
  profit: number;
  discount: number;
  date: string;
  orderStatus: OrderStatus;
  orderType: OrderType;
  notes: string;
  userId: string;
  userName: string;
  orderItems: OrderItemDto[];
  cashDrawerEntries?: CashDrawerEntrySummaryDto[];
}

export interface OrderCreateDto {
  orderNumber: number;
  discount: number;
  orderStatus: OrderStatus;
  orderType: OrderType;
  notes: string;
  userId: string;
  orderItems: OrderItemCreateDto[];
}

export interface OrderCheckoutCreateDto {
  expectedTotal?: number;
  order: OrderCreateDto;
  cashboxId: number;
  paymentMethodId: number;
  clientRequestId: string;
}

export interface OrderUpdateDto {
  id: number;
  orderStatus?: OrderStatus;
  orderType?: OrderType;
  notes?: string;
  discount?: number;
}

// ==========================================
// Payment Method & Treasury DTOs
// ==========================================

export interface PaymentMethodDto {
  id: number;
  name: string;
  isAvailable?: boolean;
}

export interface CashboxDto {
  id: number;
  name: string;
  description?: string | null;
  initialBalance?: number;
  currentBalance?: number;
  isActive?: boolean;
  isDrawer: boolean;
}

export interface CashDrawerEntryDto {
  id: number;
  amount: number;
  entryType: CashDrawerEntryType;
  affectsCash: boolean;
  cashShiftId?: number | null;
  notes?: string;
  date: string;
  cashboxId: number;
  cashboxName?: string;
  userId: string;
  userName?: string;
  orderId?: number | null;
}

export interface CashDrawerEntryCreateDto {
  amount: number;
  entryType: CashDrawerEntryType;
  notes?: string;
  cashboxId: number;
  clientRequestId?: string;
}

export interface CashShiftContextDto {
  sourceCashboxId: number;
  sourceCashboxName: string;
  previousShiftId: number;
  lastEntryId: number;
  startedAt: string;
  canClose: boolean;
  expectedCash?: number | null;
}

export interface CashShiftDto {
  id: number;
  sourceCashboxId: number;
  sourceCashboxName: string;
  targetCashboxId: number;
  targetCashboxName: string;
  startedAt: string;
  closedAt: string;
  actualCash: number;
  expectedCash?: number | null;
  difference?: number | null;
  userId: string;
  userName: string;
  notes?: string | null;
}

// ==========================================
// Shift Close DTOs
// ==========================================

export interface ShiftSummaryDto {
  totalSales: number;
  totalManualInflow: number;
  totalOutflow: number;
  expectedCash: number;
  totalOrdersCount: number;
  shiftFrom: string;
  shiftTo: string;
  paymentBreakdown: {
    paymentMethodName: string;
    totalAmount: number;
    ordersCount: number;
  }[];
}

// ==========================================
// Reports DTOs
// ==========================================

export interface DailySalesReportDto {
  grossSales?: number;
  totalRefunds?: number;
  date: string;
  totalSales: number;
  totalCost: number;
  totalProfit: number;
  totalOrders: number;
  ordersCount?: number;
}

export interface ProductReportDto {
  productId: number;
  name: string;
  barCode?: string | null;
  quantitySold: number;
  totalRevenue: number;
  totalCost: number;
  totalProfit: number;
}

export interface CategoryReportDto {
  categoryId: number;
  name: string;
  quantitySold: number;
  totalRevenue: number;
  totalCost: number;
  totalProfit: number;
}

export interface PaymentMethodSalesReportDto {
  grossSales?: number;
  totalRefunds?: number;
  paymentMethodId: number;
  paymentMethodName: string;
  name?: string;
  totalOrders: number;
  ordersCount?: number;
  totalSales: number;
}

export interface CancelledOrderDto {
  orderId: number;
  orderNumber: number;
  date: string;
  total: number;
  notes?: string;
  reason?: string;
  userName?: string;
}

export interface OrderStatusSummaryDto {
  status: string;
  count: number;
  totalSales: number;
}

export interface OrderTypeSummaryDto {
  type: string;
  count: number;
  totalSales: number;
}

export interface ComprehensiveReportDto {
  grossSales?: number;
  totalRefunds?: number;
  totalSales: number;
  totalCost: number;
  totalProfit: number;
  totalDiscount: number;
  totalOrdersCount: number;
  averageOrderValue: number;
  cancelledOrdersCount?: number;
  dailySales?: DailySalesReportDto[];
  salesByPaymentMethod?: PaymentMethodSalesReportDto[];
  bestSellingProducts: ProductReportDto[];
  bestSellingCategories?: CategoryReportDto[];
  orderStatusSummary?: OrderStatusSummaryDto[];
  orderTypeSummary?: OrderTypeSummaryDto[];
}

// ==========================================
// Hardware & Settings DTOs
// ==========================================

export interface PrinterDto {
  id: number;
  name: string;
  printerName: string;
  printerType: PrinterType;
  printStationId: number;
  printStationName?: string;
  ipAddress?: string | null;
  port?: number | null;
  paperWidth?: number;
}

export interface PrinterCreateDto {
  name: string;
  printerName: string;
  printerType: PrinterType;
  printStationId: number;
}

export interface PrinterUpdateDto {
  id: number;
  name: string;
  printerName: string;
  printerType: PrinterType;
  printStationId: number;
}

export interface PrintStationDto {
  id: number;
  name: string;
  printerId?: number | null;
  printerName?: string | null;
  categoriesCount?: number;
}

export interface PrintStationCreateDto {
  name: string;
}

export interface PrintStationUpdateDto {
  id: number;
  name: string;
}

export interface CashboxCreateDto {
  name: string;
  description?: string;
  initialBalance?: number;
  isActive?: boolean;
}

export interface CashboxUpdateDto {
  id: number;
  name: string;
  description?: string;
  isActive?: boolean;
}

export interface CashboxBalanceDto {
  id: number;
  name: string;
  initialBalance: number;
  totalInflow: number;
  totalOutflow: number;
  currentBalance: number;
}

export interface AddonDto {
  id: number;
  name: string;
}

export interface AddonCreateDto {
  name: string;
}

export interface AddonUpdateDto {
  id: number;
  name: string;
}

export interface PaymentMethodDto {
  id: number;
  name: string;
  isTaxFree?: boolean;
  isCash: boolean;
  logoUrl?: string | null;
  isAvailable?: boolean;
}

export interface PaymentMethodCreateDto {
  name: string;
  isTaxFree?: boolean;
  isCash?: boolean;
  logoUrl?: string | null;
}

export interface PaymentMethodUpdateDto {
  id: number;
  name: string;
  isTaxFree?: boolean;
  isCash?: boolean;
  logoUrl?: string | null;
}


export interface UserUpdateDto {
  userName: string;
  email: string;
  phoneNumber?: string;
  permissions?: string[];
}

export interface RegisterRequestDto {
  userName: string;
  email: string;
  password?: string;
  phoneNumber?: string;
  permissions?: string[];
}

export interface RestaurantDto {
  id: number;
  name: string;
  logoUrl?: string | null;
  address?: string | null;
  phoneNumber?: string | null;
  taxNumber?: string | null;
}

export interface RestaurantUpdateDto {
  id: number;
  name: string;
  logoUrl?: string | null;
  address?: string | null;
  phoneNumber?: string | null;
  taxNumber?: string | null;
}

export interface RestaurantInfoDto {
  name: string;
  phone?: string;
  address?: string;
  taxNumber?: string;
  logoUrl?: string | null;
  currency?: string;
}

// ==========================================
// Generic API Result
// ==========================================

export interface ApiResult<T> {
  data?: T;
  isSuccess: boolean;
  errorMessage?: string;
  statusCode?: number;
}
