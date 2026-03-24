namespace SlotMathEngine.Core.Models;

public class Symbol
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsWild { get; set; }
    public bool IsScatter { get; set; }
}
