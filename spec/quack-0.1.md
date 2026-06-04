# Quack! v0.1

A tiny semantic protocol for agent coordination.

Quack is text-first, binary-ready, and JSON-friendly.

Text is for humans and injection.
CBOR is for machines and compact transport.
JSON is for A2A and interoperability.

Quack does not require agents to speak JSON.
It only asks them to quack clearly.

---

## 1. The Quack Frame

A Quack Frame is the canonical abstract model. All encodings round-trip through it.

### 1.1 Canonical fields

| Field | Name | Required | Type | Purpose |
|---|---|---|---|---|
| Protocol version | `version` | ✅ | int | `1` for v0.1 |
| Verb | `verb` | ✅ | string | One of the 11 verbs (§2) |
| Frame ID | `id` | ✅ | string | ULID — unique quack identifier |
| Timestamp | `timestamp` | ❌ | unix ms | When the frame was created |
| Source | `source` | ✅ | string | Emitting agent or component |
| Destination | `destination` | ❌ | string | Target agent. Omitted = pond-wide (§1.4) |
| Context | `context` | ❌ | string | Logical grouping scope (e.g., "k8s/default/web") |
| Correlation | `correlation` | ❌ | string | Links frames into a sequence |
| Risk | `risk` | ❌ | `n` \| `l` \| `m` \| `h` \| `c` | none / low / medium / high / critical (§1.3) |
| Summary | `summary` | ❌ | string | Human-readable one-liner |
| Digest | `digest` | ❌ | string | Digest binding (e.g., "sha256:abc...") |
| Time to live | `ttl` | ❌ | int | Relative freshness window in ms from `timestamp` (§3.3) |
| Payload | `data` | ❌ | map | Verb-structured payload (§4) |
| Tone | `tone` | ❌ | string | Optional. Purely decorative. `serious-duck`, `playful-duck`, `angry-goose`, `sleepy-duckling`. Has no semantic effect on protocol behavior. |

### 1.2 Frame ID

Frame IDs are ULIDs — 26-character, sortable, URL-safe identifiers. They provide rough temporal ordering without requiring a `timestamp` field.

```
01JQA1X5Z8W7M3N9P2R6V0K4B1
```

### 1.3 Risk

Risk gates delivery at the protocol level. Each agent declares its maximum accepted risk in its A2A Agent Card (§7). A frame whose `risk` exceeds the receiver's declared threshold is rejected before delivery — the receiver never sees it.

| Wire | Value | Meaning |
|---|---|---|
| `n` | none | No risk. Informational. |
| `l` | low | Minimal impact. Routine. |
| `m` | medium | Moderate impact. Requires attention. |
| `h` | high | Significant impact. Requires approval. |
| `c` | critical | Severe impact. Maximum scrutiny. |

Risk is ordered: `n < l < m < h < c`. A `maxRisk: medium` agent accepts `n`, `l`, `m` and rejects `h`, `c`.

### 1.4 Addressing

Quack frames are point-to-point by default via `destination`. When `destination` is omitted, the frame is **pond-wide** — every agent in the context receives it.

Only announce-type verbs may omit `destination`: `quack`, `honk`, `splash`, `molt`. All other verbs require `destination`. A frame that violates this is rejected.

**Relay pattern.** Human-in-the-loop approval is out of band by design. When a human approves a `hatch` through a browser or external tool, the gateway or approval service that observed that decision emits the `bob` (or `nack`) into Quack as the Quack-speaking agent. From Quack's perspective, the gateway *is* the approver. The human interaction happens outside the protocol — Quack sees only the outcome, not the deliberation.

---

## 2. Verbs

Quack defines 11 verbs. Each verb carries a semantic role, a duck-natural metaphor, and a technical contract.

| Verb | Emoji | Role | Duck metaphor | Technical contract |
|---|---|---|---|---|
| `quack` | 🦆 | announce / observe | A duck quacks to declare presence and state to the pond. | "This happened." Does not request action. |
| `peck` | 🐤 | request | A duck pecks to probe, demand attention, or draw a response. | Bounded request. Expects `bob` or `nack`. |
| `bob` | 🦢 | accepted / understood | A swan bows its head — a graceful, unmistakable signal of recognition and trust. | "Received and understood." Also serves as approval after `hatch`. |
| `nack` | 🐦‍⬛ | rejected / cannot comply | A blackbird turns away. Dark, decisive, final. No ambiguity. | Terminal rejection of a `peck` or `hatch`. |
| `egg` | 🥚 | produced artifact / plan | A duck lays an egg — concrete, inspectable, durable. Potential for action. | An artifact that can be examined and acted upon. Must carry `digest`. Requires prior `splash`. |
| `hatch` | 🐣 | approval / activation requested | An egg must be hatched. The gate between potential and motion. | Authorization requested. References a prior `egg`. Expects `bob` or `nack` or `molt`. |
| `flap` | 🪽 | execution started | A duck flaps its wings to take off. The moment of commitment. | Execution begun. The approved plan is now in motion. Irreversible boundary. |
| `perch` | 🕊️ | completed | A duck perches — flight over, wings folded, a stable resting state. | Terminal. Task complete. Nothing follows for this correlation. |
| `honk` | 🪿 | warning / policy violation | Loud, sharp, unmistakable. Something is wrong. | Warning raised. Must include `reason` in `data`. Does not terminate. |
| `molt` | 🪹 | canceled / superseded | Old feathers shed, new ones grow. The old form is gone. | Canceled or superseded. Terminal for the referenced correlation. |
| `splash` | 💦 | attach evidence | A duck splashes down, leaving visible ripples — proof of arrival. | Evidence attached. Must include `evidence` in `data`. Required before `egg`. |

---

## 3. Protocol Rules

These rules are enforced by Quack-core. A frame that violates them is rejected: the `EmitAsync` call returns a `QuackResult.Rejected`, and a `nack` frame is emitted to the trace stream for observability.

### 3.1 Sequencing rules

1. **No FLAP without HATCH + BOB** — a `flap` requires a prior `hatch` and a prior `bob` (the approval) in the same `correlation` context.
2. **No HATCH without EGG** — a `hatch` must reference a prior `egg` via `correlation`.
3. **No EGG without SPLASH** — an `egg` requires a prior `splash` in the same `context` context.
4. **No EGG without proof** — an `egg` must carry `digest` (digest binding over the artifact).

### 3.2 Content rules

5. **HONK must explain** — a `honk` must include `data.reason`.
6. **MOLT must reference** — a `molt` must include `correlation` pointing to the quack or task it supersedes.
7. **SPLASH must carry evidence** — a `splash` must include `data.evidence` with at least one reference.
8. **Broadcast verbs only** — only `quack`, `honk`, `splash`, and `molt` may omit `destination`. All other verbs require `destination`.

### 3.3 Freshness

A frame may carry a `ttl` (time to live) — a relative freshness window in milliseconds measured from the frame's `timestamp` field. If `timestamp` is omitted, `ttl` is measured from emission time.

Quack-core does **not** reject stale frames at the protocol level. Freshness checking is receiver-side: each agent decides what to do with a frame whose `ttl` has elapsed. A common pattern is to drop stale frames, log them, or emit a `nack` — but the protocol does not mandate any of these.

A frame without `ttl` has no freshness constraint. Sequences with no `ttl` on any frame do not expire. This is the default.

```
Freshness check:  (receiver_clock - frame.timestamp) > frame.ttl  →  stale
```

### 3.4 Enforcement scope

Protocol rules apply only when both sender and receiver are Quack-aware (both declare the Quack extension in their Agent Card). When one side does not speak Quack, enforcement is skipped and the frames pass through as best-effort structured data.

---

## 4. Verb Data Contracts

The `data` field carries verb-structured payload. Each verb defines its minimum required fields. Beyond those, `data` is an open map — domain extensions may add fields freely.

| Verb | Emoji | Required `data` fields | Notes |
|---|---|---|---|
| `quack` | 🦆 | none | Announcements may be bare. |
| `peck` | 🐤 | none | The request is in `summary`. |
| `bob` | 🦢 | none | Acknowledgment is self-contained. |
| `nack` | 🐦‍⬛ | none | Rejection is self-contained. |
| `egg` | 🥚 | `eggId` | The artifact identifier. Must be accompanied by `digest` on the frame. |
| `hatch` | 🐣 | `eggId` | References the egg requiring approval. |
| `flap` | 🪽 | `eggId` | References the approved plan being executed. |
| `perch` | 🕊️ | none | Completion is self-contained. |
| `honk` | 🪿 | `reason` | Why the warning was raised. |
| `molt` | 🪹 | none | `reason` is optional; `correlation` on the frame is the mandatory reference. |
| `splash` | 💦 | `evidence` | Array of evidence references. At least one entry required. |

### 4.1 Evidence reference shape

Each entry in `splash.data.evidence`:

```json
{
  "kind": "k8s.events",
  "digest": "sha256:abc123...",
  "uri": "artifact://events/default/web"
}
```

---

## 5. Encodings

### 5.1 Compliance levels

| Encoding | Requirement | Purpose |
|---|---|---|
| Quack-Text | **MUST** | Human interface, injection, logs, headers, CLI, audit trails |
| Quack-HTTP | **SHOULD** | HTTP header injection via Structured Fields (RFC 9651) |
| Quack-CBOR | **MAY** | Compact binary transport for machines |
| Quack-JSON | **MAY** | A2A interoperability, web tooling |

A conformant Quack implementation must support Quack-Text. CBOR and JSON are optional bridges.

All encodings must round-trip through the abstract Frame model:

```
Quack-Text → Frame → Quack-JSON
Quack-JSON → Frame → Quack-CBOR
Quack-CBOR → Frame → Quack-Text
```

### 5.2 Quack-Text

The mandatory text encoding. Designed for human readability in logs, CLIs, HTTP headers, and audit trails.

#### Grammar (ABNF)

```abnf
quack-text = "QK1" SP verb *( SP key-value)

verb       = "quack" / "peck" / "bob" / "nack" / "egg" / "hatch"
           / "flap" / "perch" / "honk" / "molt" / "splash"

key-value  = key "=" value
key        = 1*(ALPHA / DIGIT)             ; lowercase alphanumeric
value      = bare-token / quoted-string

bare-token = 1*(unreserved)                ; no spaces, "=", or DQUOTE
quoted-string = DQUOTE *(escaped-char / %x20-21 / %x23-5B / %x5D-7E) DQUOTE
escaped-char = "\" ("\" / DQUOTE)

unreserved = ALPHA / DIGIT / "-" / "." / "_" / ":" / "/" / "@" / "+"
```

#### Text field mapping

| Frame field | Text key |
|---|---|
| `verb` | (the verb itself, first token after `QK1`) |
| `version` | (implicit in `QK1` prefix) |
| `id` | `quackId` |
| `timestamp` | `timestamp` |
| `source` | `source` |
| `destination` | `destination` |
| `context` | `context` |
| `correlation` | `correlation` |
| `risk` | `risk` (values: `none`, `low`, `medium`, `high`, `critical`) |
| `summary` | `summary` |
| `digest` | `digest` |
| `ttl` | `ttl` (value in ms) |
| `tone` | `tone` (values: `serious-duck`, `playful-duck`, `angry-goose`, `sleepy-duckling`) |
| `data` | not encoded inline — reference by digest: `evidence=@sha256:...` |

#### Examples

```
QK1 quack quackId=01JQA1X5Z8W7M source=observer context=k8s/default/web risk=medium summary="deployment unavailable"

QK1 splash quackId=01JQA2X6Z9W8M source=observer context=k8s/default/web evidence=@sha256:abc123

QK1 egg quackId=01JQA3Y7A0X9N source=planner destination=executor context=k8s/default/web correlation=plan-456 digest=sha256:abc123 eggId=01JQA3Y7A0X9N risk=medium summary="restart deployment plan"

QK1 hatch quackId=01JQA4Z8B1Y0P source=executor destination=human correlation=plan-456 eggId=01JQA3Y7A0X9N risk=medium ttl=300000 summary="approval required"

QK1 bob quackId=01JQA5A9C2Z1Q source=human destination=executor correlation=plan-456 summary="approved"

QK1 flap quackId=01JQA6B0D3A2R source=executor destination=gateway correlation=plan-456 eggId=01JQA3Y7A0X9N digest=sha256:abc123 summary="execute approved plan"

QK1 perch quackId=01JQA7C1E4B3S source=gateway destination=executor correlation=plan-456 summary="deployment restarted"
```

#### Pond-wide (broadcast)

```
QK1 quack quackId=01JQA8D2F5C4T source=observer context=k8s/default/web risk=high summary="image drift detected"

QK1 honk quackId=01JQA9E3G6D5U source=gateway correlation=plan-456 risk=critical data.reason="digest mismatch"
```

#### Forward compatibility

Unknown keys must be accepted and preserved. A `q=2` frame may introduce new keys; a `q=1` parser carries them forward without rejecting the frame.

#### Binary data

Quack-Text does not carry binary payloads inline. Evidence and artifacts are referenced by digest (`@sha256:...`). The actual payload lives at the referenced location.

### 5.3 Quack-CBOR

Binary encoding using CBOR (RFC 8949). Integer keys for compactness.

| CBOR key | Field |
|---|---|---|
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

Media type: `application/vnd.quack+cbor`

### 5.4 Quack-JSON

JSON encoding for A2A interoperability. Lowercase field names match the canonical wire names.

```json
{
  "version": 1,
  "verb": "egg",
  "id": "01JQA3Y7A0X9N",
  "source": "planner",
  "destination": "executor",
  "context": "k8s/default/web",
  "correlation": "plan-456",
  "risk": "m",
  "ttl": 3600000,
  "tone": "serious-duck",
  "digest": "sha256:abc123",
  "summary": "restart deployment plan",
  "data": {
    "eggId": "01JQA3Y7A0X9N"
  }
}
```

Media type: `application/vnd.quack+json`

### 5.5 Quack-HTTP (Structured Fields)

For HTTP header injection, Quack frames may be encoded as HTTP Structured Fields (RFC 9651). This is the recommended encoding for `Quack` and `Quack-Trace` headers.

```http
Quack: version=1, verb="egg", id="01JQA3Y7A0X9N", source="planner", destination="executor", context="k8s/default/web", correlation="plan-456", risk="m", summary="restart deployment plan"
Quack-Trace: context="k8s/default/web", correlation="plan-456"
```

Structured Fields values use RFC 9651 Dictionary syntax. String values are quoted; integers and tokens are bare. Unknown keys must be preserved. This encoding carries the same fields as Quack-JSON but uses comma-separated key=value pairs instead of JSON object syntax.

Quack-HTTP is optional. Quack-Text (inline `QK1 ...` string in a single header value) and Quack-JSON (base64-encoded header value) are also valid HTTP header encodings. Implementations SHOULD prefer Quack-HTTP for new integrations.

### 5.6 Freshness projection

Core Quack uses `ttl` (relative milliseconds from `timestamp`) for freshness. Profile specifications such as `quack-mutation-v0` project this into an absolute `expiresAt` (ISO 8601 UTC) field when they target A2A container semantics. The projection rule is:

```
expiresAt = (frame.timestamp || now) + frame.ttl
```

A profile that carries `expiresAt` in its frame model MUST round-trip it through the core `ttl` field. Receivers MUST treat `expiresAt` in the past the same way they treat an elapsed `ttl` — receiver-side staleness, not a protocol-level rejection.

---

## 6. Rejection

When Quack-core rejects a frame (violation of §3), two things happen:

1. **The `EmitAsync` call returns `QuackResult.Rejected(reason)`.** The sender handles it in code without parsing frames.

2. **A `nack` frame is emitted to the trace stream.** The pond sees the rejection for observability and debugging.

```
QK1 hatch quackId=01JQA4Z8B1Y0P source=executor destination=human correlation=plan-456 eggId=01JQA3Y7A0X9N risk=medium summary="approval required" ttl=300000
```

---

## 7. A2A Integration

Quack rides inside A2A as an extension. A2A owns agent discovery, protocol
bindings, task lifecycle, message/artifact containers, and extension activation;
Quack contributes a lightweight semantic frame that can be carried by those
containers.

This section targets A2A Protocol `1.0`. Patch releases such as `1.0.1` keep
the same protocol compatibility value.

### 7.1 Agent Card declaration

A Quack-aware agent declares support through
`AgentCard.capabilities.extensions[]`. If the agent exposes multiple A2A
interfaces, each interface advertises its A2A `protocolVersion`.

```json
{
  "name": "Kubernetes MCP Guard Planner",
  "description": "Plans safe Kubernetes remediation actions.",
  "supportedInterfaces": [
    {
      "url": "https://planner.example.com/a2a",
      "protocolBinding": "JSONRPC",
      "protocolVersion": "1.0"
    }
  ],
  "capabilities": {
    "extensions": [
      {
        "uri": "https://quack.dev/ext/quack/v0.1",
        "description": "Quack semantic protocol for agent coordination.",
        "required": false,
        "params": {
          "maxRisk": "high",
          "maxTtl": 3600000,
          "maxQuackVersion": 1,
          "supportedEncodings": [
            "application/vnd.quack+json",
            "text/vnd.quack"
          ]
        }
      }
    ]
  }
}
```

`maxRisk` gates delivery at the protocol level (§1.3). Frames with `risk`
exceeding this threshold are rejected before reaching the agent.

`maxTtl` is the maximum freshness window this agent accepts, in milliseconds.
Frames with `ttl` exceeding this value are rejected before reaching the agent.
Omitting `maxTtl` means the agent accepts any freshness window.

`maxQuackVersion` is the highest core Quack `version` value the agent accepts.

`required` controls whether non-Quack agents can interact with this agent.
`false` means non-Quack callers are accepted without Quack enforcement. `true`
means the agent expects the caller to activate the extension, for example with
the A2A `A2A-Extensions` header on HTTP bindings.

### 7.2 Lightweight signals — metadata

Small Quack-Text frames (`quack`, `honk`, `bob`, `nack`) may be carried in A2A
`Message.metadata` under the extension URI. This is useful for logs, routing,
trace hints, and low-friction agent coordination.

```json
{
  "role": "ROLE_AGENT",
  "parts": [{ "text": "Deployment default/web is crashlooping." }],
  "metadata": {
    "https://quack.dev/ext/quack/v0.1": "QK1 quack quackId=01J... source=observer context=k8s/default/web risk=medium summary=\"deployment unavailable\""
  }
}
```

### 7.3 Payload frames — unified Part data

Structured frames (`egg`, `splash`, `flap`, `hatch`) are carried in an A2A 1.0
unified `Part` with `data` and `mediaType`. The legacy A2A `kind` discriminator
is not used.

```json
{
  "role": "ROLE_AGENT",
  "parts": [
    { "text": "Proposed restart plan." },
    {
      "data": {
        "version": 1,
        "verb": "egg",
        "id": "01JQA3Y7A0X9N",
        "source": "planner",
        "destination": "executor",
        "context": "k8s/default/web",
        "correlation": "plan-456",
        "risk": "m",
        "digest": "sha256:abc123",
        "summary": "restart deployment plan",
        "data": { "eggId": "01JQA3Y7A0X9N" }
      },
      "mediaType": "application/vnd.quack+json"
    }
  ]
}
```

---

## 8. Trace Format

Quack traces render as emoji-annotated Quack-Text lines. This is the human-facing audit view.

```
🦆 QK1 quack  quackId=01JQA1 source=observer context=k8s/default/web risk=medium summary="deployment unavailable"
💦 QK1 splash quackId=01JQA2 source=observer context=k8s/default/web evidence=@sha256:abc123
🥚 QK1 egg    quackId=01JQA3 source=planner destination=executor context=k8s/default/web correlation=plan-456 digest=sha256:abc123 eggId=01JQA3 risk=medium summary="restart plan"
🐣 QK1 hatch  quackId=01JQA4 source=executor destination=human correlation=plan-456 eggId=01JQA3 risk=medium ttl=300000 summary="approval required"
🦢 QK1 bob    quackId=01JQA5 source=human destination=executor correlation=plan-456 summary="approved"
🪽 QK1 flap   quackId=01JQA6 source=executor destination=gateway correlation=plan-456 eggId=01JQA3 digest=sha256:abc123 summary="execute approved plan"
🕊️ QK1 perch  quackId=01JQA7 source=gateway destination=executor correlation=plan-456 summary="deployment restarted"
```

Verb emoji mapping:

| Verb | Emoji |
|---|---|
| `quack` | 🦆 |
| `peck` | 🐤 |
| `bob` | 🦢 |
| `nack` | 🐦‍⬛ |
| `egg` | 🥚 |
| `hatch` | 🐣 |
| `flap` | 🪽 |
| `perch` | 🕊️ |
| `honk` | 🪿 |
| `molt` | 🪹 |
| `splash` | 💦 |

A machine-readable structured trace format (no emoji, aligned columns, explicit sequencing) is also available via `quack trace --structured`.

---

## 9. Versioning

The protocol version is carried in `version`. v0.1 = `1`.

When a parser encounters a frame with a higher `version` value:

- If the frame is structurally parseable (known fields present and valid), accept it. Ignore unknown fields.
- If the frame cannot be parsed (unknown verb, missing required field of a known type), reject it with `nack`.

This means `version=2` frames are accepted by `version=1` parsers as long as the core frame structure is intact. Senders should use the Agent Card's declared `maxQuackVersion` to avoid emitting frames that a receiver cannot understand (negotiation is planned for a future version).

---

## 10. Safety Poem

> No mutation without a FLAP.
> No FLAP without a HATCH.
> No HATCH without an EGG.
> No EGG without SPLASH.

These are not just poetry. They are protocol invariants enforced at the frame level (§3).
