using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Restaurant.DataAccess.Repositories.IRepositories;
using System;
using System.Threading.Tasks;

namespace Restaurant.DataAccess.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly RestaurantDbContext _db;
    private readonly ILogger<UnitOfWork> _logger;

    public ICategoryRepository Categories { get; private set; }
    public ICategoryPrintStationRepository CategoryPrintStations { get; private set; }
    public IOrderRepository Orders { get; private set; }
    public IOrderItemRepository OrderItems { get; private set; }
    public IPaymentMethodRepository PaymentMethods { get; private set; }
    public IPrintStationRepository PrintStations { get; private set; }
    public IPrinterRepository Printers { get; private set; }
    public IProductRepository Products { get; private set; }
    public IApplicationUserRepository ApplicationUsers { get; private set; }
    public ICashboxRepository Cashboxes { get; private set; }
    public ICashDrawerEntryRepository CashDrawerEntries { get; private set; }
    public IGenericRepository<Restaurant.Models.CashShift> CashShifts { get; private set; }
    public IAddonRepository Addons { get; private set; }
    public IRestaurantRepository Restaurants { get; private set; }

    public UnitOfWork(RestaurantDbContext db, ILogger<UnitOfWork>? logger = null)
    {
        _db = db;
        _logger = logger ?? NullLogger<UnitOfWork>.Instance;
        Categories = new CategoryRepository(_db);
        CategoryPrintStations = new CategoryPrintStationRepository(_db);
        Orders = new OrderRepository(_db);
        OrderItems = new OrderItemRepository(_db);
        PaymentMethods = new PaymentMethodRepository(_db);
        PrintStations = new PrintStationRepository(_db);
        Printers = new PrinterRepository(_db);
        Products = new ProductRepository(_db);
        ApplicationUsers = new ApplicationUserRepository(_db);
        Cashboxes = new CashboxRepository(_db);
        CashDrawerEntries = new CashDrawerEntryRepository(_db);
        CashShifts = new GenericRepository<Restaurant.Models.CashShift>(_db);
        Addons = new AddonRepository(_db);
        Restaurants = new RestaurantRepository(_db);
    }

    public async Task<int> SaveAsync()
    {
        return await _db.SaveChangesAsync();
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        var executionStrategy = _db.Database.CreateExecutionStrategy();
        var receiptId = Guid.NewGuid();
        var receiptWritten = false;
        T result = default!;
        var committedResult = await executionStrategy.ExecuteAsync<object?, T>(null, async (_, _, _) =>
        {
            receiptWritten = false;
            var hasWrites = false;
            void OnSaved(object? sender, SavedChangesEventArgs args) => hasWrites |= args.EntitiesSavedCount > 0;
            // Ledger reads and writes must observe one consistent balance, including checkout/shift close.
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                : await _db.Database.BeginTransactionAsync();
            _db.SavedChanges += OnSaved;
            try
            {
                result = await operation();
                if (_db.Database.IsRelational() && hasWrites)
                {
                    // Raw SQL avoids saving any additional, unfinished changes in the caller's tracker.
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO [__TransactionReceipts] ([Id]) VALUES ({receiptId})");
                    receiptWritten = true;
                }
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                try { await transaction.RollbackAsync(); }
                catch (Exception rollbackError)
                {
                    // An already committed or disconnected transaction cannot be rolled back.
                    _logger.LogWarning(rollbackError, "Rollback failed; preserving the original transaction error.");
                }
                finally { _db.ChangeTracker.Clear(); }
                throw;
            }
            finally { _db.SavedChanges -= OnSaved; }
        }, async (_, _, cancellationToken) => new ExecutionResult<T>(
            receiptWritten && await _db.Set<TransactionReceipt>().AsNoTracking()
                .AnyAsync(r => r.Id == receiptId, cancellationToken), result));

        if (receiptWritten)
        {
            try
            {
                await executionStrategy.ExecuteAsync(() => _db.Set<TransactionReceipt>()
                    .Where(r => r.Id == receiptId).ExecuteDeleteAsync());
            }
            catch (Exception cleanupError)
            {
                // Cleanup is housekeeping: a committed sale must still be reported as successful.
                _logger.LogWarning(cleanupError, "Could not remove transaction receipt {ReceiptId}.", receiptId);
            }
        }
        return committedResult;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
