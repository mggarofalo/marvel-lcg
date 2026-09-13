using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>Posts JSON telemetry once per envelope without retrying.</summary>
public sealed class HttpTelemetryExporter : ITelemetryExporter, IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly HttpClient client;
    private readonly Uri endpoint;

    /// <summary>Creates an exporter for an operator-approved endpoint.</summary>
    public HttpTelemetryExporter(Uri endpoint, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!IsAllowedEndpoint(endpoint))
        {
            throw new ArgumentException(
                "telemetry endpoint requires HTTPS or loopback HTTP without credentials, query, or fragment",
                nameof(endpoint));
        }

        this.endpoint = endpoint;
        client = handler is null
            ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            : new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(2);
    }

    /// <summary>Checks the transport and credential boundary used by exporters.</summary>
    public static bool IsAllowedEndpoint(Uri endpoint) =>
        endpoint.IsAbsoluteUri
        && endpoint.OriginalString.Length <= 2048
        && endpoint.UserInfo.Length == 0
        && endpoint.Query.Length == 0
        && endpoint.Fragment.Length == 0
        && endpoint.Scheme is "https" or "http"
        && (endpoint.Scheme != "http" || endpoint.IsLoopback);

    /// <inheritdoc />
    public void Export(TelemetryEnvelope envelope)
    {
        using HttpResponseMessage response = client.PostAsJsonAsync(
            endpoint, envelope, Options).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
