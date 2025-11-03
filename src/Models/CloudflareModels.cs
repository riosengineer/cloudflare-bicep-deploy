using System.Collections.Generic;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace CloudflareExtension.Models;

// DNS Record Types supported by Cloudflare - using constants for compatibility
public static class DnsRecordType
{
    public const string A = "A";
    public const string AAAA = "AAAA";
    public const string CNAME = "CNAME";
    public const string MX = "MX";
    public const string TXT = "TXT";
    public const string SRV = "SRV";
    public const string PTR = "PTR";
    public const string NS = "NS";
    public const string CAA = "CAA";
}

// Zone Status - using string instead of enum for compatibility
public static class ZoneStatus
{
    public const string Active = "Active";
    public const string Pending = "Pending";
    public const string Initializing = "Initializing";
    public const string Moved = "Moved";
    public const string Deleted = "Deleted";
    public const string Deactivated = "Deactivated";
}

// Cloudflare Zone Resource Identifiers
public class CloudflareZoneIdentifiers
{
    [TypeProperty("The zone name (domain)", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}

// Cloudflare Zone Resource
[BicepDocHeading("Zone", "Manages a Cloudflare DNS Zone")]
[BicepDocExample(
    "Creating a basic DNS zone",
    "This example shows how to create a DNS zone in Cloudflare using Bicep.",
    @"resource zone 'Zone' = {
  name: 'zone'
  plan: 'plan'
  nameServers: [
    'ns1.example.com'
    'ns2.example.com'
  ]
}
"
)]
[ResourceType("Zone")]
public class CloudflareZone : CloudflareZoneIdentifiers
{
    [TypeProperty("The zone plan type", ObjectTypePropertyFlags.Required)]
    public string Plan { get; set; } = "free";

    [TypeProperty("Whether the zone is paused")]
    public bool Paused { get; set; } = false;

    [TypeProperty("Zone status")]
    public string Status { get; set; } = ZoneStatus.Pending;

    [TypeProperty("Zone ID (output only)")]
    public string? ZoneId { get; set; }

    [TypeProperty("Name servers assigned to the zone")]
    public string[]? NameServers { get; set; }
}

// Cloudflare DNS Record Resource Identifiers
public class CloudflareDnsRecordIdentifiers
{
    [TypeProperty("The DNS record name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The zone name this record belongs to", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string ZoneName { get; set; }
}

// Cloudflare DNS Record Resource
[BicepDocHeading("DnsRecord", "Manages a Cloudflare DNS Record")]
[BicepDocExample(
    "Creating a basic DNS record",
    "This example shows how to create a DNS record in Cloudflare using Bicep.",
    @"resource cnameRecord 'DnsRecord' = {
  name: 'cname'
  zoneName: domainName
  zoneId: zoneId
  type: 'CNAME'
  content: 'test.example.com'
  ttl: 300
  proxied: false
  comment: 'DNS record created via Bicep'
}
"
)]
[ResourceType("DnsRecord")]
public class CloudflareDnsRecord : CloudflareDnsRecordIdentifiers
{
    [TypeProperty("The DNS record type", ObjectTypePropertyFlags.Required)]
    public required string Type { get; set; }

    [TypeProperty("The DNS record content/value", ObjectTypePropertyFlags.Required)]
    public required string Content { get; set; }

    [TypeProperty("Time to live for the record")]
    public int Ttl { get; set; } = 300;

    [TypeProperty("Whether the record is proxied through Cloudflare")]
    public bool Proxied { get; set; } = false;

    [TypeProperty("Priority for MX/SRV records")]
    public int Priority { get; set; } = 0;

    [TypeProperty("DNS record ID (output only)")]
    public string? RecordId { get; set; }

    [TypeProperty("Whether this record can be proxied")]
    public bool Proxiable { get; set; } = false;

    [TypeProperty("A comment describing the DNS record")]
    public string? Comment { get; set; }

    [TypeProperty("The zone ID this record belongs to", ObjectTypePropertyFlags.Required)]
    public required string ZoneId { get; set; }
}


public static class CloudflareSecurityRuleActions
{
    private static readonly Dictionary<string, string> NormalizedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["allow"] = "allow",
        ["block"] = "block",
        ["challenge"] = "challenge",
        ["js_challenge"] = "js_challenge",
        ["managed_challenge"] = "managed_challenge",
        ["log"] = "log"
    };

    public static bool TryNormalize(string action, out string normalizedAction)
    {
        if (NormalizedActions.TryGetValue(action, out var normalized))
        {
            normalizedAction = normalized;
            return true;
        }

        normalizedAction = string.Empty;
        return false;
    }

    public static IEnumerable<string> SupportedActions => NormalizedActions.Keys;
}

public class CloudflareSecurityRuleIdentifiers
{
    [TypeProperty("The logical name of the security rule", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The zone ID this security rule applies to", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string ZoneId { get; set; }
}

[BicepDocHeading("SecurityRule", "Manages a Cloudflare Security Rule")]
[BicepDocExample(
        "Block traffic from China",
        "Creates a security rule that blocks requests originating from China using the free-plan API.",
        @"resource blockChinaTraffic 'SecurityRule' = {
    name: 'blockChinaTraffic'
    zoneId: zoneId
    description: 'Block requests from CN'
    expression: '(ip.src.country eq ""CN"")'
    action: 'block'
    enabled: true
}
"
)]
[ResourceType("SecurityRule")]
public class CloudflareSecurityRule : CloudflareSecurityRuleIdentifiers
{
    [TypeProperty("Human friendly description shown in the Cloudflare dashboard")]
    public string? Description { get; set; }

    [TypeProperty("Whether the rule is enabled (set to false to pause the rule)")]
    public bool Enabled { get; set; } = true;

    [TypeProperty("Security rule expression that defines matching traffic", ObjectTypePropertyFlags.Required)]
    public required string Expression { get; set; }

    [TypeProperty("Action applied to matching requests (allow, block, challenge, js_challenge, managed_challenge, log)", ObjectTypePropertyFlags.Required)]
    public required string Action { get; set; }

    [TypeProperty("Cloudflare security rule ID (output only)")]
    public string? RuleId { get; set; }

    [TypeProperty("Cloudflare filter ID associated with this security rule (output only)")]
    public string? FilterId { get; set; }

    [TypeProperty("Optional reference identifier persisted with the rule; defaults to the resource name on create")]
    public string? Reference { get; set; }
}