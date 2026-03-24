using SlotMathEngine.Core.Engine;
using SlotMathEngine.Core.Models;
using SlotMathEngine.UnitTests.TestHelpers;

namespace SlotMathEngine.UnitTests;

public class PaylineEvaluatorTests
{
    private static List<List<string>> MakeWindow(params string[] reelSymbols)
    {
        // Creates a 1-row window where each reel shows exactly one symbol
        return reelSymbols.Select(s => new List<string> { s }).ToList();
    }

    [Fact]
    public void Evaluate_ThreeOfAKind_ReturnsCorrectPayout()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var evaluator = new PaylineEvaluator(config);
        var window = MakeWindow("A", "A", "A");

        var wins = evaluator.Evaluate(window);

        Assert.Single(wins);
        Assert.Equal("A", wins[0].MatchedSymbol);
        Assert.Equal(3, wins[0].MatchCount);
        Assert.Equal(10.0, wins[0].Payout); // payout 10 * betPerLine 1.0
    }

    [Fact]
    public void Evaluate_NoMatch_ReturnsEmpty()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var evaluator = new PaylineEvaluator(config);
        var window = MakeWindow("A", "B", "C");

        var wins = evaluator.Evaluate(window);

        Assert.Empty(wins);
    }

    [Fact]
    public void Evaluate_TwoOfAKind_ReturnsEmpty_WhenMinIsThree()
    {
        var config = ConfigFactory.SimpleThreeReel();
        var evaluator = new PaylineEvaluator(config);
        var window = MakeWindow("A", "A", "B");

        var wins = evaluator.Evaluate(window);

        Assert.Empty(wins);
    }

    [Fact]
    public void Evaluate_WildSubstitutes_ForAnchorSymbol()
    {
        var config = ConfigFactory.SimpleThreeReel();
        // Add WILD to reel strips and paytable for this test
        config.ReelStrips[0].Symbols.Add("WILD");
        config.Symbols.Add(new Symbol { Id = "WILD", Name = "Wild", IsWild = true });

        var evaluator = new PaylineEvaluator(config);
        var window = MakeWindow("WILD", "A", "A");

        var wins = evaluator.Evaluate(window);

        Assert.Single(wins);
        Assert.Equal("A", wins[0].MatchedSymbol);
        Assert.Equal(3, wins[0].MatchCount);
    }

    [Fact]
    public void Evaluate_AllWilds_PayAsWild()
    {
        var config = new SimulationConfig
        {
            GameId = "wild-test",
            Reels = 3,
            Rows = 1,
            BetPerLine = 1.0,
            WildSymbolId = "WILD",
            ScatterSymbolId = "SCATTER",
            Symbols = new List<Symbol> { new() { Id = "WILD", Name = "Wild", IsWild = true } },
            ReelStrips = Enumerable.Range(0, 3)
                .Select(i => new ReelStrip { ReelIndex = i, Symbols = new List<string> { "WILD" } })
                .ToList(),
            Paylines = new List<PaylineDefinition>
            {
                new() { Id = 1, RowPositions = new List<int> { 0, 0, 0 } }
            },
            Paytable = new Paytable
            {
                Payouts = new Dictionary<string, Dictionary<int, double>>
                {
                    ["WILD"] = new() { [3] = 25.0 }
                }
            }
        };

        var evaluator = new PaylineEvaluator(config);
        var window = MakeWindow("WILD", "WILD", "WILD");
        var wins = evaluator.Evaluate(window);

        Assert.Single(wins);
        Assert.Equal("WILD", wins[0].MatchedSymbol);
        Assert.Equal(25.0, wins[0].Payout);
    }

    [Fact]
    public void CountScatters_ReturnsCorrectCount()
    {
        var config = ConfigFactory.SimpleThreeReel();
        config.Symbols.Add(new Symbol { Id = "SCATTER", Name = "Scatter", IsScatter = true });
        var evaluator = new PaylineEvaluator(config);

        // 3-reel, 1-row window with 2 scatters
        var window = new List<List<string>>
        {
            new() { "SCATTER" },
            new() { "A" },
            new() { "SCATTER" }
        };

        Assert.Equal(2, evaluator.CountScatters(window));
    }
}
