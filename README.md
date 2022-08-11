# Bricks and Hearts

## Project setup

### Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download)
* SQL Server
  * You can download the Developer edition from [here](https://www.microsoft.com/en-gb/sql-server/sql-server-downloads)

### Setup

* Go to Keeper and download `appsettings.Development.json`. Put this file in the `web` folder.
* Install EF core tools `dotnet tool install --global dotnet-ef`
* Build the project `dotnet build`
* Run the migrations `dotnet ef database update --project web`
  * This will create a BricksAndHearts database in your local SQL Server if it doesn't exist already

## Running the project

```sh
dotnet run --project web
```

Or launch from your IDE, e.g. Rider.

Copyright © 2022 Change Ahead, the trading name of Homeless To Wellness Ltd.
