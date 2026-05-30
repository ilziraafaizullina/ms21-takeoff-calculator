namespace MS21TakeoffCalculator.Models;

public sealed class ThsRecord
{
    public decimal WeightT { get; init; }
    public IReadOnlyDictionary<decimal, decimal> ValuesByCg { get; init; } = new Dictionary<decimal, decimal>();
}
