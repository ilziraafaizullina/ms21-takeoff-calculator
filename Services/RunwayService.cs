using MS21TakeoffCalculator.Models;

namespace MS21TakeoffCalculator.Services;

public sealed class RunwayService
{
    private readonly IReadOnlyList<Runway> _runways;

    public RunwayService(IReadOnlyList<Runway> runways)
    {
        _runways = runways;
    }

    public IReadOnlyList<Runway> GetByIcao(string icao)
    {
        if (string.IsNullOrWhiteSpace(icao))
        {
            return [];
        }

        return _runways
            .Where(runway => string.Equals(runway.ICAO, icao.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
