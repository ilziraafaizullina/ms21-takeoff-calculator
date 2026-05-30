using System.Windows;
using MS21TakeoffCalculator.Services;
using MS21TakeoffCalculator.ViewModels;

namespace MS21TakeoffCalculator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var messageBoxService = new MessageBoxService();

        try
        {
            var settings = new AppSettingsService().Load();
            var csvLoader = new CsvLoaderService();
            var data = new ApplicationDataService(csvLoader, settings).Load();
            var fileSelectionService = new FileSelectionService();

            DataContext = new MainViewModel(
                new AirportService(data.Airports),
                new RunwayService(data.Runways),
                new PerformanceCalculationService(data.SpeedTables, data.ThsTables, fileSelectionService),
                messageBoxService);
        }
        catch (Exception exception)
        {
            messageBoxService.Show($"CSV read error: {exception.Message}");
            Close();
        }
    }
}
