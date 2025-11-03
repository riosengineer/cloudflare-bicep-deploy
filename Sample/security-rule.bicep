targetScope = 'local'

extension Cloudflare

@description('Cloudflare Zone ID for the domain.')
@minLength(32)
@maxLength(32)
param zoneId string

@description('Security rule expression to evaluate.')
param securityRuleExpression string
// Example: '(ip.src.country eq "CN")'

@description('Whether the security rule is enabled.')
param enabled bool = true

// Block traffic from country using Cloudflare free plan security rules
resource blockCountryTraffic 'SecurityRule' = {
  name: 'blockCountryTraffic'
  zoneId: zoneId
  description: 'Block requests originating from specified country'
  expression: securityRuleExpression
  action: 'block'
  enabled: enabled
}

// Outputs
output securityRuleName string = blockCountryTraffic.name
output securityRuleAction string = blockCountryTraffic.action
output securityRuleExpression string = blockCountryTraffic.expression
output securityRuleEnabled bool = blockCountryTraffic.enabled
output securityRuleId string = blockCountryTraffic.ruleId
