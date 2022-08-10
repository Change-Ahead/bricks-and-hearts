﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class LandlordProfileModel
{
    [DisplayName("ID")]
    public int? LandlordId { get; set; }

    [Required]
    [StringLength(60)]
    [DisplayName("Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [DisplayName("First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [DisplayName("Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public AddressModel Address { get; set; } = new();

    [StringLength(1000)]
    [DisplayName("Company")]
    public string? CompanyName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [DisplayName("Type of landlord")]
    public string LandlordType { get; set; } = string.Empty;

    public string? MembershipId { get; set; }

    public bool CharterApproved { get; set; }

    public bool Unassigned { get; set; }

    public string? InviteLink { get; set; }

    [DisplayName("For / not for profit")]
    public bool IsLandlordForProfit { get; set; }

    public int NumOfProperties { get; set; }

    public static LandlordProfileModel FromDbModel(LandlordDbModel landlord)
    {
        return new LandlordProfileModel
        {
            LandlordId = landlord.Id,

            Title = landlord.Title,
            FirstName = landlord.FirstName,
            LastName = landlord.LastName,
            Email = landlord.Email,
            Phone = landlord.Phone,
            CompanyName = landlord.CompanyName,
            LandlordType = landlord.LandlordType,
            MembershipId = landlord.MembershipId,
            CharterApproved = landlord.CharterApproved,
            IsLandlordForProfit = landlord.IsLandlordForProfit,
            NumOfProperties = landlord.Properties.Count,
            InviteLink = landlord.InviteLink,
            Address = new AddressModel
            {
                AddressLine1 = landlord.AddressLine1,
                AddressLine2 = landlord.AddressLine2,
                AddressLine3 = landlord.AddressLine3,
                TownOrCity = landlord.TownOrCity,
                County = landlord.County,
                Postcode = landlord.Postcode
            }
        };
    }
}