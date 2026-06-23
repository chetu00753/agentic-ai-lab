using System.ClientModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using OpenAI;

Env.Load();

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

try
{
    var client = new OpenAIClient(
        new ApiKeyCredential(openRouterKey!),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
    );

    IChatClient chat = client.GetChatClient("openai/gpt-oss-120b:free").AsIChatClient();

    var chatOptions = new ChatOptions { Temperature = 0f, MaxOutputTokens = 200 };

    var bookSummary = await chat.GetStructuredAsync<BookSummary>(
        "{ \"Title\": string, \"Author\": string, \"Summary\": string }",
        "Summarise: Clean Code by Robert Martin.",
        chatOptions
    );
    Console.WriteLine($"Title: {bookSummary.Title}");
    Console.WriteLine($"Author: {bookSummary.Author}");
    Console.WriteLine($"Summary: {bookSummary.Summary}");

    Console.WriteLine();

    var concept = await chat.GetStructuredAsync<TechConcept>(
        "{ \"Name\": string, \"OneLiner\": string, \"Example\": string }",
        "Explain the Dependency Injection design pattern.",
        chatOptions
    );
    Console.WriteLine($"Concept: {concept.Name}");
    Console.WriteLine($"What: {concept.OneLiner}");
    Console.WriteLine($"Example: {concept.Example}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

public record BookSummary(string Title, string Author, string Summary);
public record TechConcept(string Name, string OneLiner, string Example);

public static class ChatClientExtensions
{
    // Instructs the model to return JSON matching `jsonShape`, then deserialises to T.
    // Throws JsonSerializationException if the model returns unparseable output.
    public static async Task<T> GetStructuredAsync<T>(
        this IChatClient chat,
        string jsonShape,
        string prompt,
        ChatOptions? options = null
    )
    {
        var fullPrompt =
            $"Return ONLY valid JSON (no markdown, no explanation) matching this shape: {jsonShape}.\n{prompt}";

        var response = await chat.GetResponseAsync(fullPrompt, options);

        var raw = response.Text.Trim().TrimStart('`').Replace("json", "").Trim('`');

        return JsonConvert.DeserializeObject<T>(raw)
            ?? throw new JsonSerializationException(
                $"Model returned null or empty JSON for type {typeof(T).Name}. Raw: {raw}"
            );
    }
}
