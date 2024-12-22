﻿using Library.Data;
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

            if (GetUserRole() == "BookKeeper")
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

            if (GetUserRole() == "BookKeeper")
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
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Status == TransactionStatus.UnFinalized);

            if (transaction == null)
            {
                transaction = new Transaction
                {
                    UserId = userId,
                    TransactionItems = new List<TransactionItem>()
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
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

            book.TotalQuantity -= quantity;

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
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == TransactionStatus.UnFinalized);

            if (transaction == null)
            {
                return RedirectToAction("TransactionList", "Transactions");
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

            transaction.DeliverDate = DateTime.Now;
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

            transaction.ReturnDate = DateTime.Now;
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
