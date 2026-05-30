namespace MS21TakeoffCalculator.Models;

public sealed class CalculationInput
{
    public string ICAO { get; init; } = string.Empty;
    public string Runway { get; init; } = string.Empty;
    public int RunwayLengthM { get; init; }
    public decimal RunwaySlopePercent { get; init; }
    public decimal WindKt { get; init; }
    public decimal OatC { get; init; }
    public decimal QnhHpa { get; init; }
    public decimal TowT { get; init; }
    public decimal Cg { get; init; }
    public string Configuration { get; init; } = string.Empty;
}
