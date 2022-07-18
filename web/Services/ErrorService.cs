namespace BricksAndHearts.Services;

public interface IErrorService
{
    public (string statusName, string statusMessage) GetStatusMessage(int statusCode);
}
public class ErrorService : IErrorService
{ 
    public (string statusName, string statusMessage) GetStatusMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => ("Bad Request",
                "The request cannot be processed by the server. This may be due to an error in the request which has been input."),
            401 => ("Unauthorised", "Authentication is required to access this page."),
            403 => ("Forbidden",
                "Your account does not have access to this content. If you believe that it should, contact the administrator."),
            404 => ("Page not found", "Unfortunately this page does not appear to exist."),
            405 => ("Method Not Allowed", "This action may not be performed on this resource."),
            406 => ("Not acceptable", "Cannot find an appropriate representation of this source."),
            407 => ("Proxy Authentication Required", "Authentication (by a proxy) is required to access this page."),
            408 => ("Request Timeout",
                "This connection has timed out. To reload this page, please refresh your browser."),
            409 => ("Conflict", "This request conflicts with the state of the server."),
            410 => ("Gone", "Unfortunately this page is no longer available."),
            413 => ("Payload Too Large", "This request is too large for the server."),
            414 => ("URI Too Long", "The requested URI is too long for the server to interpret."),
            415 => ("Unsupported Media Type", "The requested media type is not supported by the server."),
            417 => ("Expectation Failed", "The server cannot meet the expectations of the request."),
            423 => ("Locked", "This resource is locked."),
            424 => ("Failed Dependency", "This request had failed due to the failure of the previous request."),
            426 => ("Upgrade Required",
                "The server cannot perform this request using the current protocol. This error may be resolved by upgrading to a new protocol."),
            429 => ("Too Many Requests",
                "You have sent too many requests in a short amount of time. Please wait before attempting to send any further requests."),
            451 => ("Unavailable For Legal Reasons", "This resource cannot be legally provided."),
            500 => ("Internal Server Error", "The server has encountered an unexpected error."),
            501 => ("Not Implemented", "The server does not support the request method."),
            502 => ("Bad Gateway", "The server has failed while attempting to access an external resource."),
            503 => ("Service Unavailable",
                "The server cannot handle the request, possibly due to maintenance. Please try again later."),
            504 => ("Gateway Timeout",
                "The server has failed while attempting to access an external resource, due to the external resource taking too long to respond."),
            505 => ("HTTP Version Not Supported",
                "The HTTP version used in the request is not supported by the server."),
            507 => ("Insufficient Storage", "The server lacks sufficient storage space to process this request."),
            _ => ("Unknown Error",
                "This error is rare or unexpected. For further details, try searching the error code or contact the administrator.")
        };
    }
}