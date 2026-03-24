namespace SlotMathEngine.Core.Models;

public class ReelStrip
{
    public int ReelIndex { get; set; }
    public List<string> Symbols { get; set; } = new();

    /// <summary>
    /// Returns the probability of landing a given symbol on this reel.
    /// </summary>
    public double GetSymbolProbability(string symbolId)
    {
        if (Symbols.Count == 0) return 0;
        int count = Symbols.Count(s => s == symbolId);
        return (double)count / Symbols.Count;
    }

    public IEnumerable<string> UniqueSymbols() => Symbols.Distinct();
}
