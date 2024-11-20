using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CakeHut.Controllers
{
    [Authorize(Roles = "admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _applicationDb;
        private readonly int pageSize = 2;

        public CategoryController(ApplicationDbContext applicationDb)
        {
            _applicationDb = applicationDb;
        }
        public IActionResult Index(int? pageIndex)
        {
            //List<Category> categoriesList = _applicationDb.Categories.ToList();

            IQueryable<Category> query = _applicationDb.Categories;

           // IQueryable<ApplicationUser> query = userManager.Users.OrderByDescending(u => u.CreatedDate);

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

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category categoryObj)
        {
            List<Category> categoriesList = _applicationDb.Categories.ToList();

            if (categoriesList.Any(c => c.Name == categoryObj.Name))
            {
                ModelState.AddModelError("name", "Category Name already exist");
            }

            if (categoryObj.Name == categoryObj.DisplayOrder.ToString()) {

                ModelState.AddModelError("name", "Category Name cannot exactly match the Display Order");
            }
            if (ModelState.IsValid)
            {
                _applicationDb.Categories.Add(categoryObj);
                _applicationDb.SaveChanges();
                TempData["success"]="Category Created Successfully";
                return RedirectToAction("Index");
            }
            return View(categoryObj);
        }


        public IActionResult Edit(int? id)
        {
            if (id == null || id==0) {
                return NotFound();
            }

            Category? categoryData = _applicationDb.Categories.FirstOrDefault(c => c.Id == id);
            //Category? category = _applicationDb.Categories.Find(id);
            if (categoryData == null) {
                return NotFound();
            }

            return View(categoryData);
        }

        [HttpPost]
        public IActionResult Edit(Category categoryObj)
        {
            bool categoryNameExists = _applicationDb.Categories
                .Any(c => c.Name == categoryObj.Name && c.Id != categoryObj.Id);

            if (categoryNameExists)
            {
                ModelState.AddModelError("Name", "Category Name already exists.");
            }

            
            if (categoryObj.Name == categoryObj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("DisplayOrder", "Category Name cannot exactly match the Display Order.");
            }


            if (ModelState.IsValid)
            {
                _applicationDb.Categories.Update(categoryObj);
                _applicationDb.SaveChanges();
                TempData["success"] = "Category Updated Successfully";
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

            Category? categoryData = _applicationDb.Categories.FirstOrDefault(c => c.Id == id);
            //Category? category = _applicationDb.Categories.Find(id);
            if (categoryData == null)
            {
                return NotFound();
            }

            return View(categoryData);
        }

        [HttpPost, ActionName("Delete")]
        //[HttpDelete]
        public IActionResult DeletePost(int? id)
        {
            Category? categoryData = _applicationDb.Categories.FirstOrDefault(c => c.Id == id);
            if (categoryData == null)
            {
                return NotFound(); 
            }

            _applicationDb.Categories.Remove(categoryData);
            _applicationDb.SaveChanges();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");
        }

    }
}
