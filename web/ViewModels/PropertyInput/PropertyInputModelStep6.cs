using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep6 : PropertyInputModelBase
{
    public PropertyInputModelStep6(PropertyDbModel property)
    {
        Rent = property.Rent;
        Availability = property.Availability;
        AvailableFrom = property.AvailableFrom;
        TotalUnits = property.TotalUnits;
        OccupiedUnits = property.OccupiedUnits;
    }

    public int OccupiedUnits { get; set; }

    public int TotalUnits { get; set; }

    public DateTime? AvailableFrom { get; set; }

    public string Availability { get; set; }

    public int? Rent { get; set; }
}