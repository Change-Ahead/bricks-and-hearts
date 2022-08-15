using BricksAndHearts.Database;
using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public interface IPostcodeApiService
{
    public Task<string> MakeApiRequestToPostcodeApi(string postalCode);
    public PostcodeioResponseModel TurnResponseBodyToModel(string responseBody);
}    


public class PostcodeApiService : IPostcodeApiService
{
    private readonly HttpClient _client;
    private readonly ILogger<PostcodeApiService> _logger;
    private readonly BricksAndHeartsDbContext _dbContext;

    public PostcodeApiService(ILogger<PostcodeApiService> logger, BricksAndHeartsDbContext dbContext, HttpClient client)
    {
        _logger = logger;
        _dbContext = dbContext;
        _client = client;
    }

    public async Task<string> MakeApiRequestToPostcodeApi(string postalCode)
    {
        var escapedPostalCode = Uri.EscapeDataString(postalCode);
        var uri =
            $"https://api.postcodes.io/postcodes/{escapedPostalCode}";
        var responseBody = string.Empty;
        _logger.LogInformation("Making API Request to {Uri}", uri);
        try
        {
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successful API request to {Uri}", uri);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Message :{Message} ", e.Message);
        }

        return responseBody;
    }

    public PostcodeioResponseModel TurnResponseBodyToModel(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return new PostcodeioResponseModel();
        }

        var postcodeResponse =
            JsonConvert.DeserializeObject<PostcodeioResponseModel>(responseBody);
        return postcodeResponse!;
    }
}
