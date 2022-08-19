using Newtonsoft.Json;

namespace BricksAndHearts.Models;

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
    public double? Lat { get; set; }
    [JsonProperty("longitude")] 
    public double? Lon { get; set; }
}