namespace BricksAndHearts.Database;

public class PropertyDbModel
{
    public int Id { get; set; }
    public virtual LandlordDbModel Landlord { get; set; }
    public string Address { get; set; }
}