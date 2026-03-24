using SlotMathEngine.Core.Engine;
using SlotMathEngine.UnitTests.TestHelpers;

namespace SlotMathEngine.UnitTests;

public class RtpCalculatorTests
{
    [Fact]
    public void Calculate_SimpleConfig_MatchesHandCalculatedRtp()
    {
        // Using ConfigFactory.SimpleThreeReel():
        // Reel strips: each has A(1), B(2), C(2) over 5 stops.
        // P(A) = 1/5, P(B) = 2/5, P(C) = 2/5 per reel.
        //
        // Payouts (per betPerLine = 1.0):
        //   AAA = 10   → P = (1/5)^3 = 1/125  → E = 10/125  = 0.080
        //   BBB = 3    → P = (2/5)^3 = 8/125  → E = 24/125  = 0.192
        //   CCC = 1    → P = (2/5)^3 = 8/125  → E = 8/125   = 0.064
        //
        // Total expected payout = (10 + 24 + 8) / 125 = 42/125 = 0.336
        // Bet per spin = betPerLine(1.0) * paylines(1) = 1.0
        // Theoretical RTP = 0.336 / 1.0 = 33.6%

        var config = ConfigFactory.SimpleThreeReel();
        var calculator = new RtpCalculator(config);

        double rtp = calculator.Calculate();

        Assert.Equal(0.336, rtp, precision: 5);
    }

    [Fact]
    public void Calculate_ReturnsValueBetweenZeroAndOne()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var calculator = new RtpCalculator(config);

        double rtp = calculator.Calculate();

        Assert.InRange(rtp, 0.0, 1.0);
    }

    [Fact]
    public void Calculate_IsReproducible()
    {
        var config = ConfigFactory.SimpleThreeReel();

        double rtp1 = new RtpCalculator(config).Calculate();
        double rtp2 = new RtpCalculator(config).Calculate();

        Assert.Equal(rtp1, rtp2);
    }
}
