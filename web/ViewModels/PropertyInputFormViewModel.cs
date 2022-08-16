using BricksAndHearts.Database;
using BricksAndHearts.ViewModels.PropertyInput;

namespace BricksAndHearts.ViewModels;

public class PropertyInputFormViewModel
{
    public static readonly int MaximumStep = 6;
    public int Step { get; set; }
    public bool IsEdit { get; set; }
    public int PropertyId { get; set; }
    public int LandlordId { get; set; }

    public PropertyInputModelStep1? Step1 { get; set; }
    public PropertyInputModelStep2? Step2 { get; set; }
    public PropertyInputModelStep3? Step3 { get; set; }
    public PropertyInputModelStep4? Step4 { get; set; }
    public PropertyInputModelStep5? Step5 { get; set; }
    public PropertyInputModelStep6? Step6 { get; set; }

    public void InitialiseViewModel(int step, PropertyDbModel property)
    {
        Step = step;
        PropertyId = property.Id;
        LandlordId = property.LandlordId;

        switch (step) //Only create models *up to* the named step
        {
            case 6:
                Step6 = new PropertyInputModelStep6();
                Step6.PropertyInputModelStepInitialiser(property);
                goto case 5;
            case 5:
                Step5 = new PropertyInputModelStep5();
                Step5.PropertyInputModelStepInitialiser(property);
                goto case 4;
            case 4:
                Step4 = new PropertyInputModelStep4();
                Step4.PropertyInputModelStepInitialiser(property);
                goto case 3;
            case 3:
                Step3 = new PropertyInputModelStep3();
                Step3.PropertyInputModelStepInitialiser(property);
                goto case 2;
            case 2:
                Step2 = new PropertyInputModelStep2();
                Step2.PropertyInputModelStepInitialiser(property);
                goto case 1;
            case 1:
                Step1 = new PropertyInputModelStep1();
                Step1.PropertyInputModelStepInitialiser(property);
                break;
        }
    }

    public PropertyViewModel FormToViewModel()
    {
        var property = new PropertyViewModel();
        property.PropertyId = PropertyId;
        property.LandlordId = LandlordId;

        switch (Step)
        {
            case 6:
                property.Availability = Step6!.Availability;
                property.Rent = Step6.Rent;
                property.AvailableFrom = Step6.AvailableFrom;
                property.TotalUnits = Step6.TotalUnits;
                property.OccupiedUnits = Step6.OccupiedUnits;
                break;
            case 5:
                property.LandlordRequirements.AcceptsBenefits =
                    Step5!.HousingRequirementModel.AcceptsBenefits;
                property.LandlordRequirements.AcceptsCouple =
                    Step5.HousingRequirementModel.AcceptsCouple;
                property.LandlordRequirements.AcceptsFamily =
                    Step5.HousingRequirementModel.AcceptsFamily;
                property.LandlordRequirements.AcceptsPets =
                    Step5.HousingRequirementModel.AcceptsPets;
                property.LandlordRequirements.AcceptsSingleTenant =
                    Step5.HousingRequirementModel.AcceptsSingleTenant;
                property.LandlordRequirements.AcceptsWithoutGuarantor =
                    Step5.HousingRequirementModel.AcceptsWithoutGuarantor;
                property.LandlordRequirements.AcceptsNotEET =
                    Step5.HousingRequirementModel.AcceptsNotEET;
                break;
            case 4:
                property.Description = Step4!.Description;
                break;
            case 3:
                property.PropertyType = Step3!.PropertyType;
                property.NumOfBedrooms = Step3.NumOfBedrooms;
                break;
            case 2:
                property.Address = Step2!.Address!;
                break;
            case 1:
                property.Address = Step1!.Address!;
                break;
        }

        return property;
    }
}