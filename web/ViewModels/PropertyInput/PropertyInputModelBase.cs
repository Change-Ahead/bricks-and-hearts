using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public abstract class PropertyInputModelBase
{
    [ValidateNever]
    public virtual string Title { get; set; }

    public static readonly int MaximumStep = 6;
    public int LandlordId { get; set; }
    public int PropertyId { get; set; }
    public bool IsEdit { get; set; }
    public int Step { get; set; }

    public virtual void InitialiseViewModel(PropertyDbModel property)
    {
        PropertyId = property.Id;
        LandlordId = property.LandlordId;
    }

    public abstract PropertyViewModel FormToViewModel();
}