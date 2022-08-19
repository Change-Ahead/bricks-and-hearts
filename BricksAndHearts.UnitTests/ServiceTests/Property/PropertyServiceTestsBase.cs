using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Property;

public class PropertyServiceTestsBase : IClassFixture<TestDatabaseFixture>
{
    protected BricksAndHeartsUser CreateAdminUser()
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

    protected BricksAndHeartsUser CreateLandlordUser()
    {
        var userDbModel = new UserDbModel
        {
            Id = 1,
            GoogleUserName = "John Doe",
            GoogleEmail = "test.email@gmail.com",
            IsAdmin = false,
            LandlordId = 1
        };

        var landlordDbModel = new LandlordDbModel
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