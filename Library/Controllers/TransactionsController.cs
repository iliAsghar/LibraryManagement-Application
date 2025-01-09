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
    [Authorize] // Ensure that the user is authenticated to access this controller
    public class Transactions : BaseController
    {
        // Logger for the Transactions controller
        private readonly ILogger<Transactions> _logger;
        // Database context for accessing transaction data
        private readonly MyDBContext _context;

        // Constructor to initialize logger and context
        public Transactions(ILogger<Transactions> logger, MyDBContext context) : base(context)
        {
            _logger = logger;
            _context = context;
        }

        // Displays a list of transactions based on user role
        [Authorize] // Ensure that the user is authenticated to access this method
        public async Task<IActionResult> TransactionList()
        {
            IQueryable<Transaction> transactions;

            // If user is BookKeeper or Admin, get all transactions
            if (GetUserRole() == "BookKeeper" || GetUserRole() == "Admin")
            {
                transactions = _context.Transactions
                    .Include(t => t.User) // Include user details in the transaction
                    .Include(t => t.TransactionItems) // Include transaction items
                        .ThenInclude(ti => ti.Book); // Include related books for each transaction item
            }
            else // If user is a normal user, only get their transactions
            {
                var userId = GetLoggedInUserId(); // Get the logged-in user's ID

                transactions = _context.Transactions
                    .Where(t => t.UserId == userId)
                    .Include(t => t.User)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book);
            }

            var transactionList = await transactions.ToListAsync(); // Fetch the transactions from the database

            // Create a list of transaction view models from the list of transactions
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
                    BookTitle = ti.Book?.Title, // Book title
                    Description = ti.Book?.Description, // Book description
                    Author = ti.Book?.Author, // Author of the book
                    Quantity = ti.Quantity // Quantity of the book in the transaction
                }).ToList()
            }).ToList();

            return View(transactionViewModels); // Return the view with the transaction view models
        }

        // Displays details of a specific transaction by ID
        [Authorize] // Ensure that the user is authenticated to access this method
        public async Task<IActionResult> ShowTransaction(int id)
        {
            var userId = GetLoggedInUserId(); // Get the logged-in user's ID
            Transaction? transaction = null;

            // If user is BookKeeper or Admin, retrieve the transaction by ID
            if (GetUserRole() == "BookKeeper" || GetUserRole() == "Admin")
            {
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                    .FirstOrDefaultAsync(t => t.Id == id); // Find the transaction by ID
            }
            else if (GetUserRole() == "User") // If user is a normal user
            {
                // If ID is -1, get the latest ongoing transaction
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
                else // Otherwise, get the transaction by ID
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

            // If transaction exists, create a view model for the transaction
            if (transaction != null)
            {
                var items = new List<TransactionItemViewModel>();
                foreach (var item in transaction.TransactionItems) // Iterate through transaction items
                {
                    var itemViewModel = new TransactionItemViewModel
                    {
                        Id = item.Id,
                        BookTitle = item.Book.Title,
                        Description = item.Book.Description,
                        Author = item.Book.Author,
                        Quantity = item.Quantity,
                        BookCoverPath = item.Book.CoverPath // Path to the book cover image
                    };

                    items.Add(itemViewModel); // Add the item view model to the list
                }

                // Create the transaction view model
                model = new TransactionViewModel(transaction.User.NationalId, transaction.Status)
                {
                    Id = transaction.Id,
                    RequestDate = transaction.RequestDate,
                    DeliverDate = transaction.DeliverDate,
                    ReturnDate = transaction.ReturnDate,
                    Status = transaction.Status,
                    Items = items // Set the list of item view models
                };
            }

            return View(model); // Return the view with the transaction details
        }

        // Adds a book to the user's current transaction
        [Authorize(policy: "NormalUser")] // Ensure that only normal users can access this method
        public async Task<IActionResult> AddBookToTransaction(int bookId, int quantity)
        {
            var userId = GetLoggedInUserId(); // Get the logged-in user's ID

            // Retrieve the user's ongoing transaction
            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                .Where(t => t.Status != TransactionStatus.Returned &&
                            t.Status != TransactionStatus.Rejected)
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId);

            // If the user doesn't have an existing transaction, create a new one
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
            else // If there is an existing transaction
            {
                // Check if the transaction is in a status that allows adding more items
                if (transaction.Status == TransactionStatus.PendingApproval ||
                    transaction.Status == TransactionStatus.PendingDelivery ||
                    transaction.Status == TransactionStatus.Delivered)
                {
                    return View("ShowTransaction", -1); // Show the transaction if it's in a restricted status
                }
            }

            // Check the current number of items in the transaction
            var currentItemCount = transaction.TransactionItems.Sum(i => i.Quantity);

            // Check if adding this quantity exceeds limits (assumed max is 5)
            if (currentItemCount + quantity > 5)
            {
                return RedirectToAction("BookList", "Books");
            }

            var transactionItem = transaction.TransactionItems
                .FirstOrDefault(t => t.BookId == bookId); // Check if the book is already in the transaction

            // Check if the quantity for this book exceeds limits (assumed max is 2)
            if (transactionItem != null && transactionItem.Quantity + quantity > 2)
            {
                return RedirectToAction("BookList", "Books");
            }

            // Retrieve the book from the database
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null || book.TotalQuantity < quantity) // Check if the book exists and if there is enough quantity
            {
                return RedirectToAction("BookList", "Books");
            }

            // If the book is not already in the transaction, add it
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
            else // If it's already in the transaction, update the quantity
            {
                transactionItem.Quantity += quantity;
                _context.TransactionItems.Update(transactionItem);
            }

            // Update the book and transaction in the database
            _context.Books.Update(book);
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync(); // Save changes asynchronously

            return RedirectToAction("BookList", "Books"); // Redirect to book list
        }

        // Finalizes the user's transaction
        [Authorize(policy: "NormalUser")] // Ensure that only normal users can access this method
        public async Task<IActionResult> FinalizeTransaction()
        {
            var userId = GetLoggedInUserId(); // Get the logged-in user's ID

            // Retrieve the user's current unfinalized transaction
            var transaction = await _context.Transactions
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Book)
                .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Status == TransactionStatus.UnFinalized);

            if (transaction == null) // If no such transaction exists
            {
                return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
            }

            // Update the quantity of each book in the transaction
            foreach (var item in transaction.TransactionItems)
            {
                var book = item.Book;
                book.TotalQuantity -= item.Quantity; // Decrease total quantity of the book
                _context.Books.Update(book); // Update book in the database
            }

            // Set request date and change status of the transaction
            transaction.RequestDate = DateTime.Now;
            transaction.Status = TransactionStatus.PendingApproval;

            _context.Transactions.Update(transaction); // Update transaction in the database
            await _context.SaveChangesAsync(); // Save changes asynchronously

            return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
        }

        // Approves a transaction by ID (for BookKeeper only)
        [Authorize(policy: "BookKeeper")] // Ensure that only BookKeepers can access this method
        public async Task<IActionResult> ApproveTransaction(int id)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t =>
                t.Id == id); // Retrieve the transaction by ID

            if (transaction == null) // If the transaction doesn't exist
            {
                return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
            }

            // Set approval date and change status of the transaction
            transaction.ApproveDate = DateTime.Now;
            transaction.DeliverDate = DateTime.Now;
            transaction.Status = TransactionStatus.Delivered;
            _context.Transactions.Update(transaction); // Update transaction in the database
            await _context.SaveChangesAsync(); // Save changes asynchronously

            return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
        }

        // Rejects a transaction by ID (for BookKeeper only)
        [Authorize(policy: "BookKeeper")] // Ensure that only BookKeepers can access this method
        public async Task<IActionResult> RejectTransaction(int id)
        {
            var transaction = await _context.Transactions
                            .FirstOrDefaultAsync(t =>
                            t.Id == id); // Retrieve the transaction by ID

            if (transaction == null) // If the transaction doesn't exist
            {
                return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
            }

            // Set reject date and change status of the transaction
            transaction.RejectDate = DateTime.Now;
            transaction.Status = TransactionStatus.Rejected;
            _context.Transactions.Update(transaction); // Update transaction in the database
            await _context.SaveChangesAsync(); // Save changes asynchronously

            return RedirectToAction("TransactionList", "Transactions"); // Redirect to transaction list
        }

        // Confirms the return of a transaction (for BookKeeper only)
        [HttpPost]
        [Authorize(policy: "BookKeeper")] // Ensure that only BookKeepers can access this method
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

        // Returns a transaction for a specific user identified by National ID (for BookKeeper only)
        [HttpPost]
        [Authorize(policy: "BookKeeper")] // Ensure that only BookKeepers can access this method
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

        // Displays the return transaction view (for BookKeeper only)
        [HttpGet]
        [Authorize(policy: "BookKeeper")] // Ensure that only BookKeepers can access this method
        public IActionResult ReturnTransaction()
        {
            return View(); // Return the view for returning a transaction
        }

        // Gets the currently logged-in user's ID
        private int GetLoggedInUserId()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString()); // Get the user's ID from claims
            return userId; // Return the user ID
        }

        // Returns an error view with a request identifier
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // Show error view with request ID
        }
    }
}