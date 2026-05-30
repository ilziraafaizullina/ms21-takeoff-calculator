using MS21TakeoffCalculator.Models;
using System.IO;

namespace MS21TakeoffCalculator.Services;

public sealed class ApplicationDataService
{
    private static readonly string[] RawReferenceFiles =
    [
        "speed_1f_0ft.csv",
        "speed_1f_1000ft.csv",
        "speed_1f_2000ft.csv",
        "speed_2_0ft.csv",
        "speed_2_1000ft.csv",
        "speed_2_2000ft.csv",
        "ths_flaps_1f.csv",
        "ths_flaps_2.csv"
    ];

    private static readonly string[] SpeedFiles =
    [
        "speed_1f_0ft_prepared.csv",
        "speed_1f_1000ft_prepared.csv",
        "speed_1f_2000ft_prepared.csv",
        "speed_2_0ft_prepared.csv",
        "speed_2_1000ft_prepared.csv",
        "speed_2_2000ft_prepared.csv"
    ];

    private static readonly string[] ThsFiles =
    [
        "ths_flaps_1f_prepared.csv",
        "ths_flaps_2_prepared.csv"
    ];

    private readonly CsvLoaderService _csvLoaderService;
    private readonly AppSettings _settings;

    public ApplicationDataService(CsvLoaderService csvLoaderService, AppSettings settings)
    {
        _csvLoaderService = csvLoaderService;
        _settings = settings;
    }

    public ApplicationData Load()
    {
        var dataRoot = ResolveDataRoot(_settings.DataDirectory);
        var rawRoot = Path.Combine(dataRoot, "raw");
        var preparedRoot = Path.Combine(dataRoot, "prepared");

        var airports = _csvLoaderService.LoadAirports(Path.Combine(rawRoot, "airports.csv"));
        var runways = _csvLoaderService.LoadRunways(Path.Combine(rawRoot, "runways.csv"));
        var windSlopeCorrections = _csvLoaderService.LoadWindSlopeCorrections(Path.Combine(rawRoot, "wind_slope_corrections.csv"));

        foreach (var fileName in RawReferenceFiles)
        {
            _csvLoaderService.EnsureReadable(Path.Combine(rawRoot, fileName));
        }

        var speedTables = SpeedFiles.ToDictionary(
            fileName => fileName,
            fileName => _csvLoaderService.LoadSpeedRecords(Path.Combine(preparedRoot, fileName)),
            StringComparer.OrdinalIgnoreCase);

        var thsTables = ThsFiles.ToDictionary(
            fileName => fileName,
            fileName => _csvLoaderService.LoadThsRecords(Path.Combine(preparedRoot, fileName)),
            StringComparer.OrdinalIgnoreCase);

        return new ApplicationData(airports, runways, windSlopeCorrections, speedTables, thsTables);
    }

    private static string ResolveDataRoot(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            if (Directory.Exists(configuredPath))
            {
                return configuredPath;
            }

            throw new DirectoryNotFoundException($"Configured data directory was not found: {configuredPath}");
        }

        var baseData = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        if (Directory.Exists(baseData))
        {
            return baseData;
        }

        var currentData = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configuredPath));
        if (Directory.Exists(currentData))
        {
            return currentData;
        }

        throw new DirectoryNotFoundException($"Data directory was not found: {configuredPath}");
    }
}

public sealed record ApplicationData(
    IReadOnlyList<Airport> Airports,
    IReadOnlyList<Runway> Runways,
    IReadOnlyList<WindSlopeCorrection> WindSlopeCorrections,
    IReadOnlyDictionary<string, IReadOnlyList<SpeedRecord>> SpeedTables,
    IReadOnlyDictionary<string, IReadOnlyList<ThsRecord>> ThsTables);
