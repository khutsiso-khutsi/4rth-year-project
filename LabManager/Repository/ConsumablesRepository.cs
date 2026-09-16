using _4th_year_set_up.Models;
using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public class ConsumablesRepository : IConsumablesRepository
    {
        private readonly ISqlDataAcess _dataAcess;

        public ConsumablesRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess;
        }

        // =====================================================
        // ADD CONSUMABLE
        // =====================================================


        public async Task<bool> AddConsumables(Consumable consumables)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_AddConsumable",
                    new
                    {
                        consumables.ConsumableName,
                        consumables.SupplierID,
                        consumables.ReorderLevel,
                        QuantityOnHand = consumables.OnHand
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // ADD SUPPLIER
        // =====================================================


        public async Task<bool> AddSupplier(Supplier supplier)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_AddSupplier",
                    new
                    {
                        supplier.SupplierName,
                        supplier.ContactPerson,
                        supplier.EmailAddress
                  
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // UPDATE CONSUMABLE
        // =====================================================
        public async Task<bool> UpdateConsumables(Consumable consumables)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_UpdateConsumable",
                    new
                    {
                        consumables.ConsumableID,
                        consumables.ConsumableName,
                        consumables.SupplierID,
                        consumables.ReorderLevel
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // DELETE CONSUMABLE
        // =====================================================
        public async Task<bool> DeleteConsumables(int id)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_DeleteConsumable",
                    new
                    {
                        ConsumableID = id
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // ADJUST STOCK
        // =====================================================
        public async Task<bool> AdjustStock(
            int id,
            string adjustmentType,
            int quantity)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_AdjustConsumableStock",
                    new
                    {
                        ConsumableID = id,
                        AdjustmentType = adjustmentType,
                        Quantity = quantity
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // GET ALL CONSUMABLES
        // =====================================================
        public async Task<IEnumerable<Consumable>> GetAllConsumables()
        {
            try
            {
                string query = "sp_GetConsumables";

                return await _dataAcess.GetData<Consumable, dynamic>(
                    query,
                    new { });
            }
            catch
            {
                return Enumerable.Empty<Consumable>();
            }
        }

        // =====================================================
        // GET CONSUMABLE BY ID
        // =====================================================
        public async Task<Consumable> GetById(int id)
        {
            try
            {
                string query = "sp_GetConsumableById";

                IEnumerable<Consumable> result =
                    await _dataAcess.GetData<Consumable, dynamic>(
                        query,
                        new
                        {
                            ConsumableID = id
                        });

                return result.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        // GET ALL SUPPLIERS
        // =====================================================
        public async Task<IEnumerable<Supplier>> GetAllSuppliers()
        {
            try
            {
                string query = "sp_GetSuppliers";

                return await _dataAcess.GetData<Supplier, dynamic>(
                    query,
                    new { });
            }
            catch
            {
                return Enumerable.Empty<Supplier>();
            }
        }


        public async Task<IEnumerable<Supplier>> GetSuppliers()
        {

            try
            {
                string query = "sp_GetSuppliersForeign";

                return await _dataAcess.GetData<Supplier, dynamic>(
                    query,
                    new { });
            }
            catch
            {
                return Enumerable.Empty<Supplier>();
            }
        }
    }
}