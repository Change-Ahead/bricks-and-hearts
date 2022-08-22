using System.Collections.Generic;
using System.Security.Claims;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.UnitTests.ControllerTests;

public class ControllerTestsBase
{
    protected readonly ClaimsPrincipal AnonUser = A.Fake<ClaimsPrincipal>();

    protected static void MakeUserPrincipalInController(BricksAndHeartsUser user, Controller underTest)
    {
        var userPrincipal = new ClaimsPrincipal(user);
        underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };
    }
    
    // Create a user that is logged into Google but is not a landlord or admin
    protected static UserDbModel CreateUserDbModel(bool isAdmin, bool isLandlord)
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = isAdmin,
        };
        if (isLandlord)
        {
            userDbModel.LandlordId = 1;
            userDbModel.Landlord = new LandlordDbModel
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                User = userDbModel
            };
        }
        else
        {
            userDbModel.LandlordId = null;
        }
        return userDbModel;
    }
    
    protected static BricksAndHeartsUser CreateUnregisteredUser()
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = null
        };

        var unregisteredUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return unregisteredUser;
    }
    
    protected static BricksAndHeartsUser CreateNonAdminNonLandlordUser()
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = null
        };

        var nonAdminNonLandlordUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return nonAdminNonLandlordUser;
    }
    
    protected static BricksAndHeartsUser CreateAdminUser()
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = true,
            LandlordId = null
        };

        var adminUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return adminUser;
    }
    
    protected static BricksAndHeartsUser CreateLandlordUser()
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = 1,
            Landlord = new LandlordDbModel
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe"
            }
        }; 
        
        var landlordUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return landlordUser;
    }

    protected static TenantDbModel CreateTenant()
    {
        return new TenantDbModel
        {
            Name = "Test Tenant",
            Postcode = new PostcodeDbModel
            {
                Postcode = "CB2 1LA"
            }
        };
    }
}