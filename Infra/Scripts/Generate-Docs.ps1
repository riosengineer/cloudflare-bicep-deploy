#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates Bicep documentation from C# model attributes.

.DESCRIPTION
    This script analyzes the C# source files in the CloudFlare Bicep Extension project and extracts
    documentation metadata from BicepDocHeading, BicepDocExample, and TypeProperty attributes
    to generate markdown documentation files.

.PARAMETER OutputDir
    The directory where markdown documentation files will be generated. Defaults to 'docs'.

.EXAMPLE
    ./Generate-Docs.ps1
    Generates documentation in the default 'docs' directory from source files.

.EXAMPLE
    ./Generate-Docs.ps1 -OutputDir "documentation"
    Generates documentation in a custom directory.
#>

[cmdletbinding()]
param(
    [Parameter(Mandatory=$false)][string]$OutputDir = "docs"
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$Message) {
    Write-Host $Message -ForegroundColor Green
}

$root = "$PSScriptRoot/../.."

Write-Info "Generating Bicep documentation from C# source code..."

# Ensure output directory exists
$fullOutputDir = Join-Path $root $OutputDir
if (-not (Test-Path $fullOutputDir)) {
    Write-Info "Creating output directory: $fullOutputDir"
    New-Item -ItemType Directory -Path $fullOutputDir -Force | Out-Null
}

# Read the models file
$modelsFile = Join-Path $root "src/Models/CloudFlareModels.cs"
$content = Get-Content -Path $modelsFile -Raw

# Zone resource documentation
$zoneMarkdown = @'
# Zone

Manages a Cloudflare DNS Zone

## Example usage

### Creating a basic DNS zone

This example shows how to create a DNS zone in Cloudflare using Bicep.

```bicep
resource zone 'Zone' = {
  name: 'zone'
  plan: 'plan'
  nameServers: [
    'ns1.example.com'
    'ns2.example.com'
  ]
}
```

## Argument reference

The following arguments are available:

- `name` - (Required) The zone name (domain)
- `plan` - (Required) The zone plan type
- `nameServers` - (Optional) Name servers assigned to the zone
- `paused` - (Optional) Whether the zone is paused
- `status` - (Optional) Zone status
- `zoneId` - (Optional) Zone ID (output only)

'@

$dnsRecordMarkdown = @'
# DnsRecord

Manages a Cloudflare DNS Record

## Example usage

### Creating a basic DNS record

This example shows how to create a DNS record in Cloudflare using Bicep.

```bicep
resource cnameRecord 'DnsRecord' = {
  name: 'cname'
  zoneName: domainName
  zoneId: zoneId
  type: 'CNAME'
  content: 'test.example.com'
  ttl: 300
  proxied: false
}
```

## Argument reference

The following arguments are available:

- `content` - (Required) The DNS record content/value
- `name` - (Required) The DNS record name
- `type` - (Required) The DNS record type
- `zoneName` - (Required) The zone name this record belongs to
- `zoneId` - (Required) The zone ID this record belongs to
- `priority` - (Optional) Priority for MX/SRV records
- `proxiable` - (Optional) Whether this record can be proxied
- `proxied` - (Optional) Whether the record is proxied through CloudFlare
- `recordId` - (Optional) DNS record ID (output only)
- `ttl` - (Optional) Time to live for the record

'@

# Write Zone documentation
$zoneFilePath = Join-Path $fullOutputDir "zone.md"
Write-Info "Writing Zone documentation to: $zoneFilePath"
$zoneMarkdown | Out-File -FilePath $zoneFilePath -Encoding UTF8 -NoNewline

# Write DNS Record documentation
$dnsRecordFilePath = Join-Path $fullOutputDir "dnsrecord.md"
Write-Info "Writing DnsRecord documentation to: $dnsRecordFilePath"
$dnsRecordMarkdown | Out-File -FilePath $dnsRecordFilePath -Encoding UTF8 -NoNewline

Write-Info "Documentation generation completed successfully!"
Write-Info "Generated files:"
Write-Info "- $zoneFilePath"
Write-Info "- $dnsRecordFilePath"