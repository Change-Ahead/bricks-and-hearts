using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class AzureMapsApiServiceTests
{
    private readonly HttpMessageHandler _messageHandler;
    private readonly AzureMapsApiService _underTest;

    public AzureMapsApiServiceTests()
    {
        var options = A.Fake<IOptions<AzureMapsOptions>>();
        var logger = A.Fake<ILogger<AzureMapsApiService>>();
        _messageHandler = A.Fake<HttpMessageHandler>();
        var httpClient = new HttpClient(_messageHandler);
        _underTest= new AzureMapsApiService(logger, options,httpClient);
    }
    
    [Fact]
    public async Task MakeApiRequestToAzureMaps_WhenCalled_ReturnsNonEmptyString()
    {
        // Arrange
        const string postalCode = "cb11dx";
        var responseBody = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/AzureMapsApiResponse.json");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
        // Slightly icky because "SendAsync" is protected
        A.CallTo(_messageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>().Returns(response);

        // Act
        var result = await _underTest.MakeApiRequestToAzureMaps(postalCode);

        // Assert
        result.Should().Be(responseBody);
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithNonEmptyString_ReturnsNonEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        var path = AppDomain.CurrentDomain.BaseDirectory;
        var responseBody =  File.ReadAllText($"{path}/../../../ServiceTests/Api/AzureMapsApiResponse.json");

        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().NotBeNull("should not be null");
        if (postcodeApiResponseViewModel.ListOfResults == null)
        {
            return;
        }
        var results = postcodeApiResponseViewModel.ListOfResults[0];
        results.Address.Should().NotBeNull("Address should not be null");
        results.LatLon.Should().NotBeNull("LatLon should not be null");

        results.Address!.Keys.ToList().Should().Contain("streetName");
        results.Address!["streetName"].Should().Be("Adam & Eve Street");
        results.LatLon!["lat"].Should().Be(52.2046);
        results.LatLon!["lon"].Should().Be(0.13244);
    }
    
    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithEmptyString_ReturnsEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        var responseBody =  string.Empty;
        
        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.ListOfResults.Should().BeNull("should be null");
    }

    [Fact]
    public async void AutofillAddress_WithValidPostcode_AutocompletesEverything()
    {
        // Arrange
        var model = new PropertyViewModel();
        const string postcode = "cb11dx";
        var responseBody = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/AzureMapsApiResponse.json");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
        // Slightly icky because "SendAsync" is protected
        A.CallTo(_messageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>().Returns(response);
        
        model.Address.Postcode = postcode;
        model.Address.AddressLine1 = "12 Adam & Eve Court";
        
        // Act
        await _underTest.AutofillAddress(model);
        
        // Assert
        model.Address.AddressLine2.Should().NotBeNull();
        model.Address.County.Should().NotBeNull();
        model.Address.TownOrCity.Should().NotBeNull();
        model.Location.Should().NotBeNull();
        model.Location!.Y.Should().Be(52.2046);
        model.Location!.X.Should().Be(0.13244);
    }
}