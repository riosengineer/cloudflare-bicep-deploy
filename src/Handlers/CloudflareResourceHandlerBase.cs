using System.Net.Http.Headers;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using CloudflareExtension.Models;

namespace CloudflareExtension.Handlers;

public abstract class CloudflareResourceHandlerBase<TResource, TIdentifiers> : TypedResourceHandler<TResource, TIdentifiers>
    where TResource : class, TIdentifiers
    where TIdentifiers : class
{
    protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected static HttpClient CreateClient(Configuration configuration)
    {
        var client = new HttpClient();
        
        // Set up Cloudflare API authentication
        var apiToken = configuration.ApiToken ?? Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN");
        var apiKey = configuration.ApiKey ?? Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY");
        var email = configuration.Email ?? Environment.GetEnvironmentVariable("CLOUDFLARE_EMAIL");

        if (!string.IsNullOrEmpty(apiToken))
        {
            // Use API Token (preferred method)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        }
        else if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(email))
        {
            // Use API Key + Email (legacy method)
            client.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);
            client.DefaultRequestHeaders.Add("X-Auth-Email", email);
        }
        else
        {
            throw new InvalidOperationException("Cloudflare authentication not configured. Please provide either API Token or API Key + Email.");
        }

        client.DefaultRequestHeaders.Add("User-Agent", "Cloudflare-Bicep-Extension/0.1.0");
        return client;
    }

    protected override TIdentifiers GetIdentifiers(TResource resource)
    {
        return resource;
    }
}