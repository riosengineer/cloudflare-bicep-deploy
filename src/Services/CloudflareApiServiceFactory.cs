using CloudflareExtension.Models;

namespace CloudflareExtension.Services;

/// <summary>
/// Factory for creating ICloudflareApiService instances
/// </summary>
public interface ICloudflareApiServiceFactory
{
    /// <summary>
    /// Creates a new ICloudflareApiService instance
    /// </summary>
    ICloudflareApiService Create();
}

/// <summary>
/// Default factory that creates CloudflareApiService instances using IHttpClientFactory
/// </summary>
public class CloudflareApiServiceFactory : ICloudflareApiServiceFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Configuration? _configuration;

    public CloudflareApiServiceFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public CloudflareApiServiceFactory(IHttpClientFactory httpClientFactory, Configuration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public ICloudflareApiService Create()
    {
        var config = _configuration ?? Configuration.GetConfiguration();
        var client = _httpClientFactory.CreateClient("cloudflare");
        return new CloudflareApiService(client, config);
    }
}
