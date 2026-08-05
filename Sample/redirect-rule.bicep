targetScope = 'local'

extension Cloudflare

@description('Cloudflare Zone ID for the domain.')
@minLength(32)
@maxLength(32)
param zoneId string

@description('URL pattern to match using * wildcards (e.g. example.com/old-page*).')
param urlPattern string

@description('The target URL to redirect matching requests to.')
param redirectUrl string

@description('HTTP status code for the redirect.')
@allowed([301, 302])
param statusCode int = 301

@description('Whether the redirect rule is enabled.')
param enabled bool = true

resource oldPageRedirect 'RedirectRule' = {
  name: 'oldPageRedirect'
  zoneId: zoneId
  urlPattern: urlPattern
  redirectUrl: redirectUrl
  statusCode: statusCode
  enabled: enabled
}

output redirectRuleName string = oldPageRedirect.name
output redirectRuleId string = oldPageRedirect.pageRuleId
output redirectRuleUrlPattern string = oldPageRedirect.urlPattern
output redirectRuleUrl string = oldPageRedirect.redirectUrl
output redirectRuleStatusCode int = oldPageRedirect.statusCode
output redirectRuleEnabled bool = oldPageRedirect.enabled
