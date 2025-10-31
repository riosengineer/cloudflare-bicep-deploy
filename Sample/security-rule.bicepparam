using './security-rule.bicep'

param zoneId = '9ed669474439c150ea4496ad5dcb9219'
param firewallExpression = 'ip.src.country eq "CN"'
param enabled = true
