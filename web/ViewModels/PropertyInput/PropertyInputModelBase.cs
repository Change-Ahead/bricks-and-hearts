using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public abstract class PropertyInputModelBase
{
    public static readonly int MaximumStep = 6;

    [ValidateNever]
    public virtual string Title { get; set; }

    public int LandlordId { get; set; }
    public int PropertyId { get; set; }
    public bool IsEdit { get; set; }
    public int Step { get; set; }

    public abstract string PreviousAction { get; set; }

    public virtual void InitialiseViewModel(PropertyDbModel property)
    {
        PropertyId = property.Id;
        LandlordId = property.LandlordId;
    }

    public abstract PropertyViewModel FormToViewModel();
}