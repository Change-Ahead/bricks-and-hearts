using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BricksAndHearts.ViewModels;

public class AddressModel
{
    [StringLength(10000)]
    public string? AddressLine1 { get; set; }

    [StringLength(10000)]
    public string? AddressLine2 { get; set; }

    [StringLength(10000)]
    public string? AddressLine3 { get; set; }

    [StringLength(10000)]
    public string? TownOrCity { get; set; }

    [StringLength(10000)]
    public string? County { get; set; }

    [RegularExpression(
        @"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})",
        ErrorMessage = "Please enter a valid postcode")]
    public string? Postcode { get; set; }

    public string ToShortAddressString()
    {
        var address = new StringBuilder(AddressLine1);
        if (AddressLine2 != null)
        {
            address.Append($", {AddressLine2}");
        }

        if (AddressLine3 != null)
        {
            address.Append($", {AddressLine3}");
        }

        if (TownOrCity != null)
        {
            address.Append($", {TownOrCity}");
        }

        return address.ToString();
    }
}