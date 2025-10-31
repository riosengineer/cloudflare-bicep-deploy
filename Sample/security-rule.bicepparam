using './security-rule.bicep'

param zoneId = '00000000000000000000000000000000'
param firewallExpression = 'ip.src.country eq "CN"'
param enabled = true
