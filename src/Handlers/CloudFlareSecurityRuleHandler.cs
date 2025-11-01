using Bicep.Local.Extension.Host.Handlers;
using CloudFlareExtension.Models;
using CloudFlareExtension.Services;

namespace CloudFlareExtension.Handlers;

public class CloudFlareSecurityRuleHandler : TypedResourceHandler<CloudFlareSecurityRule, CloudFlareSecurityRuleIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Properties.ZoneId))
            {
                throw new InvalidOperationException($"ZoneId is required for security rule '{request.Properties.Name}'. Provide the zone ID where the rule should be applied.");
            }

            if (string.IsNullOrWhiteSpace(request.Properties.Description))
            {
                request.Properties.Description = request.Properties.Name;
            }

            if (string.IsNullOrWhiteSpace(request.Properties.Reference))
            {
                request.Properties.Reference = request.Properties.Name.Trim();
            }
            else
            {
                request.Properties.Reference = request.Properties.Reference.Trim();
            }

            var config = Configuration.GetConfiguration();
            using var apiService = new CloudFlareApiService(config);

            if (string.IsNullOrWhiteSpace(request.Properties.RuleId))
            {
                var existingRule = await apiService.FindSecurityRuleAsync(request.Properties.ZoneId, request.Properties, cancellationToken);
                if (existingRule is not null)
                {
                    request.Properties.RuleId = existingRule.Id;
                    if (string.IsNullOrWhiteSpace(request.Properties.FilterId) && existingRule.Filter is not null)
                    {
                        request.Properties.FilterId = existingRule.Filter.Id;
                    }
                    if (!string.IsNullOrWhiteSpace(existingRule.Ref))
                    {
                        request.Properties.Reference = existingRule.Ref;
                    }
                }
            }

            if (!CloudFlareSecurityRuleActions.TryNormalize(request.Properties.Action, out var normalizedAction))
            {
                var supported = string.Join(", ", CloudFlareSecurityRuleActions.SupportedActions);
                throw new InvalidOperationException($"Action '{request.Properties.Action}' is not supported. Allowed values: {supported}.");
            }

            request.Properties.Action = normalizedAction;

            var updatedRule = await apiService.UpsertSecurityRuleAsync(request.Properties, cancellationToken);

            request.Properties.RuleId = updatedRule.RuleId;
            request.Properties.FilterId = updatedRule.FilterId;
            request.Properties.Expression = updatedRule.Expression;
            request.Properties.Action = updatedRule.Action;
            request.Properties.Enabled = updatedRule.Enabled;
            request.Properties.Description = updatedRule.Description;
            request.Properties.Reference = updatedRule.Reference;

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create/update security rule '{request.Properties.Name}' in zone '{request.Properties.ZoneId}': {ex.Message}", ex);
        }
    }

    protected override CloudFlareSecurityRuleIdentifiers GetIdentifiers(CloudFlareSecurityRule properties)
        => new()
        {
            Name = properties.Name,
            ZoneId = properties.ZoneId
        };
}
