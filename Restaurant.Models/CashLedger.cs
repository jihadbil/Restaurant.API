using Restaurant.Models.Enums;

namespace Restaurant.Models;

public static class CashLedger
{
    public static bool IsInflow(CashDrawerEntryType type) => type is
        CashDrawerEntryType.SalePayment or CashDrawerEntryType.Inflow or
        CashDrawerEntryType.TransferIn or CashDrawerEntryType.CashSurplus;

    public static decimal Balance(decimal initialBalance, IEnumerable<CashDrawerEntry> entries) =>
        initialBalance + entries.Where(e => e.AffectsCash)
            .Sum(e => IsInflow(e.EntryType) ? e.Amount : -e.Amount);
}
