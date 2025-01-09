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
        // Logger for the HomeController
        private readonly ILogger<HomeController> _logger;
        // Database context for accessing contact information
        private readonly MyDBContext _context;

        // Constructor to initialize logger and context
        public HomeController(ILogger<HomeController> logger, MyDBContext context) : base(context)
        {
            _logger = logger;
            _context = context;
        }

        // Displays the appropriate view based on user role
        [Authorize] // Ensure that the user is authenticated to access this method
        public IActionResult Index()
        {
            var viewName = GetViewForRole(); // Get the view name based on user role
            return View(viewName); // Return the view for the specific role
        }

        // Displays contact information
        public async Task<IActionResult> Contact()
        {
            // Retrieve the contact information from the database
            var contactInfo = await _context.OurContacts.FirstOrDefaultAsync();

            // If there is no contact information, return a default empty model
            if (contactInfo == null)
            {
                ContactViewModel emptyModel = new ContactViewModel
                {
                    Title = "اطلاعات تماس موجود نیست", // Title indicating no contact info
                    Description = "",
                    Address = "",
                    PhoneNumber = "",
                    Email = ""
                };

                return View("Contact/Contact", emptyModel); // Return the view with the empty model
            }

            // Create a model with the retrieved contact information
            ContactViewModel model = new ContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View("Contact/Contact", model); // Return the view with the populated model
        }

        // Displays the contact editing form (only for Admins)
        [Authorize(policy: "Admin")] // Ensure that the user has the Admin policy to access this method
        public async Task<IActionResult> EditContact()
        {
            // Retrieve the existing contact information
            var contactInfo = await _context.OurContacts.FirstOrDefaultAsync();

            // If no contact info exists, return an empty editing model
            if (contactInfo == null)
            {
                EditContactViewModel raw = new EditContactViewModel();
                return View("Contact/EditContact", raw); // Return the empty edit view
            }

            // Create a model with existing contact information for editing
            EditContactViewModel model = new EditContactViewModel
            {
                Title = contactInfo.Title,
                Description = contactInfo.Description,
                Address = contactInfo.Address,
                PhoneNumber = contactInfo.PhoneNumber,
                Email = contactInfo.Email
            };

            return View("Contact/EditContact", model); // Return the edit view with the model
        }

        // Handles submission of the edited contact information
        [HttpPost]
        [Authorize(policy: "Admin")] // Ensure that the user has the Admin policy to access this method
        public async Task<IActionResult> EditContact(EditContactViewModel model)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return View(model); // Return the view with the model if the state is invalid
            }

            // Retrieve the existing contact information for update
            var existingContact = _context.OurContacts.FirstOrDefault();

            if (existingContact != null)
            {
                // Update existing contact information
                existingContact.Title = model.Title;
                existingContact.Description = model.Description;
                existingContact.Address = model.Address;
                existingContact.PhoneNumber = model.PhoneNumber;
                existingContact.Email = model.Email;

                // Update the contact in the database
                _context.OurContacts.Update(existingContact);
                await _context.SaveChangesAsync(); // Save changes asynchronously
                return RedirectToAction("Contact"); // Redirect to the Contact view
            }

            // If no existing contact info, create a new contact
            var newContact = new OurContact
            {
                Email = model.Email,
                Title = model.Title,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                Description = model.Description,
            };

            // Add the new contact to the database
            _context.OurContacts.Add(newContact);
            await _context.SaveChangesAsync(); // Save changes asynchronously

            return RedirectToAction("Contact"); // Redirect to the Contact view
        }

        // Gets the appropriate view name based on user role
        private string GetViewForRole()
        {
            // Determine which view to return based on user role
            if (GetUserRole() == "Admin")
            {
                return "AdminIndex"; // Admin view
            }
            else if (GetUserRole() == "BookKeeper")
            {
                return "BookKeeperIndex"; // BookKeeper view
            }
            return "NormalUserIndex"; // Default view for normal users
        }

        // Returns an error view with a request identifier
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // Show error view with request ID
        }
    }
}