using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly ILogger<MembersController> _logger;
        private readonly MyDBContext _context;

        public MembersController(ILogger<MembersController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult MemberList()
        {
            var members = _context.Users.
                Where(u => !u.IsAdmin).ToList();
            return View(members);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
