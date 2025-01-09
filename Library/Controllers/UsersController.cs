using Library.Data;
using Library.Models;
using Library.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library.Controllers
{
    [Authorize] // اطمینان از احراز هویت کاربر برای دسترسی به این کنترلر
    public class UsersController : BaseController
    {
        private readonly ILogger<UsersController> _logger; // Logger برای ثبت لاگ‌ها
        private readonly MyDBContext _context; // زمینه پایگاه داده برای دسترسی به داده‌های کاربری

        // سازنده برای مقداردهی logger و context
        public UsersController(ILogger<UsersController> logger, MyDBContext context) : base(context)
        {
            _logger = logger;
            _context = context;
        }

        // نمایش جزئیات کاربر بر اساس ID (دسترسی فقط برای BookKeeper)
        [Authorize(Policy = "BookKeeper")]
        public async Task<ActionResult> ShowUser(int id)
        {
            // جستجوی کاربر با ID مشخص و شامل تراکنش‌ها و اقلام آنها
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Include(u => u.Transactions) // شامل تراکنش‌های کاربر
                    .ThenInclude(t => t.TransactionItems) // شامل اقلام تراکنش‌ها
                        .ThenInclude(ti => ti.Book) // شامل کتاب‌های اقلام تراکنش
                .FirstOrDefaultAsync();

            if (user == null) // اگر کاربر پیدا نشد
            {
                return NotFound(); // بازگشت پیغام پیدا نشدن
            }

            // ساخت مدل نمایشی کاربر
            UserViewModel model = new UserViewModel()
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                LastName = user.Lastname,
                NationalId = user.NationalId,
                Role = user.Role,
                PfpPath = user.PfpPath,
                Transactions = user.Transactions // قرار دادن تراکنش‌ها در مدل
            };

            return View(model); // بازگشت به نما برای نمایش جزئیات کاربر
        }

        // ارتقاء یک کاربر به وضعیت BookKeeper (دسترسی فقط برای Admin)
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> PromoteToBookKeeper(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id); // جستجوی کاربر با ID مشخص

            if (user == null || user.Role == "BookKeeper") // اگر کاربر وجود نداشت یا قبلاً BookKeeper بود
            {
                return RedirectToAction("ShowUser", "Users", new { id }); // بازگشت به نشان دادن کاربر
            }

            // بررسی وجود تراکنش‌های کاربر
            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.UserId == user.Id);

            if (hasTransactions) // اگر کاربر دارای تراکنش باشد
            {
                TempData["ErrorMessage"] = "کاربر نمیتواند ارتقا یابد زیرا که تراکنش دارد."; // پیام خطا
                return RedirectToAction("ShowUser", "Users", new { id }); // بازگشت به نشان دادن کاربر
            }

            user.Role = "BookKeeper"; // تغییر نقش کاربر
            _context.Users.Update(user); // بروزرسانی کاربر در پایگاه داده
            await _context.SaveChangesAsync(); // ذخیره تغییرات به صورت غیرهمزمان

            return RedirectToAction("ShowUser", "Users", new { id }); // بازگشت به نشان دادن کاربر
        }

        // تنزل یک کاربر به کاربر عادی (دسترسی فقط برای Admin)
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> DemoteToMember(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id); // جستجوی کاربر با ID مشخص

            if (user == null || user.Role == "User") // اگر کاربر وجود نداشت یا قبلاً کاربر عادی بود
            {
                return RedirectToAction("ShowUser", "Users", new { id }); // بازگشت به نشان دادن کاربر
            }

            user.Role = "User"; // تغییر نقش کاربر به User
            _context.Users.Update(user); // بروزرسانی کاربر در پایگاه داده
            await _context.SaveChangesAsync(); // ذخیره تغییرات به صورت غیرهمزمان

            return RedirectToAction("ShowUser", "Users", new { id }); // بازگشت به نشان دادن کاربر
        }

        // نمایش لیست اعضای سایت (دسترسی فقط برای BookKeeper)
        [Authorize(Policy = "BookKeeper")]
        public IActionResult MemberList()
        {
            var members = _context.Users
                .Where(u => u.Role == "User") // جستجوی کاربران عادی
                .ToList();

            ViewData["Title"] = "اعضا"; // عنوان نمای
            return View("UserList", members); // بازگشت به نمای کاربرها
        }

        // نمایش لیست کتابداران (دسترسی فقط برای Admin)
        [Authorize(Policy = "Admin")]
        public IActionResult BookKeeperList()
        {
            var bookkeepers = _context.Users
                .Where(u => u.Role == "BookKeeper") // جستجوی کاربران با نقش BookKeeper
                .ToList();

            ViewData["Title"] = "کتابدار ها"; // عنوان نمای
            return View("UserList", bookkeepers); // بازگشت به نمای کاربرها
        }

        // نمایش لیست تمامی کاربران (دسترسی فقط برای Admin)
        [Authorize(Policy = "Admin")]
        public IActionResult UserList()
        {
            var users = _context.Users
                .Where(u => u.Role == "User" || u.Role == "BookKeeper") // جستجوی کاربرانی با نقش User یا BookKeeper
                .ToList();

            ViewData["Title"] = "کاربران"; // عنوان نمای
            return View("UserList", users); // بازگشت به نمای کاربرها
        }

        // بازگشت یک صفحه خطا
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ViewModels.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // نمایش نمای خطا با شناسه درخواست
        }
    }
}