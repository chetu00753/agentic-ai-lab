# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
Sa
This is a learning project for a 45-day Agentic AI roadmap.
Stack: .NET 10, C#, Microsoft.Extensions.AI, OpenRouter as the LLM provider.
Machine: Ubuntu.
Rules:
- Explain what new code does before writing it.
- Use IChatClient for all model calls.
- Never hardcode API keys; always read from environment variables.
- Keep each day's code in its own folder (Day01/, Day02/, etc.).

## What this repo is

A day-by-day learning lab for building agentic AI apps in C# on .NET 10. Each day is an independent console app in its own folder (`Day01/`, `Day03/`, etc.). Progress notes are in `LOG.md`.

## Running and building

```bash
# Run a specific day's project
cd Day03 && dotnet run

# Build without running
cd Day03 && dotnet build

# Create a new day project (copies .env, installs packages, writes boilerplate)
./new-day.sh Day04
```

There are no tests in this repo.

## Architecture

### One project per day
Each `DayXX/` folder is a standalone .NET 10 console app with its own `.csproj`. Projects do not reference each other.

### LLM stack
- **Provider**: [OpenRouter](https://openrouter.ai) (free tier), configured via `OPENROUTER_API_KEY` in `.env`
- **Client abstraction**: `Microsoft.Extensions.AI.IChatClient` — all model calls go through this interface
- **Wiring**: `OpenAIClient` is instantiated with the OpenRouter base URL and key, then `.AsIChatClient()` adapts it to `IChatClient`
- **Model in use**: `openai/gpt-oss-120b:free`

### Key packages (installed by `new-day.sh`)
| Package | Purpose |
|---|---|
| `Microsoft.Extensions.AI.OpenAI` | `IChatClient` abstraction + OpenAI adapter |
| `OpenAI` (v2.11.0) | Underlying OpenAI-compatible SDK |
| `DotNetEnv` | Loads `.env` into `Environment` at startup |
| `Newtonsoft.Json` | JSON deserialization for structured output |

### Patterns introduced per day
- **Day01**: basic single-turn call via `GetResponseAsync`
- **Day02**: multi-turn conversation loop with `ChatRole.System/User/Assistant`, token tracking via `response.Usage`
- **Day03**: structured output via `GetStructuredAsync<T>` extension (defined inline in `Program.cs`) — instructs the model to return raw JSON matching a shape string, then deserialises with `Newtonsoft.Json`

### Environment / secrets
The root `.env` file holds `OPENROUTER_API_KEY`. `new-day.sh` copies it into each new day's folder. Each `Program.cs` calls `Env.Load()` at startup to populate `Environment` variables.
