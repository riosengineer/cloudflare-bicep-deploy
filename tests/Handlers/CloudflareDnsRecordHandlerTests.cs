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

    // --- CreateOrUpdate behavioural tests ---

    [TestMethod]
    public async Task CreateOrUpdate_NewRecord_CreatesAndReturnsRecordId()
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test",
            ZoneName = "example.com",
            ZoneId = "zone-abc",
            Type = "A",
            Content = "192.168.1.1"
        };

        _mockApiService.Setup(s => s.FindDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareDnsRecordApiResult?)null);

        _mockApiService.Setup(s => s.UpsertDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareDnsRecord
            {
                Name = "test.example.com",
                ZoneName = "example.com",
                ZoneId = "zone-abc",
                Type = "A",
                Content = "192.168.1.1",
                RecordId = "rec-xyz-789",
                Proxiable = true
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        result.RecordId.Should().Be("rec-xyz-789");
        result.Proxiable.Should().BeTrue();
        _mockApiService.Verify(s => s.UpsertDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_ExistingRecord_SetsRecordIdBeforeUpsert()
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test",
            ZoneName = "example.com",
            ZoneId = "zone-abc",
            Type = "A",
            Content = "10.0.0.1"
        };

        _mockApiService.Setup(s => s.FindDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareDnsRecordApiResult { Id = "existing-rec-111" });

        _mockApiService.Setup(s => s.UpsertDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareDnsRecord rec, CancellationToken _) =>
            {
                rec.RecordId ??= "fallback-id";
                return rec;
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert — UpsertDnsRecordAsync must receive the RecordId so it can PUT instead of POST
        _mockApiService.Verify(s => s.UpsertDnsRecordAsync(
            It.Is<CloudflareDnsRecord>(r => r.RecordId == "existing-rec-111"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_MissingZoneId_ThrowsInvalidOperationException()
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test",
            ZoneName = "example.com",
            ZoneId = "",
            Type = "A",
            Content = "192.168.1.1"
        };

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ZoneId is required*");
    }

    [TestMethod]
    public async Task Preview_ReturnsPropertiesWithoutCallingApi()
    {
        // Arrange
        var properties = new CloudflareDnsRecord
        {
            Name = "test",
            ZoneName = "example.com",
            ZoneId = "zone-abc",
            Type = "CNAME",
            Content = "alias.example.com"
        };

        // Act
        var result = await HandlerTestHelper.InvokePreviewAsync(_handler, properties);

        // Assert
        result.Name.Should().Be("test");
        _mockApiService.Verify(s => s.UpsertDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiService.Verify(s => s.FindDnsRecordAsync(It.IsAny<CloudflareDnsRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

