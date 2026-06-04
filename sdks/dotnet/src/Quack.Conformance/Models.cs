namespace Quack.Conformance;

using System.Text.Json;
using System.Text.Json.Serialization;

// ── Fixture file shapes ──────────────────────────────────────────────────────

// mutation/invalid and negotiate/invalid — wrapped with metadata
record InvalidFixture(
    [property: JsonPropertyName("expectedRejection")] string ExpectedRejection,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("agentMaxRisk")] string? AgentMaxRisk,
    [property: JsonPropertyName("agentCardSkills")] string[]? AgentCardSkills,
    // Optional frames sent before the test frame to establish agent state.
    // Needed for stateful tests (e.g. single-execution-violation requires a
    // prior successful perch to be in the agent's history).
    [property: JsonPropertyName("seedFrames")] JsonElement[]? SeedFrames,
    [property: JsonPropertyName("frame")] JsonElement Frame
);

// ── Wire contract ─────────────────────────────────────────────────────────────
//
// The runner posts to: POST {baseUrl}/quack/conform
//
// Request body:
//   { "sessionId": "...", "config": { "maxRisk": "...", "agentCardSkills": [...] }, "frame": { ... } }
//
// Response body (any HTTP status):
//   { "ok": true }                                    ← accepted
//   { "ok": false, "rejection": "error_code" }        ← rejected

record ConformRequest(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("config")] ConformConfig? Config,
    [property: JsonPropertyName("frame")] JsonElement Frame
);

record ConformConfig(
    [property: JsonPropertyName("maxRisk")] string? MaxRisk,
    [property: JsonPropertyName("agentCardSkills")] string[]? AgentCardSkills
);

record ConformResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("rejection")] string? Rejection
);

// ── Test case model ───────────────────────────────────────────────────────────

abstract record TestCase(string Name, string Profile);

// mutation/invalid or negotiate/invalid — wrapped fixture, specific expected rejection
record WrappedInvalidCase(string Name, string Profile, InvalidFixture Fixture) : TestCase(Name, Profile);

// core/invalid — raw frame, any rejection is a pass
record RawInvalidCase(string Name, string Profile, JsonElement Frame) : TestCase(Name, Profile);

// */valid — sequence of frames that must all be accepted
record ValidSequenceCase(string Name, string Profile, JsonElement[] Frames) : TestCase(Name, Profile);

record FixtureSuite(string Profile, string Validity, List<TestCase> Cases);

// ── Run output ────────────────────────────────────────────────────────────────

abstract record TestResult
{
    public sealed record Pass : TestResult;
    public sealed record Fail(string Reason) : TestResult;
    public sealed record Err(string Message) : TestResult;
}

sealed class RunReport
{
    int passed, failed, errored;

    public void Record(TestResult result)
    {
        switch (result)
        {
            case TestResult.Pass: passed++; break;
            case TestResult.Fail: failed++; break;
            case TestResult.Err: errored++; break;
        }
    }

    public int Passed => passed;
    public int Failed => failed;
    public int Errored => errored;
    public int Total => passed + failed + errored;
    public bool AllPassed => failed == 0 && errored == 0;
}
