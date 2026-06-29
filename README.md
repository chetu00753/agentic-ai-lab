# Agentic AI Lab

A 45-day hands-on learning lab for building agentic AI applications in C# on .NET 10.

Each day is an independent console app in its own folder. Progress notes are in [LOG.md](LOG.md).

## Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 10, C# |
| LLM abstraction | `Microsoft.Extensions.AI` (`IChatClient`) |
| Provider | [OpenRouter](https://openrouter.ai) (free tier) |
| Model | `openai/gpt-oss-120b:free` |
| Key packages | `Microsoft.Extensions.AI.OpenAI`, `OpenAI` v2.11.0, `DotNetEnv`, `Newtonsoft.Json` |

## Running a day's project

```bash
cd Day03 && dotnet run
```

API key goes in a `.env` file at the project root:

```
OPENROUTER_API_KEY=your_key_here
```

## Progress

| Day | Topic | Key concepts |
|---|---|---|
| [Day01](Day01/) | First model call | `OpenAIClient`, `IChatClient`, `GetResponseAsync` |
| [Day02](Day02/) | Multi-turn conversation | `ChatRole` (System/User/Assistant), message history, token tracking |
| [Day03](Day03/) | Structured output | `Temperature`, `GetStructuredAsync<T>`, JSON deserialization |
| [Day04](Day04/) | Claude Code setup | `CLAUDE.md`, custom slash commands, MCP server |
| [Day05](Day05/) | Streaming responses | `GetStreamingResponseAsync`, `IAsyncEnumerable`, `UsageContent`, cost estimation |
| [Day06](Day06/) | Tool calling | `AIFunctionFactory`, `UseFunctionInvocation`, tool loop, parallel & sequential tools |
| [Day07](Day07/) | Tool calling revisited | `DataTable.Compute` for safe arithmetic, `MaximumIterationsPerRequest`, correct `UseFunctionInvocation` overload |
| [Day08](Day08/) | Multi-tool file agent | `read_file` + `search_web` tools, path traversal prevention, `SafeTool` exception wrapper, sandboxed `data/` directory |
| [Day09](Day09/) | Fallback client + write tool | `FallbackChatClient` with permanent-skip (404) and cooldown (429/5xx), multi-provider setup (Gemini/OpenRouter/Groq), `list_files` + `write_file` tools |

## Architecture

```
DayXX/Program.cs  (standalone console app)
    └── IChatClient
            └── FunctionInvokingChatClient  (middleware, Day06+)
                    └── OpenAIChatClient
                            └── OpenRouter API
```

Every `DayXX/` folder is a self-contained `.csproj` — projects do not reference each other. Use `./new-day.sh DayXX` to scaffold a new day.
