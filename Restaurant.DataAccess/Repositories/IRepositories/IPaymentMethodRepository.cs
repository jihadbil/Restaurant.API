using Restaurant.Models;

namespace Restaurant.DataAccess.Repositories.IRepositories;

public interface IPaymentMethodRepository : IGenericRepository<PaymentMethod>
{
    void Update(PaymentMethod paymentMethod);
}
