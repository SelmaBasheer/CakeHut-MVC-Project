using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace CakeHut.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Name { get; set; }


        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

       
        public string? ImageUrl { get; set; }


        [Required]
        public decimal Weight { get; set; }

        [Required]
        public string Availability { get; set; }

        
        [Range(0, 5)]
        public double Ratings { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        //public decimal DiscountedPrice
        //{
        //    get
        //    {
        //        return DiscountPercentage > 0 ? Price - (Price * DiscountPercentage / 100) : Price;
        //    }
        //}

        //[Range(0, 100)]
        //public int DiscountPercentage { get; set; } = 0; 

        //public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountedPrice { get; set; }


        public ICollection<ProductImage> Images { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number")]
        public int Stock { get; set; }

    }
}
