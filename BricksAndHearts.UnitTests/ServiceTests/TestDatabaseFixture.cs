using System;
using System.Collections.Generic;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;

namespace BricksAndHearts.UnitTests.ServiceTests;

public class TestDatabaseFixture
{
    private static readonly object Lock = new();
    private static bool _databaseInitialised;

    public static readonly Dictionary<string, PostcodeDbModel> Postcodes = new()
    {
        { "CB1 1DX", AddPostcode("CB1 1DX") },
        { "BN1 1AJ", AddPostcode("BN1 1AJ", -0.14256, 50.821451) },
        { "CB2 1LA", AddPostcode("CB2 1LA", 0.129235, 52.196849) },
        { "NW5 1TL", AddPostcode("NW5 1TL", -0.144754, 51.553935) },
        { "PE1 1BF", AddPostcode("PE1 1BF", -0.242008, 52.571459) },
        { "LS1 1AZ", AddPostcode("LS1 1AZ", -1.564095, 53.796296) },
        { "SE1 9BG", AddPostcode("SE1 9BG", -0.087584, 51.506543) }
    };

    public TestDatabaseFixture()
    {
        lock (Lock)
        {
            if (_databaseInitialised)
            {
                return;
            }

            using (var context = CreateContext(false))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.Postcodes.AddRange(
                    Postcodes.Values
                );

                context.Landlords.AddRange(
                    CreateApprovedLandlord(), // landlordId = 1
                    CreateUnapprovedLandlord(), // landlordId = 2
                    CreateLandlordWithLink(), // landlordId = 3
                    CreateUnlinkedLandlordWithLink(), // landlordId = 4
                    CreateLandlordWithMembershipId(5), // landlordId = 5
                    CreateLandlordWithMembershipId(6) // landlordId = 6
                );

                context.Users.AddRange(
                    CreateAdminUser(),
                    CreateNonAdminUser(),
                    CreateRequestedAdminUser(),
                    // Add in landlords
                    CreateApprovedLandlordUser(), // landlordId = 1
                    CreateUnapprovedLandlordUser(), // landlordId = 2
                    CreateLandlordUserWithLink(), // landlordId = 3
                    CreateUnlinkedLandlordUser()
                );

                context.Properties.AddRange(
                    CreateCompleteProperty(1),
                    CreateCompleteProperty(1),
                    CreateCompleteProperty(2),
                    CreateIncompleteProperty(2),
                    CreateBrightonProperty(3),
                    CreateLondonProperty(3),
                    CreatePeterboroughProperty(3),
                    CreateLeedsProperty(3)
                );
                context.Properties.AddRange(
                    CreateCompleteProperty(),
                    CreateIncompleteProperty(),
                    CreateAvailableProperty(),
                    CreateDraftProperty(),
                    CreateMultiUnitProperty(),
                    CreateAvailableSoonProperty()
                );

                context.Tenants.AddRange(
                    CreateTenant(),
                    CreateFalseTenant()
                );

                context.SaveChanges();
            }

            _databaseInitialised = true;
        }
    }

    private BricksAndHeartsDbContext CreateContext(bool readOnly)
    {
        var config = new ConfigurationManager();
        config.AddJsonFile("appsettings.json");
        return new TestDbContext(config, readOnly);
    }

    public BricksAndHeartsDbContext CreateReadContext()
    {
        return CreateContext(true);
    }

    public BricksAndHeartsDbContext CreateWriteContext()
    {
        var context = CreateContext(false);
        // Begin a transaction so the test's writes don't interfere with other tests running in parallel (provides test isolation)
        // Transaction is never committed so is automatically rolled back at the end of the test
        context.Database.BeginTransaction();
        return context;
    }

    // Begin User Models

    public UserDbModel CreateAdminUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "AdminUser",
            GoogleEmail = "test.email@gmail.com",
            GoogleAccountId = "1",
            IsAdmin = true,
            LandlordId = null,
            HasRequestedAdmin = false
        };
    }

    private static UserDbModel CreateNonAdminUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "NonAdminUser",
            GoogleEmail = "test.email2@gmail.com",
            GoogleAccountId = "2",
            IsAdmin = false,
            LandlordId = null,
            HasRequestedAdmin = false
        };
    }

    private static UserDbModel CreateRequestedAdminUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "HasRequestedAdminUser",
            GoogleEmail = "test.email3@gmail.com",
            GoogleAccountId = "3",
            IsAdmin = false,
            LandlordId = null,
            HasRequestedAdmin = true
        };
    }

    public UserDbModel CreateApprovedLandlordUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "ApprovedLandlordUser",
            GoogleEmail = "test.email4@gmail.com",
            GoogleAccountId = "4",
            IsAdmin = false,
            LandlordId = 1
        };
    }

    private static UserDbModel CreateUnapprovedLandlordUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "UnapprovedLandlordUser",
            GoogleEmail = "test.email5@gmail.com",
            GoogleAccountId = "5",
            IsAdmin = false,
            LandlordId = 2
        };
    }

    private static UserDbModel CreateLandlordUserWithLink()
    {
        return new UserDbModel
        {
            GoogleUserName = "LinkedLandlordUser",
            GoogleEmail = "test.email6@gmail.com",
            GoogleAccountId = "6",
            IsAdmin = false,
            LandlordId = 3
        };
    }

    private static UserDbModel CreateUnlinkedLandlordUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "UnlinkedLandlordUser",
            GoogleEmail = "test.email7@gmail.com",
            GoogleAccountId = "7",
            IsAdmin = false,
            HasRequestedAdmin = false
        };
    }

    // Please if you want to add in a new user with landlord ID
    // DO NOT use landlordId = 4
    // Use landlordId = 5 instead
    // PLEASE!

    // Begin Landlord models
    private static LandlordDbModel CreateApprovedLandlord()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord1@gmail.com",
            FirstName = "Landlord1Approved",
            LastName = "Landlord1Sur",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordType = "Non profit",
            CharterApproved = true,
            MembershipId = "member1",
            AddressLine1 = "adr1",
            AddressLine2 = "adr2",
            AddressLine3 = "adr3",
            TownOrCity = "city",
            County = "county",
            Postcode = "cb2 1la"
        };
    }

    private static LandlordDbModel CreateUnapprovedLandlord()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord2@gmail.com",
            FirstName = "Landlord2Unapproved",
            LastName = "Landlord2Sur",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordType = "Non profit",
            CharterApproved = false,
            AddressLine1 = "adr1",
            AddressLine2 = "adr2",
            AddressLine3 = "adr3",
            TownOrCity = "city",
            County = "county",
            Postcode = "cb2 1la"
        };
    }

    private static LandlordDbModel CreateLandlordWithLink()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord3@gmail.com",
            FirstName = "Landlord3Link",
            LastName = "Landlord3Sur",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordType = "Non profit",
            CharterApproved = true,
            InviteLink = "InvitimusLinkimus",
            AddressLine1 = "adr1",
            AddressLine2 = "adr2",
            AddressLine3 = "adr3",
            TownOrCity = "city",
            County = "county",
            Postcode = "cb2 1la"
        };
    }

    private static LandlordDbModel CreateUnlinkedLandlordWithLink()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord4@gmail.com",
            FirstName = "Landlord4Unlinked",
            LastName = "Landlord4Sur",
            Title = "Mr",
            Phone = "004",
            LandlordType = "Non profit",
            CharterApproved = true,
            InviteLink = "invite-unlinked-landlord",
            AddressLine1 = "adr1",
            AddressLine2 = "adr2",
            AddressLine3 = "adr3",
            TownOrCity = "city",
            County = "county",
            Postcode = "cb2 1la"
        };
    }

    private static LandlordDbModel CreateLandlordWithMembershipId(int memberId)
    {
        return new LandlordDbModel
        {
            Email = "test.landlord5&6@gmail.com",
            FirstName = "Landlord5&6MembershipId",
            LastName = "Landlord5&6Sur",
            Title = "Mr",
            Phone = "005&6",
            LandlordType = "Non profit",
            CharterApproved = true,
            MembershipId = $"Member-{memberId}",
            AddressLine1 = "adr1",
            AddressLine2 = "adr2",
            AddressLine3 = "adr3",
            TownOrCity = "city",
            County = "county",
            Postcode = "cb2 1la"
        };
    }

    public LandlordProfileModel CreateLandlordProfileWithEditedEmail(string email)
    {
        return new LandlordProfileModel
        {
            LandlordId = 3,
            Email = email,
            FirstName = "Landlord3Link",
            LastName = "Landlord3Sur",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordType = "Non profit",
            CharterApproved = true,
            InviteLink = "InvitimusLinkimus",
            Address = new AddressModel
            {
                AddressLine1 = "adr1",
                AddressLine2 = "adr2",
                AddressLine3 = "adr3",
                TownOrCity = "city",
                County = "county",
                Postcode = "cb2 1la"
            }
        };
    }

    public LandlordProfileModel CreateLandlordProfileWithEditedMemberId(int memberId)
    {
        return new LandlordProfileModel
        {
            LandlordId = 5,
            Email = "test.landlord5&6@gmail.com",
            FirstName = "Landlord3Link",
            LastName = "Landlord3Sur",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordType = "Non profit",
            CharterApproved = true,
            MembershipId = $"Member-{memberId}",
            InviteLink = "InvitimusLinkimus",
            Address = new AddressModel
            {
                AddressLine1 = "adr1",
                AddressLine2 = "adr2",
                AddressLine3 = "adr3",
                TownOrCity = "city",
                County = "county",
                Postcode = "cb2 1la"
            }
        };
    }

    private PropertyDbModel CreateCompleteProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = false,
            AddressLine1 = "Complete Property",
            AddressLine2 = "Complete Street",
            TownOrCity = "Complete Town",
            County = "Complete County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.Occupied,
            TotalUnits = 1,
            OccupiedUnits = 1
        };
    }

    private PropertyDbModel CreateIncompleteProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = true,
            AddressLine1 = "Incomplete Property",
            AddressLine2 = "Incomplete Street",
            TownOrCity = "Incomplete Town",
            County = "Incomplete County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.Draft
        };
    }

    private PropertyDbModel CreateAvailableProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = true,
            AddressLine1 = "Available Property",
            AddressLine2 = "Available Street",
            TownOrCity = "Available Town",
            County = "Available County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.Available
        };
    }

    private PropertyDbModel CreateAvailableSoonProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = true,
            AddressLine1 = "AvailableSoon Property",
            AddressLine2 = "Available Street",
            TownOrCity = "Available Town",
            County = "Available County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.AvailableSoon,
            AvailableFrom = DateTime.MinValue
        };
    }

    private PropertyDbModel CreateMultiUnitProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = true,
            AddressLine1 = "MultiUnit Property",
            AddressLine2 = "Available Street",
            TownOrCity = "Available Town",
            County = "Available County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.Available,
            TotalUnits = 5,
            OccupiedUnits = 0
        };
    }

    private PropertyDbModel CreateDraftProperty()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            IsIncomplete = true,
            AddressLine1 = "Draft Property",
            AddressLine2 = "Draft Street",
            TownOrCity = "Draft Town",
            County = "Draft County",
            Postcode = Postcodes["CB2 1LA"],
            Availability = AvailabilityState.Draft
        };
    }

    private PropertyDbModel CreateIncompleteProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            LandlordId = landlordId,
            AddressLine1 = "22 Test Road",
            County = "Cambridgeshire",
            TownOrCity = "Cambridge",
            Postcode = Postcodes["CB1 1DX"],
            IsIncomplete = true
        };
    }

    private PropertyDbModel CreateBrightonProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            LandlordId = landlordId,
            AddressLine1 = "Brighton Property",
            County = "East Sussex",
            TownOrCity = "Brighton",
            Postcode = Postcodes["BN1 1AJ"],
            IsIncomplete = true,
        };
    }

    private PropertyDbModel CreateLondonProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            LandlordId = landlordId,
            AddressLine1 = "London Property",
            County = "Greater London",
            TownOrCity = "London",
            Postcode = Postcodes["SE1 9BG"],
            IsIncomplete = true
        };
    }

    private PropertyDbModel CreatePeterboroughProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            LandlordId = landlordId,
            AddressLine1 = "Peterborough Property",
            County = "Cambridgeshire",
            TownOrCity = "Peterborough",
            Postcode = Postcodes["PE1 1BF"],
            IsIncomplete = true
        };
    }

    private PropertyDbModel CreateLeedsProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            LandlordId = landlordId,
            AddressLine1 = "Leeds Property",
            County = "West Yorkshire",
            TownOrCity = "Leeds",
            Postcode = Postcodes["LS1 1AZ"],
            IsIncomplete = true
        };
    }

    private static TenantDbModel CreateTenant()
    {
        return new TenantDbModel
        {
            Name = "Example Tenant",
            Email = "exampletenant@gmail.com",
            Type = "Single",
            HasPet = false,
            NotInEET = true,
            UniversalCredit = true,
            HousingBenefits = false,
            Under35 = false,
            Postcode = Postcodes["LS1 1AZ"]
        };
    }

    private static TenantDbModel CreateFalseTenant()
    {
        return new TenantDbModel
        {
            Name = "Example Tenant",
            Email = "exampletenant@gmail.com",
            Type = "Single",
            HasPet = false,
            NotInEET = false,
            UniversalCredit = false,
            HousingBenefits = false,
            Under35 = false,
            Postcode = Postcodes["PE1 1BF"]
        };
    }

    private PropertyDbModel CreateCompleteProperty(int landlordId)
    {
        return new PropertyDbModel
        {
            AcceptsBenefits = true,
            AcceptsCouple = true,
            AcceptsFamily = true,
            AcceptsPets = false,
            AcceptsNotInEET = true,
            AcceptsSingleTenant = true,
            LandlordId = landlordId,
            AddressLine1 = "22 Test Road",
            County = "Cambridgeshire",
            TownOrCity = "Cambridge",
            AcceptsWithoutGuarantor = true,
            Postcode = Postcodes["CB1 1DX"],
            IsIncomplete = false
        };
    }

    private static PostcodeDbModel AddPostcode(string postcode, double lon, double lat)
    {
        return new PostcodeDbModel
        {
            Postcode = postcode,
            Location = new Point(lon, lat) { SRID = 4326 }
        };
    }

    private static PostcodeDbModel AddPostcode(string postcode)
    {
        return new PostcodeDbModel
        {
            Postcode = postcode
        };
    }
}