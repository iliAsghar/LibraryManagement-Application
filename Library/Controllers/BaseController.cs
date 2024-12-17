using Microsoft.AspNetCore.Mvc;

public class BaseController : Controller
{
    public BaseController()
    {
        if (User != null)
        {
            if (User.IsInRole("Admin"))
            {
                ViewBag.UserRole = "Admin";
            }
            else if (User.IsInRole("BookKeeper"))
            {
                ViewBag.UserRole = "BookKeeper";
            }
            else if (User.IsInRole("NormalUser"))
            {
                ViewBag.UserRole = "NormalUser";
            }
        }
    }
}