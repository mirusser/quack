# Quack .NET SDK v0.1

The .NET SDK is a reference implementation of the Quack protocol.
It owns frame construction, validation, emission, tracing, and DI
integration. It does **not** own transport — that lives in adapter packages.

## 1. Design Principles

1. **Core has no network opinions.** No A2A, no MCP, no HTTP. Only `System.Text.Json`.
2. **Sinks are pluggable.** The SDK defines `IQuackSink`; adapters implement it.
3. **Validation is built-in.** `EmitAsync` always validates before delivery.
4. **Errors are values, not exceptions.** `QuackResult` carries success or structured rejection.
5. **Traces are separate from delivery.** Trace stream failures never affect `EmitAsync`.
6. **The spec is the source of truth.** The SDK consumes golden fixtures; it doesn't define them.

## 2. Package Structure

```
Quack.Core           Frame model, verbs, validation, IQuackSink, IQuackEmitter,
                     QuackResult, ITraceSink, built-in sinks, DI extensions.
                     Dependencies: none (System.Text.Json for JSON codec only).

Quack.Json           Quack-JSON codec. Separate: Core sinks don't need JSON.
                     Depends on: Quack.Core.

Quack.Cbor           Quack-CBOR codec.
                     Depends on: Quack.Core.

Quack.AspNetCore     Middleware, HTTP header injection, DI helpers for ASP.NET Core.
                     Depends on: Quack.Core, Quack.Json.

Quack.A2A            A2A adapter — Agent Card helpers, DataPart/metadata injection.
                     Depends on: Quack.Core, Quack.Json, A2A SDK.

Quack.Testing        Golden fixture runner, fake sinks, test helpers.
                     Depends on: Quack.Core, Quack.Json.
```

Dependency graph:

```
Quack.Core
  ├── Quack.Json ──────┬── Quack.AspNetCore
  │                    ├── Quack.A2A
  ├── Quack.Cbor       └── Quack.Testing
  └── (standalone)
```

Meta-package `Quack` pulls in `Quack.Core` + `Quack.Json`.

## 3. Core Types

### 3.1 QuackFrame

An immutable record. Fields map 1:1 to the canonical Frame (§1.1 of the protocol spec).

```csharp
public sealed record QuackFrame
{
    public int Version { get; init; }                // Protocol version (1 for v0.1)
    public QuackVerb Verb { get; init; }              // Verb
    public string Id { get; init; }                    // ULID frame ID
    public long? Timestamp { get; init; }              // Unix milliseconds
    public string Source { get; init; }                // Source agent
    public string? Destination { get; init; }          // Destination. Omitted = pond-wide.
    public string? Context { get; init; }              // Context
    public string? Correlation { get; init; }          // Correlation ID
    public QuackRisk Risk { get; init; }               // Risk level
    public string? Summary { get; init; }              // Human-readable summary
    public string? Digest { get; init; }               // Digest binding (e.g. "sha256:abc...")
    public int? Ttl { get; init; }                     // Freshness window in ms
    public QuackTone? Tone { get; init; }              // Decorative tone
    public IReadOnlyDictionary<string, JsonElement>? Data { get; init; }
}
```

### 3.2 Enums

```csharp
public enum QuackVerb
{
    Quack, Peck, Bob, Nack, Egg, Hatch,
    Flap, Perch, Honk, Molt, Splash
}

public enum QuackRisk
{
    None = 0,       // n
    Low = 1,        // l
    Medium = 2,     // m
    High = 3,       // h
    Critical = 4    // c
}

public enum QuackTone
{
    SeriousDuck,
    PlayfulDuck,
    AngryGoose,
    SleepyDuckling
}
```

## 4. Verb Helpers — Static Factory Methods

Each verb gets a static factory method on `QuackFrame`. Required fields for each verb
are positional; optional fields are named parameters.

### 4.1 Announce-type (may omit `destination` per §1.4)

```csharp
// quack — minimum: source
public static QuackFrame Quack(
    string source,
    string? destination = null,
    string? context = null,
    string? correlation = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    QuackTone? tone = null);

// honk — minimum: source, reason (data.reason)
public static QuackFrame Honk(
    string source,
    string reason,
    string? destination = null,
    string? context = null,
    string? correlation = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    QuackTone? tone = null);

// splash — minimum: source, evidence (data.evidence)
public static QuackFrame Splash(
    string source,
    EvidenceRef[] evidence,
    string? destination = null,
    string? context = null,
    string? correlation = null,
    string? summary = null,
    QuackTone? tone = null);

// molt — minimum: source, correlation
public static QuackFrame Molt(
    string source,
    string correlation,
    string? destination = null,
    string? context = null,
    string? reason = null,
    string? summary = null,
    QuackTone? tone = null);
```

### 4.2 Request-type (require `destination` per §1.4)

```csharp
// peck — minimum: source, destination
public static QuackFrame Peck(
    string source,
    string destination,
    string? context = null,
    string? correlation = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    QuackTone? tone = null);

// egg — minimum: source, destination, eggId, digest
public static QuackFrame Egg(
    string source,
    string destination,
    string eggId,
    string digest,
    string? context = null,
    string? correlation = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    string? ttl = null,
    QuackTone? tone = null);

// hatch — minimum: source, destination, eggId
public static QuackFrame Hatch(
    string source,
    string destination,
    string eggId,
    string correlation,
    string? context = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    int? ttl = null,
    QuackTone? tone = null);

// flap — minimum: source, destination, eggId, digest
public static QuackFrame Flap(
    string source,
    string destination,
    string eggId,
    string digest,
    string correlation,
    string? context = null,
    QuackRisk risk = QuackRisk.None,
    string? summary = null,
    QuackTone? tone = null);
```

### 4.3 Response-type (require `destination` per §1.4)

```csharp
// bob — minimum: source, destination, correlation
public static QuackFrame Bob(
    string source,
    string destination,
    string correlation,
    string? context = null,
    string? summary = null,
    QuackTone? tone = null);

// nack — minimum: source, destination, correlation
public static QuackFrame Nack(
    string source,
    string destination,
    string correlation,
    string? context = null,
    string? summary = null,
    QuackTone? tone = null);

// perch — minimum: source, destination, correlation
public static QuackFrame Perch(
    string source,
    string destination,
    string correlation,
    string? context = null,
    string? summary = null,
    QuackTone? tone = null);
```

All factories auto-generate `Id` as a ULID and set `Version = 1` and `Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`
unless explicitly overridden.

## 5. Emission Model

### 5.1 IQuackSink

The fundamental contract. Every delivery target implements this.

```csharp
public interface IQuackSink
{
    ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default);
}
```

`EmitAsync` MUST validate the frame before delivery. It returns `QuackResult`, never throws
for protocol-level rejections. Infrastructure failures (network down, disk full) may throw.

### 5.2 IQuackEmitter

Application-facing facade. Wraps a sink pipeline and adds `Src` if not set.

```csharp
public interface IQuackEmitter
{
    string AgentName { get; }
    ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct = default);
}
```

Default implementation (`QuackEmitter`) delegates to a `CompositeQuackSink`, validates
the frame, auto-sets `Src = AgentName` if the frame's `Src` is null, and gates on `MaxRisk`
(see §5.3).

### 5.3 Risk Gating

Before delivery, `QuackEmitter` compares `frame.Risk` against `options.MaxRisk`.
If `frame.Risk > options.MaxRisk`, the frame is rejected — `EmitAsync` returns
`QuackResult.Rejected(RISK_EXCEEDS_MAX)` and the frame never reaches sinks.

This is separate from validation — it's a policy decision, not a protocol rule violation.

### 5.4 TTL Gating

If `options.MaxTtl` is set and `frame.Ttl > options.MaxTtl`, the frame is rejected with
`QuackResult.Rejected(TTL_EXCEEDS_MAX)`.

## 6. Built-in Sinks

All sinks in `Quack.Core`. Adapter sinks (A2A, MCP) live in their respective packages.

| Sink | Package | Behavior |
|------|---------|----------|
| `LoggerQuackSink` | Core | Writes Quack-Text to `ILogger` at Information level |
| `ConsoleQuackSink` | Core | Writes Quack-Text to stdout (or stderr if risk ≥ High) |
| `FileQuackSink` | Core | Appends Quack-Text to a file; rotates on size |
| `ActivityQuackSink` | Core | Sets OTel `Activity` tags and baggage |
| `NoopQuackSink` | Core | Discards all frames; always returns Success |
| `CompositeQuackSink` | Core | Fans out to an ordered list of inner sinks |

### 6.1 CompositeQuackSink

Fans out in registration order. Each inner sink receives the same frame.

Failure behavior: by default, if one sink fails, the composite **continues** to remaining
sinks and returns the **first** error. This is configurable:

```csharp
public enum CompositeFailureMode
{
    Continue,   // Try all sinks, return first error
    Stop         // Abort on first error
}
```

Each inner sink receives the same frame. The composite validates **once** before fan-out.

## 7. QuackResult — Error as Values

```csharp
public readonly struct QuackResult
{
    public bool IsSuccess { get; }
    public bool IsRejected { get; }
    public QuackFrame? Frame { get; }
    public IReadOnlyList<QuackError> Errors { get; }

    public static QuackResult Success(QuackFrame frame);
    public static QuackResult Rejected(params QuackError[] errors);
}

public readonly struct QuackError
{
    public string Code { get; }        // Machine-readable: "FLAP_WITHOUT_HATCH"
    public string Message { get; }     // Human-readable: "FLAP requires prior HATCH + BOB"
    public QuackRule Rule { get; }     // Which §3 rule was violated
}

public enum QuackRule
{
    FlapWithoutHatch,              // §3.1.1
    HatchWithoutEgg,               // §3.1.2
    EggWithoutSplash,              // §3.1.3
    EggWithoutProof,               // §3.1.4
    HonkWithoutReason,             // §3.2.5
    MoltWithoutCorrelation,        // §3.2.6
    SplashWithoutEvidence,         // §3.2.7
    NonBroadcastWithoutDestination,// §3.2.8
    RiskExceedsMax,                // risk > maxRisk
    TtlExceedsMax,                 // ttl > maxTtl
    InvalidFrame,                  // catch-all for malformed frames
}
```

### 7.1 Error Deserialization Safety

`QuackResult` values never carry exceptions. They carry structured errors.
Callers check `IsSuccess` / `IsRejected`; no try/catch needed for protocol rejections.

### 7.2 Rejection Trace

Per §6 of the protocol spec, when `EmitAsync` returns `QuackResult.Rejected`:

1. A `nack` frame is emitted to the trace stream (via `ITraceSink`)
2. The trace delivery is best-effort — failure does not affect the returned `QuackResult`
3. The `nack` frame carries `correlation` from the rejected frame and `data.reason` from the first error

## 8. Validation

### 8.1 IQuackValidator

```csharp
public interface IQuackValidator
{
    ValidationResult Validate(QuackFrame frame);
    ValidationResult Validate(QuackFrame frame, IQuackHistory history);
}
```

### 8.2 ValidationResult

```csharp
public readonly struct ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<QuackError> Errors { get; }

    public static ValidationResult Valid();
    public static ValidationResult Invalid(params QuackError[] errors);
}
```

### 8.3 Stateless vs Stateful

- **Stateless** `Validate(frame)`: checks structural rules only (§3.2.5–§3.2.8, plus `version`, `verb`,
  `id`, `source` are present).
- **Stateful** `Validate(frame, history)`: also checks sequencing rules (§3.1.1–§3.1.4) by
  looking up prior frames in the same `correlation` / `context`.

### 8.4 IQuackHistory

The validator's window into frame history. Implementations are application-specific
(in-memory for tests, persisted for production).

```csharp
public interface IQuackHistory
{
    IReadOnlyList<QuackFrame> GetByCorrelation(string correlation);
    IReadOnlyList<QuackFrame> GetByContext(string context);
}
```

### 8.5 Rule mapping table

| § Rule | Code | Check |
|--------|------|-------|
| 3.1.1 | `FLAP_WITHOUT_HATCH` | History must contain `hatch` + `bob` with same `correlation` |
| 3.1.2 | `HATCH_WITHOUT_EGG` | History must contain `egg` with same `correlation` |
| 3.1.3 | `EGG_WITHOUT_SPLASH` | History must contain `splash` with same `context` |
| 3.1.4 | `EGG_WITHOUT_PROOF` | Frame must carry non-null `Digest` |
| 3.2.5 | `HONK_WITHOUT_REASON` | `Data["reason"]` must exist and be non-empty |
| 3.2.6 | `MOLT_WITHOUT_CORRELATION` | Frame must carry non-null `Correlation` |
| 3.2.7 | `SPLASH_WITHOUT_EVIDENCE` | `Data["evidence"]` must exist, be an array, ≥1 entry |
| 3.2.8 | `NON_BROADCAST_WITHOUT_DESTINATION` | Non-broadcast verb must carry non-null `Destination` |

Broadcast verbs: `quack`, `honk`, `splash`, `molt` (§1.4).

## 9. Trace Stream

### 9.1 ITraceSink

Separate from `IQuackSink`. Trace is for observability, not delivery.

```csharp
public interface ITraceSink
{
    void Write(QuackFrame frame);
    void WriteRejection(QuackFrame frame, QuackResult result);
}
```

- Fire-and-forget. No return value. Must not throw.
- Trace sink failures must never propagate to `EmitAsync` callers.
- Built-in implementations: `LoggerTraceSink`, `ConsoleTraceSink` (stderr),
  `FileTraceSink`, `CompositeTraceSink`.

### 9.2 Trace Format

`Write` renders frames as emoji-prefixed Quack-Text lines (per §8 of the protocol spec).

`WriteRejection` renders the original frame + a `nack` frame on the next line:

```
🥚 QK1 egg    quackId=01JQA3 src=planner ... say="restart plan"
🐦‍⬛ QK1 nack   quackId=01JQA9 src=quack-core corr=... data.reason="EGG_WITHOUT_SPLASH"
```

A machine-readable structured trace (no emoji, aligned columns, explicit sequencing)
is available via `ITraceSink.WriteStructured(QuackFrame)` — see `quack-sdk-adapters-0.1.md`.

## 10. DI Integration

### 10.1 Registration

```csharp
public static class QuackServiceCollectionExtensions
{
    public static IQuackBuilder AddQuack(
        this IServiceCollection services,
        Action<QuackOptions>? configure = null);
}
```

### 10.2 Options

```csharp
public sealed class QuackOptions
{
    public string AgentName { get; set; } = "quack-agent";
    public QuackRisk MaxRisk { get; set; } = QuackRisk.High;
    public int? MaxTtl { get; set; }        // null = accept any TTL
    public int MaxQuackVersion { get; set; } = 1;
}
```

### 10.3 Builder

```csharp
public interface IQuackBuilder
{
    IServiceCollection Services { get; }
    IQuackBuilder AddSink<T>() where T : class, IQuackSink;
    IQuackBuilder AddTraceSink<T>() where T : class, ITraceSink;
}
```

### 10.4 Registered Services

`AddQuack` registers:

| Service | Lifetime | Notes |
|---------|----------|-------|
| `QuackOptions` | Singleton | Configured options |
| `IQuackEmitter` | Singleton | Wraps sink pipeline |
| `IQuackValidator` | Singleton | Stateless; history passed per-call |
| `ITraceSink` | Singleton | Composite of registered trace sinks |
| `IQuackSink` | Singleton | Composite of registered delivery sinks |

### 10.5 Usage Pattern

```csharp
// Registration
builder.Services.AddQuack(options =>
{
    options.AgentName = "planner";
    options.MaxRisk = QuackRisk.Medium;
    options.MaxTtl = 3_600_000;
})
.AddSink<LoggerQuackSink>()
.AddSink<ConsoleQuackSink>()
.AddTraceSink<LoggerTraceSink>();

// Consumption
public sealed class Planner
{
    private readonly IQuackEmitter _quack;

    public Planner(IQuackEmitter quack) => _quack = quack;

    public async Task ProposeAsync(string planId, string digest)
    {
        var frame = QuackFrame.Egg(
            source: null,  // auto-set by IQuackEmitter
            destination: "executor",
            eggId: planId,
            digest: digest,
            context: "k8s/default/web",
            correlation: planId,
            risk: QuackRisk.Medium,
            summary: "restart deployment plan");

        var result = await _quack.EmitAsync(frame);
        if (result.IsRejected)
        {
            // handle rejection — nack already emitted to trace
        }
    }
}
```

## 11. Frame ID Generation

Frame IDs are ULIDs (26-char, sortable, URL-safe). The SDK uses a monotonic ULID
generator to guarantee uniqueness within a process:

```csharp
public static class QuackId
{
    public static string NewId();  // Returns a new ULID string
}
```

The ULID library is an internal implementation detail. The SDK guarantees:
- Monotonically increasing within a process
- URL-safe Base32 encoding
- 26 characters

## 12. EmitterAutoProperties

When `IQuackEmitter.EmitAsync` processes a frame, it auto-sets these properties
if not already provided:

| Property | Auto-value if null |
|----------|--------------------|
| `Id` | `QuackId.NewId()` |
| `Timestamp` | `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` |
| `Source` | `options.AgentName` |
| `Version` | `1` |

Auto-set properties are applied **before** validation, so the validator sees a complete frame.

## 13. Extension Points

### 13.1 Custom Sinks

Any class implementing `IQuackSink` works. Register via `builder.AddSink<T>()` or
manually compose with `CompositeQuackSink`.

### 13.2 Custom History

Implement `IQuackHistory` for your storage backend. Pass to `Validate(frame, history)`.

### 13.3 Without DI

The SDK is fully usable without DI:

```csharp
var sink = new CompositeQuackSink(
    new ConsoleQuackSink(),
    new LoggerQuackSink(logger));

var validator = new QuackValidator();

var frame = QuackFrame.Honk("gateway", "digest mismatch",
    correlation: "plan-456", risk: QuackRisk.High);

var validation = validator.Validate(frame);
if (!validation.IsValid) return;

var result = await sink.EmitAsync(frame);
```
