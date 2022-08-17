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
    
    // Create a user that is logged into Google but is not a landlord or admin
    protected UserDbModel CreateUserDbModel(bool isAdmin, bool isLandlord)
    {
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = isAdmin,
        };
        if (isLandlord)
        {
            userDbModel.LandlordId = 1;
            var landlordDbModel = new LandlordDbModel()
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
    
    protected BricksAndHeartsUser CreateUnregisteredUser()
    {
        var userDbModel = new UserDbModel()
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
    
    protected BricksAndHeartsUser CreateNonAdminNonLandlordUser()
    {
        var userDbModel = new UserDbModel()
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
    
    protected BricksAndHeartsUser CreateAdminUser()
    {
        var userDbModel = new UserDbModel()
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
    
    protected BricksAndHeartsUser CreateLandlordUser()
    {
        var userDbModel = new UserDbModel()
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = 1
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

    protected TenantDbModel CreateTenant()
    {
        return new TenantDbModel
        {
            Name = "Test Tenant",
            Postcode = "CB2 1LA"
        };
    }
}