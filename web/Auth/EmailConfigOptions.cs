namespace BricksAndHearts.Auth;

public class EmailConfigOptions
{
    public const string Email = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public List<string> ToAddress { get; set; } = new ();
}