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

    AITool calc = AIFunctionFactory.Create(
        ([Description("The arithmetic expression to evaluate")] string expression) =>
            // DataTable.Compute safely evaluates basic arithmetic without running arbitrary code
            new System.Data.DataTable()
                .Compute(expression, null)
                .ToString(),
        name: "calculator",
        description: "Evaluate a basic arithmetic expression like '3 + 4 * 2'."
    );

    AITool date = AIFunctionFactory.Create(
        () => DateTime.Today.ToString("yyyy-MM-dd"),
        name: "get_current_date",
        description: "Returns today's date."
    );

    var options = new ChatOptions { Tools = [calc, date] };
    var res = await chat.GetResponseAsync("Tell me today's date and what 12 * 8 is.", options);
    Console.WriteLine(res.Text);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
