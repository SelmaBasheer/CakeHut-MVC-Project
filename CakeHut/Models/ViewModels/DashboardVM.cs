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
        public int ShippedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int CancelledCount { get; set; }

        // Sales and revenue metrics
        public decimal TotalSales { get; set; }
        public double TotalRevenueToday { get; set; }
        public double TotalRevenueThisWeek { get; set; }
        public double TotalRevenueThisMonth { get; set; }
        public double TotalRevenueThisYear { get; set; }

        // Paginated data
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }

        // Charts
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<double> ChartData { get; set; } = new List<double>();

        public List<string> FilterChartLabels { get; set; } // Labels for the new chart
        public List<double> FilterChartData { get; set; }   // Data for the new chart
        public string SelectedFilter { get; set; }          // Currently selected filter (e.g., "weekly", "monthly", "yearly")


        // Sales data by product and category
        public Dictionary<int, int> ProductQuantitiesSold { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> CategorySales { get; set; } = new Dictionary<int, int>();


        /*
        public IEnumerable<Order> Orders { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int ShippedCount { get; set; }
        public int CancelledCount { get; set; }
        public List<Product> TopSellingProducts { get; set; }
        public Dictionary<int, int> ProductQuantitiesSold { get; set; }
        public List<Category> TopSellingCategories { get; set; }
        public Dictionary<int, int> CategorySales { get; set; }
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }

        public double WeeklyRevenue { get; set; }
        public double MonthlyRevenue { get; set; }
        public double YearlyRevenue { get; set; }
        public int WeeklyOrders { get; set; }
        public int MonthlyOrders { get; set; }
        public int YearlyOrders { get; set; }

        public string SelectedFilter { get; set; } // To store the selected filter (e.g., Daily, Weekly, etc.)
        public DateTime? StartDate { get; set; } // For custom date range filtering
        public DateTime? EndDate { get; set; }   // For custom date range filtering

        public List<string> SalesChartLabels { get; set; } // Chart labels for the filtered sales
        public List<double> SalesChartData { get; set; }   // Chart data for the filtered sales

        // Constructor to initialize default values
        public DashboardVM()
        {
            Orders = new List<Order>();
            Products = new List<Product>();
            TopSellingProducts = new List<Product>();
            TopSellingCategories = new List<Category>();
            ProductQuantitiesSold = new Dictionary<int, int>();
            CategorySales = new Dictionary<int, int>();
        }
        */
    }
}
