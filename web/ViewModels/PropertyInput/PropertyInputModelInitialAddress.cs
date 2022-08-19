using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelInitialAddress : PropertyInputModelBase
{
    [Required]
    public string? AddressLine1 { get; set; }

    [Required]
    public string? Postcode { get; set; }

    [ValidateNever]
    public override string PreviousAction { get; set; } = "";

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        Title = "Address";
        AddressLine1 = property.AddressLine1;
        Postcode = property.Postcode;
    }

    public override PropertyViewModel FormToViewModel()
    {
        Postcode =
            Regex.Replace(Postcode!, Constants.PostcodeFormatRegex, "$1 $2").ToUpper();
        return new PropertyViewModel
        {
            Address = new AddressModel
            {
                AddressLine1 = AddressLine1,
                Postcode = Postcode
            }
        };
    }
}