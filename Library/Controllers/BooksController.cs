using Library.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Library.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using Library.ViewModels;

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

        [Authorize(Policy = "BookKeeper")]
        [Authorize(Policy = "NormalUser")]
        public IActionResult BookList()
        {
            var books = _context.Books.ToList();
            return View(books);
        }

        public async Task<ActionResult> ShowBook(int id)
        {
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
            {
                TempData["ErrorMessage"] = "کتاب مورد نظر یافت نشد!";
                return RedirectToAction("BookList");
            }

            BookViewModel model = new BookViewModel()
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                Author = book.Author,
                CoverPath = book.CoverPath,
                TotalQuantity = book.TotalQuantity
            };

            return View(model);
        }

        [Authorize(Policy = "BookKeeper")]
        public async Task<IActionResult> AddBook(AddBookViewModel model, IFormFile coverImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var book = new Book
            {
                Title = model.Title,
                Description = model.Description,
                Author = model.Author,
                TotalQuantity = model.TotalQuantity
            };

            if (coverImage != null && coverImage.Length > 0)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var uniqueFileName = $"{Guid.NewGuid()}-{coverImage.FileName}";

                var filePath = Path.Combine(uploadPath, uniqueFileName);

                if (!IsValidImage(coverImage))
                {
                    ModelState.AddModelError("CoverImage", "فایل باید یک تصویر باشد!");
                    return View(model);
                }

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


        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(extension);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
