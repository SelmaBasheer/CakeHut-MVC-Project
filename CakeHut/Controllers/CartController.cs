using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace CakeHut.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly decimal shippingFee;

        public CartController(ApplicationDbContext context, IConfiguration configuration
            , UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
        }

        public async Task<IActionResult> Index()
        {
            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return Unauthorized(); 
            }

            var addresses = await context.Addresses
                 .Where(a => a.UserId == appUser.Id)
                 .ToListAsync() ?? new List<Address>();

            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal subtotal = CartHelper.GetSubtotal(cartItems);

            var activeCoupons = await context.Coupons
                .Where(c => c.IsActive && c.ExpiryDate >= DateTime.Now)
                .ToListAsync();

            ViewBag.ActiveCoupons = activeCoupons;

            if (cartItems.Count == 0)
            {
                ViewBag.ErrorMessage = "Your cart is empty. Please add items to proceed to checkout.";
                ViewBag.CartItems = null;
            }
            else
            {
                ViewBag.CartItems = cartItems;
                ViewBag.ShippingFee = shippingFee;
                ViewBag.Subtotal = subtotal;
                ViewBag.Total = subtotal + shippingFee;
            }
            ViewBag.Addresses = addresses;

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(CheckoutDto model)
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            int maxQuantityPerPerson = 5;

            // Track total quantity across all products
            int totalQuantity = 0;

            foreach (var item in cartItems)
            {
                totalQuantity += item.Quantity; // Increment the total quantity

                // Check if the total quantity exceeds the limit
                if (totalQuantity > maxQuantityPerPerson)
                {
                    ModelState.AddModelError("Quantity", $"You can only have a maximum of {maxQuantityPerPerson} items in total.");
                    return View(model);
                }
                var product =  context.Products.Find(item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("Product", $"Product with ID {item.ProductId} not found.");
                    return View(model);
                }

                // Check if the available stock is sufficient
                if (product.Stock < item.Quantity)
                {
                    // Show an error if stock is insufficient
                    ViewBag.ErrorMessage = $"Insufficient stock for {product.Name}. Only {product.Stock} items left.";
                    return View(model);
                }
            }

            decimal subtotal = CartHelper.GetSubtotal(cartItems);
            decimal originalTotal = subtotal + shippingFee; // Calculate the original total
            ViewBag.OriginalTotal = originalTotal;

            // Apply coupon if selected
           // decimal discount = 0;
            if (model.SelectedCouponId.HasValue)
            {
                var selectedCoupon = await context.Coupons
                    .FirstOrDefaultAsync(c => c.Id == model.SelectedCouponId.Value && c.IsActive && c.ExpiryDate >= DateTime.Now);

                if (selectedCoupon != null)
                {
                   
                    decimal discount = ((decimal)selectedCoupon.DiscountPercentage / 100) * subtotal;
                    ViewBag.Discount = discount;
                    decimal discountedTotal = originalTotal - discount; // Calculate discounted total
                    ViewBag.Total = discountedTotal; // Update total with discount
                    TempData["DiscountedTotal"] = discountedTotal.ToString("F2"); // Store discounted total in TempData
                    //ViewBag.CouponMessage = $"Coupon applied! You got {selectedCoupon.DiscountPercentage}% discount. New total: {discountedTotal:C2}.";

                    TempData["CouponMessage"] = $"Coupon applied! You got {selectedCoupon.DiscountPercentage}% discount.";
                    TempData["CouponId"] = selectedCoupon.Id.ToString();
                }
            }
            else
            {
                TempData.Remove("DiscountedTotal");
                TempData.Remove("CouponMessage");
                TempData.Remove("CouponId");
                ViewBag.Total = originalTotal; // No coupon applied
            }
           


            ViewBag.CartItems = cartItems;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Subtotal = subtotal;
            


            //var discountedPrice = product.Price - (product.Price * coupon.DiscountPercentage / 100);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if shopping cart is empty or not
            if (cartItems.Count == 0)
            {
                ViewBag.ErrorMessage = "Your cart is empty";
                return View(model);
            }

            TempData["DeliveryAddress"] = model.DeliveryAddress;
            TempData["PaymentMethod"] = model.PaymentMethod;
            //TempData["DiscountedTotal"] = (subtotal + shippingFee - discount).ToString("F2");

            if (model.PaymentMethod == "paypal" || model.PaymentMethod == "credit_card")
            {
                return RedirectToAction("Index", "Checkout");
            }

            return RedirectToAction("Confirm");
        }



        public IActionResult Confirm()
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal total = 0;
            if (TempData["DiscountedTotal"] != null && decimal.TryParse(TempData["DiscountedTotal"].ToString(), out var discountedTotal))
            {
                total = discountedTotal;
            }
            else
            {
                total = CartHelper.GetSubtotal(cartItems) + shippingFee;
            }

            string couponMessage = TempData["CouponMessage"] as string;
            ViewBag.CouponMessage = couponMessage;

            int cartSize = 0;
            foreach (var item in cartItems)
            {
                cartSize += item.Quantity;
            }


            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            string paymentMethod = TempData["PaymentMethod"] as string ?? "";
            TempData.Keep();


            if (cartSize == 0 || deliveryAddress.Length == 0 || paymentMethod.Length == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.DeliveryAddress = deliveryAddress;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.Total = total;
            ViewBag.CartSize = cartSize;

            return View();
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Confirm(int any)
        {
            var cartItems = CartHelper.GetCartItems(Request, Response, context);

            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            string paymentMethod = TempData["PaymentMethod"] as string ?? "";
            TempData.Keep();

            if (cartItems.Count == 0 || deliveryAddress.Length == 0 || paymentMethod.Length == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Use the discounted total if available
            decimal total;
            if (TempData["DiscountedTotal"] != null && decimal.TryParse(TempData["DiscountedTotal"].ToString(), out var discountedTotal))
            {
                total = discountedTotal;
            }
            else
            {
                total = CartHelper.GetSubtotal(cartItems) + shippingFee; // Fallback to original total if no discount
            }

            // save the order
            var order = new Order
            {
                ClientId = appUser.Id,
                Items = cartItems,
                ShippingFee = shippingFee,
                DeliveryAddress = deliveryAddress,
                PaymentMethod = paymentMethod,
                PaymentStatus = "pending",
                PaymentDetails = "",
                OrderStatus = "created",
                CreatedAt = DateTime.Now,
                TotalAmount = total,
                CouponId = int.TryParse(TempData["CouponId"]?.ToString(), out var couponId) ? couponId : (int?)null

            };

            context.Orders.Add(order);


            // Decrease stock for each item in the cart
            foreach (var item in cartItems)
            {
                Console.WriteLine($"Processing cart item with ID: {item.Id}, Quantity: {item.Quantity}");

                var product = await context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Decrease stock based on the quantity purchased
                    product.Stock -= item.Quantity;

                    if (product.Stock < 0)
                    {
                        // Handle case where stock becomes negative, which should not happen if cart is validated properly
                        //return BadRequest($"Insufficient stock for {product.Name}.");
                        ViewBag.ErrorMessage = $"Insufficient stock for {product.Name}.";
                    }

                    // Update the product stock in the database
                    context.Products.Update(product);
                }
            }

            context.SaveChanges();


            // delete the shopping cart cookie
            Response.Cookies.Delete("shopping_cart");

            ViewBag.SuccessMessage = "Order created successfully";

            return View();
        }


    }
}
