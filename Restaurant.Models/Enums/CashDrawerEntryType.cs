namespace Restaurant.Models.Enums;

/// <summary>
/// نوع حركة درج النقود (الخزينة)
/// </summary>
public enum CashDrawerEntryType
{
    /// <summary>
    /// دفعة مبيعات مرتبطة بطلب
    /// </summary>
    SalePayment = 1,

    /// <summary>
    /// إيداع يدوي في الخزينة
    /// </summary>
    Inflow = 2,

    /// <summary>
    /// سحب يدوي من الخزينة
    /// </summary>
    Outflow = 3
}
