namespace Quack.Negotiate.Tests;

[TestClass]
public sealed class NegotiateConformanceTests
{
    private readonly NegotiateValidator _validator = new();
    private readonly InMemoryQuackHistory _history = new();

    [TestMethod]
    public void Conformance_RejectPreenWithoutDabble()
    {
        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "no-dabble",
            SkillId = "k8s-rollout-restart", Applicability = "full",
            Constraints = new(), AssuranceLevel = "high",
        };

        var result = _validator.Validate(preen, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "PREEN_REQUIRES_DABBLE"));
    }

    [TestMethod]
    public void Conformance_RejectSettleWithoutPreen()
    {
        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "no-preen",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new(), AgreedAssuranceLevel = "high",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_REQUIRES_PREEN"));
    }

    [TestMethod]
    public void Conformance_RejectSettleExpiredWindow()
    {
        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "exp-nego",
            SkillId = "skill-1", Applicability = "full",
            Constraints = new(), AssuranceLevel = "high",
        };
        _history.Add(preen.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "exp-nego",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new(), AgreedAssuranceLevel = "high",
            ValidFrom = "2020-01-01T00:00:00Z", ValidUntil = "2020-01-02T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_OUTSIDE_VALIDITY"));
    }

    [TestMethod]
    public void Conformance_RejectSettleConstraintRelaxation()
    {
        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "constraint-corr",
            SkillId = "skill-1", Applicability = "full",
            Constraints = new() { ["max"] = "5" }, AssuranceLevel = "high",
        };
        _history.Add(preen.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "constraint-corr",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new() { ["max"] = "5", ["extra"] = "yes" },
            AgreedAssuranceLevel = "high",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_CONSTRAINT_MISMATCH"));
    }

    [TestMethod]
    public void Conformance_ValidFullFlow()
    {
        var dabble = new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "nego-full",
            TaskIntent = "Restart deployment", RequiredCapabilities = ["k8s-exec"],
        };
        _history.Add(dabble.ToQuackFrame());

        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "nego-full",
            SkillId = "k8s-rollout-restart", Applicability = "full",
            Constraints = new() { ["maxNodes"] = "3" }, AssuranceLevel = "high",
            PreExecutionGates = ["resource-check"],
            EstimatedDuration = "PT30S",
        };
        _history.Add(preen.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "nego-full",
            NegotiationId = "neg-full", SkillId = "k8s-rollout-restart",
            AgreedConstraints = new() { ["maxNodes"] = "3" },
            AgreedAssuranceLevel = "high",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsTrue(result.IsValid, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Conformance_ShunWithSuggestions_AlwaysValid()
    {
        var shun = new ShunFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            ReasonCode = "skill_unavailable", Reason = "Maintenance window active",
            SuggestedSkillIds = ["k8s-scale-replicas"],
        };

        var result = _validator.Validate(shun, _history);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Conformance_ValidPreenWithPartialApplicability()
    {
        _history.Add(new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "partial-corr",
            TaskIntent = "Scale deployment", RequiredCapabilities = ["k8s-exec"],
        }.ToQuackFrame());

        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "partial-corr",
            SkillId = "k8s-scale-replicas", Applicability = "partial",
            ApplicabilityNotes = "Can scale but requires quota increase",
            Constraints = new() { ["maxReplicas"] = "10" }, AssuranceLevel = "medium",
        };

        var result = _validator.Validate(preen, _history);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Conformance_RejectMissingDestination()
    {
        var dabble = new DabbleFrame
        {
            // No Destination set — negotiation frames must be point-to-point
            Source = "planner", Context = "ctx",
            TaskIntent = "Restart deployment", RequiredCapabilities = ["k8s-exec"],
        };

        var result = _validator.Validate(dabble, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "NEGOTIATION_REQUIRES_DESTINATION"));
    }

    [TestMethod]
    public void Conformance_RejectRiskAboveMax()
    {
        var dabble = new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Risk = QuackRisk.Critical,
            TaskIntent = "Critical operation", RequiredCapabilities = ["k8s-exec"],
        };

        var result = _validator.Validate(dabble, _history, maxRisk: QuackRisk.High);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "RISK_ABOVE_MAX"));
    }

    [TestMethod]
    public void Conformance_RejectExpiredFrame()
    {
        var dabble = new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            ExpiresAt = "2020-01-01T00:00:00Z",
            TaskIntent = "Expired operation", RequiredCapabilities = ["k8s-exec"],
        };

        var result = _validator.Validate(dabble, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "FRAME_EXPIRED"));
    }

    [TestMethod]
    public void Conformance_ValidMultiplePreensForSingleDabble()
    {
        _history.Add(new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "multi-preen-corr",
            TaskIntent = "Restart or scale deployment", RequiredCapabilities = ["k8s-exec"],
        }.ToQuackFrame());

        // Two preen responses for the same dabble
        var preen1 = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "multi-preen-corr",
            SkillId = "k8s-rollout-restart", Applicability = "full",
            Constraints = new() { ["freshnessWindow"] = "5m" }, AssuranceLevel = "high",
        };
        var preen2 = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "multi-preen-corr",
            SkillId = "k8s-scale-replicas", Applicability = "conditional",
            Constraints = new() { ["maxReplicas"] = "10" }, AssuranceLevel = "medium",
        };

        Assert.IsTrue(_validator.Validate(preen1, _history).IsValid);
        Assert.IsTrue(_validator.Validate(preen2, _history).IsValid);
    }

    [TestMethod]
    public void Conformance_RejectSettleInvalidWindow_ValidFromAfterValidUntil()
    {
        _history.Add(new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "inv-window",
            TaskIntent = "Test", RequiredCapabilities = [],
        }.ToQuackFrame());
        _history.Add(new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "inv-window",
            SkillId = "skill-1", Applicability = "full",
            Constraints = new(), AssuranceLevel = "high",
        }.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "inv-window",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new(), AgreedAssuranceLevel = "high",
            ValidFrom = "2099-01-01T00:00:00Z", ValidUntil = "2026-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_INVALID_WINDOW" || e.Code == "SETTLE_OUTSIDE_VALIDITY"));
    }
}
