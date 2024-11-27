using CakeHut.Data;
using CakeHut.Models;
using CakeHut.Models.ViewModels;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.IO;
using ClosedXML.Excel;


namespace CakeHut.Controllers
{
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashBoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1, int itemsPerPage = 5, string filter = "weekly")
        {
            // Retrieve all data
            var orders = _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Items)
                .OrderByDescending(o => o.Id)
                .ToList();
            ViewBag.Orders = orders.Take(5);
            var products = _context.Products.ToList();
            var categories = _context.Categories.ToList();
            var orderDetails = _context.OrderItems.Include(oi => oi.Product).ToList();

            DateTime today = DateTime.Now;
            DateTime filterStartDate;

            switch (filter.ToLower())
            {
                case "weekly":
                    filterStartDate = today.AddDays(-7);
                    break;
                case "monthly":
                    filterStartDate = today.AddMonths(-1);
                    break;
                case "yearly":
                    filterStartDate = today.AddYears(-1);
                    break;
                default:
                    filterStartDate = today.AddDays(-7); // Default to weekly
                    break;
            }

            var filteredOrders = orders
                .Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= today)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var filterChartLabels = filteredOrders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => g.Key.ToShortDateString())
                .ToList();

            var filterChartData = filteredOrders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => g.Sum(o => (double)o.TotalAmount))
                .ToList();

            // Calculate product quantities sold
            var productQuantitiesSold = products.ToDictionary(
                p => p.Id,
                p => orderDetails.Where(od => od.ProductId == p.Id).Sum(od => od.Quantity)
            );

            // Top selling products
            var topSellingProducts = products
                .OrderByDescending(p => productQuantitiesSold.GetValueOrDefault(p.Id, 0))
                .Take(5)
                .ToList();

            // Category sales
            var categorySales = categories.ToDictionary(
                c => c.Id,
                c => orderDetails.Where(od => od.Product.CategoryId == c.Id).Sum(od => od.Quantity)
            );

            var topSellingCategories = categories
                .OrderByDescending(c => categorySales.GetValueOrDefault(c.Id, 0))
                .Take(1)
                .ToList();

            // Order status counts
            int shippedCount = orders.Count(o => o.OrderStatus == "delivered");
            int approvedCount = orders.Count(o => o.OrderStatus == "accepted");
            int cancelledCount = orders.Count(o => o.OrderStatus == "canceled");

            // Total counts
            int productCount = products.Count();
            int orderCount = orders.Count();
            int categoryCount = categories.Count();

            // Sales metrics
            decimal totalSales = orders.Sum(o => o.TotalAmount);

            
            DateTime oneWeekAgo = today.AddDays(-7);
            DateTime oneMonthAgo = today.AddMonths(-1);
            DateTime oneYearAgo = today.AddYears(-1);

            double totalRevenueToday = orders
                .Where(o => o.CreatedAt.Date == today.Date)
                .Sum(o => (double)o.TotalAmount);

            double totalRevenueThisWeek = orders
                .Where(o => o.CreatedAt >= oneWeekAgo && o.CreatedAt <= today)
                .Sum(o => (double)o.TotalAmount);

            double totalRevenueThisMonth = orders
                .Where(o => o.CreatedAt >= oneMonthAgo && o.CreatedAt <= today)
                .Sum(o => (double)o.TotalAmount);

            double totalRevenueThisYear = orders
                .Where(o => o.CreatedAt >= oneYearAgo && o.CreatedAt <= today)
                .Sum(o => (double)o.TotalAmount);

            // Chart data
            var chartData = new List<double> { totalRevenueToday, totalRevenueThisWeek, totalRevenueThisMonth, totalRevenueThisYear };
            var chartLabels = new List<string> { "Today", "This Week", "This Month", "This Year" };

            // Paginated orders for the last week
            var ordersLastWeek = orders
                .Where(o => o.CreatedAt >= oneWeekAgo && o.CreatedAt <= today)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var numberOfOrdersLastWeek = ordersLastWeek.Count;

            Console.WriteLine($"OrdersLastWeek Count: {ordersLastWeek.Count}");

            // Prepare ViewModel
            var dashboardVM = new DashboardVM
            {
                Orders = ordersLastWeek,
                ProductCount = productCount,
                OrderCount = orderCount,
                CategoryCount = categoryCount,
                TopSellingProducts = topSellingProducts,
                ProductQuantitiesSold = productQuantitiesSold,
                TopSellingCategories = topSellingCategories,
                CategorySales = categorySales,
                ShippedCount = shippedCount,
                ApprovedCount = approvedCount,
                CancelledCount = cancelledCount,
                TotalSales = totalSales,
                TotalRevenueToday = totalRevenueToday,
                TotalRevenueThisWeek = totalRevenueThisWeek,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                TotalRevenueThisYear = totalRevenueThisYear,
                ChartData = chartData,
                ChartLabels = chartLabels,
                CurrentPage = page,
                ItemsPerPage = itemsPerPage,
                NumberOfOrdersLastWeek = numberOfOrdersLastWeek,
                FilterChartLabels = filterChartLabels,
                FilterChartData = filterChartData,
                SelectedFilter = filter
            };

            return View(dashboardVM);
        }

        public IActionResult GetRevenueData()
        {
            return View();
        }

        public IActionResult DownloadSalesReportExcel()
        {
            var orders = _context.Orders
        .Include(o => o.Items)
        .ThenInclude(oi => oi.Product)
        .ToList();

            // Calculate top-selling products
            var topSellingProducts = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Product)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Take(5)
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToList();

            var allProducts = _context.Products.ToList();

            using (var stream = new MemoryStream())
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Sales Report");

                    // Sales Report Header
                    worksheet.Cell(1, 1).Value = "Sales Report";
                    worksheet.Cell(2, 1).Value = $"Date: {DateTime.Now:dd-MM-yyyy}";

                    int currentRow = 4;

                    // Top Selling Products Table
                    worksheet.Cell(currentRow++, 1).Value = "Top Selling Products";
                    worksheet.Cell(currentRow, 1).Value = "Product Name";
                    worksheet.Cell(currentRow, 2).Value = "Total Sold";
                    worksheet.Cell(currentRow, 3).Value = "Total Revenue";

                    foreach (var product in topSellingProducts)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = product.ProductName;
                        worksheet.Cell(currentRow, 2).Value = product.TotalSold;
                        worksheet.Cell(currentRow, 3).Value = product.TotalRevenue.ToString("C");
                    }

                    currentRow++;

                    // All Products Table
                    worksheet.Cell(currentRow++, 1).Value = "All Products";
                    worksheet.Cell(currentRow, 1).Value = "Product ID";
                    worksheet.Cell(currentRow, 2).Value = "Product Name";
                    worksheet.Cell(currentRow, 3).Value = "Price";

                    foreach (var product in allProducts)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = product.Id;
                        worksheet.Cell(currentRow, 2).Value = product.Name;
                        worksheet.Cell(currentRow, 3).Value = product.Price.ToString("C");
                    }

                    currentRow++;

                    // Orders Table
                    worksheet.Cell(currentRow++, 1).Value = "Orders";
                    worksheet.Cell(currentRow, 1).Value = "Order ID";
                    worksheet.Cell(currentRow, 2).Value = "Client Name";
                    worksheet.Cell(currentRow, 3).Value = "Total Amount";
                    worksheet.Cell(currentRow, 4).Value = "Order Date";

                    foreach (var order in orders)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = order.Id;
                        worksheet.Cell(currentRow, 2).Value = order.ClientId;
                        worksheet.Cell(currentRow, 3).Value = order.TotalAmount.ToString("C");
                        worksheet.Cell(currentRow, 4).Value = order.CreatedAt.ToString("dd-MM-yyyy");
                    }

                    workbook.SaveAs(stream);
                }

                stream.Position = 0;
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesReport.xlsx");
            }
        }

        public IActionResult DownloadSalesReportPDF()
        {
            var orders = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .ToList();

            // Calculate top-selling products and categories
            var topSellingProducts = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Product)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Take(5)
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToList();

            //var topSellingCategories = orders
            //    .SelectMany(o => o.Items)
            //    .GroupBy(i => i.Product.Category)
            //    .OrderByDescending(g => g.Sum(i => i.Quantity))
            //    .Take(1)
            //    .Select(g => new
            //    {
            //        CategoryName = g.Key.Name,
            //        TotalSold = g.Sum(i => i.Quantity),
            //        TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
            //    })
            //    .ToList();

            var allProducts = _context.Products.ToList();

            using (var stream = new MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                PdfWriter.GetInstance(doc, stream).CloseStream = false;
                doc.Open();

                // Sales Report Header
                doc.Add(new Paragraph("Sales Report"));
                doc.Add(new Paragraph($"Date: {DateTime.Now:dd-MM-yyyy}"));
                doc.Add(new Paragraph("\n"));



                // Top Selling Products Table
                doc.Add(new Paragraph("Top Selling Products"));
                PdfPTable topProductsTable = new PdfPTable(3);
                topProductsTable.AddCell("Product Name");
                topProductsTable.AddCell("Total Sold");
                topProductsTable.AddCell("Total Revenue");

                foreach (var product in topSellingProducts)
                {
                    topProductsTable.AddCell(product.ProductName);
                    topProductsTable.AddCell(product.TotalSold.ToString());
                    topProductsTable.AddCell(product.TotalRevenue.ToString("C"));
                }

                doc.Add(topProductsTable);
                doc.Add(new Paragraph("\n"));

                // Top Selling Categories Table
                //doc.Add(new Paragraph("Top Selling Categories"));
                //PdfPTable topCategoriesTable = new PdfPTable(3);
                //topCategoriesTable.AddCell("Category Name");
                //topCategoriesTable.AddCell("Total Sold");
                //topCategoriesTable.AddCell("Total Revenue");

                //foreach (var category in topSellingCategories)
                //{
                //    topCategoriesTable.AddCell(category.CategoryName);
                //    topCategoriesTable.AddCell(category.TotalSold.ToString());
                //    topCategoriesTable.AddCell(category.TotalRevenue.ToString("C"));
                //}

                //doc.Add(topCategoriesTable);
                //doc.Add(new Paragraph("\n"));

                // All Products Table
                doc.Add(new Paragraph("All Products"));
                PdfPTable allProductsTable = new PdfPTable(3);
                allProductsTable.AddCell("Product ID");
                allProductsTable.AddCell("Product Name");
                allProductsTable.AddCell("Price");

                foreach (var product in allProducts)
                {
                    allProductsTable.AddCell(product.Id.ToString());
                    allProductsTable.AddCell(product.Name);
                    allProductsTable.AddCell(product.Price.ToString("C"));
                }

                doc.Add(allProductsTable);

                doc.Add(new Paragraph("\n"));

                // Orders Table
                doc.Add(new Paragraph("Orders"));
                PdfPTable ordersTable = new PdfPTable(4);
                ordersTable.AddCell("Order ID");
                ordersTable.AddCell("Client Name");
                ordersTable.AddCell("Total Amount");
                ordersTable.AddCell("Order Date");

                foreach (var order in orders)
                {
                    ordersTable.AddCell(order.Id.ToString());
                    ordersTable.AddCell(order.ClientId);
                    ordersTable.AddCell(order.TotalAmount.ToString("C"));
                    ordersTable.AddCell(order.CreatedAt.ToString("dd-MM-yyyy"));
                }

                doc.Add(ordersTable);

                doc.Close();

                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "SalesReport.pdf");
            }
        }

        public IActionResult New()
        {
            return View();
        }


    }
}
