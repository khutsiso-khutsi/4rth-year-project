using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public  class OrderRepository : IOrderRepository
    {

        private readonly ISqlDataAcess _dataAcess;

        public OrderRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess; 
        }


       public async   Task<bool> AddOrder(ConsumableOrder order)
        {
            try
            {
                await _dataAcess.SaveData("",new {order.OrderDate,order.OrderNumber,order.CompletedDate,order.Status});
                return true;
            }
            catch
            {
                return false;
            }
        }


       public async  Task<bool> UpdateOrder(ConsumableOrder order)
        {
            try
            {
                await _dataAcess.SaveData("", order);
                return true;
            }
            catch
            {
                return false;
            }
             
        }

       public async  Task<bool> DeleteOrder(int id )
        {
            try
            {
                await _dataAcess.SaveData("", new { Id = id });
                return true;
            }
            catch
            {
                return false;
            }
             
        }

       public async Task<IEnumerable<ConsumableOrder>> GetAllOrders()
        {
            string query = "";
            return await _dataAcess.GetData<ConsumableOrder, dynamic>(query, new { });
        }

        public async Task<ConsumableOrder> GetOrderById(int id)
        {
            string query = "";
            IEnumerable<ConsumableOrder> result = await _dataAcess.GetData<ConsumableOrder, dynamic>(query, new { Id = id });
            return result.FirstOrDefault();
        }
    }
}
