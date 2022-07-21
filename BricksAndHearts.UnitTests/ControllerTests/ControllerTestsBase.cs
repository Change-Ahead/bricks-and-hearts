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

    protected BricksAndHeartsUser CreateUnregisteredUserInController(Controller _underTest)
    {
        // Arrange
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = null,
        };

        var unregisteredUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        var unregisteredUserPrincipal = new ClaimsPrincipal(unregisteredUser);

        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = unregisteredUserPrincipal }
        };
        return unregisteredUser;
    }
    
    protected BricksAndHeartsUser CreateAdminUserInController(Controller _underTest)
    {
        // Arrange
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = true,
            LandlordId = null,
        };

        var adminUser = new BricksAndHeartsUser(userDbModel, new List<Claim>(), "google");
        var adminUserPrincipal = new ClaimsPrincipal(adminUser);

        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminUserPrincipal }
        };
        return adminUser;
    }
}