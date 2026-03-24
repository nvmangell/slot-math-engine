using Serilog;
using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Output;
using SlotMathEngine.Core.Simulation;

// ─── Logging setup ──────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/simulation-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// ─── Argument parsing ────────────────────────────────────────────────────────
string configPath = "configs/default-game.json";
long spinCount = 1_000_000;
string outputDir = "sample-outputs";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
    else if (args[i] == "--spins" && i + 1 < args.Length) spinCount = long.Parse(args[++i]);
    else if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
}

// ─── Load config ─────────────────────────────────────────────────────────────
Log.Information("Loading game config from {ConfigPath}", configPath);
var config = await GameConfigLoader.LoadAsync(configPath);
Log.Information("Loaded game: {GameId} | Reels: {Reels} | Paylines: {Paylines} | Bonus: {Bonus}",
    config.GameId, config.Reels, config.Paylines.Count,
    config.BonusRoundConfig != null ? $"{config.BonusRoundConfig.FreeSpinCount} free spins @ {config.BonusRoundConfig.WinMultiplier}x" : "none");

// ─── Theoretical RTP ─────────────────────────────────────────────────────────
Log.Information("Calculating theoretical RTP (base + bonus)...");
var calculator = new RtpCalculator(config);
double theoreticalRtp = calculator.Calculate();
double theoreticalBaseRtp = calculator.CalculateBaseRtp();
double theoreticalBonusRtp = theoreticalRtp - theoreticalBaseRtp;
double bonusTriggerProb = calculator.CalculateBonusTriggerProbability();
Log.Information("Theoretical RTP: {Total:P4}  (Base: {Base:P4} + Bonus: {Bonus:P4})",
    theoreticalRtp, theoreticalBaseRtp, theoreticalBonusRtp);

// ─── Monte Carlo simulation ───────────────────────────────────────────────────
Log.Information("Starting Monte Carlo simulation: {Spins:N0} spins", spinCount);
var simulator = new MonteCarloSimulator(config);
// Bonus games have higher variance; 0.5% convergence is appropriate at 1M spins
var report = simulator.RunAggregated(spinCount, theoreticalRtp, theoreticalBaseRtp, convergenceTolerancePct: 0.005);

// ─── Print results ────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine(new string('─', 60));
Console.WriteLine($"  Game               : {report.GameId}");
Console.WriteLine($"  Total Spins        : {report.TotalSpins:N0}");
Console.WriteLine($"  Total Wagered      : {report.TotalWagered:N2}");
Console.WriteLine($"  Total Paid         : {report.TotalPaid:N2}");
Console.WriteLine(new string('─', 60));
Console.WriteLine($"  Theoretical RTP    : {report.TheoreticalRtp:P3}");
Console.WriteLine($"    ├─ Base Game     : {report.TheoreticalBaseRtp:P3}");
Console.WriteLine($"    └─ Bonus Round   : {report.TheoreticalBonusRtp:P3}");
Console.WriteLine($"  Simulated RTP      : {report.SimulatedRtp:P3}");
Console.WriteLine($"    ├─ Base Game     : {report.BaseGameRtp:P3}");
Console.WriteLine($"    └─ Bonus Round   : {report.BonusRtp:P3}");
Console.WriteLine($"  RTP Delta          : {report.RtpDelta:P4}   {(report.ConvergenceStatus == "PASS" ? "✓ PASS" : "✗ FAIL")}");
Console.WriteLine(new string('─', 60));
Console.WriteLine($"  Hit Frequency      : {report.HitFrequency:P2}");
Console.WriteLine($"  Average Win        : {report.AverageWin:N3}x");
Console.WriteLine($"  Max Win Observed   : {report.MaxWin:N1}x");
Console.WriteLine($"  Volatility Index   : {report.VolatilityIndex:N2}");
Console.WriteLine($"  Bonus Trigger Freq : 1 in {(report.BonusTriggerFrequency > 0 ? (1.0 / report.BonusTriggerFrequency) : 0):N0}");
if (config.BonusRoundConfig != null)
    Console.WriteLine($"  Bonus Config       : {config.BonusRoundConfig.FreeSpinCount} free spins × {config.BonusRoundConfig.WinMultiplier}x  (max {config.BonusRoundConfig.MaxWinMultiplier:N0}x cap)");
Console.WriteLine($"  Duration           : {report.DurationMs:N0}ms");
Console.WriteLine(new string('─', 60));
Console.WriteLine("  Win Distribution:");
foreach (var kv in report.WinDistribution)
    Console.WriteLine($"    {kv.Key,-18} {kv.Value:N0}");
Console.WriteLine(new string('─', 60));
Console.WriteLine();

// ─── Write output files ───────────────────────────────────────────────────────
Directory.CreateDirectory(outputDir);
var reportWriter = new CsvReportWriter();

string jsonPath = Path.Combine(outputDir, $"summary_{config.GameId}_{spinCount}spins.json");
await reportWriter.WriteReportJsonAsync(report, jsonPath);
Log.Information("Summary written to {Path}", jsonPath);

// For smaller runs, also write spin-level CSV
if (spinCount <= 100_000)
{
    Log.Information("Writing spin-level CSV (run <=100K spins)...");
    string csvPath = Path.Combine(outputDir, $"spins_{config.GameId}.csv");
    var spinResults = simulator.Run(spinCount);
    await reportWriter.WriteSpinResultsAsync(spinResults, csvPath);
    Log.Information("Spin CSV written to {Path}", csvPath);
}

Log.Information("Done.");
