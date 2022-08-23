using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelTenantPreferences : PropertyInputModelBase, IValidatableObject
{
    [Required]
    public bool? AcceptsSingleTenant { get; set; }

    [Required]
    public bool? AcceptsCouple { get; set; }

    [Required]
    public bool? AcceptsFamily { get; set; }

    [Required]
    public bool? AcceptsPets { get; set; }

    [Required]
    public bool? AcceptsNotInEET { get; set; }

    [Required]
    public bool? AcceptsCredit { get; set; }

    [Required]
    public bool? AcceptsBenefits { get; set; }

    [Required]
    public bool? AcceptsUnder35 { get; set; }

    [Required]
    public bool? AcceptsWithoutGuarantor { get; set; }

    [ValidateNever]
    public override string PreviousAction { get; set; } = "PropertyInputStepFourDescription";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcceptsSingleTenant == false && AcceptsCouple == false && AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }
    }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Step = 5;
        Title = "What kind of tenant would you prefer?";
        AcceptsSingleTenant = property.AcceptsSingleTenant;
        AcceptsCouple = property.AcceptsCouple;
        AcceptsFamily = property.AcceptsFamily;
        AcceptsPets = property.AcceptsPets;
        AcceptsBenefits = property.AcceptsBenefits;
        AcceptsNotInEET = property.AcceptsNotInEET;
        AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor;
        AcceptsCredit = property.AcceptsCredit;
        AcceptsUnder35 = property.AcceptsUnder35;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            LandlordRequirements = new HousingRequirementModel
            {
                AcceptsSingleTenant = AcceptsSingleTenant,
                AcceptsCouple = AcceptsCouple,
                AcceptsFamily = AcceptsFamily,
                AcceptsPets = AcceptsPets,
                AcceptsBenefits = AcceptsBenefits,
                AcceptsNotInEET = AcceptsNotInEET,
                AcceptsWithoutGuarantor = AcceptsWithoutGuarantor,
                AcceptsCredit = AcceptsCredit,
                AcceptsUnder35 = AcceptsUnder35
            }
        };
    }
}