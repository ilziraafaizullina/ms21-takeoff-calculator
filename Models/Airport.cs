namespace MS21TakeoffCalculator.Models;

public sealed class Airport
{
    public string ICAO { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ElevationM { get; set; }
}
