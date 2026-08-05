using Bicep.Local.Extension.Host.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Handlers;

public class CloudflareDnsRecordHandler : TypedResourceHandler<CloudflareDnsRecord, CloudflareDnsRecordIdentifiers>
{
    private readonly ICloudflareApiServiceFactory _apiServiceFactory;

    public CloudflareDnsRecordHandler(ICloudflareApiServiceFactory apiServiceFactory)
    {
        _apiServiceFactory = apiServiceFactory;
    }

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        // For preview, just return the requested configuration without making API calls
        return Task.FromResult(GetResponse(request));
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var apiService = _apiServiceFactory.Create();

            // Use the zone ID provided in the Bicep template
            if (string.IsNullOrEmpty(request.Properties.ZoneId))
            {
                throw new InvalidOperationException($"ZoneId is required for DNS record '{request.Properties.Name}'. Please provide the Cloudflare Zone ID in your Bicep template.");
            }

            if (string.IsNullOrWhiteSpace(request.Properties.RecordId))
            {
                var existingRecord = await apiService.FindDnsRecordAsync(
                    request.Properties,
                    cancellationToken);

                if (existingRecord is not null)
                {
                    request.Properties.RecordId = existingRecord.Id;
                }
            }

            var createdRecord = await apiService.UpsertDnsRecordAsync(request.Properties, cancellationToken);
            
            // Update properties with the created record data
            request.Properties.RecordId = createdRecord.RecordId;
            request.Properties.ZoneId = createdRecord.ZoneId;
            request.Properties.Proxiable = createdRecord.Proxiable;

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create/update DNS record '{request.Properties.Name}' in zone '{request.Properties.ZoneName}': {ex.Message}", ex);
        }
    }

    protected override CloudflareDnsRecordIdentifiers GetIdentifiers(CloudflareDnsRecord properties)
        => new()
        {
            Name = properties.Name,
            ZoneName = properties.ZoneName,
        };
}