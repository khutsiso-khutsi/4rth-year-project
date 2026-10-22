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
            // Consumables
            Task<bool> AddConsumables(Consumable consumables);
            Task<bool> UpdateConsumables(Consumable consumables);
            Task<bool> DeleteConsumables(int id);
            Task<bool> AdjustStock(int id, string adjustmentType, int quantity);
            Task<IEnumerable<Consumable>> GetAllConsumables();
            Task<Consumable> GetById(int id);

            // Suppliers
            Task<bool> AddSupplier(Supplier supplier);
            Task<bool> UpdateSupplier(Supplier supplier);
            Task<bool> DeleteSupplier(int supplierId);
            Task<IEnumerable<Supplier>> GetAllSuppliers();
            Task<Supplier> GetSupplierById(int supplierId);
            Task<bool> SupplierNameExists(string supplierName, int? excludeSupplierId = null);
            Task<bool> SupplierHasConsumables(int supplierId);
        


        Task<IEnumerable<Supplier>> GetSuppliers();


    } 
}
