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
    public class Transactions : BaseController
    {
        private readonly ILogger<Transactions> _logger;
        private readonly MyDBContext _context;

        public Transactions(ILogger<Transactions> logger, MyDBContext context) : base(context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> TransactionList()
        {
            IQueryable<Transaction> transactions;

            if (GetUserRole() == "BookKeeper" || GetUserRole() == "Admin")
            {
                transactions = _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book);
            }
            else
            {
                var userId = GetLoggedInUserId();

                transactions = _context.Transactions
                    .Where(t => t.UserId == userId)
                    .Include(t => t.User)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book);
            }

            var transactionList = await transactions.ToListAsync();

            var transactionViewModels = transactionList.Select(t => new TransactionViewModel(
                t.User.NationalId,
                t.Status)
            {
                Id = t.Id,
                RequestDate = t.RequestDate,
                DeliverDate = t.DeliverDate,
                ReturnDate = t.ReturnDate,
                Items = t.TransactionItems.Select(ti => new TransactionItemViewModel
                {
                    BookTitle = ti.Book?.Title,
                    Description = ti.Book?.Description,
                    Author = ti.Book?.Author,
                    Quantity = ti.Quantity
                }).ToList()
            }).ToList();

            return View(transactionViewModels);
        }

        [Authorize]
        public async Task<IActionResult> ShowTransaction(int id)
        {
            var userId = GetLoggedInUserId();
            Transaction? transaction = null;

            if (GetUserRole() == "BookKeeper" || GetUserRole() == "Admin")
            {
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            else if (GetUserRole() == "User")
            {
                if (id == -1)
                {
                    transaction = await _context.Transactions
                        .Include(t => t.User)
                        .Include(t => t.TransactionItems)
                            .ThenInclude(ti => ti.Book)
                        .FirstOrDefaultAsync(t =>
                            t.UserId == userId &&
                            t.Status != TransactionStatus.Delivered &&
                            t.Status != TransactionStatus.Returned);
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
                        Description = item.Book.Description,
                        Author = item.Book.Author,
                        Quantity = item.Quantity,
                        BookCoverPath = item.Book.CoverPath
                    };

                    items.Add(itemViewModel);
                }

                model = new TransactionViewModel(transaction.User.NationalId, transaction.Status)
                {
                    Id = transaction.Id,
                    RequestDate = transaction.RequestDate,
                    DeliverDate = transaction.DeliverDate,
                    ReturnDate = transaction.ReturnDate,
                    Status = transaction.Status,
                    Items = items
                };
            }

            return View(model);
        }

        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> AddBookToTransaction(int bookId, int quantity)
        {
            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                .Where(t => t.Status != TransactionStatus.Returned &&
                            t.Status != TransactionStatus.Rejected)
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId);

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId,
                    TransactionItems = new List<TransactionItem>()
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            } else
            {
                if (transaction.Status == TransactionStatus.PendingApproval ||
                    transaction.Status == TransactionStatus.PendingDelivery ||
                    transaction.Status == TransactionStatus.Delivered)
                {
                    return View("ShowTransaction", -1);
                }
            }

            var currentItemCount = transaction.TransactionItems.Sum(i => i.Quantity);

            if (currentItemCount + quantity > 5)
            {
                return RedirectToAction("BookList", "Books");
            }

            var transactionItem = transaction.TransactionItems
                .FirstOrDefault(t => t.BookId == bookId);

            if (transactionItem != null && transactionItem.Quantity + quantity > 2)
            {
                return RedirectToAction("BookList", "Books");
            }

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null || book.TotalQuantity < quantity)
            {
                return RedirectToAction("BookList", "Books");
            }

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

            _context.Books.Update(book);
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("BookList", "Books");
        }


        [Authorize(policy: "NormalUser")]
        public async Task<IActionResult> FinalizeTransaction()
        {
            var userId = GetLoggedInUserId();

            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Book)
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == TransactionStatus.UnFinalized);

            if (transaction == null)
            {
                return RedirectToAction("TransactionList", "Transactions");
            }

            foreach (var item in transaction.TransactionItems)
            {
                var book = item.Book;
                book.TotalQuantity -= item.Quantity;
                _context.Books.Update(book);
            }

            transaction.RequestDate = DateTime.Now;
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

            transaction.ApproveDate = DateTime.Now;
            transaction.DeliverDate = DateTime.Now;
            transaction.Status = TransactionStatus.Delivered;
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

            transaction.RejectDate = DateTime.Now;
            transaction.Status = TransactionStatus.Rejected;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("TransactionList", "Transactions");
        }

        [HttpPost]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ConfirmReturnTransaction(int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Book)
                .FirstOrDefaultAsync(t =>
                t.Id == transactionId);

            if (transaction == null)
            {
                return RedirectToAction("TransactionList", "Transactions");
            }

            transaction.ReturnDate = DateTime.Now;
            transaction.Status = TransactionStatus.Returned;
            
            foreach (var item in transaction.TransactionItems)
            {
                var book = item.Book;
                book.TotalQuantity = book.TotalQuantity + item.Quantity;
                _context.Books.Update(book);
            }

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("ShowTransaction", new { id = transaction.Id });
        }

        [HttpPost]
        [Authorize(policy: "BookKeeper")]
        public async Task<IActionResult> ReturnTransaction(string? userNId)
        {
            if (userNId != "")
            {
                var user = await _context.Users
                    .Where(u => u.NationalId == userNId)
                    .FirstOrDefaultAsync();

                var currentTransaction = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Status == TransactionStatus.Delivered)
                    .FirstOrDefaultAsync();

                if (currentTransaction == null)
                {
                    ViewBag.Message = "No transactions found for the given user.";
                    return View();
                }

                ViewData["UserNId"] = user.NationalId;
                return RedirectToAction("ShowTransaction", new { id = currentTransaction.Id });
            }

            return View();
        }

        [HttpGet]
        [Authorize(policy: "BookKeeper")]
        public IActionResult ReturnTransaction()
        {
            return View();
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
