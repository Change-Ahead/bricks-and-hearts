using BricksAndHearts.Database;
using BricksAndHearts.ViewModels.PropertyInput;

namespace BricksAndHearts.ViewModels;

public class PropertyInputFormViewModel
{
    public static readonly int MaximumStep = 6;

    public PropertyInputFormViewModel(int step, PropertyDbModel property)
    {
        Step = step;
        PropertyId = property.Id;
        LandlordId = property.LandlordId;

        switch (step) //Only create models *up to* the named step
        {
            case 6:
                Step6 = new PropertyInputModelStep6(property);
                goto case 5;
            case 5:
                Step5 = new PropertyInputModelStep5(property);
                goto case 4;
            case 4:
                Step4 = new PropertyInputModelStep4(property);
                goto case 3;
            case 3:
                Step3 = new PropertyInputModelStep3(property);
                goto case 2;
            case 2:
                Step2 = new PropertyInputModelStep2(property);
                goto case 1;
            case 1:
                Step1 = new PropertyInputModelStep1(property);
                break;
        }
    }

    public int Step { get; set; }

    public int PropertyId { get; set; }
    public int LandlordId { get; set; }

    public PropertyInputModelStep1? Step1 { get; set; }
    public PropertyInputModelStep2? Step2 { get; set; }
    public PropertyInputModelStep3? Step3 { get; set; }
    public PropertyInputModelStep4? Step4 { get; set; }
    public PropertyInputModelStep5? Step5 { get; set; }
    public PropertyInputModelStep6? Step6 { get; set; }
}