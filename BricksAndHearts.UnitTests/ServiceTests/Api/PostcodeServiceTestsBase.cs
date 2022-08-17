using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class PostcodeServiceTestsBase: IClassFixture<TestDatabaseFixture>
{
    protected readonly ILogger<PostcodeService> Logger;
    protected readonly HttpMessageHandler MessageHandler;
    protected readonly HttpClient HttpClient;
    protected readonly PostcodeService UnderTest;

    protected PostcodeServiceTestsBase()
    {
        Logger = A.Fake<ILogger<PostcodeService>>();
        MessageHandler = A.Fake<HttpMessageHandler>();
        HttpClient = new HttpClient(MessageHandler);
        UnderTest = new PostcodeService(Logger, null!, HttpClient);
    }
    
    protected async Task<HttpResponseMessage> CreateHttpResponseMessageForSingleApiRequest()
    {
        var responseBody =
            await File.ReadAllTextAsync(
                $"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/PostcodeioApiSingleResponse.json");
        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
    }
    
    public async Task<HttpResponseMessage> CreateHttpResponseMessageForBulkApiRequest()
    {
        var responseBody =
            await File.ReadAllTextAsync(
                $"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/PostcodeioApiBulkResponse.json");
        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
    }
}