using Microsoft.Extensions.AI;

namespace Suppliers.IntegrationTests.Infrastructure;

/// <summary>Stands in for the real model: returns canned text (or throws), and records how often it was called.</summary>
public sealed class FakeChatClient : IChatClient
{
    private Func<ChatResponse> _respond = () => new ChatResponse(new ChatMessage(ChatRole.Assistant, "{}"));

    public int CallCount { get; private set; }

    public void RespondWith(string text) =>
        _respond = () => new ChatResponse(new ChatMessage(ChatRole.Assistant, text));

    public void Throw(Exception exception) => _respond = () => throw exception;

    public void Reset()
    {
        RespondWith("{}");
        CallCount = 0;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_respond());
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
