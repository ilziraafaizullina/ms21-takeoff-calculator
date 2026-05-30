using System.Windows;

namespace MS21TakeoffCalculator.Services;

public interface IMessageBoxService
{
    void Show(string message);
}

public sealed class MessageBoxService : IMessageBoxService
{
    public void Show(string message) =>
        MessageBox.Show(message, "MS-21 Takeoff Calculator", MessageBoxButton.OK, MessageBoxImage.Information);
}
