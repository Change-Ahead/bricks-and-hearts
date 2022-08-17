﻿using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using BricksAndHearts.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BricksAndHearts.Services;

public interface IPostcodeService
{
    public string FormatPostcode(string postcode);
    public Task AddPostcodesToDatabaseIfAbsent(List<string> postcodes);
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
        const string postcodePattern = @"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})";
        return Regex.IsMatch(postcode, postcodePattern)
            ? Regex.Replace(postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2").ToUpper()
            : "";
    }

    public async Task AddPostcodesToDatabaseIfAbsent(List<string> postcodes)
    {
        var postcodeResponseList = new List<PostcodeioResponseModel>();
        var postcodesToLookupChunks = postcodes.Distinct()
            .Where(p => p != "" && _dbContext.Postcodes.All(dbRecord => dbRecord.Postcode != p))
            .Chunk(100);

        var addPostcodeChunkToResponseList = postcodesToLookupChunks.Select(async p => postcodeResponseList.AddRange(await BulkGetLatLonForPostcodes(p.ToList())));
        await Task.WhenAll(addPostcodeChunkToResponseList);

        foreach (var response in postcodeResponseList)
        {
            AddPostcodeToPostcodeDb(response);
        }
    }

    private async Task<List<PostcodeioResponseModel>> BulkGetLatLonForPostcodes(List<string> postcodesToLookup)
    {
        var bulkResponseBody = await MakeBulkApiRequestToPostcodeApi(postcodesToLookup);
        var postcodeResponse = TurnBulkResponseBodyToModel(bulkResponseBody);
        return postcodeResponse;
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
        var postcodeResponseList = new List<PostcodeioResponseModel>();
        if (string.IsNullOrEmpty(bulkResponseBody))
        {
            return postcodeResponseList;
        }
        var jObjectResponse = JObject.Parse(bulkResponseBody);
        var postcodeList = jObjectResponse["result"]!.ToList();
        foreach (var postcode in postcodeList)
        {
            var postcodeDbModel = postcode.ToObject<PostcodeioResponseModel>();
            postcodeResponseList.Add(postcodeDbModel!);
        }
        return postcodeResponseList;
    }
    
    private void AddPostcodeToPostcodeDb(PostcodeioResponseModel postcodeResponse)
    {
        if (postcodeResponse.Result?.Lat == null || postcodeResponse.Result.Lon == null || postcodeResponse.Result.Postcode == null)
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
}
