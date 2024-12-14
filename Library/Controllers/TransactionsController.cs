using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Library.Controllers
{
    [Authorize]
    public class Transactions : Controller
    {
        private readonly ILogger<Transactions> _logger;
        private readonly MyDBContext _context;

        public Transactions(ILogger<Transactions> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize(policy: "NormalUser")]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> TransactionList()
        {
            var userId = GetLoggedInUserId();

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Book)
                .ToListAsync();

            return View(transactions);
        }
        
        [Authorize(policy: "NormalUser")]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ShowTransaction(int id)
        {
            var userId = GetLoggedInUserId();

            TransactionViewModel model = new TransactionViewModel();

            if (id == -1)
            {
                var transaction = await _context.Transactions
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                    .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Status == "Unfinalized");

                if ( transaction != null )
                {
                    model.Transaction = transaction;
                    model.TransactionItems = transaction?.TransactionItems;
                    model.Books = transaction?.TransactionItems?.Select(ti => ti.Book).ToList();
                } else
                {
                    TempData["NoActiveTransaction"] = "شما در حال حاضر هیچ امانت فعالی ندارید!";
                }
            } else
            {
                var transaction = await _context.Transactions
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                    .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Id == id);

                if ( transaction != null )
                {
                    model.Transaction = transaction;
                    model.TransactionItems = transaction?.TransactionItems;
                    model.Books = transaction?.TransactionItems?.Select(ti => ti.Book).ToList();
                }
            }

            return View(model);
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> AddBookToTransaction(int bookId, int quantity)
        {
            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                !t.IsFinalized);

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId,
                    TransactionDate = DateTime.Now,
                    IsFinalized = false
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            }

            var transactionItem = await _context.TransactionItems
                .FirstOrDefaultAsync(t =>
                t.TransactionId == transaction.Id &&
                t.BookId == bookId);

            if (transactionItem == null)
            {
                transactionItem = new TransactionItem
                {
                    BookId = bookId,
                    Quantity = quantity,
                    TransactionId = transaction.Id
                };
                _context.TransactionItems.Add(transactionItem);
                await _context.SaveChangesAsync();
            } else
            {
                transactionItem.Quantity += quantity;
                _context.TransactionItems.Update(transactionItem);
                await _context.SaveChangesAsync();
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کتاب به سبد امانت اضافه شد!";
            return RedirectToAction("BookList", "Books");
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> FinalizeTransaction()
        {
            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == "Unfinalized");

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction to finalize.";
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = "PendingApproval";
            //transaction.TransactionDate = DateTime.Now;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Finalized!";
            return RedirectToAction("TransactionList", "Transactions");
        }

        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ApproveTransaction(int id)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.Id == id);

            if (transaction != null)
            {
                transaction.Status = "Approved"; 
            }

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id;
                return RedirectToAction("TransactionList", "Transactions");
            }

            //transaction.TransactionDate = DateTime.Now;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Approved!";
            return RedirectToAction("TransactionList", "Transactions");
        }

        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> RejectTransaction(int id)
        {
            var transaction = await _context.Transactions
                            .FirstOrDefaultAsync(t =>
                            t.Id == id);

            if (transaction != null)
            {
                transaction.Status = "Rejected";
            }

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id;
                return RedirectToAction("TransactionList", "Transactions");
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Rejected!";
            return RedirectToAction("TransactionList", "Transactions");
        }
        
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ReturnTransaction(int id)
        {
            var transaction = await _context.Transactions
                            .FirstOrDefaultAsync(t =>
                            t.Id == id);

            if (transaction != null)
            {
                transaction.Status = "Returned";
            }

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id;
                return RedirectToAction("TransactionList", "Transactions");
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Rejected!";
            return RedirectToAction("TransactionList", "Transactions");
        }
        private int GetLoggedInUserId()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            return userId;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
