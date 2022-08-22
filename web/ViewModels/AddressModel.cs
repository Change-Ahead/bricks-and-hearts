using System.ComponentModel.DataAnnotations;

namespace BricksAndHearts.ViewModels;

public class AddressModel
{
    [Required]
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

    [Required]
    [RegularExpression(
        Constants.PostcodeValidationRegex,
        ErrorMessage = "Please enter a valid postcode")]
    public string? Postcode { get; set; }

    public string ToShortAddressString()
    {
        var address = AddressLine1;
        address += AddressLine2 != null ? $", {AddressLine2}" : "";
        address += AddressLine3 != null ? $", {AddressLine3}" : "";
        address += TownOrCity != null ? $", {TownOrCity}" : "";

        return address;
    }
}