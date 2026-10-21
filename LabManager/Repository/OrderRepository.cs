using _4th_year_set_up.Models;
using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repositories
{
    public class ConsumableOrderRepository : IOrderRepository
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
            return await _db.GetData<ConsumableOrder, dynamic>(
                "sp_GetConsumableOrderById",
                new
                {
                    OrderId = id
                });
        }


        // =========================================================
        // CREATE ORDER
        // =========================================================


        public async Task CreateOrder(
            ConsumableOrder order,
            string orderItemsJson)
        {
            await _db.SaveData(
                "sp_CreateConsumableOrder",
                new
                {
                    OrderNumber = order.OrderNumber,
                    SupplierID = order.SupplierID,
                    OrderDate = order.OrderDate,
                    OrderItemsJson = orderItemsJson
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
            return await _db.GetData<Supplier, dynamic>(
                "sp_GetSuppliers",
                new { });
        }

      


        public async Task<IEnumerable<Consumable>> GetConsumables()
        {
            return await _db.GetData<Consumable, dynamic>(
                "sp_GetorderConsumables",
                new { });
        }





        public async Task<IEnumerable<Consumable>> GetConsumablesBySupplier(int supplierId)
        {
            if (supplierId <= 0)
                return Enumerable.Empty<Consumable>();

            return await _db.GetData<Consumable, dynamic>(
                "dbo.sp_GetConsumablesBySupplier",
                new { SupplierID = supplierId });
        }


        public async Task<IEnumerable<OrderItemRow>> GetOrderItems(int orderId)
        {
            return await _db.GetData<OrderItemRow, dynamic>(
                "dbo.sp_GetConsumableOrderItems",
                new { OrderID = orderId });
        }




    }
}