﻿using Microsoft.AspNetCore.Mvc;
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
    private string GetUserRole()
    {
        if (User.HasClaim("Role", "Admin"))
            return "Admin";
        else if (User.HasClaim("Role", "BookKeeper"))
            return "BookKeeper";
        else if (User.HasClaim("Role", "NormalUser"))
            return "NormalUser";
        else
            return "Unknown";  // Default if no role is found
    }
}
