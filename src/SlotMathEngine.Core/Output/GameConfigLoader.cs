using System.Text.Json;
using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Output;

/// <summary>
/// Loads a <see cref="SimulationConfig"/> from a JSON file on disk.
/// </summary>
public static class GameConfigLoader
{
    public static async Task<SimulationConfig> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Game config not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<SimulationConfig>(json, options)
            ?? throw new InvalidOperationException($"Failed to deserialize config from {filePath}");

        Validate(config);
        return config;
    }

    private static void Validate(SimulationConfig config)
    {
        if (config.ReelStrips.Count != config.Reels)
            throw new InvalidOperationException($"Expected {config.Reels} reel strips, got {config.ReelStrips.Count}");

        if (config.Paylines.Count == 0)
            throw new InvalidOperationException("At least one payline must be defined");

        foreach (var payline in config.Paylines)
        {
            if (payline.RowPositions.Count != config.Reels)
                throw new InvalidOperationException(
                    $"Payline {payline.Id} has {payline.RowPositions.Count} row positions but game has {config.Reels} reels");
        }
    }
}
