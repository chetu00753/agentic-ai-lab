# Day 06 — Teaching the AI to Use Tools (Function Calling)

## What this program does

We ask the AI: **"What is 238 × 17?"**

The AI does NOT calculate it itself. Instead, it calls a **calculator tool** we gave it, gets the answer, and then tells us the result in a sentence.

---

## Explain it like I'm 6

Imagine you are talking to a very smart friend (the AI). You ask:

> "Hey, what is 238 times 17?"

Your friend is great at talking but not great at math. So you also hand them a **calculator** before you ask the question. You say:

> "Here, use this calculator if you need it."

Here is what happens next:

---

### Round 1 — You ask the question

You ask: *"What is 238 × 17?"*

Your friend looks at it and thinks:
> "I should use that calculator!"

So your friend writes a little note back to you:

> "Please press these buttons on the calculator: **238 * 17**"

Your friend has NOT answered your question yet. They just told you what to type into the calculator.

---

### Round 2 — You press the calculator buttons

You (actually the program, not you-you) take that note, press **238 * 17** on the calculator, and get **4046**.

You write the result on a new piece of paper and hand BOTH papers back to your friend:

> Paper 1 (your original question): "What is 238 × 17?"
> Paper 2 (assistant's note): "Use the calculator with 238 * 17"
> Paper 3 (calculator result): "The calculator said: 4046"

---

### Round 3 — Your friend finally answers

Now your friend reads all three papers and says:

> "238 × 17 = 4,046."

That's the sentence that gets printed on screen. Done!

---

## Why does it need 2 trips to the AI?

Because the AI cannot run code. It can only read and write words. So:

- **Trip 1:** AI reads your question → writes "please run the calculator"
- **Trip 2:** AI reads question + calculator result → writes the final answer

Without Trip 2, the AI would have no idea what the calculator said.

---

## What `UseFunctionInvocation()` does

This is the magic piece. Without it, your program would receive the AI's "please press these calculator buttons" note and just... stop. `res.Text` would be empty.

`UseFunctionInvocation()` adds a helper that sits between your code and the AI. That helper:

1. Sees the "please run the calculator" note
2. Actually runs the calculator for you
3. Sends the result back to the AI automatically
4. Waits for the AI's real text answer
5. Gives you THAT as the final response

So from YOUR code's point of view, you called `GetResponseAsync` once and got a sentence back. The helper handled all the back-and-forth invisibly.

---

## The pipeline (who talks to who)

```
Your code
   |
   v
FunctionInvocationChatClient   <-- the helper from UseFunctionInvocation()
   |                               it loops until AI stops calling tools
   v
OpenAIChatClient               <-- translates to HTTP calls
   |
   v
OpenRouter / AI model          <-- the actual brain
```

---

## Key pieces of code

| Thing | What it is |
|---|---|
| `AIFunctionFactory.Create(...)` | Wraps your C# lambda as a tool the AI can "call" |
| `ChatOptions { Tools = [calc] }` | Tells the AI "you have access to this tool" |
| `UseFunctionInvocation()` | Adds the helper that runs the loop automatically |
| `res.Text` | The final plain-text answer after all tool calls are done |

---

## Q&A — Questions asked while learning

---

### Q: What is the need of tools? And does the calculator run on my machine or in the AI cloud?

**Why tools exist:**

The AI is just a text-in, text-out machine. It is very good at understanding language, reasoning, and writing — but it has hard limits:

- It **cannot do precise math** (it guesses, and often gets it wrong)
- It **cannot access the internet**
- It **cannot read your files**
- It **cannot call your database**
- It **cannot know what time it is right now**

Tools are how you plug real capabilities into the AI. The AI stays in charge of the conversation and decides *when* to use a tool, but the tool itself runs in your program.

**Where the calculator actually runs:**

The calculator runs entirely on **your machine**. The AI never touches your code.

```
Your Machine                          OpenRouter Cloud
────────────────────────────────      ──────────────────
Program.cs runs
  │
  ├─► sends question + tool schema ──────────────────►  AI model
  │                                                       │
  │◄── returns: "call calculator('238 * 17')" ◄──────────┘
  │
  ├─► YOUR CODE runs this line:      ← happens here, on your machine
  │     new DataTable().Compute("238 * 17", null)
  │     result = "4046"
  │
  ├─► sends result "4046" ───────────────────────────►  AI model
  │                                                       │
  │◄── returns: "238 × 17 = 4,046." ◄────────────────────┘
  │
  └─► prints the answer
```

The AI only ever sees **text**. It sends you a text message saying "please run calculator with this input". Your program runs the real C# code, gets the real number, and sends that number back as text.

**What the AI actually receives (the tool schema):**

When you write `AIFunctionFactory.Create(...)`, MEAI converts your lambda into a JSON description and sends it to the AI in the first request:

```json
{
  "name": "calculator",
  "description": "Evaluate a basic arithmetic expression like '3 + 4 * 2'.",
  "parameters": {
    "expression": { "type": "string", "description": "The arithmetic expression to evaluate" }
  }
}
```

The AI reads this and decides on its own whether to use the tool. Your actual C# code never leaves your machine — the AI only ever sees the name and description.

**Simple mental model — the hotel concierge:**

| Who | Role |
|---|---|
| You (the user) | Ask the question |
| AI model | The concierge — understands you, decides what to do |
| Tool (calculator) | The taxi driver — does the actual job, on your machine |
| Your machine | The city where the taxi actually drives |

The concierge never drives the taxi. The taxi never chats with you. They each do their own job.

---

### Q: Can the AI call multiple tools in one response?

Yes. This is called **parallel tool calling**.

Instead of one tool call in the message, the AI can return a list of them — all in the same reply, before waiting for any results.

**Example:** if you asked *"What is 238 * 17 and what is 99 + 44?"*, the model might respond in Trip 1 with:

```json
[
  { "id": "call_1", "name": "calculator", "arguments": { "expression": "238 * 17" } },
  { "id": "call_2", "name": "calculator", "arguments": { "expression": "99 + 44" } }
]
```

Both calls come back at once. Then `FunctionInvocationChatClient` runs both, sends both results back, and the model writes one final answer.

**The AI can also call tools sequentially** — where the result of one tool influences what it does next:

1. Call `get_user_id("Alice")` → gets back `42`
2. Call `get_orders(user_id: 42)` → gets back the order list
3. Write the final answer

In this case the model does one tool call, waits for the result, reads it, then decides to make another. Each round-trip is a separate HTTP request. `UseFunctionInvocation()` handles both patterns — it just keeps looping until `finish_reason` is `stop`.

---

### Q: What if an error happens when the tool runs on my machine? How does the AI handle it? Is there a retry mechanism?

**What MEAI does when your tool throws:**

`FunctionInvocationChatClient` wraps every tool call in a try/catch. If your code throws, it does **not** crash the program. Instead it catches the exception and sends the error message back to the model as the tool result — just like a normal result, but the text describes what went wrong.

So the model receives something like:

```
[Tool result for call_1]: "Error: Input string was not in a correct format."
```

**What the AI does with that error:**

The model reads it and decides on its own. It has three common reactions:

| Situation | What the model typically does |
|---|---|
| Bad input it can fix (e.g. sent `"238x17"` instead of `"238*17"`) | Calls the tool again with corrected arguments |
| Error it cannot fix | Tells the user in plain text: "I tried the calculator but it failed because..." |
| Ambiguous error | May ask the user for clarification |

**Is there an automatic retry mechanism?**

No. MEAI has no built-in retry counter. The only "retry" that happens is if the **model itself** decides to call the tool again — which is just another normal iteration of the loop. You cannot configure "retry 3 times on failure" in the framework.

If you want controlled retry logic, write it inside your tool function:

```csharp
AITool calc = AIFunctionFactory.Create(
    ([Description("The arithmetic expression to evaluate")] string expression) =>
    {
        try
        {
            return new System.Data.DataTable().Compute(expression, null).ToString();
        }
        catch (Exception ex)
        {
            // Return a descriptive error string — the model reads this and decides what to do
            return $"Error: {ex.Message}. Make sure the expression uses only numbers and operators like +, -, *, /.";
        }
    },
    name: "calculator",
    description: "Evaluate a basic arithmetic expression like '3 + 4 * 2'."
);
```

Returning a helpful error string (instead of letting it throw) is better than throwing — because you control the message the model reads, which helps it self-correct.

**The full picture with an error:**

```
Your Machine                            OpenRouter Cloud
────────────────────────────────        ──────────────────
  ├─► sends question + tool schema ───────────────────►  AI model
  │                                                        │
  │◄── "call calculator('238x17')" ◄───────────────────────┘
  │
  ├─► YOUR CODE runs DataTable.Compute("238x17")
  │     → throws exception (invalid format)
  │     → MEAI catches it, formats as error string
  │
  ├─► sends "Error: invalid format" back ─────────────►  AI model
  │                                                        │
  │                                          model decides: retry with "238*17"?
  │                                          or tell the user it failed?
  │◄── (model's decision comes back here) ◄───────────────┘
```

---

### Q: The AI is the deciding factor — can it get into an infinite loop of tool calling? How is this handled?

Yes, it is theoretically possible. Here is how it can happen and how it is handled.

**How an infinite loop could happen:**

Imagine a buggy tool that always returns an error, and a stubborn model that keeps retrying:

```
Trip 1: User asks question
Trip 2: Model calls tool → tool returns error
Trip 3: Model calls tool again (tries to fix it) → tool returns error again
Trip 4: Model calls tool again → error again
... forever
```

Or a poorly designed set of tools where Tool A's output triggers Tool B, and Tool B's output triggers Tool A — the model bounces between them endlessly.

**How MEAI handles this — the `MaximumIterations` cap:**

`FunctionInvocationChatClient` has a built-in hard stop. It will not loop forever. The default maximum is **128 iterations**. After that it stops the loop and returns whatever the last response was, even if it still contains a tool call and not text.

You can configure this yourself:

```csharp
IChatClient chat = new ChatClientBuilder(
        client.GetChatClient("openai/gpt-oss-120b:free").AsIChatClient())
    .UseFunctionInvocation(options =>
    {
        options.MaximumIterationsPerRequest = 10; // stop after 10 tool calls
    })
    .Build();
```

Set it low (like 5–10) when you know your task only needs a couple of tool calls. This saves you from runaway API costs.

**Three real scenarios where loops happen:**

| Scenario | Why it loops | Fix |
|---|---|---|
| Tool always throws/errors | Model keeps retrying hoping it works | Return a clear error string, not an exception |
| Vague tool description | Model doesn't know when to stop using it | Write a precise `description` that says when the tool applies |
| Two tools that depend on each other | Model alternates between them | Redesign tools so their outputs don't feed each other circularly |

**Who controls what:**

```
MEAI controls:   maximum number of iterations (hard ceiling, default 128)
Model controls:  whether to call another tool or write a final answer
You control:     what error/result text the model reads (which influences its decision)
```

The model is the deciding factor — but MEAI holds the emergency brake. If the model never writes a plain-text `stop` response, MEAI will forcibly end the loop at `MaximumIterationsPerRequest`.

---

### Q: Give me real-world scenarios where tools are used in software development — parallel tools, dependent tools, 3rd party APIs, DB calls, etc.

---

#### Scenario 1 — Parallel tools (independent calls in one response)

**Use case: Developer dashboard — load page stats**

User asks: *"Show me the server health and today's error count."*

These two are completely independent, so the model fires both at once:

```
Trip 1: Model calls:
  → get_server_health()          (hits your infra API)
  → get_error_count(date: today) (hits your logging DB)

Trip 2: Model gets both results back together, writes:
  "Server is healthy (CPU 34%, RAM 61%). Today's error count is 142."
```

Tools involved:
```csharp
AIFunctionFactory.Create(() => infraApi.GetHealth(), name: "get_server_health", ...)
AIFunctionFactory.Create((string date) => db.Query("SELECT COUNT(*) FROM errors WHERE date = @date", date), name: "get_error_count", ...)
```

No dependency between them — both results arrive before the model writes anything.

---

#### Scenario 2 — Sequential dependent tools

**Use case: Customer support agent — look up order status**

User asks: *"What is the status of Alice's latest order?"*

The model cannot call `get_order` without knowing Alice's ID first:

```
Trip 1: Model calls:
  → get_customer_id(name: "Alice")

Trip 2: Model receives customer_id = 9821, then calls:
  → get_latest_order(customer_id: 9821)

Trip 3: Model receives order details, writes:
  "Alice's latest order (#ORD-4421) is out for delivery, expected tomorrow."
```

Why sequential: the second tool call depends on the output of the first. The model figures this out on its own — you do not tell it the order. It reads Trip 2's result and decides what to call next.

---

#### Scenario 3 — 3rd party API with chained dependency

**Use case: DevOps agent — deploy after CI passes**

User asks: *"Deploy the latest build of the payments service if CI is green."*

```
Trip 1: Model calls:
  → get_latest_build(service: "payments")
    → returns: { build_id: "b-882", status: "success", commit: "a3f91c" }

Trip 2: Model sees CI is green, now calls:
  → trigger_deployment(build_id: "b-882", environment: "production")
    → returns: { deploy_id: "d-109", status: "started" }

Trip 3: Model calls:
  → poll_deployment_status(deploy_id: "d-109")
    → returns: { status: "complete", url: "https://payments.internal" }

Trip 4: Model writes:
  "Payments service build b-882 deployed successfully to production."
```

If CI had returned `"status: failed"`, the model would have skipped the deployment entirely and told you instead. You wrote zero if/else logic.

---

#### Scenario 4 — DB calls with conditional branching

**Use case: HR chatbot — approve leave request**

User (manager) says: *"Approve Raj's leave request for next week."*

```
Trip 1: Model calls:
  → get_employee_id(name: "Raj")
    → returns: employee_id = 204

Trip 2: Model calls (parallel — both needed before deciding):
  → get_pending_leave_requests(employee_id: 204)
    → returns: [{ request_id: "LR-77", dates: "Jul 1–5", days: 5 }]
  → get_leave_balance(employee_id: 204)
    → returns: { available_days: 3 }

Trip 3: Model sees balance (3) < requested (5), does NOT approve. Writes:
  "Raj only has 3 leave days remaining but requested 5. Cannot approve.
   Would you like to approve a partial leave or decline the request?"
```

The model used DB results to make a business decision — without you writing any if/else logic. The tools just fetch data; the AI reasons over it.

---

#### Scenario 5 — Mixed: DB + 3rd party API + notification

**Use case: E-commerce agent — process a refund**

User (support agent) says: *"Refund order #ORD-991 and notify the customer."*

```
Trip 1: Model calls:
  → get_order(order_id: "ORD-991")
    → returns: { customer_id: 55, amount: 1299, payment_id: "pay_xk92", email: "bob@example.com" }

Trip 2: Model calls (parallel — independent):
  → issue_refund(payment_id: "pay_xk92", amount: 1299)   ← Stripe API
  → get_customer_name(customer_id: 55)                    ← your DB

Trip 3: Model receives refund confirmed + name "Bob", then calls:
  → send_email(to: "bob@example.com", subject: "Your refund",
               body: "Hi Bob, your refund of ₹1299 has been processed.")  ← SendGrid API

Trip 4: Model writes:
  "Refund of ₹1299 issued successfully (refund ID: re_abc). Bob has been notified by email."
```

What to notice:
- Trip 2 is parallel because refund and name lookup are independent
- Trip 3 depends on both Trip 2 results (needs name for email body, needs refund confirmation)
- The model handles the sequencing — you just define the tools

---

#### The pattern that runs through all of them

```
Independent data needed?     → Model fires them in parallel   (one Trip)
Result A needed to call B?   → Model calls sequentially       (separate Trips)
Business decision needed?    → Model reasons over results, may branch
Side effect needed?          → Model calls write/notify/deploy tool last
```

You write the tools. The model writes the workflow.
