using CakeHut.Data;
using CakeHut.Models;
using CakeHut.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace CakeHut.Controllers
{
	[Authorize(Roles = "admin")]
	public class ProductController : Controller
    {
        private readonly ApplicationDbContext _applicationDb;

        public ProductController(ApplicationDbContext applicationDb)
        {
            _applicationDb = applicationDb;
        }
        public IActionResult Index()
        {
            List<Product> productsList = _applicationDb.Products.ToList();
            return View(productsList);
        }

        public async Task<IActionResult> Create()
        {
            List<Category> categories = await _applicationDb.Categories.ToListAsync();
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
        public async Task<IActionResult> Create(Product productObj, IFormFile imageFile)
        {
            
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Process the file (e.g., save to server or database)
                    //var filePath = Path.Combine("wwwroot/images", imageFile.FileName); 
                    var fileName = Path.GetFileName(imageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Cakes");
                    var filePath = Path.Combine(uploadPath, fileName);

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    productObj.ImageUrl = "/Cakes/" + imageFile.FileName; 
                }

                _applicationDb.Products.Add(productObj);
                _applicationDb.SaveChanges();
                TempData["success"] = "Product Created Successfully";
                return RedirectToAction("Index");
            }

            // If ModelState is invalid, populate the categories again
            List<Category> categories = await _applicationDb.Categories.ToListAsync();
            IEnumerable<SelectListItem> CategoryList = categories
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            ViewBag.CategoryList = CategoryList;

            return View(productObj);
        }

        
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product? productData = _applicationDb.Products.FirstOrDefault(c => c.Id == id);
            if (productData == null)
            {
                return NotFound();
            }

            List<Category> categories = await _applicationDb.Categories.ToListAsync();
            IEnumerable<SelectListItem> CategoryList = categories
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                    Selected = u.Id == productData.CategoryId
                });
            
            ProductVM productVM = new ProductVM()
            {
                Product = productData,
                CategorySelectListItems = CategoryList
            };

            return View(productVM);
        }


        [HttpPost]
        public IActionResult Edit(Product productObj)
        {

            if (ModelState.IsValid)
            {
                _applicationDb.Products.Update(productObj);
                _applicationDb.SaveChanges();
                TempData["success"] = "Product Updated Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product? productData = _applicationDb.Products.FirstOrDefault(c => c.Id == id);
            if (productData == null)
            {
                return NotFound();
            }

            return View(productData);
        }

        [HttpPost, ActionName("Delete")]
        //[HttpDelete]
        public IActionResult DeletePost(int? id)
        {
            Product? productData = _applicationDb.Products.FirstOrDefault(c => c.Id == id);
            if (productData == null)
            {
                return NotFound();
            }

            _applicationDb.Products.Remove(productData);
            _applicationDb.SaveChanges();
            TempData["success"] = "Product Deleted Successfully";
            return RedirectToAction("Index");
        }
    }
}
