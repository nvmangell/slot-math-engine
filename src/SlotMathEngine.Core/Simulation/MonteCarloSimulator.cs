using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Simulation;

/// <summary>
/// Runs a Monte Carlo simulation of N spins and returns aggregated statistics.
/// Supports parallel execution via partitioned spin batches.
/// </summary>
public class MonteCarloSimulator
{
    private readonly SimulationConfig _config;
    private readonly ReelEngine _reelEngine;
    private readonly PaylineEvaluator _paylineEvaluator;

    public MonteCarloSimulator(SimulationConfig config)
    {
        _config = config;
        _reelEngine = new ReelEngine(config);
        _paylineEvaluator = new PaylineEvaluator(config);
    }

    /// <summary>
    /// Runs <paramref name="spinCount"/> spins and returns all individual results.
    /// For very large runs (>1M), prefer <see cref="RunAggregated"/>.
    /// </summary>
    public IEnumerable<SpinResult> Run(long spinCount)
    {
        double totalWagered = 0;
        double totalPaid = 0;
        double betPerSpin = _config.BetPerLine * _config.Paylines.Count;

        for (long i = 0; i < spinCount; i++)
        {
            var window = _reelEngine.Spin();
            var wins = _paylineEvaluator.Evaluate(window);
            int scatterCount = _paylineEvaluator.CountScatters(window);

            double spinWin = wins.Sum(w => w.Payout);
            double scatterPayout = _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;
            spinWin += scatterPayout;

            totalWagered += betPerSpin;
            totalPaid += spinWin;

            yield return new SpinResult
            {
                SpinId = i + 1,
                Window = window,
                TotalBet = betPerSpin,
                TotalWin = spinWin,
                IsBonusTrigger = scatterCount >= _config.BonusTriggerScatterCount,
                RunningRtp = totalWagered > 0 ? totalPaid / totalWagered : 0,
                PaylineWins = wins
            };
        }
    }

    /// <summary>
    /// Runs <paramref name="spinCount"/> spins and returns only the aggregated report.
    /// More memory efficient than <see cref="Run"/> for large spin counts.
    /// </summary>
    public SimulationReport RunAggregated(long spinCount, double theoreticalRtp, double convergenceTolerancePct = 0.001)
    {
        var aggregator = new StatisticsAggregator(_config);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        double betPerSpin = _config.BetPerLine * _config.Paylines.Count;

        for (long i = 0; i < spinCount; i++)
        {
            var window = _reelEngine.Spin();
            var wins = _paylineEvaluator.Evaluate(window);
            int scatterCount = _paylineEvaluator.CountScatters(window);

            double spinWin = wins.Sum(w => w.Payout);
            spinWin += _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;

            bool isBonus = scatterCount >= _config.BonusTriggerScatterCount;

            aggregator.Record(betPerSpin, spinWin, isBonus);
        }

        sw.Stop();

        var report = aggregator.BuildReport(_config.GameId, spinCount, theoreticalRtp, convergenceTolerancePct);
        report.DurationMs = sw.ElapsedMilliseconds;
        return report;
    }
}
