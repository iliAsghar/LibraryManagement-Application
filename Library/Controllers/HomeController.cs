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
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDBContext _context;

        public HomeController(ILogger<HomeController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var viewName = GetViewForRole();
            return View(viewName);
        }

        public async Task<IActionResult> Contact()
        {
            var contactInfo = await _context.Contacts
                .FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                return NotFound();
            }

            ContactViewModel model = new ContactViewModel()
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View(model);
        }

        private string GetViewForRole()
        {
            if (GetUserRole() == "Admin")
            {
                return "AdminIndex";
            }
            else if (GetUserRole() == "BookKeeper")
            {
                return "BookKeeperIndex";
            }
            return "NormalUserIndex";
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}