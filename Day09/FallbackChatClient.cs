using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Walks a priority-ordered list of (client, label) candidates and falls back
/// to the next one on provider-side errors. Remembers failures between requests:
///   404 → permanent skip (model does not exist at that endpoint)
///   429 / 5xx → 60-second cooldown, then retry
/// Priority: Gemini → OpenRouter → Groq (controlled by list order).
/// </summary>
class FallbackChatClient(IReadOnlyList<(IChatClient Client, string Label)> candidates) : IChatClient
{
    // Index → earliest UTC time to retry. DateTime.MaxValue = skip forever.
    private readonly Dictionary<int, DateTime> _skipUntil = [];
    private const int CooldownSeconds = 60;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        List<Exception> errors = [];

        for (int i = 0; i < candidates.Count; i++)
        {
            var (client, label) = candidates[i];

            if (IsOnCooldown(i, label))
                continue;

            try
            {
                var response = await client.GetResponseAsync(messages, options, cancellationToken);
                Console.WriteLine($"[Provider] {label}");
                return response;
            }
            catch (Exception ex) when (ShouldFallback(ex))
            {
                RecordFailure(i, label, ex);
                errors.Add(ex);
            }
        }

        throw new AggregateException("All providers failed.", errors);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        List<Exception> errors = [];

        for (int i = 0; i < candidates.Count; i++)
        {
            var (client, label) = candidates[i];

            if (IsOnCooldown(i, label))
                continue;

            var buffer = new List<ChatResponseUpdate>();
            bool failed = false;

            try
            {
                await foreach (
                    var update in client.GetStreamingResponseAsync(messages, options, cancellationToken)
                )
                    buffer.Add(update);
            }
            catch (Exception ex) when (ShouldFallback(ex))
            {
                RecordFailure(i, label, ex);
                errors.Add(ex);
                failed = true;
            }

            if (!failed)
            {
                foreach (var u in buffer)
                    yield return u;
                yield break;
            }
        }

        throw new AggregateException("All providers failed.", errors);
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        foreach (var (client, _) in candidates)
        {
            var svc = client.GetService(serviceType, key);
            if (svc is not null)
                return svc;
        }
        return null;
    }

    public void Dispose()
    {
        foreach (var (client, _) in candidates)
            client.Dispose();
    }

    private bool IsOnCooldown(int index, string label)
    {
        if (!_skipUntil.TryGetValue(index, out var until))
            return false;
        if (DateTime.UtcNow >= until)
        {
            _skipUntil.Remove(index);
            return false;
        }
        var remaining = until == DateTime.MaxValue ? "permanent" : $"{(until - DateTime.UtcNow).TotalSeconds:F0}s left";
        Console.WriteLine($"[Skip] {label} ({remaining})");
        return true;
    }

    private void RecordFailure(int index, string label, Exception ex)
    {
        var status = StatusCode(ex);
        if (status == 404)
        {
            _skipUntil[index] = DateTime.MaxValue;
            Console.WriteLine($"[Fallback] {label} → 404, skipping permanently");
        }
        else
        {
            _skipUntil[index] = DateTime.UtcNow.AddSeconds(CooldownSeconds);
            Console.WriteLine($"[Fallback] {label} → {status}, cooling down {CooldownSeconds}s");
        }
    }

    private static int? StatusCode(Exception ex)
    {
        if (ex is ClientResultException cre)
            return cre.Status;
        if (ex.InnerException is ClientResultException inner)
            return inner.Status;
        return null;
    }

    private static bool ShouldFallback(Exception ex)
    {
        var status = StatusCode(ex);
        if (status is int s)
            return s is 404 or 429 or 500 or 502 or 503 or 504;

        var msg = ex.Message;
        return msg.Contains("429")
            || msg.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase);
    }
}
