using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace CloudflareExtension.Models;

/// <summary>
/// Cloudflare API Configuration - supports both API Token and API Key + Email authentication
/// </summary>
public class Configuration
{
    [TypeProperty("Cloudflare API Token (recommended)")]
    public string? ApiToken { get; set; }

    [TypeProperty("Cloudflare API Key (legacy)")]
    public string? ApiKey { get; set; }

    [TypeProperty("Cloudflare account email (required with API Key)")]
    public string? Email { get; set; }

    [TypeProperty("Base URL for Cloudflare API")]
    public string BaseUrl { get; set; } = "https://api.cloudflare.com/client/v4";

    /// <summary>
    /// Gets the authentication configuration from environment variables or configuration
    /// </summary>
    public static Configuration GetConfiguration()
    {
        return new Configuration
        {
            ApiToken = Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN"),
            ApiKey = Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY"),
            Email = Environment.GetEnvironmentVariable("CLOUDFLARE_EMAIL"),
            BaseUrl = Environment.GetEnvironmentVariable("CLOUDFLARE_BASE_URL") ?? "https://api.cloudflare.com/client/v4"
        };
    }

    /// <summary>
    /// Validates that we have valid authentication configuration
    /// </summary>
    public void Validate()
    {
        if (!string.IsNullOrEmpty(ApiToken))
        {
            // API Token is preferred method
            return;
        }

        if (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(Email))
        {
            // API Key + Email is the legacy method
            return;
        }

        throw new InvalidOperationException(
            "Cloudflare authentication not configured. Please provide either:\n" +
            "1. CLOUDFLARE_API_TOKEN environment variable, or\n" +
            "2. Both CLOUDFLARE_API_KEY and CLOUDFLARE_EMAIL environment variables");
    }
}