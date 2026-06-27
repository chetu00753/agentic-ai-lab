Day 01:

Learned how to call model from code.

Steps-
Got open router API key from open router for learning coz its free and enough for learning.
Placed it in .env file

Created a console application.
Installed OpenAI SDK and Microsoft.Extensions.AI.OpenAI packages,
Installed DotEnv package to read the api key from env.

Created OpenAIClient using api key and configured the end point to call.

created IChatClient client.

set the model to call.

called the model using a simple question using GetResponseAsync

log the response.

Issues Faced and fixed-
API not loaded properly.
Response json handling.
added try catch block to read the exception message.
Got 429 from some models becuase they were busy.

-----------------------------------------------------------------------------------------------------------------------------------------

Day 02:

Learned about the different chat roles in a session.

System - to set the context or rules which will be sent in each request.
User - End user, here it's me.
Assistant - AI Model.

Learned how to set the System context using system persona.

used Readline to get the User input from console instead of hardcoding. added rge user input to message list using User persona.

used a while loop to iterate the conversation.

got the assistant response, added it to the message list under assitant persona.

in the next loop, whole previous conversation sent with the request, preserving it as a context memory.

Learned about token usage, added console log to show the token usage for each loop, total tokens , both ins and outs.

Extra:
Wrote a shell script to create a new project folder with boilerplate started code for each day.

-----------------------------------------------------------------------------------------------------------------------------------------

Day 03:

Learned about chat option configurations,

Temperature: 0 means deterministic, 1 creative.

Learned how to read structured output from model response.

Added a helper method to get the response T from model.

Handled json string invalid format issues.

-----------------------------------------------------------------------------------------------------------------------------------------

Day 04:

Setup claude code in my system.

Learned about CLAUDE.md file and how it works.

Created a custom command /explain to explain the particular code block.

Learned how to prompt to claude code, how to creately configure CLAUDE.md file to get the better results,
how to create custom commands, how to connect MCP server to Caude code so it can read my project files.

-----------------------------------------------------------------------------------------------------------------------------------------

Day 05:

Learned about streaming responses from the model.

instead of waiting for the full response, the model sends it chunk by chunk and we print each chunk as it arrives using GetStreamingResponseAsync which returns IAsyncEnumerable.

used await foreach to iterate over the chunks and write each chunk's text to console in real time.

Learned about token usage in streaming responses.

in MEA 10.7.0, there is no .Usage property on the chunk directly. usage is delivered as a UsageContent object inside chunk.Contents, typically on the final chunk.

used OfType<UsageContent>().FirstOrDefault() to extract it from the Contents list.

accessed input and output token counts from usageContent.Details.

Added a StreamingWithCost() helper method that streams to console and prints estimated cost at the end.

takes pricePerMillionTokens as a decimal parameter, calculates cost as (totalTokens / 1_000_000) * price.

handles the case where provider doesn't return usage data and prints a fallback message.

-----------------------------------------------------------------------------------------------------------------------------------------

Day 06:

Learned about Tool Calling (Function Calling) — how to give the AI real capabilities by letting it call C# functions on my machine.

The AI itself cannot do math, access the internet, read files, or call databases. Tools are how you plug those capabilities in.

How it works-
Defined a tool using AIFunctionFactory.Create(), wrapping a C# lambda.
AIFunctionFactory reads the [Description] attributes on parameters and builds a JSON schema that gets sent to the model.
The model reads the schema and decides on its own when to call the tool and with what arguments.
The actual C# code never leaves my machine — the model only sees the name, description, and parameter types.

The tool loop-
Trip 1: my question + tool schema sent to model. Model replies with a tool call (not text). res.Text is empty at this point.
Trip 2: MEAI runs the C# function on my machine, sends the result back to the model.
Trip 3: Model reads the result and writes the final plain-English answer.

Without UseFunctionInvocation() middleware, this loop does not happen — the program just gets the raw tool call response and stops, so res.Text is empty.

Added UseFunctionInvocation() by installing the Microsoft.Extensions.AI package (it lives there, not in Abstractions).

Pipeline-
My code → FunctionInvocationChatClient (middleware) → OpenAIChatClient → OpenRouter model

The middleware owns the loop. It keeps sending tool results back until the model returns finish_reason = stop.

Key things learned-
Parallel tool calling: model can fire multiple tool calls in one response when they are independent.
Sequential tool calling: model calls Tool A, reads its result, then calls Tool B using that result. The model figures out the dependency from the parameter descriptions.
Error handling: if a tool throws, MEAI catches it and sends the error string back to the model. The model then decides to retry or tell the user. No automatic retry in the framework itself.
Infinite loop protection: FunctionInvocationChatClient has a MaximumIterationsPerRequest cap (default 128). Can be lowered to control runaway API costs.

Wrote Scenario 2 — Customer Support Agent:
Two tools: get_customer_id(name) and get_latest_order(customerId).
Model calls them sequentially because the second depends on the first.
Added a while loop so the agent keeps running until the user types "stop".

Detailed notes and Q&A from today's session are in Day06/README.md — covers the tool loop explained simply, real-world scenarios (parallel tools, dependent tools, DB calls, 3rd party APIs, error handling, infinite loop protection).

-----------------------------------------------------------------------------------------------------------------------------------------

Day 07:

Revisited Tool Calling from Day06 with a cleaner, minimal setup.

Built a single-file agent with two tools:
- calculator: uses System.Data.DataTable.Compute() to safely evaluate arithmetic expressions like "3 + 4 * 2" without running arbitrary code.
- get_current_date: returns today's date as a formatted string.

Key thing practiced — configuring MaximumIterationsPerRequest properly.

The correct overload in Microsoft.Extensions.AI 10.7.0 is:
UseFunctionInvocation(ILoggerFactory, Action<FunctionInvokingChatClient>)

Passing the configure lambda as the first argument causes a compile error (CS1660) because it gets matched to ILoggerFactory. Fix is to use named parameters:
.UseFunctionInvocation(loggerFactory: null, configure: opts => opts.MaximumIterationsPerRequest = 8)

Learned what MaximumIterationsPerRequest actually protects against:
The middleware runs a loop — send → tool call → result → send again. A confused model can loop forever with no final answer.
MaximumIterationsPerRequest caps the number of round-trips. On hitting the limit, the middleware stops and returns whatever the model last produced, preventing runaway API cost.
Rule of thumb: set it slightly above the maximum legitimate tool calls your flow needs.

-----------------------------------------------------------------------------------------------------------------------------------------

