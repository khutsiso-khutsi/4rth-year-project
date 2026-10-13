using _4th_year_set_up.Models;
using LabManager.Models;


namespace LabManager.Repository
{
    public interface IOrderRepository
    {

        Task<IEnumerable<ConsumableOrder>> GetOrders(
            string status = "All Statuses");

        Task<IEnumerable<ConsumableOrder>> GetOrderById(
            int id);

        Task CreateOrder(
            ConsumableOrder order);

        Task ReceiveOrder(
            int orderId,
            string receivedItemsJson);

        Task CancelOrder(
            int orderId,
            string cancellationReason);

        Task<IEnumerable<Supplier>> GetSuppliers();

        Task<IEnumerable<Consumable>> GetConsumables();

    }
}

