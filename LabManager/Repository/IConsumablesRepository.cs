using _4th_year_set_up.Models;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
   public interface IConsumablesRepository
    {

        Task<bool> AddConsumables(Consumable consumables);

        Task<bool> AddSupplier(Supplier supplier);

        Task<bool> UpdateConsomables(Consumable consumables);

        Task<bool> DeleteConsomubles(int Id);

        Task<IEnumerable<Consumable>> GetAllConsumables();

        Task<Consumable> GetById (int id);  



    }
}
