# Zone

Manages a Cloudflare DNS Zone

Reference template: [Sample/dns.bicep](../Sample/dns.bicep)

## Argument reference

The following arguments are available:

- `name` - (Required) The zone name (domain)
- `plan` - (Required) The zone plan type
- `nameServers` - (Optional) Name servers assigned to the zone
- `paused` - (Optional) Whether the zone is paused
- `status` - (Optional) Zone status
- `zoneId` - (Optional) Zone ID (output only)
