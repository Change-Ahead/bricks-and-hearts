namespace BricksAndHearts.Auth;

public class EmailConfigOptions
{
    public const string Email = "Email";

    public string Host { get; set; } = String.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string FromAddress { get; set; } = String.Empty;
    public string ToAddress { get; set; } = String.Empty;
}