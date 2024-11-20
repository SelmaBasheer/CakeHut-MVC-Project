using CakeHut.Data;
using CakeHut.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CakeHut.Controllers
{
    [Authorize(Roles = "user")]
    [Route("/Client/Orders/{action=Index}/{id?}")]
    public class ClientOrdersController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly WalletService _walletService;
        private readonly int pageSize = 5;

        public ClientOrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, WalletService walletService)
        {
            this.context = context;
            this.userManager = userManager;
            _walletService = walletService;
        }

        public async Task<IActionResult> Index(int pageIndex)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            IQueryable<Order> query = context.Orders
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
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = context.Orders.Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.ClientId == currentUser.Id).FirstOrDefault(o => o.Id == id);

            var coupon = await context.Orders
                .Include(o => o.AppliedCoupon)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (coupon != null)
            {
                // Check if the AppliedCoupon is null
                if (order.AppliedCoupon == null)
                {
                    // Log or inspect to see what the CouponId is
                    Console.WriteLine($"Order ID: {order.Id}, Coupon ID: {order.CouponId}");
                }
            }


            if (order == null)
            {
                return RedirectToAction("Index");
            }


            return View(order);
        }

        public async Task<IActionResult> CancelOrder(int id)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == currentUser.Id);
            if (order == null || order.OrderStatus != "created") // Only allow cancellation if status is "created"
            {
                return RedirectToAction("Details", new { id });
            }

            //order.OrderStatus = "canceled";
            //context.Update(order);
            //await context.SaveChangesAsync();
            //TempData["Message"] = "Order has been successfully canceled.";


            //return RedirectToAction("Details", new { id });

            if (order.OrderStatus == "canceled")
            {
                return BadRequest("Order is already canceled.");
            }

            // Mark order as canceled
            order.OrderStatus = "canceled";
            order.CancellationDate = DateTime.UtcNow;

            // Calculate the subtotal (without coupon discount)
            decimal subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            Console.WriteLine($"subtotal: {subtotal}");

            // Calculate the discount if coupon is applied
            decimal discountAmount = 0;
            if (order.AppliedCoupon != null)
            {
                discountAmount = (order.AppliedCoupon.DiscountPercentage / 100M) * subtotal;
            }

            Console.WriteLine($"discountAmount: {discountAmount}");

            // Calculate the total paid amount (after applying the discount)
            decimal totalPaid = subtotal - discountAmount;

            // Include the shipping fee in the refund
            decimal refundAmount = totalPaid + order.ShippingFee;


            Console.WriteLine($"Refund Amount: {refundAmount}");

            // Add refund to user's wallet
            await _walletService.AddTransactionAsync(currentUser.Id, refundAmount, "Refund for canceled order", "Refund");

            // Save changes
            await context.SaveChangesAsync();

            // Notify user that refund has been added to their wallet
            TempData["Message"] = $"Your order has been canceled. A refund of {refundAmount:F2} Rs has been credited to your wallet.";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await context.Orders
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
                var table = new PdfPTable(4)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(new float[] { 40f, 20f, 20f, 20f }); // Column widths

                // Add Table Headers
                table.AddCell(new PdfPCell(new Phrase("Product", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Unit Price", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Quantity", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Total", FontFactory.GetFont(FontFactory.HELVETICA_BOLD))) { HorizontalAlignment = Element.ALIGN_CENTER });

                // Add Table Data
                foreach (var item in order.Items)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.Product.Name, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{item.UnitPrice} Rs", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase($"{item.UnitPrice * item.Quantity} Rs", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);

                // Add Totals
                decimal subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
                document.Add(new Paragraph($"\nSubtotal: {subtotal} Rs", normalFont));

                if (order.ShippingFee > 0)
                {
                    document.Add(new Paragraph($"Shipping Fee: {order.ShippingFee} Rs", normalFont));
                }

                if (order.AppliedCoupon != null)
                {
                    var discount = (order.AppliedCoupon.DiscountPercentage / 100M) * subtotal;
                    document.Add(new Paragraph($"Discount: -{discount:F2} Rs", normalFont));
                }

                document.Add(new Paragraph($"Total Amount: {order.TotalAmount} Rs", titleFont));

                // Close the Document
                document.Close();

                // Return PDF as a downloadable file
                return File(memoryStream.ToArray(), "application/pdf", $"Invoice_Order_{order.Id}.pdf");
            }
        }
    
    }
}
