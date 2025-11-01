targetScope = 'local'

extension CloudFlare

@description('Domain name for the DNS record samples')
param domainName string = 'rios.engineer'

@description('CloudFlare Zone ID for the domain')
@minLength(32)
@maxLength(32)
param zoneId string

@description('Test value for the TXT record')
param testValue string = 'Hello from Bicep CloudFlare Extension!'

// Create TXT Record in CloudFlare
resource txtRecord 'DnsRecord' = {
  name: 'txtRecord'
  zoneName: domainName
  zoneId: zoneId
  type: 'TXT'
  content: testValue
  ttl: 300
  proxied: false
  comment: 'TXT record for domain verification'
}

// Create A Record pointing to an IP
resource aRecord 'DnsRecord' = {
  name: 'aRecord'
  zoneName: domainName
  zoneId: zoneId
  type: 'A'
  content: '192.168.1.101'
  ttl: 300
  proxied: false
  comment: 'A record for primary server'
}

// Create CNAME Record pointing to another domain
resource cnameRecord 'DnsRecord' = {
  name: 'cname'
  zoneName: domainName
  zoneId: zoneId
  type: 'CNAME'
  content: 'test.example.com'
  ttl: 300
  proxied: false
  comment: 'CNAME record for aliasing'
}

// Create AAAA Record (IPv6)
resource aaaaRecord 'DnsRecord' = {
  name: 'aaaaRecord'
  zoneName: domainName
  zoneId: zoneId
  type: 'AAAA'
  content: '2001:db8::2'
  ttl: 300
  proxied: false
  comment: 'AAAA record for IPv6 address'
}

// Create MX Record for mail
resource mxRecord 'DnsRecord' = {
  name: 'mxRecord'
  zoneName: domainName
  zoneId: zoneId
  type: 'MX'
  content: 'mail2.example.com'
  priority: 20
  ttl: 300
  proxied: false
  comment: 'Mail exchange record for mail routing'
}

// Outputs
output txtRecordName string = txtRecord.name
output txtRecordContent string = txtRecord.content
output aRecordName string = aRecord.name
output aRecordContent string = aRecord.content
output cnameRecordName string = cnameRecord.name
output cnameRecordContent string = cnameRecord.content
output aaaaRecordName string = aaaaRecord.name
output aaaaRecordContent string = aaaaRecord.content
output mxRecordName string = mxRecord.name
output mxRecordContent string = mxRecord.content
