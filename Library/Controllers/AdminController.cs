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

        public IActionResult Index()
        {
            // تعداد کاربران
            var userCount = _context.Users.Count();

            // تعداد کتاب‌ها
            var bookCount = _context.Books.Count();

            // تعداد تراکنش‌ها (بدون تراکنش‌های UnFinalized)
            var transactionCount = _context.Transactions
                                           .Count(t => t.Status != TransactionStatus.UnFinalized);

            // ارسال اطلاعات به View
            ViewBag.UserCount = userCount;
            ViewBag.BookCount = bookCount;
            ViewBag.TransactionCount = transactionCount;

            return View();
        }
    }
}
