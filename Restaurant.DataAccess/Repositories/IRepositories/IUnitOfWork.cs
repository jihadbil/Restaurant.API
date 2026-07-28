using System;
using System.Threading.Tasks;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IUnitOfWork : IDisposable
{
    ICategoryRepository Categories { get; }
    ICategoryPrintStationRepository CategoryPrintStations { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    IPaymentMethodRepository PaymentMethods { get; }
    IPrintStationRepository PrintStations { get; }
    IPrinterRepository Printers { get; }
    IProductRepository Products { get; }
    IApplicationUserRepository ApplicationUsers { get; }
    ICashboxRepository Cashboxes { get; }
    ICashDrawerEntryRepository CashDrawerEntries { get; }
    IAddonRepository Addons { get; }
    IRestaurantRepository Restaurants { get; }

    Task<int> SaveAsync();
}
