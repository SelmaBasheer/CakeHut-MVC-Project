//using CakeHut.Data;
//using CakeHut.Models;
//using CakeHut.Models.ViewModels;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace CakeHut.Controllers
//{
//    public class ReportsController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public ReportsController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public IActionResult Index()
//        {
//            return View();
//        }

//        public IActionResult SalesReport(SalesReportViewModel report)
//        {
//            // Logic to retrieve the sales report
//            return View(report);
//        }

//        public IActionResult DailyReport()
//        {
//            var today = DateTime.Today;
//            var orders = _context.Orders
//                .Where(o => o.CreatedAt >= today && o.CreatedAt < today.AddDays(1))
//                .Include(o => o.Items)
//                .ThenInclude(i => i.Product)
//                .ToList();

//            var report = GenerateSalesReport(orders);
//            return View("SalesReport", report);
//        }

//        public IActionResult WeeklyReport()
//        {
//            var startOfWeek = DateTime.Today.AddDays(-7);
//            var orders = _context.Orders
//                .Where(o => o.CreatedAt >= startOfWeek && o.CreatedAt < DateTime.Today.AddDays(1))
//                .Include(o => o.Items)
//                .ThenInclude(i => i.Product)
//                .ToList();

//            var report = GenerateSalesReport(orders);
//            return View("SalesReport", report);
//        }

//        public IActionResult YearlyReport()
//        {
//            var startOfYear = new DateTime(DateTime.Today.Year, 1, 1);
//            var orders = _context.Orders
//                .Where(o => o.CreatedAt >= startOfYear && o.CreatedAt < DateTime.Today.AddDays(1))
//                .Include(o => o.Items)
//                .ThenInclude(i => i.Product)
//                .ToList();

//            var report = GenerateSalesReport(orders);
//            return View("SalesReport", report);
//        }

//        public IActionResult CustomReport(DateTime startDate, DateTime endDate)
//        {
//            var orders = _context.Orders
//                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
//                .Include(o => o.Items)
//                .ThenInclude(i => i.Product)
//                .ToList();

//            var report = GenerateSalesReport(orders);
//            return View("SalesReport", report);
//        }


//        private SalesReportViewModel GenerateSalesReport(List<Order> orders)
//        {
//            decimal totalSalesAmount = orders.Sum(o => o.TotalAmount);
//            int totalOrdersCount = orders.Count;
//            int totalItemsSold = orders.Sum(o => o.Items.Sum(i => i.Quantity));
//            decimal totalDiscountAmount = orders.Sum(o => o.Items.Sum(i =>
//                i.Product.Price * i.Quantity - (i.UnitPrice * i.Quantity)));

//            return new SalesReportViewModel
//            {
//                TotalSalesAmount = totalSalesAmount,
//                TotalDiscountAmount = totalDiscountAmount,
//                TotalOrdersCount = totalOrdersCount,
//                TotalItemsSold = totalItemsSold,
//                Orders = orders
//            };
//        }

//    }
//}
