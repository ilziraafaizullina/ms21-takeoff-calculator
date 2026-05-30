using MS21TakeoffCalculator.Models;

namespace MS21TakeoffCalculator.Services;

public sealed class AirportService
{
    private readonly IReadOnlyDictionary<string, Airport> _airportsByIcao;

    public AirportService(IReadOnlyList<Airport> airports)
    {
        _airportsByIcao = airports.ToDictionary(
            airport => airport.ICAO,
            StringComparer.OrdinalIgnoreCase);
    }

    public Airport? FindByIcao(string icao)
    {
        if (string.IsNullOrWhiteSpace(icao))
        {
            return null;
        }

        return _airportsByIcao.TryGetValue(icao.Trim(), out var airport)
            ? airport
            : null;
    }
}
