#region

using BricksAndHearts.Auth;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace BricksAndHearts.Controllers;

public abstract class AbstractController : Controller
{
    protected BricksAndHeartsUser CurrentUser => GetCurrentUser();

    protected BricksAndHeartsUser GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            throw new Exception("GetCurrentUser called when not authenticated");
        }

        return (BricksAndHeartsUser)User.Identity;
    }

    protected void AddFlashMessage(string flashType, string flashMessage)
    {
        TempData["FlashTypes"] ??= new string[] {};
        TempData["FlashMessages"] ??= new string[] {};

        var flashMessageArray = TempData["FlashMessages"] as string[];
        var flashMessageList = new List<string>(flashMessageArray!);
        flashMessageList.Add(flashMessage);

        var flashTypeArray = TempData["FlashTypes"] as string[];
        var flashTypeList = new List<string>(flashTypeArray!);
        flashTypeList.Add(flashType);

        TempData["FlashMessages"] = flashMessageList.ToArray();
        TempData["FlashTypes"] = flashTypeList.ToArray();
    }
}