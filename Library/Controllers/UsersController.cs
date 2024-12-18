using Library.Data;
using Library.Models;
using Library.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly ILogger<UsersController> _logger;
        private readonly MyDBContext _context;

        public UsersController(ILogger<UsersController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize(Policy = "BookKeeper")]
        public async Task<ActionResult> ShowUser(int id)
        {

            var user = await _context.Users
                .Where(u => u.Id == id)
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
                Id = user.Id,
                NationalId = user.NationalId,
                Name = user.Name,
                LastName = user.Lastname,
                Email = user.Email,
                Role = user.Role,
                Transactions = user.Transactions
            };

            return View(model);
        }

        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> PromoteToBookKeeper(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return View(user);
            }

            if (user.Role == "BookKeeper")
            {
                return View(user);
            }

            user.Role = "BookKeeper";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return View(user);
        }

        [Authorize(Policy = "Admin")]
        public async Task<ActionResult> DemoteToMember(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return View(user);
            }

            if (user.Role == "User")
            {
                return View(user);
            }

            user.Role = "User";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return View(user);
        }

        [Authorize(Policy = "BookKeeper")]
        public IActionResult MemberList()
        {
            var members = _context.Users
                .Where(u => u.Role == "User")
                .ToList();

            ViewData["Title"] = "اعضا";
            return View("UserList", members);
        }

        [Authorize(Policy = "Admin")]
        public IActionResult BookKeeperList()
        {
            var bookkeepers = _context.Users
                .Where(u => u.Role == "BookKeeper")
                .ToList();

            ViewData["Title"] = "کتابدار ها";
            return View("UserList", bookkeepers);
        }

        [Authorize(Policy = "Admin")]
        public IActionResult UserList()
        {
            var users = _context.Users
                .Where(u => u.Role == "User" || u.Role == "BookKeeper")
                .ToList();

            ViewData["Title"] = "کاربران";
            return View("UserList", users);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ViewModels.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
