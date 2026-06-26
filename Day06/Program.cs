// Scenario 2 — Sequential dependent tools: Customer Support Agent
// The model cannot fetch the order until it knows the customer's ID.
// So it calls get_customer_id first, reads the result, then calls get_latest_order.
// This demonstrates that the model decides the sequence — we just define the tools.

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;

Env.Load();

var openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

// --- Fake in-memory database (simulates real DB calls) ---

var customers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["Alice"] = 9821,
    ["Bob"]   = 1042,
    ["Raj"]   = 204,
};

var orders = new Dictionary<int, object>
{
    [9821] = new { OrderId = "ORD-4421", Item = "Wireless Headphones", Status = "Out for delivery",  Expected = "tomorrow" },
    [1042] = new { OrderId = "ORD-3310", Item = "Mechanical Keyboard",  Status = "Delivered",         Expected = "already delivered on Jun 24" },
    [204]  = new { OrderId = "ORD-5502", Item = "Monitor Stand",        Status = "Processing",        Expected = "3–5 business days" },
};

// ---------------------------------------------------------

try
{
    var client = new OpenAIClient(
        new ApiKeyCredential(openRouterKey!),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") }
    );

    IChatClient chat = new ChatClientBuilder(
            client.GetChatClient("openai/gpt-oss-120b:free").AsIChatClient())
        .UseFunctionInvocation()
        .Build();

    // Tool 1: resolves a customer name → ID.
    // The model must call this first because get_latest_order needs an ID, not a name.
    AITool getCustomerId = AIFunctionFactory.Create(
        ([Description("The customer's full name")] string name) =>
            customers.TryGetValue(name, out var id)
                ? id.ToString()
                : "Customer not found",
        name: "get_customer_id",
        description: "Look up a customer's numeric ID by their full name."
    );

    // Tool 2: fetches the latest order for a given customer ID.
    // Depends on Tool 1's output — the model knows this from the parameter description.
    AITool getLatestOrder = AIFunctionFactory.Create(
        ([Description("The customer's numeric ID, obtained from get_customer_id")] int customerId) =>
            orders.TryGetValue(customerId, out var order)
                ? JsonSerializer.Serialize(order)
                : "No orders found for this customer",
        name: "get_latest_order",
        description: "Get the latest order details for a customer using their numeric ID."
    );


    var options = new ChatOptions { Tools = [getCustomerId, getLatestOrder] };

    Console.WriteLine("Customer Support Agent ready. Ask about any order. Type 'stop' to quit.\n");

    while (true)
    {
        Console.Write("You: ");
        var question = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(question)) continue;
        if (question.Equals("stop", StringComparison.OrdinalIgnoreCase)) break;

        Console.WriteLine("\nAgent is thinking...\n");
        var res = await chat.GetResponseAsync(question, options);
        Console.WriteLine($"Agent: {res.Text}\n");
    }

    Console.WriteLine("Goodbye!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
