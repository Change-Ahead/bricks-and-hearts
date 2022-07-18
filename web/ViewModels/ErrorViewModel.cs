namespace BricksAndHearts.ViewModels;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string? StatusName { get; set; }

    public string? StatusMessage { get; set; }
}
