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

        Task<bool> AddOrder(ConsumableOrder order) ;


        Task<bool> UpdateOrder(ConsumableOrder order) ; 

        Task<bool> DeleteOrder(int id ) ;

        Task<IEnumerable<ConsumableOrder>> GetAllOrders() ;

        Task<ConsumableOrder> GetOrderById(int id);
    }
}
