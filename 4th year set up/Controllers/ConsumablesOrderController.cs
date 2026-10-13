using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using static _4th_year_set_up.Models.Consumable;


namespace LabManager.Controllers
{
    public class ConsumableOrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;

        public ConsumableOrderController(
            IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        // =========================================================
        // ORDERS PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string status = "All Statuses")
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "All Statuses";
            }

            var orders =
                await _orderRepository.GetOrders(status);

            IEnumerable<Supplier> category = await _orderRepository.GetSuppliers();
            ViewBag.Supplier = category.Select(c => new SelectListItem { Value = c.SupplierID.ToString(), Text = c.SupplierName });

            IEnumerable<Consumable> consumables = await _orderRepository.GetConsumables();
            ViewBag.Consumable = consumables.Select(c => new SelectListItem { Value = c.ConsumableID.ToString(), Text = c.ConsumableName });

            ViewBag.StatusFilter = status;
          

            ViewBag.Email =
                HttpContext.Session.GetString("Email")
                ?? User.Identity?.Name
                ?? "";

            return View(orders);
        }


        // =========================================================
        // CREATE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(
     int supplierId,
     int consumableId)
        {
            if (supplierId <= 0)
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Index));
            }

            if (consumableId <= 0)
            {
                TempData["Error"] =
                    "Please enter the order items.";

                return RedirectToAction(nameof(Index));
            }

            var suppliers =
                await _orderRepository.GetSuppliers();

            var selectedSupplier =
                suppliers.FirstOrDefault(
                    s => s.SupplierID == supplierId);

            if (selectedSupplier == null)
            {
                TempData["Error"] =
                    "Selected supplier could not be found.";

                return RedirectToAction(nameof(Index));
            }

            string orderNumber =
                GenerateOrderNumber();

            var order = new ConsumableOrder
            {
                OrderNumber = orderNumber,
                SupplierID = supplierId,
                OrderDate = DateTime.Now,
                Status = "Ordered",
                ConsumableID = consumableId  
            };

            await _orderRepository.CreateOrder(order);

            TempData["Success"] =
                $"Order {orderNumber} created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // RECEIVE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]

        [HttpPost]
        public async Task<IActionResult> ReceiveOrder(
    int orderId,
    string receivedItemsJson)
        {
            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order."
                });
            }

            if (string.IsNullOrWhiteSpace(receivedItemsJson))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select at least one item as received."
                });
            }

            try
            {
                // Validate that the submitted JSON is valid.
                var receivedItems =
                    JsonSerializer.Deserialize<List<ReceivedOrderItem>>(
                        receivedItemsJson);

                if (receivedItems == null || receivedItems.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please select at least one item as received."
                    });
                }

                string json = JsonSerializer.Serialize(receivedItems);

                await _orderRepository.ReceiveOrder(orderId, json);

                return Ok(new
                {
                    success = true,
                    message = "Order received and stock updated."
                });
            }
            catch (JsonException)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid received items data."
                });
            }
            catch (Exception)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to receive the order. Please try again."
                });
            }
        }


        // =========================================================
        // CANCEL ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(
            int orderId,
            string cancellationReason)
        {
            if (orderId <= 0)
            {
                TempData["Error"] =
                    "Invalid order.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                cancellationReason))
            {
                TempData["Error"] =
                    "Cancellation reason is required.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _orderRepository.CancelOrder(
                    orderId,
                    cancellationReason.Trim());

                TempData["Success"] =
                    "Order cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;
            }

            return RedirectToAction(nameof(Index ));
        }


        // =========================================================
        // ORDER NUMBER
        // =========================================================

        private string GenerateOrderNumber()
        {
            Random random = new Random();

            int number =
                random.Next(100000, 999999);

            return $"ORD-{DateTime.Now.Year}-{number}";
        }
    }
}