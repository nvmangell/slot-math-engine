using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Models;
using SlotMathEngine.Core.Output;
using SlotMathEngine.Core.Simulation;

namespace SlotMathEngine.Api.Controllers;

/// <summary>
/// Singleton service that loads game configs from disk and runs simulations.
/// Configs are cached in memory after first load.
/// </summary>
public class SimulationService
{
    private readonly ILogger<SimulationService> _logger;
    private readonly string _configBasePath;
    private readonly Dictionary<string, SimulationConfig> _configCache = new();

    public SimulationService(ILogger<SimulationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configBasePath = configuration["GameConfigPath"] ?? "configs";
    }

    public async Task<SimulationConfig> GetConfigAsync(string gameId)
    {
        if (_configCache.TryGetValue(gameId, out var cached))
            return cached;

        var path = Path.Combine(_configBasePath, $"{gameId}.json");
        _logger.LogInformation("Loading config {GameId} from {Path}", gameId, path);
        var config = await GameConfigLoader.LoadAsync(path);
        _configCache[gameId] = config;
        return config;
    }

    public async Task<SimulationRunResult> RunSimulationAsync(SimulationRequest request)
    {
        var config = await GetConfigAsync(request.GameId);

        _logger.LogInformation("Running simulation for {GameId}: {Spins:N0} spins", request.GameId, request.SpinCount);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var rtpCalc = new RtpCalculator(config);
        double theoreticalRtp = rtpCalc.Calculate();

        var simulator = new MonteCarloSimulator(config);
        var report = simulator.RunAggregated(request.SpinCount, theoreticalRtp, request.ConvergenceTolerance);

        sw.Stop();
        _logger.LogInformation("Simulation complete in {Ms}ms | RTP: {Rtp:P3} | Status: {Status}",
            sw.ElapsedMilliseconds, report.SimulatedRtp, report.ConvergenceStatus);

        return new SimulationRunResult { Report = report, TheoreticalRtp = theoreticalRtp };
    }

    public async Task<PaytableInfo> GetPaytableInfoAsync(string gameId)
    {
        var config = await GetConfigAsync(gameId);

        var symbolProbabilities = new Dictionary<string, Dictionary<string, double>>();
        foreach (var strip in config.ReelStrips)
        {
            var probs = new Dictionary<string, double>();
            foreach (var sym in strip.UniqueSymbols())
                probs[sym] = strip.GetSymbolProbability(sym);
            symbolProbabilities[$"reel{strip.ReelIndex + 1}"] = probs;
        }

        return new PaytableInfo
        {
            GameId = config.GameId,
            Reels = config.Reels,
            Rows = config.Rows,
            PaylineCount = config.Paylines.Count,
            BetPerLine = config.BetPerLine,
            Payouts = config.Paytable.Payouts,
            SymbolProbabilities = symbolProbabilities
        };
    }
}

public record SimulationRequest
{
    public string GameId { get; init; } = "default-game-v1";
    public long SpinCount { get; init; } = 1_000_000;
    public double ConvergenceTolerance { get; init; } = 0.001;
}

public record SimulationRunResult
{
    public SimulationReport Report { get; init; } = null!;
    public double TheoreticalRtp { get; init; }
}

public record PaytableInfo
{
    public string GameId { get; init; } = string.Empty;
    public int Reels { get; init; }
    public int Rows { get; init; }
    public int PaylineCount { get; init; }
    public double BetPerLine { get; init; }
    public Dictionary<string, Dictionary<int, double>> Payouts { get; init; } = new();
    public Dictionary<string, Dictionary<string, double>> SymbolProbabilities { get; init; } = new();
}
