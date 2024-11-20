using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CakeHut.Controllers
{
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly int pageSize = 2;

        public CouponController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index(int? pageIndex)
        {
            try
            {
                if (pageIndex == null || pageIndex < 1)
                    pageIndex = 1;

                // Update the IsActive status for expired coupons
                var expiredCoupons = context.Coupons
                    .Where(c => c.ExpiryDate < DateTime.UtcNow && c.IsActive)
                    .ToList();

                foreach (var coupon in expiredCoupons)
                {
                    coupon.IsActive = false;
                }

                if (expiredCoupons.Count > 0)
                    await context.SaveChangesAsync();

                var query = context.Coupons.OrderByDescending(c => c.CreatedDate).AsNoTracking();

                int totalItems = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var coupons = await query
                    .Skip((pageIndex.Value - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.PageIndex = pageIndex;
                ViewBag.TotalPages = totalPages;

                return View(coupons);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while loading coupons. Please try again later.";
                Console.WriteLine(ex.Message);
                return View("Error");
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                // Check if the expiry date is valid
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    ModelState.AddModelError("ExpiryDate", "Expiry date must be today or a future date.");
                }

                // Check for duplicate coupon code
                bool exists = await context.Coupons
                    .AnyAsync(c => c.Code == coupon.Code);

                if (exists)
                {
                    ModelState.AddModelError("Code", "A coupon with this code already exists.");
                    return View(coupon);
                }
                try
                {
                    context.Add(coupon);
                    await context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "An error occurred while creating the coupon. Please try again.";
                    Console.WriteLine(ex.Message);
                }
            }
            return View(coupon);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var coupon = await context.Coupons.FindAsync(id);
                if (coupon == null)
                    return NotFound();

                return View(coupon);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while loading the coupon for editing. Please try again.";
                Console.WriteLine(ex.Message);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }
                

            // Retrieve the existing coupon from the database
            var existingCoupon = context.Coupons.Find(id);
            if (existingCoupon == null)
            {
                return NotFound();
            }

            // Check if the admin is trying to activate an inactive coupon
            if (!existingCoupon.IsActive && coupon.IsActive)
            {
                // Check if the expiry date is today or in the future
                if (coupon.ExpiryDate < DateTime.Today)
                {
                    ModelState.AddModelError("IsActive", "Cannot activate a coupon with an expiry date in the past. Set the expiry date to today or a future date.");
                }
            }

            // Ensure the expiry date is valid
            if (coupon.ExpiryDate < DateTime.Today)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry date must be today or a future date.");
            }
            else
            {
                coupon.IsActive = true;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // context.Update(coupon);
                    existingCoupon.Code = coupon.Code;
                    existingCoupon.DiscountPercentage = coupon.DiscountPercentage;
                    existingCoupon.ExpiryDate = coupon.ExpiryDate;
                    existingCoupon.IsActive = coupon.IsActive;

                    await context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "An error occurred while updating the coupon. Please try again.";
                    Console.WriteLine(ex.Message);
                }
            }
            return View(coupon);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var coupon = await context.Coupons.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                if (coupon == null)
                    return NotFound();

                return View(coupon);
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while loading the coupon for deletion. Please try again.";
                Console.WriteLine(ex.Message);
                return View("Error");
            }
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var coupon = await context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    ViewData["ErrorMessage"] = "Coupon not found.";
                    return View("Error");
                }

                context.Coupons.Remove(coupon);
                await context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while deleting the coupon. Please try again.";
                Console.WriteLine(ex.Message);
                return View("Error");
            }
        }
    }
}
