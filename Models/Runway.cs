namespace MS21TakeoffCalculator.Models;

public sealed class Runway
{
    public string ICAO { get; set; } = string.Empty;
    public string RunwayName { get; set; } = string.Empty;
    public int LengthM { get; set; }
    public decimal SlopePercent { get; set; }

    public override string ToString() => RunwayName;
}
