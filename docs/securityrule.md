# SecurityRule

Manages a Cloudflare WAF Custom Rule

Reference template: [Sample/security-rule.bicep](../Sample/security-rule.bicep)

## Argument reference

The following arguments are available:

- `action` - (Required) Action applied to matching requests (allow, block, challenge, js_challenge, managed_challenge, log)
- `expression` - (Required) Security rule expression that defines matching traffic
- `name` - (Required) The logical name of the custom rule
- `zoneId` - (Required) The zone ID this rule applies to
- `description` - (Optional) Human friendly description shown in the Cloudflare dashboard
- `enabled` - (Optional) Whether the rule is enabled (set to false to pause the rule)
- `reference` - (Optional) Reference identifier persisted with the rule; defaults to the resource name on first deploy
- `ruleId` - (Optional) Cloudflare custom rule ID (output only)
