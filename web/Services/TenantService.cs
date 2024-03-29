﻿using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ITenantService
{
    public TenantCountModel CountTenants();
    public Task<(List<TenantDbModel> TenantList, int Count)> FilterNearestTenantsToProperty(
        HousingRequirementModel filters, bool isMatching, string? postcode, int currentPage, int tenantsPerPage);
    public Task<(List<TenantDbModel> TenantList, int Count)> GetNearestTenantsToProperty(
        PropertyViewModel currentProperty, int tenantsPerPage = 5);
}

public class TenantService : ITenantService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IPostcodeService _postcodeService;

    public TenantService(BricksAndHeartsDbContext dbContext, IPostcodeService postcodeService)
    {
        _dbContext = dbContext;
        _postcodeService = postcodeService;
    }

    public TenantCountModel CountTenants()
    {
        return new TenantCountModel
        {
            RegisteredTenants = _dbContext.Tenants.Count(),
            LocatedTenants = _dbContext.Tenants.Count(t => t.Postcode != null)
        };
    }

    public async Task<(List<TenantDbModel> TenantList, int Count)> FilterNearestTenantsToProperty(
        HousingRequirementModel filters, bool isMatching, string? postcode, int currentPage, int tenantsPerPage)
    {
        var sortedTenantQuery = await SortTenantsByLocationAndFilter(filters,
            isMatching, postcode, currentPage, tenantsPerPage);
        return (await sortedTenantQuery.TenantList.ToListAsync(), sortedTenantQuery.Count);
    }

    public async Task<(List<TenantDbModel> TenantList, int Count)> GetNearestTenantsToProperty(
        PropertyViewModel currentProperty, int tenantsPerPage = 5)
    {
        var sortedTenantQuery = await SortTenantsByLocationAndFilter(currentProperty.LandlordRequirements,
            true, currentProperty.Address.Postcode, 1, tenantsPerPage);
        return (await sortedTenantQuery.TenantList.ToListAsync(), sortedTenantQuery.Count);
    }

    private IQueryable<TenantDbModel> GetFilteredTenantQuery(HousingRequirementModel filters, bool isMatching)
    {
        var tenantQuery = _dbContext.Tenants
            .Include(p => p.Postcode)
            .AsQueryable();
        tenantQuery = tenantQuery.Where(t => (t.Type == "Single" && filters.AcceptsSingleTenant == true)
                                             || (t.Type == "Couple" && filters.AcceptsCouple == true)
                                             || (t.Type == "Family" && filters.AcceptsFamily == true)
                                             || (filters.AcceptsSingleTenant != true
                                                 && filters.AcceptsCouple != true
                                                 && filters.AcceptsFamily != true));
        /*the above are INCLUSIVE filters*/
        if (!isMatching)
        {
            return tenantQuery.Where(t => (filters.AcceptsPets == null || t.HasPet == filters.AcceptsPets)
                                          && (filters.AcceptsNotInEET == null || t.InEET == !filters.AcceptsNotInEET)
                                          && (filters.AcceptsCredit == null
                                              || t.UniversalCredit == filters.AcceptsCredit)
                                          && (filters.AcceptsBenefits == null
                                              || t.HousingBenefits == filters.AcceptsBenefits)
                                          && (filters.AcceptsUnder35 == null || t.Under35 == filters.AcceptsUnder35)
                                          && (filters.AcceptsWithoutGuarantor == null
                                              || t.HasGuarantor == !filters.AcceptsWithoutGuarantor));
            /*Above are EXCLUSIVE filters for the filters page*/
        }

        if (filters.AcceptsPets == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HasPet == false);
        }

        if (filters.AcceptsNotInEET == false)
        {
            tenantQuery = tenantQuery.Where(t => t.InEET == true);
        }

        if (filters.AcceptsCredit == false)
        {
            tenantQuery = tenantQuery.Where(t => t.UniversalCredit == false);
        }

        if (filters.AcceptsBenefits == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HousingBenefits == false);
        }

        if (filters.AcceptsUnder35 == false)
        {
            tenantQuery = tenantQuery.Where(t => t.Under35 == false);
        }

        if (filters.AcceptsWithoutGuarantor == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HasGuarantor == true);
        }

        return tenantQuery;
        /*Above are INCLUSIVE filters for the matching page*/
    }

    private async Task<(IQueryable<TenantDbModel> TenantList, int Count)> SortTenantsByLocationAndFilter(
        HousingRequirementModel filters, bool isMatching, string? postalCode, int page, int tenantsPerPage)
    {
        var tenantQuery = GetFilteredTenantQuery(filters, isMatching);
        if (postalCode == null)
        {
            return (tenantQuery.Skip(tenantsPerPage * (page - 1))
                .Take(tenantsPerPage), tenantQuery.Count());
        }

        var postcode = _postcodeService.FormatPostcode(postalCode);
        var postcodeList = new List<string> { postcode };
        await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodeList);
        var targetLocation = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
        if (targetLocation?.Location == null)
        {
            return (tenantQuery.Skip(tenantsPerPage * (page - 1))
                .Take(tenantsPerPage), 0);
        }

        var tenants = tenantQuery
            .Include(t => t.Postcode)
            .Where(t => t.Postcode != null)
            .OrderBy(p => p.Postcode!.Location!.Distance(targetLocation.Location))
            .Skip(tenantsPerPage * (page - 1))
            .Take(tenantsPerPage);
        return (tenants, tenantQuery.Count());
    }
}