using Microsoft.EntityFrameworkCore;

namespace CakeHut.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [Precision(16, 2)]
        public decimal UnitPrice { get; set; }

        // Foreign key to Product
        public int ProductId { get; set; }

        // navigation property
        public Product Product { get; set; } = new Product();
    }
}
