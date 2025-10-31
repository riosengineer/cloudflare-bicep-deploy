using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using CloudFlareExtension.Models;

namespace CloudFlareExtension.Services;

/// <summary>
/// CloudFlare API Service for making authenticated requests to CloudFlare API
/// </summary>
public class CloudFlareApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Configuration _config;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions ApiSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CloudFlareApiService(Configuration config)
    {
        _config = config;
        _config.Validate();

        _baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
            ? "https://api.cloudflare.com/client/v4"
            : _config.BaseUrl.TrimEnd('/');

        _httpClient = new HttpClient();
        // Don't set BaseAddress - use full URLs instead

        // Only set the essential Authorization header
        if (!string.IsNullOrEmpty(_config.ApiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiToken);
        }
        else if (!string.IsNullOrEmpty(_config.ApiKey) && !string.IsNullOrEmpty(_config.Email))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Email", _config.Email);
        }
    }

    /// <summary>
    /// Creates a new CloudFlare zone
    /// </summary>
    public async Task<CloudFlareZone> CreateZoneAsync(CloudFlareZone zone, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            name = zone.Name,
            plan = new { id = zone.Plan },
            jump_start = false // You can make this configurable
        };

        var response = await _httpClient.PostAsJsonAsync(BuildUrl("/zones"), request, cancellationToken);
        response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<CloudFlareApiResponse<CloudFlareZoneApiResult>>(ApiSerializerOptions, cancellationToken);
        
        if (result?.Success != true)
        {
            throw new InvalidOperationException($"CloudFlare API error: {string.Join(", ", result?.Errors?.Select(e => e.Message) ?? ["Unknown error"])}");
        }

        zone.ZoneId = result.Result.Id;
        zone.Status = result.Result.Status;
        zone.NameServers = result.Result.NameServers;
        
        return zone;
    }

    /// <summary>
    /// Gets an existing CloudFlare zone by name
    /// </summary>
    public async Task<CloudFlareZone?> GetZoneAsync(string zoneName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(BuildUrl($"/zones?name={Uri.EscapeDataString(zoneName)}"), cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to get zone '{zoneName}': {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

    var result = await response.Content.ReadFromJsonAsync<CloudFlareApiResponse<CloudFlareZoneApiResult[]>>(ApiSerializerOptions, cancellationToken);
        
        if (result?.Success != true || result.Result?.Length == 0)
        {
            return null;
        }

        var apiZone = result.Result![0];
        return new CloudFlareZone
        {
            Name = apiZone.Name,
            ZoneId = apiZone.Id,
            Status = apiZone.Status,
            Plan = "free",
            NameServers = apiZone.NameServers,
            Paused = apiZone.Paused
        };
    }

    /// <summary>
    /// Creates a new DNS record
    /// </summary>
    public async Task<CloudFlareDnsRecord> CreateDnsRecordAsync(CloudFlareDnsRecord record, string zoneId, CancellationToken cancellationToken = default)
    {
        // Build request data - include priority for MX and SRV records
        var requestData = new Dictionary<string, object>
        {
            { "name", record.Name },
            { "ttl", record.Ttl },
            { "type", record.Type },
            { "content", record.Content },
            { "proxied", record.Proxied }
        };

        // Add priority for record types that require it
        if (record.Type == "MX" || record.Type == "SRV")
        {
            requestData.Add("priority", record.Priority);
        }

        // Add comment if provided
        if (!string.IsNullOrEmpty(record.Comment))
        {
            requestData.Add("comment", record.Comment);
        }

        var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        // Use the exact URL that you confirmed works
    var fullUrl = BuildUrl($"/zones/{zoneId}/dns_records");
        
        var response = await _httpClient.PostAsync(fullUrl, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create DNS record: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<CloudFlareApiResponse<CloudFlareDnsRecordApiResult>>(responseJson, ApiSerializerOptions);
        
        if (result?.Success != true)
        {
            throw new InvalidOperationException($"CloudFlare API error: {string.Join(", ", result?.Errors?.Select(e => e.Message) ?? ["Unknown error"])}");
        }

        // Map API response back to our model
        record.RecordId = result.Result.Id;
        record.ZoneId = zoneId;
        record.Proxiable = result.Result.Proxiable;
        
        // Map comment back if it was returned
        if (!string.IsNullOrEmpty(result.Result.Comment))
        {
            record.Comment = result.Result.Comment;
        }
        
        return record;
    }

    public async Task<CloudFlareFirewallRule> UpsertFirewallRuleAsync(CloudFlareFirewallRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(rule.ZoneId))
        {
            throw new InvalidOperationException($"ZoneId is required to manage firewall rule '{rule.Name}'.");
        }

        var payload = BuildFirewallRulePayload(rule);

        if (!string.IsNullOrWhiteSpace(rule.RuleId))
        {
            var response = await SendJsonAsync(HttpMethod.Put,
                $"/zones/{rule.ZoneId}/firewall/rules/{rule.RuleId}",
                payload,
                cancellationToken);

            var apiRule = await DeserializeApiResponseAsync<CloudFlareFirewallRuleApiResult>(response, cancellationToken);
            return MapApiFirewallRule(rule, apiRule);
        }
        else
        {
            var response = await SendJsonAsync(HttpMethod.Post,
                $"/zones/{rule.ZoneId}/firewall/rules",
                new[] { payload },
                cancellationToken);

            var apiRules = await DeserializeApiResponseAsync<CloudFlareFirewallRuleApiResult[]>(response, cancellationToken);
            if (apiRules.Length == 0)
            {
                throw new InvalidOperationException("CloudFlare API returned no firewall rules in the response.");
            }

            return MapApiFirewallRule(rule, apiRules[0]);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private Dictionary<string, object?> BuildFirewallRulePayload(CloudFlareFirewallRule rule)
    {
        var description = string.IsNullOrWhiteSpace(rule.Description) ? rule.Name : rule.Description;

        var filter = new Dictionary<string, object?>
        {
            ["expression"] = rule.Expression,
            ["paused"] = !rule.Enabled,
            ["description"] = description
        };

        if (!string.IsNullOrWhiteSpace(rule.FilterId))
        {
            filter["id"] = rule.FilterId;
        }

        return new Dictionary<string, object?>
        {
            ["action"] = rule.Action,
            ["description"] = description,
            ["paused"] = !rule.Enabled,
            ["filter"] = filter
        };
    }

    private async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string relativePath, object payload, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, BuildUrl(relativePath))
        {
            Content = CreateJsonContent(payload)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"CloudFlare API request to '{relativePath}' failed: {response.StatusCode} - {error}");
        }

        return response;
    }

    private static StringContent CreateJsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload, ApiSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private static async Task<T> DeserializeApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<CloudFlareApiResponse<T>>(ApiSerializerOptions, cancellationToken);

        if (apiResponse is null)
        {
            throw new InvalidOperationException("CloudFlare API returned an empty response.");
        }

        if (apiResponse.Success != true)
        {
            var errors = apiResponse.Errors is { Length: > 0 }
                ? string.Join(", ", apiResponse.Errors.Select(e => e.Message))
                : "Unknown error";
            throw new InvalidOperationException($"CloudFlare API error: {errors}");
        }

        return apiResponse.Result;
    }

    private static CloudFlareFirewallRule MapApiFirewallRule(CloudFlareFirewallRule rule, CloudFlareFirewallRuleApiResult apiRule)
    {
        rule.RuleId = apiRule.Id;
        rule.Action = apiRule.Action;
        rule.Description = apiRule.Description;

        if (apiRule.Filter is not null)
        {
            rule.FilterId = apiRule.Filter.Id;
            rule.Expression = apiRule.Filter.Expression;
            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                rule.Description = apiRule.Filter.Description;
            }
            rule.Enabled = !(apiRule.Paused || apiRule.Filter.Paused);
        }
        else
        {
            rule.Enabled = !apiRule.Paused;
        }

        return rule;
    }

    private string BuildUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return _baseUrl;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (!relativePath.StartsWith('/'))
        {
            relativePath = "/" + relativePath;
        }

        return _baseUrl + relativePath;
    }
}

// CloudFlare API response models
public class CloudFlareApiResponse<T>
{
    public bool Success { get; set; }
    public T Result { get; set; } = default!;
    public CloudFlareApiError[]? Errors { get; set; }
}

public class CloudFlareApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CloudFlareZoneApiResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Paused { get; set; }
    public string[] NameServers { get; set; } = [];
}

public class CloudFlareDnsRecordApiResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Ttl { get; set; }
    public bool Proxied { get; set; }
    public bool Proxiable { get; set; }
    public int Priority { get; set; }
    public string? Comment { get; set; }
}

public class CloudFlareFirewallRuleApiResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("filter")]
    public CloudFlareFirewallRuleFilterApiResult? Filter { get; set; }
}

public class CloudFlareFirewallRuleFilterApiResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}