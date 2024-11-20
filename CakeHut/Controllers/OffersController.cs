using CakeHut.Data;
using CakeHut.Models;
using CakeHut.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CakeHut.Controllers
{
    public class OffersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OffersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                List<Offer> objOfferList = _context.Offers.Include(o => o.Product).Include(o => o.Category).ToList();
                return View(objOfferList);
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while processing your request. Please try again.";
                return View(new List<Offer>());
            }
        }

        public IActionResult Create()
        {
            try
            {
                OfferVM offerVM = new OfferVM
                {
                    Offer = new Offer(),
                    Categories = _context.Categories.ToList(),
                    Products = _context.Products.ToList()
                };
                return View(offerVM);
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while initializing the offer creation form. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Create(OfferVM offerVM)
        {
            if (ModelState.IsValid)
            {
                if (offerVM.Offer.Offertype == Offer.OfferType.Category)
                {
                    offerVM.Offer.CategoryId = offerVM.SelectedCategoryId;
                    offerVM.Offer.ProductId = null;

                    var existingCategoryOffer = _context.Offers
                        .FirstOrDefault(o => o.CategoryId == offerVM.Offer.CategoryId);

                    if (existingCategoryOffer != null)
                    {
                        TempData["error"] = "An offer already exists for the selected Category";
                        offerVM.Categories = _context.Categories.ToList();
                        offerVM.Products = _context.Products.ToList();
                        return View(offerVM);
                    }
                }
                else if (offerVM.Offer.Offertype == Offer.OfferType.Product)
                {
                    offerVM.Offer.ProductId = offerVM.SelectedProductId;
                    offerVM.Offer.CategoryId = null;

                    var existingProductOffer = _context.Offers
                        .FirstOrDefault(o => o.ProductId == offerVM.Offer.ProductId);

                    if (existingProductOffer != null)
                    {
                        TempData["error"] = "An offer already exists for the selected Product";
                        offerVM.Categories = _context.Categories.ToList();
                        offerVM.Products = _context.Products.ToList();
                        return View(offerVM);
                    }
                }

                _context.Offers.Add(offerVM.Offer);
                _context.SaveChanges();

                UpdateDiscountedPrices();
                TempData["success"] = "Offer created successfully";
                return RedirectToAction(nameof(Index));
            }

            offerVM.Categories = _context.Categories.ToList();
            offerVM.Products = _context.Products.ToList();
            return View(offerVM);
        }

        public IActionResult Edit(int id)
        {
            try
            {
                var offer = _context.Offers
                    .Include(o => o.Product)
                    .Include(o => o.Category)
                    .FirstOrDefault(o => o.OfferId == id);

                if (offer == null)
                {
                    return NotFound();
                }

                OfferVM offerVM = new OfferVM
                {
                    Offer = offer,
                    Categories = _context.Categories.ToList(),
                    Products = _context.Products.ToList(),
                    SelectedCategoryId = offer.CategoryId,
                    SelectedProductId = offer.ProductId
                };
                return View(offerVM);
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while fetching the offer for editing. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Edit(OfferVM offerVM)
        {
            if (ModelState.IsValid)
            {
                if (offerVM.Offer.Offertype == Offer.OfferType.Category)
                {
                    offerVM.Offer.CategoryId = offerVM.SelectedCategoryId;
                    offerVM.Offer.ProductId = null;
                }
                else if (offerVM.Offer.Offertype == Offer.OfferType.Product)
                {
                    offerVM.Offer.ProductId = offerVM.SelectedProductId;
                    offerVM.Offer.CategoryId = null;
                }

                _context.Offers.Update(offerVM.Offer);
                _context.SaveChanges();

                UpdateDiscountedPrices();
                TempData["success"] = "Offer updated successfully";
                return RedirectToAction(nameof(Index));
            }

            offerVM.Categories = _context.Categories.ToList();
            offerVM.Products = _context.Products.ToList();
            return View(offerVM);
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var offer = _context.Offers
                    .Include(o => o.Product)
                    .Include(o => o.Category)
                    .FirstOrDefault(o => o.OfferId == id);

                if (offer == null)
                {
                    return NotFound();
                }

                return View(offer);
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while retrieving the offer for deletion. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var offer = _context.Offers.FirstOrDefault(o => o.OfferId == id);

                if (offer == null)
                {
                    return NotFound();
                }

                _context.Offers.Remove(offer);
                _context.SaveChanges();

                UpdateDiscountedPrices();
                TempData["success"] = "Offer deleted successfully";
            }
            catch (Exception)
            {
                TempData["error"] = "An error occurred while deleting the offer. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void UpdateDiscountedPrices()
        {
            var products = _context.Products.Include(p => p.Category).ToList();

            foreach (var product in products)
            {
                var categoryOffer = _context.Offers
                    .Where(o => o.CategoryId == product.CategoryId && o.Offertype == Offer.OfferType.Category)
                    .OrderByDescending(o => o.OfferDiscount)
                    .FirstOrDefault();

                var productOffer = _context.Offers
                    .Where(o => o.ProductId == product.Id && o.Offertype == Offer.OfferType.Product)
                    .OrderByDescending(o => o.OfferDiscount)
                    .FirstOrDefault();

                double maxDiscount = 0;

                if (categoryOffer != null)
                {
                    maxDiscount = categoryOffer.OfferDiscount;
                }

                if (productOffer != null && productOffer.OfferDiscount > maxDiscount)
                {
                    maxDiscount = productOffer.OfferDiscount;
                }

                product.DiscountedPrice = maxDiscount > 0
                    ? Math.Round(product.Price * (1 - ((decimal)maxDiscount / 100)), 2)
                    : product.Price;

                _context.Products.Update(product);
            }

            _context.SaveChanges();
        }
    }
}
