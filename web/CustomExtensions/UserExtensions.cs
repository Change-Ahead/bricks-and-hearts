using System.Security.Claims;
namespace BricksAndHearts.CustomExtensions;

public static class UserExtensions
{
    public static bool IsLandlordOrAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Landlord") || user.IsInRole("Admin");
    }
}