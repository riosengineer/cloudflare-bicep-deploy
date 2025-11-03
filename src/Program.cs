using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CloudFlareExtension.Handlers;
using CloudFlareExtension.Models;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "Cloudflare",
        version: "1.0.0",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<CloudFlareZoneHandler>()
    .WithResourceHandler<CloudFlareDnsRecordHandler>()
    .WithResourceHandler<CloudFlareSecurityRuleHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();