using CloudflareExtension.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Tests.Handlers;

[TestClass]
public class CloudflareSecurityRuleHandlerTests
{
    private Mock<ICloudflareApiServiceFactory> _mockFactory = null!;
    private Mock<ICloudflareApiService> _mockApiService = null!;
    private CloudflareSecurityRuleHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockApiService = new Mock<ICloudflareApiService>(MockBehavior.Loose);
        _mockFactory = new Mock<ICloudflareApiServiceFactory>(MockBehavior.Loose);
        _mockFactory.Setup(f => f.Create()).Returns(_mockApiService.Object);
        _handler = new CloudflareSecurityRuleHandler(_mockFactory.Object);
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "block-bad-traffic",
            ZoneId = "zone123",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "block"
        };

        // Act - Use reflection to call the protected method
        var method = typeof(CloudflareSecurityRuleHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareSecurityRuleIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("block-bad-traffic");
        result.ZoneId.Should().Be("zone123");
    }

    [TestMethod]
    [DataRow("allow")]
    [DataRow("block")]
    [DataRow("challenge")]
    [DataRow("js_challenge")]
    [DataRow("managed_challenge")]
    [DataRow("log")]
    public void GetIdentifiers_HandlesVariousActions(string action)
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "test-rule",
            ZoneId = "zone123",
            Expression = "(ip.src.country eq \"CN\")",
            Action = action
        };

        // Act
        var method = typeof(CloudflareSecurityRuleHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareSecurityRuleIdentifiers;

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-rule");
        result.ZoneId.Should().Be("zone123");
    }

    [TestMethod]
    public void Constructor_WithFactory_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareSecurityRuleHandler(_mockFactory.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatesHandler()
    {
        // Arrange & Act
        var handler = new CloudflareSecurityRuleHandler();

        // Assert
        handler.Should().NotBeNull();
    }
}
