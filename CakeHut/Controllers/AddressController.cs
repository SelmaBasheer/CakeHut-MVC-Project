using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CakeHut.Controllers
{
    public class AddressController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly int pageSize = 5;
        public AddressController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        public async Task<IActionResult> Index(int pageIndex)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            IQueryable<Address> query = _context.Addresses.Where(a => a.UserId == user.Id);

            if (pageIndex <= 0)
            {
                pageIndex = 1;
            }


            decimal count = query.Count();
            int totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);


            var addresses = query.ToList();

            ViewBag.Addresses = addresses;
            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;

            return View();
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Address address)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                address.UserId = user.Id; // Set UserId here
                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User not found.");
            }

            //var user = await _userManager.GetUserAsync(User);
            //address.UserId = user?.Id;

            //ModelState.Remove("User");

            //if (ModelState.IsValid)
            //{
            //    _context.Addresses.Add(address);
            //    await _context.SaveChangesAsync();
            //    return RedirectToAction("Index");
            //}

            //var errors = ModelState.Values.SelectMany(v => v.Errors);
            //foreach (var error in errors)
            //{
            //    Console.WriteLine(error.ErrorMessage); // Replace with logger if needed
            //}

            return View(address);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            return View(address);
        }
        
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Address address)
        {
            

            if (id != address.AddressId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null) { 
                address.UserId = user.Id;
            }

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                var existingAddress = await _context.Addresses.FindAsync(id);
                if (existingAddress == null)
                {
                    return NotFound();
                }

                // Update the properties of the existing address
                existingAddress.HomeAddress = address.HomeAddress;
                existingAddress.Street = address.Street;
                existingAddress.City = address.City;
                existingAddress.State = address.State;
                existingAddress.PostalCode = address.PostalCode;
                existingAddress.Landmark = address.Landmark;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Console.WriteLine(error.ErrorMessage); // Replace with logger if needed
            }
            return View(address);
        }
        

        //[HttpPost]
        //public async Task<IActionResult> Edit(Address address)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Addresses.Update(address);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    return View(address);
        //}
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address != null)
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
