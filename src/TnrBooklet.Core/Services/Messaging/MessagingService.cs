using Focus.Core.Models;

namespace Focus.Core.Services.Messaging;

public sealed class MessagingService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _http;

    public MessagingService(AppSettings settings, HttpClient? http = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _http = http ?? new HttpClient();
    }

    public async Task<IReadOnlyList<MessageSendResult>> SendAsync(string text, CancellationToken ct = default)
    {
        var bridges = BuildBridges();
        if (bridges.Count == 0)
            return new[] { new MessageSendResult(false, "no channels") };

        var results = new List<MessageSendResult>(bridges.Count);
        foreach (var bridge in bridges)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await bridge.SendAsync(text, ct).ConfigureAwait(false));
        }

        return results;
    }

    public async Task<IReadOnlyList<MessageSendResult>> SendReminderAsync(
        TaskItem task,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!_settings.MessagingOnReminder)
            return Array.Empty<MessageSendResult>();

        var text = TodoListFormatter.FormatReminder(task);
        return await SendAsync(text, ct).ConfigureAwait(false);
    }

    private List<IMessageBridge> BuildBridges()
    {
        var bridges = new List<IMessageBridge>(2);

        if (_settings.TelegramEnabled
            && !string.IsNullOrWhiteSpace(_settings.TelegramBotToken)
            && !string.IsNullOrWhiteSpace(_settings.TelegramChatId))
        {
            bridges.Add(new TelegramMessageBridge(
                _http,
                _settings.TelegramBotToken!,
                _settings.TelegramChatId!));
        }

        if (_settings.DiscordEnabled
            && !string.IsNullOrWhiteSpace(_settings.DiscordWebhookUrl))
        {
            bridges.Add(new DiscordMessageBridge(_http, _settings.DiscordWebhookUrl!));
        }

        return bridges;
    }
}
