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

        [Authorize(policy: "BookKeeper")]
        public IActionResult MemberList()
        {
            var members = _context.Users.
                Where(u => u.Role == "User")
                .ToList();

            return View(members);
        }

        [Authorize(policy: "Admin")]
        public IActionResult BookKeeperList()
        {
            var bookkeepers = _context.Users.
                Where(u => u.Role == "BookKeeper")
                .ToList();

            return View(bookkeepers);
        }

        [Authorize(policy: "Admin")]
        public IActionResult UserList()
        {
            var users = _context.Users.
                Where(u => u.Role == "User" || u.Role == "BookKeeper")
                .ToList();

            return View(users);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
