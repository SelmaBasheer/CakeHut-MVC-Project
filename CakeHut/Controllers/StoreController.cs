using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CakeHut.Controllers
{
    public class StoreController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly int pageSize = 8;

        public StoreController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index(int? pageIndex, string? search, string? sort)
        {
            //var products = context.Products.OrderByDescending(p => p.Id).ToList();

            IQueryable<Product> query = context.Products;

            // search functionality
            if (search != null && search.Length > 0)
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            // sort functionality
            if (sort == "price_asc")
            {
                query = query.OrderBy(p => p.Price);
            }
            else if (sort == "price_desc")
            {
                query = query.OrderByDescending(p => p.Price);
            }
            else if (sort == "name_asc")
            {
                query = query.OrderBy(p => p.Name);
            }
            else if (sort == "name_desc")
            {
                query = query.OrderByDescending(p => p.Name);
            }
            else if (sort == "availability")
            {
                query = query.OrderBy(p => p.Availability);
            }
            else if (sort == "rating")
            {
                query = query.OrderByDescending(p => p.Ratings);
            }
            else
            {
                // newest products first
                query = query.OrderByDescending(p => p.Id);
            }


            if (pageIndex == null || pageIndex < 1)
            {
                pageIndex = 1;
            }

            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);
            query = query.Skip(((int)pageIndex - 1) * pageSize).Take(pageSize);

            var products = query.ToList();

            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;

            var storeSearchModel = new StoreSearchModel()
            {
                Search = search,
                Sort = sort
            };

            //return View(users);

            ViewBag.Products = products;
            return View(storeSearchModel);
        }

        public IActionResult Details(int id)
        {
            var product = context.Products
                         .Include(p => p.Reviews)
                         .Include(p => p.Images)
                         .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return RedirectToAction("Index", "Store");
            }

            //product.DiscountedPrice = CalculateDiscountedPrice(product);

            return View(product);
        }

        [HttpPost]
        public IActionResult SubmitRating(int ProductId, int Rating, string CustomerName, string Content)
        {
            // Find the product by ID
            //var product = context.Products.Include(p => p.Reviews).FirstOrDefault(p => p.Id == ProductId);

            var product = context.Products.Include(p => p.Reviews)
                                  .FirstOrDefault(p => p.Id == ProductId);
            if (product == null)
            {
                return NotFound();
            }

            
            var review = new Review
            {
                ProductId = ProductId,
                CustomerName = CustomerName,
                Content = Content,
                Rating = Rating,
                Date = DateTime.Now
            };

            
            context.Reviews.Add(review);

            
            product.Ratings = context.Reviews.Where(r => r.ProductId == ProductId)
                                      .Average(r => r.Rating);

            
            //context.Products.Update(product);
            context.SaveChanges();

            
            return RedirectToAction("Details", new { id = ProductId });
        }
        

    }
}
