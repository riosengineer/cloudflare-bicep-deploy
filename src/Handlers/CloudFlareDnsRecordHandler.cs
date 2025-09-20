using Bicep.Local.Extension.Host.Handlers;
using CloudFlareExtension.Models;
using CloudFlareExtension.Services;

namespace CloudFlareExtension.Handlers;

public class CloudFlareDnsRecordHandler : TypedResourceHandler<CloudFlareDnsRecord, CloudFlareDnsRecordIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        // For preview, just return the requested configuration without making API calls
        return Task.FromResult(GetResponse(request));
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var config = Configuration.GetConfiguration();
            using var apiService = new CloudFlareApiService(config);

            // Use the zone ID provided in the Bicep template
            if (string.IsNullOrEmpty(request.Properties.ZoneId))
            {
                throw new InvalidOperationException($"ZoneId is required for DNS record '{request.Properties.Name}'. Please provide the CloudFlare Zone ID in your Bicep template.");
            }

            // Create the DNS record
            var createdRecord = await apiService.CreateDnsRecordAsync(request.Properties, request.Properties.ZoneId, cancellationToken);
            
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

    protected override CloudFlareDnsRecordIdentifiers GetIdentifiers(CloudFlareDnsRecord properties)
        => new()
        {
            Name = properties.Name,
            ZoneName = properties.ZoneName,
        };
}