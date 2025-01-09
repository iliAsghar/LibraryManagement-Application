using Library.Data; // Import database context
using Library.Models; // Import models
using Microsoft.AspNetCore.Mvc; // For controller actions

namespace Library.Controllers
{
    public class AdminsController : BaseController
    {
        private readonly MyDBContext _context;
        // Constructor to initialize database context
        public AdminsController(MyDBContext context) : base(context)
        {
            _context = context;
        }
        // Display admin dashboard with counts of users, books, and finalized transactions
        public IActionResult Dashboard()
        {
            var userCount = _context.Users.Count();// Total number of users

            var bookCount = _context.Books.Count(); // Total number of books

            var transactionCount = _context.Transactions
                                           .Count(t => t.Status != TransactionStatus.UnFinalized);
            // Pass data to the view using ViewBag
            ViewBag.UserCount = userCount;
            ViewBag.BookCount = bookCount;
            ViewBag.TransactionCount = transactionCount;

            return View();
        }
    }
}
