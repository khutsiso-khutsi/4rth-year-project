using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repositories
{
    public class ConsumableOrderRepository: IOrderRepository
    {
        private readonly ISqlDataAcess _db;

        public ConsumableOrderRepository(ISqlDataAcess db)
        {
            _db = db;
        }

        // =========================================================
        // GET ALL ORDERS
        // =========================================================

        public async Task<IEnumerable<ConsumableOrder>> GetOrders(
            string status = "All Statuses")
        {
            return await _db.GetData<ConsumableOrder, object>(
                "sp_GetConsumableOrders",
                new
                {
                    Status = status
                });
        }


        // =========================================================
        // GET ORDER BY ID
        // =========================================================

        public async Task<IEnumerable<ConsumableOrder>> GetOrderById(
            int id)
        {
            return await _db.GetData<ConsumableOrder, object>(
                "sp_GetConsumableOrderById",
                new
                {
                    Id = id
                });
        }


        // =========================================================
        // CREATE ORDER
        // =========================================================

        public async Task CreateOrder(
            ConsumableOrder order)
        {
            await _db.SaveData(
                "sp_CreateConsumableOrder",
                new
                {
                    OrderNumber = order.OrderNumber,
                    Supplier = order.Supplier,
                    Items = order.Items
                });
        }


        // =========================================================
        // RECEIVE ORDER
        // =========================================================

        public async Task ReceiveOrder(
            int orderId,
            string receivedItemsJson)
        {
            await _db.SaveData(
                "sp_ReceiveConsumableOrder",
                new
                {
                    OrderId = orderId,
                    ReceivedItemsJson = receivedItemsJson
                });
        }


        // =========================================================
        // CANCEL ORDER
        // =========================================================

        public async Task CancelOrder(
            int orderId,
            string cancellationReason)
        {
            await _db.SaveData(
                "sp_CancelConsumableOrder",
                new
                {
                    OrderId = orderId,
                    CancellationReason =
                        cancellationReason
                });
        }


        // =========================================================
        // GET SUPPLIERS
        // =========================================================

        public async Task<IEnumerable<Supplier>> GetSuppliers()
        {
            return await _db.GetData<Supplier, object>(
                "sp_GetSuppliers",
                new { });
        }
    }
}