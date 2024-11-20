using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class Address
    {
        [Key]
        public int AddressId { get; set; }

        
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }

        [Required]
        public string Street { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        public string? Landmark { get; set; }

        public bool IsPrimary { get; set; }
    }
}
