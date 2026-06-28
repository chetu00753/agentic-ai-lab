#!/bin/bash

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <project-name>"
    exit 1
fi

PROJECT_NAME="$1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/$PROJECT_NAME"

echo "Creating project '$PROJECT_NAME'..."
dotnet new console -n "$PROJECT_NAME" --output "$PROJECT_DIR"

echo "Copying .env..."
cp "$SCRIPT_DIR/.env" "$PROJECT_DIR/.env"

cd "$PROJECT_DIR"

echo "Installing packages..."
dotnet add package DotNetEnv --version 3.2.0
dotnet add package Microsoft.Extensions.AI --version 10.7.0
dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.7.0
dotnet add package OpenAI --version 2.11.0
dotnet add package Newtonsoft.Json

echo "Writing boilerplate..."
cat > Program.cs << 'EOF'
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

    // Wraps any tool body: catches exceptions and returns them as error strings.
    static string SafeTool(Func<string> fn)
    {
        try { return fn(); }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
    }

    // Sample tool — reads a file from ./data/, sandboxed to that folder.
    AITool readFile = AIFunctionFactory.Create(
        ([Description("Relative path inside ./data/")] string filePath) => SafeTool(() =>
        {
            var safe    = Path.GetFullPath(Path.Combine("data", filePath));
            var allowed = Path.GetFullPath("data");
            if (!safe.StartsWith(allowed))
                return "Error: path escapes allowed directory";
            if (!File.Exists(safe))
                return "Error: file does not exist";
            return File.ReadAllText(safe);
        }),
        name: "read_file",
        description: "Read a file from the ./data/ folder."
    );

    var options = new ChatOptions { Tools = [readFile] };

    var response = await chat.GetResponseAsync("Say hello in one sentence.", options);

    Console.WriteLine(response.Text);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
EOF

echo "Creating data/ folder with sample file..."
mkdir -p data
cat > data/notes.txt << 'NOTES'
Project: Agentic AI Learning Lab
Notes go here.
NOTES

echo ""
echo "Done! To run:"
echo "  cd $PROJECT_NAME && dotnet run"
