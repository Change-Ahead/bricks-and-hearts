using BricksAndHearts.Auth;
using BricksAndHearts.ViewModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public interface IAzureMapsApiService
{
    public Task<PropertyViewModel> AutofillAddress(PropertyViewModel model);
}

public class AzureMapsApiService : IAzureMapsApiService
{
    private readonly HttpClient _client = new();
    private readonly ILogger<AzureMapsApiService> _logger;
    private readonly IOptions<AzureMapsOptions> _options;

    public AzureMapsApiService(ILogger<AzureMapsApiService> logger, IOptions<AzureMapsOptions> options,
    HttpClient? client = null)
    {
        _logger = logger;
        _options = options;
        if (client != null)
        {
            _client = client;
        }
    }

    public async Task<string> MakeApiRequestToAzureMaps(string postalCode)
    {
        var escapedPostalCode = Uri.EscapeDataString(postalCode);
        var uri =
            $"https://atlas.microsoft.com/search/address/structured/json?api-version=1.0&countryCode=GB&postalCode={escapedPostalCode}&subscription-key={_options.Value.SubscriptionKey}";
        var responseBody = string.Empty;
        _logger.LogInformation("Making API Request to {Uri}", uri);
        try
        {
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successful API request to {Uri}", uri);
        }
        catch(Exception e)
        {
            _logger.LogWarning("Message :{Message} ",e.Message);
        }
        return responseBody;
    }

    public AzureMapsResponseModel TurnResponseBodyToModel(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return new AzureMapsResponseModel();
        }

        var postcodeResponse =
            JsonConvert.DeserializeObject<AzureMapsResponseModel>(responseBody);
        return postcodeResponse!;
    }

    public async Task<PropertyViewModel> AutofillAddress(PropertyViewModel model)
    {
        if (string.IsNullOrEmpty(model.Address.Postcode))
        {
            throw new ArgumentException("Model does not have a postcode");
        }

        if (string.IsNullOrEmpty(model.Address.AddressLine1))
        {
            throw new ArgumentException("Model does not have address line 1");
        }

        var postCode = model.Address.Postcode;
        var responseBody = await MakeApiRequestToAzureMaps(postCode);
        var postcodeResponse = TurnResponseBodyToModel(responseBody);
        if (postcodeResponse.ListOfResults == null || postcodeResponse.ListOfResults.Count < 1)
        {
            var address = new AddressModel()
            {
                Postcode = model.Address.Postcode,
                AddressLine1 = model.Address.AddressLine1
            };
            model = new PropertyViewModel()
            {
                Address = address
            };
            return model;
        }

        var results = postcodeResponse.ListOfResults[0];
        if (results.Address == null)
        {
            var address = new AddressModel()
            {
                Postcode = model.Address.Postcode,
                AddressLine1 = model.Address.AddressLine1
            };
            model = new PropertyViewModel()
            {
                Address = address
            };
            return model;
        }

        if (results.Address.ContainsKey("streetName"))
        {
            model.Address.AddressLine2 = results.Address["streetName"];
        }
        else
        {
            model.Address.AddressLine2 = null;
        }
        
        if (results.Address.ContainsKey("municipality"))
        {
            model.Address.TownOrCity = results.Address["municipality"];
        }
        else
        {
            model.Address.TownOrCity = null;
        }
        
        if (results.Address.ContainsKey("countrySecondarySubdivision"))
        {
            model.Address.County = results.Address["countrySecondarySubdivision"];
        }
        else
        {
            model.Address.County = null;
        }
        
        if (results.LatLon != null && results.LatLon.ContainsKey("lat"))
        {
            model.Lat = results.LatLon["lat"];
        }
        else
        {
            model.Lat = null;
        }

        if (results.LatLon != null && results.LatLon.ContainsKey("lon"))
        {
            model.Lon = results.LatLon["lon"];
        }
        else
        {
            model.Lon = null;
        }

        return model;
    }
}
