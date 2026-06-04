Quack!
A tiny semantic protocol for agent coordination.

Quack runs on A2A.
Quack speaks in typed envelopes.
Quack never mutates without proof.
Quack loudly when something is unsafe.

If an agent cannot express the next step as a clear quack, maybe it should not do it.

Agents can still use natural language, but every meaningful coordination step must have a tiny typed Quack envelope next to it.

small
injectable
language-agnostic
low-level
usable with A2A
not married to JSON

Quack does not require agents to speak JSON.
It only asks them to quack clearly.

Quack is text-first, binary-ready, and JSON-friendly.

Text is for humans and injection.
CBOR is for machines and compact transport.
JSON is for A2A and interoperability.

No mutation without a FLAP. No FLAP without a HATCH. No HATCH without an EGG. No EGG without SPLASH.

All encodings MUST round-trip through the abstract Quack Frame model.

Quack-Text → Frame → Quack-JSON
Quack-JSON → Frame → Quack-CBOR
Quack-CBOR → Frame → Quack-Text