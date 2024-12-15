using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly MyDBContext _context;

        public UsersController(ILogger<UsersController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize(policy: "Admin")]
        [Authorize(policy: "BookKeeper")]
        public async Task<ActionResult> ShowUser(int id)
        {
            var user = await _context.Users
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                // todo do something here
                return View();
            } 

            return View(user);
        }

        [Authorize(policy: "Admin")]
        public async Task<ActionResult> PromoteToBookKeeper(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                // todo do something here
                return View(user);
            }

            if (user.Role == "BookKeeper")
            {
                // todo do something here
                return View(user);
            }

            user.Role = "BookKeeper";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return View(user);
        }

        [Authorize(policy: "Admin")]
        public async Task<ActionResult> DemoteToMember(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                // todo do something here
                return View(user);
            }

            if (user.Role == "User")
            {
                // todo do something here
                return View(user);
            }

            user.Role = "User";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return View(user);
        }

        [Authorize(policy: "BookKeeper")]
        public IActionResult MemberList()
        {
            var members = _context.Users.
                Where(u => u.Role == "User")
                .ToList();

            ViewData["ViewType"] = "MemberList";
            return View("UserList", members);
        }

        [Authorize(policy: "Admin")]
        public IActionResult BookKeeperList()
        {
            var bookkeepers = _context.Users.
                Where(u => u.Role == "BookKeeper")
                .ToList();

            ViewData["ViewType"] = "AdminList";
            return View("UserList", bookkeepers);
        }

        [Authorize(policy: "Admin")]
        public IActionResult UserList()
        {
            var users = _context.Users.
                Where(u => u.Role == "User" || u.Role == "BookKeeper")
                .ToList();

            ViewData["ViewType"] = "UserList";
            return View("UserList", users);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
