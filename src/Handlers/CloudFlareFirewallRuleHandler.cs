using Bicep.Local.Extension.Host.Handlers;
using CloudFlareExtension.Models;
using CloudFlareExtension.Services;

namespace CloudFlareExtension.Handlers;

public class CloudFlareFirewallRuleHandler : TypedResourceHandler<CloudFlareFirewallRule, CloudFlareFirewallRuleIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Properties.ZoneId))
            {
                throw new InvalidOperationException($"ZoneId is required for firewall rule '{request.Properties.Name}'. Provide the zone ID where the rule should be applied.");
            }

            if (!CloudFlareFirewallRuleActions.TryNormalize(request.Properties.Action, out var normalizedAction))
            {
                var supported = string.Join(", ", CloudFlareFirewallRuleActions.SupportedActions);
                throw new InvalidOperationException($"Action '{request.Properties.Action}' is not supported. Allowed values: {supported}.");
            }

            request.Properties.Action = normalizedAction;

            if (string.IsNullOrWhiteSpace(request.Properties.Description))
            {
                request.Properties.Description = request.Properties.Name;
            }

            var config = Configuration.GetConfiguration();
            using var apiService = new CloudFlareApiService(config);

            var updatedRule = await apiService.UpsertFirewallRuleAsync(request.Properties, cancellationToken);

            request.Properties.RuleId = updatedRule.RuleId;
            request.Properties.FilterId = updatedRule.FilterId;
            request.Properties.Expression = updatedRule.Expression;
            request.Properties.Action = updatedRule.Action;
            request.Properties.Enabled = updatedRule.Enabled;
            request.Properties.Description = updatedRule.Description;

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create/update firewall rule '{request.Properties.Name}' in zone '{request.Properties.ZoneId}': {ex.Message}", ex);
        }
    }

    protected override CloudFlareFirewallRuleIdentifiers GetIdentifiers(CloudFlareFirewallRule properties)
        => new()
        {
            Name = properties.Name,
            ZoneId = properties.ZoneId
        };
}
