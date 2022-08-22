using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace BricksAndHearts.Database;

public class PostcodeDbModel
{
    [Key]
    public string Postcode { get; set; } = null!;
    public Point? Location { get; set; }
}