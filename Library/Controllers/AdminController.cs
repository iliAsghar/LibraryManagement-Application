using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    public class AdminsController : BaseController
    {
        private readonly MyDBContext _context;

        public AdminsController(MyDBContext context) : base(context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var userCount = _context.Users.Count();

            var bookCount = _context.Books.Count();

            var transactionCount = _context.Transactions
                                           .Count(t => t.Status != TransactionStatus.UnFinalized);

            ViewBag.UserCount = userCount;
            ViewBag.BookCount = bookCount;
            ViewBag.TransactionCount = transactionCount;

            return View();
        }
    }
}
