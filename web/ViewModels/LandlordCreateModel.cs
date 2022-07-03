using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BricksAndHearts.ViewModels;

public class LandlordCreateModel
{
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

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;
}