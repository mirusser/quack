# Quack Negotiate v0.1

`quack-negotiate-v0` is an A2A 1.0 profile extension for dynamic,
context-aware skill negotiation between agents.

Quack does not replace A2A. A2A owns discovery, transport, task lifecycle,
streaming, authentication, authorization, message/artifact containers, and
protocol binding semantics. Quack adds one narrow thing: a structured negotiation
exchange where agents determine — **for this specific task, in this specific
context** — which skills are meaningfully applicable and at what assurance level.

> **Guiding design question** (keep this in mind for every design decision):
> *"Given this specific task and context, do your capabilities make sense right now?"*
>
> It is not enough that an agent *has* a skill. The question is whether that
> skill is *applicable* to the task at hand, *available* under current
> constraints, and *assured* at a level the caller can rely on.

**Negotiation is advisory.** The negotiation profile establishes a capability
contract between agents, but it does not enforce execution. The contract records
what was agreed — skill, constraints, assurance level, validity window — but
enforcement is the responsibility of the execution profile (e.g.,
`quack-mutation-v0`). If either agent violates the agreed terms during execution,
the other SHOULD `honk` (via the core protocol). The negotiation frames serve as
the audit trail for what was promised versus what was delivered.

This profile is an A2A JSON projection of the core Quack frame model. It uses
the canonical Quack field names directly in camelCase JSON — `version`, `verb`,
`id`, `source`, `destination`, etc.

---

## 1. Relationship to Mutation Profile

The negotiation profile is orthogonal to the mutation profile
(`quack-mutation-v0`). They can be used independently or together:

| Profile | Answers the question... | Lifecycle |
|---------|------------------------|-----------|
| `quack-negotiate-v0` | "Given this task, can you help me — and how?" | `dabble → preen → settle/shun` |
| `quack-mutation-v0` | "Given we agreed on a plan, is every step proven?" | `splash → egg → hatch → flap → perch` |

When composed (both profiles active on the same agent):

1. **Negotiation first** — establish capability alignment and constraints.
2. **Mutation second** — execute the agreed work with digest-bound proof.

The `negotiationId` from the negotiation phase becomes the bridge: it is carried
as the `correlation` field in subsequent mutation frames, linking the capability
contract to the execution evidence.

---

## 2. A2A Binding

### 2.1 Agent Card Declaration

Agents declare support through `AgentCard.capabilities.extensions[]` using the
A2A 1.0 `AgentExtension` object.

```json
{
  "name": "Kubernetes Executor Agent",
  "description": "Executes Kubernetes remediation plans with pre-execution safety gates.",
  "version": "1.0.0",
  "supportedInterfaces": [
    {
      "url": "https://executor.example.com/a2a",
      "protocolBinding": "JSONRPC",
      "protocolVersion": "1.0"
    }
  ],
  "capabilities": {
    "streaming": true,
    "extensions": [
      {
        "uri": "https://example.org/extensions/quack-negotiate/v0",
        "description": "Supports dynamic, context-aware skill negotiation for task delegation.",
        "required": false,
        "params": {
          "maxRisk": "high",
          "maxQuackVersion": "0.1",
          "supportedVerbs": ["dabble", "preen", "settle", "shun"],
          "negotiationProfiles": ["quack-negotiate-v0"],
          "supportedMediaTypes": ["application/vnd.quack+json"]
        }
      }
    ]
  },
  "defaultInputModes": ["application/vnd.quack+json", "text/plain"],
  "defaultOutputModes": ["application/vnd.quack+json", "text/plain"],
  "skills": [
    {
      "id": "k8s-rollout-restart",
      "name": "Rollout restart deployment",
      "description": "Performs a rolling restart of a Kubernetes deployment with pre-execution safety gates: resource availability check, PDB compliance, and rollout status monitoring.",
      "tags": ["kubernetes", "deployment", "restart", "remediation"],
      "examples": [
        "Restart the deployment 'web' in namespace 'prod'",
        "Rollout restart for 'api-gateway' in 'staging'"
      ],
      "inputModes": ["application/vnd.quack+json", "text/plain"],
      "outputModes": ["application/vnd.quack+json", "text/plain"]
    },
    {
      "id": "k8s-scale-replicas",
      "name": "Scale deployment replicas",
      "description": "Scales a Kubernetes deployment to a specified replica count with resource quota checks and HPA suspension during the operation.",
      "tags": ["kubernetes", "deployment", "scale", "remediation"],
      "examples": [
        "Scale 'worker-pool' in 'prod' to 5 replicas",
        "Scale down 'batch-processor' in 'dev' to 0"
      ],
      "inputModes": ["application/vnd.quack+json", "text/plain"],
      "outputModes": ["application/vnd.quack+json", "text/plain"]
    }
  ]
}
```

Extension parameters:

| Parameter | Required | Meaning |
|---|---|---:|
| `maxRisk` | Yes | Highest risk value this agent will accept for a negotiation: `none`, `low`, `medium`, `high`, or `critical`. |
| `maxQuackVersion` | Yes | Highest Quack profile version accepted by this agent. |
| `supportedVerbs` | Yes | Must include `dabble`, `preen`, `settle`, `shun`. |
| `negotiationProfiles` | Yes | Negotiation profiles implemented. This spec defines `quack-negotiate-v0`. |
| `supportedMediaTypes` | Yes | Must include `application/vnd.quack+json` for conforming agents. |

### 2.2 Extension Activation

Clients activate the extension using A2A 1.0 service parameters. For HTTP
bindings, this means the `A2A-Extensions` header:

```http
POST /message:send HTTP/1.1
Host: executor.example.com
Content-Type: application/a2a+json
A2A-Version: 1.0
A2A-Extensions: https://example.org/extensions/quack-negotiate/v0
Authorization: Bearer token
```

The response SHOULD echo the activated extension URI through the same
binding-specific mechanism.

### 2.3 Message and Artifact Parts

Negotiation frames are carried in A2A 1.0 unified `Part` objects with:

- exactly one content field: `data`
- `mediaType: "application/vnd.quack+json"`
- the containing `Message.extensions[]` or `Artifact.extensions[]` including
  the negotiate extension URI

Negotiation frames are conversational — they flow between agents as part of a
task's message exchange. Accepted negotiations SHOULD also be recorded as A2A
Artifacts to provide a durable record of the capability contract.

---

## 3. Frame Model

All negotiation frames use camelCase JSON fields and share the canonical Quack
frame structure.

| Field | Required | Type | Meaning |
|---|---|---:|---|---|
| `version` | Yes | string | Profile version. This document defines `0.1`. |
| `profile` | Yes | string | Must be `quack-negotiate-v0`. |
| `verb` | Yes | string | One of `dabble`, `preen`, `settle`, `shun`. |
| `id` | Yes | string | Opaque frame identifier. UUIDs are recommended. |
| `timestamp` | Yes | string | ISO 8601 UTC timestamp with millisecond precision when available. |
| `source` | Yes | string | Quack-speaking sender (the agent emitting this frame). |
| `destination` | Yes | string | Intended receiver. Negotiation frames are always point-to-point. |
| `context` | Yes | string | A2A context scope for this negotiation. |
| `taskId` | No | string | A2A task scope when the negotiation belongs to a specific task. |
| `correlation` | No | string | Links to a prior negotiation or the enclosing workflow. The `negotiationId` is carried here in `settle` and `shun` frames. |
| `risk` | Yes | string | `none`, `low`, `medium`, `high`, or `critical`. |
| `expiresAt` | No | string | Optional ISO 8601 UTC freshness bound for this negotiation frame. |
| `summary` | No | string | Human-readable one-line summary. |
| `data` | Yes | object | Verb-specific payload (§4). |

Container consistency rules:

- If the containing A2A Message has `contextId`, the frame `context` MUST
  match it.
- If the containing A2A Message has `taskId`, the frame `taskId` MUST match it.
- A `destination` is always required for negotiation frames. Pond-wide
  negotiation is not meaningful.

---

## 4. Verbs

### 4.1 `dabble` (duck tips forward, beak underwater — searching below the surface)

`dabble` asks a target agent: "Given this specific task and context, which of
your skills are applicable, and at what assurance level?"

The `dabble` verb is the entry point for negotiation. It carries enough
task-specific context for the receiver to make a meaningful applicability
determination — not just "do you have this skill?" but "does this skill make
sense for *this* task, in *this* context, right now?"

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `taskIntent` | string | Human-readable description of what the caller wants done. The receiver uses this to match against its skills. |
| `requiredCapabilities` | array of string | Capability tags the caller believes are needed (e.g., `["k8s-exec", "prod-access"]`). MAY be empty if the caller is doing open-ended discovery. |

Optional `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `constraints` | object | Caller-side constraints the receiver should consider when forming a preen: deadline, risk tolerance, required input schemas, etc. |
| `preferredSkills` | array of string | Skill IDs the caller already knows about and prefers. Hints to narrow the response. |

Example:

```json
{
  "version": "0.1",
  "profile": "quack-negotiate-v0",
  "verb": "dabble",
  "id": "01JQA8B2C3D4E5F6G7H8I9J0K1",
  "timestamp": "2026-06-04T14:00:00.000Z",
  "source": "planner",
  "destination": "executor",
  "context": "k8s/prod/web",
  "risk": "medium",
  "summary": "Restart deployment web in prod",
  "data": {
    "taskIntent": "Restart deployment 'web' in namespace 'prod' to recover from crashloop — 3 of 5 pods are failing with OOMKilled.",
    "requiredCapabilities": ["k8s-exec", "prod-access"],
    "constraints": {
      "deadline": "2026-06-04T14:05:00.000Z",
      "maxDowntime": "30s",
      "preApprovalRequired": false
    },
    "preferredSkills": ["k8s-rollout-restart"]
  }
}
```

### 4.2 `preen` (duck preens feathers — displaying quality and fitness)

`preen` responds to a `dabble` with: "For this task, I can provide skill X under
these specific constraints and at this assurance level."

Multiple `preen` frames MAY be sent in response to a single `dabble` — the
target may offer multiple applicable skills, each in its own `preen` frame.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `skillId` | string | The skill being offered. MUST match an `id` from the target's Agent Card `skills[]`. |
| `applicability` | string | Assessment of how well the skill matches the task: `full` (exact match), `partial` (applicable with caveats), or `conditional` (applicable only if additional constraints are met). |
| `constraints` | object | Constraints the offeror imposes if this skill is used. Must be satisfied before execution. |
| `assuranceLevel` | string | The offeror's confidence in executing under these constraints: `high`, `medium`, or `low`. |

Optional `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `applicabilityNotes` | string | Human-readable explanation of the applicability assessment. Especially important for `partial` and `conditional` matches. |
| `preExecutionGates` | array of string | Gates the caller MUST pass before the skill can be invoked. E.g., `["resource-quota-check", "pdb-validation"]`. |
| `estimatedDuration` | string | ISO 8601 duration. How long the skill execution is expected to take under normal conditions. |
| `inputSchema` | object | **Optional.** A JSON Schema describing the structured inputs the skill expects. Not all skills have or need structured schemas — simple skills may rely on the textual `taskIntent` alone. When present, callers can use it to validate their inputs before invocation. |
| `requiredInputs` | array of object | Human-readable description of inputs the skill requires. Each object has `name`, `type`, and `description`. Useful when `inputSchema` is absent or as a summary alongside it. |

Example:

```json
{
  "version": "0.1",
  "profile": "quack-negotiate-v0",
  "verb": "preen",
  "id": "01JQA8C5F7H9J1L3N5P7R9T1V3",
  "timestamp": "2026-06-04T14:00:02.000Z",
  "source": "executor",
  "destination": "planner",
  "context": "k8s/prod/web",
  "correlation": "01JQA8B2C3D4E5F6G7H8I9J0K1",
  "risk": "medium",
  "summary": "Can restart via rollout restart within 30s",
  "data": {
    "skillId": "k8s-rollout-restart",
    "applicability": "full",
    "constraints": {
      "freshnessWindow": "5m",
      "maxParallelRestarts": 1,
      "requiresStableApiServer": true,
      "requiresQuotaHeadroom": "20%"
    },
    "assuranceLevel": "high",
    "applicabilityNotes": "Rollout restart is the standard remediation for crashlooping deployments. Currently 3/5 pods failing — restart will trigger new pod creation. No PDB violation expected.",
    "preExecutionGates": [
      "resource-quota-check",
      "pdb-validation",
      "apiserver-health-check"
    ],
    "estimatedDuration": "PT45S",
    "requiredInputs": [
      {
        "name": "namespace",
        "type": "string",
        "description": "Kubernetes namespace. Must match context scope."
      },
      {
        "name": "deploymentName",
        "type": "string",
        "description": "Name of the deployment to restart."
      }
    ]
  }
}
```

### 4.3 `settle` (duck settles on the water — decision made, coming to rest)

`settle` binds the caller to a specific preen: "I settle on your preen for skill X
under these agreed constraints."

An `settle` frame MUST reference a specific `preen` frame by its `id` (via
`correlation`). It forms a binding contract between the two agents — the
negotiation is now concluded and the agreed skill is ready for invocation.

An `settle` is terminal for the negotiation phase. The `negotiationId` carried
in `data` becomes the durable reference for the capability contract and SHOULD
be used as the `correlation` in subsequent work frames.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `negotiationId` | string | Durable identifier for this negotiation contract. Used to link subsequent work to the capability agreement. |
| `skillId` | string | The accepted skill. MUST match the `skillId` from the referenced `preen`. |
| `agreedConstraints` | object | The final constraints both sides agree to. MUST be a subset or refinement of the `preen` constraints — the settler cannot unilaterally relax constraints. |
| `agreedAssuranceLevel` | string | The assurance level both sides accept: `high`, `medium`, or `low`. |
| `validFrom` | string | ISO 8601 UTC start of the capability contract's validity. |
| `validUntil` | string | ISO 8601 UTC end of the capability contract's validity. After this time, the contract MUST be renegotiated. |

Example:

```json
{
  "version": "0.1",
  "profile": "quack-negotiate-v0",
  "verb": "settle",
  "id": "01JQA8D7G9K1M3O5Q7S9U1W3Y5",
  "timestamp": "2026-06-04T14:00:05.000Z",
  "source": "planner",
  "destination": "executor",
  "context": "k8s/prod/web",
  "correlation": "01JQA8C5F7H9J1L3N5P7R9T1V3",
  "risk": "medium",
  "summary": "Accepted: k8s-rollout-restart for prod/web",
  "data": {
    "negotiationId": "neg-01JQA8D7G9",
    "skillId": "k8s-rollout-restart",
    "agreedConstraints": {
      "freshnessWindow": "5m",
      "maxParallelRestarts": 1,
      "requiresStableApiServer": true,
      "requiresQuotaHeadroom": "20%"
    },
    "agreedAssuranceLevel": "high",
    "validFrom": "2026-06-04T14:00:05.000Z",
    "validUntil": "2026-06-04T14:05:05.000Z"
  }
}
```

### 4.4 `shun` (duck turns away — will not engage)

`shun` rejects a `dabble` or a specific `preen`: "I cannot or will not handle
this task / preen."

A `shun` is terminal for that negotiation path. The caller MAY re-dabble with
adjusted constraints or different `requiredCapabilities`. A `shun` with no
`correlation` means "I reject the entire dabble — none of my skills apply."

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `reasonCode` | string | Stable machine-readable reason: `no_applicable_skills`, `skill_unavailable`, `constraint_violation`, `risk_too_high`, `context_mismatch`, or `insufficient_assurance`. |
| `reason` | string | Human-readable explanation. This is the most important field — it tells the caller *why* the shun happened, enabling them to adjust and re-dabble. |

Optional `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `suggestedSkillIds` | array of string | If the target has skills the caller didn't ask for but might be relevant, list them here. Turns a rejection into a helpful redirect. |
| `suggestedConstraintRelaxation` | object | If the shun is due to constraint violation, suggest which constraints to relax. |

Example (skill unavailable):

```json
{
  "version": "0.1",
  "profile": "quack-negotiate-v0",
  "verb": "shun",
  "id": "01JQA8E9H1K3M5O7Q9S1U3W5Y7",
  "timestamp": "2026-06-04T14:00:03.000Z",
  "source": "executor",
  "destination": "planner",
  "context": "k8s/prod/web",
  "correlation": "01JQA8B2C3D4E5F6G7H8I9J0K1",
  "risk": "low",
  "summary": "k8s-rollout-restart unavailable — maintenance window active",
  "data": {
    "reasonCode": "skill_unavailable",
    "reason": "Rollout restart is currently gated by an active maintenance window for namespace 'prod'. The window ends at 2026-06-04T14:15:00Z.",
    "suggestedSkillIds": ["k8s-scale-replicas"],
    "suggestedConstraintRelaxation": {
      "deadline": "extend beyond 2026-06-04T14:15:00.000Z"
    }
  }
}
```

---

## 5. JSON Schema

This is the normative schema shape. Implementations MAY split it into separate
schema files, but conformance tests MUST validate the same constraints.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://example.org/schemas/quack-negotiate-v0.schema.json",
  "title": "Quack Negotiate v0 Frame",
  "type": "object",
  "required": [
    "version",
    "profile",
    "verb",
    "id",
    "timestamp",
    "source",
    "destination",
    "context",
    "risk",
    "data"
  ],
  "properties": {
    "version": { "const": "0.1" },
    "profile": { "const": "quack-negotiate-v0" },
    "verb": {
      "enum": ["dabble", "preen", "settle", "shun"]
    },
    "id": { "type": "string", "minLength": 1 },
    "timestamp": { "type": "string", "format": "date-time" },
    "source": { "type": "string", "minLength": 1 },
    "destination": { "type": "string", "minLength": 1 },
    "context": { "type": "string", "minLength": 1 },
    "taskId": { "type": "string", "minLength": 1 },
    "correlation": { "type": "string", "minLength": 1 },
    "risk": {
      "enum": ["none", "low", "medium", "high", "critical"]
    },
    "expiresAt": { "type": "string", "format": "date-time" },
    "summary": { "type": "string", "minLength": 1 },
    "data": { "type": "object" }
  },
  "allOf": [
    {
      "if": {
        "properties": { "verb": { "const": "dabble" } }
      },
      "then": {
        "properties": {
          "data": {
            "$ref": "#/$defs/queryData"
          }
        }
      }
    },
    {
      "if": {
        "properties": { "verb": { "const": "preen" } }
      },
      "then": {
        "properties": {
          "data": {
            "$ref": "#/$defs/offerData"
          }
        }
      }
    },
    {
      "if": {
        "properties": { "verb": { "const": "settle" } }
      },
      "then": {
        "properties": {
          "data": {
            "$ref": "#/$defs/settleData"
          }
        }
      }
    },
    {
      "if": {
        "properties": { "verb": { "const": "shun" } }
      },
      "then": {
        "properties": {
          "data": {
            "$ref": "#/$defs/shunData"
          }
        }
      }
    }
  ],
  "$defs": {
    "dabbleData": {
      "type": "object",
      "required": ["taskIntent", "requiredCapabilities"],
      "properties": {
        "taskIntent": { "type": "string", "minLength": 1 },
        "requiredCapabilities": {
          "type": "array",
          "items": { "type": "string", "minLength": 1 }
        },
        "constraints": { "type": "object" },
        "preferredSkills": {
          "type": "array",
          "items": { "type": "string", "minLength": 1 }
        }
      },
      "additionalProperties": true
    },
    "preenData": {
      "type": "object",
      "required": ["skillId", "applicability", "constraints", "assuranceLevel"],
      "properties": {
        "skillId": { "type": "string", "minLength": 1 },
        "applicability": {
          "enum": ["full", "partial", "conditional"]
        },
        "constraints": { "type": "object" },
        "assuranceLevel": {
          "enum": ["high", "medium", "low"]
        },
        "applicabilityNotes": { "type": "string", "minLength": 1 },
        "preExecutionGates": {
          "type": "array",
          "items": { "type": "string", "minLength": 1 }
        },
        "estimatedDuration": { "type": "string", "minLength": 1 },
        "inputSchema": { "type": "object" },
        "requiredInputs": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["name", "type"],
            "properties": {
              "name": { "type": "string", "minLength": 1 },
              "type": { "type": "string", "minLength": 1 },
              "description": { "type": "string", "minLength": 1 }
            },
            "additionalProperties": true
          }
        }
      },
      "additionalProperties": true
    },
    "settleData": {
      "type": "object",
      "required": [
        "negotiationId",
        "skillId",
        "agreedConstraints",
        "agreedAssuranceLevel",
        "validFrom",
        "validUntil"
      ],
      "properties": {
        "negotiationId": { "type": "string", "minLength": 1 },
        "skillId": { "type": "string", "minLength": 1 },
        "agreedConstraints": { "type": "object" },
        "agreedAssuranceLevel": {
          "enum": ["high", "medium", "low"]
        },
        "validFrom": { "type": "string", "format": "date-time" },
        "validUntil": { "type": "string", "format": "date-time" }
      },
      "additionalProperties": true
    },
    "shunData": {
      "type": "object",
      "required": ["reasonCode", "reason"],
      "properties": {
        "reasonCode": {
          "enum": [
            "no_applicable_skills",
            "skill_unavailable",
            "constraint_violation",
            "risk_too_high",
            "context_mismatch",
            "insufficient_assurance"
          ]
        },
        "reason": { "type": "string", "minLength": 1 },
        "suggestedSkillIds": {
          "type": "array",
          "items": { "type": "string", "minLength": 1 }
        },
        "suggestedConstraintRelaxation": { "type": "object" }
      },
      "additionalProperties": true
    }
  },
  "additionalProperties": false
}
```

---

## 6. State Machine

The negotiation state machine is scoped to an A2A `context` and a `negotiationId`.

```text
dabble → preen → settle
   \       \      \→ shun
    \       \→ shun
     \
      → shun
```

Rules:

1. A `dabble` initiates negotiation for a specific `context` and optional `taskId`.
   Multiple `dabble` frames MAY be sent as the caller refines its requirements.
2. An `preen` MUST reference a preceding `dabble` by carrying the `dabble` frame's
   `id` in the `correlation` field.
3. Multiple `preen` frames MAY be sent in response to a single `dabble`. Each
   `preen` represents a distinct applicable skill.
4. An `settle` MUST reference a specific `preen` frame via `correlation`.
5. An `settle` MUST NOT relax constraints declared in the referenced `preen`.
   It MAY accept a subset of the offered constraints (the settler chooses which
   to bind to).
6. An `settle` produces a `negotiationId` that becomes the durable reference
   for the capability contract.
7. An `settle` is terminal for the negotiation phase. The `validUntil` field
   defines the contract's expiry. After expiry, a new negotiation MUST be
   initiated.
8. A `shun` referencing a `dabble` (via `correlation`) rejects the entire
    dabble — none of the target's skills are applicable.
9. A `shun` referencing a `preen` rejects that specific preen. The caller
    MAY settle on another outstanding preen or re-dabble.
10. A receiver MUST reject frames whose `risk` exceeds its declared `maxRisk`.
11. A receiver MUST reject `settle` frames where `validFrom` is in the past or
    `validUntil` is before `validFrom`.
12. The negotiation contract is advisory, not enforced at the protocol level.
    Either agent MAY `honk` (via the core protocol) if the other violates agreed
    constraints during execution. Formal enforcement is the responsibility of
    the execution profile (e.g., `quack-mutation-v0`).

---

## 7. Composition with Mutation Profile

When `quack-negotiate-v0` and `quack-mutation-v0` are used together, the full
lifecycle flows as follows:

```text
[Discovery — A2A Agent Card]
        |
[Negotiation — quack-negotiate-v0]
  dabble → preen → settle
        |
  negotiationId = "neg-01J..."
        |
[Mutation — quack-mutation-v0]
  splash → egg → hatch → flap → perch
    \          \        \        \→ honk
     \          \        \→ molt
      \          \→ molt
```

The `negotiationId` from the `settle` frame is carried as the `correlation`
field in all mutation frames, creating a traceable link from capability
agreement through execution evidence.

**Invariant**: A `flap` (execution request) MUST reference a valid capability
contract established through a negotiation `settle`. The `flap` frame's
`correlation` MUST carry the `negotiationId`, and the frame's `timestamp`
MUST fall within the contract's `validFrom` to `validUntil` window.

---

## 8. Rejection and Error Mapping

Quack does not define new A2A RPC methods or protocol bindings. Invalid
negotiation input is rejected using A2A 1.0 error semantics for the active
binding.

Recommended mapping:

| Condition | A2A error category |
|---|---|
| Extension marked required but not activated | `ExtensionSupportRequiredError` |
| Frame fails JSON schema validation | validation error / invalid params |
| Frame violates state-machine rules | validation error / failed precondition |
| Frame exceeds `maxRisk` | authorization or validation error |
| Frame references unknown `correlation` | invalid params / not found |
| `settle` attempts to relax `preen` constraints | validation error / invalid params |
| `settle` outside contract validity window | failed precondition |

Where the binding supports structured error details, implementations SHOULD add
a detail object with:

```json
{
  "@type": "type.googleapis.com/google.rpc.ErrorInfo",
  "reason": "QUACK_NEGOTIATE_STATE_VIOLATION",
  "domain": "example.org",
  "metadata": {
    "verb": "settle",
    "negotiationId": "neg-01JQA8D7G9",
    "failedRule": "settle_constraints_must_not_exceed_preen"
  }
}
```

---

## 9. Non-Normative Trace Rendering

Implementations may render negotiation frames as one-line text for logs, CLIs,
or audit trails. This text is not the canonical wire format.

```text
🔍 QK1 dabble  src=planner dst=executor ctx=k8s/prod/web risk=medium intent="Restart deployment web"
🪶 QK1 preen   src=executor dst=planner ctx=k8s/prod/web skill=k8s-rollout-restart applicability=full assurance=high
✅ QK1 settle  src=planner dst=executor ctx=k8s/prod/web skill=k8s-rollout-restart neg=neg-01JQA8 assurance=high
🚫 QK1 shun    src=executor dst=planner ctx=k8s/prod/web reason=skill_unavailable suggestion=k8s-scale-replicas
```

Verb emoji mapping:

| Verb | Emoji | Rationale |
|------|-------|-----------|
| `dabble` | 🔍 | Searching below the surface — a dabbling duck tips forward, beak underwater, probing for what's available. |
| `preen` | 🪶 | Displaying plumage — a duck preens to show its quality, laying out what it can offer. |
| `settle` | ✅ | Coming to rest — a duck settles on the water, the decision is made. |
| `shun` | 🚫 | Turning away — the duck will not engage with this offering. |

---

## 10. Conformance Fixtures

A conforming implementation MUST include fixtures or equivalent tests proving
that valid flows pass and invalid negotiation paths are rejected.

Required valid fixtures:

| Fixture | Expected result |
|---|---|
| `valid_dabble_preen_settle.json` | Accepted; negotiation contract established with `negotiationId`. |
| `valid_multiple_preens_single_dabble.json` | All preens received; first `settle` binds the contract. |
| `valid_preen_partial_applicability.json` | `applicability: "partial"` with `applicabilityNotes` explaining the gap. |
| `valid_shun_with_suggestions.json` | `shun` with `reasonCode` and `suggestedSkillIds` — caller can re-dabble. |

Required rejection fixtures:

| Fixture | Expected rejection |
|---|---|
| `reject_preen_without_dabble.json` | `preen_requires_prior_dabble` |
| `reject_settle_without_preen.json` | `settle_requires_preen` |
| `reject_settle_constraint_relaxation.json` | `settle_constraints_must_not_exceed_preen` |
| `reject_settle_outside_validity_window.json` | `settle_outside_contract_validity_window` |
| `reject_preen_unknown_skill_id.json` | `preen_skill_not_in_agent_card` |
| `reject_risk_above_max.json` | `risk_above_agent_max` |
| `reject_missing_destination.json` | `negotiation_requires_destination` |
| `reject_expired_frame.json` | `frame_expired` |

---

## 11. Safety Rule

> No execution without a negotiation.
> No negotiation without a task-specific dabble.
> No dabble without a concrete context.
>
> Every `preen` must answer: "Given *this* task, in *this* context, right now —
> can I deliver, and at what assurance?"
>
> Every `settle` must bind: "I understand the constraints, and I trust this
> assurance level for the stated validity window."
>
> After `validUntil`, all bets are off. Renegotiate.
