using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class Offer
    {
        [Key]
        public int OfferId { get; set; }

        [Required]
        public string OfferCode { get; set; }

        [Required]
        public OfferType Offertype { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public double OfferDiscount { get; set; }

        [Required]
        public string OfferDescription { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public enum OfferType
        {
            Category,
            Product,
            Referral
        }


        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

    }
}
