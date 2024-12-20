﻿using CakeHut.Models;
using System.Text.Json;

namespace CakeHut.Data
{
    public class CartHelper
    {
        public static Dictionary<int, int> GetCartDictionary(HttpRequest request, HttpResponse response)
        {
            string cookieValue = request.Cookies["shopping_cart"] ?? "";

            try
            {
                var cart = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));
                Console.WriteLine("[CartHelper] cart=" + cookieValue + " -> " + cart);
                var dictionary = JsonSerializer.Deserialize<Dictionary<int, int>>(cart);
                if (dictionary != null)
                {
                    return dictionary;
                }
            }
            catch (Exception)
            {
            }

            if (cookieValue.Length > 0)
            {
                // this cookie is not valid => delete it
                response.Cookies.Delete("shopping_cart");
            }

            return new Dictionary<int, int>();
        }


        public static int GetCartSize(HttpRequest request, HttpResponse response)
        {

            if (!request.HttpContext.User.Identity.IsAuthenticated)
            {
                return 0; 
            }

            var cartDictionary = GetCartDictionary(request, response);
            
            return cartDictionary?.Count ?? 0;
        }

        public static List<OrderItem> GetCartItems(HttpRequest request, HttpResponse response
            , ApplicationDbContext context)
        {
            var cartItems = new List<OrderItem>();

            var cartDictionary = GetCartDictionary(request, response);
            foreach (var pair in cartDictionary)
            {
                int productId = pair.Key;
                int quantity = pair.Value;
                var product = context.Products.Find(productId);
                if (product == null) continue;

                decimal priceToUse = product.DiscountedPrice < product.Price ? product.DiscountedPrice : product.Price;

                var item = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = priceToUse,
                    Product = product,
                };

                cartItems.Add(item);
            }

            return cartItems;
        }


        public static decimal GetSubtotal(List<OrderItem> cartItems)
        {
            decimal subtotal = 0;

            foreach (var item in cartItems)
            {
                subtotal += item.Quantity * item.UnitPrice;
            }

            return subtotal;
        }
    }
}
