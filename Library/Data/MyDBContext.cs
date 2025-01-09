using Library.Models; // وارد کردن مدل‌های دامنه
using Microsoft.EntityFrameworkCore; // وارد کردن امکانات EF Core

namespace Library.Data
{
    public class MyDBContext : DbContext // کلاس MyDBContext از DbContext ارث‌بری می‌کند
    {
        // سازنده کلاس با پیکربندی DbContextOptions
        public MyDBContext(DbContextOptions<MyDBContext> options)
            : base(options) // ارسال گزینه‌ها به سازنده پایه DbContext
        {
        }

        // نمایش مجموعه کاربرها در پایگاه داده
        public DbSet<User> Users { get; set; }

        // نمایش مجموعه کتاب‌ها در پایگاه داده
        public DbSet<Book> Books { get; set; }

        // نمایش مجموعه تراکنش‌ها در پایگاه داده
        public DbSet<Transaction> Transactions { get; set; }

        // نمایش مجموعه اقلام تراکنش‌ها در پایگاه داده
        public DbSet<TransactionItem> TransactionItems { get; set; }

        // نمایش مجموعه اطلاعات تماس در پایگاه داده
        public DbSet<OurContact> OurContacts { get; set; }

        // نمایش مجموعه کنترل‌کننده‌های ادمین در پایگاه داده
        public DbSet<OurContact> AdminControllers { get; set; }
    }
}