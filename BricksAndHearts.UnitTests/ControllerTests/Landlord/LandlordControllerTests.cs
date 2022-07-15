using System.Collections.Generic;
using System.Security.Claims;
using BricksAndHearts.Auth;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTests
{
    private readonly LandlordController _underTest = new(null, null, null);

    [Fact]
    public void RegisterGet_CalledByUnregisteredUser_ReturnsRegisterViewWithEmail()
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

        // Act
        var result = _underTest.RegisterGet() as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>()
            .Which.Email.Should().Be(unregisteredUser.GoogleEmail);
    }
}
