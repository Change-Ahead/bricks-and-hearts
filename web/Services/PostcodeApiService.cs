using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public interface IPostcodeApiService
{
    public Task<List<PropertyDbModel>?> SortPropertiesByLocation(string postalCode);
}

public class PostcodeApiService : IPostcodeApiService
{
    private readonly HttpClient _client = new();
    private readonly ILogger<PostcodeApiService> _logger;
    private readonly BricksAndHeartsDbContext _dbContext;

    public PostcodeApiService(ILogger<PostcodeApiService> logger, BricksAndHeartsDbContext dbContext, HttpClient? client = null)
    {
        _logger = logger;
        _dbContext = dbContext;
        if (client != null)
        {
            _client = client;
        }
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

    public async Task<List<PropertyDbModel>?> SortPropertiesByLocation(string postalCode)
    {
        var responseBody = await MakeApiRequestToPostcodeApi(postalCode);
        var model = TurnResponseBodyToModel(responseBody);
        if (model.Result == null || model.Result.Lat == null || model.Result.Lon == null)
        {
            return null;
        }

        var properties = _dbContext.Properties
            // This is a simpler method using pythagoras, not too accurate
            /*.FromSqlInterpolated(
                @$"
                SELECT *
                FROM dbo.Property
                WHERE Lon is not NULL and Lat is not NULL
                ORDER BY ((Lat-{model.Result.Lat})*(Lat-{model.Result.Lat})) + ((Lon - {model.Result.Lon})*(Lon - {model.Result.Lon})) ASC");
            */
            // This is a more complicated method, if things break, use method 1 and try again
            .FromSqlInterpolated(
                @$"SELECT *, (
                  6371 * acos (
                  cos ( radians({model.Result.Lat}) )
                  * cos( radians( Lat ) )
                  * cos( radians( Lon ) - radians({model.Result.Lon}) )
                  + sin ( radians({model.Result.Lat}) )
                  * sin( radians( Lat ) )
                    )
                ) AS distance 
                FROM dbo.Property
                ORDER BY distance
                "
                );
            return properties.ToList();
    }
}
