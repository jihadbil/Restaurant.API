using Restaurant.DataAccess.Repositories.IRepositories;
using System;
using System.Threading.Tasks;

namespace Restaurant.DataAccess.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly RestaurantDbContext _db;

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
    public IAddonRepository Addons { get; private set; }
    public IRestaurantRepository Restaurants { get; private set; }

    public UnitOfWork(RestaurantDbContext db)
    {
        _db = db;
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
        Addons = new AddonRepository(_db);
        Restaurants = new RestaurantRepository(_db);
    }

    public async Task<int> SaveAsync()
    {
        return await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
