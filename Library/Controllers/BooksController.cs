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
    public class BooksController : BaseController
    {
        private readonly ILogger<BooksController> _logger;
        private readonly MyDBContext _context;

        public BooksController(ILogger<BooksController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public IActionResult BookList(List<Book> filteredBooks = null)
        {
            var books = new List<Book>();

            if (filteredBooks != null)
            {
                books = filteredBooks.ToList();
                ViewData["IsSearchResult"] = "True";
            } else
            {
                books = _context.Books.ToList();
            }
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
        public IActionResult AddBook()
        {
            return View(new AddBookViewModel());
        }

        [HttpPost]
        [Authorize(Policy = "BookKeeper")]
        public async Task<IActionResult> AddBook(AddBookViewModel model, IFormFile coverImage)
        {
            if (coverImage == null || coverImage.Length <= 0)
            {
                ModelState.AddModelError("CoverPath", "تصویر جلد الزامی است!");
            }

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

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "کتاب جدید با موفقیت ثبت شد!";
            return RedirectToAction("Index", "Books");
        }

        public async Task<IActionResult> Search(string query, string advancedFilter = null, bool isPopup = false)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                if (isPopup)
                {
                    return Json(new { results = new List<object>() });
                }
                return View("SearchResults", new List<Book>());
            }

            var booksQuery = _context.Books.AsQueryable();

            booksQuery = booksQuery.Where(book =>
                EF.Functions.Like(book.Title, $"%{query}%") ||
                EF.Functions.Like(book.Author, $"%{query}%") ||
                EF.Functions.Like(book.Description, $"%{query}%"));

            if (!string.IsNullOrEmpty(advancedFilter))
            {
                booksQuery = booksQuery.Where(book => book.TotalQuantity > 0);
            }

            if (isPopup)
            {
                var topResults = await booksQuery
                    .Take(3)
                    .Select(book => new
                    {
                        book.Id,
                        book.Title,
                        book.Author,
                        book.CoverPath
                    })
                    .ToListAsync();

                return Json(new { results = topResults });
            }

            var searchResults = await booksQuery.ToListAsync();

            return View("BookList", searchResults);
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
