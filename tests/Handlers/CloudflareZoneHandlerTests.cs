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

    // --- CreateOrUpdate behavioural tests ---

    [TestMethod]
    public async Task CreateOrUpdate_WhenZoneDoesNotExist_CreatesZoneAndReturnsZoneId()
    {
        // Arrange
        var properties = new CloudflareZone { Name = "example.com", Plan = "free" };

        _mockApiService.Setup(s => s.GetZoneAsync("example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareZone?)null);

        _mockApiService.Setup(s => s.CreateZoneAsync(It.IsAny<CloudflareZone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareZone
            {
                Name = "example.com",
                Plan = "free",
                ZoneId = "zone-abc-123",
                Status = ZoneStatus.Pending,
                NameServers = ["ns1.cloudflare.com", "ns2.cloudflare.com"]
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        result.ZoneId.Should().Be("zone-abc-123");
        result.Status.Should().Be(ZoneStatus.Pending);
        result.NameServers.Should().BeEquivalentTo(["ns1.cloudflare.com", "ns2.cloudflare.com"]);
        _mockApiService.Verify(s => s.CreateZoneAsync(It.IsAny<CloudflareZone>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_WhenZoneAlreadyExists_DoesNotCreateAndReturnsExistingData()
    {
        // Arrange
        var properties = new CloudflareZone { Name = "example.com", Plan = "free" };

        _mockApiService.Setup(s => s.GetZoneAsync("example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareZone
            {
                Name = "example.com",
                ZoneId = "zone-existing-456",
                Status = ZoneStatus.Active,
                Plan = "pro",
                Paused = false,
                NameServers = ["ns3.cloudflare.com", "ns4.cloudflare.com"]
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        result.ZoneId.Should().Be("zone-existing-456");
        result.Status.Should().Be(ZoneStatus.Active);
        result.NameServers.Should().BeEquivalentTo(["ns3.cloudflare.com", "ns4.cloudflare.com"]);
        _mockApiService.Verify(s => s.CreateZoneAsync(It.IsAny<CloudflareZone>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateOrUpdate_WhenApiThrows_WrapsExceptionWithContext()
    {
        // Arrange
        var properties = new CloudflareZone { Name = "fail.com", Plan = "free" };

        _mockApiService.Setup(s => s.GetZoneAsync("fail.com", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fail.com*");
    }

    [TestMethod]
    public async Task Preview_ReturnsRequestPropertiesWithoutCallingApi()
    {
        // Arrange
        var properties = new CloudflareZone { Name = "example.com", Plan = "free" };

        // Act
        var result = await HandlerTestHelper.InvokePreviewAsync(_handler, properties);

        // Assert
        result.Name.Should().Be("example.com");
        _mockApiService.Verify(s => s.GetZoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiService.Verify(s => s.CreateZoneAsync(It.IsAny<CloudflareZone>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

