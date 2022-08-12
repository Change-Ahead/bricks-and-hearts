using System.ComponentModel.DataAnnotations;

namespace BricksAndHearts.Database;

public class PostcodeDbModel
{
    [Key]
    public string Postcode { get; set; } = null!;
    public decimal? Lat { get; set; }
    public decimal? Lon { get; set; }
}