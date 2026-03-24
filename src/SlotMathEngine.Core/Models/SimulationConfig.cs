namespace SlotMathEngine.Core.Models;

public class SimulationConfig
{
    public string GameId { get; set; } = string.Empty;
    public int Reels { get; set; } = 5;
    public int Rows { get; set; } = 3;
    public double BetPerLine { get; set; } = 1.0;
    public List<ReelStrip> ReelStrips { get; set; } = new();
    public List<PaylineDefinition> Paylines { get; set; } = new();
    public Paytable Paytable { get; set; } = new();
    public List<Symbol> Symbols { get; set; } = new();

    /// <summary>Symbol ID used as Wild (substitutes in payline evaluation).</summary>
    public string WildSymbolId { get; set; } = "WILD";

    /// <summary>Symbol ID used as Scatter (evaluated independently of paylines).</summary>
    public string ScatterSymbolId { get; set; } = "SCATTER";

    /// <summary>Min scatter count to trigger bonus.</summary>
    public int BonusTriggerScatterCount { get; set; } = 3;

    /// <summary>
    /// Free spin bonus round configuration. Null means no bonus round is simulated.
    /// </summary>
    public BonusRoundConfig? BonusRoundConfig { get; set; }
}
