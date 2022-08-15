#region

using BricksAndHearts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

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
        if (TempData["FlashMessages"] == null||TempData["FlashTypes"] == null)
        {
            TempData["FlashTypes"] = new List<string>{flashType};
            TempData["FlashMessages"] = new List<string> {flashMessage};
        }
        else
        {
            (TempData["FlashTypes"] as List<string>)!.Add(flashType);
            (TempData["FlashMessages"] as List<string>)!.Add(flashMessage);
        }
    }
}