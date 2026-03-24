using SlotMathEngine.Core.Models;

namespace SlotMathEngine.UnitTests.TestHelpers;

/// <summary>
/// Produces deterministic minimal game configs for unit testing.
/// </summary>
public static class ConfigFactory
{
    /// <summary>
    /// 3-reel, 1-row, 1-payline config with known, hand-calculable RTP.
    /// Each reel strip has exactly: A(1), B(2), C(2) → total 5 stops.
    /// P(A on one reel) = 1/5. P(AAA) = 1/125.
    /// Payout for AAA = 10. Expected = 10/125 = 0.08.
    /// Bet per spin = 1. Theoretical RTP = 8%.
    /// </summary>
    public static SimulationConfig SimpleThreeReel() => new()
    {
        GameId = "test-3reel",
        Reels = 3,
        Rows = 1,
        BetPerLine = 1.0,
        WildSymbolId = "WILD",
        ScatterSymbolId = "SCATTER",
        BonusTriggerScatterCount = 3,
        Symbols = new List<Symbol>
        {
            new() { Id = "A", Name = "A" },
            new() { Id = "B", Name = "B" },
            new() { Id = "C", Name = "C" }
        },
        ReelStrips = new List<ReelStrip>
        {
            new() { ReelIndex = 0, Symbols = new List<string> { "A", "B", "B", "C", "C" } },
            new() { ReelIndex = 1, Symbols = new List<string> { "A", "B", "B", "C", "C" } },
            new() { ReelIndex = 2, Symbols = new List<string> { "A", "B", "B", "C", "C" } }
        },
        Paylines = new List<PaylineDefinition>
        {
            new() { Id = 1, RowPositions = new List<int> { 0, 0, 0 } }
        },
        Paytable = new Paytable
        {
            Payouts = new Dictionary<string, Dictionary<int, double>>
            {
                ["A"] = new() { [3] = 10.0 },
                ["B"] = new() { [3] = 3.0 },
                ["C"] = new() { [3] = 1.0 }
            }
        }
    };
}
