using './security-rule.bicep'

param zoneId = '199a3a389856780d8026ce4a2ad06997'
param securityRuleExpression = '(http.request.uri.path eq "/__bicep_cf_test_block__")'
param enabled = true
