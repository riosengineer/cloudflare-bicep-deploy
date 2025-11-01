using System.Net;
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
    /// Creates or updates a DNS record.
    /// </summary>
    public async Task<CloudFlareDnsRecord> UpsertDnsRecordAsync(CloudFlareDnsRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.ZoneId))
        {
            throw new InvalidOperationException($"ZoneId is required to manage DNS record '{record.Name}'.");
        }

        var payload = BuildDnsRecordPayload(record);
        HttpResponseMessage response;

        if (!string.IsNullOrWhiteSpace(record.RecordId))
        {
            response = await SendJsonAsync(HttpMethod.Put,
                $"/zones/{record.ZoneId}/dns_records/{record.RecordId}",
                payload,
                cancellationToken);
        }
        else
        {
            response = await SendJsonAsync(HttpMethod.Post,
                $"/zones/{record.ZoneId}/dns_records",
                payload,
                cancellationToken);
        }

        var apiRecord = await DeserializeApiResponseAsync<CloudFlareDnsRecordApiResult>(response, cancellationToken);
        return MapApiDnsRecord(record, apiRecord);
    }

    public async Task<CloudFlareDnsRecordApiResult?> FindDnsRecordAsync(CloudFlareDnsRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.ZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.ZoneName);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.Type);

        foreach (var candidateName in GetRecordLookupNames(record))
        {
            var query = new Dictionary<string, string>
            {
                ["name"] = candidateName,
                ["type"] = record.Type,
                ["per_page"] = "1"
            };

            var match = await QuerySingleAsync<CloudFlareDnsRecordApiResult>($"/zones/{record.ZoneId}/dns_records", query, cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public async Task<CloudFlareSecurityRuleApiResult?> FindSecurityRuleAsync(string zoneId, CloudFlareSecurityRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        ArgumentNullException.ThrowIfNull(rule);

        if (!string.IsNullOrWhiteSpace(rule.RuleId))
        {
            var response = await _httpClient.GetAsync(BuildUrl($"/zones/{zoneId}/firewall/rules/{rule.RuleId}"), cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"CloudFlare API request to '/zones/{zoneId}/firewall/rules/{rule.RuleId}' failed: {response.StatusCode} - {errorText}");
            }

            return await DeserializeApiResponseAsync<CloudFlareSecurityRuleApiResult>(response, cancellationToken);
        }

        foreach (var query in GetSecurityRuleLookupQueries(rule))
        {
            var match = await QuerySingleAsync<CloudFlareSecurityRuleApiResult>(
                $"/zones/{zoneId}/firewall/rules",
                query,
                cancellationToken,
                allowClientErrors: true);
            if (match is not null)
            {
                return match;
            }
        }

        var listQuery = new Dictionary<string, string>
        {
            ["per_page"] = "500"
        };

        var listPath = BuildPath($"/zones/{zoneId}/firewall/rules", listQuery);
        var listResponse = await _httpClient.GetAsync(BuildUrl(listPath), cancellationToken);

        if (!listResponse.IsSuccessStatusCode)
        {
            var error = await listResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"CloudFlare API request to '{listPath}' failed: {listResponse.StatusCode} - {error}");
        }

        var rules = await DeserializeApiResponseAsync<CloudFlareSecurityRuleApiResult[]>(listResponse, cancellationToken);

        return rules.FirstOrDefault(candidate =>
            (!string.IsNullOrWhiteSpace(candidate.Description) && string.Equals(candidate.Description, rule.Name, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(candidate.Description) && string.Equals(candidate.Description, rule.Description, StringComparison.OrdinalIgnoreCase)) ||
            (candidate.Filter is not null && !string.IsNullOrWhiteSpace(candidate.Filter.Description) && string.Equals(candidate.Filter.Description, rule.Name, StringComparison.OrdinalIgnoreCase)) ||
            (candidate.Filter is not null && !string.IsNullOrWhiteSpace(candidate.Filter.Expression) && string.Equals(candidate.Filter.Expression, rule.Expression, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<CloudFlareSecurityRule> UpsertSecurityRuleAsync(CloudFlareSecurityRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(rule.ZoneId))
        {
            throw new InvalidOperationException($"ZoneId is required to manage security rule '{rule.Name}'.");
        }

        if (!string.IsNullOrWhiteSpace(rule.RuleId))
        {
            var payload = BuildSecurityRulePayload(rule, includeRef: false);
            var response = await SendJsonAsync(HttpMethod.Put,
                $"/zones/{rule.ZoneId}/firewall/rules/{rule.RuleId}",
                payload,
                cancellationToken);

            var apiRule = await DeserializeApiResponseAsync<CloudFlareSecurityRuleApiResult>(response, cancellationToken);
            return MapApiSecurityRule(rule, apiRule);
        }
        else
        {
            var payload = BuildSecurityRulePayload(rule, includeRef: true);
            var response = await SendJsonAsync(HttpMethod.Post,
                $"/zones/{rule.ZoneId}/firewall/rules",
                new[] { payload },
                cancellationToken);

            var apiRules = await DeserializeApiResponseAsync<CloudFlareSecurityRuleApiResult[]>(response, cancellationToken);
            if (apiRules.Length == 0)
            {
                throw new InvalidOperationException("CloudFlare API returned no security rules in the response.");
            }

            return MapApiSecurityRule(rule, apiRules[0]);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private Dictionary<string, object?> BuildSecurityRulePayload(CloudFlareSecurityRule rule, bool includeRef)
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

        var payload = new Dictionary<string, object?>
        {
            ["action"] = rule.Action,
            ["description"] = description,
            ["paused"] = !rule.Enabled,
            ["filter"] = filter
        };

        if (includeRef && !string.IsNullOrWhiteSpace(rule.Name))
        {
            payload["ref"] = rule.Name;
        }

        return payload;
    }

    private Dictionary<string, object?> BuildDnsRecordPayload(CloudFlareDnsRecord record)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = NormalizeDnsRecordName(record.Name, record.ZoneName),
            ["ttl"] = record.Ttl,
            ["type"] = record.Type,
            ["content"] = record.Content,
            ["proxied"] = record.Proxied
        };

        if (string.Equals(record.Type, "MX", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(record.Type, "SRV", StringComparison.OrdinalIgnoreCase))
        {
            payload["priority"] = record.Priority;
        }

        if (!string.IsNullOrWhiteSpace(record.Comment))
        {
            payload["comment"] = record.Comment;
        }

        return payload;
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

    private static CloudFlareDnsRecord MapApiDnsRecord(CloudFlareDnsRecord record, CloudFlareDnsRecordApiResult apiRecord)
    {
        record.RecordId = apiRecord.Id;
        record.Content = apiRecord.Content;
        record.Proxied = apiRecord.Proxied;
        record.Proxiable = apiRecord.Proxiable;
        record.Ttl = apiRecord.Ttl;

        if (!string.IsNullOrEmpty(apiRecord.Comment))
        {
            record.Comment = apiRecord.Comment;
        }

        if (string.Equals(record.Type, "MX", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(record.Type, "SRV", StringComparison.OrdinalIgnoreCase))
        {
            record.Priority = apiRecord.Priority;
        }

        return record;
    }

    private static CloudFlareSecurityRule MapApiSecurityRule(CloudFlareSecurityRule rule, CloudFlareSecurityRuleApiResult apiRule)
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

    private static IEnumerable<Dictionary<string, string>> GetSecurityRuleLookupQueries(CloudFlareSecurityRule rule)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in EnumerateSecurityRuleDescriptions(rule))
        {
            if (seen.Add(candidate))
            {
                yield return new Dictionary<string, string>
                {
                    ["per_page"] = "1",
                    ["description"] = candidate
                };
            }
        }
    }

    private static IEnumerable<string> EnumerateSecurityRuleDescriptions(CloudFlareSecurityRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.Name))
        {
            yield return rule.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(rule.Description))
        {
            yield return rule.Description.Trim();
        }
    }

    private static IEnumerable<string> GetRecordLookupNames(CloudFlareDnsRecord record)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var zone = NormalizeZoneName(record.ZoneName);

        var canonical = NormalizeDnsRecordName(record.Name, record.ZoneName);
        if (seen.Add(canonical))
        {
            yield return canonical;
        }

        var trimmed = record.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            trimmed = trimmed.TrimEnd('.');
            if (seen.Add(trimmed))
            {
                yield return trimmed;
            }
        }

        if (IsRootRecordName(record.Name, zone) && seen.Add(zone))
        {
            yield return zone;
        }
    }

    private static bool IsRootRecordName(string? recordName, string zone)
    {
        if (string.IsNullOrWhiteSpace(zone))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(recordName))
        {
            return true;
        }

        recordName = recordName.Trim();

        if (recordName == "@")
        {
            return true;
        }

        return string.Equals(recordName.TrimEnd('.'), zone, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDnsRecordName(string? recordName, string zoneName)
    {
        var zone = NormalizeZoneName(zoneName);

        if (string.IsNullOrWhiteSpace(recordName) || recordName == "@")
        {
            return zone;
        }

        var trimmed = recordName.Trim().TrimEnd('.');

        if (string.Equals(trimmed, zone, StringComparison.OrdinalIgnoreCase))
        {
            return zone;
        }

        if (!string.IsNullOrEmpty(zone) && trimmed.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return string.IsNullOrEmpty(zone) ? trimmed : $"{trimmed}.{zone}";
    }

    private static string NormalizeZoneName(string? zoneName)
    {
        return string.IsNullOrWhiteSpace(zoneName)
            ? string.Empty
            : zoneName.Trim().TrimEnd('.');
    }

    private async Task<T?> QuerySingleAsync<T>(string basePath, Dictionary<string, string> queryParameters, CancellationToken cancellationToken, bool allowClientErrors = false)
    {
        var path = BuildPath(basePath, queryParameters);
        var response = await _httpClient.GetAsync(BuildUrl(path), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (allowClientErrors && (int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"CloudFlare API request to '{path}' failed: {response.StatusCode} - {error}");
        }

        var results = await DeserializeApiResponseAsync<T[]>(response, cancellationToken);
        return results.Length > 0 ? results[0] : default;
    }

    private static string BuildPath(string basePath, Dictionary<string, string> queryParameters)
    {
        if (queryParameters is null || queryParameters.Count == 0)
        {
            return basePath;
        }

        var query = BuildQueryString(queryParameters);
        return string.IsNullOrWhiteSpace(query) ? basePath : $"{basePath}?{query}";
    }

    private static string BuildQueryString(Dictionary<string, string> parameters)
        => string.Join("&", parameters
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

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

public class CloudFlareSecurityRuleApiResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("filter")]
    public CloudFlareSecurityRuleFilterApiResult? Filter { get; set; }
}

public class CloudFlareSecurityRuleFilterApiResult
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