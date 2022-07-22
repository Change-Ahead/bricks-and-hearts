using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace BricksAndHearts.Controllers;

[AllowAnonymous]
public class ApiController : AbstractController
{
    private readonly IApiService _apiService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger, IApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }
    
    public async Task<string> MakeApiRequestToAzureMaps(string postalCode)
    {
        string responseBody = await _apiService.MakeApiRequestToAzureMaps(postalCode);
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