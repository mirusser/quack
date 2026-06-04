namespace Quack;

/// <summary>
/// Quack verbs — 11 core verbs plus 4 negotiate-profile verbs.
/// Each verb maps to a duck-natural metaphor and carries a semantic contract.
/// </summary>
public enum QuackVerb
{
    // ── Core protocol verbs (§2) ──

    /// <summary>🦆 Announce — "Something happened."</summary>
    Quack,
    /// <summary>🐤 Request — "Please do this."</summary>
    Peck,
    /// <summary>🦢 Acknowledge / Approved — "Received and understood."</summary>
    Bob,
    /// <summary>🐦‍⬛ Reject — "Cannot do this."</summary>
    Nack,
    /// <summary>🥚 Produced artifact / Plan — "This is the plan."</summary>
    Egg,
    /// <summary>🐣 Approval requested — "Approve this plan."</summary>
    Hatch,
    /// <summary>🪽 Execution started — "Execute the approved plan."</summary>
    Flap,
    /// <summary>🕊️ Completed — "Finished / landed."</summary>
    Perch,
    /// <summary>🪿 Warning — "Something is wrong."</summary>
    Honk,
    /// <summary>🪹 Canceled / Superseded — "Plan no longer valid."</summary>
    Molt,
    /// <summary>💦 Attach evidence — "Here is the evidence."</summary>
    Splash,

    // ── Negotiate-profile verbs (quack-negotiate-v0) ──

    /// <summary>🔍 Query capabilities — "Given this task, what can you do?"</summary>
    Dabble,
    /// <summary>🪶 Offer capability — "For this task I can provide skill X."</summary>
    Preen,
    /// <summary>✅ Accept negotiation — "I accept your offer and bind the contract."</summary>
    Settle,
    /// <summary>🚫 Reject negotiation — "I cannot or will not engage."</summary>
    Shun,
}
