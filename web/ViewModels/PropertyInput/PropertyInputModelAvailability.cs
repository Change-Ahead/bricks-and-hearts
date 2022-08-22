using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelAvailability : PropertyInputModelBase, IValidatableObject
{
    public int OccupiedUnits { get; set; }

    [Required]
    public int TotalUnits { get; set; }

    public DateTime? AvailableFrom { get; set; }

    [Required]
    public string? Availability { get; set; }

    [Required]
    public int? Rent { get; set; }

    [ValidateNever]
    public override string PreviousAction { get; set; } = "PropertyInputStepFiveTenantPreferences";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Availability == AvailabilityState.AvailableSoon)
        {
            if (AvailableFrom == null)
            {
                yield return new ValidationResult("Available From must be provided if property is Available Soon");
            }

            if (AvailableFrom < DateTime.Today)
            {
                yield return new ValidationResult("Available From must be in the future");
            }
        }


        if (OccupiedUnits > TotalUnits)
        {
            yield return new ValidationResult(
                "The number of occupied units must be less than or equal to the total units at the property.");
        }
    }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Step = 6;
        Title = "Rent, Deposits, Availability, and Duration";
        Rent = property.Rent;
        Availability = property.Availability;
        AvailableFrom = property.AvailableFrom;
        TotalUnits = property.TotalUnits;
        OccupiedUnits = property.OccupiedUnits;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            Availability = Availability,
            Rent = Rent,
            AvailableFrom = AvailableFrom,
            TotalUnits = TotalUnits,
            OccupiedUnits = OccupiedUnits
        };
    }
}