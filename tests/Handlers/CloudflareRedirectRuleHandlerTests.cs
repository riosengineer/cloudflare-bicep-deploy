using CloudflareExtension.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Tests.Handlers;

[TestClass]
public class CloudflareRedirectRuleHandlerTests
{
    private Mock<ICloudflareApiServiceFactory> _mockFactory = null!;
    private Mock<ICloudflareApiService> _mockApiService = null!;
    private CloudflareRedirectRuleHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockApiService = new Mock<ICloudflareApiService>(MockBehavior.Loose);
        _mockFactory = new Mock<ICloudflareApiServiceFactory>(MockBehavior.Loose);
        _mockFactory.Setup(f => f.Create()).Returns(_mockApiService.Object);
        _handler = new CloudflareRedirectRuleHandler(_mockFactory.Object);
    }

    [TestMethod]
    public void GetIdentifiers_ReturnsCorrectIdentifiers()
    {
        var properties = new CloudflareRedirectRule
        {
            Name = "old-page-redirect",
            ZoneId = "zone123",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page"
        };

        var method = typeof(CloudflareRedirectRuleHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareRedirectRuleIdentifiers;

        result.Should().NotBeNull();
        result!.Name.Should().Be("old-page-redirect");
        result.ZoneId.Should().Be("zone123");
    }

    [TestMethod]
    [DataRow("zone-abc", "rule-one")]
    [DataRow("zone-xyz", "redirect-two")]
    public void GetIdentifiers_ReturnsCorrectIdentifiers_ForVariousInputs(string zoneId, string name)
    {
        var properties = new CloudflareRedirectRule
        {
            Name = name,
            ZoneId = zoneId,
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page"
        };

        var method = typeof(CloudflareRedirectRuleHandler)
            .GetMethod("GetIdentifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(_handler, [properties]) as CloudflareRedirectRuleIdentifiers;

        result.Should().NotBeNull();
        result!.Name.Should().Be(name);
        result.ZoneId.Should().Be(zoneId);
    }

    [TestMethod]
    public void Constructor_WithFactory_CreatesHandler()
    {
        var handler = new CloudflareRedirectRuleHandler(_mockFactory.Object);
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_Parameterless_CreatesHandler()
    {
        var handler = new CloudflareRedirectRuleHandler();
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void RedirectRule_DefaultStatusCode_Is301()
    {
        var rule = new CloudflareRedirectRule
        {
            Name = "test",
            ZoneId = "zone123",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page"
        };

        rule.StatusCode.Should().Be(301);
    }

    [TestMethod]
    public void RedirectRule_DefaultPriority_Is1()
    {
        var rule = new CloudflareRedirectRule
        {
            Name = "test",
            ZoneId = "zone123",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page"
        };

        rule.Priority.Should().Be(1);
    }

    [TestMethod]
    public void RedirectRule_DefaultEnabled_IsTrue()
    {
        var rule = new CloudflareRedirectRule
        {
            Name = "test",
            ZoneId = "zone123",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page"
        };

        rule.Enabled.Should().BeTrue();
    }
}
