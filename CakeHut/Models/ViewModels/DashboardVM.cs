namespace CakeHut.Models.ViewModels
{
    public class DashboardVM
    {
        // Lists to display in the dashboard
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        public List<Product> TopSellingProducts { get; set; } = new List<Product>();
        public List<Category> TopSellingCategories { get; set; } = new List<Category>();

        // Aggregated counts and totals
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int CategoryCount { get; set; }
        public double NumberOfOrdersLastWeek { get; set; }

        // Order status counts
        public int DeliveredCount { get; set; }
        public int CreatedCount { get; set; }
        public int CancelledCount { get; set; }
        public int ReturnedCount { get; set; }
        public int PaymentAccepted { get; set; }
        public int PaymentPending { get; set; }

        // Sales and revenue metrics
        public decimal TotalSales { get; set; }
        public double TotalRevenueToday { get; set; }
        public double TotalRevenueThisWeek { get; set; }
        public double TotalRevenueThisMonth { get; set; }
        public double TotalRevenueThisYear { get; set; }

        // Paginated data
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Charts
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<double> ChartData { get; set; } = new List<double>();
        public List<double> YAxisValues { get; set; } = new List<double>();
        

        public List<string> FilterChartLabels { get; set; } 
        public List<double> FilterChartData { get; set; }   
        public string SelectedFilter { get; set; }          


        // Sales data by product and category
        public Dictionary<int, int> ProductQuantitiesSold { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> CategorySales { get; set; } = new Dictionary<int, int>();

        
    }

}
