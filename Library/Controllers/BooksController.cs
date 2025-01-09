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
    [Authorize] // Ensure that the user is authenticated to access this controller
    public class BooksController : BaseController
    {
        // Logger for the BooksController
        private readonly ILogger<BooksController> _logger;
        // Database context for accessing book data
        private readonly MyDBContext _context;

        // Constructor to initialize logger and context
        public BooksController(ILogger<BooksController> logger, MyDBContext context) : base(context)
        {
            _logger = logger;
            _context = context;
        }

        // Displays a list of books, filtered if applicable
        [Authorize] // Ensure that the user is authenticated to access this method
        public IActionResult BookList(List<Book> filteredBooks = null)
        {
            var books = new List<Book>();

            // If there are filtered books, use them; otherwise get all books from the database
            if (filteredBooks != null)
            {
                books = filteredBooks.ToList(); // Copy filtered books to local list
                ViewData["IsSearchResult"] = "True"; // Indicate that the results are from a search
            }
            else
            {
                books = _context.Books.ToList(); // Get all books from the database
            }
            return View(books); // Return the view with the list of books
        }

        // Displays the details of a specific book identified by its id
        public async Task<ActionResult> ShowBook(int id)
        {
            // Retrieve the book from the database
            var book = await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id);

            // If the book is not found, redirect to book list with an error message
            if (book == null)
            {
                TempData["ErrorMessage"] = "کتاب مورد نظر یافت نشد!"; // Error message for book not found
                return RedirectToAction("BookList");
            }

            // Create a view model for the book details
            BookViewModel model = new BookViewModel()
            {
                Id = book.Id,
                Title = book.Title,
                Description = book.Description,
                Author = book.Author,
                Genre = book.Genre,
                CoverPath = book.CoverPath,
                TotalQuantity = book.TotalQuantity
            };

            return View(model); // Return the view with the book details
        }

        // Displays the form to add a new book
        [Authorize(Policy = "BookKeeper")] // Ensure that the user has the BookKeeper policy to access this method
        public IActionResult AddBook()
        {
            return View(new AddBookViewModel()); // Return the view for adding a new book
        }

        // Handles the submission of the new book form
        [HttpPost]
        [Authorize(Policy = "BookKeeper")] // Ensure that the user has the BookKeeper policy to access this method
        public async Task<IActionResult> AddBook(AddBookViewModel model, IFormFile coverImage)
        {
            // Validate the cover image file
            if (coverImage == null || coverImage.Length <= 0)
            {
                ModelState.AddModelError("CoverPath", "تصویر جلد الزامی است!"); // Error message if cover image is missing
            }

            // If the model state is invalid, return the view with the current model
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create a new book instance with the provided model data
            var book = new Book
            {
                Title = model.Title,
                Description = model.Description,
                Author = model.Author,
                Genre = model.Genre,
                TotalQuantity = model.TotalQuantity
            };

            // Set the path for saving the uploaded cover image
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            // Create the directory if it does not exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate a unique filename for the cover image
            var uniqueFileName = $"{Guid.NewGuid()}-{coverImage.FileName}";

            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Validate the uploaded file to ensure it is an image
            if (!IsValidImage(coverImage))
            {
                ModelState.AddModelError("CoverImage", "فایل باید یک تصویر باشد!"); // Error for invalid image format
                return View(model);
            }

            // Save the cover image to the server
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await coverImage.CopyToAsync(fileStream); // Copy the file to the specified file path
            }

            // Set the CoverPath property of the book to the path of the uploaded image
            book.CoverPath = "/images/" + uniqueFileName;

            // Add the new book to the database and save changes
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = "کتاب جدید با موفقیت ثبت شد!"; // Success message after adding the book
            return RedirectToAction("Index", "Books"); // Redirect to the books index page
        }

        // Edits the quantity of a specific book
        public async Task<IActionResult> EditBookQuantity(int bookId, int newQuantity)
        {
            // Retrieve the book from the database
            var book = await _context.Books
                .Where(b => b.Id == bookId)
                .FirstOrDefaultAsync();

            // If the book exists, update its quantity
            if (book != null)
            {
                book.TotalQuantity = newQuantity; // Update the book's total quantity
                _context.Books.Update(book); // Mark the book as modified
                _context.SaveChanges(); // Save changes to the database
            }

            return RedirectToAction("ShowBook", new { id = bookId }); // Redirect to the book details page
        }

        // Searches for books based on the provided query and filter
        public async Task<IActionResult> Search(string query, string advancedFilter = null, bool isPopup = false)
        {
            // If the query is empty, return an empty result or an empty view depending on the request type
            if (string.IsNullOrWhiteSpace(query))
            {
                if (isPopup)
                {
                    return Json(new { results = new List<object>() }); // Return empty JSON result for popup
                }
                return View("SearchResults", new List<Book>()); // Return the search results view with no results
            }

            // Query the books from the database
            var booksQuery = _context.Books.AsQueryable();

            // Filter the books based on the search query
            booksQuery = booksQuery.Where(book =>
                EF.Functions.Like(book.Title, $"%{query}%") ||
                EF.Functions.Like(book.Author, $"%{query}%") ||
                EF.Functions.Like(book.Description, $"%{query}%") ||
                EF.Functions.Like(book.Genre, $"%{query}%"));

            // Apply advanced filter if specified
            if (!string.IsNullOrEmpty(advancedFilter))
            {
                booksQuery = booksQuery.Where(book => book.TotalQuantity > 0); // Filter out books with zero quantity
            }

            // If the search is a popup request, return the top 3 results as JSON
            if (isPopup)
            {
                var topResults = await booksQuery
                    .Take(3)
                    .Select(book => new
                    {
                        book.Id,
                        book.Title,
                        book.Author,
                        book.Genre,
                        book.CoverPath
                    })
                    .ToListAsync();

                return Json(new { results = topResults }); // Return top results as JSON
            }

            // Get the complete search results and return the BookList view
            var searchResults = await booksQuery.ToListAsync();

            return View("BookList", searchResults); // Return the view with all search results
        }

        // Validates that the uploaded file is an image
        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" }; // Allowed image file extensions
            var extension = Path.GetExtension(file.FileName).ToLower(); // Get the file extension

            return allowedExtensions.Contains(extension); // Check if the extension is allowed
        }

        // Returns an error view with the request identifier
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // Show error view with request ID
        }
    }
}