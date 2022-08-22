using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using BricksAndHearts.Models;
using NetTopologySuite.Geometries;
using Newtonsoft.Json.Linq;

namespace BricksAndHearts.Services;

public interface IPostcodeService
{
    public string FormatPostcode(string postcode);
    public Task<PostcodeDbModel?> GetPostcodeAndAddIfAbsent(string? postcode);
    public Task AddPostcodesToDatabaseIfAbsent(IEnumerable<string> postcodes);
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
        const string postcodePattern =
            @"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})";
        return Regex.IsMatch(postcode, postcodePattern)
            ? Regex.Replace(postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2").ToUpper()
            : "";
    }

    public async Task<PostcodeDbModel?> GetPostcodeAndAddIfAbsent(string? postcode)
    {
        if (string.IsNullOrEmpty(postcode))
        {
            return null;
        }

        postcode = FormatPostcode(postcode);

        var dbModel = await _dbContext.Postcodes.FindAsync(postcode);
        if (dbModel != null)
        {
            return dbModel;
        }

        await AddPostcodesToDatabaseIfAbsent(new[] { postcode });

        return await _dbContext.Postcodes.FindAsync(postcode);
    }

    public async Task AddPostcodesToDatabaseIfAbsent(IEnumerable<string> postcodes)
    {
        var formattedPostcodes = postcodes.Select(FormatPostcode).Distinct().ToList();
        var postcodesToAddChunks = formattedPostcodes
            .Where(p => p != "" && _dbContext.Postcodes.All(dbRecord => dbRecord.Postcode != p))
            .Chunk(100);

        var postcodesToUpdateChunks = formattedPostcodes
            .Where(p => p != "" && _dbContext.Postcodes.Any(dbRecord => dbRecord.Postcode == p && dbRecord.Location == null))
            .Chunk(100);

        var postcodeAddResponses = await Task.WhenAll(postcodesToAddChunks.Select(async p => await GetPostcodeDetails(p)));
        var postcodeUpdateResponses = await Task.WhenAll(postcodesToUpdateChunks.Select(async p => await GetPostcodeDetails(p)));

        var postcodeAddGroups = postcodeAddResponses
            .SelectMany(p => p)
            .GroupBy(p => p.Result?.Lat == null || p.Result.Lon == null || p.Result.Postcode == null)
            .ToDictionary(grouping => grouping.Key ? "invalid" : "valid", grouping => grouping.ToArray());
        var postcodeUpdateGroups = postcodeUpdateResponses
            .SelectMany(p => p)
            .GroupBy(p => p.Result?.Lat == null || p.Result.Lon == null || p.Result.Postcode == null)
            .ToDictionary(grouping => grouping.Key ? "invalid" : "valid", grouping => grouping.ToArray());

        if (postcodeAddGroups.ContainsKey("valid"))
        {
            AddPostcodesToPostcodeDb(postcodeAddGroups["valid"]);
        }

        if (postcodeUpdateGroups.ContainsKey("valid"))
        {
            UpdatePostcodes(postcodeUpdateGroups["valid"]);
        }

        var invalidPostcodeCount = 0;
        if (postcodeAddGroups.ContainsKey("invalid"))
        {
            invalidPostcodeCount += postcodeAddGroups["invalid"].Length;
        }
        if (postcodeUpdateGroups.ContainsKey("invalid"))
        {
            invalidPostcodeCount += postcodeUpdateGroups["invalid"].Length;
        }

        if (invalidPostcodeCount > 0)
        {
            _logger.LogWarning($"{invalidPostcodeCount} postcodes cannot be converted to coordinates.");
        }
    }

    private async Task<IEnumerable<PostcodeioResponseModel>> GetPostcodeDetails(IEnumerable<string> postcodesToLookup)
    {
        var bulkResponseBody = await GetBulkPostcodeApiResponse(postcodesToLookup);
        var postcodeResponse = TurnBulkResponseBodyToModel(bulkResponseBody);
        return postcodeResponse;
    }

    private async Task<string> GetBulkPostcodeApiResponse(IEnumerable<string> postcodes)
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

    private IEnumerable<PostcodeioResponseModel> TurnBulkResponseBodyToModel(string bulkResponseBody)
    {
        if (string.IsNullOrEmpty(bulkResponseBody))
        {
            return new List<PostcodeioResponseModel>();
        }

        var jObjectResponse = JObject.Parse(bulkResponseBody);
        var postcodeList = jObjectResponse["result"]!;
        return postcodeList.Select(postcode => postcode.ToObject<PostcodeioResponseModel>()!);
    }

    private void AddPostcodesToPostcodeDb(IEnumerable<PostcodeioResponseModel> postcodeResponses)
    {
        var postcodeDbModels = postcodeResponses.Select(p => new PostcodeDbModel
        {
            Postcode = p.Result!.Postcode!,
            Location = p.Result.Lon != null && p.Result.Lat != null
                ? new Point((double)p.Result.Lon, (double)p.Result.Lat) { SRID = 4326 }
                : null
        });
        _dbContext.Postcodes.AddRange(postcodeDbModels);
        _dbContext.SaveChanges();
    }
    
    private void UpdatePostcodes(IEnumerable<PostcodeioResponseModel> postcodeResponses)
    {
        foreach (var postcode in postcodeResponses)
        {
            var postcodeDbModel = _dbContext.Postcodes.Single(p => p.Postcode == postcode.Result!.Postcode);
            postcodeDbModel.Location = postcode.Result is { Lon: not null, Lat: not null }
                ? new Point((double)postcode.Result.Lon, (double)postcode.Result.Lat) { SRID = 4326 }
                : null;
        }
        _dbContext.SaveChanges();
    }
}