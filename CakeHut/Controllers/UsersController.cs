using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CakeHut.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("/Admin/[controller]/{action=Index}/{id?}")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly int pageSize = 5;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
        }
        public IActionResult Index(int? pageIndex)
        {
            IQueryable<ApplicationUser> query = userManager.Users.OrderByDescending(u => u.CreatedDate);

            // pagination functionality
            if (pageIndex == null || pageIndex < 1)
            {
                pageIndex = 1;
            }

            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);
            query = query.Skip(((int)pageIndex - 1) * pageSize).Take(pageSize);

            var users = query.ToList();

            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Users");
            }
            var appUser = await userManager.FindByIdAsync(id);

            if (appUser == null)
            {
                return RedirectToAction("Index", "Users");
            }

            ViewBag.Roles = await userManager.GetRolesAsync(appUser);

            return View(appUser);
        }

		[HttpPost]
		public async Task<IActionResult> BlockUser(string id)
		{
			if (id == null)
			{
				TempData["ErrorMessage"] = "Invalid User ID.";
				return RedirectToAction("Index");
			}

			var user = await userManager.FindByIdAsync(id);
			if (user == null)
			{
				TempData["ErrorMessage"] = "User not found.";
				return RedirectToAction("Index");
			}

			user.IsBlocked = true;
			var result = await userManager.UpdateAsync(user);

			if (result.Succeeded)
			{
				TempData["SuccessMessage"] = "User has been blocked successfully.";
			}
			else
			{
				TempData["ErrorMessage"] = "Error while blocking user.";
			}

			return RedirectToAction("Details", new { id });
		}

		[HttpPost]
		public async Task<IActionResult> UnblockUser(string id)
		{
			if (id == null)
			{
				TempData["ErrorMessage"] = "Invalid User ID.";
				return RedirectToAction("Index");
			}

			var user = await userManager.FindByIdAsync(id);
			if (user == null)
			{
				TempData["ErrorMessage"] = "User not found.";
				return RedirectToAction("Index");
			}

			user.IsBlocked = false;
			var result = await userManager.UpdateAsync(user);

			if (result.Succeeded)
			{
				TempData["SuccessMessage"] = "User has been unblocked successfully.";
			}
			else
			{
				TempData["ErrorMessage"] = "Error while unblocking user.";
			}

			return RedirectToAction("Details", new { id });
		}
	}
}

