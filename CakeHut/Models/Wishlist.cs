namespace CakeHut.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public int ProductId { get; set; } 

        // Navigation properties
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
}
