# Zone

Manages a Cloudflare DNS Zone

## Example usage

### Creating a basic DNS zone

This example shows how to create a DNS zone in Cloudflare using Bicep.

```bicep
resource zone 'Zone' = {
  name: 'zone'
  plan: 'plan'
  nameServers: [
    'ns1.example.com'
    'ns2.example.com'
  ]
}
```

## Argument reference

The following arguments are available:

- `name` - (Required) The zone name (domain)
- `plan` - (Required) The zone plan type
- `nameServers` - (Optional) Name servers assigned to the zone
- `paused` - (Optional) Whether the zone is paused
- `status` - (Optional) Zone status
- `zoneId` - (Optional) Zone ID (output only)
