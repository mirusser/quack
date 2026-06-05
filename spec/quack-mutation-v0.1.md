# Quack Mutation v0.1

`quack-mutation-v0` is a minimal A2A 1.0 profile extension for
digest-bound mutation coordination between agents.

Quack does not replace A2A. A2A owns discovery, transport, task lifecycle,
streaming, authentication, authorization, message/artifact containers, and
protocol binding semantics. Quack adds one narrow thing: a structured mutation
frame that says what evidence exists, what mutation was proposed, what approval
was requested, what grant was consumed, and what happened at execution time.

This version targets A2A Protocol `1.0`. A2A patch releases, such as `1.0.1`,
do not change the protocol compatibility value used in `AgentInterface`
declarations or `A2A-Version` requests.

Normative extension URI:

```text
https://example.org/extensions/quack-mutation/v0
```

Normative frame media type:

```text
application/vnd.quack+json
```

Quack-Text, if implemented, is only a trace/debug rendering. The canonical
wire format for this profile is JSON carried in A2A unified `Part.data`.

This profile is an A2A JSON projection of the core Quack frame model. It uses
the canonical Quack field names directly in camelCase JSON — `version`, `verb`,
`id`, `source`, `destination`, etc.

## 1. A2A Binding

### 1.1 Agent Card Declaration

Agents declare support through `AgentCard.capabilities.extensions[]` using the
A2A 1.0 `AgentExtension` object.

```json
{
  "name": "Executor Agent",
  "description": "Executes approved mutation plans.",
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
        "uri": "https://example.org/extensions/quack-mutation/v0",
        "description": "Supports digest-bound mutation frames in A2A messages and artifacts.",
        "required": true,
        "params": {
          "maxRisk": "high",
          "maxQuackVersion": "0.1",
          "supportedVerbs": ["splash", "egg", "hatch", "flap", "perch", "honk", "molt"],
          "mutationProfiles": ["quack-mutation-v0"],
          "supportedMediaTypes": ["application/vnd.quack+json"]
        }
      }
    ]
  },
  "defaultInputModes": ["application/vnd.quack+json", "text/plain"],
  "defaultOutputModes": ["application/vnd.quack+json", "text/plain"],
  "skills": [
    {
      "id": "execute-approved-plan",
      "name": "Execute approved mutation plan",
      "description": "Consumes a Quack flap frame and executes only after approval and pre-execution gates pass.",
      "tags": ["mutation", "approval", "execution"]
    }
  ]
}
```

Extension parameters:

| Parameter | Required | Meaning |
|---|---:|---|
| `maxRisk` | Yes | Highest risk value this agent will accept: `none`, `low`, `medium`, `high`, or `critical`. |
| `maxQuackVersion` | Yes | Highest Quack profile version accepted by this agent. |
| `supportedVerbs` | Yes | Verbs this agent accepts. |
| `mutationProfiles` | Yes | Mutation profiles implemented by this agent. This spec defines `quack-mutation-v0`. |
| `supportedMediaTypes` | Yes | Must include `application/vnd.quack+json` for conforming agents. |

If the Agent Card marks the extension `required: true`, clients MUST activate
the extension for mutation requests. If they do not, the server MUST reject the
request using A2A's extension-support error semantics.

### 1.2 Extension Activation

Clients activate the extension using A2A 1.0 service parameters. For HTTP
bindings, this means the `A2A-Extensions` header. Clients also send
`A2A-Version: 1.0`.

```http
POST /message:send HTTP/1.1
Host: executor.example.com
Content-Type: application/a2a+json
A2A-Version: 1.0
A2A-Extensions: https://example.org/extensions/quack-mutation/v0
Authorization: Bearer token
```

The response SHOULD echo the activated extension URI through the same
binding-specific mechanism.

### 1.3 Message and Artifact Parts

A Quack frame is carried in an A2A 1.0 unified `Part` with:

- exactly one content field: `data`
- `mediaType: "application/vnd.quack+json"`
- the containing `Message.extensions[]` or `Artifact.extensions[]` including
  the Quack extension URI

Message example:

```json
{
  "message": {
    "messageId": "msg-001",
    "role": "ROLE_USER",
    "context": "anomaly-default-web",
    "taskId": "task-123",
    "extensions": ["https://example.org/extensions/quack-mutation/v0"],
    "parts": [
      {
        "mediaType": "application/vnd.quack+json",
        "data": {
          "version": "0.1",
          "profile": "quack-mutation-v0",
          "verb": "flap",
          "id": "frame-006",
          "timestamp": "2026-06-04T12:00:00.000Z",
          "source": "executor",
          "destination": "gateway",
          "context": "anomaly-default-web",
          "taskId": "task-123",
          "risk": "high",
          "summary": "Execute approved restart plan.",
          "data": {
            "planId": "plan-123",
            "grantId": "grant-789",
            "intentDigest": "sha256:8d1f...",
            "reviewDigest": "sha256:61ad..."
          }
        }
      }
    ]
  }
}
```

Artifact example:

```json
{
  "artifactId": "artifact-plan-123",
  "name": "Approved execution outcome",
  "extensions": ["https://example.org/extensions/quack-mutation/v0"],
  "parts": [
    {
      "mediaType": "application/vnd.quack+json",
      "data": {
        "version": "0.1",
        "profile": "quack-mutation-v0",
        "verb": "perch",
        "id": "frame-007",
        "timestamp": "2026-06-04T12:00:12.000Z",
        "source": "gateway",
        "destination": "executor",
        "context": "anomaly-default-web",
        "taskId": "task-123",
        "risk": "high",
        "summary": "Deployment restarted.",
        "data": {
          "planId": "plan-123",
          "executionId": "exec-456",
          "outcome": "succeeded"
        }
      }
    }
  ]
}
```

Messages are for coordination and task interaction. Durable mutation evidence,
plans, approval records, and execution outcomes SHOULD also be emitted as A2A
Artifacts so clients can retrieve them through task history and artifact APIs.

## 2. Frame Model

All Quack frames use camelCase JSON fields.

| Field | Required | Type | Meaning |
|---|---:|---|---|
| `version` | Yes | string | Profile version. This document defines `0.1`. |
| `profile` | Yes | string | Must be `quack-mutation-v0`. |
| `verb` | Yes | string | One of `splash`, `egg`, `hatch`, `flap`, `perch`, `honk`, `molt`. |
| `id` | Yes | string | Opaque frame identifier. UUIDs are recommended. |
| `timestamp` | Yes | string | ISO 8601 UTC timestamp with millisecond precision when available. |
| `source` | Yes | string | Quack-speaking sender. |
| `destination` | No | string | Intended receiver. Omit only for broadcast-style `splash`, `honk`, or `molt`. |
| `context` | Yes | string | A2A context scope for the mutation conversation. |
| `taskId` | No | string | A2A task scope when the frame belongs to a specific task. |
| `correlation` | No | string | Optional application-level correlation. Not a proof. |
| `risk` | Yes | string | `none`, `low`, `medium`, `high`, or `critical`. |
| `expiresAt` | No | string | Optional ISO 8601 UTC freshness bound for this frame. |
| `summary` | No | string | Human-readable one-line summary. |
| `data` | Yes | object | Verb-specific payload. |

Container consistency rules:

- If the containing A2A Message has `contextId`, the frame `context` MUST
  match it.
- If the containing A2A Message has `taskId`, the frame `taskId` MUST match it.
- If the frame is carried by an Artifact, the frame `context` and `taskId`
  MUST match the surrounding Task or TaskArtifactUpdateEvent that delivers that
  Artifact.
- If a frame carries both `context` and `taskId`, the receiver MUST enforce
  A2A's rule that the task belongs to that context.
- Frame IDs, plan IDs, challenge IDs, grant IDs, and execution IDs are opaque
  handles. They are never accepted as integrity proof.

## 3. Verbs

### 3.1 `splash`

`splash` attaches evidence artifacts before a mutation plan is proposed.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `evidenceArtifacts` | array | One or more digest-bound evidence references. |

Evidence artifact shape:

```json
{
  "kind": "k8s.events",
  "digest": "sha256:7e3a...",
  "uri": "artifact://events/default/web",
  "mediaType": "application/json"
}
```

### 3.2 `egg`

`egg` proposes one concrete mutation plan. It does not authorize execution.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `planId` | string | Opaque workflow handle. |
| `mutationIntent` | object | Domain-specific executable intent. |
| `intentDigest` | string | Digest proving executable intent is unchanged. |
| `reviewSurface` | object | Domain-specific immutable review snapshot or digest-bound reference. |
| `reviewDigest` | string | Digest proving approved review snapshot is unchanged. |
| `evidenceArtifacts` | array | Digest-bound references used to produce the plan. |
| `approvalPolicy` | string | Policy label, for example `same-subject`. |
| `executionReusePolicy` | string | `single-execution` in this profile. |
| `validFrom` | string | ISO 8601 UTC start of plan validity. |
| `validUntil` | string | ISO 8601 UTC end of plan validity. |
| `freshnessPolicy` | object | Adapter-owned checks required before execution. |

### 3.3 `hatch`

`hatch` requests out-of-band approval for a specific `egg`.

Human interaction is outside Quack. A browser UI, approval authority, or other
trusted approval service records the human decision and issues an approval
grant if approved. Quack sees the challenge and, later, a grant reference in
`flap`; it does not treat a chat acknowledgement as authorization.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `planId` | string | Opaque plan handle from the `egg`. |
| `challengeId` | string | Opaque approval challenge handle. |
| `approvalUrl` | string | URL or URI for the out-of-band review surface. |
| `intentDigest` | string | Digest copied from the `egg`. |
| `reviewDigest` | string | Digest copied from the `egg`. |
| `challengeExpiresAt` | string | ISO 8601 UTC challenge expiry. |

### 3.4 `flap`

`flap` requests execution of an approved plan. It is the mutation boundary.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `planId` | string | Opaque plan handle from the `egg`. |
| `grantId` | string | Durable approval grant handle. |
| `intentDigest` | string | Digest that must match the approved plan. |
| `reviewDigest` | string | Digest that must match the approved review snapshot. |
| `preExecutionGates` | array | Gates the receiver must evaluate immediately before mutation. |

The receiver MUST NOT mutate until the approval grant, plan validity window,
authorization, intent digest, review digest, reuse policy, freshness policy,
and domain policy checks all pass immediately before execution.

### 3.5 `perch`

`perch` records a terminal execution outcome.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `planId` | string | Opaque plan handle. |
| `executionId` | string | Opaque execution attempt handle. |
| `outcome` | string | `succeeded`, `failed`, or `blocked`. |
| `observedAt` | string | ISO 8601 UTC timestamp for the outcome. |

`blocked` is terminal for the current plan under `single-execution` semantics.
A retry requires a new plan unless a future profile explicitly defines
retryable execution statuses.

### 3.6 `honk`

`honk` records a warning, policy violation, digest mismatch, freshness failure,
or other safety event. It does not authorize or execute anything.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `reasonCode` | string | Stable machine-readable reason. |
| `reason` | string | Human-readable explanation. |

Optional fields include `planId`, `challengeId`, `grantId`, `executionId`,
`failedGate`, and `evidenceArtifacts`.

### 3.7 `molt`

`molt` records cancellation or supersession.

Required `data` fields:

| Field | Type | Meaning |
|---|---|---|
| `terminalFor` | string | `plan`, `challenge`, `task`, or `context`. |
| `reasonCode` | string | Stable machine-readable reason. |
| `reason` | string | Human-readable explanation. |

At least one relevant handle SHOULD be included, such as `planId`,
`challengeId`, `taskId`, `supersededByPlanId`, or `supersededByFrameId`.

## 4. JSON Schema

This is the normative schema shape. Implementations MAY split it into separate
schema files, but conformance tests MUST validate the same constraints.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://example.org/schemas/quack-mutation-v0.schema.json",
  "title": "Quack Mutation v0 Frame",
  "type": "object",
  "required": [
    "version",
    "profile",
    "verb",
    "id",
    "timestamp",
    "source",
    "context",
    "risk",
    "data"
  ],
  "properties": {
    "version": { "const": "0.1" },
    "profile": { "const": "quack-mutation-v0" },
    "verb": {
      "enum": ["splash", "egg", "hatch", "flap", "perch", "honk", "molt"]
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
    "summary": { "type": "string" },
    "data": { "type": "object" }
  },
  "allOf": [
    {
      "if": { "properties": { "verb": { "const": "splash" } } },
      "then": { "$ref": "#/$defs/splash" }
    },
    {
      "if": { "properties": { "verb": { "const": "egg" } } },
      "then": { "$ref": "#/$defs/egg" }
    },
    {
      "if": { "properties": { "verb": { "const": "hatch" } } },
      "then": { "$ref": "#/$defs/hatch" }
    },
    {
      "if": { "properties": { "verb": { "const": "flap" } } },
      "then": { "$ref": "#/$defs/flap" }
    },
    {
      "if": { "properties": { "verb": { "const": "perch" } } },
      "then": { "$ref": "#/$defs/perch" }
    },
    {
      "if": { "properties": { "verb": { "const": "honk" } } },
      "then": { "$ref": "#/$defs/honk" }
    },
    {
      "if": { "properties": { "verb": { "const": "molt" } } },
      "then": { "$ref": "#/$defs/molt" }
    }
  ],
  "$defs": {
    "digest": {
      "type": "string",
      "pattern": "^sha256:[0-9a-f]{64}$"
    },
    "evidenceArtifact": {
      "type": "object",
      "required": ["kind", "digest", "uri"],
      "properties": {
        "kind": { "type": "string", "minLength": 1 },
        "digest": { "$ref": "#/$defs/digest" },
        "uri": { "type": "string", "minLength": 1 },
        "mediaType": { "type": "string" }
      },
      "additionalProperties": true
    },
    "splash": {
      "properties": {
        "data": {
          "type": "object",
          "required": ["evidenceArtifacts"],
          "properties": {
            "evidenceArtifacts": {
              "type": "array",
              "minItems": 1,
              "items": { "$ref": "#/$defs/evidenceArtifact" }
            }
          },
          "additionalProperties": true
        }
      }
    },
    "egg": {
      "properties": {
        "data": {
          "type": "object",
          "required": [
            "planId",
            "mutationIntent",
            "intentDigest",
            "reviewSurface",
            "reviewDigest",
            "evidenceArtifacts",
            "approvalPolicy",
            "executionReusePolicy",
            "validFrom",
            "validUntil",
            "freshnessPolicy"
          ],
          "properties": {
            "planId": { "type": "string", "minLength": 1 },
            "mutationIntent": { "type": "object" },
            "intentDigest": { "$ref": "#/$defs/digest" },
            "reviewSurface": { "type": "object" },
            "reviewDigest": { "$ref": "#/$defs/digest" },
            "evidenceArtifacts": {
              "type": "array",
              "minItems": 1,
              "items": { "$ref": "#/$defs/evidenceArtifact" }
            },
            "approvalPolicy": { "type": "string", "minLength": 1 },
            "executionReusePolicy": { "const": "single-execution" },
            "validFrom": { "type": "string", "format": "date-time" },
            "validUntil": { "type": "string", "format": "date-time" },
            "freshnessPolicy": { "type": "object" }
          },
          "additionalProperties": true
        }
      }
    },
    "hatch": {
      "properties": {
        "data": {
          "type": "object",
          "required": [
            "planId",
            "challengeId",
            "approvalUrl",
            "intentDigest",
            "reviewDigest",
            "challengeExpiresAt"
          ],
          "properties": {
            "planId": { "type": "string", "minLength": 1 },
            "challengeId": { "type": "string", "minLength": 1 },
            "approvalUrl": { "type": "string", "minLength": 1 },
            "intentDigest": { "$ref": "#/$defs/digest" },
            "reviewDigest": { "$ref": "#/$defs/digest" },
            "challengeExpiresAt": { "type": "string", "format": "date-time" }
          },
          "additionalProperties": true
        }
      }
    },
    "flap": {
      "properties": {
        "data": {
          "type": "object",
          "required": [
            "planId",
            "grantId",
            "intentDigest",
            "reviewDigest",
            "preExecutionGates"
          ],
          "properties": {
            "planId": { "type": "string", "minLength": 1 },
            "grantId": { "type": "string", "minLength": 1 },
            "intentDigest": { "$ref": "#/$defs/digest" },
            "reviewDigest": { "$ref": "#/$defs/digest" },
            "preExecutionGates": {
              "type": "array",
              "minItems": 1,
              "items": { "type": "string", "minLength": 1 }
            }
          },
          "additionalProperties": true
        }
      }
    },
    "perch": {
      "properties": {
        "data": {
          "type": "object",
          "required": ["planId", "executionId", "outcome", "observedAt"],
          "properties": {
            "planId": { "type": "string", "minLength": 1 },
            "executionId": { "type": "string", "minLength": 1 },
            "outcome": { "enum": ["succeeded", "failed", "blocked"] },
            "observedAt": { "type": "string", "format": "date-time" }
          },
          "additionalProperties": true
        }
      }
    },
    "honk": {
      "properties": {
        "data": {
          "type": "object",
          "required": ["reasonCode", "reason"],
          "properties": {
            "reasonCode": { "type": "string", "minLength": 1 },
            "reason": { "type": "string", "minLength": 1 }
          },
          "additionalProperties": true
        }
      }
    },
    "molt": {
      "properties": {
        "data": {
          "type": "object",
          "required": ["terminalFor", "reasonCode", "reason"],
          "properties": {
            "terminalFor": {
              "enum": ["plan", "challenge", "task", "context"]
            },
            "reasonCode": { "type": "string", "minLength": 1 },
            "reason": { "type": "string", "minLength": 1 }
          },
          "additionalProperties": true
        }
      }
    }
  },
  "additionalProperties": false
}
```

## 5. Digest and Canonicalization Rules

Quack uses separate digests for separate proofs.

| Digest | Required On | Meaning |
|---|---|---|
| `intentDigest` | `egg`, `hatch`, `flap` | Proves executable mutation intent is unchanged. |
| `reviewDigest` | `egg`, `hatch`, `flap` | Proves the approved review snapshot is unchanged. |
| `evidenceArtifacts[].digest` | `splash`, `egg` | Proves referenced evidence artifacts are unchanged. |

Rules:

1. All digest inputs MUST be serialized with RFC 8785 JSON Canonicalization
   Scheme (JCS) before hashing.
2. Digests MUST use SHA-256 and be encoded as `sha256:` followed by 64
   lowercase hexadecimal characters.
3. `intentDigest` is computed over the domain adapter's executable
   `mutationIntent` canonical JSON.
4. `reviewDigest` is computed over the immutable review snapshot shown to the
   approver, including profile/version, requester, approval policy, validity
   window, freshness policy, `intentDigest`, evidence artifact digests or
   digest-bound references, redaction metadata, and review-surface context.
5. `planId` MUST NOT be treated as an integrity mechanism. It is only an opaque
   workflow handle.
6. A receiver MUST reject `hatch` or `flap` frames whose digests do not match
   the stored `egg` and approval records for the same `planId`.

## 6. State Machine

The validator state machine is scoped to an A2A `context` and, when present,
an A2A `taskId`.

```text
splash* -> egg -> hatch -> flap -> perch
              \       \      \-> honk
               \       \-> molt
                \-> molt
```

`honk` may occur at any point. `molt` is terminal for the handle named in its
payload.

Rules:

1. A `splash` records evidence for a `context` and optional `taskId`.
2. An `egg` MUST reference at least one evidence artifact previously introduced
   by a `splash` in the same state-machine scope.
3. An `egg` creates or records exactly one `planId`.
4. A `hatch` MUST reference an existing `egg` by `planId` and matching
   `intentDigest` and `reviewDigest`.
5. A `hatch` creates or records exactly one approval challenge.
6. A successful out-of-band approval is represented conversationally as a
`bob` for the related `hatch`, but execution consumes an Approval Grant. The
grant is the durable, digest-bound authorization artifact derived from the
challenge, the positive decision, approver identity, approval policy, validity
window, and any required domain approval data.
7. A `flap` MUST reference an existing `egg`, an approval challenge that
   resulted in an Approval Grant, and matching `intentDigest` and
   `reviewDigest`.
8. A `flap` MUST pass pre-execution gates immediately before mutation:
   approval grant validation, plan validity, caller authorization, intent
   digest validation, review digest validation, execution reuse policy,
   freshness policy, and domain policy checks.
9. `executionReusePolicy: "single-execution"` means one approved plan can have
   at most one successful execution.
10. A `perch` records the terminal execution result. `blocked` and `failed` are
    terminal for this profile unless a future profile defines retry semantics.
11. A receiver MUST reject frames that exceed its declared `maxRisk`.
12. A receiver MUST reject frames whose `expiresAt` is in the past.

## 7. Rejection and Error Mapping

Quack does not define new A2A RPC methods or protocol bindings. Invalid Quack
input is rejected using A2A 1.0 error semantics for the active binding.

Recommended mapping:

| Condition | A2A error category |
|---|---|
| Extension marked required but not activated | `ExtensionSupportRequiredError` |
| Part is not `mediaType: application/vnd.quack+json` | `ContentTypeNotSupportedError` |
| Frame fails JSON schema validation | validation error / invalid params |
| Frame violates state-machine rules | validation error / failed precondition |
| Frame exceeds `maxRisk` | authorization or validation error, depending on local policy |
| Digest, grant, freshness, or policy gate fails before mutation | failed precondition plus a `honk` artifact or status message |

Where the binding supports structured error details, implementations SHOULD add
a detail object with:

```json
{
  "@type": "type.googleapis.com/google.rpc.ErrorInfo",
  "reason": "QUACK_STATE_VIOLATION",
  "domain": "example.org",
  "metadata": {
    "verb": "flap",
    "planId": "plan-123",
    "failedRule": "flap_requires_approval_grant"
  }
}
```

## 8. Conformance Fixtures

A conforming implementation MUST include fixtures or equivalent tests proving
that valid flows pass and invalid mutation paths are rejected.

Required valid fixtures:

| Fixture | Expected result |
|---|---|
| `valid_splash_egg_hatch_flap_perch.json` | Accepted; one successful terminal execution. |
| `valid_honk_before_flap.json` | Accepted; warning does not authorize or terminate execution by itself. |
| `valid_molt_challenge.json` | Accepted; challenge is terminal and no grant is usable. |

Required rejection fixtures:

| Fixture | Expected rejection |
|---|---|
| `reject_egg_without_splash.json` | `egg_requires_prior_splash` |
| `reject_hatch_without_egg.json` | `hatch_requires_egg` |
| `reject_flap_without_grant.json` | `flap_requires_approval_grant` |
| `reject_flap_digest_mismatch.json` | `intent_or_review_digest_mismatch` |
| `reject_flap_expired_plan.json` | `plan_validity_window_expired` |
| `reject_flap_reused_grant.json` | `single_execution_violation` |
| `reject_frame_context_task_mismatch.json` | `a2a_context_task_mismatch` |
| `reject_unsupported_media_type.json` | `unsupported_quack_media_type` |
| `reject_risk_above_max.json` | `risk_above_agent_max` |
| `reject_unknown_required_extension.json` | `extension_support_required` |

Each rejection fixture SHOULD also produce a `honk` status message or artifact
when the frame was parseable and the rejection happened after Quack validation
started.

## 9. Non-Normative Trace Rendering

Implementations may render frames as one-line text for logs, CLIs, headers, or
audit trails. This text is not the canonical wire format.

```text
QK1 splash src=observer ctx=anomaly-default-web risk=medium evidence=sha256:7e3a...
QK1 egg src=planner dst=executor ctx=anomaly-default-web plan=plan-123 intent=sha256:8d1f... review=sha256:61ad...
QK1 hatch src=executor dst=approval ctx=anomaly-default-web plan=plan-123 challenge=challenge-456
QK1 flap src=executor dst=gateway ctx=anomaly-default-web plan=plan-123 grant=grant-789
QK1 perch src=gateway dst=executor ctx=anomaly-default-web plan=plan-123 outcome=succeeded
```

If Quack-Text is present, it MUST round-trip through the canonical frame model
without changing the JSON digest inputs. Text parsing failures MUST NOT be used
to authorize or execute mutations.

## 10. Safety Rule

No mutation without a `flap`.
No `flap` without an Approval Grant.
No Approval Grant without an approved `hatch`.
No `hatch` without an `egg`.
No `egg` without `splash`.
