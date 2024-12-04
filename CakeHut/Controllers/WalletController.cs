using CakeHut.Data;
using CakeHut.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CakeHut.Controllers
{
    public class WalletController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WalletService _walletService;

        public WalletController(UserManager<ApplicationUser> userManager, WalletService walletService)
        {
            _userManager = userManager;
            _walletService = walletService;
        }

        public async Task<IActionResult> Index()
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
    }
}
