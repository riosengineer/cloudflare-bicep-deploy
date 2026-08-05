using './security-rule.bicep'

param zoneId = ''
param securityRuleExpression = '(http.request.uri.path eq "/__bicep_cf_test_block__")'
param enabled = true
