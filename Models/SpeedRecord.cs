using CsvHelper.Configuration.Attributes;

namespace MS21TakeoffCalculator.Models;

public sealed class SpeedRecord
{
    public decimal OATC { get; set; }
    public int CorrectedRunwayLengthM { get; set; }
    public decimal MaxTakeoffWeightT { get; set; }
    public int V1Kt { get; set; }
    public int VRKt { get; set; }
    public int V2Kt { get; set; }
    public int LimitationCode { get; set; }
}
