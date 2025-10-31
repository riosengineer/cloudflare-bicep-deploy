# FirewallRule

Manages a Cloudflare Firewall Rule

## Example usage

### Block traffic from China

Creates a firewall rule that blocks requests originating from China using the free-plan API.

```bicep
resource blockChinaTraffic 'FirewallRule' = {
    name: 'blockChinaTraffic'
    zoneId: zoneId
    description: 'Block requests from CN'
    expression: 'ip.src.country eq "CN"'
    action: 'block'
    enabled: true
}
```

## Argument reference

The following arguments are available:

- `action` - (Required) Action applied to matching requests (allow, block, challenge, js_challenge, managed_challenge, log)
- `expression` - (Required) Firewall expression that defines matching traffic
- `name` - (Required) The logical name of the firewall rule
- `zoneId` - (Required) The zone ID this rule applies to
- `description` - (Optional) Human friendly description shown in the Cloudflare dashboard
- `enabled` - (Optional) Whether the rule is enabled (set to false to pause the rule)
- `filterId` - (Optional) Cloudflare filter ID associated with this rule (output only)
- `ruleId` - (Optional) Cloudflare rule ID (output only)
