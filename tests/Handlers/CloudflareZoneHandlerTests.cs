using CloudflareExtension.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Tests.Handlers;

[TestClass]
public class CloudflareZoneHandlerTests
{
    private Mock<ICloudflareApiServiceFactory> _mockFactory = null!;
    private Mock<ICloudflareApiService> _mockApiService = null!;
    private CloudflareZoneHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockApiService = new Mock<ICloudflareApiService>(MockBehavior.Loose);
        _mockFactory = new Mock<ICloudflareApiServiceFactory>(MockBehavior.Loose);
        _mockFactory.Setup(f => f.Create()).Returns(_mockApiService.Object);
        _handler = new CloudflareZoneHandler(_mockFactory.Object);
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var properties = new CloudflareZone
        {
            Name = "example.com",
            Plan = "free"
        };

        // Act - Use reflection to call the protected method
        var method = typeof(CloudflareZoneHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareZoneIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("example.com");
    }

    [TestMethod]
    [DataRow("example.com")]
    [DataRow("test.example.com")]
    [DataRow("my-domain.co.uk")]
    public void GetIdentifiers_HandlesVariousDomainFormats(string zoneName)
    {
        // Arrange
        var properties = new CloudflareZone
        {
            Name = zoneName,
            Plan = "free"
        };

        // Act
        var method = typeof(CloudflareZoneHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareZoneIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(zoneName);
    }

    [TestMethod]
    [DataRow("free")]
    [DataRow("pro")]
    [DataRow("business")]
    [DataRow("enterprise")]
    public void GetIdentifiers_HandlesVariousPlans(string plan)
    {
        // Arrange
        var properties = new CloudflareZone
        {
            Name = "example.com",
            Plan = plan
        };

        // Act
        var method = typeof(CloudflareZoneHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareZoneIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("example.com");
    }

    [TestMethod]
    public void Constructor_WithFactory_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareZoneHandler(_mockFactory.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareZoneHandler();

        // Assert
        handler.Should().NotBeNull();
    }
}
