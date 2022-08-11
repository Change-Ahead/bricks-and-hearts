using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep5 : PropertyInputModelBase
{
    public PropertyInputModelStep5(PropertyDbModel property)
    {
        AcceptsSingleTenant = property.AcceptsSingleTenant;
        AcceptsCouple = property.AcceptsCouple;
        AcceptsFamily = property.AcceptsFamily;
        AcceptsPets = property.AcceptsPets;
        AcceptsBenefits = property.AcceptsBenefits;
        AcceptsNotEET = property.AcceptsNotEET;
        AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor;
    }

    [Required]
    public bool? AcceptsWithoutGuarantor { get; set; }

    [Required]
    public bool? AcceptsNotEET { get; set; }

    [Required]
    public bool? AcceptsBenefits { get; set; }

    [Required]
    public bool? AcceptsPets { get; set; }

    [Required]
    public bool? AcceptsFamily { get; set; }

    [Required]
    public bool? AcceptsCouple { get; set; }

    [Required]
    public bool? AcceptsSingleTenant { get; set; }
}