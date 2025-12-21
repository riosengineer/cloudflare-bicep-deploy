using CloudflareExtension.Models;

namespace CloudflareExtension.Services;

/// <summary>
/// Interface for Cloudflare API operations - enables testing with mock implementations
/// </summary>
public interface ICloudflareApiService : IDisposable
{
    /// <summary>
    /// Creates a new Cloudflare zone
    /// </summary>
    Task<CloudflareZone> CreateZoneAsync(CloudflareZone zone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing Cloudflare zone by name
    /// </summary>
    Task<CloudflareZone?> GetZoneAsync(string zoneName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a DNS record
    /// </summary>
    Task<CloudflareDnsRecord> UpsertDnsRecordAsync(CloudflareDnsRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing DNS record by name and type
    /// </summary>
    Task<CloudflareDnsRecordApiResult?> FindDnsRecordAsync(CloudflareDnsRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing security rule by reference or expression
    /// </summary>
    Task<CloudflareSecurityRuleApiResult?> FindSecurityRuleAsync(string zoneId, CloudflareSecurityRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a security rule
    /// </summary>
    Task<CloudflareSecurityRule> UpsertSecurityRuleAsync(CloudflareSecurityRule rule, CancellationToken cancellationToken = default);
}
