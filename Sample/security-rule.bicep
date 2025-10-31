targetScope = 'local'

extension CloudFlare

@description('CloudFlare Zone ID for the domain.')
@minLength(32)
@maxLength(32)
param zoneId string

@description('Firewall expression to evaluate.')
param firewallExpression string

@description('Whether the firewall rule is enabled.')
param enabled bool = true

// Block low scoring traffic from China using CloudFlare free plan security rules
resource blockLowScoreTraffic 'FirewallRule' = {
  name: 'blockLowScoreTraffic'
  zoneId: zoneId
  description: 'Block requests originating from specified country'
  expression: firewallExpression
  action: 'block'
  enabled: enabled
}

// Outputs
output firewallRuleName string = blockLowScoreTraffic.name
output firewallRuleAction string = blockLowScoreTraffic.action
output firewallRuleExpression string = blockLowScoreTraffic.expression
output firewallRuleEnabled bool = blockLowScoreTraffic.enabled
output firewallRuleId string = blockLowScoreTraffic.ruleId
