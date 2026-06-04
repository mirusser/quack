using System.Net.Http;
using Quack.Conformance;

// ── Argument parsing ──────────────────────────────────────────────────────────

string? endpoint = null;
string? fixturesPath = null;
string? profile = null;
bool verbose = false;
bool help = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--endpoint" or "-e" when i + 1 < args.Length:
            endpoint = args[++i];
            break;
        case "--fixtures" or "-f" when i + 1 < args.Length:
            fixturesPath = args[++i];
            break;
        case "--profile" or "-p" when i + 1 < args.Length:
            profile = args[++i];
            break;
        case "--verbose" or "-v":
            verbose = true;
            break;
        case "--help" or "-h":
            help = true;
            break;
    }
}

if (help || endpoint is null)
{
    Console.WriteLine("""
        quack-conform — Quack protocol conformance test runner

        Usage:
          quack-conform --endpoint <url> [options]

        Options:
          --endpoint, -e  <url>       Base URL of agent under test (required)
          --fixtures, -f  <path>      Path to fixtures directory (default: ./spec/fixtures)
          --profile,  -p  <name>      Only run this profile: core, mutation, negotiate
          --verbose,  -v              Show failure detail inline
          --help,     -h              Show this help

        Wire contract:
          The runner posts to POST {endpoint}/quack/conform with:
            { "sessionId": "...", "config": { "maxRisk": "...", "agentCardSkills": [...] }, "frame": { ... } }
          The agent must respond with:
            { "ok": true }               on acceptance
            { "ok": false, "rejection": "error_code" }  on rejection

        Examples:
          quack-conform --endpoint http://localhost:5000
          quack-conform --endpoint http://localhost:5000 --fixtures ../../spec/fixtures --profile mutation
        """);
    return help ? 0 : 1;
}

// Resolve fixtures path relative to CWD
fixturesPath = Path.GetFullPath(fixturesPath ?? Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "spec", "fixtures"));

// ── Header ────────────────────────────────────────────────────────────────────

var divider = new string('─', 56);

Console.WriteLine("Quack Conformance Runner");
Console.WriteLine($"Endpoint : {endpoint}");
Console.WriteLine($"Fixtures : {fixturesPath}");
Console.WriteLine($"Profile  : {profile ?? "all"}");
Console.WriteLine(divider);

// ── Run ───────────────────────────────────────────────────────────────────────

using var http = new HttpClient();
var client = new ConformanceClient(http, endpoint.TrimEnd('/'));
var runner = new ConformanceRunner(client, fixturesPath, profile, verbose);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

RunReport report;
try
{
    report = await runner.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("Cancelled.");
    return 130;
}

// ── Summary ───────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine(divider);
Console.Write($"  Passed: ");
Console.ForegroundColor = report.Passed > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
Console.Write(report.Passed);
Console.ResetColor();

Console.Write($"    Failed: ");
Console.ForegroundColor = report.Failed > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
Console.Write(report.Failed);
Console.ResetColor();

if (report.Errored > 0)
{
    Console.Write($"    Errors: ");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(report.Errored);
    Console.ResetColor();
}

Console.WriteLine($"    Total: {report.Total}");

return report.AllPassed ? 0 : 1;
