using Microsoft.AspNetCore.Identity;

namespace CakeHut.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }

		public bool IsBlocked { get; set; }

        // Navigation property for addresses
        public ICollection<Address> Addresses { get; set; }
    }
}
