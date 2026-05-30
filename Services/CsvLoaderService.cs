using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using MS21TakeoffCalculator.Models;

namespace MS21TakeoffCalculator.Services;

public sealed class CsvLoaderService
{
    private readonly CsvConfiguration _configuration = new(CultureInfo.InvariantCulture)
    {
        BadDataFound = null,
        HeaderValidated = null,
        MissingFieldFound = null,
        TrimOptions = TrimOptions.Trim
    };

    public IReadOnlyList<Airport> LoadAirports(string path) =>
        LoadRecords<Airport>(path);

    public IReadOnlyList<Runway> LoadRunways(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, _configuration, false);
        csv.Context.RegisterClassMap<RunwayMap>();
        return csv.GetRecords<Runway>().ToList();
    }

    public IReadOnlyList<SpeedRecord> LoadSpeedRecords(string path) =>
        LoadRecords<SpeedRecord>(path);

    public IReadOnlyList<WindSlopeCorrection> LoadWindSlopeCorrections(string path) =>
        LoadRecords<WindSlopeCorrection>(path);

    public void EnsureReadable(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, _configuration, false);
        while (csv.Read())
        {
        }
    }

    public IReadOnlyList<ThsRecord> LoadThsRecords(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, _configuration, false);
        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? [];
        var cgColumns = headers
            .Where(header => header.StartsWith("CG", StringComparison.OrdinalIgnoreCase))
            .Select(header => new
            {
                Header = header,
                Cg = decimal.Parse(header[2..], CultureInfo.InvariantCulture)
            })
            .ToList();

        var records = new List<ThsRecord>();
        while (csv.Read())
        {
            var weight = csv.GetField<decimal>("WeightT");
            var values = new Dictionary<decimal, decimal>();

            foreach (var column in cgColumns)
            {
                if (csv.TryGetField<decimal>(column.Header, out var value))
                {
                    values[column.Cg] = value;
                }
            }

            records.Add(new ThsRecord
            {
                WeightT = weight,
                ValuesByCg = values
            });
        }

        return records;
    }

    private IReadOnlyList<T> LoadRecords<T>(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, _configuration, false);
        return csv.GetRecords<T>().ToList();
    }

    private sealed class RunwayMap : ClassMap<Runway>
    {
        public RunwayMap()
        {
            Map(x => x.ICAO);
            Map(x => x.RunwayName).Name("Runway");
            Map(x => x.LengthM);
            Map(x => x.SlopePercent);
        }
    }
}
