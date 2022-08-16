using System.Security.Claims;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Auth;

/*
 * Middleware-like claims transformer that sets up our custom user model.
 * This will run on every request to:
 * * Read user details from the existing Identity object (which will have come from the session cookie)
 * * Look up the user in our database
 *   * If the user doesn't exist in the database, insert them
 * * Construct a BricksAndHeartsUser and return that as the new Identity
 */
public class ClaimsTransformer : IClaimsTransformation
{
    private readonly BricksAndHeartsDbContext _context;
    private readonly ILogger<ClaimsTransformer> _logger;

    public ClaimsTransformer(BricksAndHeartsDbContext context, ILogger<ClaimsTransformer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var existingClaimsIdentity = (ClaimsIdentity)principal.Identity!;
        if (existingClaimsIdentity.AuthenticationType != GoogleDefaults.AuthenticationScheme)
            throw new Exception("Authentication failed, non-Google identity type");

        var googleAccountId = existingClaimsIdentity.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var googlePhotoUrl = existingClaimsIdentity.Claims.First(c => c.Type == "urn:google:picture").Value;

        // Find the user in the DB
        var databaseUser = await _context.Users.FirstOrDefaultAsync(u => u.GoogleAccountId == googleAccountId);
        if (databaseUser == null)
        {
            // If they don't exist yet, create them
            databaseUser = await CreateUserInDatabase(googleAccountId, googlePhotoUrl, existingClaimsIdentity);
            _logger.LogInformation("Created new user '{UserEmail}' in database with id {UserId}",
                databaseUser.GoogleEmail, databaseUser.Id);
        }
        else
        {
            await UpdateUserProfilePhoto(databaseUser, googlePhotoUrl);
        }

        var claims = new List<Claim>(existingClaimsIdentity.Claims);
        // If they're an admin in the database, add the role to their claims
        if (databaseUser.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        if (databaseUser.LandlordId != null) claims.Add(new Claim(ClaimTypes.Role, "Landlord"));

        // Build and return our new user principal
        var newClaimsIdentity =
            new BricksAndHeartsUser(databaseUser, claims, existingClaimsIdentity.AuthenticationType);
        return new ClaimsPrincipal(newClaimsIdentity);
    }

    private async Task<UserDbModel> CreateUserInDatabase(string googleAccountId, string googlePhotoUrl,
        ClaimsIdentity identity)
    {
        var name = identity.Name!;
        var email = identity.Claims.First(c => c.Type == ClaimTypes.Email).Value;
        var databaseUser = new UserDbModel
        {
            GoogleUserName = name,
            GoogleEmail = email,
            GoogleAccountId = googleAccountId,
            GoogleProfileImageUrl = googlePhotoUrl,
            IsAdmin = false
        };
        _context.Users.Add(databaseUser);
        // Google Account Id has a unique index, so if this races with another request one should fail without having duplicate users
        await _context.SaveChangesAsync();
        return databaseUser;
    }

    private async Task UpdateUserProfilePhoto(UserDbModel userDbModel, string photoUrl)
    {
        userDbModel.GoogleProfileImageUrl = photoUrl;
        await _context.SaveChangesAsync();
    }
}