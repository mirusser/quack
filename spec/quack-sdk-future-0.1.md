# Quack .NET SDK — Future Considerations v0.1

Non-blocking ideas for post-v0.1. None of these are committed;
they exist to avoid painting into corners.

## 1. CLI Tool

```
quack tail ./audit/*.jsonl       — follow trace stream
quack explain <quackId>          — pretty-print one frame
quack trace --ctx <context>      — filter by context
quack trace --corr <correlation> — filter by correlation
quack trace --structured         — machine-readable output
quack validate <file>            — run fixtures
```

Package: `Quack.Cli` (global tool via `dotnet tool install -g Quack.Cli`).

## 2. Profile System

Domain-specific meaning layers on top of core Quack verbs.
Concept: a profile narrows the verb set + adds typed `data` contracts.

```
Quack.Profile.Kubernetes
  k8s.anomaly.detected    → quack
  k8s.plan.proposed       → egg
  k8s.approval.required   → hatch
  k8s.execution.started   → flap
  k8s.execution.completed → perch
  k8s.drift.detected      → honk
```

Profile packages ship their own validators, data contract types, and fixtures.
Core Quack has zero knowledge of profiles.

Open design questions:
- Do profiles extend verbs or narrow them?
- Can a frame carry multiple profiles?
- How do profiles declare in Agent Cards?

## 3. OpenTelemetry Integration

Beyond `ActivityQuackSink` (tag-setting), deeper integration:

- Quack frames as OTel Span Events (not just tags)
- Trace context propagation (`traceparent` in Quack-Text)
- `Quack.OpenTelemetry` package with exporter/sampler
- Metrics: frame count by verb, rejection rate, latency histogram

## 4. MCP Integration

`Quack.Mcp` — attach Quack frames as MCP tool call metadata or resource annotations.
Similar shape to A2A adapter: metadata injection + extraction.

## 5. Message Bus Adapters

Transport-level adapters for async messaging:

- `Quack.Nats` — NATS subject-based routing
- `Quack.RabbitMq` — exchange/routing-key based
- `Quack.Redis` — pub/sub channels

All implement `IQuackSink`. Core doesn't know they exist.

## 6. Cryptographic Signing

Attach Ed25519 signatures to frames for non-repudiation:

```
QK1 egg ... sig=base64url...
```

Open questions:
- Sign the Quack-Text representation? The CBOR? The JSON?
- Canonicalization must be specified before signing
- Signature in `data.sig` or top-level field?

## 7. Multi-Language SDKs

The spec + conformance fixtures enable non-.NET implementations.
Priority order (speculative):

1. Python — AI/ML ecosystem
2. TypeScript — web tooling, browser agents
3. Go — infrastructure tooling
4. Rust — embedded / wasm agents

Each SDK must pass the same golden fixtures.

## 8. Conformance Test Harness

A language-agnostic conformance runner:

```
quack-conformance --sdk ./my-sdk/test.sh --fixtures ./spec/fixtures/
```

The runner:
1. Feeds valid fixtures → expects clean parse (exit 0)
2. Feeds invalid fixtures → expects specific error codes
3. Round-trip test: encode → decode → compare frames

## 9. Canonicalization for Hashing

If signing is added, Quack needs a canonical form for hashing.
CBOR deterministic encoding is the natural choice (RFC 8949 §4.2).

## 10. WebAssembly / Browser Agent Support

Quack.Core could target `netstandard2.0` or `net8.0-browser` for WASM use.
The text parser is allocation-light by design — suitable for browser agents.

## 11. gRPC Transport

`Quack.Grpc` — carry Quack frames as gRPC metadata or protobuf-encoded payloads.
Natural fit for service-mesh agent communication.

## 12. Quack-Text Streaming

For high-throughput scenarios, a newline-delimited Quack-Text stream:

```
QK1 quack ...
QK1 egg ...
QK1 hatch ...
```

`QuackTextStreamReader` / `QuackTextStreamWriter` for line-at-a-time processing.

## 13. Risk Escalation Rules

Configurable rules for auto-escalation:
- If `nack` count in a `correlation` exceeds threshold → escalate to `honk`
- If `ttl` expires on a `hatch` → auto-`molt`

This belongs in a `Quack.Policy` or `Quack.Rules` package.

## 14. Frame Deduplication

Idempotency via frame ID: if a sink receives a frame with a previously-seen `id`,
drop it. Configurable per-sink.
