namespace Quack.Negotiate.Tests;

[TestClass]
public sealed class NegotiateFrameTests
{
    [TestMethod]
    public void DabbleFrame_ToQuackFrame_RoundTrips()
    {
        var dabble = new DabbleFrame
        {
            Source = "planner",
            Destination = "executor",
            Context = "k8s/prod/web",
            TaskIntent = "Restart deployment 'web' in namespace 'prod'",
            RequiredCapabilities = ["k8s-exec", "prod-access"],
            Constraints = new Dictionary<string, object> { ["deadline"] = "2026-06-05T14:05:00Z" },
            PreferredSkills = ["k8s-rollout-restart"],
        };

        var frame = dabble.ToQuackFrame();
        var roundtripped = DabbleFrame.FromQuackFrame(frame);

        Assert.AreEqual(dabble.TaskIntent, roundtripped.TaskIntent);
        Assert.AreEqual("k8s-exec", roundtripped.RequiredCapabilities[0]);
        Assert.AreEqual("k8s-rollout-restart", roundtripped.PreferredSkills[0]);
    }

    [TestMethod]
    public void PreenFrame_ToQuackFrame_RoundTrips()
    {
        var preen = new PreenFrame
        {
            Source = "executor",
            Destination = "planner",
            Context = "k8s/prod/web",
            Correlation = "dabble-id-1",
            SkillId = "k8s-rollout-restart",
            Applicability = "full",
            Constraints = new Dictionary<string, object> { ["freshnessWindow"] = "5m" },
            AssuranceLevel = "high",
            ApplicabilityNotes = "Rollout restart is standard remediation",
            PreExecutionGates = ["resource-quota-check", "pdb-validation"],
            EstimatedDuration = "PT45S",
        };

        var frame = preen.ToQuackFrame();
        var roundtripped = PreenFrame.FromQuackFrame(frame);

        Assert.AreEqual(preen.SkillId, roundtripped.SkillId);
        Assert.AreEqual(preen.Applicability, roundtripped.Applicability);
        Assert.AreEqual(preen.AssuranceLevel, roundtripped.AssuranceLevel);
        Assert.AreEqual(2, roundtripped.PreExecutionGates.Length);
    }

    [TestMethod]
    public void SettleFrame_ToQuackFrame_RoundTrips()
    {
        var settle = new SettleFrame
        {
            Source = "planner",
            Destination = "executor",
            Context = "k8s/prod/web",
            Correlation = "preen-id-1",
            NegotiationId = "neg-01JQA8",
            SkillId = "k8s-rollout-restart",
            AgreedConstraints = new Dictionary<string, object> { ["freshnessWindow"] = "5m" },
            AgreedAssuranceLevel = "high",
            ValidFrom = "2026-06-05T14:00:05.000Z",
            ValidUntil = "2026-06-05T14:05:05.000Z",
        };

        var frame = settle.ToQuackFrame();
        var roundtripped = SettleFrame.FromQuackFrame(frame);

        Assert.AreEqual(settle.NegotiationId, roundtripped.NegotiationId);
        Assert.AreEqual(settle.SkillId, roundtripped.SkillId);
        Assert.AreEqual(settle.AgreedAssuranceLevel, roundtripped.AgreedAssuranceLevel);
    }

    [TestMethod]
    public void ShunFrame_ToQuackFrame_RoundTrips()
    {
        var shun = new ShunFrame
        {
            Source = "executor",
            Destination = "planner",
            Context = "k8s/prod/web",
            Correlation = "dabble-id-1",
            ReasonCode = "skill_unavailable",
            Reason = "Rollout restart is gated by maintenance window",
            SuggestedSkillIds = ["k8s-scale-replicas"],
        };

        var frame = shun.ToQuackFrame();
        var roundtripped = ShunFrame.FromQuackFrame(frame);

        Assert.AreEqual(shun.ReasonCode, roundtripped.ReasonCode);
        Assert.AreEqual(shun.Reason, roundtripped.Reason);
        Assert.AreEqual("k8s-scale-replicas", roundtripped.SuggestedSkillIds[0]);
    }

    [TestMethod]
    public void SettleFrame_ConstraintRelaxationCheck()
    {
        // The SettleFrame should NOT allow relaxing preen constraints.
        // This is enforced by the state machine, not the frame itself.
        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "preen-id", NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new Dictionary<string, object> { ["max"] = "5" },
            AgreedAssuranceLevel = "medium",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        // Frame round-trips correctly even with relaxed constraints
        var frame = settle.ToQuackFrame();
        var rt = SettleFrame.FromQuackFrame(frame);
        Assert.AreEqual("5", rt.AgreedConstraints["max"]);
    }
}
