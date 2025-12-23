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
/// Default factory that creates CloudflareApiService instances using environment configuration
/// </summary>
public class CloudflareApiServiceFactory : ICloudflareApiServiceFactory
{
    public ICloudflareApiService Create()
    {
        var config = Configuration.GetConfiguration();
        return new CloudflareApiService(config);
    }
}
