using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SlotMathEngine.Core.Models;

namespace SlotMathEngine.Core.Output;

public class CsvReportWriter
{
    /// <summary>
    /// Writes spin-level results to a CSV file.
    /// </summary>
    public async Task WriteSpinResultsAsync(IEnumerable<SpinResult> results, string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, config);

        csv.WriteHeader<SpinResultCsvRow>();
        await csv.NextRecordAsync();

        foreach (var result in results)
        {
            csv.WriteRecord(new SpinResultCsvRow
            {
                SpinId = result.SpinId,
                TotalBet = result.TotalBet,
                TotalWin = result.TotalWin,
                IsBonus = result.IsBonusTrigger,
                RunningRtp = result.RunningRtp,
                WinCount = result.PaylineWins.Count
            });
            await csv.NextRecordAsync();
        }
    }

    /// <summary>
    /// Writes a simulation summary report to a JSON file.
    /// </summary>
    public async Task WriteReportJsonAsync(SimulationReport report, string filePath)
    {
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        var json = System.Text.Json.JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(filePath, json);
    }
}

public class SpinResultCsvRow
{
    public long SpinId { get; set; }
    public double TotalBet { get; set; }
    public double TotalWin { get; set; }
    public bool IsBonus { get; set; }
    public double RunningRtp { get; set; }
    public int WinCount { get; set; }
}
