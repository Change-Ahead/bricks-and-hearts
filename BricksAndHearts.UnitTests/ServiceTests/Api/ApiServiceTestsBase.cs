using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.Extensions.Options;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class ApiServiceTestsBase
{
    static readonly IOptions<AzureMapsOptions> options = A.Fake<IOptions<AzureMapsOptions>>();
    // A fake API service
    protected readonly IApiService _apiService = A.Fake<IApiService>();
    // A real API service
    protected readonly ApiService _underTest = new(null!, options);
}