using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LexiFlow.Api.Dtos;
using LexiFlow.Api.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LexiFlow.Api.Services;

public class OcrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OcrClient(HttpClient httpClient, IOptions<OcrOptions> options, ILogger<OcrClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.ApiBase.TrimEnd('/'));
        _logger = logger;
    }

    public async Task<OcrExtractResponseDto?> ExtractAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("OCR extract called with missing file {FilePath}", filePath);
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/ocr/extract")
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OCR service returned {Status}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<OcrResponse>(payload, JsonOptions);
            if (parsed is null)
            {
                _logger.LogWarning("Unable to deserialize OCR payload: {Payload}", payload);
                return null;
            }

            if (!DateOnly.TryParse(parsed.InvoiceDate, out var invoiceDate))
            {
                _logger.LogWarning("Unable to parse invoice date {InvoiceDate}", parsed.InvoiceDate);
                invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            return new OcrExtractResponseDto(
                parsed.Vendor,
                invoiceDate,
                parsed.Total,
                parsed.Vat,
                parsed.Currency ?? "EUR",
                parsed.RawText ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call OCR service.");
            return null;
        }
    }

    private sealed record OcrResponse(
        string Vendor,
        string InvoiceDate,
        decimal Total,
        decimal Vat,
        string? Currency,
        string? RawText
    );
}
