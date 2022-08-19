﻿using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelAvailability : PropertyInputModelBase
{
    public int OccupiedUnits { get; set; }

    [Required]
    public int TotalUnits { get; set; }

    public DateTime? AvailableFrom { get; set; }

    [Required]
    public string? Availability { get; set; }

    [Required]
    public int? Rent { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        var title = "Rent, Deposits, Availability, and Duration";
        base.InitialiseViewModel(property);
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Availability == AvailabilityState.AvailableSoon && AvailableFrom == null)
        {
            yield return new ValidationResult("Available From must be provided if property is Available Soon");
        }

        if (OccupiedUnits > TotalUnits)
        {
            yield return new ValidationResult(
                "The number of occupied units must be less than or equal to the total units at the property.");
        }
    }
}