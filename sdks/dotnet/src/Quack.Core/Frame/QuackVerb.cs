namespace Quack;

/// <summary>
/// The 11 Quack verbs. Each verb maps to a duck-natural metaphor
/// and carries a semantic contract defined by the protocol spec.
/// </summary>
public enum QuackVerb
{
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
}
