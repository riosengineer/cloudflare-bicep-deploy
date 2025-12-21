using CloudflareExtension.Models;

namespace CloudflareExtension.Tests.Models;

[TestClass]
public class CloudflareSecurityRuleActionsTests
{
    [TestMethod]
    [DataRow("allow", "allow")]
    [DataRow("ALLOW", "allow")]
    [DataRow("Allow", "allow")]
    [DataRow("block", "block")]
    [DataRow("BLOCK", "block")]
    [DataRow("Block", "block")]
    [DataRow("challenge", "challenge")]
    [DataRow("js_challenge", "js_challenge")]
    [DataRow("managed_challenge", "managed_challenge")]
    [DataRow("log", "log")]
    public void TryNormalize_ValidAction_ReturnsTrue(string input, string expected)
    {
        // Act
        var result = CloudflareSecurityRuleActions.TryNormalize(input, out var normalized);

        // Assert
        result.Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("invalid")]
    [DataRow("deny")]
    [DataRow("reject")]
    [DataRow("")]
    [DataRow("INVALID")]
    public void TryNormalize_InvalidAction_ReturnsFalse(string input)
    {
        // Act
        var result = CloudflareSecurityRuleActions.TryNormalize(input, out var normalized);

        // Assert
        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [TestMethod]
    public void SupportedActions_ContainsAllExpectedActions()
    {
        // Act
        var actions = CloudflareSecurityRuleActions.SupportedActions.ToList();

        // Assert
        actions.Should().Contain("allow");
        actions.Should().Contain("block");
        actions.Should().Contain("challenge");
        actions.Should().Contain("js_challenge");
        actions.Should().Contain("managed_challenge");
        actions.Should().Contain("log");
        actions.Should().HaveCount(6);
    }
}
