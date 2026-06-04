# Quack .NET SDK — Adapters & Encodings v0.1

Supplement to `quack-sdk-0.1.md`. Covers encoding codecs, A2A adapter,
ASP.NET Core middleware, sink pipeline details, evidence URIs, structured
traces, ULID generation, and digest binding.

## 1. Encoding Codecs

Each encoding is a codec pair: `Encode` and `Decode`. All codecs round-trip
through `QuackFrame` (per protocol spec §5.1).

### 1.1 Quack-Text

```csharp
public static class QuackText
{
    public static string Encode(QuackFrame frame);
    public static QuackFrame Decode(string text);

    // Low-level: encode/decode a single key-value pair
    public static string EncodeValue(string value);
    public static string DecodeValue(string encoded);
}
```

Field mapping per protocol spec §5.2 Text Field Mapping table.

**Encoding rules:**
- Produces `QK1 <verb> <key=value>*` (single line, no trailing newline)
- Bare tokens for simple values; quoted strings for values containing spaces, `"`, or `=`
- Backslash-escapes `\` and `"` inside quoted strings
- Unknown keys preserved as-is during round-trip
- `data` fields not encoded inline — referenced by digest (`evidence=@sha256:...`)

**Decoding rules:**
- Unknown keys stored in `Data` as string values
- Duplicate keys → decode error (not rejected at protocol level — this is a parse error)
- Missing `version` or `verb` → decode error
- Whitespace between tokens is flexible (one or more spaces)

### 1.2 Quack-JSON

```csharp
public static class QuackJson
{
    public static string Serialize(QuackFrame frame, JsonSerializerOptions? options = null);
    public static QuackFrame Deserialize(string json, JsonSerializerOptions? options = null);

    // Stream variants
    public static ValueTask SerializeAsync(Stream stream, QuackFrame frame, ...);
    public static ValueTask<QuackFrame> DeserializeAsync(Stream stream, ...);
}
```

Lowercase wire names per protocol spec §5.4. The C# property names map directly
to JSON wire names using camelCase:

| C# property | Wire name |
|-------------|-----------|
| `Version` | `version` |
| `Verb` | `verb` |
| `Id` | `id` |
| `Timestamp` | `timestamp` |
| `Source` | `source` |
| `Destination` | `destination` |
| `Context` | `context` |
| `Correlation` | `correlation` |
| `Risk` | `risk` (string values: `n`, `l`, `m`, `h`, `c`) |
| `Summary` | `summary` |
| `Digest` | `digest` |
| `Ttl` | `ttl` |
| `Tone` | `tone` |
| `Data` | `data` |

Risk values serialize as single-char strings and deserialize case-insensitively.

### 1.3 Quack-CBOR

```csharp
public static class QuackCbor
{
    public static byte[] Encode(QuackFrame frame);
    public static QuackFrame Decode(byte[] bytes);

    // Stream variants
    public static ValueTask EncodeAsync(Stream stream, QuackFrame frame, ...);
    public static ValueTask<QuackFrame> DecodeAsync(Stream stream, ...);
}
```

**Integer key mapping** per protocol spec §5.3.

| CBOR int | Field |
|----------|-------|
| 1 | `version` |
| 2 | `verb` |
| 3 | `id` |
| 4 | `timestamp` |
| 5 | `source` |
| 6 | `destination` |
| 7 | `context` |
| 8 | `correlation` |
| 9 | `risk` |
| 10 | `digest` |
| 11 | `summary` |
| 12 | `data` |
| 13 | `ttl` |
| 14 | `tone` |

**Encoding rules:**
- Deterministic CBOR (RFC 8949 §4.2) — keys sorted by integer value, definite-length only
- `risk` encoded as text string (`"n"`, `"l"`, `"m"`, `"h"`, `"c"`)
- `data` encoded as CBOR map
- Omitted optional fields are absent from the map (not null)
- Media type: `application/vnd.quack+cbor`

**Library:** Use `System.Formats.Cbor` (built-in, .NET 8+). No external dependency.

### 1.4 Quack-HTTP (Structured Fields)

For HTTP header injection per protocol spec §5.5. This is a **formatting codec**, not a
transport — it produces RFC 9651 Dictionary values suitable for HTTP headers.

```csharp
public static class QuackHttp
{
    public static string EncodeHeader(QuackFrame frame);       // → single header value
    public static QuackFrame DecodeHeader(string headerValue);  // ← from header value

    // Trace header (context + correlation only)
    public static string EncodeTraceHeader(QuackFrame frame);
}
```

Encoding produces RFC 9651 Dictionary syntax:
```
version=1, verb="egg", id="01JQA3", source="planner", destination="executor", ...
```

`risk` is encoded as a bare token (not quoted): `risk=m`.

## 2. ASP.NET Core Middleware

Package: `Quack.AspNetCore`.

### 2.1 Middleware Registration

```csharp
public static class QuackMiddlewareExtensions
{
    public static IApplicationBuilder UseQuack(this IApplicationBuilder app);
}
```

### 2.2 Behavior

The middleware attaches to the HTTP pipeline and does three things:

1. **Inbound:** Reads the `Quack` and `Quack-Trace` request headers and makes them
   available as `HttpContext.Features.Get<IQuackFeature>()`. Does not parse — parsing is
   the caller's responsibility.

2. **Outbound:** If `IQuackFeature` contains frames set during request processing,
   serializes them into `Quack` and `Quack-Trace` response headers.

3. **Correlation:** Sets `Quack-Trace` with `context` and `correlation` if present on any frame.

### 2.3 IQuackFeature

```csharp
public interface IQuackFeature
{
    QuackFrame? RequestFrame { get; }
    IReadOnlyList<QuackFrame> ResponseFrames { get; }
    void Attach(QuackFrame frame);
}
```

### 2.4 Controller / Minimal API Helpers

```csharp
// Extension methods on HttpContext / HttpRequest
public static class QuackHttpExtensions
{
    public static QuackFrame? GetQuackFrame(this HttpContext context);
    public static void AttachQuackFrame(this HttpContext context, QuackFrame frame);
    public static bool TryGetQuackFrame(this HttpRequest request, out QuackFrame frame);
}
```

### 2.5 Header Names

| Header | Content |
|--------|---------|
| `Quack` | Full frame in Quack-HTTP encoding |
| `Quack-Trace` | `context` + `correlation` for trace correlation |

## 3. A2A Adapter

Package: `Quack.A2A`. Depends on the A2A .NET SDK.

### 3.1 Agent Card Helpers

```csharp
public static class QuackAgentCardExtensions
{
    public static AgentCard WithQuackExtension(
        this AgentCard card,
        QuackOptions options);
}
```

Produces the `capabilities.extensions[]` entry per protocol spec §7.1:

```json
{
  "uri": "https://quack.dev/ext/quack/v0.1",
  "description": "Quack semantic protocol for agent coordination.",
  "required": false,
  "params": {
    "maxRisk": "high",
    "maxTtl": 3600000,
    "maxQuackVersion": 1,
    "supportedEncodings": ["application/vnd.quack+json", "text/vnd.quack"]
  }
}
```

### 3.2 Message Extension Methods

```csharp
public static class QuackA2AMessageExtensions
{
    // Attach Quack frame to A2A message metadata (for small signals)
    public static Message WithQuackMetadata(this Message message, QuackFrame frame);

    // Attach Quack frame as a DataPart (for structured payloads)
    public static Message WithQuackDataPart(this Message message, QuackFrame frame);

    // Extract Quack frame from message
    public static bool TryGetQuack(this Message message, out QuackFrame frame);

    // Extract all Quack frames (metadata + DataParts)
    public static IReadOnlyList<QuackFrame> GetQuackFrames(this Message message);
}
```

### 3.3 Metadata encoding

Small signals (`quack`, `honk`, `bob`, `nack`) are attached as metadata using
Quack-Text encoding (per protocol spec §7.2):

```json
{
  "metadata": {
    "https://quack.dev/ext/quack/v0.1": "QK1 quack quackId=01J... src=observer ..."
  }
}
```

### 3.4 DataPart encoding

Structured frames (`egg`, `splash`, `flap`, `hatch`) are attached as unified Parts
using Quack-JSON with `mediaType: "application/vnd.quack+json"` (per protocol spec §7.3).

### 3.5 A2A Sink

```csharp
public sealed class A2AQuackSink : IQuackSink
{
    public A2AQuackSink(IA2AClient client);
    public ValueTask<QuackResult> EmitAsync(QuackFrame frame, CancellationToken ct);
}
```

Uses `TryGetQuack` to extract frames from inbound A2A messages and delegates
delivery to the A2A client for outbound frames.

## 4. Sink Pipeline

### 4.1 Composition Order

`CompositeQuackSink` fans out to sinks in registration order. Each receives the same
validated frame. Sinks are independent — one sink's failure does not affect others
(unless `CompositeFailureMode.Stop` is configured).

### 4.2 Built-in Sinks — Detailed Behavior

| Sink | Encoding | Output |
|------|----------|--------|
| `LoggerQuackSink` | Quack-Text | `ILogger.LogInformation("{QuackFrame}", text)` |
| `ConsoleQuackSink` | Quack-Text | stdout (stderr if risk ≥ High) |
| `FileQuackSink` | Quack-Text | Append to file; rotate at 10 MB |
| `ActivityQuackSink` | N/A | OTel `Activity.Current?.SetTag()` |

### 4.3 ConsoleQuackSink Behavior

- Risk None/Low/Medium → stdout
- Risk High/Critical → stderr (with ANSI red if terminal supports it)
- Emoji prefix from the protocol spec §8.1 emoji mapping

### 4.4 ActivityQuackSink Tags

Sets these tags on the current `Activity`:

| Tag | Source |
|-----|--------|
| `quack.verb` | `frame.Verb` |
| `quack.id` | `frame.Id` |
| `quack.source` | `frame.Source` |
| `quack.destination` | `frame.Destination` |
| `quack.context` | `frame.Context` |
| `quack.correlation` | `frame.Correlation` |
| `quack.risk` | `frame.Risk` |

Sets baggage: `quack.correlation = frame.Correlation`.

## 5. Evidence / Artifact URI Scheme

### 5.1 EvidenceRef

```csharp
public sealed record EvidenceRef
{
    public string Kind { get; init; }       // e.g. "k8s.events"
    public string Digest { get; init; }     // e.g. "sha256:abc123..."
    public string? Uri { get; init; }       // e.g. "artifact://events/default/web"
}
```

### 5.2 URI Schemes

The SDK does not resolve evidence URIs — it only validates their shape.
Resolution is the application's responsibility.

Recognized schemes:

| Scheme | Meaning | Example |
|--------|---------|---------|
| `artifact://` | Internal artifact store | `artifact://events/default/web` |
| `https://` | Remote artifact | `https://store.example.com/...` |
| `sha256:` | Digest reference (inline) | `sha256:abc123...` |

### 5.3 Digest Reference in Quack-Text

In Quack-Text, evidence is referenced by digest, not inlined:

```
evidence=@sha256:abc123...
```

The `@` prefix signals a reference. Multiple references are comma-separated:
```
evidence=@sha256:abc123,...@sha256:def456
```

## 6. Structured Trace Format

Available via `ITraceSink.WriteStructured(QuackFrame)`.

```csharp
public interface ITraceSink
{
    void Write(QuackFrame frame);
    void WriteRejection(QuackFrame frame, QuackResult result);
    void WriteStructured(QuackFrame frame);  // Machine-readable, no emoji
}
```

### 6.1 Format

Fixed-width columns, space-separated, one line per frame:

```
2026-06-04T12:00:00.000Z egg    01JQA3Y7A0X9N planner    executor    k8s/default/web plan-456 m sha256:abc123 restart deployment plan
```

Column layout:

| Column | Width | Source |
|--------|-------|--------|
| Timestamp | 24 | `timestamp` as ISO 8601 UTC, space-padded to ms |
| Verb | 8 | `verb`, left-aligned, space-padded |
| Frame ID | 26 | `id` |
| Source | 12 | `source`, left-aligned, truncated with `…` if >12 |
| Destination | 12 | `destination`, left-aligned, `-` if null |
| Context | 24 | `context`, truncated with `…` if >24, `-` if null |
| Correlation | 16 | `correlation`, truncated, `-` if null |
| Risk | 1 | `n`/`l`/`m`/`h`/`c`, `-` if null |
| Digest | 20 | `digest`, truncated, `-` if null |
| Summary | rest | `summary`, not truncated |

### 6.2 CLI Access

```
quack trace --structured --file ./audit.jsonl
```

## 7. ULID Generation

```csharp
public static class QuackId
{
    public static string NewId();
    public static bool IsValid(string id);    // Regex: ^[0-9A-HJKMNP-TV-Z]{26}$
}
```

Implementation uses `System.Security.Cryptography.RandomNumberGenerator` for the
random component and `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` for the
timestamp component. No external dependency.

The monotonic guarantee: if two ULIDs are generated within the same millisecond,
the random component is incremented rather than re-randomized.

## 8. Digest Binding Format

The `digest` field carries a digest binding in the format:

```
<algorithm>:<hex-encoded digest>
```

| Algorithm | Prefix | Example |
|-----------|--------|---------|
| SHA-256 | `sha256:` | `sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| SHA-512 | `sha512:` | `sha512:cf83e135...` |

The SDK validates that `digest` matches this pattern if present. It does not compute
or verify digests — that is the application's responsibility.

```csharp
public static class QuackDigest
{
    public static bool TryParse(string digest, out string algorithm, out string hexDigest);
    public static bool IsValid(string digest);
}
```

## 9. Conformance Fixtures Integration

`Quack.Testing` provides a fixture runner:

```csharp
public static class QuackConformance
{
    // Run all valid fixtures and assert they parse without error
    public static IReadOnlyList<QuackConformanceResult> RunValidFixtures(string fixturesPath);

    // Run all invalid fixtures and assert they produce expected errors
    public static IReadOnlyList<QuackConformanceResult> RunInvalidFixtures(string fixturesPath);
}

public sealed record QuackConformanceResult
{
    public string FixturePath { get; init; }
    public bool Passed { get; init; }
    public string? Error { get; init; }
}
```

Fixture directory layout per protocol spec:

```
spec/fixtures/
  valid/
    quack.json         → parses to QuackFrame with Verb=Quack
    peck.json          → parses to QuackFrame with Verb=Peck
    egg.json           → ...
    hatch.json
    flap.json
    perch.json
    honk.json
    molt.json
    splash.json
    bob.json
    nack.json
    full-flow.json     → multi-frame trace
  invalid/
    missing-source.json    → decode error
    bad-risk.json       → decode error
    flap-without-hatch.json  → validation error (if stateful)
```

## 10. Namespace Conventions

| Package | Root Namespace | Example Type |
|---------|---------------|--------------|
| `Quack.Core` | `Quack` | `Quack.QuackFrame` |
| `Quack.Json` | `Quack.Json` | `Quack.Json.QuackJson` |
| `Quack.Cbor` | `Quack.Cbor` | `Quack.Cbor.QuackCbor` |
| `Quack.AspNetCore` | `Quack.AspNetCore` | `Quack.AspNetCore.QuackMiddleware` |
| `Quack.A2A` | `Quack.A2A` | `Quack.A2A.QuackA2AMessageExtensions` |
| `Quack.Testing` | `Quack.Testing` | `Quack.Testing.QuackConformance` |
