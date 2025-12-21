using CloudflareExtension.Models;

namespace CloudflareExtension.Tests.Models;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void Validate_WithApiToken_DoesNotThrow()
    {
        // Arrange
        var config = new Configuration
        {
            ApiToken = "test-api-token"
        };

        // Act & Assert
        var action = () => config.Validate();
        action.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_WithApiKeyAndEmail_DoesNotThrow()
    {
        // Arrange
        var config = new Configuration
        {
            ApiKey = "test-api-key",
            Email = "test@example.com"
        };

        // Act & Assert
        var action = () => config.Validate();
        action.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_WithNoCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Configuration();

        // Act & Assert
        var action = () => config.Validate();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cloudflare authentication not configured*");
    }

    [TestMethod]
    public void Validate_WithOnlyApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Configuration
        {
            ApiKey = "test-api-key"
        };

        // Act & Assert
        var action = () => config.Validate();
        action.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Validate_WithOnlyEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Configuration
        {
            Email = "test@example.com"
        };

        // Act & Assert
        var action = () => config.Validate();
        action.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void DefaultBaseUrl_IsCorrect()
    {
        // Arrange
        var config = new Configuration();

        // Assert
        config.BaseUrl.Should().Be("https://api.cloudflare.com/client/v4");
    }

    [TestMethod]
    public void BaseUrl_CanBeOverridden()
    {
        // Arrange
        var config = new Configuration
        {
            BaseUrl = "https://custom-api.example.com/v4"
        };

        // Assert
        config.BaseUrl.Should().Be("https://custom-api.example.com/v4");
    }
}
