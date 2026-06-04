# 🦆 Quack!

**A tiny semantic protocol for agent coordination.**

> Quack rides inside [A2A](https://google.github.io/A2A/) as an extension.  
> Quack speaks in typed envelopes.  
> Quack never mutates without proof.  
> Quack loudly when something is unsafe.

If an agent cannot express the next step as a clear quack,
maybe it should not do it.

Agents can still use natural language, but every meaningful coordination
step must have a tiny typed **Quack** envelope next to it.

> **Quack is:**
>
> small &nbsp;·&nbsp; injectable &nbsp;·&nbsp; language-agnostic
> low-level &nbsp;·&nbsp; not married to JSON

## 🪶 Verbs

Eleven typed envelopes. Verb reference: [`spec/quack-verbs.md`](spec/quack-verbs.md).

| Verb | Emoji | Kind | What it means |
|------|-------|------|----------------|
| `quack` | 🦆 | announce | "Something happened" |
| `peck` | 🐤 | request | "Please do this" |
| `bob` | 🦢 | ack | "Received / approved" |
| `nack` | 🐦‍⬛ | reject | "Cannot do this" |
| `splash` | 💦 | announce | "Here is the evidence" |
| `egg` | 🥚 | request | "This is the plan" |
| `hatch` | 🐣 | request | "Approve this plan" |
| `flap` | 🪽 | request | "Execute the approved plan" |
| `perch` | 🕊️ | announce | "Finished / landed" |
| `honk` | 🪿 | alarm | "Something is wrong" |
| `molt` | 🪹 | alarm | "Plan no longer valid" |

## 📦 Encodings

Four encodings, one canonical frame. All round-trip through the abstract
Quack Frame model.

| Encoding | Role | Priority |
|----------|------|----------|
| Quack-Text | Humans, injection, logging | **MUST** |
| Quack-HTTP | A2A headers, agent cards | SHOULD |
| Quack-JSON | A2A bodies, tooling | MAY |
| Quack-CBOR | Machines, compact transport | MAY |

Quack does not require agents to speak JSON.
It only asks them to quack clearly.

## 🔐 Proof Chain for Mutations

```
💦 SPLASH  →  🥚 EGG  →  🐣 HATCH  →  🦢 BOB  →  🪽 FLAP
(evidence)     (plan)     (human review)  (approval)   (execution)
```

Human approval is out-of-band: the gateway emits `bob`/`nack` as a relay.

Delivery rules: **risk gating**, **dst/addressing**, **ttl freshness**.
→ [`spec/quack-0.1.md`](spec/quack-0.1.md)

## 📄 Specs

| Document | What it covers |
|----------|---------------|
| [`spec/quack-0.1.md`](spec/quack-0.1.md) | Core protocol — frame model, verbs, encodings, delivery rules, A2A integration |
| [`spec/quack-verbs.md`](spec/quack-verbs.md) | Quick-reference verb table |
| [`spec/quack.schema.json`](spec/quack.schema.json) | JSON Schema for all 11 verbs |
| [`spec/quack-mutation-v0.1.md`](spec/quack-mutation-v0.1.md) | A2A mutation profile — state machine, digest rules, conformance fixtures |
| [`spec/quack-sdk-0.1.md`](spec/quack-sdk-0.1.md) | .NET SDK — frame model, sinks, validation, DI, error handling |
| [`spec/quack-sdk-adapters-0.1.md`](spec/quack-sdk-adapters-0.1.md) | .NET SDK — codecs, A2A adapter, ASP.NET middleware, URIs, traces |
| [`spec/quack-sdk-future-0.1.md`](spec/quack-sdk-future-0.1.md) | Future — CLI, profiles, OTel, MCP, signing, multi-language |
