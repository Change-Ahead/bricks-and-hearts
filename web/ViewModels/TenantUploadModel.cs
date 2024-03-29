﻿namespace BricksAndHearts.ViewModels;

public class TenantUploadModel
{
    public string? Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Postcode { get; set; }

    public string? Type { get; set; }
    public string? HasPet { get; set; }
    public string? InEET { get; set; }
    public string? UniversalCredit { get; set; }
    public string? HousingBenefits { get; set; }
    public string? Under35 { get; set; }
    public string? HasGuarantor { get; set; }
}