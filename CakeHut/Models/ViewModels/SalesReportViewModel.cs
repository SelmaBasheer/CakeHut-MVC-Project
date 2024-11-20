namespace CakeHut.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public int TotalSalesCount { get; set; }
        public decimal TotalOrderAmount { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalCouponDeductions { get; set; }
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public IEnumerable<Order> Orders { get; set; }
    }
}
