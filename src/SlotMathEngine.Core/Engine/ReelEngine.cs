using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Engine;

/// <summary>
/// Draws a visible window of symbols from each reel strip using a thread-local
/// Random instance for thread safety during parallel simulation runs.
/// </summary>
public class ReelEngine
{
    private readonly SimulationConfig _config;

    [ThreadStatic]
    private static Random? _rng;

    private static Random Rng => _rng ??= new Random(Guid.NewGuid().GetHashCode());

    public ReelEngine(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Spins all reels and returns the visible window.
    /// Returns a list of reel columns, each containing <see cref="SimulationConfig.Rows"/> symbols.
    /// </summary>
    public List<List<string>> Spin()
    {
        var window = new List<List<string>>(_config.Reels);

        for (int r = 0; r < _config.Reels; r++)
        {
            var strip = _config.ReelStrips[r];
            int stopPosition = Rng.Next(0, strip.Symbols.Count);

            var column = new List<string>(_config.Rows);
            for (int row = 0; row < _config.Rows; row++)
            {
                int index = (stopPosition + row) % strip.Symbols.Count;
                column.Add(strip.Symbols[index]);
            }
            window.Add(column);
        }

        return window;
    }
}
