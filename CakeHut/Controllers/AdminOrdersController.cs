using CakeHut.Data;
using CakeHut.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.Export.HtmlExport.StyleCollectors.StyleContracts;

namespace CakeHut.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("/Admin/Orders/{action=Index}/{id?}")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly int pageSize = 5;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WalletService _walletService;

        public AdminOrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
             WalletService walletService)
        {
            _context = context;
            _userManager = userManager;
            _walletService = walletService;
        }

        public IActionResult Index(int pageIndex)
        {
            IQueryable<Order> query = _context.Orders.Include(o => o.Client)
                .Include(o => o.Items).OrderByDescending(o => o.Id);

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

        // Order Details
        public async Task<IActionResult> Details(int id)
        {
            var order = _context.Orders.Include(o => o.Client).Include(o => o.Items)
                .ThenInclude(oi => oi.Product).FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

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

            ViewBag.NumOrders = _context.Orders.Where(o => o.ClientId == order.ClientId).Count();

            return View(order);
        }

        // Edit order status
        public IActionResult Edit(int id, string? payment_status, string? order_status)
        {
            var order = _context.Orders
                .Include(o => o.Items) 
                .FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return RedirectToAction("Index");
            }

            if (payment_status == null && order_status == null)
            {
                return RedirectToAction("Details", new { id });
            }

            if (payment_status != null)
            {
                order.PaymentStatus = payment_status;
            }

            if (order_status != null)
            {
                order.OrderStatus = order_status;

                if (order_status == "delivered")
                {
                    foreach (var item in order.Items)
                    {
                        if (item.Status == "active" || item.Status == "processing")
                        {
                            item.Status = "delivered";
                            item.DeliveredAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            bool allItemsReturned = order.Items.All(item => item.Status == "returned");

            if (allItemsReturned)
            {
                order.OrderStatus = "returned";
            }

            _context.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> EditItem(int orderId, int itemId, string item_status)
        {
            var order = _context.Orders.Include(o => o.Items)
                .Include(o => o.AppliedCoupon)
                .FirstOrDefault(o => o.Id == orderId);

            var orderItem = _context.OrderItems.FirstOrDefault(o => o.Id == itemId);
            if (order != null)
            {
                //var orderItem = _context.OrderItems.FirstOrDefault(o => o.Id == itemId);
                if (orderItem != null)
                {
                    orderItem.Status = item_status;
                    
                }
            }

            bool allItemsReturned = order.Items.All(item => item.Status == "returned");

            if (allItemsReturned)
            {
                order.OrderStatus = "returned";
            }

            if (item_status == "returned")
            {
                var currentUser = await _userManager.FindByIdAsync(order.ClientId);
                if (currentUser != null)
                {
                    var remainingItemsTotal = order.Items
                    .Where(i => i.Status != "canceled" && i.Status != "returned")
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
                        order.OrderStatus = "returned";
                        refundAmount = order.TotalAmount;
                    }
                    

                    if (order.PaymentStatus == "accepted")
                    {
                        await _walletService.AddRefundToWalletAsync(
                            currentUser.Id,
                            refundAmount,
                            "Refund",
                            "Return",
                            orderItem.Id
                        );
                    }

                    TempData["Message"] = $"Item {orderItem.Product.Name} has been returned. A refund of {refundAmount:F2} Rs has been credited to the {currentUser.FirstName}'s wallet.";
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { itemId });
        }

    }
}
