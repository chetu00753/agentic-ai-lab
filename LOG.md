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


