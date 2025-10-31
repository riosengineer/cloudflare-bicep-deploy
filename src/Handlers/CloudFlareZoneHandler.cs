using Bicep.Local.Extension.Host.Handlers;
using CloudFlareExtension.ConsoleOutput;
using CloudFlareExtension.Models;
using CloudFlareExtension.Services;

namespace CloudFlareExtension.Handlers;

public class CloudFlareZoneHandler : TypedResourceHandler<CloudFlareZone, CloudFlareZoneIdentifiers>
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

            SpectreConsoleReporter.WriteInfo($"Ensuring CloudFlare zone '{request.Properties.Name}' exists.");

            // Check if zone already exists
            var existingZone = await apiService.GetZoneAsync(request.Properties.Name, cancellationToken);
            
            if (existingZone != null)
            {
                // Zone exists, update our properties with the existing zone data
                request.Properties.ZoneId = existingZone.ZoneId;
                request.Properties.Status = existingZone.Status;
                request.Properties.NameServers = existingZone.NameServers;
                request.Properties.Paused = existingZone.Paused;

                SpectreConsoleReporter.WriteWarning($"Zone '{request.Properties.Name}' already exists. Skipping creation.");
                SpectreConsoleReporter.RenderZoneSummary(request.Properties, existedPrior: true);
            }
            else
            {
                // Create new zone
                var createdZone = await SpectreConsoleReporter.RunOperationAsync(
                    $"Create CloudFlare zone '{request.Properties.Name}'",
                    ct => apiService.CreateZoneAsync(request.Properties, ct),
                    cancellationToken);
                
                // Update properties with the created zone data
                request.Properties.ZoneId = createdZone.ZoneId;
                request.Properties.Status = createdZone.Status;
                request.Properties.NameServers = createdZone.NameServers;

                SpectreConsoleReporter.WriteSuccess($"Created CloudFlare zone '{request.Properties.Name}'.");
                SpectreConsoleReporter.RenderZoneSummary(request.Properties, existedPrior: false);
            }

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create/update CloudFlare zone '{request.Properties.Name}': {ex.Message}", ex);
        }
    }

    protected override CloudFlareZoneIdentifiers GetIdentifiers(CloudFlareZone properties)
        => new()
        {
            Name = properties.Name,
        };
}