using MS21TakeoffCalculator.Models;

namespace MS21TakeoffCalculator.Services;

public sealed class PerformanceCalculationService
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SpeedRecord>> _speedTables;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ThsRecord>> _thsTables;
    private readonly FileSelectionService _fileSelectionService;

    public PerformanceCalculationService(
        IReadOnlyDictionary<string, IReadOnlyList<SpeedRecord>> speedTables,
        IReadOnlyDictionary<string, IReadOnlyList<ThsRecord>> thsTables,
        FileSelectionService fileSelectionService)
    {
        _speedTables = speedTables;
        _thsTables = thsTables;
        _fileSelectionService = fileSelectionService;
    }

    public CalculationResult? Calculate(CalculationInput input)
    {
        var normalizedOat = Math.Round(input.OatC, 0, MidpointRounding.AwayFromZero);
        var normalizedTow = Math.Round(input.TowT, 1, MidpointRounding.AwayFromZero);
        var normalizedCg = Math.Round(input.Cg, 1, MidpointRounding.AwayFromZero);
        var pressureAltitude = NormalizePressureAltitude(input.QnhHpa);

        var speedFile = _fileSelectionService.GetSpeedFileName(input.Configuration, pressureAltitude);
        var thsFile = _fileSelectionService.GetThsFileName(input.Configuration);

        if (!_speedTables.TryGetValue(speedFile, out var speedRecords) ||
            !_thsTables.TryGetValue(thsFile, out var thsRecords))
        {
            return null;
        }

        var speed = speedRecords.FirstOrDefault(record =>
            record.OATC == normalizedOat &&
            Math.Round(record.MaxTakeoffWeightT, 1, MidpointRounding.AwayFromZero) == normalizedTow);

        var ths = thsRecords.FirstOrDefault(record => record.WeightT == normalizedTow);

        if (speed is null ||
            ths is null ||
            !ths.ValuesByCg.TryGetValue(normalizedCg, out var thsValue))
        {
            return null;
        }

        return new CalculationResult
        {
            V1 = speed.V1Kt,
            VR = speed.VRKt,
            V2 = speed.V2Kt,
            THS = Math.Round(thsValue, 2, MidpointRounding.AwayFromZero)
        };
    }

    private static int NormalizePressureAltitude(decimal qnhHpa)
    {
        var rounded = (int)Math.Round(qnhHpa / 1000m, 0, MidpointRounding.AwayFromZero) * 1000;
        return Math.Clamp(rounded, 0, 2000);
    }
}
