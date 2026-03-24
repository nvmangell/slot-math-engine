namespace SlotMathEngine.Core.Models;

public class Paytable
{
    /// <summary>
    /// Symbol ID → (count → multiplier).
    /// e.g. "WILD" → { 3 → 10, 4 → 50, 5 → 500 }
    /// </summary>
    public Dictionary<string, Dictionary<int, double>> Payouts { get; set; } = new();

    /// <summary>
    /// Returns the payout multiplier for a given symbol and match count.
    /// Returns 0 if no win.
    /// </summary>
    public double GetPayout(string symbolId, int count)
    {
        if (Payouts.TryGetValue(symbolId, out var countMap))
            if (countMap.TryGetValue(count, out var multiplier))
                return multiplier;
        return 0;
    }

    public IEnumerable<string> PayingSymbols() => Payouts.Keys;
}
