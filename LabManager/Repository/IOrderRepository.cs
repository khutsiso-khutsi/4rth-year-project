using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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
    }
}

