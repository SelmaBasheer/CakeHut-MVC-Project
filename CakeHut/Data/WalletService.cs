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
        public async Task AddTransactionAsync(string userId, decimal refundAmount, string description, string transactionType, int cancelledId)
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
                TransactionType = transactionType,
                CancelledId = cancelledId
            };

            wallet.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task AddRefundToWalletAsync(string userId, decimal refundAmount, string transactionType, 
            string cancellationType, int cancelledId)
        {
            string description = "";
            if (cancellationType == "order")
            {
                description = $"Refund for canceled order";
            }else if(cancellationType == "Return")
            {
                description = $"Refund for returned item";
            }
                description = $"Refund for canceled item";

            await AddTransactionAsync(userId, refundAmount, description, transactionType, cancelledId);
        }

        public async Task<bool> DeductFromWalletAsync(string userId, decimal amount)
        {
            var wallet = await GetWalletAsync(userId);
            if (wallet == null || wallet.Balance < amount)
            {
                return false; // Insufficient balance
            }

            wallet.Balance -= amount;

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = -amount,
                Description = "Order Payment",
                TransactionDate = DateTime.Now,
                TransactionType = "Debit"
            };

            wallet.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

    }


}
