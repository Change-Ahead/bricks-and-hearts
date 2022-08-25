# Bricks and Hearts

## Overview

### Background

ChangeAhead are an organisation that connects people in a vulnerable state with opportunities to get help and be
empowered. One of the ways they do this is matching landlords with empty properties with people in need of housing
assistance. The Bricks & Hearts project is a web platform to facilitate this. It currently consists of a landlord
portal and an admin portal.

A landlord is able to:

* sign up for an account, providing some basic information and answering questions about their properties
* log in and view a dashboard
* view and edit their personal information
* view their properties and add, edit, and remove them

A system admin can:

* log in and view a dashboard
* view all properties _for all landlords_ and add/edit/remove them
* view all landlords and their information and add/edit/remove them
* view all admins and approve/reject requests for admin rights
* import tenant data from a CSV file
* suggest suitable applicants for a given property

### Tech stack

#### Backend

* C# on .NET Core 6
  with the [ASP.NET Core MVC framework](https://docs.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-6.0)
* MSSQL database and [Entity Framework Core 6](https://docs.microsoft.com/en-us/ef/core/)

We also use the following APIs:

* [Google OAuth 2.0](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-6.0)
* [Azure Blob Storage](https://docs.microsoft.com/en-us/azure/storage/blobs/)
* [Azure Maps API](https://docs.microsoft.com/en-us/azure/azure-maps/)
* [Postcodes.io API](https://postcodes.io/)

The staging and production sites are hosted on Microsoft Azure, and [GitHub Actions](https://docs.github.com/en/actions)
is used for CI/CD.

#### Frontend

* [Razor MVC views](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-6.0)
* [Bootstrap (v5.1)](https://getbootstrap.com/docs/5.1/getting-started/introduction/)

## Project setup

### Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download)
* [SQL Server](https://www.microsoft.com/en-gb/sql-server/sql-server-downloads) (Developer version)

### Setup

1. Download and install the requirements above.
2. Clone the repository into a sensible location on your machine, and check out the `main` branch.
    ```shell
    git clone git@github.com:Change-Ahead/bricks-and-hearts.git
    git checkout main
    ```
3. You should have access to a collection of 'secrets' (login details, API keys, etc.) for the Bricks & Hearts project.
   Find the `appsettings.Development.json` file and put it into the `web` folder.
4. Edit the file and change the `ToAddress` in the `Email` section to your email address. This means any notification
   emails sent by your local environment will be sent to you.
5. Install the EF Core CLI tools.
    ```shell
   dotnet tool install --global dotnet-ef
   ```
6. Build the project.
    ```shell
   dotnet build
    ```
7. Apply the database migrations. This will create a `BricksAndHearts` database in your local SQL Server if it doesn't
   exist already.
    ```shell
   dotnet ef database update --project web
    ```

## Running the project

```shell
dotnet run --project web
```

or launch from your IDE (e.g. Rider).

## Testing

### Running unit tests

Unit tests can be run from your IDE or from the terminal:

```shell
dotnet test
```

or to filter to just controller/service tests:

```shell
dotnet test --filter "FullyQualifiedName~BricksAndHearts.UnitTests.ControllerTests"
dotnet test --filter "FullyQualifiedName~BricksAndHearts.UnitTests.ServiceTests"
```

Running the tests should create a `BricksAndHeartsTest` database in SQL Server which is used by the service tests.

### Writing unit tests

Unit tests can be found in the `BricksAndHearts.UnitTests` project and use the following frameworks:

* [xUnit](https://xunit.net/)
* [FluentAssertions](https://fluentassertions.com/introduction)
* [FakeItEasy](https://fakeiteasy.readthedocs.io/en/stable/)

There are currently two categories of unit tests:

* **Controller tests** – These test the correct behaviour of the controller actions in terms of routing, validation,
  redirects, etc. Real service objects should generally not be used here, and instead mocks should be used to fake their
  behaviour to check the controller method responds correctly.
* **Service tests** – These test the functionality of the methods in the service classes, which usually perform database
  or API operations. They should not test any controller functionality.

Some service tests check whether database operations are executed correctly. Database unit tests work slightly
differently to normal tests &ndash; see the [guide](DATABASE_UNIT_TESTS.MD) for how to use them.

## Copyright

Copyright © 2022 Change Ahead, the trading name of Homeless To Wellness Ltd.
