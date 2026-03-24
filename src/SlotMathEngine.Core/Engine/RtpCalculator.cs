using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Engine;

/// <summary>
/// Computes theoretical RTP analytically by enumerating all possible reel stop combinations
/// and summing probability-weighted payouts across all paylines.
///
/// For large reel strips this is computationally expensive — use for validation, not hot path.
/// RTP = sum over all combos of [ P(combo) * Payout(combo) ] / (betPerLine * paylineCount)
/// </summary>
public class RtpCalculator
{
    private readonly SimulationConfig _config;

    public RtpCalculator(SimulationConfig config)
    {
        _config = config;
    }

    public double Calculate()
    {
        double totalExpectedPayout = 0;
        double totalBetPerSpin = _config.BetPerLine * _config.Paylines.Count;

        // Get unique stop counts per reel
        var reelSizes = _config.ReelStrips.Select(r => r.Symbols.Count).ToList();
        long totalCombinations = reelSizes.Aggregate(1L, (acc, size) => acc * size);

        var evaluator = new PaylineEvaluator(_config);

        // Enumerate all reel stop combinations
        long[] stops = new long[_config.Reels];
        for (long combo = 0; combo < totalCombinations; combo++)
        {
            // Decode combo index into per-reel stop positions
            long remaining = combo;
            for (int r = _config.Reels - 1; r >= 0; r--)
            {
                stops[r] = remaining % reelSizes[r];
                remaining /= reelSizes[r];
            }

            // Build the window for this combination
            var window = BuildWindow(stops);

            // Probability of this exact combination
            double prob = 1.0;
            for (int r = 0; r < _config.Reels; r++)
                prob /= reelSizes[r];

            // Evaluate payline wins
            var wins = evaluator.Evaluate(window);
            double comboPayout = wins.Sum(w => w.Payout);

            // Scatter bonus payout (simplified: scatter pays regardless of paylines)
            int scatterCount = evaluator.CountScatters(window);
            comboPayout += _config.Paytable.GetPayout(_config.ScatterSymbolId, scatterCount) * _config.BetPerLine;

            totalExpectedPayout += prob * comboPayout;
        }

        return totalBetPerSpin > 0 ? totalExpectedPayout / totalBetPerSpin : 0;
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
