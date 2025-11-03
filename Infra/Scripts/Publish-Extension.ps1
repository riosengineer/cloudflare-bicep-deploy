#!/usr/bin/env pwsh
[cmdletbinding()]
param(
   [Parameter(Mandatory=$true)][string]$Target
)

$ErrorActionPreference = "Stop"

function ExecSafe([scriptblock] $ScriptBlock) {
  & $ScriptBlock
  if ($LASTEXITCODE -ne 0) {
      exit $LASTEXITCODE
  }
}

$root = "$PSScriptRoot/../.."
$extName = "cloudflare-extension"

Write-Host "Building Cloudflare Bicep Extension for multiple platforms..." -ForegroundColor Green

# build various flavors
Write-Host "Building for macOS ARM64..." -ForegroundColor Yellow
ExecSafe { dotnet publish --configuration Release $root -r osx-arm64 }

Write-Host "Building for Linux x64..." -ForegroundColor Yellow
ExecSafe { dotnet publish --configuration Release $root -r linux-x64 }

Write-Host "Building for Windows x64..." -ForegroundColor Yellow
ExecSafe { dotnet publish --configuration Release $root -r win-x64 }

Write-Host "Publishing Cloudflare extension to target: $Target" -ForegroundColor Green

# publish to the registry
ExecSafe { bicep publish-extension `
  --bin-osx-arm64 "$root/src/bin/Release/net9.0/osx-arm64/publish/$extName" `
  --bin-linux-x64 "$root/src/bin/Release/net9.0/linux-x64/publish/$extName" `
  --bin-win-x64 "$root/src/bin/Release/net9.0/win-x64/publish/$extName.exe" `
  --target "$Target" `
  --force 
}

Write-Host "Cloudflare Bicep Extension published successfully!" -ForegroundColor Green