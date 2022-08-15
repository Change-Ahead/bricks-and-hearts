#region

using BricksAndHearts.Auth;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace BricksAndHearts.Controllers;

public abstract class AbstractController : Controller
{
    protected BricksAndHeartsUser GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true) throw new Exception("GetCurrentUser called when not authenticated");

        return (BricksAndHeartsUser)User.Identity;
    }

    protected void AddFlashMessage(string flashType, string flashMessage)
    {
        TempData["FlashTypes"] ??= new List<string>();
        TempData["FlashMessages"] ??= new List<string>();
        
        (TempData["FlashTypes"] as List<string>)!.Add(flashType);
        (TempData["FlashMessages"] as List<string>)!.Add(flashMessage);
    }
}