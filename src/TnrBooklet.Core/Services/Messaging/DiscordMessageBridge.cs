using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Focus.Core.Services.Messaging;

public sealed class DiscordMessageBridge : IMessageBridge
{
    public const int MaxTextLength = 2000;

    private readonly HttpClient _http;
    private readonly string _webhookUrl;

    public DiscordMessageBridge(HttpClient http, string webhookUrl)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
    }

    public async Task<MessageSendResult> SendAsync(string text, CancellationToken ct = default)
    {
        text ??= "";
        // v1: send first chunk only when over the limit
        if (text.Length > MaxTextLength)
            text = text[..MaxTextLength];

        var payload = new DiscordWebhookPayload { Content = text };

        try
        {
            using var response = await _http.PostAsJsonAsync(_webhookUrl, payload, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return new MessageSendResult(true, null);

            var body = await SafeReadBodyAsync(response, ct).ConfigureAwait(false);
            return new MessageSendResult(false, $"Discord HTTP {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new MessageSendResult(false, $"Discord send failed: {ex.Message}");
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(body))
                return "(empty body)";
            return body.Length <= 200 ? body : body[..200];
        }
        catch
        {
            return "(unreadable body)";
        }
    }

    private sealed class DiscordWebhookPayload
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
