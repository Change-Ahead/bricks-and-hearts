using System.Linq;
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
                    CreateLandlord(),
                    CreateUnlinkedLandlordWithInviteLink());
                
                context.Users.AddRange(
                    CreateAdminUser(),
                    CreateNonAdminUser(),
                    CreateRequestedAdminUser(),
                    CreateLandlordUser());

                context.SaveChanges();
                
                var landlordId = context.Landlords.Single(l => l.FirstName == "Landlord").Id;
                var landlordUser = context.Users.Single(u => u.GoogleUserName == "LandlordUser");
                landlordUser.LandlordId = landlordId;

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
    
    public UserDbModel CreateLandlordUser()
    {
        return new UserDbModel
        {
            GoogleUserName = "LandlordUser",
            GoogleEmail = "test.email4@gmail.com",
            GoogleAccountId = "4",
            IsAdmin = false,
            HasRequestedAdmin = false
        };
    }

    public LandlordDbModel CreateLandlord()
    {
        return new LandlordDbModel
        {
            Title = "Mr",
            FirstName = "Landlord",
            LastName = "User",
            Email = "test.landlord1@gmail.com",
            Phone = "1",
            LandlordStatus = "Private Residential Sector",
        };
    }
    
    public LandlordDbModel CreateUnlinkedLandlordWithInviteLink()
    {
        return new LandlordDbModel
        {
            Title = "Mr",
            FirstName = "UnlinkedLandlord",
            LastName = "User",
            Email = "test.landlord2@gmail.com",
            Phone = "2",
            InviteLink = "001",
            LandlordStatus = "Private Residential Sector",
        };
    }
}
