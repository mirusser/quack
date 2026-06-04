namespace Quack.Conformance;

using System.IO;
using System.Text.Json;

sealed class ConformanceRunner(ConformanceClient client, string fixturesPath, string? profileFilter, bool verbose)
{
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<RunReport> RunAsync(CancellationToken ct = default)
    {
        var suites = DiscoverSuites();
        var report = new RunReport();

        foreach (var suite in suites)
        {
            if (profileFilter is not null && suite.Profile != profileFilter)
                continue;

            Console.WriteLine();
            Console.WriteLine($"  {suite.Profile} / {suite.Validity} ({suite.Cases.Count} cases)");

            foreach (var tc in suite.Cases)
            {
                ct.ThrowIfCancellationRequested();
                var result = await RunCaseAsync(tc, ct);
                report.Record(result);
                Print(tc.Name, tc, result);
            }
        }

        return report;
    }

    // ── Fixture discovery ─────────────────────────────────────────────────────

    List<FixtureSuite> DiscoverSuites()
    {
        var suites = new List<FixtureSuite>();

        // Core fixtures sit directly in fixtures/{validity}/
        AddSuiteIfExists(suites, "core", "invalid", Path.Combine(fixturesPath, "invalid"));
        AddSuiteIfExists(suites, "core", "valid",   Path.Combine(fixturesPath, "valid"));

        // Profile fixtures sit in fixtures/{profile}/{validity}/
        if (!Directory.Exists(fixturesPath))
            return suites;

        foreach (var profileDir in Directory.GetDirectories(fixturesPath).Order())
        {
            var profile = Path.GetFileName(profileDir);
            AddSuiteIfExists(suites, profile, "invalid", Path.Combine(profileDir, "invalid"));
            AddSuiteIfExists(suites, profile, "valid",   Path.Combine(profileDir, "valid"));
        }

        return suites;
    }

    void AddSuiteIfExists(List<FixtureSuite> suites, string profile, string validity, string dir)
    {
        if (!Directory.Exists(dir))
            return;

        var cases = Directory.GetFiles(dir, "*.json")
            .Order()
            .Select(path => ParseCase(Path.GetFileNameWithoutExtension(path), profile, validity, File.ReadAllText(path)))
            .ToList();

        if (cases.Count > 0)
            suites.Add(new FixtureSuite(profile, validity, cases));
    }

    TestCase ParseCase(string name, string profile, string validity, string json)
    {
        if (validity == "valid")
        {
            var frames = JsonSerializer.Deserialize<JsonElement[]>(json, JsonOpts) ?? [];
            return new ValidSequenceCase(name, profile, frames);
        }

        var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);

        if (doc.TryGetProperty("expectedRejection", out _))
        {
            var fixture = JsonSerializer.Deserialize<InvalidFixture>(json, JsonOpts)!;
            return new WrappedInvalidCase(name, profile, fixture);
        }

        // Raw frame (core profile — no wrapper)
        return new RawInvalidCase(name, profile, doc);
    }

    // ── Test execution ────────────────────────────────────────────────────────

    async Task<TestResult> RunCaseAsync(TestCase tc, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        try
        {
            return tc switch
            {
                WrappedInvalidCase w => await RunWrappedInvalidAsync(w, sessionId, ct),
                RawInvalidCase r     => await RunRawInvalidAsync(r, sessionId, ct),
                ValidSequenceCase v  => await RunValidSequenceAsync(v, sessionId, ct),
                _                    => new TestResult.Err("unrecognised test case type"),
            };
        }
        catch (ConformanceNetworkException ex)
        {
            return new TestResult.Err(ex.Message);
        }
        catch (ConformanceProtocolException ex)
        {
            return new TestResult.Err(ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TestResult.Err($"unexpected error: {ex.Message}");
        }
    }

    async Task<TestResult> RunWrappedInvalidAsync(WrappedInvalidCase tc, string sessionId, CancellationToken ct)
    {
        var config = new ConformConfig(tc.Fixture.AgentMaxRisk, tc.Fixture.AgentCardSkills);

        if (tc.Fixture.SeedFrames is { Length: > 0 } seeds)
        {
            for (int i = 0; i < seeds.Length; i++)
            {
                var seedResp = await client.SendAsync(new ConformRequest(sessionId, config, seeds[i]), ct);
                if (!seedResp.Ok)
                    return new TestResult.Err($"seed frame {i + 1} rejected unexpectedly: {seedResp.Rejection}");
            }
        }

        var resp = await client.SendAsync(new ConformRequest(sessionId, config, tc.Fixture.Frame), ct);

        if (resp.Ok)
            return new TestResult.Fail($"expected rejection '{tc.Fixture.ExpectedRejection}', agent accepted");

        if (resp.Rejection != tc.Fixture.ExpectedRejection)
            return new TestResult.Fail($"expected '{tc.Fixture.ExpectedRejection}', got '{resp.Rejection}'");

        return new TestResult.Pass();
    }

    async Task<TestResult> RunRawInvalidAsync(RawInvalidCase tc, string sessionId, CancellationToken ct)
    {
        var resp = await client.SendAsync(new ConformRequest(sessionId, null, tc.Frame), ct);
        return resp.Ok
            ? new TestResult.Fail("expected rejection, agent accepted")
            : new TestResult.Pass();
    }

    async Task<TestResult> RunValidSequenceAsync(ValidSequenceCase tc, string sessionId, CancellationToken ct)
    {
        for (int i = 0; i < tc.Frames.Length; i++)
        {
            var resp = await client.SendAsync(new ConformRequest(sessionId, null, tc.Frames[i]), ct);
            if (!resp.Ok)
                return new TestResult.Fail($"frame {i + 1}/{tc.Frames.Length} rejected: {resp.Rejection}");
        }
        return new TestResult.Pass();
    }

    // ── Output ────────────────────────────────────────────────────────────────

    void Print(string name, TestCase tc, TestResult result)
    {
        var (label, color) = result switch
        {
            TestResult.Pass => ("PASS ", ConsoleColor.Green),
            TestResult.Fail => ("FAIL ", ConsoleColor.Red),
            TestResult.Err  => ("ERROR", ConsoleColor.Yellow),
            _               => ("?    ", ConsoleColor.Gray),
        };

        Console.Write("    ");
        Console.ForegroundColor = color;
        Console.Write(label);
        Console.ResetColor();
        Console.Write("  ");
        Console.Write(name);

        if (tc is ValidSequenceCase vs)
            Console.Write($" ({vs.Frames.Length} frames)");

        if (result is TestResult.Fail f)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" — {f.Reason}");
            Console.ResetColor();
        }
        else if (result is TestResult.Err e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($" — {e.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();

        if (verbose && result is TestResult.Fail fail)
            Console.WriteLine($"         reason: {fail.Reason}");
    }
}
