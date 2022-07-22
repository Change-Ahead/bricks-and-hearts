using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Api;

public class ApiControllerTestsBase : ControllerTestsBase
{
    public static IApiService apiService = A.Fake<IApiService>();
    protected readonly ApiController _underTest = new(null!, apiService);
}