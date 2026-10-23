using _4th_year_set_up.Models;
using _4th_year_set_up.Services;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Text;
using System.Text.Json;
using static _4th_year_set_up.Models.Consumable;

namespace LabManager.Controllers
{
    public class ConsumableOrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ConsumableOrderController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ConsumableOrderController(
            IOrderRepository orderRepository,
            IEmailService emailService,
            ILogger<ConsumableOrderController> logger)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
            _logger = logger;
        }

        // Used by the multi-item Create Order form.
        public sealed class CreateOrderItemRequest
        {
            public int ConsumableID { get; set; }
            public int QuantityOrdered { get; set; }
        }

        // Sent by the Receive modal: one entry per order line.
        public sealed class ReceiveItemRequest
        {
            public int OrderItemID { get; set; }
            public int QuantityReceived { get; set; }
        }

        // Used by the optional bulk form, which may contain items for multiple suppliers.
        public sealed class BulkOrderItemRequest
        {
            public int SupplierId { get; set; }
            public int ConsumableId { get; set; }
            public int QuantityOrdered { get; set; }
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(string status = "All Statuses")
        {
            if (string.IsNullOrWhiteSpace(status))
                status = "All Statuses";

            try
            {
                var orders = await _orderRepository.GetOrders(status);
                var suppliers = (await _orderRepository.GetSuppliers()).ToList();
                var consumables = (await _orderRepository.GetConsumables()).ToList();

                ViewBag.Supplier = suppliers.Select(s => new SelectListItem
                {
                    Value = s.SupplierID.ToString(),
                    Text = s.SupplierName
                }).ToList();

                ViewBag.ConsumableDetails = consumables.Select(c => new
                {
                    id = c.ConsumableID,
                    name = c.ConsumableName,
                    supplierId = c.SupplierID,
                    quantityOnHand = c.QuantityOnHand
                }).ToList();

                ViewBag.Consumable = consumables.Select(c => new SelectListItem
                {
                    Value = c.ConsumableID.ToString(),
                    Text = c.ConsumableName
                }).ToList();

                SetCommonViewData(status);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load consumable orders.");
                TempData["Error"] = "Unable to load consumable orders.";

                ViewBag.Supplier = new List<SelectListItem>();
                ViewBag.ConsumableDetails = new List<object>();
                ViewBag.Consumable = new List<SelectListItem>();
                SetCommonViewData(status);

                return View(Enumerable.Empty<ConsumableOrder>());
            }
        }

        // =========================================================
        // AJAX: CONSUMABLES FOR A SUPPLIER (Create Order modal)
        // Returns a plain JSON array; the page accepts this shape.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetConsumablesBySupplier(int supplierId)
        {
            if (supplierId <= 0)
                return BadRequest(new { success = false, message = "Please select a valid supplier." });

            try
            {
                var consumables = await _orderRepository.GetConsumablesBySupplier(supplierId);

                return Json(consumables.Select(c => new
                {
                    consumableID = c.ConsumableID,
                    consumableName = c.ConsumableName,
                    supplierID = c.SupplierID,
                    quantityOnHand = c.QuantityOnHand
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load consumables for supplier {SupplierId}.", supplierId);
                return StatusCode(500, new { success = false, message = "Unable to load consumables for this supplier." });
            }
        }

        // =========================================================
        // CREATE ORDER (one supplier, many items)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(int supplierId, string items)
        {
            if (supplierId <= 0)
                return Fail("Please select a supplier.");

            if (string.IsNullOrWhiteSpace(items))
                return Fail("Please add at least one consumable.");

            var requestedItems = ParseItems(items);
            if (requestedItems == null)
                return Fail("The order items are not valid. Please try again.");

            if (requestedItems.Count == 0 ||
                requestedItems.Any(i => i.ConsumableID <= 0 || i.QuantityOrdered <= 0))
                return Fail("Choose at least one consumable and enter a quantity greater than zero.");

            if (requestedItems.Select(i => i.ConsumableID).Distinct().Count() != requestedItems.Count)
                return Fail("Each consumable can only appear once in an order. Change its quantity instead.");

            try
            {
                var result = await CreateOrderForSupplierAsync(supplierId, requestedItems);

                if (!result.Success)
                    return Fail(result.Message);

                TempData["Success"] = result.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create consumable order.");
                TempData["Error"] = "Unable to create the order. Please check the details and try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CREATE ORDERS BULK (one order per supplier)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrdersBulk(string consumablesJson)
        {
            if (string.IsNullOrWhiteSpace(consumablesJson))
                return Fail("Add at least one consumable before submitting.");

            List<BulkOrderItemRequest>? rows;
            try
            {
                rows = JsonSerializer.Deserialize<List<BulkOrderItemRequest>>(consumablesJson, JsonOptions);
            }
            catch (JsonException)
            {
                return Fail("The submitted consumable data is invalid.");
            }

            if (rows == null || rows.Count == 0 ||
                rows.Any(x => x.SupplierId <= 0 || x.ConsumableId <= 0 || x.QuantityOrdered <= 0))
                return Fail("Every row must have a supplier, consumable, and quantity greater than zero.");

            try
            {
                var suppliers = (await _orderRepository.GetSuppliers()).ToList();

                // Validate every row against the supplier's own consumable list
                // before creating any order.
                var consumablesBySupplier = new Dictionary<int, List<Consumable>>();
                foreach (var supplierId in rows.Select(r => r.SupplierId).Distinct())
                {
                    consumablesBySupplier[supplierId] =
                        (await _orderRepository.GetConsumablesBySupplier(supplierId)).ToList();
                }

                foreach (var row in rows)
                {
                    if (!suppliers.Any(s => s.SupplierID == row.SupplierId) ||
                        !consumablesBySupplier[row.SupplierId].Any(c => c.ConsumableID == row.ConsumableId))
                    {
                        return Fail("A selected consumable does not belong to its selected supplier. No orders were submitted.");
                    }
                }

                foreach (var supplierGroup in rows.GroupBy(x => x.SupplierId))
                {
                    var groupItems = supplierGroup
                        .GroupBy(x => x.ConsumableId)
                        .Select(g => new CreateOrderItemRequest
                        {
                            ConsumableID = g.Key,
                            QuantityOrdered = g.Sum(x => x.QuantityOrdered)
                        })
                        .ToList();

                    var result = await CreateOrderForSupplierAsync(supplierGroup.Key, groupItems);
                    if (!result.Success)
                        return Fail(result.Message);
                }

                TempData["Success"] = "Orders were created successfully, grouped by supplier.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create bulk consumable orders.");
                TempData["Error"] = "Unable to create the bulk orders.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // RECEIVE ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrder(int orderId, string receivedItemsJson)
        {
            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid order." });

            if (string.IsNullOrWhiteSpace(receivedItemsJson))
                return BadRequest(new { success = false, message = "Select at least one item as received." });

            try
            {
                var receivedItems = JsonSerializer.Deserialize<List<ReceiveItemRequest>>(
                    receivedItemsJson, JsonOptions);

                if (receivedItems == null ||
                    !receivedItems.Any(i => i.OrderItemID > 0 && i.QuantityReceived > 0))
                    return BadRequest(new { success = false, message = "Enter at least one received quantity." });

                if (receivedItems.Any(i => i.OrderItemID <= 0 || i.QuantityReceived < 0))
                    return BadRequest(new { success = false, message = "Invalid received items data." });

                // The stored procedure adds the received quantities to stock.
                await _orderRepository.ReceiveOrder(
                    orderId, JsonSerializer.Serialize(receivedItems));

                return Ok(new { success = true, message = "Order received and stock updated." });
            }
            catch (JsonException)
            {
                return BadRequest(new { success = false, message = "Invalid received items data." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive order {OrderId}.", orderId);
                // Show the database's validation message (for example "exceeds the quantity still outstanding").
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =========================================================
        // CANCEL ORDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string cancellationReason)
        {
            if (orderId <= 0)
                return Fail("Invalid order.");

            if (string.IsNullOrWhiteSpace(cancellationReason))
                return Fail("Cancellation reason is required.");

            try
            {
                var orders = await _orderRepository.GetOrders("All Statuses");
                var order = orders.FirstOrDefault(o => o.Id == orderId);

                if (order == null)
                    return Fail("Order could not be found.");

                var supplier = await GetSupplierByIdAsync(order.SupplierID);
                var reason = cancellationReason.Trim();

                // Cancel in the database before attempting notification.
                await _orderRepository.CancelOrder(orderId, reason);

                if (supplier == null || string.IsNullOrWhiteSpace(supplier.EmailAddress))
                    return Fail($"Order {order.OrderNumber} was cancelled, but the supplier email could not be found.");

                var body = $@"
<html><body style='font-family:Arial,sans-serif;color:#333;'>
<h2>Consumable Order Cancellation Notice</h2>
<p>Dear {WebUtility.HtmlEncode(supplier.ContactPerson ?? supplier.SupplierName)},</p>
<p>Order <strong>{WebUtility.HtmlEncode(order.OrderNumber)}</strong> has been cancelled.</p>
<table cellpadding='8' cellspacing='0' style='border-collapse:collapse;border:1px solid #ddd;'>
<tr><td><strong>Cancellation Date</strong></td><td>{DateTime.Now:dd MMMM yyyy HH:mm}</td></tr>
<tr><td><strong>Status</strong></td><td>Cancelled</td></tr>
<tr><td><strong>Reason</strong></td><td>{WebUtility.HtmlEncode(reason)}</td></tr>
</table>
<p>Please update your records and do not process this order further.</p>
<p>Kind regards,<br />Laboratory Manager<br />NMB-HLabSys<br />Haematology Laboratory System</p>
</body></html>";

                try
                {
                    await _emailService.SendEmailAsync(
                        supplier.EmailAddress,
                        $"Order Cancellation - {order.OrderNumber}",
                        body);

                    TempData["Success"] = $"Order {order.OrderNumber} was cancelled and the supplier was notified.";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Order {OrderNumber} was cancelled but email notification failed.", order.OrderNumber);
                    TempData["Error"] = $"Order {order.OrderNumber} was cancelled, but the email could not be sent.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {OrderId}.", orderId);
                TempData["Error"] = "Unable to cancel the order. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // AJAX: LINE ITEMS FOR THE RECEIVE MODAL
        // Reads the order-item rows (orderItemID, quantityOrdered,
        // quantityReceived) through IOrderRepository.GetOrderItems, which
        // calls dbo.sp_GetConsumableOrderItems.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid order." });

            try
            {
                var orderItems = (await _orderRepository.GetOrderItems(orderId)).ToList();
                if (orderItems.Count == 0)
                    return NotFound(new { success = false, message = "No items were found for this order." });

                var items = orderItems.Select(i => new
                {
                    orderItemID = i.OrderItemID,
                    consumableID = i.ConsumableID,
                    consumableName = i.ConsumableName,
                    quantityOrdered = i.QuantityOrdered,
                    quantityReceived = i.QuantityReceived
                }).ToList();

                return Json(new { success = true, items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve items for order {OrderId}.", orderId);
                return StatusCode(500, new { success = false, message = "Unable to load the order items." });
            }
        }

        // =========================================================
        // SHARED HELPERS
        // =========================================================

        // Creates one order for one supplier and notifies the supplier.
        // Items are validated against the supplier's own consumable list,
        // the same source the Create Order dropdown uses.
        private async Task<(bool Success, string Message)> CreateOrderForSupplierAsync(
            int supplierId,
            List<CreateOrderItemRequest> requestedItems)
        {
            var supplier = await GetSupplierByIdAsync(supplierId);
            if (supplier == null)
                return (false, "The selected supplier could not be found.");

            var supplierConsumables =
                (await _orderRepository.GetConsumablesBySupplier(supplierId)).ToList();

            var selectedItems = new List<(Consumable Consumable, int Quantity)>();

            foreach (var requested in requestedItems)
            {
                var consumable = supplierConsumables.FirstOrDefault(
                    c => c.ConsumableID == requested.ConsumableID);

                if (consumable == null)
                    return (false, "One or more selected consumables are not supplied by the selected supplier.");

                if (requested.QuantityOrdered <= 0)
                    return (false, "Quantities must be greater than zero.");

                selectedItems.Add((consumable, requested.QuantityOrdered));
            }

            if (selectedItems.Count == 0)
                return (false, "No consumables were selected.");

            var orderNumber = GenerateOrderNumber();
            var orderDate = DateTime.Now;

            var order = new ConsumableOrder
            {
                OrderNumber = orderNumber,
                SupplierID = supplierId,
                OrderDate = orderDate,
                Status = "Ordered",

                // Legacy header fields, kept populated for the existing model.
                ConsumableID = selectedItems[0].Consumable.ConsumableID,
                QuantityOrdered = selectedItems[0].Quantity
            };

            var orderItemsJson = JsonSerializer.Serialize(selectedItems.Select(x => new
            {
                ConsumableID = x.Consumable.ConsumableID,
                QuantityOrdered = x.Quantity
            }));

            await _orderRepository.CreateOrder(order, orderItemsJson);

            if (string.IsNullOrWhiteSpace(supplier.EmailAddress))
                return (true, $"Order {orderNumber} was created, but the supplier has no email address configured.");

            try
            {
                var rows = new StringBuilder();
                foreach (var item in selectedItems)
                {
                    rows.Append("<tr><td>")
                        .Append(WebUtility.HtmlEncode((string?)item.Consumable.ConsumableName))
                        .Append("</td><td>")
                        .Append(item.Quantity)
                        .Append("</td></tr>");
                }

                var body = $@"
<html>
<body style='font-family:Arial,sans-serif;color:#333;'>
  <h2>New Consumable Order</h2>
  <p>Dear {WebUtility.HtmlEncode(supplier.ContactPerson ?? supplier.SupplierName)},</p>
  <p>A new consumable order has been created by NMB-HLabSys.</p>
  <table cellpadding='8' cellspacing='0' style='border-collapse:collapse;border:1px solid #ddd;'>
    <tr><td><strong>Order Number</strong></td><td>{WebUtility.HtmlEncode(orderNumber)}</td></tr>
    <tr><td><strong>Supplier</strong></td><td>{WebUtility.HtmlEncode(supplier.SupplierName)}</td></tr>
    <tr><td><strong>Order Date</strong></td><td>{orderDate:dd MMMM yyyy HH:mm}</td></tr>
  </table>
  <br />
  <table cellpadding='8' cellspacing='0' style='border-collapse:collapse;border:1px solid #ddd;'>
    <tr><th>Consumable</th><th>Quantity Ordered</th></tr>
    {rows}
  </table>
  <p>Kind regards,<br />Laboratory Manager<br />NMB-HLabSys</p>
</body>
</html>";

                await _emailService.SendEmailAsync(
                    supplier.EmailAddress,
                    $"New Consumable Order - {orderNumber}",
                    body);

                return (true, $"Order {orderNumber} was created and the supplier was notified.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Order {OrderNumber} was saved, but the supplier email failed.", orderNumber);
                return (true, $"Order {orderNumber} was created, but the notification email could not be sent.");
            }
        }

        private List<CreateOrderItemRequest>? ParseItems(string items)
        {
            try
            {
                return JsonSerializer.Deserialize<List<CreateOrderItemRequest>>(items, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private IActionResult Fail(string message)
        {
            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        private void SetCommonViewData(string status)
        {
            ViewBag.StatusFilter = status;
            ViewBag.Email = HttpContext.Session.GetString("Email")
                ?? User.Identity?.Name
                ?? string.Empty;
        }

        private async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
        {
            var suppliers = await _orderRepository.GetSuppliers();
            return suppliers.FirstOrDefault(s => s.SupplierID == supplierId);
        }

        private static string GenerateOrderNumber()
        {
            // GUID-derived suffix avoids collisions from creating multiple orders quickly.
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"ORD-{DateTime.Now:yyyy}-{suffix}";
        }
    }
}
