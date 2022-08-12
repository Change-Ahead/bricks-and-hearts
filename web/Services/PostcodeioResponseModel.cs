using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public class PostcodeioResponseModel
{
    [JsonProperty("result")]
    public PostcodeResult? Result { get; set; }
}

public class PostcodeResult
{
    [JsonProperty("postcode")] 
    public string? Postcode { get; set; }
    [JsonProperty("latitude")] 
    public decimal? Lat { get; set; }
    [JsonProperty("longitude")] 
    public decimal? Lon { get; set; }
}