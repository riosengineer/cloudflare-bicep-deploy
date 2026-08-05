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

    // --- CreateOrUpdate behavioural tests ---

    [TestMethod]
    public async Task CreateOrUpdate_NewRule_CreatesRuleAndReturnsRuleId()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "block-cn",
            ZoneId = "zone-abc",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "block"
        };

        _mockApiService.Setup(s => s.FindSecurityRuleAsync("zone-abc", It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareSecurityRuleApiResult?)null);

        _mockApiService.Setup(s => s.UpsertSecurityRuleAsync(It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareSecurityRule
            {
                Name = "block-cn",
                ZoneId = "zone-abc",
                Expression = "(ip.src.country eq \"CN\")",
                Action = "block",
                RuleId = "rule-id-001",
                Enabled = true,
                Reference = "block-cn"
            });

        // Act
        var result = await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        result.RuleId.Should().Be("rule-id-001");
        result.Action.Should().Be("block");
        _mockApiService.Verify(s => s.UpsertSecurityRuleAsync(It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_ExistingRule_SetsRuleIdBeforeUpsert()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "block-cn",
            ZoneId = "zone-abc",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "block"
        };

        _mockApiService.Setup(s => s.FindSecurityRuleAsync("zone-abc", It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudflareSecurityRuleApiResult { Id = "rule-existing-999", Ref = "block-cn" });

        _mockApiService.Setup(s => s.UpsertSecurityRuleAsync(It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareSecurityRule rule, CancellationToken _) => rule);

        // Act
        await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert — UpsertSecurityRuleAsync must receive the RuleId so it performs a PATCH
        _mockApiService.Verify(s => s.UpsertSecurityRuleAsync(
            It.Is<CloudflareSecurityRule>(r => r.RuleId == "rule-existing-999"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateOrUpdate_InvalidAction_ThrowsInvalidOperationException()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "bad-action-rule",
            ZoneId = "zone-abc",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "deny" // not a valid Cloudflare action
        };

        _mockApiService.Setup(s => s.FindSecurityRuleAsync(It.IsAny<string>(), It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareSecurityRuleApiResult?)null);

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*deny*not supported*");
    }

    [TestMethod]
    public async Task CreateOrUpdate_MissingZoneId_ThrowsInvalidOperationException()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "rule",
            ZoneId = "",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "block"
        };

        // Act
        var act = () => HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ZoneId is required*");
    }

    [TestMethod]
    [DataRow("BLOCK", "block")]
    [DataRow("Allow", "allow")]
    [DataRow("LOG", "log")]
    public async Task CreateOrUpdate_NormalizesActionCasing(string inputAction, string expectedNormalizedAction)
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "normalize-test",
            ZoneId = "zone-abc",
            Expression = "(ip.src.country eq \"US\")",
            Action = inputAction
        };

        _mockApiService.Setup(s => s.FindSecurityRuleAsync(It.IsAny<string>(), It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareSecurityRuleApiResult?)null);

        _mockApiService.Setup(s => s.UpsertSecurityRuleAsync(It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CloudflareSecurityRule rule, CancellationToken _) => rule);

        // Act
        await HandlerTestHelper.InvokeCreateOrUpdateAsync(_handler, properties);

        // Assert — action should be normalized before being passed to the service
        _mockApiService.Verify(s => s.UpsertSecurityRuleAsync(
            It.Is<CloudflareSecurityRule>(r => r.Action == expectedNormalizedAction),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Preview_ReturnsPropertiesWithoutCallingApi()
    {
        // Arrange
        var properties = new CloudflareSecurityRule
        {
            Name = "block-cn",
            ZoneId = "zone-abc",
            Expression = "(ip.src.country eq \"CN\")",
            Action = "block"
        };

        // Act
        var result = await HandlerTestHelper.InvokePreviewAsync(_handler, properties);

        // Assert
        result.Name.Should().Be("block-cn");
        _mockApiService.Verify(s => s.UpsertSecurityRuleAsync(It.IsAny<CloudflareSecurityRule>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

