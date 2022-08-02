using BricksAndHearts.Database;
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
                    CreateUnlinkedLandlordWithLink() // landlordId = 4
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
        return CreateContext(readOnly: true);
    }

    public BricksAndHeartsDbContext CreateWriteContext()
    {
        var context = CreateContext(readOnly: false);
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
        return new UserDbModel()
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
            FirstName = "Ronnie",
            LastName = "McFace",
            Title = "Dr",
            Phone = "01189998819991197253",
            LandlordStatus = "Non profit",
            CharterApproved = true
        };
    }
    
    public LandlordDbModel CreateUnapprovedLandlord()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord2@gmail.com",
            FirstName = "Donnie",
            LastName = "McCheeks",
            Title = "Mr",
            Phone = "01189998819991197253",
            LandlordStatus = "Non profit",
            CharterApproved = false
        };
    }

    public LandlordDbModel CreateLandlordWithLink()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord3@gmail.com",
            FirstName = "Lonnie",
            LastName = "McMc",
            Title = "Sister",
            Phone = "01189998819991197253",
            LandlordStatus = "Non profit",
            CharterApproved = true,
            InviteLink = "InvitimusLinkimus"
        };
    }
    
    private LandlordDbModel CreateUnlinkedLandlordWithLink()
    {
        return new LandlordDbModel
        {
            Email = "test.landlord4@gmail.com",
            FirstName = "Unlinked",
            LastName = "Landlord",
            Title = "Mr",
            Phone = "004",
            LandlordStatus = "Non profit",
            CharterApproved = true,
            InviteLink = "invite-unlinked-landlord"
        };
    }
}
