using Restaurant.DataAccess.Repositories.IRepositories;
using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories;

public class PaymentMethodRepository : GenericRepository<PaymentMethod>, IPaymentMethodRepository
{
    public PaymentMethodRepository(RestaurantDbContext db) : base(db)
    {
    }

    public void Update(PaymentMethod paymentMethod)
    {
        _db.PaymentMethods.Update(paymentMethod);
    }
}
