namespace SlotMathEngine.Core.Models;

public class SimulationReport
{
    public string GameId { get; set; } = string.Empty;
    public long TotalSpins { get; set; }
    public double TotalWagered { get; set; }
    public double TotalPaid { get; set; }
    public double SimulatedRtp { get; set; }
    public double TheoreticalRtp { get; set; }
    public double RtpDelta { get; set; }
    public double HitFrequency { get; set; }
    public double AverageWin { get; set; }
    public double MaxWin { get; set; }
    public double VolatilityIndex { get; set; }
    public double BonusTriggerFrequency { get; set; }
    public long DurationMs { get; set; }
    public string ConvergenceStatus { get; set; } = string.Empty;

    /// <summary>Win distribution: multiplier bucket → count of spins.</summary>
    public Dictionary<string, long> WinDistribution { get; set; } = new();
}
