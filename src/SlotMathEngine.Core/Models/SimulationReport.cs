namespace SlotMathEngine.Core.Models;

public class SimulationReport
{
    public string GameId { get; set; } = string.Empty;
    public long TotalSpins { get; set; }
    public double TotalWagered { get; set; }
    public double TotalPaid { get; set; }
    /// <summary>Simulated RTP from base game spins only (excluding bonus wins).</summary>
    public double BaseGameRtp { get; set; }

    /// <summary>Simulated RTP contribution from bonus round wins.</summary>
    public double BonusRtp { get; set; }

    /// <summary>Total simulated RTP = BaseGameRtp + BonusRtp.</summary>
    public double SimulatedRtp { get; set; }

    /// <summary>Theoretical base game RTP (analytical, no bonus).</summary>
    public double TheoreticalBaseRtp { get; set; }

    /// <summary>Theoretical bonus RTP contribution (analytical).</summary>
    public double TheoreticalBonusRtp { get; set; }

    /// <summary>Total theoretical RTP = TheoreticalBaseRtp + TheoreticalBonusRtp.</summary>
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
