using Library.Data; // Import database context
using Library.Models; // Import models
using Library.ViewModels; // Import view models
using Microsoft.AspNetCore.Authentication; // For authentication
using Microsoft.AspNetCore.Authentication.Cookies; // For cookie-based authentication
using Microsoft.AspNetCore.Mvc; // For controller actions
using System.Security.Claims; // For handling user claims
using Microsoft.EntityFrameworkCore; // For database operations
using Microsoft.AspNetCore.Authorization; // For authorization attributes
using System.Net.Mail; // For email operations
using System.Net; // For network configurations

namespace Library.Controllers
{
    public class AccountController : BaseController
    {
        private readonly MyDBContext _context;
        // Constructor to initialize database context
        public AccountController(MyDBContext context) : base(context)
        {
            _context = context;
        }
        // Display user profile
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
        // Register a new user and handle profile picture upload
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
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                IsEmailVerified = false,
                Token = "",
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

            return RedirectToAction("VerifyEmail", new { userId = newMember.Id });
        }
        // Handle user login
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

            if (!user.IsEmailVerified)
            {
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

            if(user.IsEmailVerified)
            {
                return RedirectToAction("Index", "Home");
            } else
            {
                return RedirectToAction("VerifyEmail", new { userId = user.Id });
            }
        }
        // Log out the current user
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        // Show profile edit form
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
        // Save updated profile details, including optional profile picture
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
            user.Name = model.Name;
            user.Lastname = model.LastName;
            user.NationalId = model.NationalId;
            user.PhoneNumber = model.PhoneNumber;

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
        // Send email verification token
        public async Task<IActionResult> VerifyEmail(int userId)
        {
            await GenerateAndSendToken(userId);
            ViewBag.UserId = userId;
            return View(userId);
        }
        // Verify the user's email using a token
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(string token, int userId)
        {
            var user = await _context.Users
                        .Where(u => u.Id == userId)
                        .FirstOrDefaultAsync();

            if (Verify(token) == true)
            {
                user.IsEmailVerified = true;
                _context.Users.Update(user);
                _context.SaveChanges();
            } else
            {
                return View();
            }

            return RedirectToAction("Index", "Home");
        }
        // Start the password reset process
        public async Task<IActionResult> ForgotPassword(int userId)
        {
            await GenerateAndSendToken(userId);
            return View();
        }
        // Reset the password using a token
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userNId, string token, string newPass)
        {
            var user = await _context.Users
                .Where(u => u.NationalId == userNId)
                .FirstOrDefaultAsync();

            if(Verify(token))
            {
                user.Password = newPass;
                _context.Users.Update(user) ;
                _context.SaveChangesAsync();
                return View();
            } 

            return View();
        }

        private static readonly Random _random = new Random();
        // Generate and send a token via email
        public async Task GenerateAndSendToken(int userId)
        {
            var user = await _context.Users
                        .Where(u => u.Id == userId)
                        .FirstOrDefaultAsync();

            string resetToken = _random.Next(1000, 9999).ToString();
            user.Token = resetToken;
            user.TokenExpiration = DateTime.Now.AddHours(1);
            _context.Users.Update(user);
            _context.SaveChanges();

            var emailText = "Here's your Token: " + resetToken;
            SendEmail(user.Email, "User Token", emailText);
        }
        // Verify the token and update user status
        public bool Verify(string token)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Token == token);

            if (user != null && user.TokenExpiration > DateTime.Now)
            {
                user.IsEmailVerified = true;
                _context.SaveChanges();
                return true;
            }

            return false;
        }
        // Send an email with the provided details
        public void SendEmail(string toEmail, string subject, string body)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("Bekhan.Manager@gmail.com", "lhyq zlro zmer htxb"),
                EnableSsl = true
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress("Bekhan.Manager@gmail.com", "Bekhan"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
        // Get the currently logged-in user's ID
        private int GetLoggedInUserId()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier).ToString());
            return userId;
        }
        // Validate if a file is an acceptable image
        private bool IsValidImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            return allowedExtensions.Contains(extension);
        }

    }
}
