using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep5 : PropertyInputModelBase
{
    [ValidateNever]
    public HousingRequirementModel HousingRequirementModel { get; set; } = new();

    public override void PropertyInputModelStepInitialiser(PropertyDbModel property)
    {
        HousingRequirementModel.AcceptsSingleTenant = property.AcceptsSingleTenant;
        HousingRequirementModel.AcceptsCouple = property.AcceptsCouple;
        HousingRequirementModel.AcceptsFamily = property.AcceptsFamily;
        HousingRequirementModel.AcceptsPets = property.AcceptsPets;
        HousingRequirementModel.AcceptsBenefits = property.AcceptsBenefits;
        HousingRequirementModel.AcceptsNotEET = property.AcceptsNotEET;
        HousingRequirementModel.AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HousingRequirementModel.AcceptsSingleTenant == false && HousingRequirementModel.AcceptsCouple == false &&
            HousingRequirementModel.AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }
    }
}