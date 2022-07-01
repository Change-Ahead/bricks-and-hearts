using System.Security.Claims;
using BricksAndHearts.Database;

namespace BricksAndHearts.Auth;

public class BricksAndHeartsUser : ClaimsIdentity
{
    public BricksAndHeartsUser(UserDbModel dbUser, List<Claim> claims, string authenticationType) : base(claims, authenticationType)
    {
        Id = dbUser.Id;
        GoogleName = dbUser.GoogleUserName;
        GoogleEmail = dbUser.GoogleEmail;
        IsAdmin = dbUser.IsAdmin;
    }

    public int Id { get; set; }

    public string GoogleName { get; set; }

    public string GoogleEmail { get; set; }

    public bool IsAdmin { get; set; }
}