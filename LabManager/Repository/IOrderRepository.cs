using _4th_year_set_up.Models;
using LabManager.Models;


namespace LabManager.Repository
{
    public interface IOrderRepository
    {
        Task<IEnumerable<ConsumableOrder>> GetOrders(
            string status = "All Statuses");

        Task<IEnumerable<ConsumableOrder>> GetOrderById(int id);

        // One order header containing multiple item rows.
        Task CreateOrder(
            ConsumableOrder order,
            string orderItemsJson);

        Task ReceiveOrder(int orderId, string receivedItemsJson);

        Task CancelOrder(
            int orderId,
            string cancellationReason);

        Task<IEnumerable<Supplier>> GetSuppliers();

        Task<IEnumerable<Consumable>> GetConsumables();

        Task<IEnumerable<Consumable>> GetConsumablesBySupplier(int supplierId);
    }
}

