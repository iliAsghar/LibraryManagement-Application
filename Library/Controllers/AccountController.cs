using Library.Data;
using Library.Models;
using Library.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Library.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDBContext _context;

        public AccountController(MyDBContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetLoggedInUserId();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Include(u => u.Transactions)
                    .ThenInclude(t => t.TransactionItems)
                        .ThenInclude(ti => ti.Book)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            UserViewModel model = new UserViewModel()
            {
                NationalId = user.NationalId,
                Name = user.Name,
                LastName = user.Lastname,
                Email = user.Email,
                Role = user.Role,
                Transactions = user.Transactions
            };

            return View(model);
        }

        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "این ایمیل قبلا ثبت شده است!");
                return View(model);
            }

            string hashedPassword = model.Password;

            var newMember = new User
            {
                Email = model.Email,
                Password = hashedPassword,
                Name = model.Name,
                Lastname = model.LastName,
                NationalId = model.NationalId,
                Role = "User"
            };

            _context.Users.Add(newMember);
            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null || !(model.Password == user.Password))
            {
                ModelState.AddModelError("Email", "اطلاعات صحیح نیست!");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            HttpContext.SignInAsync(principal, properties);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        private int GetLoggedInUserId()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            return userId;
        }

    }
}
