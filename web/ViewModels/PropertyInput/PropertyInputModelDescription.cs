using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelDescription : PropertyInputModelBase
{
    [Required]
    public string? Description { get; set; }

    [ValidateNever]
    public override string PreviousAction { get; set; } = "PropertyInputStepThreeDetails";

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Step = 4;
        Title = "Property description";
        Description = property.Description;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            Description = Description
        };
    }
}