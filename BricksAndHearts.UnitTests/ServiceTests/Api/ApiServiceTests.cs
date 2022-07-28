using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class ApiTests
{
    // A fake options
    private static readonly IOptions<AzureMapsOptions> _options = A.Fake<IOptions<AzureMapsOptions>>();
    // A real API service
    private readonly AzureMapsAzureMapsApiService _underTest = new(null!, _options);
    
    [Fact]
    public async Task MakeApiRequestToAzureMaps_WhenCalled_ReturnsNonEmptyString()
    {
        // Setup
        var options = A.Fake<IOptions<AzureMapsOptions>>();
        var logger = A.Fake<ILogger<AzureMapsAzureMapsApiService>>();
        var fakeMessageHandler = A.Fake<HttpMessageHandler>();
        var fakeHttpClient = new HttpClient(fakeMessageHandler);
        var underTest = new AzureMapsAzureMapsApiService(logger, options, fakeHttpClient);

        // Arrange
        const string postalCode = "cb11dx";
        var responseBody = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/AzureMapsApiResponse.json");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
        // Slightly icky because "SendAsync" is protected
        A.CallTo(fakeMessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>().Returns(response);

        // Act
        var result = await underTest.MakeApiRequestToAzureMaps(postalCode);

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
        if (results.Address == null)
        {
            return;
        }
        results.Address.Keys.ToList().Should().Contain("streetName");
        results.Address["streetName"].Should().Be("Adam & Eve Street");
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
    
    // The AutofillAddress method is getting tested manually via the front-end 
    // We tested 5 different postcodes and they all work
}