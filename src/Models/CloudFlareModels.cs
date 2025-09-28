using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace CloudFlareExtension.Models;

// DNS Record Types supported by CloudFlare - using constants for compatibility
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

// CloudFlare Zone Resource Identifiers
public class CloudFlareZoneIdentifiers
{
    [TypeProperty("The zone name (domain)", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}

// CloudFlare Zone Resource
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
public class CloudFlareZone : CloudFlareZoneIdentifiers
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

// CloudFlare DNS Record Resource Identifiers
public class CloudFlareDnsRecordIdentifiers
{
    [TypeProperty("The DNS record name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The zone name this record belongs to", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string ZoneName { get; set; }
}

// CloudFlare DNS Record Resource
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
}
"
)]
[ResourceType("DnsRecord")]
public class CloudFlareDnsRecord : CloudFlareDnsRecordIdentifiers
{
    [TypeProperty("The DNS record type", ObjectTypePropertyFlags.Required)]
    public required string Type { get; set; }

    [TypeProperty("The DNS record content/value", ObjectTypePropertyFlags.Required)]
    public required string Content { get; set; }

    [TypeProperty("Time to live for the record")]
    public int Ttl { get; set; } = 300;

    [TypeProperty("Whether the record is proxied through CloudFlare")]
    public bool Proxied { get; set; } = false;

    [TypeProperty("Priority for MX/SRV records")]
    public int Priority { get; set; } = 0;

    [TypeProperty("DNS record ID (output only)")]
    public string? RecordId { get; set; }

    [TypeProperty("Whether this record can be proxied")]
    public bool Proxiable { get; set; } = false;

    [TypeProperty("The zone ID this record belongs to", ObjectTypePropertyFlags.Required)]
    public required string ZoneId { get; set; }
}