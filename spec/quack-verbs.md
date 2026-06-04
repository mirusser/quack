# Quack Verbs — v0.1

Quack defines 11 verbs. Each verb carries a semantic role, a duck-natural metaphor, and a technical contract.

| Verb | Emoji | Role | Duck metaphor | Technical contract |
|---|---|---|---|---|
| `quack` | 🦆 | announce / observe | A duck quacks to declare presence and state to the pond — the fundamental unit of duck communication. | "This happened." Does not request action. |
| `peck` | 🐤 | request | A duck pecks at something to probe, demand attention, or draw a response. Persistent and directional. | "I need something from you." Bounded request. Expects `bob` or `nack`. |
| `bob` | 🦢 | accepted / understood | A swan bows its head — a graceful, unmistakable signal of recognition and trust. | "Received and understood." Also serves as approval after `hatch`. |
| `nack` | 🐦‍⬛ | rejected / cannot comply | A blackbird turns away. Dark, decisive, final. No ambiguity. | "Cannot comply." Terminal rejection of a `peck` or `hatch`. |
| `egg` | 🥚 | produced artifact / plan | A duck lays an egg — concrete, inspectable, durable. Potential for action, not yet action. | An artifact or plan. Must carry `digest`. Requires prior `splash`. |
| `hatch` | 🐣 | approval / activation requested | An egg must be hatched before it becomes a duckling. The gate between potential and motion. | Authorization requested. References a prior `egg`. Expects `bob`, `nack`, or `molt`. |
| `flap` | 🪽 | execution started | A duck flaps its wings to take off — the moment of commitment, the transition from still to airborne. | Execution begun. The approved plan is now in motion. Irreversible boundary. |
| `perch` | 🕊️ | completed | A duck perches: flight over, wings folded, a stable resting state. Still, but present. | Terminal. Task complete. Nothing follows for this correlation. |
| `honk` | 🪿 | warning / policy violation | Loud, sharp, unmistakable. Something is wrong. (Cross-bird extensibility: this is goose energy.) | Warning raised. Must include `reason` in `data`. Does not terminate. |
| `molt` | 🪹 | canceled / superseded | Old feathers shed, new ones grow. The old form is gone. Irreversible. | Canceled or superseded. Terminal for the referenced correlation (`correlation`). |
| `splash` | 💦 | attach evidence | A duck splashes down, leaving visible ripples — proof of arrival, a mark anyone can see. | Evidence attached. Must include `evidence` in `data`. Required before `egg`. |

## Addressing model

Frames are point-to-point via `destination`. When `destination` is omitted, the frame is **pond-wide** — every agent in the context receives it.

Only announce-type verbs may broadcast: `quack`, `honk`, `splash`, `molt`. All other verbs require `destination`.

**Relay pattern.** Human-in-the-loop approval is out of band. When a human approves a `hatch` through a browser, the gateway or approval service emits the `bob` (or `nack`) on their behalf. From Quack's perspective, the gateway *is* the approver.

## Protocol-level rules

These rules are enforced by Quack-core. Violating frames are rejected: sender gets `QuackResult.Rejected`, and a `nack` frame is emitted to the trace.

1. **No FLAP without HATCH + BOB** — `flap` requires prior `hatch` + `bob` in the same `correlation`.
2. **No HATCH without EGG** — `hatch` must reference a prior `egg` via `correlation`.
3. **No EGG without SPLASH** — `egg` requires prior `splash` in the same `context`.
4. **No EGG without proof** — `egg` must carry `digest`.
5. **HONK must explain** — `honk` must include `data.reason`.
6. **MOLT must reference** — `molt` must include `correlation`.
7. **SPLASH must carry evidence** — `splash` must include `data.evidence` with ≥1 entry.
8. **Broadcast verbs only** — only `quack`, `honk`, `splash`, `molt` may omit `destination`.

## Freshness

A frame may carry `ttl` — a relative freshness window in milliseconds from `timestamp`. Quack-core does not reject stale frames; freshness checking is receiver-side. A frame without `ttl` has no freshness constraint.

> No mutation without a FLAP. No FLAP without a HATCH. No HATCH without an EGG. No EGG without SPLASH.
