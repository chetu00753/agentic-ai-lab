using System.ClientModel;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

try
{
    var client = new OpenAIClient(
        new ApiKeyCredential(openRouterKey),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
    );

    var model="openai/gpt-oss-120b:free";

    IChatClient chat = client
        .GetChatClient(model)
        .AsIChatClient();

    var response = await chat.GetResponseAsync("What is agentic AI? Give me one liner answer.");

    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
