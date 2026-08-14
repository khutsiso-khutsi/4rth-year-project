using _4th_year_set_up.Models;
using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public  class ConsumablesRepository : IConsumablesRepository
    {
        private readonly ISqlDataAcess _dataAcess;  

        public ConsumablesRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess; 
        }

        public async  Task<bool> AddConsumables(Consumable consumables)
        {
            try
            {
                await _dataAcess.SaveData("", new { consumables.ConsumableName, consumables.Supplier, consumables.ReorderLevel, consumables.StockStatus,consumables.OnHand });
                return true;
            }
            catch (Exception ex) {
               return false;
            }
        }

        public async Task<bool> AddSupplier(Supplier supplier)
        {

            try
            {


                await _dataAcess.SaveData("", new { supplier.SupplierName, supplier.Email, supplier.ContactPerson });
                return true;
            }
            catch { 
                
                return false; } 
        }

       public async  Task<bool> UpdateConsomables(Consumable consumables)
        {
            try
            {
                await _dataAcess.SaveData("", consumables);
                return true;    
            }
            catch
            {
                return false;   
            }
           
        }

       public async  Task<bool> DeleteConsomubles(int id )
        {

            await _dataAcess.SaveData("", new { Id = id });
            return true;

        }

       public async  Task<IEnumerable<Consumable>> GetAllConsumables()
        {
            string query = "";
            return await _dataAcess.GetData<Consumable, dynamic>(query, new { });
        }

       public async Task<Consumable> GetById(int id)
        {
            string query = "";
            IEnumerable<Consumable> result = await _dataAcess.GetData<Consumable,dynamic>(query, new { id });
            return result.FirstOrDefault();

        }

       
    }
}
