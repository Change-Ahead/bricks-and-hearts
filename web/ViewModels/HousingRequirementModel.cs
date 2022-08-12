using System.Collections;

namespace BricksAndHearts.ViewModels;

public class HousingRequirementModel : IEnumerable
{
    public bool? AcceptsSingleTenant { get; set; }
    public bool? AcceptsCouple { get; set; }
    public bool? AcceptsFamily { get; set; }
    public bool? AcceptsPets { get; set; }
    public bool? AcceptsNotEET { get; set; }
    public bool? AcceptsCredit { get; set; }
    public bool? AcceptsBenefits { get; set; }
    public bool? AcceptsOver35 { get; set; }
    public bool? AcceptsWithoutGuarantor { get; set; }

    public IEnumerator<bool?> GetEnumerator()
    {
        yield return AcceptsSingleTenant;
        yield return AcceptsCouple;
        yield return AcceptsFamily;
        yield return AcceptsPets;
        yield return AcceptsNotEET;
        yield return AcceptsCredit;
        yield return AcceptsBenefits;
        yield return AcceptsOver35;
        yield return AcceptsWithoutGuarantor;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public List<bool?> GetList()
    {
        var listOfFilters = new List<bool?>();
        foreach (var filter in this)
        {
            listOfFilters.Add(filter);
        }

        return listOfFilters;
    }
}
