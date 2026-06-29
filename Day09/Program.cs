using System.ClientModel;
using System.ComponentModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

Env.Load();

var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

try
{
    var candidates = BuildCandidates(geminiKey, openRouterKey, groqKey);
    if (candidates.Count == 0)
        throw new InvalidOperationException(
            "No API keys found. Set at least one of GEMINI_API_KEY, OPENROUTER_API_KEY, GROQ_API_KEY in .env"
        );

    using var logFactory = LoggerFactory.Create(b =>
        b.AddConsole().SetMinimumLevel(LogLevel.Warning)
    );

    TokenCountingChatClient? tokenCounter = null;

    IChatClient chat = new FallbackChatClient(candidates)
        .AsBuilder()
        .UseLogging(logFactory)
        .UseFunctionInvocation(
            loggerFactory: null,
            configure: opts => opts.MaximumIterationsPerRequest = 10
        )
        .Use(inner =>
        {
            tokenCounter = new TokenCountingChatClient(inner);
            return tokenCounter;
        })
        .Build();

    // Sandboxes all file operations to ./data/
    static string DataPath(string filePath)
    {
        var safe = Path.GetFullPath(Path.Combine("data", filePath));
        var allowed = Path.GetFullPath("data");
        if (!safe.StartsWith(allowed))
            throw new InvalidOperationException("Path escapes allowed directory.");
        return safe;
    }

    static string SafeTool(Func<string> fn)
    {
        try
        {
            return fn();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    AITool listFiles = AIFunctionFactory.Create(
        () =>
            SafeTool(() =>
            {
                var files = Directory
                    .GetFiles("data")
                    .Select(Path.GetFileName)
                    .Where(f => f is not null)
                    .ToArray();
                return files.Length == 0 ? "No files found." : string.Join("\n", files);
            }),
        name: "list_files",
        description: "List all files in the ./data/ folder."
    );

    AITool readFile = AIFunctionFactory.Create(
        ([Description("File name inside ./data/ (e.g. notes.txt)")] string fileName) =>
            SafeTool(() => File.ReadAllText(DataPath(fileName))),
        name: "read_file",
        description: "Read the contents of a file in the ./data/ folder."
    );

    AITool writeFile = AIFunctionFactory.Create(
        (
            [Description("File name to create inside ./data/ (e.g. report.txt)")] string fileName,
            [Description("Text content to write into the file")] string content
        ) =>
            SafeTool(() =>
            {
                File.WriteAllText(DataPath(fileName), content);
                return $"Written {content.Length} characters to {fileName}.";
            }),
        name: "write_file",
        description: "Write text content to a file in the ./data/ folder. Creates or overwrites the file."
    );

    var options = new ChatOptions { Tools = [listFiles, readFile, writeFile] };

    var messages = new List<ChatMessage>
    {
        new(
            ChatRole.System,
            "You are a helpful assistant with access to a small file system. "
                + "Use your tools to answer questions accurately."
        ),
        new(
            ChatRole.User,
            "List the files in the data folder, read each one, "
                + "then write a report.txt that summarises the key information you found across all files."
        ),
    };

    Console.WriteLine("=== Starting 3-tool conversation ===\n");

    var response = await chat.GetResponseAsync(messages, options);
    Console.WriteLine($"\nAssistant: {response.Text}");

    messages.AddRange(response.Messages);
    messages.Add(
        new ChatMessage(
            ChatRole.User,
            "Good. Now read back report.txt so I can confirm it looks right."
        )
    );

    Console.WriteLine("\n--- Second turn ---\n");

    response = await chat.GetResponseAsync(messages, options);
    Console.WriteLine($"\nAssistant: {response.Text}");

    if (tokenCounter is not null)
        LogUsageToCsv(tokenCounter.TotalIn, tokenCounter.TotalOut);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Builds the priority-ordered candidate list: Gemini → OpenRouter → Groq.
// Skips any provider whose key is missing.
static IReadOnlyList<(IChatClient Client, string Label)> BuildCandidates(
    string? geminiKey,
    string? openRouterKey,
    string? groqKey
)
{
    var list = new List<(IChatClient, string)>();

    if (!string.IsNullOrWhiteSpace(geminiKey))
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(geminiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/"),
            }
        );
        foreach (
            var model in new[]
            {
                "gemini-2.0-flash",
                "gemini-2.0-flash-lite",
                "gemini-2.5-flash-preview-05-20",
            }
        )
            list.Add((client.GetChatClient(model).AsIChatClient(), $"Gemini/{model}"));
    }

    if (!string.IsNullOrWhiteSpace(openRouterKey))
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(openRouterKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
        );
        foreach (
            var model in new[]
            {
                "meta-llama/llama-3.3-70b-instruct:free",
                "openai/gpt-oss-120b:free",
                "nvidia/nemotron-3-super-120b-a12b:free",
                "google/gemma-4-31b-it:free",
                "qwen/qwen3-next-80b-a3b-instruct:free",
            }
        )
            list.Add((client.GetChatClient(model).AsIChatClient(), $"OpenRouter/{model}"));
    }

    if (!string.IsNullOrWhiteSpace(groqKey))
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(groqKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") }
        );
        foreach (
            var model in new[] { "llama-3.3-70b-versatile", "llama3-70b-8192", "gemma2-9b-it" }
        )
            list.Add((client.GetChatClient(model).AsIChatClient(), $"Groq/{model}"));
    }

    return list;
}

static void LogUsageToCsv(long totalIn, long totalOut)
{
    var csvPath = Path.GetFullPath(Path.Combine("..", "token_usage.csv"));
    var day = Path.GetFileName(Directory.GetCurrentDirectory());
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    if (!File.Exists(csvPath))
        File.AppendAllText(csvPath, "Timestamp,Day,InputTokens,OutputTokens,TotalTokens\n");

    File.AppendAllText(csvPath, $"{timestamp},{day},{totalIn},{totalOut},{totalIn + totalOut}\n");

    Console.WriteLine(
        $"\n[Usage logged → token_usage.csv | in: {totalIn}, out: {totalOut}, total: {totalIn + totalOut}]"
    );
}

class TokenCountingChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public long TotalIn { get; private set; }
    public long TotalOut { get; private set; }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        if (response.Usage is { } u)
        {
            TotalIn += u.InputTokenCount ?? 0;
            TotalOut += u.OutputTokenCount ?? 0;
            Console.WriteLine(
                $"[Tokens] request — in: {u.InputTokenCount}, out: {u.OutputTokenCount} | "
                    + $"session total — in: {TotalIn}, out: {TotalOut}"
            );
        }

        return response;
    }
}
