# DnsRecord

Manages a Cloudflare DNS Record

## Example usage

### Creating a basic DNS record

This example shows how to create a DNS record in Cloudflare using Bicep.

```bicep
resource cnameRecord 'DnsRecord' = {
  name: 'cname'
  zoneName: domainName
  zoneId: zoneId
  type: 'CNAME'
  content: 'test.example.com'
  ttl: 300
  proxied: false
}
```

## Argument reference

The following arguments are available:

- `content` - (Required) The DNS record content/value
- `name` - (Required) The DNS record name
- `type` - (Required) The DNS record type
- `zoneName` - (Required) The zone name this record belongs to
- `zoneId` - (Required) The zone ID this record belongs to
- `priority` - (Optional) Priority for MX/SRV records
- `proxiable` - (Optional) Whether this record can be proxied
- `proxied` - (Optional) Whether the record is proxied through CloudFlare
- `recordId` - (Optional) DNS record ID (output only)
- `ttl` - (Optional) Time to live for the record
