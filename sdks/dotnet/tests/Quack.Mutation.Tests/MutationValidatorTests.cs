namespace Quack.Mutation.Tests;

[TestClass]
public sealed class MutationValidatorTests
{
    private readonly MutationValidator _validator = new();
    private readonly InMemoryQuackHistory _history = new();

    [TestMethod]
    public void Validate_ValidFullFlow_Passes()
    {
        var splash = new SplashFrame
        {
            Source = "observer", Context = "ctx-1",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc" }],
        };
        _history.Add(splash.ToQuackFrame());

        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "corr-1",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:8d1f",
            ReviewSurface = new(), ReviewDigest = "sha256:61ad",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc" }],
            ApprovalPolicy = "same-subject", ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "corr-1",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };
        _history.Add(hatch.ToQuackFrame());

        var bob = QuackFrame.Bob("human", "executor", "corr-1");
        _history.Add(bob);

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "corr-1",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            PreExecutionGates = ["check-1"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsTrue(result.IsValid, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Validate_FlapWithoutEgg_Fails()
    {
        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "corr-1",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            PreExecutionGates = ["check-1"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "FLAP_REQUIRES_EGG"));
    }

    [TestMethod]
    public void Validate_FlapDigestMismatch_Fails()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "corr-1",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:8d1f",
            ReviewSurface = new(), ReviewDigest = "sha256:61ad",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc" }],
            ApprovalPolicy = "same-subject", ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "corr-1",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };
        _history.Add(hatch.ToQuackFrame());

        var bob = QuackFrame.Bob("human", "executor", "corr-1");
        _history.Add(bob);

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "corr-1",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:WRONG", // mismatched
            ReviewDigest = "sha256:61ad",
            PreExecutionGates = ["check-1"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "DIGEST_MISMATCH"));
    }

    [TestMethod]
    public void Validate_FlapExpiredPlan_Fails()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "corr-1",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:8d1f",
            ReviewSurface = new(), ReviewDigest = "sha256:61ad",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc" }],
            ApprovalPolicy = "same-subject", ValidFrom = "2020-01-01T00:00:00Z", ValidUntil = "2020-01-02T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "corr-1",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            PreExecutionGates = ["check-1"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "PLAN_EXPIRED"));
    }

    [TestMethod]
    public void Validate_FlapWithoutGrant_Fails()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "corr-1",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:8d1f",
            ReviewSurface = new(), ReviewDigest = "sha256:61ad",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "k8s.events", Digest = "sha256:abc" }],
            ApprovalPolicy = "same-subject", ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "corr-1",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };
        _history.Add(hatch.ToQuackFrame());
        // no bob (approval grant)

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "corr-1",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:8d1f", ReviewDigest = "sha256:61ad",
            PreExecutionGates = ["check-1"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "FLAP_REQUIRES_GRANT"));
    }

    [TestMethod]
    public void Validate_Honk_AlwaysValid()
    {
        var honk = new HonkFrame
        {
            Source = "gateway", ReasonCode = "DIGEST_MISMATCH", Reason = "bad digest",
        };

        var result = _validator.Validate(honk, _history);
        Assert.IsTrue(result.IsValid);
    }
}
