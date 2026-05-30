using MS21TakeoffCalculator.Models;

namespace MS21TakeoffCalculator.Services;

public sealed class PerformanceCalculationService
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SpeedRecord>> _speedTables;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ThsRecord>> _thsTables;
    private readonly IReadOnlyList<WindSlopeCorrection> _windSlopeCorrections;
    private readonly FileSelectionService _fileSelectionService;

    public PerformanceCalculationService(
        IReadOnlyDictionary<string, IReadOnlyList<SpeedRecord>> speedTables,
        IReadOnlyDictionary<string, IReadOnlyList<ThsRecord>> thsTables,
        IReadOnlyList<WindSlopeCorrection> windSlopeCorrections,
        FileSelectionService fileSelectionService)
    {
        _speedTables = speedTables;
        _thsTables = thsTables;
        _windSlopeCorrections = windSlopeCorrections;
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

        var correctedRunwayLength = CalculateCorrectedRunwayLength(
            input.RunwayLengthM,
            input.RunwaySlopePercent,
            input.WindKt);
        var speedRunwayLength = FindNearestSpeedRunwayLength(speedRecords, correctedRunwayLength);

        var speed = speedRecords.FirstOrDefault(record =>
            record.OATC == normalizedOat &&
            record.CorrectedRunwayLengthM == speedRunwayLength &&
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

    public IReadOnlyList<decimal> GetAvailableOats(string? configuration, string? qnh)
    {
        if (!TryGetSpeedRecords(configuration, qnh, out var speedRecords))
        {
            return [];
        }

        return speedRecords
            .Select(record => Math.Round(record.OATC, 0, MidpointRounding.AwayFromZero))
            .Distinct()
            .Order()
            .ToList();
    }

    public IReadOnlyList<decimal> GetAvailableTows(
        string? configuration,
        string? qnh,
        string? oat,
        int? runwayLengthM,
        decimal? runwaySlopePercent,
        decimal? windKt)
    {
        if (!TryGetSpeedRecords(configuration, qnh, out var speedRecords) ||
            !decimal.TryParse(oat, out var selectedOat) ||
            runwayLengthM is null ||
            runwaySlopePercent is null ||
            windKt is null)
        {
            return [];
        }

        var thsWeights = GetAvailableThsWeights(configuration).ToHashSet();
        var correctedRunwayLength = CalculateCorrectedRunwayLength(
            runwayLengthM.Value,
            runwaySlopePercent.Value,
            windKt.Value);
        var speedRunwayLength = FindNearestSpeedRunwayLength(speedRecords, correctedRunwayLength);

        return speedRecords
            .Where(record =>
                Math.Round(record.OATC, 0, MidpointRounding.AwayFromZero) == selectedOat &&
                record.CorrectedRunwayLengthM == speedRunwayLength)
            .Select(record => Math.Round(record.MaxTakeoffWeightT, 1, MidpointRounding.AwayFromZero))
            .Where(tow => thsWeights.Contains(tow))
            .Distinct()
            .Order()
            .ToList();
    }

    public IReadOnlyList<decimal> GetAvailableCgs(string? configuration)
    {
        if (!TryGetThsRecords(configuration, out var thsRecords))
        {
            return [];
        }

        return thsRecords
            .SelectMany(record => record.ValuesByCg.Keys)
            .Distinct()
            .Order()
            .ToList();
    }

    private IReadOnlyList<decimal> GetAvailableThsWeights(string? configuration)
    {
        if (!TryGetThsRecords(configuration, out var thsRecords))
        {
            return [];
        }

        return thsRecords
            .Select(record => record.WeightT)
            .Distinct()
            .ToList();
    }

    private bool TryGetSpeedRecords(string? configuration, string? qnh, out IReadOnlyList<SpeedRecord> speedRecords)
    {
        speedRecords = [];

        if (string.IsNullOrWhiteSpace(configuration) ||
            !decimal.TryParse(qnh, out var qnhValue))
        {
            return false;
        }

        var pressureAltitude = NormalizePressureAltitude(qnhValue);
        var speedFile = _fileSelectionService.GetSpeedFileName(configuration, pressureAltitude);
        if (!_speedTables.TryGetValue(speedFile, out var records))
        {
            return false;
        }

        speedRecords = records;
        return true;
    }

    private bool TryGetThsRecords(string? configuration, out IReadOnlyList<ThsRecord> thsRecords)
    {
        thsRecords = [];

        if (string.IsNullOrWhiteSpace(configuration))
        {
            return false;
        }

        var thsFile = _fileSelectionService.GetThsFileName(configuration);
        if (!_thsTables.TryGetValue(thsFile, out var records))
        {
            return false;
        }

        thsRecords = records;
        return true;
    }

    public decimal CalculateCorrectedRunwayLength(int runwayLengthM, decimal slopePercent, decimal headwindKt)
    {
        var correction = FindNearestWindSlopeCorrection(runwayLengthM);
        var windCorrection = headwindKt * correction.HeadwindPerKtM;
        var slopeCorrection = slopePercent >= 0
            ? slopePercent * correction.UpSlopePerPercentM
            : Math.Abs(slopePercent) * correction.DownSlopePerPercentM;

        return slopePercent >= 0
            ? runwayLengthM + windCorrection - slopeCorrection
            : runwayLengthM + windCorrection + slopeCorrection;
    }

    private WindSlopeCorrection FindNearestWindSlopeCorrection(int runwayLengthM) =>
        _windSlopeCorrections
            .OrderBy(correction => Math.Abs(correction.RunwayLengthM - runwayLengthM))
            .ThenBy(correction => correction.RunwayLengthM)
            .First();

    private static int FindNearestSpeedRunwayLength(
        IReadOnlyList<SpeedRecord> speedRecords,
        decimal correctedRunwayLength) =>
        speedRecords
            .Select(record => record.CorrectedRunwayLengthM)
            .Distinct()
            .OrderBy(length => Math.Abs(length - correctedRunwayLength))
            .ThenBy(length => length)
            .First();

    private static int NormalizePressureAltitude(decimal qnhHpa)
    {
        var rounded = (int)Math.Round(qnhHpa / 1000m, 0, MidpointRounding.AwayFromZero) * 1000;
        return Math.Clamp(rounded, 0, 2000);
    }
}
