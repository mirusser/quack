namespace Quack.Mutation.Tests;

[TestClass]
public sealed class MutationConformanceTests
{
    private readonly MutationValidator _validator = new();
    private readonly InMemoryQuackHistory _history = new();

    [TestMethod]
    public void Conformance_RejectEggWithoutSplash()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-no-splash", Correlation = "corr-1",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };

        var result = _validator.Validate(egg, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "EGG_REQUIRES_SPLASH"));
    }

    [TestMethod]
    public void Conformance_RejectHatchWithoutEgg()
    {
        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "no-egg-corr",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(hatch, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "HATCH_REQUIRES_EGG"));
    }

    [TestMethod]
    public void Conformance_RejectFlapWithoutGrant()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "no-grant-corr",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "no-grant-corr",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };
        _history.Add(hatch.ToQuackFrame());

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "no-grant-corr",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            PreExecutionGates = ["check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "FLAP_REQUIRES_GRANT"));
    }

    [TestMethod]
    public void Conformance_RejectFlapDigestMismatch()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "mismatch-corr",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:ORIGINAL",
            ReviewSurface = new(), ReviewDigest = "sha256:REVIEW",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var hatch = new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "mismatch-corr",
            PlanId = "plan-1", ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:ORIGINAL", ReviewDigest = "sha256:REVIEW",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        };
        _history.Add(hatch.ToQuackFrame());

        _history.Add(QuackFrame.Bob("human", "executor", "mismatch-corr"));

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "mismatch-corr",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:TAMPERED", ReviewDigest = "sha256:REVIEW",
            PreExecutionGates = ["check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "DIGEST_MISMATCH"));
    }

    [TestMethod]
    public void Conformance_RejectFlapExpiredPlan()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "exp-corr",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2020-01-01T00:00:00Z", ValidUntil = "2020-01-02T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "exp-corr",
            PlanId = "plan-1", GrantId = "grant-1",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            PreExecutionGates = ["check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "PLAN_EXPIRED"));
    }

    [TestMethod]
    public void Conformance_ValidFullFlow()
    {
        _history.Add(new SplashFrame
        {
            Source = "observer", Context = "full-ctx",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
        }.ToQuackFrame());

        _history.Add(new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "full-ctx", Correlation = "full-corr",
            PlanId = "plan-full", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        }.ToQuackFrame());

        _history.Add(new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = "full-corr",
            PlanId = "plan-full", ChallengeId = "ch-full", ApprovalUrl = "https://appr",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        }.ToQuackFrame());

        _history.Add(QuackFrame.Bob("human", "executor", "full-corr"));

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = "full-corr",
            PlanId = "plan-full", GrantId = "grant-full",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            PreExecutionGates = ["check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsTrue(result.IsValid, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Conformance_RejectFlapReusedGrant_SingleExecutionViolation()
    {
        const string corr = "reuse-corr";
        const string planId = "plan-reuse";
        const string intentDigest = "sha256:abc";
        const string reviewDigest = "sha256:def";

        _history.Add(new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = corr,
            PlanId = planId, MutationIntent = new(), IntentDigest = intentDigest,
            ReviewSurface = new(), ReviewDigest = reviewDigest,
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        }.ToQuackFrame());

        _history.Add(new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = corr,
            PlanId = planId, ChallengeId = "ch-1", ApprovalUrl = "https://appr",
            IntentDigest = intentDigest, ReviewDigest = reviewDigest,
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        }.ToQuackFrame());

        _history.Add(QuackFrame.Bob("human", "executor", corr));

        // Record a successful execution
        _history.Add(new PerchFrame
        {
            Source = "gateway", Destination = "executor", Correlation = corr,
            PlanId = planId, ExecutionId = "exec-1", Outcome = "succeeded",
            ObservedAt = "2026-06-05T12:00:00Z",
        }.ToQuackFrame());

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = corr,
            PlanId = planId, GrantId = "grant-1",
            IntentDigest = intentDigest, ReviewDigest = reviewDigest,
            PreExecutionGates = ["check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SINGLE_EXECUTION_VIOLATION"));
    }

    [TestMethod]
    public void Conformance_RejectFlapRiskAboveMax()
    {
        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = "risk-corr",
            PlanId = "plan-1", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject", Risk = QuackRisk.Critical,
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };

        var result = _validator.Validate(egg, _history, maxRisk: QuackRisk.High);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "RISK_ABOVE_MAX"));
    }

    [TestMethod]
    public void Conformance_RejectExpiredFrame()
    {
        var splash = new SplashFrame
        {
            Source = "observer", Context = "ctx-1",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ExpiresAt = "2020-01-01T00:00:00Z",
        };

        var result = _validator.Validate(splash, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "FRAME_EXPIRED"));
    }

    [TestMethod]
    public void Conformance_ValidHonkDoesNotTerminateExecution()
    {
        const string corr = "honk-flow-corr";
        _history.Add(new SplashFrame
        {
            Source = "observer", Context = "honk-ctx",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
        }.ToQuackFrame());

        var egg = new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "honk-ctx", Correlation = corr,
            PlanId = "plan-honk", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        };
        _history.Add(egg.ToQuackFrame());
        _history.Add(QuackFrame.Bob("human", "executor", corr));
        _history.Add(new HatchFrame
        {
            Source = "executor", Destination = "human", Correlation = corr,
            PlanId = "plan-honk", ChallengeId = "ch", ApprovalUrl = "https://a",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            ChallengeExpiresAt = "2099-01-01T00:00:00Z",
        }.ToQuackFrame());

        // A honk in the middle should not block a valid flap
        _history.Add(new HonkFrame
        {
            Source = "gateway", ReasonCode = "QUOTA_WARNING", Reason = "Quota approaching limit",
        }.ToQuackFrame());

        var flap = new FlapFrame
        {
            Source = "executor", Destination = "gateway", Correlation = corr,
            PlanId = "plan-honk", GrantId = "grant-1",
            IntentDigest = "sha256:abc", ReviewDigest = "sha256:def",
            PreExecutionGates = ["quota-check"],
        };

        var result = _validator.Validate(flap, _history);
        Assert.IsTrue(result.IsValid, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Conformance_ValidMoltChallengeIsTerminal()
    {
        const string corr = "molt-challenge-corr";
        _history.Add(new EggFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx-1", Correlation = corr,
            PlanId = "plan-molt", MutationIntent = new(), IntentDigest = "sha256:abc",
            ReviewSurface = new(), ReviewDigest = "sha256:def",
            EvidenceArtifacts = [new EvidenceArtifact { Kind = "log", Digest = "sha256:ev" }],
            ApprovalPolicy = "same-subject",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
            FreshnessPolicy = new(),
        }.ToQuackFrame());

        // Molt a challenge — new hatch would be required after this
        var molt = new MoltFrame
        {
            Source = "executor", Correlation = corr,
            TerminalFor = "challenge", ReasonCode = "CHALLENGE_EXPIRED",
            Reason = "Approval window expired",
            PlanId = "plan-molt",
        };

        var result = _validator.Validate(molt, _history);
        Assert.IsTrue(result.IsValid);
    }
}
