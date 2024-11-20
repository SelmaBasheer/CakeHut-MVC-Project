using CakeHut.Data;
using CakeHut.Models.ViewModels;
using CakeHut.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using DinkToPdf;
using DinkToPdf.Contracts;
using OfficeOpenXml;

namespace CakeHut.Controllers
{
    public class SalesReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Sales Report
        public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate, string filterType = "custom")
        {
            DateTime start = startDate ?? DateTime.UtcNow;
            DateTime end = endDate ?? DateTime.UtcNow;

            // Set predefined filters
            switch (filterType.ToLower())
            {
                case "daily":
                    start = DateTime.UtcNow.Date;
                    end = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
                    break;
                case "weekly":
                    start = DateTime.UtcNow.AddDays(-7);
                    break;
                case "monthly":
                    start = DateTime.UtcNow.AddMonths(-1);
                    break;
            }

            // Query filtered orders
            var orders = await _context.Orders
                .Include(o => o.AppliedCoupon)
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .ToListAsync();

            var totalSalesCount = orders.Count;
            var totalOrderAmount = orders.Sum(o => o.TotalAmount);
            //var totalDiscounts = orders.Sum(o => o.Items.Sum(i => i.DiscountedPrice - i.Price));
            //var totalCouponDeductions = orders.Where(o => o.AppliedCoupon != null)
                                              //.Sum(o => o.AppliedCoupon.OfferDiscount);

            var viewModel = new SalesReportViewModel
            {
                TotalSalesCount = totalSalesCount,
                TotalOrderAmount = totalOrderAmount,
                //TotalDiscounts = totalDiscounts,
                //TotalCouponDeductions = totalCouponDeductions,
                ReportStartDate = start,
                ReportEndDate = end,
                Orders = orders
            };

            return View(viewModel);
        }
    }
}
