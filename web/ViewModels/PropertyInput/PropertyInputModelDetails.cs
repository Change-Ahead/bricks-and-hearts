using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelDetails : PropertyInputModelBase
{
    [Required]
    public int? NumOfBedrooms { get; set; }

    [Required]
    public string? PropertyType { get; set; }

    [ValidateNever]
    public override string PreviousAction { get; set; } = "PropertyInputStepTwoAddress";

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Step = 3;
        Title = "Property Details";
        PropertyType = property.PropertyType;
        NumOfBedrooms = property.NumOfBedrooms;
        LandlordId = property.LandlordId;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            PropertyType = PropertyType,
            NumOfBedrooms = NumOfBedrooms
        };
    }
}