using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BricksAndHearts.Services;

public interface IPostcodeService
{
    public string FormatPostcode(string postcode);
    public Task AddSinglePostcodeToDatabaseIfAbsent(string postcode);
    public Task BulkAddPostcodesToDatabaseIfAbsent(List<string> postcodes);
}    


public class PostcodeService : IPostcodeService
{
    private readonly HttpClient _client;
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<PostcodeService> _logger;

    public PostcodeService(ILogger<PostcodeService> logger, BricksAndHeartsDbContext dbContext, HttpClient client)
    {
        _logger = logger;
        _dbContext = dbContext;
        _client = client;
    }

    public string FormatPostcode(string postcode)
    {
        if (Regex.IsMatch(postcode,
                @"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})"))
        {
            postcode = Regex.Replace(postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2").ToUpper();
        }
        else
        {
            postcode = "";
        }

        return postcode;
    }

    private void AddPostcodeToPostcodeDb(PostcodeioResponseModel postcodeResponse)
    {
        if (postcodeResponse.Result == null || postcodeResponse.Result.Lat == null || postcodeResponse.Result.Lon == null || postcodeResponse.Result.Postcode == null)
        {
            _logger.LogWarning("Postcode cannot be converted to coordinates.");
        }
        else
        {
            var postcodeDbModel = new PostcodeDbModel
            {
                Postcode = postcodeResponse.Result.Postcode,
                Lat = postcodeResponse.Result.Lat,
                Lon = postcodeResponse.Result.Lon,
            };
            _dbContext.Postcodes.Add(postcodeDbModel);
        }
        _dbContext.SaveChanges();
    }
    
    public async Task AddSinglePostcodeToDatabaseIfAbsent(string postcode)
    {
        if (!_dbContext.Postcodes.Any(p => p.Postcode == postcode))
        {
            var responseBody = await MakeSingleApiRequestToPostcodeApi(postcode);
            var postcodeResponse = TurnResponseBodyToModel(responseBody);
            AddPostcodeToPostcodeDb(postcodeResponse);
        }
    }

    private async Task<string> MakeSingleApiRequestToPostcodeApi(string postcode)
    {
        var escapedPostalCode = Uri.EscapeDataString(postcode);
        var uri = $"https://api.postcodes.io/postcodes/{escapedPostalCode}";
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
    
    private PostcodeioResponseModel TurnResponseBodyToModel(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return new PostcodeioResponseModel();
        }
        var postcodeResponse = JsonConvert.DeserializeObject<PostcodeioResponseModel>(responseBody);
        return postcodeResponse!;
    }

    public async Task BulkAddPostcodesToDatabaseIfAbsent(List<string> postcodes)
    {
        var postcodesToLookup = new List<string>();
        foreach (string postcode in postcodes)
        {
            if (!_dbContext.Postcodes.Any(p => p.Postcode == postcode) && !postcodesToLookup.Any(p => p == postcode))
            {
                postcodesToLookup.Add(postcode);
            }

            if (postcodesToLookup.Count() == 100)
            {
                await BulkGetLatLonForPostcodes(postcodesToLookup);
                postcodesToLookup = new List<string>();
            }
        }
        await BulkGetLatLonForPostcodes(postcodesToLookup);
    }

    private async Task BulkGetLatLonForPostcodes(List<string> postcodesToLookup)
    {
        var bulkResponseBody = await MakeBulkApiRequestToPostcodeApi(postcodesToLookup);
        var postcodeResponse = TurnBulkResponseBodyToModel(bulkResponseBody);

        foreach (PostcodeioResponseModel response in postcodeResponse)
        {
            AddPostcodeToPostcodeDb(response);
        }
    }

    private async Task<string> MakeBulkApiRequestToPostcodeApi(List<string> postcodes)
    {
        var postcodesJson = new { postcodes = postcodes.ToArray() };
        var uri = "https://api.postcodes.io/postcodes/";
        var bulkResponseBody = string.Empty;
        _logger.LogInformation("Making API Request to {Uri}", uri);
        try
        {
            var response = await _client.PostAsJsonAsync(uri, postcodesJson);
            response.EnsureSuccessStatusCode();
            bulkResponseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successful API request to {Uri}", uri);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Message :{Message} ", e.Message);
        }

        return bulkResponseBody;
    }

    private List<PostcodeioResponseModel> TurnBulkResponseBodyToModel(string bulkResponseBody)
    {
        var postcodeDbModelList = new List<PostcodeioResponseModel>();
        if (string.IsNullOrEmpty(bulkResponseBody))
        {
            return postcodeDbModelList;
        }
        JObject jObjectResponse = JObject.Parse(bulkResponseBody);
        IList<JToken> postcodeList = jObjectResponse["result"].ToList();
        foreach (JToken postcode in postcodeList)
        {
            var postcodeDbModel = postcode.ToObject<PostcodeioResponseModel>();
            postcodeDbModelList.Add(postcodeDbModel);
        }
        return postcodeDbModelList;
    }
}
