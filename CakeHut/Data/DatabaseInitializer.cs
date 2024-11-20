using CakeHut.Models;
using Microsoft.AspNetCore.Identity;

namespace CakeHut.Data
{
    public class DatabaseInitializer
    {
        public static async Task SeedDataAsync(UserManager<ApplicationUser>? userManager,
            RoleManager<IdentityRole>? roleManager)
        {
            if (userManager == null || roleManager == null)
            {
                Console.WriteLine("userManager or roleManager is null => exit");
                return;
            }

            // check if we have the admin role or not
            var exists = await roleManager.RoleExistsAsync("admin");
            if (!exists)
            {
                Console.WriteLine("Admin role is not defined and will be created");
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }

            // check if we have the user role or not
            exists = await roleManager.RoleExistsAsync("user");
            if (!exists)
            {
                Console.WriteLine("User role is not defined and will be created");
                await roleManager.CreateAsync(new IdentityRole("user"));
            }


            // check if we have at least one admin user or not
            var adminUsers = await userManager.GetUsersInRoleAsync("admin");
            if (adminUsers.Any())
            {
                // Admin user already exists => exit
                Console.WriteLine("Admin user already exists => exit");
                return;
            }


            // create the admin user
            var user = new ApplicationUser()
            {
                FirstName = "Admin",
                LastName = "Admin",
                UserName = "admin@gmail.com", // UserName will be used to authenticate the user
                Email = "admin@gmail.com",
                CreatedDate = DateTime.Now,
            };

            string initialPassword = "Admin123";


            var result = await userManager.CreateAsync(user, initialPassword);
            if (result.Succeeded)
            {
                // set the user role
                await userManager.AddToRoleAsync(user, "admin");
                Console.WriteLine("Admin user created successfully! Please update the initial password!");
                Console.WriteLine("Email: " + user.Email);
                Console.WriteLine("Initial password: " + initialPassword);
            }
        }
    }
}

