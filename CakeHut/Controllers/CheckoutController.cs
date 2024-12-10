using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Text;
using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CakeHut.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private string PaypalClientId { get; set; } = "";
        private string PaypalSecret { get; set; } = "";
        private string PaypalUrl { get; set; } = "";

        private readonly decimal shippingFee;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(IConfiguration configuration, ApplicationDbContext context
            , UserManager<ApplicationUser> userManager, ILogger<CheckoutController> logger)
        {
            PaypalClientId = configuration["PaypalSettings:ClientId"]!;
            PaypalSecret = configuration["PaypalSettings:Secret"]!;
            PaypalUrl = configuration["PaypalSettings:Url"]!;

            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
            this.context = context;
            this.userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
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

                int cartSize = 0;
                foreach (var item in cartItems)
                {
                    cartSize += item.Quantity;
                }
                if (cartSize == 0 || TempData["DeliveryAddress"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                string couponMessage = TempData["CouponMessage"] as string ?? "";
                ViewBag.CouponMessage = couponMessage;

                string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
                TempData.Keep();

                ViewBag.DeliveryAddress = deliveryAddress;
                ViewBag.Total = total;
                ViewBag.PaypalClientId = PaypalClientId;
                ViewBag.CartSize = cartSize;
                return View();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the checkout page.");
                return View("Error"); 
            }
        }

        [HttpPost]
        public async Task<JsonResult> CreateOrder()
        {
            try
            {
                List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
                
                decimal totalAmount;
                if (TempData["DiscountedTotal"] != null && decimal.TryParse(TempData["DiscountedTotal"].ToString(), out var discountedTotal))
                {
                    totalAmount = discountedTotal;
                }
                else
                {
                    totalAmount = CartHelper.GetSubtotal(cartItems) + shippingFee;
                }


                string deliveryAddress = TempData["DeliveryAddress"]?.ToString();

                if (string.IsNullOrEmpty(deliveryAddress))
                {
                    _logger.LogWarning("Delivery address is missing.");
                    return new JsonResult(new { Id = "", Error = "Delivery address is missing." });
                }

                var appUser = await userManager.GetUserAsync(User);
                if (appUser == null)
                {
                    _logger.LogWarning("User not found while creating PayPal order.");
                    return new JsonResult(new { Id = "", Error = "User not found." });
                }

                string fullName = string.IsNullOrEmpty(appUser.FirstName)
                    ? $"{appUser.FirstName} {appUser.LastName}" 
                    : appUser.FirstName; 


                JsonObject createOrderRequest = new JsonObject
                    {
                        { "intent", "CAPTURE" },
                        { "application_context", new JsonObject
                            {
                                { "shipping_preference", "SET_PROVIDED_ADDRESS" } 
                            }
                        }
                    };

                                JsonObject amount = new JsonObject
                    {
                        { "currency_code", "USD" },
                        { "value", totalAmount.ToString("F2") } 
                    };

                                JsonObject shipping = new JsonObject
                    {
                        { "name", new JsonObject { { "full_name", fullName } } },
                        { "address", new JsonObject
                            {
                                { "address_line_1", deliveryAddress },
                                { "admin_area_2", "City" },         
                                { "admin_area_1", "State" },        
                                { "postal_code", "12345" },         
                                { "country_code", "US" }           
                            }
                        }
                    };

                                JsonObject purchaseUnit1 = new JsonObject
                    {
                        { "amount", amount },
                        { "shipping", shipping }
                    };

                                JsonArray purchaseUnits = new JsonArray
                    {
                        purchaseUnit1
                    };

                createOrderRequest.Add("purchase_units", purchaseUnits);


                string accessToken = await GetPaypalAccessToken();

                
                string url = PaypalUrl + "/v2/checkout/orders";


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                    requestMessage.Content = new StringContent(createOrderRequest.ToString(), null, "application/json");

                    var httpResponse = await client.SendAsync(requestMessage);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var strResponse = await httpResponse.Content.ReadAsStringAsync();
                        var jsonResponse = JsonNode.Parse(strResponse);

                        if (jsonResponse != null)
                        {
                            string paypalOrderId = jsonResponse["id"]?.ToString() ?? "";

                            return new JsonResult(new { Id = paypalOrderId });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a PayPal order.");
            }

            return new JsonResult(new { Id = "" });
        }

        [HttpPost]
        public async Task<JsonResult> CompleteOrder([FromBody] JsonObject data)
        {
            try
            {
                var orderId = data?["orderID"]?.ToString();
                var deliveryAddress = data?["deliveryAddress"]?.ToString();

                if (orderId == null || deliveryAddress == null)
                {
                    return new JsonResult("error");
                }

                // get access token
                string accessToken = await GetPaypalAccessToken();


                string url = PaypalUrl + "/v2/checkout/orders/" + orderId + "/capture";


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                    requestMessage.Content = new StringContent("", null, "application/json");

                    var httpResponse = await client.SendAsync(requestMessage);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var strResponse = await httpResponse.Content.ReadAsStringAsync();
                        var jsonResponse = JsonNode.Parse(strResponse);

                        if (jsonResponse != null)
                        {
                            string paypalOrderStatus = jsonResponse["status"]?.ToString() ?? "";
                            if (paypalOrderStatus == "COMPLETED")
                            {
                                // save the order in the database

                                await SaveOrder(jsonResponse.ToString(), deliveryAddress);
                                return new JsonResult("success");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while completing the PayPal order.");
            }

            return new JsonResult("error");
        }

        private async Task SaveOrder(string paypalResponse, string deliveryAddress)
        {
            try
            {
                var cartItems = CartHelper.GetCartItems(Request, Response, context);

                var appUser = await userManager.GetUserAsync(User);
                if (appUser == null)
                {
                    return;
                }
                decimal total;
                if (TempData["DiscountedTotal"] != null && decimal.TryParse(TempData["DiscountedTotal"].ToString(), out var discountedTotal))
                {
                    total = discountedTotal;
                }
                else
                {
                    total = CartHelper.GetSubtotal(cartItems) + shippingFee;
                }

                var order = new Order
                {
                    ClientId = appUser.Id,
                    Items = cartItems,
                    ShippingFee = shippingFee,
                    DeliveryAddress = deliveryAddress,
                    PaymentMethod = "paypal",
                    PaymentStatus = "accepted",
                    PaymentDetails = paypalResponse,
                    OrderStatus = "created",
                    CreatedAt = DateTime.Now,
                    TotalAmount = total,
                    CouponId = int.TryParse(TempData["CouponId"]?.ToString(), out var couponId) ? couponId : (int?)null
                };

                context.Orders.Add(order);
                context.SaveChanges();

                Response.Cookies.Delete("shopping_cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the order.");
            }
        }

        public async Task<string> Token()
        {
            return await GetPaypalAccessToken();
        }
        

        private async Task<string> GetPaypalAccessToken()
        {
            string accessToken = "";


            string url = PaypalUrl + "/v1/oauth2/token";

            using (var client = new HttpClient())
            {
                string credentials64 =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(PaypalClientId + ":" + PaypalSecret));

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials64);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("grant_type=client_credentials", null
                    , "application/x-www-form-urlencoded");

                var httpResponse = await client.SendAsync(requestMessage);


                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();

                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        accessToken = jsonResponse["access_token"]?.ToString() ?? "";
                    }
                }
            }

            return accessToken;
        }
    }
}
