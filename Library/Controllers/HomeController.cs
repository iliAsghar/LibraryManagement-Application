using Library.Data;
using Library.Models;
using Library.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Library.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDBContext _context;

        public HomeController(ILogger<HomeController> logger, MyDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public IActionResult Index()
        {
            var viewName = GetViewForRole();
            return View(viewName);
        }


        public async Task<IActionResult> Contact()
        {
            var contactInfo = await _context.Contacts.FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                ContactViewModel emptyModel = new ContactViewModel
                {
                    Title = "اطلاعات تماس موجود نیست",
                    Description = "",
                    Address = "",
                    PhoneNumber = null,
                    Email = ""
                };

                return View(emptyModel);
            }

            ContactViewModel model = new ContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View(model);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditContact()
        {
            var contactInfo = await _context.Contacts.FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                return NotFound();
            }

            var model = new EditContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContact(EditContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contactInfo = await _context.Contacts.FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                return NotFound();
            }

            contactInfo.Title = model.Title;
            contactInfo.Description = model.Description;
            contactInfo.Address = model.Address;
            contactInfo.PhoneNumber = model.PhoneNumber;
            contactInfo.Email = model.Email;

            _context.Contacts.Update(contactInfo);
            await _context.SaveChangesAsync();

            return RedirectToAction("Contact");
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