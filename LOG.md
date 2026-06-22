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
