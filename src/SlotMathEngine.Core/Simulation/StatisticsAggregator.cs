using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Simulation;

/// <summary>
/// Accumulates per-spin statistics during a simulation run and produces
/// a final <see cref="SimulationReport"/> on demand.
/// </summary>
public class StatisticsAggregator
{
    private readonly SimulationConfig _config;

    private double _totalWagered;
    private double _totalPaid;
    private double _maxWin;
    private long _winningSpins;
    private long _bonusTriggers;
    private long _totalSpins;

    // For volatility: track sum of squares of win amounts
    private double _winSumOfSquares;
    private double _winSum;

    // Win distribution buckets (as multiplier of bet)
    private readonly Dictionary<string, long> _winDistribution = new()
    {
        ["0x"] = 0,
        ["0.01x-0.99x"] = 0,
        ["1x-4.99x"] = 0,
        ["5x-19.99x"] = 0,
        ["20x-99.99x"] = 0,
        ["100x-499.99x"] = 0,
        ["500x+"] = 0
    };

    public StatisticsAggregator(SimulationConfig config)
    {
        _config = config;
    }

    public void Record(double bet, double win, bool isBonus)
    {
        _totalSpins++;
        _totalWagered += bet;
        _totalPaid += win;

        if (win > _maxWin) _maxWin = win;
        if (win > 0) _winningSpins++;
        if (isBonus) _bonusTriggers++;

        _winSum += win;
        _winSumOfSquares += win * win;

        // Bucket by win multiplier relative to total bet
        double multiplier = bet > 0 ? win / bet : 0;
        if (multiplier == 0) _winDistribution["0x"]++;
        else if (multiplier < 1) _winDistribution["0.01x-0.99x"]++;
        else if (multiplier < 5) _winDistribution["1x-4.99x"]++;
        else if (multiplier < 20) _winDistribution["5x-19.99x"]++;
        else if (multiplier < 100) _winDistribution["20x-99.99x"]++;
        else if (multiplier < 500) _winDistribution["100x-499.99x"]++;
        else _winDistribution["500x+"]++;
    }

    public SimulationReport BuildReport(string gameId, long spinCount, double theoreticalRtp, double tolerancePct)
    {
        double simulatedRtp = _totalWagered > 0 ? _totalPaid / _totalWagered : 0;
        double delta = Math.Abs(simulatedRtp - theoreticalRtp);

        // Volatility index: std deviation of win amounts normalised by bet per spin
        double betPerSpin = _totalSpins > 0 ? _totalWagered / _totalSpins : 1;
        double mean = _totalSpins > 0 ? _winSum / _totalSpins : 0;
        double variance = _totalSpins > 0 ? (_winSumOfSquares / _totalSpins) - (mean * mean) : 0;
        double stdDev = variance > 0 ? Math.Sqrt(variance) : 0;
        double volatilityIndex = betPerSpin > 0 ? stdDev / betPerSpin : 0;

        return new SimulationReport
        {
            GameId = gameId,
            TotalSpins = spinCount,
            TotalWagered = Math.Round(_totalWagered, 2),
            TotalPaid = Math.Round(_totalPaid, 2),
            SimulatedRtp = Math.Round(simulatedRtp, 5),
            TheoreticalRtp = Math.Round(theoreticalRtp, 5),
            RtpDelta = Math.Round(delta, 5),
            HitFrequency = _totalSpins > 0 ? Math.Round((double)_winningSpins / _totalSpins, 4) : 0,
            AverageWin = _winningSpins > 0 ? Math.Round(_totalPaid / _winningSpins, 3) : 0,
            MaxWin = _maxWin,
            VolatilityIndex = Math.Round(volatilityIndex, 2),
            BonusTriggerFrequency = _totalSpins > 0 ? Math.Round((double)_bonusTriggers / _totalSpins, 5) : 0,
            ConvergenceStatus = delta <= tolerancePct ? "PASS" : "FAIL",
            WinDistribution = new Dictionary<string, long>(_winDistribution)
        };
    }
}
