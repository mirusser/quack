namespace Quack.Tests;

[TestClass]
public sealed class QuackValidatorTests
{
    private readonly QuackValidator _validator = new();

    [TestMethod]
    public void Validate_ValidQuackFrame_Passes()
    {
        var frame = QuackFrame.Quack("observer");

        var result = _validator.Validate(frame);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_HonkWithoutReason_FailsHonkWithoutReason()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Honk,
            Id = QuackId.NewId(),
            Source = "gateway",
        };

        var result = _validator.Validate(frame);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.HonkWithoutReason));
    }

    [TestMethod]
    public void Validate_HonkWithReason_Passes()
    {
        var frame = QuackFrame.Honk("gateway", "digest mismatch");

        var result = _validator.Validate(frame);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_MoltWithoutCorrelation_Fails()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Molt,
            Id = QuackId.NewId(),
            Source = "gateway",
        };

        var result = _validator.Validate(frame);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.MoltWithoutCorrelation));
    }

    [TestMethod]
    public void Validate_SplashWithoutEvidence_Fails()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Splash,
            Id = QuackId.NewId(),
            Source = "observer",
        };

        var result = _validator.Validate(frame);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.SplashWithoutEvidence));
    }

    [TestMethod]
    public void Validate_NonBroadcastWithoutDestination_Fails()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Peck,
            Id = QuackId.NewId(),
            Source = "client",
            // no destination
        };

        var result = _validator.Validate(frame);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.NonBroadcastWithoutDestination));
    }

    [TestMethod]
    public void Validate_BroadcastWithoutDestination_Passes()
    {
        // Quack is a broadcast verb — can omit destination
        var frame = QuackFrame.Quack("observer");

        var result = _validator.Validate(frame);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_EggWithoutDigest_FailsInStatefulMode()
    {
        var frame = new QuackFrame
        {
            Version = 1,
            Verb = QuackVerb.Egg,
            Id = QuackId.NewId(),
            Source = "planner",
            Destination = "executor",
            // no digest
        };

        var history = new InMemoryQuackHistory();

        var result = _validator.Validate(frame, history);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.EggWithoutProof));
    }

    [TestMethod]
    public void Validate_HatchWithoutEgg_Fails()
    {
        var frame = QuackFrame.Hatch("reviewer", "gateway", "plan-1", "corr-1");
        var history = new InMemoryQuackHistory();

        var result = _validator.Validate(frame, history);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.HatchWithoutEgg));
    }

    [TestMethod]
    public void Validate_HatchAfterEgg_Passes()
    {
        var egg = QuackFrame.Egg("planner", "gateway", "plan-1", "sha256:abc", correlation: "corr-1");
        var history = new InMemoryQuackHistory();
        history.Add(egg);

        var frame = QuackFrame.Hatch("reviewer", "gateway", "plan-1", "corr-1");

        var result = _validator.Validate(frame, history);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_FlapWithoutHatchAndBob_Fails()
    {
        var frame = QuackFrame.Flap("executor", "gateway", "plan-1", "sha256:abc", "corr-1");
        var history = new InMemoryQuackHistory();

        var result = _validator.Validate(frame, history);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.FlapWithoutHatch));
    }

    [TestMethod]
    public void Validate_EggWithoutSplash_Fails()
    {
        var frame = QuackFrame.Egg("planner", "executor", "plan-1", "sha256:abc", context: "ctx-1");
        var history = new InMemoryQuackHistory();

        var result = _validator.Validate(frame, history);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Rule == QuackRule.EggWithoutSplash));
    }

    [TestMethod]
    public void Validate_EggAfterSplash_Passes()
    {
        var splash = QuackFrame.Splash("observer", [
            new EvidenceRef { Kind = "k8s.events", Digest = "sha256:abc" }
        ], context: "ctx-1");
        var history = new InMemoryQuackHistory();
        history.Add(splash);

        var frame = QuackFrame.Egg("planner", "executor", "plan-1", "sha256:abc", context: "ctx-1");

        var result = _validator.Validate(frame, history);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_FlapWithFullChain_Passes()
    {
        var history = new InMemoryQuackHistory();
        history.Add(QuackFrame.Egg("planner", "executor", "plan-1", "sha256:abc", correlation: "corr-1"));
        history.Add(QuackFrame.Hatch("reviewer", "gateway", "plan-1", "corr-1"));
        history.Add(QuackFrame.Bob("gateway", "planner", "corr-1"));

        var frame = QuackFrame.Flap("executor", "gateway", "plan-1", "sha256:abc", "corr-1");

        var result = _validator.Validate(frame, history);

        Assert.IsTrue(result.IsValid);
    }
}

internal sealed class InMemoryQuackHistory : IQuackHistory
{
    private readonly List<QuackFrame> _frames = new();

    public void Add(QuackFrame frame) => _frames.Add(frame);

    public IReadOnlyList<QuackFrame> GetByCorrelation(string correlation) =>
        _frames.Where(f => f.Correlation == correlation).ToList();

    public IReadOnlyList<QuackFrame> GetByContext(string context) =>
        _frames.Where(f => f.Context == context).ToList();
}
