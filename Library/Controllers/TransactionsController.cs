using Library.Data;
using Library.Models;
using Library.ViewModels;
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
            IQueryable<Transaction> transactions;

            if (User.IsInRole("BookKeeper"))
            {
                transactions = _context.Transactions
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book);
            }
            else
            {
                var userId = GetLoggedInUserId();

                transactions = _context.Transactions
                    .Where(t => t.UserId == userId)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book);
            }

            var transactionList = await transactions.ToListAsync();

            return View(transactionList);
        }
        
        [Authorize(policy: "NormalUser")]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ShowTransaction(int id)
        {
            Transaction? transaction = null;

            if (User.IsInRole("BookKeeper"))
            {
                transaction = await _context.Transactions
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                    .FirstOrDefaultAsync(t => t.Id == id);
            } 
            else if (User.IsInRole("NormalUser"))
            {
                var userId = GetLoggedInUserId();

                if (id == -1)
                {
                    transaction = await _context.Transactions
                        .Include(t => t.TransactionItems)
                            .ThenInclude(ti => ti.Book)
                        .FirstOrDefaultAsync(t =>
                        t.UserId == userId &&
                        t.Status == "Unfinalized"); // todo change this
                } else
                {
                    transaction = await _context.Transactions
                        .Include(t => t.TransactionItems)
                            .ThenInclude(ti => ti.Book)
                        .FirstOrDefaultAsync(t =>
                        t.UserId == userId &&
                        t.Id == id);
                }
            }

            TransactionViewModel model = new TransactionViewModel();

            if (transaction != null)
            {
                model.Transaction = transaction;
                model.TransactionItems = transaction?.TransactionItems;
                model.Books = transaction?.TransactionItems?.Select(ti => ti.Book).ToList();
            } else
            {
                TempData["NoActiveTransaction"] = "شما در حال حاضر هیچ امانت فعالی ندارید!";
                // todo make this better
            }

            return View(model);
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> AddBookToTransaction(int bookId, int quantity)
        {
            // todo make this check for max quantity


            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == "Unfinalized"); // todo change this

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId,
                    TransactionDate = DateTime.Now, // todo do something with this
                    Status = "Unfinalized"
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
            } else
            {
                transactionItem.Quantity += quantity;
                _context.TransactionItems.Update(transactionItem);
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کتاب به سبد امانت اضافه شد!"; // todo change this
            return RedirectToAction("BookList", "Books"); // todo maybe change this to go back where we were?
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
                TempData["ErrorMessage"] = "No transaction to finalize."; //todo change this
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = "PendingApproval";

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Finalized!"; // todo change this
            return RedirectToAction("TransactionList", "Transactions");
        }

        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ApproveTransaction(int id)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.Id == id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id; // todo change this?
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = "Approved"; 
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Approved!"; //todo change this?
            return RedirectToAction("TransactionList", "Transactions");
        }

        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> RejectTransaction(int id)
        {
            var transaction = await _context.Transactions
                            .FirstOrDefaultAsync(t =>
                            t.Id == id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id; // todo change this?
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = "Rejected";
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Rejected!"; // todo change this?
            return RedirectToAction("TransactionList", "Transactions");
        }
        
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ReturnTransaction(int id)
        {
            var transaction = await _context.Transactions
                            .FirstOrDefaultAsync(t =>
                            t.Id == id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "No transaction with id: " + id; // todo change this?
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = "Returned";
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Transaction Rejected!"; // todo change this?
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
