using System.ClientModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

var client = new OpenAIClient(
    new ApiKeyCredential(openRouterKey!),
    new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
);

IChatClient chat = client.GetChatClient("openai/gpt-oss-120b:free").AsIChatClient();

await StreamingWithCost(chat, "What is the SOLID principle?", pricePerMillionTokens: 0.15m);

// Streams the response to console, then prints estimated cost based on token usage.
// pricePerMillionTokens applies to both input and output tokens (use a blended rate or
// the output rate — whichever fits your model's pricing).
static async Task StreamingWithCost(IChatClient chat, string prompt, decimal pricePerMillionTokens)
{
    long inputTokens = 0;
    long outputTokens = 0;

    Console.Write("AI: ");
    await foreach (var chunk in chat.GetStreamingResponseAsync(prompt))
    {
        Console.Write(chunk.Text);

        // Usage arrives as a UsageContent item inside Contents (typically on the final chunk).
        var usageContent = chunk.Contents.OfType<UsageContent>().FirstOrDefault();
        if (usageContent is not null)
        {
            inputTokens = usageContent.Details.InputTokenCount ?? inputTokens;
            outputTokens = usageContent.Details.OutputTokenCount ?? outputTokens;
        }
    }
    Console.WriteLine();

    long totalTokens = inputTokens + outputTokens;
    decimal cost = (totalTokens / 1_000_000m) * pricePerMillionTokens;

    if (totalTokens > 0)
        Console.WriteLine($"[Tokens: {inputTokens} in + {outputTokens} out = {totalTokens} | Cost: ${cost:F6} @ ${pricePerMillionTokens}/M]");
    else
        Console.WriteLine("[Token usage not reported by this model/provider]");
}
