using Microsoft.EntityFrameworkCore;
using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;
using Restaurant.Models.Enums;

namespace Restaurant.Services;

internal static class LedgerQueries
{
    public static async Task<decimal> BalanceAsync(IUnitOfWork unit, Cashbox cashbox) => cashbox.InitialBalance
        + (await unit.CashDrawerEntries.Query().Where(e => e.CashboxId == cashbox.Id && e.AffectsCash)
            .SumAsync(e => (decimal?)(e.EntryType == CashDrawerEntryType.SalePayment || e.EntryType == CashDrawerEntryType.Inflow
                || e.EntryType == CashDrawerEntryType.TransferIn || e.EntryType == CashDrawerEntryType.CashSurplus ? e.Amount : -e.Amount)) ?? 0);
}
