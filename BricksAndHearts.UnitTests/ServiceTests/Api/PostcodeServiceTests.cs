using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Services;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Api;

public class PostcodeServiceTests : PostcodeServiceTestsBase
{
    
    public PostcodeServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    private TestDatabaseFixture Fixture { get; }
    
    [Fact]
    public void FormatPostcode_WithValidPostcode_ReturnsFormattedPostcode()
    {
        // Arrange
        const string postcode = "eh11ad";

        // Act
        var result = UnderTest.FormatPostcode(postcode);

        // Assert
        result.Should().Be("EH1 1AD");
    }
    
    [Fact]
    public void FormatPostcode_WithInvalidPostcode_ReturnsEmptyString()
    {
        // Arrange
        const string postcode = "invalid";

        // Act
        var result = UnderTest.FormatPostcode(postcode);

        // Assert
        result.Should().Be("");
    }
    
    [Fact]
    public async void AddSinglePostcodeToDatabaseIfAbsent_WithAbsentPostcode_CallsHttpClientAndAddsCorrectPostcodeToDatabase()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PostcodeService(Logger, context, HttpClient);
        var postcodesBeforeCount = context.Postcodes.Count();

        const string postcode = "EH1 1AD";//_httpClient hard-coded to return API response for this postcode
        context.Postcodes.Should().NotContain(p => p.Postcode == postcode);
        var response = CreateHttpResponseMessageForSingleApiRequest();
        A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .Returns(response);

        // Act
        await service.AddSinglePostcodeToDatabaseIfAbsent(postcode);
        
        context.ChangeTracker.Clear();
        
        // Assert
        A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .MustHaveHappened();
        context.Postcodes.Count().Should().Be(postcodesBeforeCount + 1);
        context.Postcodes.Should().Contain(p => p.Postcode == postcode);
    }
    
    [Fact]
    public async void AddSinglePostcodeToDatabaseIfAbsent_WithPresentPostcode_DoesNotCallHttpClientAndDoesNotChangeDatabase()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PostcodeService(Logger, context, HttpClient);
        var postcodesBeforeCount = context.Postcodes.Count();

        const string postcode = "CB2 1LA";
        context.Postcodes.Should().Contain(p => p.Postcode == postcode);

        // Act
        await service.AddSinglePostcodeToDatabaseIfAbsent(postcode);
        
        context.ChangeTracker.Clear();
        
        // Assert
        A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .MustNotHaveHappened();
        context.Postcodes.Count().Should().Be(postcodesBeforeCount);
        context.Postcodes.Should().Contain(p => p.Postcode == postcode);
    }
    
    [Fact]
    public async void AddSinglePostcodeToDatabaseIfAbsent_WithEmptyString_DoesNotCallHttpClientAndDoesNotChangeDatabase()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PostcodeService(Logger, context, HttpClient);
        var postcodesBeforeCount = context.Postcodes.Count();

        const string postcode = "";

        // Act
        await service.AddSinglePostcodeToDatabaseIfAbsent(postcode);
        
        context.ChangeTracker.Clear();
        
        // Assert
        A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .MustNotHaveHappened();
        context.Postcodes.Count().Should().Be(postcodesBeforeCount);
        context.Postcodes.Should().NotContain(p => p.Postcode == postcode);
    }
    
    [Fact]
    public async void BulkAddPostcodeToDatabaseIfAbsent_WithAbsentPostcodes_CallsHttpClientAndAddsCorrectPostcodeToDatabase()
    {
       // Arrange
       await using var context = Fixture.CreateWriteContext();
       var service = new PostcodeService(Logger, context, HttpClient);
       var postcodesBeforeCount = context.Postcodes.Count();

       List<string> postcodes = new() { "EH1 1AD", "CB3 9AJ" };//_httpClient hard-coded to return API response for these postcodes
       foreach (string postcode in postcodes)
       {
           context.Postcodes.Should().NotContain(p => p.Postcode == postcode);
       }
       
       var response = CreateHttpResponseMessageForBulkApiRequest();
       A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
           .Returns(response);

       // Act
       await service.BulkAddPostcodesToDatabaseIfAbsent(postcodes);
       
       context.ChangeTracker.Clear();
       
       // Assert
       A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
           .MustHaveHappened();
       context.Postcodes.Count().Should().Be(postcodesBeforeCount + postcodes.Count());
       foreach (string postcode in postcodes)
       {
           context.Postcodes.Should().Contain(p => p.Postcode == postcode);
       }
    }
    
    [Fact]
    public async void BulkAddPostcodeToDatabaseIfAbsent_WithPresentPostcodes_DoesNotCallHttpClientAndDoesNotChangeDatabase()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PostcodeService(Logger, context, HttpClient);
        var postcodesBeforeCount = context.Postcodes.Count();

        List<string> postcodes = new() { "CB2 1LA", "NW5 1TL" };//_httpClient hard-coded to return API response for these postcodes
        foreach (string postcode in postcodes)
        {
            context.Postcodes.Should().Contain(p => p.Postcode == postcode);
        }

        // Act
        await service.BulkAddPostcodesToDatabaseIfAbsent(postcodes);
       
        context.ChangeTracker.Clear();
       
        // Assert
        A.CallTo(MessageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>()
            .MustNotHaveHappened();
        context.Postcodes.Count().Should().Be(postcodesBeforeCount);
        foreach (string postcode in postcodes)
        {
            context.Postcodes.Should().Contain(p => p.Postcode == postcode);
        }
    }
}