using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CakeHut.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;

        public HomeController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            var products = context.Products.OrderByDescending(p => p.Id).Take(4).ToList();
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        public IActionResult AdminHome()
        {
            return RedirectToAction("Index", "Dashboard");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
