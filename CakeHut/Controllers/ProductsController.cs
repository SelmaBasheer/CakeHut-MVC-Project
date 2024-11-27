using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace CakeHut.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly int pageSize = 4;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }

        public IActionResult Index(int? pageIndex)
        {
            IQueryable<Product> query = context.Products;

            query = query.OrderByDescending(p => p.CreatedAt);

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


        public async Task<IActionResult> Create()
        {
            List<Category> categories = await context.Categories.ToListAsync();
            IEnumerable<SelectListItem> CategoryList = categories
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            ViewBag.CategoryList = CategoryList;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductDto productDto)
        {
            if (productDto.ImageFiles == null || !productDto.ImageFiles.Any())
            {
                ModelState.AddModelError("ImageFiles", "At least one image file is required");
            }

            if (!ModelState.IsValid)
            {
                List<Category> categories = await  context.Categories.ToListAsync();
                ViewBag.CategoryList = categories
                    .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
                return View(productDto);
            }

            
            var existingProduct = await context.Products
                .FirstOrDefaultAsync(p => p.Name == productDto.Name && p.CategoryId == productDto.CategoryId);

            if (existingProduct != null)
            {
                ModelState.AddModelError("", "A product with this name and category already exists.");
                return View(productDto);
            }

            
            Product product = new Product()
            {
                Name = productDto.Name,
                CategoryId = productDto.CategoryId,
                Description = productDto.Description,
                Price = productDto.Price,
                Weight = productDto.Weight,
                Stock = productDto.Stock,  
                Availability = productDto.Stock > 0 ? "In Stock" : "Out of Stock",
                DiscountedPrice = productDto.Price
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            
            List<ProductImage> images = new List<ProductImage>();
            string primaryImageUrl = string.Empty;

            foreach (var imageFile in productDto.ImageFiles)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFiles", "Only image files (.jpg, .jpeg, .png) are allowed.");
                    return View(productDto);
                }

                string newFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{imageFile.FileName}";
                string imageFullPath = Path.Combine(environment.WebRootPath, "cakes", newFileName);

                using (var stream = new FileStream(imageFullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                images.Add(new ProductImage
                {
                    ImageUrl = newFileName,
                    ProductId = product.Id
                });

                if (string.IsNullOrEmpty(primaryImageUrl))
                {
                    primaryImageUrl = newFileName;
                }
            }

            context.ProductImages.AddRange(images);
            await context.SaveChangesAsync();

            
            product.ImageUrl = primaryImageUrl; 
            context.Products.Update(product);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Products");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var productDto = new ProductDto()
            {
                Name = product.Name,
                CategoryId = product.CategoryId,
                Description = product.Description,
                Price = product.Price,
                Weight = product.Weight,
                Availability = product.Availability,
                Images = product.Images.Select(i => new ProductImageDto { Id = i.Id, ImageUrl = i.ImageUrl }).ToList(),
                Stock = product.Stock
            };

            List<Category> categories = await context.Categories.ToListAsync();
            ViewBag.CategoryList = categories
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

            ViewData["ProductId"] = product.Id;

            return View(productDto);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProductDto productDto, List<int> RemoveImageIds)
        {
            var product = await context.Products
                .Include(p => p.Images) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            product.Name = productDto.Name;
            product.CategoryId = productDto.CategoryId;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.Weight = productDto.Weight;
            product.Stock = productDto.Stock;  
            product.Availability = productDto.Stock > 0 ? "In Stock" : "Out of Stock";  


            if (RemoveImageIds != null && RemoveImageIds.Any())
            {
                var imagesToRemove = product.Images.Where(i => RemoveImageIds.Contains(i.Id)).ToList();
                foreach (var image in imagesToRemove)
                {
                    
                    var imagePath = Path.Combine(environment.WebRootPath, "cakes", image.ImageUrl);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }

                    
                    context.ProductImages.Remove(image);
                }
            }
            
            if (productDto.ImageFiles != null && productDto.ImageFiles.Any())
            {
                foreach (var imageFile in productDto.ImageFiles)
                {
                    string newFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{imageFile.FileName}";
                    string imageFullPath = Path.Combine(environment.WebRootPath, "cakes", newFileName);

                    using (var stream = new FileStream(imageFullPath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = newFileName,
                        ProductId = product.Id
                    });
                }
            }

            context.Products.Update(product);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Products");
        }



        public IActionResult Delete(int id)
        {
            var product = context.Products.Include(p => p.Images).FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            
            foreach (var image in product.Images)
            {
                var imagePath = Path.Combine(environment.WebRootPath, "cakes", image.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath); 
                }
            }
            
            context.Products.Remove(product);
            context.SaveChanges();

            return RedirectToAction("Index", "Products");
        }

    }
}
