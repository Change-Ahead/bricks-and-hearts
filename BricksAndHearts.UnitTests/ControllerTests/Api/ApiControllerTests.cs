using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Results = BricksAndHearts.ViewModels.Results;

namespace BricksAndHearts.UnitTests.ControllerTests.Api;

public class ApiControllerTests : ApiControllerTestsBase
{
    [Fact]
    public async Task MakeApiRequestToAzureMaps_WhenCalled_ReturnsNonEmptyString()
    {
        // Arrange
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };
        string postalCode = "cb11dx";

        // Act
        string json = "some json string";
        A.CallTo(() => apiService.MakeApiRequestToAzureMaps(postalCode)).Returns(json);
        string? responseBody = await _underTest.MakeApiRequestToAzureMaps(postalCode);
        
        // Assert
        responseBody.Should().NotBeNullOrEmpty("should not be null or empty");
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithNonEmptyString_ReturnsNonEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };

        string path = System.AppDomain.CurrentDomain.BaseDirectory;
        string responseBody =  File.ReadAllText($"{path}/../../../ControllerTests/Api/AzureMapsApiResponse.json");
        // string responseBody =  File.ReadAllText("C:/Users/aliluo/RiderProjects/bricks-and-hearts/BricksAndHearts.UnitTests/ControllerTests/Api/AzureMapsApiResponse.json");

        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().NotBeNull("should not be null");
        Results results = postcodeApiResponseViewModel.ListOfResults[0];
        results.Address.Should().NotBeNull("Address should not be null");
        results.Address.Keys.ToList().Should().Contain("streetName");
        results.Address["streetName"].Should().Be("Adam & Eve Street");
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithEmptyString_ReturnsEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };
        string responseBody =  String.Empty;
        
        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().BeNull("should not be null");
    }
   
}