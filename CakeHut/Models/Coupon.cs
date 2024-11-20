using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } // Coupon Code

        [Required]
        [Range(1, 100)]
        [Display(Name = "Discount Percentage")]
        public int DiscountPercentage { get; set; } // Discount Percentage

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } // Expiry Date

        [Required]
        public bool IsActive { get; set; } // Whether the coupon is active

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
