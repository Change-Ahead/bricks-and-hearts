using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;
[AllowAnonymous]
public class LoginController : Controller
{
    public IActionResult Index(string returnUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
}