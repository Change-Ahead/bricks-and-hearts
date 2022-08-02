using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class LandlordProfileModel
{
    [DisplayName("Id")]
    public int LandlordId { get; set; }
    
    [Required]
    [StringLength(60)]
    [DisplayName("Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [DisplayName("First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [DisplayName("Last Name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(1000)]
    [DisplayName("Company")]
    public string? CompanyName { get; set; }

    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Phone] public string Phone { get; set; } = string.Empty;

    public string LandlordStatus { get; set; } = string.Empty;

    public bool LandlordProvidedCharterStatus { get; set; } = false;

    public bool CharterApproved { get; set; } = false;

    public bool CurrentUserIsAdmin { get; set; }

    public bool Unassigned { get; set; }

    public static LandlordProfileModel FromDbModel(LandlordDbModel landlord, BricksAndHeartsUser user)
    {
        return new LandlordProfileModel
        {
            LandlordId = landlord.Id,
            CompanyName = landlord.CompanyName,
            Email = landlord.Email,
            FirstName = landlord.FirstName,
            LastName = landlord.LastName,
            Phone = landlord.Phone,
            Title = landlord.Title,
            LandlordStatus = landlord.LandlordStatus,
            LandlordProvidedCharterStatus = landlord.LandlordProvidedCharterStatus,
            CharterApproved = landlord.CharterApproved,
            CurrentUserIsAdmin = user.IsAdmin
        };
    }
}