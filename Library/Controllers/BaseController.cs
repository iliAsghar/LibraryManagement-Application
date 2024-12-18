using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            ViewBag.UserRole = GetUserRole();

            ViewBag.UserClaims = User.Claims.ToList();
        }
    }

    // Example: Get the user's role from claims
    public string GetUserRole()
    {
        return User.Claims.ElementAtOrDefault(2).Value;
    }
}
