using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LexiFlow.Api.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexiFlow.Api.Services;

public class LexOfficeClient
{
    private readonly HttpClient _httpClient;
    private readonly LexOfficeOptions _options;
    private readonly ILogger<LexOfficeClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LexOfficeClient(HttpClient httpClient, IOptions<LexOfficeOptions> options, ILogger<LexOfficeClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_options.ApiBase.TrimEnd('/'));
    }

    public async Task<string?> CreateVoucherAsync(VoucherRequest payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vouchers");
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LexOffice mock returned {Status}", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<VoucherResponse>(body, JsonOptions);
            return parsed?.VoucherId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LexOffice mock.");
            return null;
        }
    }

    public sealed record VoucherRequest(string Vendor, DateOnly Date, decimal Total, decimal Vat, string Currency, string? RawText);

    private sealed record VoucherResponse(string VoucherId);
}
