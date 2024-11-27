using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Text;
using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Identity;

namespace CakeHut.Controllers
{
    public class PaymentController : Controller
    {
        private string PaypalClientId { get; set; } = "";
        private string PaypalSecret { get; set; } = "";
        private string PaypalUrl { get; set; } = "";

        private readonly decimal shippingFee;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public PaymentController(IConfiguration configuration, ApplicationDbContext context
            , UserManager<ApplicationUser> userManager)
        {
            PaypalClientId = configuration["PaypalSettings:ClientId"]!;
            PaypalSecret = configuration["PaypalSettings:Secret"]!;
            PaypalUrl = configuration["PaypalSettings:Url"]!;

            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
            this.context = context;
            this.userManager = userManager;
        }
        public IActionResult Index()
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal total = CartHelper.GetSubtotal(cartItems) + shippingFee;

            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            TempData.Keep();

            ViewBag.DeliveryAddress = deliveryAddress;
            ViewBag.Total = total;
            ViewBag.PaypalClientId = PaypalClientId;
            return View();
        }


        [HttpPost]
        public async Task<JsonResult> CreateOrder()
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal totalAmount = CartHelper.GetSubtotal(cartItems) + shippingFee;

            // create the request body
            JsonObject createOrderRequest = new JsonObject();
            createOrderRequest.Add("intent", "CAPTURE");

            JsonObject amount = new JsonObject();
            amount.Add("currency_code", "INR");
            amount.Add("value", totalAmount);

            JsonObject purchaseUnit1 = new JsonObject();
            purchaseUnit1.Add("amount", amount);

            JsonArray purchaseUnits = new JsonArray();
            purchaseUnits.Add(purchaseUnit1);

            createOrderRequest.Add("purchase_units", purchaseUnits);

            // get access token
            string accessToken = await GetPaypalAccessToken();

            // send request
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


            return new JsonResult(new { Id = "" });
        }

        [HttpPost]
        public async Task<JsonResult> CompleteOrder([FromBody] CompleteOrderRequest data)
        {
            if (data == null || string.IsNullOrEmpty(data.OrderID) || string.IsNullOrEmpty(data.DeliveryAddress))
            {
                return new JsonResult("error: invalid data");
            }

            // Get access token
            string accessToken = await GetPaypalAccessToken();
            string url = $"{PaypalUrl}/v2/checkout/orders/{data.OrderID}/capture";

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
                    string paypalOrderStatus = jsonResponse?["status"]?.ToString() ?? "";

                    if (paypalOrderStatus == "COMPLETED")
                    {
                        await SaveOrder(jsonResponse.ToString(), data.DeliveryAddress);
                        return new JsonResult("success");
                    }
                }
                else
                {
                    var errorResponse = await httpResponse.Content.ReadAsStringAsync();
                    Console.WriteLine("Error capturing order: " + errorResponse);
                }
            }

            return new JsonResult("error: unable to capture payment or save order.");
        }

        private async Task SaveOrder(string paypalResponse, string deliveryAddress)
        {
            var cartItems = CartHelper.GetCartItems(Request, Response, context);
            var appUser = await userManager.GetUserAsync(User);

            if (appUser == null || cartItems == null || !cartItems.Any())
            {
                return; 
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
                OrderStatus = "pending",
                CreatedAt = DateTime.Now,
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            Response.Cookies.Delete("shopping_cart");
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
