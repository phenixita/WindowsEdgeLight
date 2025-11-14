# Windows Edge Light

A lightweight WPF application that adds a customizable glowing edge light effect around your primary monitor on Windows. Perfect for ambient lighting during video calls, streaming, or just adding a professional touch to your workspace.

## Features

- **Primary Monitor Display**: Automatically detects and displays on your primary monitor, even in multi-monitor setups
- **DPI Aware**: Properly handles high-DPI displays (4K monitors with scaling)
- **Click-Through Transparency**: Overlay doesn't interfere with your work - all clicks pass through to applications beneath
- **Customizable Brightness**: Adjust opacity with easy-to-use controls
- **Toggle On/Off**: Quickly enable or disable the edge light effect
- **Always On Top**: Stays visible above all other windows
- **Keyboard Shortcuts**: 
  - `Ctrl+Shift+L` - Toggle light on/off
  - `Esc` - Exit application
- **Gradient Effect**: Beautiful white gradient with subtle blur for a professional look

## Screenshots

The application creates a smooth, glowing border around the edges of your primary monitor:

- Adjustable brightness levels (20% to 100% opacity)
- Soft blur effect for a natural glow
- Minimal UI controls that fade in on hover

## Installation

### Prerequisites

- Windows 10 or later
- .NET 8.0 Runtime (Windows Desktop)

### Building from Source

1. Clone this repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/WindowsEdgeLight.git
   cd WindowsEdgeLight
   ```

2. Build the project:
   ```bash
   cd WindowsEdgeLight
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

## Usage

1. Launch `WindowsEdgeLight.exe`
2. The edge light will appear around your primary monitor
3. Hover over the top-right corner to reveal controls:
   - 🔅 **Decrease Brightness** - Reduces opacity
   - 🔆 **Increase Brightness** - Increases opacity
   - 💡 **Toggle Light** - Turn the effect on/off
   - ✖ **Exit** - Close the application

### Keyboard Shortcuts

- **Ctrl+Shift+L**: Toggle the edge light on/off
- **Esc**: Exit the application

## Technical Details

### Architecture

- **Framework**: .NET 8.0 WPF (Windows Presentation Foundation)
- **Language**: C# 
- **UI**: XAML with transparent window overlay
- **Monitor Detection**: Windows Forms Screen API for accurate multi-monitor support

### Key Features Implementation

- **Click-Through**: Uses Win32 `WS_EX_TRANSPARENT` and `WS_EX_LAYERED` window styles
- **DPI Scaling**: Converts physical pixels to WPF Device Independent Pixels for proper sizing
- **Primary Monitor**: Uses `Screen.PrimaryScreen.Bounds` with DPI correction
- **Gradient Border**: Custom Rectangle with LinearGradientBrush and BlurEffect

## Multi-Monitor Support

The application specifically targets the **primary monitor** in your display setup:
- Automatically detects primary monitor position and dimensions
- Correctly handles DPI scaling (e.g., 150%, 200% on 4K displays)
- Works with any monitor arrangement (horizontal, vertical, mixed)
- Does not span across multiple monitors

## Development

### Project Structure

```
WindowsEdgeLight/
├── WindowsEdgeLight/
│   ├── App.xaml              # Application entry point
│   ├── App.xaml.cs
│   ├── MainWindow.xaml       # Main UI layout
│   ├── MainWindow.xaml.cs    # Application logic
│   ├── WindowsEdgeLight.csproj
│   └── AssemblyInfo.cs
└── README.md
```

### Building

Requires:
- Visual Studio 2022 or later (with .NET desktop development workload)
- Or .NET 8.0 SDK for command-line builds

## Version History

### v0.2 - Primary Monitor Display Fix
- Fixed window to display on primary monitor only
- Added proper DPI scaling support for high-resolution displays
- Resolved namespace conflicts with Windows Forms integration
- Improved multi-monitor setup compatibility

### v0.1 - Initial Release
- Basic edge light overlay functionality
- Customizable brightness controls
- Toggle and keyboard shortcut support
- Click-through transparency

## License

This project is provided as-is for personal and educational use.

## Contributing

Contributions are welcome! Feel free to:
- Report bugs
- Suggest new features
- Submit pull requests

## Acknowledgments

Inspired by the need for professional lighting effects during video conferences and streaming setups.

---

**Note**: This application is designed for Windows only and requires the .NET 8.0 runtime.
