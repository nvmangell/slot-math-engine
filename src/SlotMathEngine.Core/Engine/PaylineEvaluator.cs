using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Engine;

/// <summary>
/// Evaluates win conditions for each defined payline against a spin window.
/// Supports Wild substitution: Wilds match any paying symbol.
/// </summary>
public class PaylineEvaluator
{
    private readonly SimulationConfig _config;

    public PaylineEvaluator(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Evaluates all paylines against the given window.
    /// Returns all winning payline results (zero-length if no wins).
    /// </summary>
    public List<PaylineWin> Evaluate(List<List<string>> window)
    {
        var wins = new List<PaylineWin>();

        foreach (var payline in _config.Paylines)
        {
            var win = EvaluatePayline(payline, window);
            if (win != null)
                wins.Add(win);
        }

        return wins;
    }

    private PaylineWin? EvaluatePayline(PaylineDefinition payline, List<List<string>> window)
    {
        // Read the symbol at each reel position defined by this payline
        var symbols = new List<string>();
        for (int reelIdx = 0; reelIdx < _config.Reels && reelIdx < payline.RowPositions.Count; reelIdx++)
        {
            int rowIdx = payline.RowPositions[reelIdx];
            symbols.Add(window[reelIdx][rowIdx]);
        }

        // Determine the anchor symbol: first non-Wild, non-Scatter symbol from the left
        string? anchorSymbol = null;
        foreach (var s in symbols)
        {
            if (s != _config.WildSymbolId && s != _config.ScatterSymbolId)
            {
                anchorSymbol = s;
                break;
            }
        }

        // If all wilds, use Wild as the paying symbol
        if (anchorSymbol == null && symbols.All(s => s == _config.WildSymbolId))
            anchorSymbol = _config.WildSymbolId;

        if (anchorSymbol == null)
            return null;

        // Count consecutive matches from left (symbol or wild)
        int matchCount = 0;
        foreach (var s in symbols)
        {
            if (s == anchorSymbol || s == _config.WildSymbolId)
                matchCount++;
            else
                break;
        }

        if (matchCount < 3)
            return null;

        double payout = _config.Paytable.GetPayout(anchorSymbol, matchCount);
        if (payout <= 0)
            return null;

        return new PaylineWin
        {
            PaylineId = payline.Id,
            MatchedSymbol = anchorSymbol,
            MatchCount = matchCount,
            Payout = payout * _config.BetPerLine
        };
    }

    /// <summary>
    /// Counts scatter symbols anywhere in the window (not payline-restricted).
    /// </summary>
    public int CountScatters(List<List<string>> window)
    {
        return window.Sum(reel => reel.Count(s => s == _config.ScatterSymbolId));
    }
}
