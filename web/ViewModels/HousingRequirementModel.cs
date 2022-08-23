namespace BricksAndHearts.ViewModels;

public class HousingRequirementModel
{
    public bool? AcceptsSingleTenant { get; set; }
    public bool? AcceptsCouple { get; set; }
    public bool? AcceptsFamily { get; set; }
    public bool? AcceptsPets { get; set; }
    public bool? AcceptsNotInEET { get; set; }
    public bool? AcceptsCredit { get; set; }
    public bool? AcceptsBenefits { get; set; }
    public bool? AcceptsUnder35 { get; set; }
    public bool? AcceptsWithoutGuarantor { get; set; }

    public bool AllAreNull => AcceptsSingleTenant == null
                              && AcceptsCouple == null
                              && AcceptsFamily == null
                              && AcceptsPets == null
                              && AcceptsNotInEET == null
                              && AcceptsCredit == null
                              && AcceptsBenefits == null
                              && AcceptsUnder35 == null
                              && AcceptsWithoutGuarantor == null;
}