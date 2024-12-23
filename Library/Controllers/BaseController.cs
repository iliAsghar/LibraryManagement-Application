using Library.Controllers;
using Library.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

public class BaseController : Controller
{
    protected readonly MyDBContext _context;
    public BaseController(MyDBContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            ViewBag.UserRole = GetUserRole();

            ViewBag.UserClaims = User.Claims.ToList();

            ViewBag.UserPfpPath = GetUserPfpPath();

            ViewBag.UserFullName = GetUserFullName();
        }
    }

    private string GetUserPfpPath()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = GetLoggedInUserId();

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user?.PfpPath;
        }
        return null;
    }

    private int GetLoggedInUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    public string GetUserRole()
    {
        return User.Claims.ElementAtOrDefault(2)?.Value;
    }

    private string GetUserFullName()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = GetLoggedInUserId();

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var userFullName = user?.Name + " " + user?.Lastname;
            return userFullName;
        }
        return null;
    }
}
