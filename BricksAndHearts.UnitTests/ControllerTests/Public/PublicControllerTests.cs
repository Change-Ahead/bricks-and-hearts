using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Public;

public class PublicControllerTests : PublicControllerTestsBase
{
    [Fact]
    public void ViewPublicProperty_CalledWithValidLink_ReturnCorrectModel()
    {
        // Arrange
        int propertyId = 1;
        var property = CreateExamplePropertyDbModel();
        var viewLink = "viewLink";
        property.PublicViewLink = viewLink;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(propertyId)).Returns(property);
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AnonUser }
        };

        // Act
        var result = UnderTest.ViewPublicProperty(propertyId, viewLink) as ViewResult;
        
        // Assert
        result!.Model.Should().BeOfType<PublicPropertyViewModel>().Which.SearchResult.Should()
            .Be(PublicPropertySearchResult.Success);
    }
    
    [Fact]
    public void ViewPublicProperty_CalledWithInvalidLink_ReturnCorrectModel()
    {
        // Arrange
        int propertyId = 1;
        var property = CreateExamplePropertyDbModel();
        var invalidLink = "invalidLink";
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(propertyId)).Returns(property);
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AnonUser }
        };

        // Act
        var result = UnderTest.ViewPublicProperty(propertyId, invalidLink) as ViewResult;
        
        // Assert
        result!.Model.Should().BeOfType<PublicPropertyViewModel>().Which.SearchResult.Should()
            .Be(PublicPropertySearchResult.IncorrectPublicViewLink);
    }
}