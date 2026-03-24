using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Simulation;

/// <summary>
/// Runs a Monte Carlo simulation of N spins and returns aggregated statistics.
/// When a <see cref="BonusRoundConfig"/> is present in the config, bonus trigger spins
/// automatically simulate the configured number of free spins at the set multiplier.
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
    /// Runs <paramref name="spinCount"/> spins and returns all individual base-spin results.
    /// Bonus rounds are NOT simulated here — use <see cref="RunAggregated"/> for full simulation.
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
    /// Simulates bonus free spin rounds when triggered and <see cref="BonusRoundConfig"/> is set.
    /// More memory efficient than <see cref="Run"/> for large spin counts.
    /// </summary>
    public SimulationReport RunAggregated(long spinCount, double theoreticalRtp, double theoreticalBaseRtp = 0, double convergenceTolerancePct = 0.001)
    {
        var aggregator = new StatisticsAggregator(_config);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        double betPerSpin = _config.BetPerLine * _config.Paylines.Count;
        var bonusConfig = _config.BonusRoundConfig;

        for (long i = 0; i < spinCount; i++)
        {
            var window = _reelEngine.Spin();
            var wins = _paylineEvaluator.Evaluate(window);
            int scatterCount = _paylineEvaluator.CountScatters(window);

            double baseWin = wins.Sum(w => w.Payout);
            baseWin += _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;

            bool isBonus = scatterCount >= _config.BonusTriggerScatterCount;

            double bonusWin = 0;
            if (isBonus && bonusConfig != null)
                bonusWin = SimulateFreeSpins(bonusConfig, betPerSpin);

            aggregator.Record(betPerSpin, baseWin, bonusWin, isBonus);
        }

        sw.Stop();

        // Fall back to theoreticalRtp as base if theoreticalBaseRtp not provided
        double baseRtp = theoreticalBaseRtp > 0 ? theoreticalBaseRtp : theoreticalRtp;

        var report = aggregator.BuildReport(_config.GameId, spinCount, theoreticalRtp, baseRtp, convergenceTolerancePct);
        report.DurationMs = sw.ElapsedMilliseconds;
        return report;
    }

    private double SimulateFreeSpins(BonusRoundConfig bonusConfig, double betPerSpin)
    {
        double totalBonusWin = 0;
        double maxWinCap = bonusConfig.MaxWinMultiplier > 0 ? bonusConfig.MaxWinMultiplier * betPerSpin : double.MaxValue;

        for (int fs = 0; fs < bonusConfig.FreeSpinCount; fs++)
        {
            var window = _reelEngine.Spin();
            var wins = _paylineEvaluator.Evaluate(window);
            int scatterCount = _paylineEvaluator.CountScatters(window);

            double spinWin = wins.Sum(w => w.Payout);
            spinWin += _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;

            totalBonusWin += spinWin * bonusConfig.WinMultiplier;

            if (totalBonusWin >= maxWinCap)
            {
                totalBonusWin = maxWinCap;
                break;
            }
        }

        return totalBonusWin;
    }
}
