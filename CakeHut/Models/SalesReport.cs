namespace CakeHut.Models
{
    public class SalesReport
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalCouponsUsed { get; set; }
    }
}
