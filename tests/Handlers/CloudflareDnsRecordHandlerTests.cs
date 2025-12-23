using CloudflareExtension.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Tests.Handlers;

[TestClass]
public class CloudflareDnsRecordHandlerTests
{
    private Mock<ICloudflareApiServiceFactory> _mockFactory = null!;
    private Mock<ICloudflareApiService> _mockApiService = null!;
    private CloudflareDnsRecordHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockApiService = new Mock<ICloudflareApiService>(MockBehavior.Loose);
        _mockFactory = new Mock<ICloudflareApiServiceFactory>(MockBehavior.Loose);
        _mockFactory.Setup(f => f.Create()).Returns(_mockApiService.Object);
        _handler = new CloudflareDnsRecordHandler(_mockFactory.Object);
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test.example.com",
            ZoneName = "example.com",
            ZoneId = "zone123",
            Type = "A",
            Content = "192.168.1.1"
        };

        // Act - Use reflection to call the protected method
        var method = typeof(CloudflareDnsRecordHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareDnsRecordIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test.example.com");
        result.ZoneName.Should().Be("example.com");
    }

    [TestMethod]
    [DataRow("A", "192.168.1.1")]
    [DataRow("AAAA", "2001:db8::1")]
    [DataRow("CNAME", "alias.example.com")]
    [DataRow("TXT", "v=spf1 include:example.com ~all")]
    public void GetIdentifiers_HandlesVariousDnsRecordTypes(string recordType, string content)
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test.example.com",
            ZoneName = "example.com",
            ZoneId = "zone123",
            Type = recordType,
            Content = content
        };

        // Act
        var method = typeof(CloudflareDnsRecordHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareDnsRecordIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test.example.com");
        result.ZoneName.Should().Be("example.com");
    }

    [TestMethod]
    public void Constructor_WithFactory_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareDnsRecordHandler(_mockFactory.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareDnsRecordHandler();

        // Assert
        handler.Should().NotBeNull();
    }
}
