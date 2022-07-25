using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class ApiTests : ApiServiceTestsBase
{
    [Fact]
    public async Task MakeApiRequestToAzureMaps_WhenCalled_ReturnsNonEmptyString()
    {
        // Arrange
        string postalCode = "cb11dx";

        // Act
        // Uses the fake version of apiService because we don't want to make the API call
        // Just want to check that the code works
        string json = "some json string";
        A.CallTo(() => _apiService.MakeApiRequestToAzureMaps(postalCode)).Returns(json);
        string? responseBody = await _apiService.MakeApiRequestToAzureMaps(postalCode);
        
        // Assert
        responseBody.Should().NotBeNullOrEmpty("should not be null or empty");
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithNonEmptyString_ReturnsNonEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        string path = System.AppDomain.CurrentDomain.BaseDirectory;
        string responseBody =  File.ReadAllText($"{path}/../../../ServiceTests/Api/AzureMapsApiResponse.json");

        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().NotBeNull("should not be null");
        BricksAndHearts.ViewModels.Results results = postcodeApiResponseViewModel.ListOfResults[0];
        results.Address.Should().NotBeNull("Address should not be null");
        results.Address.Keys.ToList().Should().Contain("streetName");
        results.Address["streetName"].Should().Be("Adam & Eve Street");
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithEmptyString_ReturnsEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        string responseBody =  String.Empty;
        
        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().BeNull("should not be null");
    }
    
    // The AutofillAddress method is getting tested manually via the front-end 
    // We tested 5 different postcodes and they all work
}