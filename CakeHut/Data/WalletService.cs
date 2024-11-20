using CakeHut.Models;
using Microsoft.EntityFrameworkCore;

namespace CakeHut.Data
{
    public class WalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Method to get user's wallet balance
        public async Task<Wallet> GetWalletAsync(string userId)
        {
            return await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        // Method to add a transaction (e.g., refund for canceled order)
        public async Task AddTransactionAsync(string userId, decimal refundAmount, string description, string transactionType)
        {
            var wallet = await GetWalletAsync(userId);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    Transactions = new List<WalletTransaction>()
                };
                _context.Wallets.Add(wallet);
            }

            wallet.Balance += refundAmount;

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = refundAmount,
                Description = description,
                TransactionDate = DateTime.Now,
                TransactionType = transactionType
            };

            wallet.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task AddRefundToWalletAsync(string userId, decimal refundAmount, string transactionType)
        {
            string description = $"Refund for canceled order";
            await AddTransactionAsync(userId, refundAmount, description, transactionType);
        }
    }


}
