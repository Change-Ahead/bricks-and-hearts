namespace BricksAndHearts.Services;

public class ErrorService
{
    public static string GetStatusMessage(int statusCode)
    {
        string statusMessage = "";
        switch (statusCode)
        {
            case 404:
                statusMessage = "Page not found";
                break;
            default:
                statusMessage = "Unknown error occurred";
                break;
        }

        return statusMessage;
    }
}