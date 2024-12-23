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

        public HomeController(ILogger<HomeController> logger, MyDBContext context) : base(context)
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
            var contactInfo = await _context.OurContacts.FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                ContactViewModel emptyModel = new ContactViewModel
                {
                    Title = "اطلاعات تماس موجود نیست",
                    Description = "",
                    Address = "",
                    PhoneNumber = "",
                    Email = ""
                };

                return View("Contact/Contact", emptyModel);
            }

            ContactViewModel model = new ContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View("Contact/Contact", model);
        }


        [Authorize(policy: "Admin")]
        public async Task<IActionResult> EditContact()
        {
            var contactInfo = await _context.OurContacts.FirstOrDefaultAsync();

            if (contactInfo == null)
            {
                EditContactViewModel raw = new EditContactViewModel();
                return View("Contact/EditContact", raw);
            }

            EditContactViewModel model = new EditContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View("Contact/EditContact", model);
        }

        [HttpPost]
        [Authorize(policy:"Admin")]
        public async Task<IActionResult> EditContact(EditContactViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingContact = _context.OurContacts
                .FirstOrDefault();

            if (existingContact != null)
            {
                existingContact.Title = model.Title;
                existingContact.Description = model.Description;
                existingContact.Address = model.Address;
                existingContact.PhoneNumber = model.PhoneNumber;
                existingContact.Email = model.Email;    





                _context.OurContacts.Update(existingContact);
                await _context.SaveChangesAsync();
                return RedirectToAction("Contact");
            }

            var newContact = new OurContact
            {
                Email = model.Email,
                Title = model.Title,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                Description = model.Description,

            };


            _context.OurContacts.Add(newContact);
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