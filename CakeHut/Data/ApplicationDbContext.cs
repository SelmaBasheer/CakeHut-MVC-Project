using CakeHut.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace CakeHut.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        {
            
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Coupon> Coupons { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Wishlist> Wishlist { get; set; }

        public DbSet<ProductOffer> ProductOffers { get; set; }

        public DbSet<CategoryOffer> CategoryOffers { get; set; }

        public DbSet<Offer> Offers { get; set; }

        public DbSet<Wallet> Wallets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().Property(t => t.DisplayOrder).HasColumnName("DisplayOrder");

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1 , Name="Cakes", DisplayOrder=1},
                new Category { Id = 2, Name = "CupCakes", DisplayOrder = 2 },
                new Category { Id = 3, Name = "TrendingCakes", DisplayOrder = 3 }
                );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Chocolate Fudge Cake",
                    CategoryId = 1,
                    Description = "Moist chocolate cake with rich fudge frosting",
                    Price = 1000,
                    ImageUrl = "chocolate-fudge-cake.jpg",
                    Weight = 1,
                    Availability = "In Stock"
                },
                new Product
                {
                    Id = 2,
                    Name = "Butterscotch Cake",
                    CategoryId = 1,
                    Description = "Delicate butterscotch cake with creamy praline frosting",
                    Price = 1000,
                    ImageUrl = "vanilla-bean-cake.jpg",
                    Weight = 1,
                    Availability = "In Stock"
                },
                new Product
                {
                    Id = 3,
                    Name = "Red Velvet Cake",
                    CategoryId = 1,
                    Description = "Moist red velvet cake with cream cheese frosting",
                    Price = 1000,
                    ImageUrl = "red-velvet-cake.jpg",
                    Weight = 1,
                    Availability = "In Stock"
                }
                );
        }
    }
}
