namespace BricksAndHearts.Services;

public class ErrorService
{
    public static (string, string) GetStatusMessage(int statusCode)
    {
        string statusName = "";
        string statusMessage = "";
        switch (statusCode)
        {
            case 400:
                statusName = "Bad Request";
                statusMessage =
                    "The request cannot be processed by the server. This may be due to an error in the request which has been input.";
                break;
            case 401:
                statusName = "Unauthorised";
                statusMessage = "Authentication is required to access this page.";
                break;
            case 403:
                statusName = "Forbidden";
                statusMessage =
                    "Your account does not have access to this content. If you believe that it should, contact the administrator.";
                break;
            case 404:
                statusName = "Page not found";
                statusMessage = "Unfortunately this page does not appear to exist.";
                break;
            case 405:
                statusName = "Method Not Allowed";
                statusMessage = "This action may not be performed on this resource.";
                break;
            case 406:
                statusName = "Not Acceptable";
                statusMessage = "Cannot find an appropriate representation of this source.";
                break;
            case 407:
                statusName = "Proxy Authentication Required";
                statusMessage = "Authentication (by a proxy) is required to access this page.";
                break;
            case 408:
                statusName = "Request Timeout";
                statusMessage = "This connection has timed out. To reload this page, please refresh your browser.";
                break;
            case 409:
                statusName = "Conflict";
                statusMessage = "This request conflicts with the state of the server.";
                break;
            case 410:
                statusName = "Gone";
                statusMessage = "Unfortunately this page is no longer available.";
                break;
            case 413:
                statusName = "Payload Too Large";
                statusMessage = "This request is too large for the server.";
                break;
            case 414:
                statusName = "URI Too Long";
                statusMessage = "The requested URI is too long for the server to interpret.";
                break;
            case 415:
                statusName = "Unsupported Media Type";
                statusMessage = "The requested media type is not supported by the server.";
                break;
            case 417:
                statusName = "Expectation Failed";
                statusMessage = "The server cannot meet the expectations of the request.";
                break;
            case 418:
                statusName = "I'm a teapot";
                statusMessage = "Coffee may not be brewed in a teapot.";
                break;
            case 423:
                statusName = "Locked";
                statusMessage = "This resource is locked.";
                break;
            case 424:
                statusName = "Failed Dependency";
                statusMessage = "This request had failed due to the failure of the previous request.";
                break;
            case 426:
                statusName = "Upgrade Required";
                statusMessage =
                    "The server cannot perform this request using the current protocol. This error may be resolved by upgrading to a new protocol.";
                break;
            case 429:
                statusName = "Too Many Requests";
                statusMessage =
                    "You have sent too many requests in a short amount of time. Please wait before attempting to send any further requests.";
                break;
            case 451:
                statusName = "Unavailable For Legal Reasons";
                statusMessage = "This resource cannot be legally provided.";
                break;
            case 500:
                statusName = "Internal Server Error";
                statusMessage = "The server has encountered an unexpected error.";
                break;
            case 501:
                statusName = "Not Implemented";
                statusMessage = "The server does not support the request method.";
                break;
            case 502:
                statusName = "Bad Gateway";
                statusMessage = "The server has failed while attempting to access an external resource.";
                break;
            case 503:
                statusName = "Service Unavailable";
                statusMessage =
                    "The server cannot handle the request, possibly due to maintenance. Please try again later.";
                break;
            case 504:
                statusName = "Gateway Timeout";
                statusMessage =
                    "The server has failed while attempting to access an external resource, due to the external resource taking too long to respond.";
                break;
            case 505:
                statusName = "HTTP Version Not Supported";
                statusMessage = "The HTTP version used in the request is not supported by the server.";
                break;
            case 507:
                statusName = "Insufficient Storage";
                statusMessage = "The server lacks sufficient storage space to process this request.";
                break;
            default:
                statusName = "Unknown Error";
                statusMessage =
                    "This error is rare or unexpected. For further details, try searching the error code or contact the administrator.";
                break;
        }

        return (statusName, statusMessage);
    }
}