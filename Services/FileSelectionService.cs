namespace MS21TakeoffCalculator.Services;

public sealed class FileSelectionService
{
    public const string Flaps1F = "FLAPS 1 + F";
    public const string Flaps2 = "FLAPS 2";

    public string GetSpeedFileName(string configuration, int pressureAltitudeFt)
    {
        var prefix = configuration switch
        {
            Flaps1F => "speed_1f",
            Flaps2 => "speed_2",
            _ => throw new ArgumentException("Unknown aircraft configuration.", nameof(configuration))
        };

        return $"{prefix}_{pressureAltitudeFt}ft_prepared.csv";
    }

    public string GetThsFileName(string configuration) =>
        configuration switch
        {
            Flaps1F => "ths_flaps_1f_prepared.csv",
            Flaps2 => "ths_flaps_2_prepared.csv",
            _ => throw new ArgumentException("Unknown aircraft configuration.", nameof(configuration))
        };
}
