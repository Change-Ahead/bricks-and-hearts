using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelTenantPreferences : PropertyInputModelBase
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
    public bool? AcceptsNotEET { get; set; }

    [Required]
    public bool? AcceptsCredit { get; set; }

    [Required]
    public bool? AcceptsBenefits { get; set; }

    [Required]
    public bool? AcceptsOver35 { get; set; }

    [Required]
    public bool? AcceptsWithoutGuarantor { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Title = "What kind of tenant would you prefer?";
        AcceptsSingleTenant = property.AcceptsSingleTenant;
        AcceptsCouple = property.AcceptsCouple;
        AcceptsFamily = property.AcceptsFamily;
        AcceptsPets = property.AcceptsPets;
        AcceptsBenefits = property.AcceptsBenefits;
        AcceptsNotEET = property.AcceptsNotEET;
        AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor;
        AcceptsCredit = property.AcceptsCredit;
        AcceptsOver35 = property.AcceptsOver35;
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
                AcceptsNotEET = AcceptsNotEET,
                AcceptsWithoutGuarantor = AcceptsWithoutGuarantor,
                AcceptsCredit = AcceptsCredit,
                AcceptsOver35 = AcceptsOver35
            }
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcceptsSingleTenant == false && AcceptsCouple == false &&
            AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }
    }
}