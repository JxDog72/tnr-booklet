using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Focus.Core.Services.Messaging;

public sealed class TelegramMessageBridge : IMessageBridge
{
    public const int MaxTextLength = 4000;

    private readonly HttpClient _http;
    private readonly string _botToken;
    private readonly string _chatId;

    public TelegramMessageBridge(HttpClient http, string botToken, string chatId)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _botToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
        _chatId = chatId ?? throw new ArgumentNullException(nameof(chatId));
    }

    public async Task<MessageSendResult> SendAsync(string text, CancellationToken ct = default)
    {
        text ??= "";
        if (text.Length > MaxTextLength)
            text = text[..MaxTextLength];

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new TelegramSendMessageRequest
        {
            ChatId = _chatId,
            Text = text,
            DisableWebPagePreview = true
        };

        try
        {
            using var response = await _http.PostAsJsonAsync(url, payload, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return new MessageSendResult(true, null);

            var body = await SafeReadBodyAsync(response, ct).ConfigureAwait(false);
            return new MessageSendResult(false, $"Telegram HTTP {(int)response.StatusCode}: {body}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new MessageSendResult(false, $"Telegram send failed: {ex.Message}");
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

    private sealed class TelegramSendMessageRequest
    {
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }
    }
}
