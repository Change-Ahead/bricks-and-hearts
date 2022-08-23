# Unit testing database methods

We have the ability to write unit tests for the methods in our Service classes which perform database operations.
The framework for these tests is slightly different to our normal unit tests, so the instructions below are to help
structure database tests correctly.

## When to use database unit tests

We currently have two categories of unit tests:

* **Controller tests** – These test the correct behaviour of the controller actions in terms of routing, validation,
  redirects, etc. Real service objects should generally not be used here, and instead mocks should be used to fake their
  behaviour to check the controller method responds correctly.
* **Service tests** – These test the functionality of the methods in the service classes, which usually perform database
  or API operations. They should not test any controller functionality.

Database unit tests should be used whenever a service method is performing a database operation.

## Creating a test class

Database test classes work generally the same as for normal unit tests, but with two key differences:

1. Classes should implement the `IClassFixture<TestDatabaseFixture>` interface.
2. They should have a `TestDatabaseFixture` property which is initialised in the constructor. This object sets up the
   database for us.

For example:

```csharp
public class AdminServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public AdminServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }
    
    ...
}
```

## Test database

We have a `BricksAndHeartsTest` database, separate from the normal `BricksAndHearts` database, which all of the database
unit tests are performed on. The database contains a set of dummy records to be tested on, which will be in the same
state at the start of each test.

Dummy records are defined in methods of the `TestDatabaseFixture` class, such as `CreateAdminUser()`
, `CreateNonAdminUser()`, etc. These simply return `DbModel` classes (`UserDbModel`, etc.), and are called by the
constructor, which adds the records to the database.

An example for the `User` table is shown below:

```csharp
context.Users.AddRange(
    CreateAdminUser(),
    CreateNonAdminUser(),
    CreateRequestedAdminUser());
context.SaveChanges();
```

This pattern can be followed to add dummy records for testing for other tables and use cases.

## Writing tests

The main difference between our existing unit tests and the database ones is the creation of a database context. Normal
tests don’t use this as they don’t interact with the database at all.

### Types of test

When dealing with databases, the service methods can be categorised into two types:

* **Read-only** – These don’t change any data in the database. An example would be `AdminService.GetAdminLists()`.

* **Write** – These methods do modify data (and may also perform read queries). An example would be
  `LandlordService.RegisterLandlordWithUser()`. Any writes made to the database only persist for the duration of your
  test, after which they are automatically rolled back.

> Methods which write to the database using an explicit transaction are not supported by this framework and will throw a
> `System.InvalidOperationException: The connection is already in a transaction and cannot participate in another transaction.`

### Steps

1. At the start of your test, create a new context:
    1. `using var context = Fixture.CreateReadContext();` if you’re testing a read-only method.
    2. `using var context = Fixture.CreateWriteContext();` if you’re testing a write method.
2. Create your Service object with this context.
3. If you’re doing a write test, you must add `context.ChangeTracker.Clear();` immediately before the “Assert” step.
   This is so that the queries used in this step query the actual database, instead of the context’s `ChangeTracker`.

## Example

The following is a full example of how to write database unit tests.

```csharp
public class AdminServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public AdminServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    // Example of a read-only test
    [Fact]
    public async void GetAdminLists_OnlyGetsAdmins()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context);

        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        var adminLists = await service.GetAdminLists();

        // Assert
        adminLists.CurrentAdmins.Should().OnlyContain(u => u.Id == adminUser.Id);
    }

    // Example of a write test
    [Fact]
    public void RequestAdminAccess_SetsHasRequestedAdminToTrue_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context);

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        service.RequestAdminAccess(new BricksAndHeartsUser(nonAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following
        // database queries actually query the database, as if this were a new context.
        // This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == nonAdminUser.Id).HasRequestedAdmin.Should().BeTrue();
        context.Users.Single(u => u.Id == adminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }
}
```

## Further reading

This approach was based on [this guide](https://docs.microsoft.com/en-us/ef/core/testing/testing-with-the-database) in
the Entity Framework documentation.

Information about fixtures can be found [here](https://xunit.net/docs/shared-context#class-fixture).