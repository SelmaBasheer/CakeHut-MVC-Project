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
using DocumentFormat.OpenXml.InkML;
using OfficeOpenXml;


namespace CakeHut.Controllers
{
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly int pageSize = 10;

        public DashBoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1, int itemsPerPage = 5, string filter = "weekly")
        {
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
                    filterStartDate = today.AddDays(-7);
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
                CancelledCount = cancelledCount,
                TotalSales = totalSales,
                TotalRevenueToday = totalRevenueToday,
                TotalRevenueThisWeek = totalRevenueThisWeek,
                TotalRevenueThisMonth = totalRevenueThisMonth,
                TotalRevenueThisYear = totalRevenueThisYear,
                ChartData = chartData,
                ChartLabels = chartLabels,
                NumberOfOrdersLastWeek = numberOfOrdersLastWeek,
                FilterChartLabels = filterChartLabels,
                FilterChartData = filterChartData,
                SelectedFilter = filter
            };

            return View(dashboardVM);
        }


        [HttpGet]
        public IActionResult GetChartData(string filter = "weekly")
        {
            var orders = _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Items)
                .OrderByDescending(o => o.Id)
                .ToList();

            DateTime today = DateTime.Now;
            DateTime filterStartDate;

            // Filter based on the selected time period
            switch (filter.ToLower())
            {
                case "weekly":
                    filterStartDate = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                    break;
                case "monthly":
                    filterStartDate = new DateTime(today.Year, today.Month, 1);
                    break;
                case "yearly":
                    filterStartDate = new DateTime(today.Year, 1, 1);
                    break;
                default:
                    filterStartDate = today.AddDays(-7);
                    break;
            }

            // Filter orders by the selected time period
            var filteredOrders = orders
                .Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= today)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var chartLabels = new List<string>();
            var chartData = new List<double>();

            switch (filter.ToLower())
            {
                case "weekly":
                    chartLabels = Enum.GetNames(typeof(DayOfWeek)).ToList(); // Monday, Tuesday, ...
                    chartData = Enumerable.Range(0, 7)
                        .Select(i => filteredOrders
                            .Where(o => o.CreatedAt.Date == filterStartDate.AddDays(i).Date)
                            .Sum(o => (double)o.TotalAmount))
                        .ToList();
                    break;

                case "monthly":
                    var weeksInMonth = Enumerable.Range(0, 5)
                        .Select(i => filterStartDate.AddDays(i * 7).ToString("MMM dd"))
                        .ToList();
                    chartLabels = weeksInMonth;
                    chartData = weeksInMonth
                        .Select(week => filteredOrders
                            .Where(o => o.CreatedAt.Date >= filterStartDate.AddDays(chartLabels.IndexOf(week) * 7)
                                        && o.CreatedAt.Date < filterStartDate.AddDays((chartLabels.IndexOf(week) + 1) * 7))
                            .Sum(o => (double)o.TotalAmount))
                        .ToList();
                    break;

                case "yearly":
                    chartLabels = Enumerable.Range(1, 12)
                        .Select(month => new DateTime(today.Year, month, 1).ToString("MMM"))
                        .ToList();
                    chartData = chartLabels
                        .Select(month => filteredOrders
                            .Where(o => o.CreatedAt.Month == chartLabels.IndexOf(month) + 1)
                            .Sum(o => (double)o.TotalAmount))
                        .ToList();
                    break;
            }

            var maxValue = chartData.Any() ? chartData.Max() : 0; // Max value from chartData
            var yAxisMax = Math.Ceiling(maxValue / 10000) * 10000; // Round up to the nearest 10,000
            var yAxisStep = yAxisMax / 5; // Divide into 5 steps
            var yAxisValues = Enumerable.Range(1, 5).Select(i => (int)(i * yAxisStep)).ToList();

            // Return the updated chart data as JSON
            return Json(new { ChartData = chartData, ChartLabels = chartLabels, YAxisValues = yAxisValues });
        }


        //Sale Report Filtering
        public IActionResult Invoice(DateTime? startDate, DateTime? endDate, int? pageIndex, string? filter = "weekly")
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    startDate = DateTime.Now.AddDays(-7);
                    endDate = DateTime.Now;
                }

                var filteredOrders = _context.Orders
                    .Include(o => o.Client)
                    .Where(order => order.CreatedAt >= startDate && order.CreatedAt <= endDate)
                    .OrderByDescending(order => order.CreatedAt);


                // pagination functionality
                if (pageIndex == null || pageIndex < 1)
                {
                    pageIndex = 1;
                }

                decimal count = filteredOrders.Count();
                int totalPages = (int)Math.Ceiling(count / pageSize);

                var paginatedOrders = filteredOrders
                    .OrderByDescending(order => order.CreatedAt) 
                    .Skip(((int)pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.PageIndex = pageIndex;
                ViewBag.TotalPages = totalPages;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;

                double totalRevenue = (double)filteredOrders.Sum(order => order.TotalAmount);
                int cancelledCount = filteredOrders.Count(order => order.OrderStatus == "canceled");
                int deliveredCount = filteredOrders.Count(order => order.OrderStatus == "delivered");
                int returnedCount = filteredOrders.Count(order => order.OrderStatus == "returned");
                int createdCount = filteredOrders.Count(order => order.OrderStatus == "created");
                int paymentAccepted = filteredOrders.Count(order => order.PaymentStatus == "accepted");
                int paymentPending = filteredOrders.Count(order => order.PaymentStatus == "pending");


                var orders = _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Items)
                .OrderByDescending(o => o.Id)
                .ToList();

                DateTime today = DateTime.Now;
                DateTime filterStartDate;

                // Filter based on the selected time period
                switch (filter.ToLower())
                {
                    case "weekly":
                        filterStartDate = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                        break;
                    case "monthly":
                        filterStartDate = new DateTime(today.Year, today.Month, 1);
                        break;
                    case "yearly":
                        filterStartDate = new DateTime(today.Year, 1, 1);
                        break;
                    default:
                        filterStartDate = today.AddDays(-7);
                        break;
                }

                // Filter orders by the selected time period
                var filteredChartOrders = orders
                    .Where(o => o.CreatedAt >= filterStartDate && o.CreatedAt <= today)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();

                var chartLabels = new List<string>();
                var chartData = new List<double>();

                switch (filter.ToLower())
                {
                    case "weekly":
                        chartLabels = Enum.GetNames(typeof(DayOfWeek)).ToList(); // Monday, Tuesday, ...
                        chartData = Enumerable.Range(0, 7)
                            .Select(i => filteredChartOrders
                                .Where(o => o.CreatedAt.Date == filterStartDate.AddDays(i).Date)
                                .Sum(o => (double)o.TotalAmount))
                            .ToList();
                        break;

                    case "monthly":
                        var weeksInMonth = Enumerable.Range(0, 5)
                            .Select(i => filterStartDate.AddDays(i * 7).ToString("MMM dd"))
                            .ToList();
                        chartLabels = weeksInMonth;
                        chartData = weeksInMonth
                            .Select(week => filteredChartOrders
                                .Where(o => o.CreatedAt.Date >= filterStartDate.AddDays(chartLabels.IndexOf(week) * 7)
                                            && o.CreatedAt.Date < filterStartDate.AddDays((chartLabels.IndexOf(week) + 1) * 7))
                                .Sum(o => (double)o.TotalAmount))
                            .ToList();
                        break;

                    case "yearly":
                        chartLabels = Enumerable.Range(1, 12)
                            .Select(month => new DateTime(today.Year, month, 1).ToString("MMM"))
                            .ToList();
                        chartData = chartLabels
                            .Select(month => filteredChartOrders
                                .Where(o => o.CreatedAt.Month == chartLabels.IndexOf(month) + 1)
                                .Sum(o => (double)o.TotalAmount))
                            .ToList();
                        break;
                }

                var maxValue = chartData.Any() ? chartData.Max() : 0; // Max value from chartData
                var yAxisMax = Math.Ceiling(maxValue / 10000) * 10000; // Round up to the nearest 10,000
                var yAxisStep = yAxisMax / 5; // Divide into 5 steps
                var yAxisValues = Enumerable.Range(1, 5).Select(i => (int)(i * yAxisStep)).ToList();

                // Return the updated chart data as JSON
                //return Json(new { ChartData = chartData, ChartLabels = chartLabels, YAxisValues = yAxisValues });


                var viewModel = new DashboardVM
                {
                    Orders = paginatedOrders,
                    TotalSales = (decimal)totalRevenue,
                    CancelledCount = cancelledCount,
                    CreatedCount = createdCount,
                    DeliveredCount = deliveredCount,
                    ReturnedCount = returnedCount,
                    OrderCount = (int)count,
                    PaymentAccepted = paymentAccepted,
                    PaymentPending = paymentPending,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    ChartData = chartData,
                    ChartLabels = chartLabels
                };

                ViewBag.Filter = filter;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }


        public IActionResult ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    startDate = DateTime.Now.AddDays(-7);
                    endDate = DateTime.Now;
                }

                var filteredOrders = _context.Orders
                    .Include(o => o.Client)
                    .Where(order => order.CreatedAt >= startDate && order.CreatedAt <= endDate)
                    .OrderByDescending(order => order.CreatedAt)
                    .ToList();

                // Calculate metrics
                double totalRevenue = (double)filteredOrders.Sum(order => order.TotalAmount);
                int cancelledCount = filteredOrders.Count(order => order.OrderStatus == "canceled");
                int deliveredCount = filteredOrders.Count(order => order.OrderStatus == "delivered");
                int returnedCount = filteredOrders.Count(order => order.OrderStatus == "returned");
                int createdCount = filteredOrders.Count(order => order.OrderStatus == "created");
                int paymentAccepted = filteredOrders.Count(order => order.PaymentStatus == "accepted");
                int paymentPending = filteredOrders.Count(order => order.PaymentStatus == "pending");

                // Create an Excel package
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Orders");

                    // Add headers to the first row
                    worksheet.Cells[1, 1].Value = "Order ID";
                    worksheet.Cells[1, 2].Value = "Client Name";
                    worksheet.Cells[1, 3].Value = "Total Amount";
                    worksheet.Cells[1, 4].Value = "Order Status";
                    worksheet.Cells[1, 5].Value = "Payment Status";
                    worksheet.Cells[1, 6].Value = "Created At";

                    // Add data rows
                    int row = 2;
                    foreach (var order in filteredOrders)
                    {
                        worksheet.Cells[row, 1].Value = order.Id;
                        worksheet.Cells[row, 2].Value = order.Client?.FirstName;
                        worksheet.Cells[row, 3].Value = order.TotalAmount;
                        worksheet.Cells[row, 4].Value = order.OrderStatus;
                        worksheet.Cells[row, 5].Value = order.PaymentStatus;
                        worksheet.Cells[row, 6].Value = order.CreatedAt;
                        row++;
                    }

                    // Add metrics to the Excel sheet below the order data
                    worksheet.Cells[row + 1, 1].Value = "Total Sales";
                    worksheet.Cells[row + 1, 2].Value = totalRevenue;
                    worksheet.Cells[row + 2, 1].Value = "Cancelled Orders";
                    worksheet.Cells[row + 2, 2].Value = cancelledCount;
                    worksheet.Cells[row + 3, 1].Value = "Delivered Orders";
                    worksheet.Cells[row + 3, 2].Value = deliveredCount;
                    worksheet.Cells[row + 4, 1].Value = "Returned Orders";
                    worksheet.Cells[row + 4, 2].Value = returnedCount;
                    worksheet.Cells[row + 5, 1].Value = "Created Orders";
                    worksheet.Cells[row + 5, 2].Value = createdCount;
                    worksheet.Cells[row + 6, 1].Value = "Payment Accepted";
                    worksheet.Cells[row + 6, 2].Value = paymentAccepted;
                    worksheet.Cells[row + 7, 1].Value = "Payment Pending";
                    worksheet.Cells[row + 7, 2].Value = paymentPending;

                    // Set the response headers for the file download
                    var file = package.GetAsByteArray();
                    return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders_Report.xlsx");
                }
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }



        public IActionResult ExportToPdf(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    startDate = DateTime.Now.AddDays(-7);
                    endDate = DateTime.Now;
                }

                var filteredOrders = _context.Orders
                    .Include(o => o.Client)
                    .Where(order => order.CreatedAt >= startDate && order.CreatedAt <= endDate)
                    .OrderByDescending(order => order.CreatedAt)
                    .ToList();

                // Calculate metrics
                double totalRevenue = (double)filteredOrders.Sum(order => order.TotalAmount);
                int cancelledCount = filteredOrders.Count(order => order.OrderStatus == "canceled");
                int deliveredCount = filteredOrders.Count(order => order.OrderStatus == "delivered");
                int returnedCount = filteredOrders.Count(order => order.OrderStatus == "returned");
                int createdCount = filteredOrders.Count(order => order.OrderStatus == "created");
                int paymentAccepted = filteredOrders.Count(order => order.PaymentStatus == "accepted");
                int paymentPending = filteredOrders.Count(order => order.PaymentStatus == "pending");

                // Create a memory stream to store the PDF
                using (var memoryStream = new MemoryStream())
                {
                    // Create a Document object
                    var document = new iTextSharp.text.Document(PageSize.A4);
                    var writer = PdfWriter.GetInstance(document, memoryStream);
                    document.Open();

                    // Title
                    var title = new Paragraph("Orders Report")
                    {
                        Alignment = Element.ALIGN_CENTER,
                        Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18)
                    };
                    document.Add(title);
                    document.Add(new Phrase("\n")); // Add a line break

                    // Table for the orders
                    var orderTable = new PdfPTable(6)
                    {
                        WidthPercentage = 100
                    };

                    // Set column widths
                    orderTable.SetWidths(new float[] { 1f, 2f, 2f, 2f, 2f, 2f });

                    // Add headers
                    orderTable.AddCell("Order ID");
                    orderTable.AddCell("Client Name");
                    orderTable.AddCell("Total Amount");
                    orderTable.AddCell("Order Status");
                    orderTable.AddCell("Payment Status");
                    orderTable.AddCell("Created At");

                    // Add data rows
                    foreach (var order in filteredOrders)
                    {
                        orderTable.AddCell(order.Id.ToString());
                        orderTable.AddCell(order.Client?.FirstName);
                        orderTable.AddCell(order.TotalAmount.ToString());
                        orderTable.AddCell(order.OrderStatus);
                        orderTable.AddCell(order.PaymentStatus);
                        orderTable.AddCell(order.CreatedAt.ToString("yyyy-MM-dd"));
                    }

                    // Add the table to the document
                    document.Add(orderTable);

                    // Add metrics section
                    document.Add(new Phrase("\n")); // Add a line break
                    document.Add(new Phrase("Metrics:"));
                    document.Add(new Phrase($"\nTotal Sales: {totalRevenue}"));
                    document.Add(new Phrase($"\nCancelled Orders: {cancelledCount}"));
                    document.Add(new Phrase($"\nDelivered Orders: {deliveredCount}"));
                    document.Add(new Phrase($"\nReturned Orders: {returnedCount}"));
                    document.Add(new Phrase($"\nCreated Orders: {createdCount}"));
                    document.Add(new Phrase($"\nPayment Accepted: {paymentAccepted}"));
                    document.Add(new Phrase($"\nPayment Pending: {paymentPending}"));

                    // Close the document
                    document.Close();

                    // Return the PDF as a byte array
                    var file = memoryStream.ToArray();
                    return File(file, "application/pdf", "Orders_Report.pdf");
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return View("Error");
            }
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

    }
}
