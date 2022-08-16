using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep1 : PropertyInputModelBase
{
    [Required]
    public AddressModel? Address { get; set; }

    public override void PropertyInputModelStepInitialiser(PropertyDbModel property)
    {
        Address = new AddressModel
        {
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            AddressLine3 = property.AddressLine3,
            TownOrCity = property.TownOrCity,
            County = property.County,
            Postcode = property.Postcode
        };
    }
}