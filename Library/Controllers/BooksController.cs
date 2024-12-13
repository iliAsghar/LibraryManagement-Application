using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Library.Data;
using Microsoft.AspNetCore.Authorization;

namespace Library.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly ILogger<BooksController> _logger;
        private readonly MyDBContext _context;

        public BooksController(ILogger<BooksController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult BookList()
        {
            var books = _context.Books.ToList();
            return View(books);
        }

        public async Task<IActionResult> AddBook(Book book, IFormFile coverImage)
        {
            if(!ModelState.IsValid)
            {
                return View(book);
            }

            if (coverImage !=  null && coverImage.Length > 0)
            {
                var upleadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(upleadPath))
                {
                    Directory.CreateDirectory(upleadPath);
                }

                var uniqueFileName = $"{book.Id}-{book.Title}{Path.GetExtension(coverImage.FileName)}";

                var filePath = Path.Combine(upleadPath, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(fileStream);
                }

                book.CoverPath = "/images/" + uniqueFileName;
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "کتاب جدید با موفقیت ثبت شد!";
            return RedirectToAction("Index", "Books");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
