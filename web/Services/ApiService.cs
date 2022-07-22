using BricksAndHearts.Auth;
using Microsoft.Extensions.Options;

namespace BricksAndHearts.Services;

public interface IApiService
{
    public Task<string> MakeApiRequestToAzureMaps(string postalCode);
}

public class ApiService : IApiService
{
    private readonly HttpClient _client = new HttpClient();
    private readonly ILogger<ApiService> _logger;
    private readonly IOptions<AzureMapsOptions> _options;

    public ApiService(ILogger<ApiService> logger, IOptions<AzureMapsOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task<string> MakeApiRequestToAzureMaps(string postalCode)
    {
        string uri =
            $"https://atlas.microsoft.com/search/address/structured/json?api-version=1.0&countryCode=GB&postalCode={postalCode}&subscription-key={_options.Value.SubscriptionKey}";
        string responseBody = String.Empty;
        _logger.LogInformation("Making API Request to {0}",uri);
        try
        {
            HttpResponseMessage response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successful API request to {0}",uri);
        }
        catch(HttpRequestException e)
        {
            _logger.LogWarning("Message :{0} ",e.Message);	
        }
        return responseBody;
    }
    // Begin code copied from BusBoard
    /*public static async Task<Dictionary<string,string>> Post2LatLong(string postCode)
    {
        // Make API call
        string uri = $"http://api.postcodes.io/postcodes/{postCode}";
        string responseBody = await MakeApiReq(uri, "Access to postcode API");
           
        // Deserialize JSON
        PostCodeResponse postCodeResponse = JsonConvert.DeserializeObject<PostCodeResponse>(responseBody);
        string lat;
        string lon;
        try
        {
            lat = postCodeResponse.res["latitude"].ToString();
            lon = postCodeResponse.res["longitude"].ToString();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
            
        return new Dictionary<string, string>()
        {
            { "long", lon },
            { "lat", lat },
        };
    }*/
}