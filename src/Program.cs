using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CloudflareExtension.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var config = Configuration.GetConfiguration();
config.Validate();

builder.Services
    .AddHttpClient("cloudflare", client =>
    {
        if (!string.IsNullOrEmpty(config.ApiToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken);
        }
        else if (!string.IsNullOrEmpty(config.ApiKey) && !string.IsNullOrEmpty(config.Email))
        {
            client.DefaultRequestHeaders.Add("X-Auth-Key", config.ApiKey);
            client.DefaultRequestHeaders.Add("X-Auth-Email", config.Email);
        }

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("cloudflare-bicep-extension", version));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/riosengineer/cloudflare-bicep-deploy)"));
    });

builder.Services
    .AddSingleton(config)
    .AddSingleton<ICloudflareApiServiceFactory, CloudflareApiServiceFactory>()
    .AddBicepExtension(
        name: "Cloudflare",
        version: "1.0.0",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<CloudflareZoneHandler>()
    .WithResourceHandler<CloudflareDnsRecordHandler>()
    .WithResourceHandler<CloudflareSecurityRuleHandler>()
    .WithResourceHandler<CloudflareRedirectRuleHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();