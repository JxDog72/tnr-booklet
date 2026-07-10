namespace Focus.Core.Services.Messaging;

public sealed record MessageSendResult(bool Success, string? Error);

public interface IMessageBridge
{
    Task<MessageSendResult> SendAsync(string text, CancellationToken ct = default);
}
