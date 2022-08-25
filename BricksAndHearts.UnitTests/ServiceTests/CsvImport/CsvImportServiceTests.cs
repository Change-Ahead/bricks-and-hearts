using System.IO;
using System.Net.Http;
using System.Text;
using BricksAndHearts.Services;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.CsvImport;

public class CsvImportServiceTests : IClassFixture<TestDatabaseFixture>
{
    private HttpClient HttpClient;
    private readonly HttpMessageHandler MessageHandler;
    private readonly IPostcodeService PostcodeService;

    public CsvImportServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
        PostcodeService = A.Fake<PostcodeService>();
        Logger = A.Fake<ILogger<CsvImportService>>();
        MessageHandler = A.Fake<HttpMessageHandler>();
        HttpClient = new HttpClient(MessageHandler);
    }
    private TestDatabaseFixture Fixture { get; }
    private ILogger<CsvImportService> Logger { get; }

    [Fact]
    public async void CheckIfImportWorks_CalledOnFileWithMissingColumn_ReturnsDangerMessage()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new CsvImportService(context, PostcodeService, Logger);

        var data = Encoding.UTF8.GetBytes(
            "Name,Phone,Postcode,Type,HasPet,InEET,UniversalCredit,HousingBenefits,Under35,HasGuarantor");
        var csvFile = new FormFile(new MemoryStream(data), 0, data.Length, null!, "fakeFile.csv");

        // Act
        var flashResponse = service.CheckIfImportWorks(csvFile);

        // Assert
        flashResponse.Item1.Should().Contain("danger");
        flashResponse.Item2.Should()
            .Contain(
                "Import has failed because column Email is missing. Please add this column to your records before attempting to import them.");
    }

    [Fact]
    public async void CheckIfImportWorks_CalledOnFileWithExtraColumn_ReturnsWarningMessage()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new CsvImportService(context, PostcodeService, Logger);

        var data = Encoding.UTF8.GetBytes(
            "Extra,Name,Email,Phone,Postcode,Type,HasPet,InEET,UniversalCredit,HousingBenefits,Under35,HasGuarantor");
        var csvFile = new FormFile(new MemoryStream(data), 0, data.Length, null!, "fakeFile.csv");

        // Act
        var flashResponse = service.CheckIfImportWorks(csvFile);

        // Assert
        flashResponse.Item1.Should().Contain("warning");
        flashResponse.Item2.Should()
            .Contain("The column \"Extra\" does not exist in the database. All data in this column has been ignored.");
    }

    [Fact]
    public async void CheckIfImportWorks_CalledOnFileWithCorrectColumns_ReturnsNoMessage()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new CsvImportService(context, PostcodeService, Logger);

        var data = Encoding.UTF8.GetBytes(
            "Name,Email,Phone,Postcode,Type,HasPet,InEET,UniversalCredit,HousingBenefits,Under35,HasGuarantor");
        var csvFile = new FormFile(new MemoryStream(data), 0, data.Length, null!, "fakeFile.csv");

        // Act
        var flashResponse = service.CheckIfImportWorks(csvFile);

        // Assert
        flashResponse.Item1.Should().BeEmpty();
        flashResponse.Item2.Should().BeEmpty();
    }

    // Cannot test ImportTenants method: it involves writing to the database and the method includes a transaction
}