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
    protected readonly ClaimsPrincipal _anonUser = A.Fake<ClaimsPrincipal>();

    protected void MakeUserPrincipalInController(BricksAndHeartsUser user, Controller _underTest)
    {
        var userPrincipal = new ClaimsPrincipal(user);
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userPrincipal }
        };
    }
    
    protected BricksAndHeartsUser CreateUnregisteredUser()
    {
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = null,
        };

        var unregisteredUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return unregisteredUser;
    }
    
    protected BricksAndHeartsUser CreateAdminUser()
    {
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = true,
            LandlordId = null,
        };

        var adminUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return adminUser;
    }
    
    protected BricksAndHeartsUser CreateLandlordUser()
    {
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = 1,
        };

        var landlordDbModel = new LandlordDbModel()
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            User = userDbModel
        };
        
        var landlordUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        return landlordUser;
    }
}