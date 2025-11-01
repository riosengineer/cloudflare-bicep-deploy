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

            var normalizedInputZone = request.Properties.ZoneName?.Trim().TrimEnd('.') ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Properties.ZoneId))
            {
                var zone = await apiService.GetZoneAsync(normalizedInputZone, cancellationToken);
                if (zone is not null && !string.Equals(zone.Name, normalizedInputZone, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Zone name '{request.Properties.ZoneName}' does not match the CloudFlare zone '{zone.Name}' (ID {request.Properties.ZoneId}).");
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

    protected override CloudFlareDnsRecordIdentifiers GetIdentifiers(CloudFlareDnsRecord properties)
        => new()
        {
            Name = properties.Name,
            ZoneName = properties.ZoneName,
        };
}