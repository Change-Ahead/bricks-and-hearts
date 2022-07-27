using BricksAndHearts.Auth;
using BricksAndHearts.ViewModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public interface IAzureMapsApiService
{
    public Task<string> MakeApiRequestToAzureMaps(string postalCode);
    public Task<PropertyViewModel> AutofillAddress(PropertyViewModel model);
    public PostcodeApiResponseViewModel TurnResponseBodyToModel(string responseBody);
}

public class AzureMapsAzureMapsApiService : IAzureMapsApiService
{
    private readonly HttpClient _client = new HttpClient();
    private readonly ILogger<AzureMapsAzureMapsApiService> _logger;
    private readonly IOptions<AzureMapsOptions> _options;

    public AzureMapsAzureMapsApiService(ILogger<AzureMapsAzureMapsApiService> logger, IOptions<AzureMapsOptions> options)
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
    
    public PostcodeApiResponseViewModel TurnResponseBodyToModel(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return new PostcodeApiResponseViewModel();
        }

        PostcodeApiResponseViewModel postcodeResponse =
            JsonConvert.DeserializeObject<PostcodeApiResponseViewModel>(responseBody);
        return postcodeResponse;
    }

    public async Task<PropertyViewModel> AutofillAddress(PropertyViewModel model)
    {
        if (string.IsNullOrEmpty(model.Address.Postcode))
        {
            throw new ArgumentException("Model does not has a postcode!");
        }

        if (string.IsNullOrEmpty(model.Address.AddressLine1))
        {
            throw new ArgumentException("Model does not has address line 1!");
        }

        string postCode = model.Address.Postcode;
        string responseBody = await MakeApiRequestToAzureMaps(postCode);
        PostcodeApiResponseViewModel postcodeResponse = TurnResponseBodyToModel(responseBody);
        if (postcodeResponse.ListOfResults == null)
        {
            return model;
        }

        BricksAndHearts.ViewModels.Results results = postcodeResponse.ListOfResults[0];
        if (results.Address == null)
        {
            return model;
        }
        
        if (results.Address.Keys.ToList().Contains("streetName"))
        {
            model.Address.AddressLine2 = results.Address["streetName"];
        }
        if (results.Address.Keys.ToList().Contains("municipality"))
        {
            model.Address.TownOrCity = results.Address["municipality"];
        }
        if (results.Address.Keys.ToList().Contains("countrySecondarySubdivision"))
        {
            model.Address.County = results.Address["countrySecondarySubdivision"];
        }

        return model;
    }
}