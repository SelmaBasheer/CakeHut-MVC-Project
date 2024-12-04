using Microsoft.EntityFrameworkCore;

namespace CakeHut.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [Precision(16, 2)]
        public decimal UnitPrice { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = new Product();

        public string Status { get; set; } = "active";

        public bool IsReturned { get; set; } = false;
        public string? ReturnReason { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }
    }
}
