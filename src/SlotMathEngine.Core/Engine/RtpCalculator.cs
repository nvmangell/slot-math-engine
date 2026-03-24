using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Engine;

/// <summary>
/// Computes theoretical RTP analytically by enumerating all possible reel stop combinations
/// and summing probability-weighted payouts across all paylines.
///
/// Base game RTP = Σ P(combo) × Payout(combo) / betPerSpin
///
/// Bonus contribution = P(trigger) × FreeSpinCount × baseRtp × WinMultiplier
/// Total RTP = baseRtp + bonusContribution
///
/// For large reel strips this is computationally expensive — use for validation, not hot path.
/// </summary>
public class RtpCalculator
{
    private readonly SimulationConfig _config;
    private (double baseRtp, double bonusTriggerProbability)? _cachedResult;

    public RtpCalculator(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Calculates total theoretical RTP including bonus round contribution (if configured).
    /// Returns base game RTP when no <see cref="BonusRoundConfig"/> is set.
    /// </summary>
    public double Calculate()
    {
        var (baseRtp, bonusTriggerProbability) = CalculateBaseRtpAndTriggerProbability();

        if (_config.BonusRoundConfig is null)
            return baseRtp;

        double bonusContribution = bonusTriggerProbability
            * _config.BonusRoundConfig.FreeSpinCount
            * baseRtp
            * _config.BonusRoundConfig.WinMultiplier;

        return baseRtp + bonusContribution;
    }

    /// <summary>
    /// Returns the base game RTP (paylines + scatter, no bonus).
    /// </summary>
    public double CalculateBaseRtp()
    {
        var (baseRtp, _) = CalculateBaseRtpAndTriggerProbability();
        return baseRtp;
    }

    /// <summary>
    /// Returns the analytical probability that any given spin triggers the bonus round.
    /// </summary>
    public double CalculateBonusTriggerProbability()
    {
        var (_, prob) = CalculateBaseRtpAndTriggerProbability();
        return prob;
    }

    private (double baseRtp, double bonusTriggerProbability) CalculateBaseRtpAndTriggerProbability()
    {
        if (_cachedResult.HasValue)
            return _cachedResult.Value;

        double totalExpectedPayout = 0;
        double bonusTriggerExpected = 0;
        double totalBetPerSpin = _config.BetPerLine * _config.Paylines.Count;

        var reelSizes = _config.ReelStrips.Select(r => r.Symbols.Count).ToList();
        long totalCombinations = reelSizes.Aggregate(1L, (acc, size) => acc * size);

        var evaluator = new PaylineEvaluator(_config);

        long[] stops = new long[_config.Reels];
        for (long combo = 0; combo < totalCombinations; combo++)
        {
            long remaining = combo;
            for (int r = _config.Reels - 1; r >= 0; r--)
            {
                stops[r] = remaining % reelSizes[r];
                remaining /= reelSizes[r];
            }

            var window = BuildWindow(stops);

            double prob = 1.0;
            for (int r = 0; r < _config.Reels; r++)
                prob /= reelSizes[r];

            var wins = evaluator.Evaluate(window);
            double comboPayout = wins.Sum(w => w.Payout);

            int scatterCount = evaluator.CountScatters(window);
            comboPayout += _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;

            totalExpectedPayout += prob * comboPayout;

            if (scatterCount >= _config.BonusTriggerScatterCount)
                bonusTriggerExpected += prob;
        }

        double baseRtp = totalBetPerSpin > 0 ? totalExpectedPayout / totalBetPerSpin : 0;
        _cachedResult = (baseRtp, bonusTriggerExpected);
        return _cachedResult.Value;
    }

    private List<List<string>> BuildWindow(long[] stops)
    {
        var window = new List<List<string>>(_config.Reels);
        for (int r = 0; r < _config.Reels; r++)
        {
            var strip = _config.ReelStrips[r];
            var column = new List<string>(_config.Rows);
            for (int row = 0; row < _config.Rows; row++)
            {
                int index = (int)((stops[r] + row) % strip.Symbols.Count);
                column.Add(strip.Symbols[index]);
            }
            window.Add(column);
        }
        return window;
    }
}
