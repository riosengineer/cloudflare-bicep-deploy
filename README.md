# CloudFlare Bicep Extension (Experimental)

A custom Azure Bicep extension for creating CloudFlare DNS resources through Infrastructure as Code (IaC). [Check this out to learn how to create your own .NET Bicep extension](https://techcommunity.microsoft.com/blog/azuregovernanceandmanagementblog/create-your-own-bicep-local-extension-using-net/4439967)

## 🚀 Overview

This project provides a Bicep extension that enables you to create CloudFlare DNS records directly from your Azure Bicep templates.

> **Note:** This is an experimental Bicep feature and is subject to change. Do not use it in production.

## ⚡ Current Capabilities

Experimental / sample only. Limited functionality for now:

- Create CloudFlare DNS Records (A, AAAA, CNAME, MX, TXT, SRV, PTR, NS, CAA)
- Manage DNS record properties (content, TTL, proxied status)
- Support for multiple CloudFlare zones

See the [Sample](Sample/) folder for an example Bicep template.

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Bicep CLI](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install)

## 📋 Bicep Usage Example

```bicep
targetScope = 'local'
extension CloudFlare

@description('Domain name for the DNS record')
param domainName string = 'example.com'

@description('CloudFlare Zone ID for the domain')
param zoneId string

@description('Test value for the TXT record')
param testValue string = 'hello'

// Create TXT Record in CloudFlare
resource txtRecord 'DnsRecord' = {
  name: 'txtRecord'
  zoneName: domainName
  zoneId: zoneId
  type: 'TXT'
  content: testValue
  ttl: 300
  proxied: false
}

output recordId string = txtRecord.id
output recordName string = txtRecord.name
```

For comprehensive usage examples, please refer to the [`Sample/`](Sample/) directory in this repository.

## Local Development or Azure Container Registry

Here are the steps to run it either locally or using an ACR.

### Local build

Run script `Publish-Extension.ps1` from the folder [Infra/Scripts/](Infra/Scripts) to publish the project and to publish the extension locally for Bicep to use:

```powershell
./Infra/Scripts/Publish-Extension.ps1 -Target ./cloudflare-extension
```

This creates the binary that contains the CloudFlare API calls. Prepare your `bicepconfig.json` to refer to the binary. Set `experimentalFeaturesEnabled` -> `localDeploy` to `true` and refer the extension `cloudflare` to the binary:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
    "CloudFlare": "../bin/cloudflare" // local
  },
  "implicitExtensions": []
}
```

Run `bicep local-deploy main.bicepparam` to test the extension locally. Also, see the example in the [Sample](Sample/) folder.

### Azure Container Registry build

If you want to make use of an Azure Container Registry then I would recommend to fork the project, and run the GitHub Actions. Or, run the [Bicep template](Infra/main.bicep) for the ACR deployment locally and then push it using the same principal:

```powershell
[string] $target = "br:<registry-name>.azurecr.io/cloudflare:<version>"

./Infra/Scripts/Publish-Extension.ps1 -Target $target
```

In the `bicepconfig.json` you refer to the ACR:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
      "CloudFlare": "br:cloudflarebicep.azurecr.io/cloudflare:0.1.0" // ACR
    // "CloudFlare": "../bin/cloudflare" // local
  },
  "implicitExtensions": []
}
```

### Public ACR

If you want to try it out without effort, then you can use `br:cloudflarebicep.azurecr.io/extensions/cloudflare:1.0.0` as the ACR reference which I have published.

### CloudFlare API Setup

You will need to create a CloudFlare API token from the [CloudFlare API Tokens page](https://dash.cloudflare.com/profile/api-tokens).

- Create Custom Token
- Permissions: Zone - DNS - Edit
- Zone Resources: Include - Specific Zone - Your Domain
- Save and make a note of the API Token
- Make this an enviornment variable `CLOUDFLARE_API_TOKEN` locally (`$env:CLOUDFLARE_API_TOKEN = "here"`), or as a GitHub enviornment secret if running in a pipeline so that `bicep local-deploy` will authenticate successfully.

## 🤝 Contributing

We welcome contributions to the CloudFlare Bicep Extension! Please see our [Contributing Guide](CONTRIBUTING.md) for detailed information on how to contribute to this project.
