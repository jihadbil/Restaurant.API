using System.Threading.Tasks;
using Restaurant.Models.DTOs;

namespace Restaurant.Desktop.Services.IServices
{
    public interface IWpfPrintingService
    {
        Task PrintOrderAsync(OrderDto order);
        Task PrintReceiptAsync(OrderDto order);
        Task PrintKitchenTicketsAsync(OrderDto order);
    }
}
