namespace SlotMathEngine.Core.Models;

public class SpinResult
{
    public long SpinId { get; set; }

    /// <summary>
    /// The visible window: reelIndex → list of symbols from top to bottom row.
    /// </summary>
    public List<List<string>> Window { get; set; } = new();

    public double TotalBet { get; set; }
    public double TotalWin { get; set; }
    public bool IsBonusTrigger { get; set; }

    /// <summary>Running RTP after this spin (totalPaid / totalWagered so far).</summary>
    public double RunningRtp { get; set; }

    public List<PaylineWin> PaylineWins { get; set; } = new();
}

public class PaylineWin
{
    public int PaylineId { get; set; }
    public string MatchedSymbol { get; set; } = string.Empty;
    public int MatchCount { get; set; }
    public double Payout { get; set; }
}
