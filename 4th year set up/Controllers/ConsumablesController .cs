using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LabManager.Controllers
{
    public class ManagerDashboardController : Controller
    {
        private readonly IConsumablesRepository _consumablesRepository;

        public ManagerDashboardController(
            IConsumablesRepository consumablesRepository)
        {
            _consumablesRepository = consumablesRepository;
        }

        // =====================================================
        // CONSUMABLES PAGE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Consumables()
        {
            var consumables =
                await _consumablesRepository.GetAllConsumables();

            var suppliers =
                await _consumablesRepository.GetAllSuppliers();

            ViewBag.Suppliers = suppliers;

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

                return RedirectToAction(nameof(Consumables));
            }

            if (QuantityOnHand < 0)
            {
                TempData["Error"] =
                    "Quantity on hand cannot be negative.";

                return RedirectToAction(nameof(Consumables));
            }

            if (ReorderLevel < 0)
            {
                TempData["Error"] =
                    "Reorder level cannot be negative.";

                return RedirectToAction(nameof(Consumables));
            }

            if (SupplierID <= 0)
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Consumables));
            }

            var consumable = new Consumable
            {
                ConsumableName = ConsumableName,
                OnHand = QuantityOnHand,
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

            return RedirectToAction(nameof(Consumables));
        }

        // =====================================================
        // ADD SUPPLIER
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSupplier(
            string SupplierName,
            string ContactPerson,
            string Email)
        {
            if (string.IsNullOrWhiteSpace(SupplierName))
            {
                TempData["Error"] =
                    "Supplier name is required.";

                return RedirectToAction(nameof(Consumables));
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                TempData["Error"] =
                    "Supplier email is required.";

                return RedirectToAction(nameof(Consumables));
            }

            var supplier = new Supplier
            {
                SupplierName = SupplierName,
                ContactPerson = ContactPerson,
                EmailAddress = Email
            };

            bool result =
                await _consumablesRepository
                    .AddSupplier(supplier);

            if (result)
            {
                TempData["Success"] =
                    "Supplier added successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to add supplier.";
            }

            return RedirectToAction(nameof(Consumables));
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

                return RedirectToAction(nameof(Consumables));
            }

            if (Quantity < 0)
            {
                TempData["Error"] =
                    "Quantity cannot be negative.";

                return RedirectToAction(nameof(Consumables));
            }

            if (string.IsNullOrWhiteSpace(AdjustmentType))
            {
                TempData["Error"] =
                    "Please select an adjustment type.";

                return RedirectToAction(nameof(Consumables));
            }

            if (AdjustmentType != "increase" &&
                AdjustmentType != "decrease" &&
                AdjustmentType != "set")
            {
                TempData["Error"] =
                    "Invalid adjustment type.";

                return RedirectToAction(nameof(Consumables));
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

            return RedirectToAction(nameof(Consumables));
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

                return RedirectToAction(nameof(Consumables));
            }

            if (string.IsNullOrWhiteSpace(ConsumableName))
            {
                TempData["Error"] =
                    "Consumable name is required.";

                return RedirectToAction(nameof(Consumables));
            }

            if (SupplierID <= 0)
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Consumables));
            }

            if (ReorderLevel < 0)
            {
                TempData["Error"] =
                    "Reorder level cannot be negative.";

                return RedirectToAction(nameof(Consumables));
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

            return RedirectToAction(nameof(Consumables));
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

                return RedirectToAction(nameof(Consumables));
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

            return RedirectToAction(nameof(Consumables));
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
    }
}