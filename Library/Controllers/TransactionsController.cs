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

        [Authorize(Roles = "User, BookKeeper")]
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

            var transactionViewModels = transactionList.Select(t => new TransactionViewModel(
                t.User.NationalId,
                t.Status)
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Items = t.TransactionItems.Select(ti => new TransactionItemViewModel
                {
                    Description = ti.Book?.Description,
                    BookTitle = ti.Book?.Title,
                    Quantity = ti.Quantity
                }).ToList()
            }).ToList();

            return View(transactionViewModels);
        }

        [Authorize(policy: "NormalUser")]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ShowTransaction(int id)
        {
            var userId = GetLoggedInUserId();
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
                if (id == -1)
                {
                    transaction = await _context.Transactions
                        .Include(t => t.User)
                        .Include(t => t.TransactionItems)
                            .ThenInclude(ti => ti.Book)
                        .FirstOrDefaultAsync(t =>
                            t.UserId == userId &&
                            t.Status == TransactionStatus.UnFinalized);
                }
                else
                {
                    transaction = await _context.Transactions
                        .Include(t => t.User)
                        .Include(t => t.TransactionItems)
                            .ThenInclude(ti => ti.Book)
                        .FirstOrDefaultAsync(t =>
                            t.UserId == userId &&
                            t.Id == id);
                }
            }

            TransactionViewModel? model = null;

            if (transaction != null)
            {
                var items = new List<TransactionItemViewModel>();
                foreach (var item in transaction.TransactionItems)
                {
                    var itemViewModel = new TransactionItemViewModel
                    {
                        Id = item.Id,
                        BookTitle = item.Book.Title,
                        Quantity = item.Quantity,
                        Description = item.Book.Description,
                        BookCoverPath = item.Book.CoverPath
                    };

                    items.Add(itemViewModel);
                }

                model = new TransactionViewModel(transaction.User.NationalId, transaction.Status)
                {
                    Id = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    Status = transaction.Status,
                    Items = items
                };
            }

            return View(model);
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> AddBookToTransaction(int bookId, int quantity)
        {
            var maxQuantity = 2;
            if (quantity > maxQuantity)
            {
                return RedirectToAction("BookList", "Books");
            }

            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == TransactionStatus.UnFinalized);

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId,
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
            }
            else
            {
                transactionItem.Quantity += quantity;
                _context.TransactionItems.Update(transactionItem);
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("BookList", "Books");
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> FinalizeTransaction()
        {
            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == TransactionStatus.UnFinalized);

            if (transaction == null)
            {
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = TransactionStatus.PendingApproval;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

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
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = TransactionStatus.Approved;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

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
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = TransactionStatus.Rejected;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

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
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.Status = TransactionStatus.Returned;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

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
