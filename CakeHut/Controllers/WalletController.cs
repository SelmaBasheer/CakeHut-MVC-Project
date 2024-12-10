using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CakeHut.Controllers
{
    public class WalletController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly WalletService _walletService;
        private readonly int pageSize = 5;

        public WalletController(UserManager<ApplicationUser> userManager, WalletService walletService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _walletService = walletService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? pageIndex)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wallet = await _walletService.GetWalletAsync(currentUser.Id);
            if (wallet == null)
            {
                return View(new Wallet
                {
                    Transactions = new List<WalletTransaction>()
                });
            }

            // pagination functionality
            if (pageIndex == null || pageIndex < 1)
            {
                pageIndex = 1;
            }

            decimal totalTransactions = wallet.Transactions.Count;
            int totalPages = (int)Math.Ceiling(totalTransactions / pageSize);

            // Paginate the transactions
            var paginatedTransactions = wallet.Transactions
                .OrderByDescending(t => t.TransactionDate) 
                .Skip(((int)pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Store pagination data in ViewBag
            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = totalPages;
            ViewBag.PaginatedTransactions = paginatedTransactions;

            return View(wallet);
        }

        // Add a transaction (refund, etc.) to the wallet
        public async Task<IActionResult> AddFunds(decimal amount, string description, string transactionType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            //await _walletService.AddTransactionAsync(currentUser.Id, amount, description, transactionType, cancelledId);
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> WalletPayment(decimal totalAmount)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wallet = await _walletService.GetWalletAsync(currentUser.Id);
            if (wallet == null || wallet.Balance < totalAmount)
            {
                ViewBag.ErrorMessage = "Insufficient wallet balance to complete the payment.";
                return RedirectToAction("Index", "Cart");
            }

            // Deduct the amount from the wallet
            wallet.Balance -= totalAmount;

            var transaction = new WalletTransaction
            {
                UserId = currentUser.Id,
                Amount = -totalAmount, 
                Description = "Payment for order",
                TransactionDate = DateTime.Now,
                TransactionType = "Debit"
            };

            wallet.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            return RedirectToAction("Confirm", "Cart");
        }

    }
}
