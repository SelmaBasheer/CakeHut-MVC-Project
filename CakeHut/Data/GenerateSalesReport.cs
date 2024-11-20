//using CakeHut.Models;
//using CakeHut.Models.ViewModels;

//namespace CakeHut.Data
//{
//    public class GenerateSalesReport
//    {
//        private SalesReportViewModel GenerateSalesReports(List<Order> orders)
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
