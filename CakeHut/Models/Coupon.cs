using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } 

        [Required]
        [Range(1, 100)]
        [Display(Name = "Discount Percentage")]
        public int DiscountPercentage { get; set; } 

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } 

        [Required]
        public bool IsActive { get; set; } 

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
