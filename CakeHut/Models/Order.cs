using Microsoft.EntityFrameworkCore;

namespace CakeHut.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string ClientId { get; set; } = "";
        public ApplicationUser Client { get; set; } = null!;

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        [Precision(16, 2)]
        public decimal ShippingFee { get; set; }

        [Precision(16, 2)]
        public decimal TotalAmount { get; set; }

        public string DeliveryAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string PaymentDetails { get; set; } = ""; 
        public string OrderStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public int? CouponId { get; set; }
        public Coupon? AppliedCoupon { get; set; }

        public DateTime? CancellationDate { get; set; }
    }
}
