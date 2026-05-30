namespace MS21TakeoffCalculator.Models;

public sealed class WindSlopeCorrection
{
    public int RunwayLengthM { get; set; }
    public decimal HeadwindPerKtM { get; set; }
    public decimal DownSlopePerPercentM { get; set; }
    public decimal UpSlopePerPercentM { get; set; }
}
