using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Simulation;
using SlotMathEngine.UnitTests.TestHelpers;

namespace SlotMathEngine.StatisticalTests;

/// <summary>
/// Statistical convergence tests — these are slower by design and validate that
/// simulated RTP converges to the theoretical value at scale.
/// Not run in fast CI; tagged with Trait for selective execution.
/// </summary>
public class RtpConvergenceTests
{
    [Fact]
    [Trait("Category", "Statistical")]
    public void SimulatedRtp_ConvergesToTheoreticalRtp_At1MillionSpins()
    {
        var config = ConfigFactory.SimpleThreeReel();
        double theoreticalRtp = new RtpCalculator(config).Calculate();

        var simulator = new MonteCarloSimulator(config);
        var report = simulator.RunAggregated(1_000_000, theoreticalRtp, convergenceTolerancePct: 0.005);

        // At 1M spins, delta should be well within 0.5%
        Assert.Equal("PASS", report.ConvergenceStatus);
        Assert.InRange(report.RtpDelta, 0, 0.005);
    }

    [Fact]
    [Trait("Category", "Statistical")]
    public void HitFrequency_IsWithinExpectedRange()
    {
        var config = ConfigFactory.SimpleThreeReel();
        double theoreticalRtp = new RtpCalculator(config).Calculate();

        var simulator = new MonteCarloSimulator(config);
        var report = simulator.RunAggregated(500_000, theoreticalRtp);

        // With simple 3-reel config, hit frequency should be >0% and <100%
        Assert.InRange(report.HitFrequency, 0.01, 0.99);
    }

    [Fact]
    [Trait("Category", "Statistical")]
    public void TotalPaid_DivTotalWagered_EqualsSimulatedRtp()
    {
        var config = ConfigFactory.SimpleThreeReel();
        double theoreticalRtp = new RtpCalculator(config).Calculate();

        var simulator = new MonteCarloSimulator(config);
        var report = simulator.RunAggregated(100_000, theoreticalRtp);

        double derived = report.TotalWagered > 0 ? report.TotalPaid / report.TotalWagered : 0;
        Assert.Equal(report.SimulatedRtp, Math.Round(derived, 5));
    }
}
