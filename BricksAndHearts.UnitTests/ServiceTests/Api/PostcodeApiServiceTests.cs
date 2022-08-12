using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class PostcodeApiServiceTests : IClassFixture<TestDatabaseFixture>
{
    private ILogger<PostcodeService> _logger;
    private HttpMessageHandler _messageHandler;
    private HttpClient _httpClient;
    private PostcodeService _underTest;
    private TestDatabaseFixture _fixture { get; }

    public PostcodeApiServiceTests(TestDatabaseFixture fixture)
    {
        _logger = A.Fake<ILogger<PostcodeService>>();
        _messageHandler = A.Fake<HttpMessageHandler>();
        _httpClient = new HttpClient(_messageHandler);
        _fixture = fixture;
        _underTest = new PostcodeService(_logger, null!, _httpClient);
    }

    [Fact]
    public async Task MakeApiRequestToPostcodeApi_WhenCalled_ReturnsNonEmptyString()
    {
        // Arrange
        const string postalCode = "cb11dx";
        var responseBody =
            await File.ReadAllTextAsync(
                $"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/PostcodeioApiResponse.json");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
        // Slightly icky because "SendAsync" is protected
        A.CallTo(_messageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .Returns(response);

        // Act
        var result = await _underTest.MakeApiRequestToPostcodeApi(postalCode);

        // Assert
        result.Should().Be(responseBody);
    }


    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithNonEmptyString_ReturnsNonEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        var path = AppDomain.CurrentDomain.BaseDirectory;
        var responseBody =  File.ReadAllText($"{path}/../../../ServiceTests/Api/PostcodeioApiResponse.json");

        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.Result.Should().NotBeNull("should not be null");
        var result = postcodeApiResponseViewModel.Result;
        result!.Lat.Should().NotBeNull("Address should not be null");
        result!.Lon.Should().NotBeNull("LatLon should not be null");
    }

    [Fact]
    public void TurnResponseBodyToModel_WhenCalledWithEmptyString_ReturnsEmptyPostcodeApiResponseViewModel()
    {
        // Arrange
        var responseBody =  string.Empty;
        
        // Act
        var postcodeApiResponseViewModel = _underTest.TurnResponseBodyToModel(responseBody);
        
        // Assert
        postcodeApiResponseViewModel.Result.Should().BeNull("should be null");
    }
}