using System.ClientModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

// --- Color config ---
var userColor = ConsoleColor.Cyan;
var aiColor   = ConsoleColor.Green;
// --------------------

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

try
{
    var client = new OpenAIClient(
        new ApiKeyCredential(openRouterKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
    );

    var model = "openai/gpt-oss-120b:free";

    IChatClient chat = client
        .GetChatClient(model)
        .AsIChatClient();

    var messages = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.System, "You are a concise .NET coding assistant."),
    };

    var sessionInputTokens = 0;
    var sessionOutputTokens = 0;

    while (true)
    {
        Console.ForegroundColor = userColor;
        Console.Write("You: ");
        Console.ResetColor();
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
            break;

        messages.Add(new ChatMessage(ChatRole.User, userInput));

        var response = await chat.GetResponseAsync(messages);

        Console.ForegroundColor = aiColor;
        Console.WriteLine($"AI: {response.Text}");
        Console.ResetColor();

        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        sessionInputTokens += (int)inputTokens;
        sessionOutputTokens += (int)outputTokens;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            $"[This response — In: {inputTokens} | Out: {outputTokens}]  "
            + $"[Session total — In: {sessionInputTokens} | Out: {sessionOutputTokens} | Total: {sessionInputTokens + sessionOutputTokens}]"
        );
        Console.ResetColor();

        messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
