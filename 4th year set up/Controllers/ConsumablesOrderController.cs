using LabManager.Models;
using LabManager.Repository;
using LabManager.Repositories;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Orders(
            string status = "All Statuses")
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "All Statuses";
            }

            var orders =
                await _orderRepository.GetOrders(status);

            var suppliers =
                await _orderRepository.GetSuppliers();

            ViewBag.StatusFilter = status;
            ViewBag.Suppliers = suppliers;

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
            string supplier,
            string items)
        {
            if (string.IsNullOrWhiteSpace(supplier))
            {
                TempData["Error"] =
                    "Please select a supplier.";

                return RedirectToAction(nameof(Orders));
            }

            if (string.IsNullOrWhiteSpace(items))
            {
                TempData["Error"] =
                    "Please enter the order items.";

                return RedirectToAction(nameof(Orders));
            }

            string orderNumber =
                GenerateOrderNumber();

            var order = new ConsumableOrder
            {
                OrderNumber = orderNumber,
                Supplier = supplier.Trim(),
                Items = items.Trim(),
                OrderDate = DateTime.Now,
                Status = "Ordered"
            };

            await _orderRepository.CreateOrder(order);

            TempData["Success"] =
                $"Order {orderNumber} created successfully.";

            return RedirectToAction(nameof(Orders));
        }


        // =========================================================
        // RECEIVE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrder(
            [FromBody] ReceiveOrderRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            if (request.OrderId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid order."
                });
            }

            if (request.ReceivedItems == null ||
                request.ReceivedItems.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Please select at least one item as received."
                });
            }

            try
            {
                string json =
                    JsonSerializer.Serialize(
                        request.ReceivedItems);

                await _orderRepository.ReceiveOrder(
                    request.OrderId,
                    json);

                return Ok(new
                {
                    success = true,
                    message =
                        "Order received and stock updated."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
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

                return RedirectToAction(nameof(Orders));
            }

            if (string.IsNullOrWhiteSpace(
                cancellationReason))
            {
                TempData["Error"] =
                    "Cancellation reason is required.";

                return RedirectToAction(nameof(Orders));
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

            return RedirectToAction(nameof(Orders));
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