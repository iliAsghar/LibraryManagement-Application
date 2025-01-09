using Library.Data; // Import database context
using Microsoft.AspNetCore.Mvc; // For controller actions
using Microsoft.AspNetCore.Mvc.Filters; // For action filters
using System.Security.Claims; // For handling user claims
public class BaseController : Controller
{
    protected readonly MyDBContext _context;
    // Constructor to initialize database context
    public BaseController(MyDBContext context)
    {
        _context = context;
    }
    // Execute logic before every action method
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            ViewBag.UserRole = GetUserRole(); // Set the user's role in ViewBag
            ViewBag.UserClaims = User.Claims.ToList(); // Set all user claims in ViewBag
            ViewBag.UserPfpPath = GetUserPfpPath(); // Set user's profile picture path in ViewBag
            ViewBag.UserFullName = GetUserFullName(); // Set user's full name in ViewBag
        }
    }
    // Get the logged-in user's profile picture path
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
    // Get the logged-in user's ID
    private int GetLoggedInUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
    // Get the logged-in user's role
    public string GetUserRole()
    {
        return User.Claims.ElementAtOrDefault(2)?.Value;
    }
    // Get the logged-in user's full name
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
