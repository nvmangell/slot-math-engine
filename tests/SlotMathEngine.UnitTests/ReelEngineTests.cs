using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Models;
using SlotMathEngine.UnitTests.TestHelpers;

namespace SlotMathEngine.UnitTests;

public class ReelEngineTests
{
    [Fact]
    public void Spin_ReturnsCorrectDimensions()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var engine = new ReelEngine(config);

        var window = engine.Spin();

        Assert.Equal(config.Reels, window.Count);
        foreach (var reel in window)
            Assert.Equal(config.Rows, reel.Count);
    }

    [Fact]
    public void Spin_OnlyReturnsSymbolsFromReelStrip()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var engine = new ReelEngine(config);
        var allValidSymbols = config.ReelStrips.SelectMany(r => r.Symbols).ToHashSet();

        for (int i = 0; i < 500; i++)
        {
            var window = engine.Spin();
            var allSpun = window.SelectMany(reel => reel);
            foreach (var s in allSpun)
                Assert.Contains(s, allValidSymbols);
        }
    }

    [Fact]
    public void Spin_SymbolFrequencyMatchesReelStripDistribution()
    {
        // Run enough spins so observed frequency converges to expected
        var config = ConfigFactory.SimpleThreeReel();
        var engine = new ReelEngine(config);

        int totalSpins = 100_000;
        int aCount = 0;

        for (int i = 0; i < totalSpins; i++)
        {
            var window = engine.Spin();
            if (window[0][0] == "A") aCount++;
        }

        // Reel 0 has 1 A in 5 symbols → expected ~20%
        double observedFreq = (double)aCount / totalSpins;
        Assert.InRange(observedFreq, 0.18, 0.22);
    }

    [Fact]
    public void Spin_MultipleCallsProduceDifferentResults()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var engine = new ReelEngine(config);

        var results = Enumerable.Range(0, 20)
            .Select(_ => string.Join(",", engine.Spin().Select(r => r[0])))
            .ToList();

        // At least some results should differ (not all identical)
        Assert.True(results.Distinct().Count() > 1);
    }
}
