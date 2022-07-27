using Newtonsoft.Json;

namespace BricksAndHearts.Services;

public class PostcodeApiResponseModel
{
    [JsonProperty("results")]
    public List<Results>? ListOfResults { get; set; }
}

public class Results
{
    [JsonProperty("address")] 
    public Dictionary<string, string>? Address { get; set; }
    [JsonProperty("position")] 
    public Dictionary<string,string>? LatLon { get; set; }
}