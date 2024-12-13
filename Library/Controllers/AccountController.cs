using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Library.Controllers
{
    public class AccountController : Controller
    {
        private MyDBContext _context;
        public AccountController(MyDBContext context)
        {
            _context = context;
        }

        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = _context.Users.
                FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "این ایمیل قبلا ثبت شده است!");
                return View(model);
            }

            var newMember = new User
            {
                Email = model.Email,
                Password = model.Password,
                Name = model.Name,
                Lastname = model.LastName,
                NationalId = model.NationalId,
                IsAdmin = false
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

            var user = _context.Users.
                FirstOrDefault(u =>
                u.Email.ToLower() == model.Email.ToLower()
                && u.Password == model.Password
                );

            if (user == null)
            {
                ModelState.AddModelError("Email", "اطلاعات صحیح نیست!");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            HttpContext.SignInAsync(principal, properties);

            if(user.IsAdmin)
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
