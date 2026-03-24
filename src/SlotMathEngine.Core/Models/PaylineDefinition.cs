namespace SlotMathEngine.Core.Models;

/// <summary>
/// A payline is a list of (reel, row) positions to evaluate left-to-right.
/// For a standard 5x3 grid, each payline has 5 positions, one per reel.
/// </summary>
public class PaylineDefinition
{
    public int Id { get; set; }

    /// <summary>
    /// Row index (0-based) to read from each reel, in reel order.
    /// e.g. [1, 1, 1, 1, 1] is the middle line across all 5 reels.
    /// </summary>
    public List<int> RowPositions { get; set; } = new();
}
