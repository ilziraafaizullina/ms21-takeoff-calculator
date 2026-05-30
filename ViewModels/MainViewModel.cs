using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MS21TakeoffCalculator.Models;
using MS21TakeoffCalculator.Services;

namespace MS21TakeoffCalculator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AirportService _airportService;
    private readonly RunwayService _runwayService;
    private readonly PerformanceCalculationService _calculationService;
    private readonly IMessageBoxService _messageBoxService;

    public MainViewModel(
        AirportService airportService,
        RunwayService runwayService,
        PerformanceCalculationService calculationService,
        IMessageBoxService messageBoxService)
    {
        _airportService = airportService;
        _runwayService = runwayService;
        _calculationService = calculationService;
        _messageBoxService = messageBoxService;

        Configurations =
        [
            FileSelectionService.Flaps1F,
            FileSelectionService.Flaps2
        ];
    }

    public ObservableCollection<Runway> AvailableRunways { get; } = [];

    public IReadOnlyList<string> Configurations { get; }

    [ObservableProperty]
    private string? icao;

    [ObservableProperty]
    private string? airportAbbreviation;

    [ObservableProperty]
    private string? airportName;

    [ObservableProperty]
    private Runway? selectedRunway;

    [ObservableProperty]
    private string? runwayLength;

    [ObservableProperty]
    private string? runwaySlope;

    [ObservableProperty]
    private string? wind;

    [ObservableProperty]
    private string? oat;

    [ObservableProperty]
    private string? qnh;

    [ObservableProperty]
    private string? tow;

    [ObservableProperty]
    private string? cg;

    [ObservableProperty]
    private string? selectedConfiguration;

    [ObservableProperty]
    private string? v1;

    [ObservableProperty]
    private string? vr;

    [ObservableProperty]
    private string? v2;

    [ObservableProperty]
    private string? ths;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isIcaoInvalid;

    partial void OnIcaoChanged(string? value)
    {
        ClearResults();
        AirportAbbreviation = null;
        AirportName = null;
        SelectedRunway = null;
        AvailableRunways.Clear();

        var normalizedIcao = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedIcao))
        {
            IsIcaoInvalid = false;
            StatusMessage = null;
            return;
        }

        var airport = _airportService.FindByIcao(normalizedIcao);
        if (airport is null)
        {
            IsIcaoInvalid = true;
            StatusMessage = "N/A DATA";
            return;
        }

        IsIcaoInvalid = false;
        StatusMessage = null;
        AirportAbbreviation = airport.Abbreviation;
        AirportName = airport.Name;

        foreach (var runway in _runwayService.GetByIcao(normalizedIcao))
        {
            AvailableRunways.Add(runway);
        }
    }

    partial void OnSelectedRunwayChanged(Runway? value)
    {
        ClearResults();
        RunwayLength = value?.LengthM.ToString(CultureInfo.InvariantCulture);
        RunwaySlope = value?.SlopePercent.ToString("0.##", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void Compute()
    {
        if (HasMissingRequiredFields())
        {
            _messageBoxService.Show("All parameters are required.");
            return;
        }

        if (IsIcaoInvalid)
        {
            StatusMessage = "N/A DATA";
            return;
        }

        if (!TryBuildInput(out var input))
        {
            _messageBoxService.Show("All parameters are required.");
            return;
        }

        var result = _calculationService.Calculate(input);
        if (result is null)
        {
            ClearResults();
            _messageBoxService.Show("DATA NOT FOUND");
            return;
        }

        V1 = result.V1.ToString(CultureInfo.InvariantCulture);
        Vr = result.VR.ToString(CultureInfo.InvariantCulture);
        V2 = result.V2.ToString(CultureInfo.InvariantCulture);
        Ths = result.THS.ToString("0.##", CultureInfo.InvariantCulture);
        StatusMessage = null;
    }

    [RelayCommand]
    private void Reset()
    {
        Icao = null;
        AirportAbbreviation = null;
        AirportName = null;
        SelectedRunway = null;
        RunwayLength = null;
        RunwaySlope = null;
        Wind = null;
        Oat = null;
        Qnh = null;
        Tow = null;
        Cg = null;
        SelectedConfiguration = null;
        StatusMessage = null;
        IsIcaoInvalid = false;
        AvailableRunways.Clear();
        ClearResults();
    }

    private bool HasMissingRequiredFields() =>
        string.IsNullOrWhiteSpace(Icao) ||
        SelectedRunway is null ||
        string.IsNullOrWhiteSpace(Wind) ||
        string.IsNullOrWhiteSpace(Oat) ||
        string.IsNullOrWhiteSpace(Qnh) ||
        string.IsNullOrWhiteSpace(Tow) ||
        string.IsNullOrWhiteSpace(Cg) ||
        string.IsNullOrWhiteSpace(SelectedConfiguration);

    private bool TryBuildInput(out CalculationInput input)
    {
        input = new CalculationInput();

        if (!TryParseDecimal(Wind, out var wind) ||
            !TryParseDecimal(Oat, out var oat) ||
            !TryParseDecimal(Qnh, out var qnh) ||
            !TryParseDecimal(Tow, out var tow) ||
            !TryParseDecimal(Cg, out var cg) ||
            SelectedRunway is null ||
            string.IsNullOrWhiteSpace(SelectedConfiguration))
        {
            return false;
        }

        input = new CalculationInput
        {
            ICAO = Icao?.Trim().ToUpperInvariant() ?? string.Empty,
            Runway = SelectedRunway.RunwayName,
            WindKt = wind,
            OatC = oat,
            QnhHpa = qnh,
            TowT = tow,
            Cg = cg,
            Configuration = SelectedConfiguration
        };

        return true;
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
        {
            return true;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private void ClearResults()
    {
        V1 = null;
        Vr = null;
        V2 = null;
        Ths = null;
    }
}
