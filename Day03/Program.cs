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
