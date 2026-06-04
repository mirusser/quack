namespace Quack.Negotiate.Tests;

[TestClass]
public sealed class NegotiateValidatorTests
{
    private readonly NegotiateValidator _validator = new();
    private readonly InMemoryQuackHistory _history = new();

    [TestMethod]
    public void Validate_PreenWithoutDabble_Fails()
    {
        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "no-such-dabble",
            SkillId = "k8s-rollout-restart", Applicability = "full",
            Constraints = new(), AssuranceLevel = "high",
        };

        var result = _validator.Validate(preen, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "PREEN_REQUIRES_DABBLE"));
    }

    [TestMethod]
    public void Validate_SettleWithoutPreen_Fails()
    {
        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "no-such-preen",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new(), AgreedAssuranceLevel = "high",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_REQUIRES_PREEN"));
    }

    [TestMethod]
    public void Validate_FullDabblePreenSettleFlow_Passes()
    {
        var dabble = new DabbleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "neg-cor-1",
            TaskIntent = "Restart deployment", RequiredCapabilities = ["k8s-exec"],
        };
        _history.Add(dabble.ToQuackFrame());

        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "neg-cor-1",
            SkillId = "k8s-rollout-restart", Applicability = "full",
            Constraints = new() { ["max"] = "5" }, AssuranceLevel = "high",
        };
        _history.Add(preen.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "neg-cor-1",
            NegotiationId = "neg-1", SkillId = "k8s-rollout-restart",
            AgreedConstraints = new() { ["max"] = "5" },
            AgreedAssuranceLevel = "high",
            ValidFrom = "2026-01-01T00:00:00Z", ValidUntil = "2099-01-01T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsTrue(result.IsValid, string.Join(", ", result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Validate_SettleExpiredWindow_Fails()
    {
        var preen = new PreenFrame
        {
            Source = "executor", Destination = "planner", Context = "ctx",
            Correlation = "exp-corr",
            SkillId = "skill-1", Applicability = "full",
            Constraints = new(), AssuranceLevel = "high",
        };
        _history.Add(preen.ToQuackFrame());

        var settle = new SettleFrame
        {
            Source = "planner", Destination = "executor", Context = "ctx",
            Correlation = "exp-corr",
            NegotiationId = "neg-1", SkillId = "skill-1",
            AgreedConstraints = new(), AgreedAssuranceLevel = "high",
            ValidFrom = "2020-01-01T00:00:00Z", ValidUntil = "2020-01-02T00:00:00Z",
        };

        var result = _validator.Validate(settle, _history);
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Code == "SETTLE_OUTSIDE_VALIDITY"));
    }
}
