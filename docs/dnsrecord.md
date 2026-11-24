# DnsRecord

Manages a Cloudflare DNS Record

Reference template: [Sample/dns.bicep](../Sample/dns.bicep)

## Argument reference

The following arguments are available:

- `content` - (Required) The DNS record content/value
- `name` - (Required) The DNS record name
- `type` - (Required) The DNS record type
- `zoneId` - (Required) The zone ID this record belongs to
- `zoneName` - (Required) The zone name this record belongs to
- `comment` - (Optional) A comment describing the DNS record
- `priority` - (Optional) Priority for MX/SRV records
- `proxiable` - (Optional) Whether this record can be proxied
- `proxied` - (Optional) Whether the record is proxied through Cloudflare
- `recordId` - (Optional) DNS record ID (output only)
- `ttl` - (Optional) Time to live for the record
