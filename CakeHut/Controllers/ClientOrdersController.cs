using CakeHut.Data;
using CakeHut.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace CakeHut.Controllers
{
    [Authorize(Roles = "user")]
    [Route("/Client/Orders/{action=Index}/{id?}")]
    public class ClientOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientOrdersController> _logger;
        private readonly WalletService _walletService;
        private readonly int pageSize = 5;
        private readonly decimal shippingFee;

        public ClientOrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            ILogger<ClientOrdersController> logger, 
            IConfiguration configuration, WalletService walletService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _walletService = walletService;
            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
        }

        public async Task<IActionResult> Index(int pageIndex)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            IQueryable<Order> query = _context.Orders
                .Include(o => o.Items).OrderByDescending(o => o.Id)
                .Where(o => o.ClientId == currentUser.Id);

            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }


            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);


            var orders = query.ToList();

            ViewBag.Orders = orders;
            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;

            return View();
        }


        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = _context.Orders.Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.ClientId == currentUser.Id).FirstOrDefault(o => o.Id == id);

            var coupon = await _context.Orders
                .Include(o => o.AppliedCoupon)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (coupon != null)
            {
                if (order.AppliedCoupon == null)
                {
                    Console.WriteLine($"Order ID: {order.Id}, Coupon ID: {order.CouponId}");
                }
            }


            if (order == null)
            {
                return RedirectToAction("Index");
            }


            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrderItem(int orderId, int itemId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.AppliedCoupon)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == currentUser.Id);

                if (order == null)
                {
                    TempData["Message"] = "Order not found.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                var orderItem = order.Items.FirstOrDefault(i => i.Id == itemId);
                if (orderItem == null || orderItem.Status == "canceled")
                {
                    TempData["Message"] = "Invalid or already canceled item.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                orderItem.Status = "canceled";

                var remainingItemsTotal = order.Items
                    .Where(i => i.Status != "canceled")
                    .Sum(i => i.UnitPrice * i.Quantity);


                decimal discount = 0;
                if (order.AppliedCoupon != null)
                {
                    discount = (order.AppliedCoupon.DiscountPercentage / 100m) * remainingItemsTotal;
                }

                decimal itemDiscountShare = 0;
                if (order.AppliedCoupon != null)
                {
                    itemDiscountShare = (orderItem.UnitPrice * order.AppliedCoupon.DiscountPercentage) / 100m;
                }
                decimal refundAmount = (orderItem.UnitPrice * orderItem.Quantity) - itemDiscountShare;



                if (remainingItemsTotal == 0)
                {
                    refundAmount += order.ShippingFee; 
                    order.OrderStatus = "canceled"; 
                    order.CancellationDate = DateTime.UtcNow; 
                }
                else
                {
                    order.TotalAmount = remainingItemsTotal + order.ShippingFee - discount;
                }

                await _context.SaveChangesAsync();

                if (order.PaymentStatus == "accepted")
                {
                    await _walletService.AddRefundToWalletAsync(
                        currentUser.Id,
                        refundAmount,
                        "Refund",
                        "Item",
                        orderItem.Id
                    );
                }

                TempData["Message"] = $"Item {orderItem.Product.Name} has been canceled. A refund of {refundAmount:F2} Rs has been credited to your wallet.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred while canceling the item. Please try again.";
                
                _logger.LogError(ex, "Error canceling order item.");
            }

            return RedirectToAction("Details", new { id = orderId });
        }




        public async Task<IActionResult> CancelOrder(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == currentUser.Id);
            if (order == null || order.OrderStatus != "created") 
            {
                return RedirectToAction("Details", new { id });
            }

            if (order.OrderStatus == "canceled")
            {
                return BadRequest("Order is already canceled.");
            }

            order.OrderStatus = "canceled";
            order.CancellationDate = DateTime.UtcNow;

            decimal refundAmount = order.TotalAmount;

            Console.WriteLine($"Refund Amount: {refundAmount}");

            await _walletService.AddRefundToWalletAsync(
                        currentUser.Id,
                        refundAmount,
                        "Refund",
                        "Order",
                        order.Id
                    );

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Your order has been canceled. A refund of {refundAmount:F2} Rs has been credited to your wallet.";

            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> ReturnOrderItem(int orderId, int itemId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == user.Id);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                var item = order.Items.FirstOrDefault(i => i.Id == itemId);

                if (item == null)
                {
                    return NotFound("Item not found.");
                }

                // Check if return is allowed (within 5 hours)
                if (order.CreatedAt.AddHours(5) < DateTime.UtcNow || item.Status != "delivered")
                //if (item.Status != "delivered")
                {
                    TempData["ErrorMessage"] = "Return is not allowed for this item as the time exceeded";
                    return RedirectToAction("Details", new { id = orderId });
                }
                ViewBag.ItemId = itemId;
                return View("ReturnOrderItem", order); // Return a form view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while preparing the return form.");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReturnRequest(int orderItemId, int orderId, string returnReason)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == user.Id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                var orderItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);

                if (orderItem == null || orderItem.Status != "delivered")
                {
                    TempData["ErrorMessage"] = "Return is not allowed for this item.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                // Check if return is still within the allowed time frame
                if (orderItem.DeliveredAt == null || orderItem.DeliveredAt.Value.AddHours(5) < DateTime.UtcNow)
                {
                    TempData["ErrorMessage"] = "Return period has expired.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                // Update item status and save reason for return
                orderItem.Status = "returnRequested";
                orderItem.ReturnReason = returnReason;
                orderItem.ReturnRequestedAt = DateTime.UtcNow;
                //orderItem.IsReturned = true;
                //order.OrderStatus = "returnRequested";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your return request has been submitted successfully.";
                return RedirectToAction("Details", new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while submitting the return request.");
                TempData["ErrorMessage"] = "Unable to process the return request.";
                return RedirectToAction("Details", new { id = orderId });
            }
        }




        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.AppliedCoupon)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == currentUser.Id);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            using (var memoryStream = new MemoryStream())
            {
                // Create iTextSharp Document
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter.GetInstance(document, memoryStream).CloseStream = false;

                document.Open();

                // Add Invoice Header
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                document.Add(new Paragraph("CakeHut Invoice", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph("\n"));

                // Add Order Details
                document.Add(new Paragraph($"Order ID: {order.Id}", normalFont));
                document.Add(new Paragraph($"Order Date: {order.CreatedAt:MM/dd/yyyy}", normalFont));
                document.Add(new Paragraph($"Delivery Address: {order.DeliveryAddress}", normalFont));
                document.Add(new Paragraph($"Payment Method: {order.PaymentMethod}", normalFont));
                document.Add(new Paragraph("\n"));

                // Add Table for Order Items
                var table = new PdfPTable(5) // Add Status column
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 30f, 20f, 20f, 20f, 20f }); // Column widths

                // Add Table Headers
                table.AddCell(new PdfPCell(new Phrase("Product", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Unit Price", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Quantity", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Status", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Add Table Data
                foreach (var item in order.Items)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.Product.Name, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{item.UnitPrice} Rs", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase($"{item.UnitPrice * item.Quantity} Rs", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(item.Status, normalFont)));
                }

                document.Add(table);

                // Calculate Subtotal, Discount, and Total
                decimal subtotal = order.Items.Where(i => i.Status != "canceled").Sum(i => i.UnitPrice * i.Quantity);
                decimal shippingFee = subtotal > 0 ? order.ShippingFee : 0; 
                decimal discount = 0;

                if (order.AppliedCoupon != null)
                {
                    discount = (order.AppliedCoupon.DiscountPercentage / 100M) * subtotal;
                }

                decimal total = subtotal + shippingFee - discount;

                // Add Totals to Invoice
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph($"Subtotal: {subtotal} Rs", normalFont));
                document.Add(new Paragraph($"Shipping Fee: {shippingFee} Rs", normalFont));

                if (order.AppliedCoupon != null)
                {
                    document.Add(new Paragraph($"Discount ({order.AppliedCoupon.Code} coupon applied!): -{discount:F2} Rs", normalFont));
                }

                document.Add(new Paragraph($"Total Amount: {total} Rs", titleFont));

                // If all items are canceled
                if (order.Items.All(i => i.Status == "canceled"))
                {
                    document.Add(new Paragraph("\nNote: All items in this order have been canceled.", FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.RED)));
                }

                // Close the Document
                document.Close();

                // Return PDF as a downloadable file
                return File(memoryStream.ToArray(), "application/pdf", $"Invoice_Order_{order.Id}.pdf");
            }
        }


        


    }
}
