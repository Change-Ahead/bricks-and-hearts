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

    protected void FlashRequestSuccess(ILogger logger, BricksAndHeartsUser user, string requestAction)
    {
        FlashMessage(logger,
            ($"Successfully {requestAction} for user {user.Id}",
                "success",
                $"Successfully {requestAction}"));
    }

    protected void FlashMessage(ILogger logger, (string logInfo, string flashtype, string flashmessage) flash)
    {
        logger.LogInformation(flash.logInfo);
        TempData["FlashType"] = flash.flashtype;
        TempData["FlashMessage"] = flash.flashmessage;
    }
    
    protected void FlashMultipleMessages(ILogger logger, (List<string> logInfo, List<string> flashtypes, List<string> flashmessages) flashes)
    {
        var flashTypes = new List<string>();
        var flashMessages = new List<string>();
        for (int i = 0; i < flashes.logInfo.Count; i++)
        {
            logger.LogInformation(flashes.logInfo[i]);
            flashTypes.Add(flashes.flashtypes[i]);
            flashMessages.Add(flashes.flashmessages[i]);
        }
        TempData["MultipleFlashTypes"] = flashTypes;
        TempData["MultipleFlashMessages"] = flashMessages;
    }
}