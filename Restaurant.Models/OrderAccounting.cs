namespace Restaurant.Models;

public static class OrderAccounting
{
    // Cumulative rounding keeps the allocated discount exact, including one-cent remainders.
    public static void Recalculate(Order order)
    {
        var items = order.OrderItems.ToList();
        foreach (var item in items)
        {
            if (item.Quantity <= 0) throw new ArgumentException("كمية المنتج يجب أن تكون أكبر من صفر.");
            Money.Validate(item.UnitSalePrice);
            Money.Validate(item.UnitCostPrice);
            Money.Validate(item.Quantity * item.UnitCostPrice);
            Money.Validate(item.UnitDiscount);
            if (item.UnitDiscount > item.UnitSalePrice) throw new ArgumentException("خصم المنتج يتجاوز سعره.");
            item.Total = item.Quantity * (item.UnitSalePrice - item.UnitDiscount);
            Money.Validate(item.Total);
        }
        Money.Validate(order.Discount);
        var gross = items.Sum(i => i.Total);
        if (order.Discount > gross) throw new ArgumentException("قيمة الخصم تتجاوز إجمالي الطلب.");
        decimal cumulative = 0, allocated = 0;
        foreach (var item in items.OrderBy(i => i.Id))
        {
            cumulative += item.Total;
            var share = gross == 0 ? 0 : decimal.Round(order.Discount * (cumulative / gross), 2, MidpointRounding.AwayFromZero);
            item.OrderDiscountShare = share - allocated;
            allocated = share;
        }
        order.Cost = items.Sum(i => i.Quantity * i.UnitCostPrice);
        order.Total = gross - order.Discount;
        order.Profit = order.Total - order.Cost;
        Money.Validate(order.Cost);
        Money.Validate(order.Total);
        if (Math.Abs(order.Profit) > Money.Maximum) throw new ArgumentException("ربح الطلب يتجاوز الحد المسموح.");
    }

    public static void Snapshot(OrderItem item, Product product)
    {
        item.ProductNameSnapshot = product.Name;
        item.ProductBarCodeSnapshot = product.BarCode;
        item.CategoryIdSnapshot = product.CategoryId;
        item.CategoryNameSnapshot = product.Category?.Name ?? string.Empty;
    }
}
