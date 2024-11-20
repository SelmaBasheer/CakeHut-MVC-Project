using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CakeHut.Controllers
{
    [Authorize] 
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WishlistController> _logger;


        public WishlistController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<WishlistController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Wishlist
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var wishlistItems = _context.Wishlist
                                        .Where(w => w.UserId == user.Id)
                                        .Select(w => w.Product)
                                        .ToList();

                return View(wishlistItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the wishlist items.");
                return View("Error"); // Ensure an "Error" view exists to handle errors gracefully.
            }
        }

        // POST: Wishlist/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var existingItem = _context.Wishlist.FirstOrDefault(w => w.UserId == user.Id && w.ProductId == productId);

                if (existingItem == null)
                {
                    var wishlistItem = new Wishlist
                    {
                        UserId = user.Id,
                        ProductId = productId
                    };

                    _context.Wishlist.Add(wishlistItem);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding an item to the wishlist.");
                return RedirectToAction("Index");
            }
        }

        // POST: Wishlist/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var wishlistItem = _context.Wishlist.FirstOrDefault(w => w.UserId == user.Id && w.ProductId == productId);

                if (wishlistItem != null)
                {
                    _context.Wishlist.Remove(wishlistItem);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing an item from the wishlist.");
                return RedirectToAction("Index");
            }
        }

        // GET: Wishlist/Count
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { count = 0 });

                var count = _context.Wishlist.Count(w => w.UserId == user.Id);
                return Json(new { count });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while counting wishlist items.");
                return Json(new { count = 0 });
            }
        }

    }
}