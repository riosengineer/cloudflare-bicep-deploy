# SecurityRule

Manages a Cloudflare Security Rule

## Example usage

### Block traffic from Country

Creates a security rule that blocks requests originating from China using the free plan API.

```bicep
resource blockCountryTraffic 'SecurityRule' = {
    name: 'blockCountryTraffic'
    zoneId: zoneId
    description: 'Block requests from CN'
    expression: '(ip.src.country eq "CN")'
    action: 'block'
    enabled: true
}
```

## Argument reference

The following arguments are available:

- `action` - (Required) Action applied to matching requests (allow, block, challenge, js_challenge, managed_challenge, log)
- `expression` - (Required) Security rule expression that defines matching traffic
- `name` - (Required) The logical name of the security rule
- `zoneId` - (Required) The zone ID this rule applies to
- `description` - (Optional) Human friendly description shown in the Cloudflare dashboard
- `enabled` - (Optional) Whether the rule is enabled (set to false to pause the rule)
- `reference` - (Optional) Reference identifier persisted with the rule; defaults to the resource name on first deploy and cannot be changed by the Cloudflare API afterwards
- `filterId` - (Optional) Cloudflare filter ID associated with this security rule (output only)
- `ruleId` - (Optional) Cloudflare security rule ID (output only)
