using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WindowsEdgeLight;

public partial class MainWindow : Window
{
        public int CurrentColorTemperature => currentColorTemperature;
    // Color temperature properties
    private int currentColorTemperature = 4000; // Kelvin
    private const int MinColorTemp = 2700;
    private const int MaxColorTemp = 6500;
    private const int ColorTempStep = 200;
    private bool isLightOn = true;
    private double currentOpacity = 1.0;  // Full brightness by default
    private const double OpacityStep = 0.15;
    private const double MinOpacity = 0.2;
    private const double MaxOpacity = 1.0;
    
    private NotifyIcon? notifyIcon;
    private ControlWindow? controlWindow;

    // Monitor management
    private int currentMonitorIndex = 0;
    private Screen[] availableMonitors = Array.Empty<Screen>();

    // Global hotkey IDs
    private const int HOTKEY_TOGGLE = 1;
    private const int HOTKEY_BRIGHTNESS_UP = 2;
    private const int HOTKEY_BRIGHTNESS_DOWN = 3;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint VK_L = 0x4C;
    private const uint VK_UP = 0x26;
    private const uint VK_DOWN = 0x28;

    public MainWindow()
    {
        InitializeComponent();
        SetupNotifyIcon();
    }

    private void SetupNotifyIcon()
    {
        notifyIcon = new NotifyIcon();
        
        // Load icon from embedded resource or file
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "ringlight_cropped.ico");
            if (File.Exists(iconPath))
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
                else
                {
                    // Try application icon from exe
                    // Prefer Environment.ProcessPath; fall back to AppContext.BaseDirectory + friendly name
                    string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, AppDomain.CurrentDomain.FriendlyName ?? "");
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                        notifyIcon.Icon = appIcon ?? System.Drawing.SystemIcons.Application;
                    }
                    else
                    {
                        notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
        }
        catch (Exception)
        {
            // Fallback to default icon if loading fails
            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }
        
        notifyIcon.Text = "Windows Edge Light - Right-click for options";
        notifyIcon.Visible = true;
        
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("📋 Keyboard Shortcuts", null, (s, e) => ShowHelp());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("💡 Toggle Light (Ctrl+Shift+L)", null, (s, e) => ToggleLight());
        contextMenu.Items.Add("🔆 Brightness Up (Ctrl+Shift+↑)", null, (s, e) => IncreaseBrightness());
        contextMenu.Items.Add("🔅 Brightness Down (Ctrl+Shift+↓)", null, (s, e) => DecreaseBrightness());
        contextMenu.Items.Add("❄️ Cooler", null, (s, e) => IncreaseColorTemperature());
        contextMenu.Items.Add("🔥 Warmer", null, (s, e) => DecreaseColorTemperature());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("✖ Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());
        
        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.DoubleClick += (s, e) => ShowHelp();
    }

    private void ShowHelp()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "Unknown";
        
        var helpMessage = $@"Windows Edge Light - Keyboard Shortcuts

💡 Toggle Light:  Ctrl + Shift + L
🔆 Brightness Up:  Ctrl + Shift + ↑
🔅 Brightness Down:  Ctrl + Shift + ↓

💡 Features:
• Click-through overlay - won't interfere with your work
• Global hotkeys work from any application
• Right-click taskbar icon for menu

Created by Scott Hanselman
Version {version}";

        System.Windows.MessageBox.Show(helpMessage, "Windows Edge Light - Help", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SetupWindow()
    {
        // Initialize available monitors on first setup
        if (availableMonitors.Length == 0)
        {
            availableMonitors = Screen.AllScreens;
            
            // Find the primary monitor index
            for (int i = 0; i < availableMonitors.Length; i++)
            {
                if (availableMonitors[i].Primary)
                {
                    currentMonitorIndex = i;
                    break;
                }
            }
        }

        var targetScreen = availableMonitors.Length > 0 ? availableMonitors[currentMonitorIndex] : Screen.PrimaryScreen;
        if (targetScreen == null) return;

        SetupWindowForScreen(targetScreen);
    }

    private void SetupWindowForScreen(Screen screen)
    {
        // Use WorkingArea instead of Bounds to exclude taskbar
        var workingArea = screen.WorkingArea;
        
        // Get DPI scale factor
        var source = PresentationSource.FromVisual(this);
        double dpiScaleX = 1.0;
        double dpiScaleY = 1.0;
        
        if (source != null)
        {
            dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }
        
        // Convert physical pixels to WPF DIPs
        this.Left = workingArea.X / dpiScaleX;
        this.Top = workingArea.Y / dpiScaleY;
        this.Width = workingArea.Width / dpiScaleX;
        this.Height = workingArea.Height / dpiScaleY;
        this.WindowState = System.Windows.WindowState.Normal;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetupWindow();
        CreateFrameGeometry();
        CreateControlWindow();
        
        var hwnd = new WindowInteropHelper(this).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        
        // Register global hotkeys
        RegisterHotKey(hwnd, HOTKEY_TOGGLE, MOD_CONTROL | MOD_SHIFT, VK_L);
        RegisterHotKey(hwnd, HOTKEY_BRIGHTNESS_UP, MOD_CONTROL | MOD_SHIFT, VK_UP);
        RegisterHotKey(hwnd, HOTKEY_BRIGHTNESS_DOWN, MOD_CONTROL | MOD_SHIFT, VK_DOWN);
        
        // Hook into Windows message processing
        HwndSource source = HwndSource.FromHwnd(hwnd);
        source.AddHook(HwndHook);
        
        // Listen for window size/location changes (docking/undocking)
        this.SizeChanged += Window_SizeChanged;
        this.LocationChanged += Window_LocationChanged;

        // No persisted settings: keep default color temperature
        currentColorTemperature = Math.Clamp(currentColorTemperature, MinColorTemp, MaxColorTemp);

        // Apply the color temperature to the edge light
        UpdateEdgeLightColor();
        controlWindow?.UpdateColorTemperatureDisplay();
    }

    private void CreateControlWindow()
    {
        controlWindow = new ControlWindow(this);
        RepositionControlWindow();
        controlWindow.Show();
    }

    private void CreateFrameGeometry()
    {
        // Get actual dimensions (accounting for margin)
        double width = this.ActualWidth - 40;  // 20px margin on each side
        double height = this.ActualHeight - 40;
        
        const double frameThickness = 80;
        const double outerRadius = 100;  // Extra rounded like macOS
        const double innerRadius = 60;   // Keep proportional
        
        // Outer rounded rectangle
        var outerRect = new RectangleGeometry(new Rect(0, 0, width, height), outerRadius, outerRadius);
        
        // Inner rounded rectangle
        var innerRect = new RectangleGeometry(
            new Rect(frameThickness, frameThickness, 
                    width - (frameThickness * 2), 
                    height - (frameThickness * 2)), 
            innerRadius, innerRadius);
        
        // Combine: outer minus inner = frame
        var frameGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, outerRect, innerRect);
        
        EdgeLightBorder.Data = frameGeometry;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        
        if (msg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            
            switch (hotkeyId)
            {
                case HOTKEY_TOGGLE:
                    ToggleLight();
                    handled = true;
                    break;
                case HOTKEY_BRIGHTNESS_UP:
                    IncreaseBrightness();
                    handled = true;
                    break;
                case HOTKEY_BRIGHTNESS_DOWN:
                    DecreaseBrightness();
                    handled = true;
                    break;
            }
        }
        
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_TOGGLE);
        UnregisterHotKey(hwnd, HOTKEY_BRIGHTNESS_UP);
        UnregisterHotKey(hwnd, HOTKEY_BRIGHTNESS_DOWN);
        
        if (notifyIcon != null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
        
        controlWindow?.Close();
        
        base.OnClosed(e);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.L && 
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && 
            (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            ToggleLight();
        }
        else if (e.Key == Key.Escape)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        ToggleLight();
    }

    private void ToggleLight()
    {
        isLightOn = !isLightOn;
        EdgeLightBorder.Visibility = isLightOn ? Visibility.Visible : Visibility.Collapsed;
    }

    public void HandleToggle()
    {
        ToggleLight();
    }

    public void IncreaseBrightness()
    {
        currentOpacity = Math.Min(MaxOpacity, currentOpacity + OpacityStep);
        EdgeLightBorder.Opacity = currentOpacity;
    }

    public void DecreaseBrightness()
    {
        currentOpacity = Math.Max(MinOpacity, currentOpacity - OpacityStep);
        EdgeLightBorder.Opacity = currentOpacity;
    }

    public void MoveToNextMonitor()
    {
        // Refresh monitor list in case of hot-plug/unplug
        availableMonitors = Screen.AllScreens;

        if (availableMonitors.Length <= 1)
        {
            // Only one monitor, nothing to do
            return;
        }

        // Bounds check: if current monitor no longer exists, reset to primary
        if (currentMonitorIndex >= availableMonitors.Length)
        {
            // Find primary monitor again
            currentMonitorIndex = 0;
            for (int i = 0; i < availableMonitors.Length; i++)
            {
                if (availableMonitors[i].Primary)
                {
                    currentMonitorIndex = i;
                    break;
                }
            }
        }

        // Cycle to next monitor
        currentMonitorIndex = (currentMonitorIndex + 1) % availableMonitors.Length;
        var targetScreen = availableMonitors[currentMonitorIndex];

        // Reposition main window to new monitor
        SetupWindowForScreen(targetScreen);
        
        // Recreate the frame geometry for new dimensions
        CreateFrameGeometry();
        
        // Reposition control window to follow
        RepositionControlWindow();
    }

    /// <summary>
    /// Increase the color temperature (more blue/cool).
    /// </summary>
    public void IncreaseColorTemperature()
    {
        currentColorTemperature = Math.Min(MaxColorTemp, currentColorTemperature + ColorTempStep);
        UpdateEdgeLightColor();
        controlWindow?.UpdateColorTemperatureDisplay();
    }

    /// <summary>
    /// Decrease the color temperature (more warm/yellow).
    /// </summary>
    public void DecreaseColorTemperature()
    {
        currentColorTemperature = Math.Max(MinColorTemp, currentColorTemperature - ColorTempStep);
        UpdateEdgeLightColor();
        controlWindow?.UpdateColorTemperatureDisplay();
    }

    private void UpdateEdgeLightColor()
    {
        try
        {
            var color = ColorFromKelvin(currentColorTemperature);
            // Create a few tint variations for gradient stops
            var stop0 = LerpColors(color, Colors.White, 0.85);
            var stop1 = LerpColors(color, Colors.White, 0.6);
            var stop2 = LerpColors(color, Colors.White, 0.75);
            var stop3 = LerpColors(color, Colors.White, 0.6);
            var stop4 = LerpColors(color, Colors.White, 0.85);

            if (EdgeLightBrush != null && EdgeLightBrush.GradientStops.Count >= 5)
            {
                EdgeLightBrush.GradientStops[0].Color = stop0;
                EdgeLightBrush.GradientStops[1].Color = stop1;
                EdgeLightBrush.GradientStops[2].Color = stop2;
                EdgeLightBrush.GradientStops[3].Color = stop3;
                EdgeLightBrush.GradientStops[4].Color = stop4;
            }

            // Update shadow color as well (slightly diluted)
            if (EdgeLightShadow != null)
            {
                var shadowColor = LerpColors(color, Colors.White, 0.6);
                EdgeLightShadow.Color = shadowColor;
            }
        }
        catch
        {
            // ignore issues
        }
    }

    private static System.Windows.Media.Color LerpColors(System.Windows.Media.Color a, System.Windows.Media.Color b, double t)
    {
        byte r = (byte)(a.R + (b.R - a.R) * t);
        byte g = (byte)(a.G + (b.G - a.G) * t);
        byte bl = (byte)(a.B + (b.B - a.B) * t);
        return System.Windows.Media.Color.FromRgb(r, g, bl);
    }

    private static System.Windows.Media.Color ColorFromKelvin(int kelvin)
    {
        // Algorithm based on approximation of black-body radiation
        double temp = kelvin / 100.0;
        double r, g, b;

        // Red
        if (temp <= 66)
        {
            r = 255;
        }
        else
        {
            r = temp - 60;
            r = 329.698727446 * Math.Pow(r, -0.1332047592);
            r = Math.Clamp(r, 0, 255);
        }

        // Green
        if (temp <= 66)
        {
            g = 99.4708025861 * Math.Log(temp) - 161.1195681661;
            g = Math.Clamp(g, 0, 255);
        }
        else
        {
            g = temp - 60;
            g = 288.1221695283 * Math.Pow(g, -0.0755148492);
            g = Math.Clamp(g, 0, 255);
        }

        // Blue
        if (temp >= 66)
        {
            b = 255;
        }
        else if (temp <= 19)
        {
            b = 0;
        }
        else
        {
            b = temp - 10;
            b = 138.5177312231 * Math.Log(b) - 305.0447927307;
            b = Math.Clamp(b, 0, 255);
        }

        return System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
    }

    private void RepositionControlWindow()
    {
        if (controlWindow == null) return;

        // Position at bottom center of main window
        controlWindow.Left = this.Left + (this.Width - controlWindow.Width) / 2;
        controlWindow.Top = this.Top + this.Height - controlWindow.Height - 124;
    }

    public bool HasMultipleMonitors()
    {
        // Refresh monitor count to handle hot-plug scenarios
        availableMonitors = Screen.AllScreens;
        return availableMonitors.Length > 1;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Recreate geometry when window size changes (e.g., different monitor resolution)
        if (EdgeLightBorder != null)
        {
            CreateFrameGeometry();
        }
        
        // Reposition control window
        RepositionControlWindow();
        
        // Update which monitor we're actually on
        UpdateCurrentMonitorIndex();
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        // Reposition control window when main window moves
        RepositionControlWindow();
        
        // Update which monitor we're actually on
        UpdateCurrentMonitorIndex();
    }

    private void UpdateCurrentMonitorIndex()
    {
        // Refresh monitor list
        availableMonitors = Screen.AllScreens;
        
        // Figure out which monitor we're actually on now
        var windowCenter = new System.Drawing.Point(
            (int)(this.Left + this.Width / 2),
            (int)(this.Top + this.Height / 2)
        );
        
        for (int i = 0; i < availableMonitors.Length; i++)
        {
            if (availableMonitors[i].Bounds.Contains(windowCenter))
            {
                currentMonitorIndex = i;
                break;
            }
        }
    }

    private void BrightnessUp_Click(object sender, RoutedEventArgs e)
    {
        IncreaseBrightness();
    }

    private void BrightnessDown_Click(object sender, RoutedEventArgs e)
    {
        DecreaseBrightness();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}