using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabManager.Controllers
{
    public class ConsumablesController : Controller
    {
        private readonly IConsumablesRepository _consumablesRepository;

        public ConsumablesController(
            IConsumablesRepository consumablesRepository)
        {
            _consumablesRepository = consumablesRepository;
        }

        // =====================================================
        // CONSUMABLES PAGE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var consumables =
                await _consumablesRepository.GetAllConsumables();

            // Full supplier records for the dropdowns and the Manage Suppliers modal
            IEnumerable<Supplier> suppliers = await _consumablesRepository.GetAllSuppliers();
            ViewBag.SupplierList = suppliers.OrderBy(s => s.SupplierName).ToList();

            ViewBag.Email =
                HttpContext.Session.GetString("Email");

            return View(consumables);
        }

        // =====================================================
        // ADD CONSUMABLE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddConsumable(
            string ConsumableName,
            int QuantityOnHand,
            int ReorderLevel,
            int SupplierID)
        {
            if (string.IsNullOrWhiteSpace(ConsumableName))
            {
                TempData["Error"] =
                    "Consumable name is required.";

                return RedirectToAction(nameof(Index));
            }

            if (QuantityOnHand < 0)
            {
                TempData["Error"] =
                    "Quantity on hand cannot be negative.";

                return RedirectToAction(nameof(Index));
            }

            if (ReorderLevel < 0)
            {
                TempData["Error"] =
                    "Reorder level cannot be negative.";

                return RedirectToAction(nameof(Index));
            }

            if (SupplierID <= 0)
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Index));
            }

            var consumable = new Consumable
            {
                ConsumableName = ConsumableName,
                QuantityOnHand = QuantityOnHand,
                ReorderLevel = ReorderLevel,
                SupplierID = SupplierID
            };

            bool result =
                await _consumablesRepository
                    .AddConsumables(consumable);

            if (result)
            {
                TempData["Success"] =
                    "Consumable added successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to add consumable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ADJUST STOCK
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(
            int ConsumableID,
            string AdjustmentType,
            int Quantity)
        {
            if (ConsumableID <= 0)
            {
                TempData["Error"] =
                    "Invalid consumable.";

                return RedirectToAction(nameof(Index));
            }

            if (Quantity < 0)
            {
                TempData["Error"] =
                    "Quantity cannot be negative.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(AdjustmentType))
            {
                TempData["Error"] =
                    "Please select an adjustment type.";

                return RedirectToAction(nameof(Index));
            }

            if (AdjustmentType != "Increase" &&
                AdjustmentType != "Decrease" &&
                AdjustmentType != "Set")
            {
                TempData["Error"] =
                    "Invalid adjustment type.";

                return RedirectToAction(nameof(Index));
            }

            bool result =
                await _consumablesRepository
                    .AdjustStock(
                        ConsumableID,
                        AdjustmentType,
                        Quantity);

            if (result)
            {
                TempData["Success"] =
                    "Stock adjusted successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to adjust stock. " +
                    "Check that the quantity is valid.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // UPDATE CONSUMABLE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConsumable(
            int ConsumableID,
            string ConsumableName,
            int SupplierID,
            int ReorderLevel)
        {
            if (ConsumableID <= 0)
            {
                TempData["Error"] =
                    "Invalid consumable.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(ConsumableName))
            {
                TempData["Error"] =
                    "Consumable name is required.";

                return RedirectToAction(nameof(Index));
            }

            if (SupplierID <= 0)
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Index));
            }

            if (ReorderLevel < 0)
            {
                TempData["Error"] =
                    "Reorder level cannot be negative.";

                return RedirectToAction(nameof(Index));
            }

            var consumable = new Consumable
            {
                ConsumableID = ConsumableID,
                ConsumableName = ConsumableName,
                SupplierID = SupplierID,
                ReorderLevel = ReorderLevel
            };

            bool result =
                await _consumablesRepository
                    .UpdateConsumables(consumable);

            if (result)
            {
                TempData["Success"] =
                    "Consumable updated successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to update consumable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE CONSUMABLE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsumable(
            int ConsumableID)
        {
            if (ConsumableID <= 0)
            {
                TempData["Error"] =
                    "Invalid consumable.";

                return RedirectToAction(nameof(Index));
            }

            bool result =
                await _consumablesRepository
                    .DeleteConsumables(ConsumableID);

            if (result)
            {
                TempData["Success"] =
                    "Consumable deleted successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to delete consumable.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GET CONSUMABLE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetConsumable(int id)
        {
            var consumable =
                await _consumablesRepository.GetById(id);

            if (consumable == null)
            {
                return NotFound();
            }

            return Json(consumable);
        }

        // =====================================================
        // ADD SUPPLIER
        // =====================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(
            [Bind("SupplierName,ContactPerson,EmailAddress")] Supplier model)
        {
            if (string.IsNullOrWhiteSpace(model.SupplierName) ||
                string.IsNullOrWhiteSpace(model.EmailAddress))
            {
                TempData["Error"] = "Supplier name and email are required.";
                return RedirectToAction(nameof(Index));
            }

            if (await _consumablesRepository.SupplierNameExists(model.SupplierName))
            {
                TempData["Error"] =
                    $"A supplier named \"{model.SupplierName.Trim()}\" already exists.";
                return RedirectToAction(nameof(Index));
            }

            model.SupplierName = model.SupplierName.Trim();

            bool result = await _consumablesRepository.AddSupplier(model);

            if (result)
                TempData["Success"] = $"{model.SupplierName} added.";
            else
                TempData["Error"] = "Unable to add supplier.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // UPDATE SUPPLIER
        // =====================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSupplier(
            [Bind("SupplierID,SupplierName,ContactPerson,EmailAddress")] Supplier model)
        {
            var existing = await _consumablesRepository.GetSupplierById(model.SupplierID);
            if (existing == null)
            {
                TempData["Error"] = "Supplier not found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.SupplierName) ||
                string.IsNullOrWhiteSpace(model.EmailAddress))
            {
                TempData["Error"] = "Supplier name and email are required.";
                return RedirectToAction(nameof(Index));
            }

            if (await _consumablesRepository.SupplierNameExists(model.SupplierName, model.SupplierID))
            {
                TempData["Error"] =
                    $"A supplier named \"{model.SupplierName.Trim()}\" already exists.";
                return RedirectToAction(nameof(Index));
            }

            model.SupplierName = model.SupplierName.Trim();

            bool result = await _consumablesRepository.UpdateSupplier(model);

            if (result)
                TempData["Success"] = $"{model.SupplierName} updated.";
            else
                TempData["Error"] = "Unable to update supplier.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE SUPPLIER
        // =====================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            var supplier = await _consumablesRepository.GetSupplierById(supplierId);
            if (supplier == null)
            {
                TempData["Error"] = "Supplier not found.";
                return RedirectToAction(nameof(Index));
            }

            if (await _consumablesRepository.SupplierHasConsumables(supplierId))
            {
                TempData["Error"] =
                    $"{supplier.SupplierName} still has consumables assigned. Reassign them first.";
                return RedirectToAction(nameof(Index));
            }

            bool result = await _consumablesRepository.DeleteSupplier(supplierId);

            if (result)
                TempData["Success"] = $"{supplier.SupplierName} deleted.";
            else
                TempData["Error"] = "Unable to delete supplier.";

            return RedirectToAction(nameof(Index));
        }
    }
}
