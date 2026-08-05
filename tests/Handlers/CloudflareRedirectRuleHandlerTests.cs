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

    // --- CreateOrUpdate behavioural tests ---

    [TestMethod]
    public async Task CreateOrUpdate_NewRule_CreatesRuleAndReturnsPageRuleId()
    {
        // Arrange
        var properties = new CloudflareRedirectRule
        {
            Name = "redirect-old-page",
            ZoneId = "zone-abc",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page",
            StatusCode = 301
        };

        _mockApiService.Setup(s => s.FindRedirectRuleAsync("zone-abc", It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflarePageRuleApiResult?)null);

        _mockApiService.Setup(s => s.UpsertRedirectRuleAsync(It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareRedirectRule
            {
                Name = "redirect-old-page",
                ZoneId = "zone-abc",
                UrlPattern = "example.com/old-page",
                RedirectUrl = "https://example.com/new-page",
                StatusCode = 301,
                PageRuleId = "pr-001",
                Priority = 1,
                Enabled = true
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        result.PageRuleId.Should().Be("pr-001");
        result.StatusCode.Should().Be(301);
        _mockApiService.Verify(s => s.UpsertRedirectRuleAsync(It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_ExistingRule_SetsPageRuleIdBeforeUpsert()
    {
        // Arrange
        var properties = new CloudflareRedirectRule
        {
            Name = "redirect-old-page",
            ZoneId = "zone-abc",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page",
            StatusCode = 301
        };

        _mockApiService.Setup(s => s.FindRedirectRuleAsync("zone-abc", It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflarePageRuleApiResult { Id = "pr-existing-777" });

        _mockApiService.Setup(s => s.UpsertRedirectRuleAsync(It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareRedirectRule rule, CancellationToken _) => rule);

        // Act
        await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert — UpsertRedirectRuleAsync must receive the PageRuleId so it can PUT instead of POST
        _mockApiService.Verify(s => s.UpsertRedirectRuleAsync(
            It.Is<CloudflareRedirectRule>(r => r.PageRuleId == "pr-existing-777"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_MissingZoneId_ThrowsInvalidOperationException()
    {
        // Arrange
        var properties = new CloudflareRedirectRule
        {
            Name = "redirect",
            ZoneId = "",
            UrlPattern = "example.com/old",
            RedirectUrl = "https://example.com/new",
            StatusCode = 301
        };

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ZoneId is required*");
    }

    [TestMethod]
    [DataRow(200)]
    [DataRow(303)]
    [DataRow(308)]
    public async Task CreateOrUpdate_InvalidStatusCode_ThrowsInvalidOperationException(int statusCode)
    {
        // Arrange
        var properties = new CloudflareRedirectRule
        {
            Name = "redirect",
            ZoneId = "zone-abc",
            UrlPattern = "example.com/old",
            RedirectUrl = "https://example.com/new",
            StatusCode = statusCode
        };

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*StatusCode must be 301 or 302*");
    }

    [TestMethod]
    public async Task Preview_ReturnsPropertiesWithoutCallingApi()
    {
        // Arrange
        var properties = new CloudflareRedirectRule
        {
            Name = "redirect-old-page",
            ZoneId = "zone-abc",
            UrlPattern = "example.com/old-page",
            RedirectUrl = "https://example.com/new-page",
            StatusCode = 301
        };

        // Act
        var result = await HandlerTestHelper.InvokePreviewAsync(_handler, properties);

        // Assert
        result.Name.Should().Be("redirect-old-page");
        _mockApiService.Verify(s => s.UpsertRedirectRuleAsync(It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiService.Verify(s => s.FindRedirectRuleAsync(It.IsAny<string>(), It.IsAny<CloudflareRedirectRule>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

