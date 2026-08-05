using './security-rule.bicep'

param zoneId = '9ed669474439c150ea4496ad5dcb9219'
param securityRuleExpression = '(http.request.uri.path eq "/__bicep_cf_test_block__")'
param enabled = true
