using Bicep.Local.Extension.Host.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Handlers;

public class CloudflareZoneHandler : TypedResourceHandler<CloudflareZone, CloudflareZoneIdentifiers>
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
            using var apiService = new CloudflareApiService(config);

            // Check if zone already exists
            var existingZone = await apiService.GetZoneAsync(request.Properties.Name, cancellationToken);
            
            if (existingZone != null)
            {
                // Zone exists, update our properties with the existing zone data
                request.Properties.ZoneId = existingZone.ZoneId;
                request.Properties.Status = existingZone.Status;
                request.Properties.NameServers = existingZone.NameServers;
                request.Properties.Paused = existingZone.Paused;

            }
            else
            {
                // Create new zone
                var createdZone = await apiService.CreateZoneAsync(request.Properties, cancellationToken);
                
                // Update properties with the created zone data
                request.Properties.ZoneId = createdZone.ZoneId;
                request.Properties.Status = createdZone.Status;
                request.Properties.NameServers = createdZone.NameServers;

            }

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create/update Cloudflare zone '{request.Properties.Name}': {ex.Message}", ex);
        }
    }

    protected override CloudflareZoneIdentifiers GetIdentifiers(CloudflareZone properties)
        => new()
        {
            Name = properties.Name,
        };
}