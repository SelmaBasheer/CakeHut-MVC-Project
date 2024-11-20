namespace CakeHut.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public decimal Balance { get; set; }
        public List<WalletTransaction> Transactions { get; set; }
    }
}
