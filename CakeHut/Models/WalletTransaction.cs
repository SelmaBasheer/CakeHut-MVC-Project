namespace CakeHut.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } 
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
    }
}
