using System.ClientModel;
using System.ComponentModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

try
{
    var client = new OpenAIClient(
        new ApiKeyCredential(openRouterKey!),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
    );

    var model = "openai/gpt-oss-120b:free";

    IChatClient chat = client
        .GetChatClient(model)
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation(
            loggerFactory: null,
            configure: opts => opts.MaximumIterationsPerRequest = 8
        )
        .Build();

    static string SafeTool(Func<string> fn)
    {
        try { return fn(); }
        catch (Exception ex) { return $"ERROR: {ex.Message}"; }
    }

    AITool readFile = AIFunctionFactory.Create(
        ([Description("Relative path inside ./data/")] string filePath) => SafeTool(() =>
        {
            var safe = Path.GetFullPath(Path.Combine("data", filePath));
            var allowed = Path.GetFullPath("data");
            if (!safe.StartsWith(allowed))
                return "ERROR: path escapes allowed directory";
            if (!File.Exists(safe))
                return "ERROR: file does not exist";
            return File.ReadAllText(safe);
        }),
        name: "read_file",
        description: "Read a file from the ./data/ folder."
    );

    // Phase 3: replace stub body with real HTTP call to a search API
    AITool searchWeb = AIFunctionFactory.Create(
        ([Description("Search query string")] string query) => SafeTool(() =>
        {
            return $"""
                [STUB] Fake search results for "{query}":
                1. Example result A — https://example.com/a
                2. Example result B — https://example.com/b
                3. Example result C — https://example.com/c
                """;
        }),
        name: "search_web",
        description: "Search the web and return the top results. (stub — returns fake data)"
    );

    var options = new ChatOptions { Tools = [readFile, searchWeb] };

    var response = await chat.GetResponseAsync(
        "Read my notes.txt and summarise the key points.",
        options
    );

    Console.WriteLine(response.Text);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
