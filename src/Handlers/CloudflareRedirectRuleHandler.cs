using Bicep.Local.Extension.Host.Handlers;
using CloudflareExtension.Models;
using CloudflareExtension.Services;

namespace CloudflareExtension.Handlers;

public class CloudflareRedirectRuleHandler : TypedResourceHandler<CloudflareRedirectRule, CloudflareRedirectRuleIdentifiers>
{
    private readonly ICloudflareApiServiceFactory _apiServiceFactory;

    public CloudflareRedirectRuleHandler(ICloudflareApiServiceFactory apiServiceFactory)
    {
        _apiServiceFactory = apiServiceFactory;
    }

    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Properties.ZoneId))
            {
                throw new InvalidOperationException($"ZoneId is required for redirect rule '{request.Properties.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(request.Properties.UrlPattern))
            {
                throw new InvalidOperationException($"UrlPattern is required for redirect rule '{request.Properties.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(request.Properties.RedirectUrl))
            {
                throw new InvalidOperationException($"RedirectUrl is required for redirect rule '{request.Properties.Name}'.");
            }

            if (request.Properties.StatusCode is not 301 and not 302)
            {
                throw new InvalidOperationException($"StatusCode must be 301 or 302 for redirect rule '{request.Properties.Name}'. Got: {request.Properties.StatusCode}.");
            }

            if (string.IsNullOrWhiteSpace(request.Properties.Description))
            {
                request.Properties.Description = request.Properties.Name;
            }

            using var apiService = _apiServiceFactory.Create();

            if (string.IsNullOrWhiteSpace(request.Properties.PageRuleId))
            {
                var existingRule = await apiService.FindRedirectRuleAsync(request.Properties.ZoneId, request.Properties, cancellationToken);
                if (existingRule is not null)
                {
                    request.Properties.PageRuleId = existingRule.Id;
                }
            }

            var updatedRule = await apiService.UpsertRedirectRuleAsync(request.Properties, cancellationToken);

            request.Properties.PageRuleId = updatedRule.PageRuleId;
            request.Properties.UrlPattern = updatedRule.UrlPattern;
            request.Properties.RedirectUrl = updatedRule.RedirectUrl;
            request.Properties.StatusCode = updatedRule.StatusCode;
            request.Properties.Priority = updatedRule.Priority;
            request.Properties.Enabled = updatedRule.Enabled;

            return GetResponse(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create/update redirect rule '{request.Properties.Name}' in zone '{request.Properties.ZoneId}': {ex.Message}", ex);
        }
    }

    protected override CloudflareRedirectRuleIdentifiers GetIdentifiers(CloudflareRedirectRule properties)
        => new()
        {
            Name = properties.Name,
            ZoneId = properties.ZoneId
        };
}
