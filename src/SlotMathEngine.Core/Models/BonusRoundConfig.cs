namespace SlotMathEngine.Core.Models;

/// <summary>
/// Configuration for the free spin bonus round triggered by scatter symbols.
/// </summary>
public class BonusRoundConfig
{
    /// <summary>Number of free spins awarded when the bonus is triggered.</summary>
    public int FreeSpinCount { get; set; } = 10;

    /// <summary>Win multiplier applied to all payouts during free spins.</summary>
    public double WinMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum total win cap expressed as a multiple of bet per spin.
    /// The total win (base + bonus) for any trigger is capped here. 0 = disabled.
    /// </summary>
    public double MaxWinMultiplier { get; set; } = 5000.0;
}
