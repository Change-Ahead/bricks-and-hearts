using System;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.Extensions.Configuration;

namespace BricksAndHearts.UnitTests.ServiceTests;

public class TestDatabaseFixture
{
    private static readonly object Lock = new();
    private static bool _databaseInitialised;

    public TestDatabaseFixture()
    {
        lock (Lock)
        {
            if (_databaseInitialised) return;
            using (var context = CreateContext(readOnly: false))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.Landlords.AddRange(
                    CreateApprovedLandlord(), // landlordId = 1
                    CreateUnapprovedLandlord(), // landlordId = 2
                    CreateLandlordWithLink(), // landlordId = 3
                    CreateUnlinkedLandlordWithLink(), // landlordId = 4
                    CreateLandlordWithMembershipId(420), // landlordId = 5
                    CreateLandlordWithMembershipId(421) // landlordId = 6
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
                    CreateIncompleteProperty(2)
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

    public UserDbModel CreateNonAdminUser()
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

    public UserDbModel CreateRequestedAdminUser()
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

    public UserDbModel CreateUnapprovedLandlordUser()
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

    public UserDbModel CreateLandlordUserWithLink()
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

    public UserDbModel CreateUnlinkedLandlordUser()
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
    public LandlordDbModel CreateApprovedLandlord()
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

    public LandlordDbModel CreateUnapprovedLandlord()
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

    public LandlordDbModel CreateLandlordWithLink()
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

    private LandlordDbModel CreateUnlinkedLandlordWithLink()
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

    private LandlordDbModel CreateLandlordWithMembershipId(int memberId)
    {
        return new LandlordDbModel
        {
            Email = "test.landlord5&6@gmail.com",
            FirstName = "Landlord5&6MembershipId",
            LastName = "Landlord5&6Sur",
            Title = "Mr",
            Phone = "005&6",
            LandlordType = "Non profit",
            LandlordProvidedCharterStatus = true,
            CharterApproved = true,
            MembershipId = $"Member-{memberId}"
        };
    }
    
    //begin property models
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB2 1LA",
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
            Postcode = "CB1 1DX",
            IsIncomplete = true
        };
    }

    private TenantDbModel CreateTenant()
    {
        return new TenantDbModel
        {
            Name = "Example Tenant",
            Email = "exampletenant@gmail.com",
            Type = "Single",
            HasPet = false,
            ETT = true,
            UniversalCredit = true
        };
    }
    
    private TenantDbModel CreateFalseTenant()
    {
        return new TenantDbModel
        {
            Name = "Example Tenant",
            Email = "exampletenant@gmail.com",
            Type = "Single"
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
            AcceptsNotEET = true,
            AcceptsSingleTenant = true,
            LandlordId = landlordId,
            AddressLine1 = "22 Test Road",
            County = "Cambridgeshire",
            TownOrCity = "Cambridge",
            AcceptsWithoutGuarantor = true,
            Postcode = "CB1 1DX",
            IsIncomplete = false
        };
    }
}