using './redirect-rule.bicep'

param zoneId = '00000000000000000000000000000000'
param urlPattern = 'example.com/old-page'
param redirectUrl = 'https://example.com/new-page'
param statusCode = 301
param enabled = true
