using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CloudFlareExtension.Models;

namespace CloudFlareExtension.Services;

/// <summary>
/// CloudFlare API Service for making authenticated requests to CloudFlare API
/// </summary>
public class CloudFlareApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Configuration _config;

    public CloudFlareApiService(Configuration config)
    {
        _config = config;
        _config.Validate();

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

        var response = await _httpClient.PostAsJsonAsync("/zones", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CloudFlareApiResponse<CloudFlareZoneApiResult>>(cancellationToken);
        
        if (result?.Success != true)
        {
            throw new InvalidOperationException($"CloudFlare API error: {string.Join(", ", result?.Errors?.Select(e => e.Message) ?? ["Unknown error"])}");
        }

        // Map API response back to our model
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
        var response = await _httpClient.GetAsync($"/zones?name={Uri.EscapeDataString(zoneName)}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to get zone '{zoneName}': {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var result = await response.Content.ReadFromJsonAsync<CloudFlareApiResponse<CloudFlareZoneApiResult[]>>(cancellationToken);
        
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
            Plan = "free", // You might want to map this properly
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

        var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        // Use the exact URL that you confirmed works
        var fullUrl = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records";
        
        var response = await _httpClient.PostAsync(fullUrl, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create DNS record: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CloudFlareApiResponse<CloudFlareDnsRecordApiResult>>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        if (result?.Success != true)
        {
            throw new InvalidOperationException($"CloudFlare API error: {string.Join(", ", result?.Errors?.Select(e => e.Message) ?? ["Unknown error"])}");
        }

        // Map API response back to our model
        record.RecordId = result.Result.Id;
        record.ZoneId = zoneId;
        record.Proxiable = result.Result.Proxiable;
        
        return record;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
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
}