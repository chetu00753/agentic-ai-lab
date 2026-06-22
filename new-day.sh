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
dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.7.0
dotnet add package OpenAI --version 2.11.0
dotnet add package Newtonsoft.Json

echo "Writing boilerplate..."
cat > Program.cs << 'EOF'
using System.ClientModel;
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

    IChatClient chat = client
        .GetChatClient("openai/gpt-oss-120b:free")
        .AsIChatClient();

    var response = await chat.GetResponseAsync("Say hello in one sentence.");

    Console.WriteLine(response.Text);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
EOF

echo ""
echo "Done! To run:"
echo "  cd $PROJECT_NAME && dotnet run"
