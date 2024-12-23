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
    public class AccountController : BaseController
    {
        private readonly MyDBContext _context;

        public AccountController(MyDBContext context) : base(context)
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
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                LastName = user.Lastname,
                NationalId = user.NationalId,
                Role = user.Role,
                PfpPath = user.PfpPath,
                Transactions = user.Transactions
            };

            return View(model);
        }

        public async Task<IActionResult> Register(RegisterViewModel model, IFormFile profilePicture)
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

            var newMember = new User
            {
                Email = model.Email,
                Name = model.Name,
                Lastname = model.LastName,
                NationalId = model.NationalId,
                PhoneNumber= model.PhoneNumber,
                Password = model.Password,
                Role = "User"
            };

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}-{profilePicture.FileName}";

            var filePath = Path.Combine(uploadPath, uniqueFileName);

            if (!IsValidImage(profilePicture))
            {
                ModelState.AddModelError("CoverImage", "فایل باید یک تصویر باشد!");
                return View(model);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            newMember.PfpPath = "/images/" + uniqueFileName;

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
                    new Claim("Role", user.Role),
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

        public IActionResult EditProfile()
        {
            var userId = GetLoggedInUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                LastName = user.Lastname,
                NationalId = user.NationalId,
                PfpPath = user.PfpPath
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? profilePicture)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetLoggedInUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Name = model.Name;
            user.Lastname = model.LastName;
            user.NationalId = model.NationalId;

            if (profilePicture != null)
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var uniqueFileName = $"{Guid.NewGuid()}-{profilePicture.FileName}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                if (!IsValidImage(profilePicture))
                {
                    ModelState.AddModelError("PfpPath", "فایل باید یک تصویر باشد!");
                    return View(model);
                }

                if (!string.IsNullOrEmpty(user.PfpPath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.PfpPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                user.PfpPath = "/images/" + uniqueFileName;
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            return RedirectToAction("Profile");
        }

        private int GetLoggedInUserId()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            return userId;
        }
        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(extension);
        }

    }
}
