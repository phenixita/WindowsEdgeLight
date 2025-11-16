using System.Windows;

namespace WindowsEdgeLight;

public partial class ControlWindow : Window
{
    private readonly MainWindow mainWindow;

    public ControlWindow(MainWindow main)
    {
        InitializeComponent();
        mainWindow = main;
        
        // Disable switch monitor button if only one monitor
        UpdateMonitorButtonState();
        UpdateColorTemperatureDisplay();
    }

    public void UpdateColorTemperatureDisplay()
    {
        try
        {
            ColorTempText.Text = $"{mainWindow.CurrentColorTemperature}K";
        }
        catch
        {
            // ignore
        }
    }

    private void UpdateMonitorButtonState()
    {
        SwitchMonitorButton.IsEnabled = mainWindow.HasMultipleMonitors();
    }

    private void BrightnessDown_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DecreaseBrightness();
    }

    private void BrightnessUp_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.IncreaseBrightness();
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.HandleToggle();
    }

    private void SwitchMonitor_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.MoveToNextMonitor();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void Cooler_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.IncreaseColorTemperature();
        UpdateColorTemperatureDisplay();
    }

    private void Warmer_Click(object sender, RoutedEventArgs e)
    {
        mainWindow.DecreaseColorTemperature();
        UpdateColorTemperatureDisplay();
    }
}
