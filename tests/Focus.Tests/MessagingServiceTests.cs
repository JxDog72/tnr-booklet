using System.Net;
using System.Text;
using Focus.Core.Models;
using Focus.Core.Services.Messaging;
using FluentAssertions;

namespace Focus.Tests;

public class MessagingServiceTests
{
    [Fact]
    public async Task SendAsync_telegram_enabled_posts_to_api_telegram_org()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"ok":true}""");
        using var http = new HttpClient(handler);
        var settings = new AppSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "test-token",
            TelegramChatId = "12345"
        };

        var service = new MessagingService(settings, http);
        var results = await service.SendAsync("hello focus");

        results.Should().ContainSingle();
        results[0].Success.Should().BeTrue();
        results[0].Error.Should().BeNull();

        handler.Requests.Should().ContainSingle();
        var request = handler.Requests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.Host.Should().Be("api.telegram.org");
        request.Uri.AbsolutePath.Should().Contain("/bottest-token/sendMessage");
        request.Body.Should().Contain("12345");
        request.Body.Should().Contain("hello focus");
        request.Body.Should().Contain("disable_web_page_preview");
    }

    [Fact]
    public async Task SendAsync_telegram_401_returns_success_false()
    {
        var handler = new RecordingHandler(HttpStatusCode.Unauthorized, """{"ok":false,"description":"Unauthorized"}""");
        using var http = new HttpClient(handler);
        var settings = new AppSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "bad-token",
            TelegramChatId = "12345"
        };

        var service = new MessagingService(settings, http);
        var results = await service.SendAsync("secret message");

        results.Should().ContainSingle();
        results[0].Success.Should().BeFalse();
        results[0].Error.Should().NotBeNullOrWhiteSpace();
        results[0].Error.Should().Contain("401");
    }

    [Fact]
    public async Task SendAsync_no_channels_returns_failure()
    {
        using var http = new HttpClient(new RecordingHandler(HttpStatusCode.OK, "{}"));
        var service = new MessagingService(new AppSettings(), http);

        var results = await service.SendAsync("hello");

        results.Should().ContainSingle();
        results[0].Success.Should().BeFalse();
        results[0].Error.Should().Be("no channels");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public RecordingHandler(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
        }

        public List<(HttpMethod Method, Uri Uri, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add((request.Method, request.RequestUri!, body));

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
