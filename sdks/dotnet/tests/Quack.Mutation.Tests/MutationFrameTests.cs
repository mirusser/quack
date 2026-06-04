namespace Quack.Mutation.Tests;

[TestClass]
public sealed class MutationFrameTests
{
    [TestMethod]
    public void SplashFrame_ToQuackFrame_RoundTrips()
    {
        var evidence = new[]
        {
            new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc123", Uri = "artifact://events/default" },
        };
        var splash = new SplashFrame
        {
            Source = "observer",
            Context = "k8s/default",
            EvidenceArtifacts = evidence,
        };

        var frame = splash.ToQuackFrame();
        var roundtripped = SplashFrame.FromQuackFrame(frame);

        Assert.AreEqual(splash.EvidenceArtifacts[0].Kind, roundtripped.EvidenceArtifacts[0].Kind);
        Assert.AreEqual(splash.EvidenceArtifacts[0].Digest, roundtripped.EvidenceArtifacts[0].Digest);
    }

    [TestMethod]
    public void EggFrame_ToQuackFrame_PreservesDigests()
    {
        var egg = new EggFrame
        {
            Source = "planner",
            Destination = "executor",
            Context = "k8s/default",
            Correlation = "plan-456",
            Risk = QuackRisk.Medium,
            PlanId = "plan-123",
            MutationIntent = new Dictionary<string, object> { ["action"] = "restart" },
            IntentDigest = "sha256:abc123",
            ReviewSurface = new Dictionary<string, object> { ["type"] = "k8s-deployment" },
            ReviewDigest = "sha256:def456",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev1" }],
            ApprovalPolicy = "same-subject",
            ExecutionReusePolicy = "single-execution",
            ValidFrom = "2026-06-05T12:00:00.000Z",
            ValidUntil = "2026-06-05T13:00:00.000Z",
            FreshnessPolicy = new Dictionary<string, object> { ["maxAge"] = "5m" },
        };

        var frame = egg.ToQuackFrame();
        var roundtripped = EggFrame.FromQuackFrame(frame);

        Assert.AreEqual(egg.PlanId, roundtripped.PlanId);
        Assert.AreEqual(egg.IntentDigest, roundtripped.IntentDigest);
        Assert.AreEqual(egg.ReviewDigest, roundtripped.ReviewDigest);
        Assert.AreEqual(egg.ApprovalPolicy, roundtripped.ApprovalPolicy);
        Assert.AreEqual(egg.ExecutionReusePolicy, roundtripped.ExecutionReusePolicy);
        Assert.AreEqual(egg.ValidFrom, roundtripped.ValidFrom);
        Assert.AreEqual(egg.ValidUntil, roundtripped.ValidUntil);
    }

    [TestMethod]
    public void HatchFrame_ToQuackFrame_PreservesFields()
    {
        var hatch = new HatchFrame
        {
            Source = "executor",
            Destination = "human",
            Correlation = "plan-456",
            Risk = QuackRisk.Medium,
            PlanId = "plan-123",
            ChallengeId = "challenge-789",
            ApprovalUrl = "https://approve.example.com/challenge-789",
            IntentDigest = "sha256:abc123",
            ReviewDigest = "sha256:def456",
            ChallengeExpiresAt = "2026-06-05T14:00:00.000Z",
        };

        var frame = hatch.ToQuackFrame();
        var roundtripped = HatchFrame.FromQuackFrame(frame);

        Assert.AreEqual(hatch.PlanId, roundtripped.PlanId);
        Assert.AreEqual(hatch.ChallengeId, roundtripped.ChallengeId);
        Assert.AreEqual(hatch.ApprovalUrl, roundtripped.ApprovalUrl);
        Assert.AreEqual(hatch.IntentDigest, roundtripped.IntentDigest);
        Assert.AreEqual(hatch.ChallengeExpiresAt, roundtripped.ChallengeExpiresAt);
    }

    [TestMethod]
    public void FlapFrame_ToQuackFrame_PreservesGates()
    {
        var flap = new FlapFrame
        {
            Source = "executor",
            Destination = "gateway",
            Correlation = "plan-456",
            Risk = QuackRisk.High,
            PlanId = "plan-123",
            GrantId = "grant-789",
            IntentDigest = "sha256:abc123",
            ReviewDigest = "sha256:def456",
            PreExecutionGates = ["resource-quota-check", "pdb-validation"],
        };

        var frame = flap.ToQuackFrame();
        var roundtripped = FlapFrame.FromQuackFrame(frame);

        Assert.AreEqual(flap.PlanId, roundtripped.PlanId);
        Assert.AreEqual(flap.GrantId, roundtripped.GrantId);
        Assert.AreEqual(flap.IntentDigest, roundtripped.IntentDigest);
        Assert.AreEqual(2, roundtripped.PreExecutionGates.Length);
        Assert.AreEqual("resource-quota-check", roundtripped.PreExecutionGates[0]);
    }

    [TestMethod]
    public void PerchFrame_ToQuackFrame_PreservesOutcome()
    {
        var perch = new PerchFrame
        {
            Source = "gateway",
            Destination = "executor",
            Correlation = "plan-456",
            PlanId = "plan-123",
            ExecutionId = "exec-456",
            Outcome = "succeeded",
            ObservedAt = "2026-06-05T12:00:12.000Z",
        };

        var frame = perch.ToQuackFrame();
        var roundtripped = PerchFrame.FromQuackFrame(frame);

        Assert.AreEqual(perch.PlanId, roundtripped.PlanId);
        Assert.AreEqual(perch.ExecutionId, roundtripped.ExecutionId);
        Assert.AreEqual(perch.Outcome, roundtripped.Outcome);
        Assert.AreEqual(perch.ObservedAt, roundtripped.ObservedAt);
    }

    [TestMethod]
    public void HonkFrame_ToQuackFrame_PreservesReason()
    {
        var honk = new HonkFrame
        {
            Source = "gateway",
            ReasonCode = "DIGEST_MISMATCH",
            Reason = "Intent digest does not match the approved plan",
            PlanId = "plan-123",
        };

        var frame = honk.ToQuackFrame();
        var roundtripped = HonkFrame.FromQuackFrame(frame);

        Assert.AreEqual(honk.ReasonCode, roundtripped.ReasonCode);
        Assert.AreEqual(honk.Reason, roundtripped.Reason);
        Assert.AreEqual(honk.PlanId, roundtripped.PlanId);
    }

    [TestMethod]
    public void MoltFrame_ToQuackFrame_PreservesTerminalFor()
    {
        var molt = new MoltFrame
        {
            Source = "planner",
            TerminalFor = "plan",
            ReasonCode = "SUPERSEDED",
            Reason = "New plan takes precedence",
            PlanId = "plan-123",
        };

        var frame = molt.ToQuackFrame();
        var roundtripped = MoltFrame.FromQuackFrame(frame);

        Assert.AreEqual(molt.TerminalFor, roundtripped.TerminalFor);
        Assert.AreEqual(molt.ReasonCode, roundtripped.ReasonCode);
        Assert.AreEqual(molt.Reason, roundtripped.Reason);
        Assert.AreEqual(molt.PlanId, roundtripped.PlanId);
    }

    [TestMethod]
    public void MutationFrame_InvalidVerb_Throws()
    {
        var frame = QuackFrame.Quack("test");

        try { SplashFrame.FromQuackFrame(frame); Assert.Fail(); }
        catch (InvalidOperationException) { }
    }
}
