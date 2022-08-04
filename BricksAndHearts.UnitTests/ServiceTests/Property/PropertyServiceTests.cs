using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Property;

public class PropertyServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public PropertyServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async void CountProperties_ReturnsPropertyCountModel_WithCorrectData()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context);

        // Act
        var result = service.CountProperties();

        // Assert
        result.Should().BeOfType<PropertyCountModel>();
        result.RegisteredProperties.Should().Be(2);
    }
}