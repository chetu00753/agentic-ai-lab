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

