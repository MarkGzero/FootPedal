# FootPedal App

A Windows system tray application that allows you to control your computer using a USB foot pedal. Configure each pedal button to send keyboard shortcuts, launch programs, or open websites.

## 🎯 Features

- **System Tray Operation**: Runs quietly in your system tray with visual connection status
- **Four Configurable Buttons**: Map Left, Middle, Right, and Top pedal buttons to different actions
- **Multiple Command Types**:
  - **Keyboard Hotkeys**: Send any keyboard shortcut (WIN+1, CTRL+C, ALT+F4, etc.)
  - **Launch Programs**: Run applications with optional arguments
  - **Open URLs**: Open websites in your default or specified browser
- **Visual Status Indicator**: Sky blue icon when connected, red when disconnected
- **Easy Configuration**: GUI settings window with hotkey capture functionality
- **Plug & Play**: Automatic device detection with disconnect notifications
- **Dark Theme UI**: Comfortable dark-themed settings interface

## 💻 Requirements

- **Operating System**: Windows (requires .NET Framework 4.8)
- **Compatible Device**: OLYMPUS IMAGING CORP. HID FootSwitch RS Series
  - VID: `07B4`
  - PID: `025F`
  - HID Usage: Consumer Control (0x0C/0x01)

## 🚀 Installation

1. Download the latest release from the releases page
2. Extract the files to your preferred location
3. Run `FootPedalApp.exe`
4. The application will appear in your system tray (near the clock)
5. Right-click the tray icon to access settings

### Configuration Files Location

All configuration and log files are stored in:
```
%APPDATA%\FootPedalApp\
```

Files:
- `config.json` - Pedal button commands and browser paths
- `debug.log` - Application activity log

## 📖 Usage

### Quick Start

1. **Access Settings**: Double-click the tray icon or right-click → Settings
2. **Configure Pedals**: Choose how to configure each button:
   - **For Hotkeys**: Click "Capture" and press your desired key combination
   - **For Programs**: Type `RUN:program.exe` (e.g., `RUN:notepad.exe`)
   - **For URLs**: Type `URL:https://example.com` or `URL:chrome https://example.com`
3. **Save**: Click "Save" to apply your configuration
4. **Test**: Press your foot pedal buttons to test

### Command Syntax

#### 1. Keyboard Hotkeys

Format: `MODIFIER+KEY`

**Supported Modifiers**: WIN, CTRL, ALT, SHIFT

**Examples**:
```
WIN+1           - Open taskbar app at position 1
WIN+2           - Open taskbar app at position 2
CTRL+C          - Copy
CTRL+V          - Paste
ALT+F4          - Close window
CTRL+SHIFT+ESC  - Task Manager
WIN+L           - Lock computer
```

**How to Capture**:
1. Click the "Capture" button next to the pedal textbox
2. Press the key combination you want (e.g., Win+1)
3. The hotkey will be automatically captured and saved

#### 2. Run Programs

Format: `RUN:program.exe [arguments]`  
Format: `RUN:"path with spaces.exe" [arguments]`

**Examples**:
```
RUN:notepad.exe
RUN:notepad.exe C:\file.txt
RUN:calc.exe
RUN:explorer.exe C:\Users
RUN:cmd.exe /k dir
RUN:"C:\Program Files\MyApp\app.exe"
RUN:"C:\Program Files\MyApp\app.exe" --arg1 value1
```

> **Note**: Use quotes around paths that contain spaces!

#### 3. Open URLs

Format: `URL:address`  
Format: `URL:browser address`

**Supported Browsers**: chrome, firefox, edge, brave

**Examples**:
```
URL:https://google.com
URL:https://youtube.com
URL:chrome https://gmail.com
URL:firefox https://github.com
URL:edge https://bing.com
```

#### 4. Disable a Pedal

Leave the textbox empty to disable that pedal button.

### System Tray Menu

Right-click the tray icon to access:
- **Settings** - Configure pedal button commands
- **Validate Config** - Check config.json for errors
- **View Log** - Open debug.log to troubleshoot issues
- **Exit** - Close the application

### Settings Window Features

- **Capture Button**: Automatically capture keyboard shortcuts
- **Double-click Textbox**: Opens a larger, resizable editor for long commands
- **Config Button**: Opens config.json in Notepad for direct editing
- **Help Button (?)**: Opens the help file with detailed command reference
- **Enable Top Pedal**: Toggle to show/hide the 4th pedal button configuration

## 🔧 Configuration

### Manual Configuration

You can directly edit the configuration file:

1. Click "Config" button in Settings window, or
2. Navigate to `%APPDATA%\FootPedalApp\config.json`

**Example config.json**:
```json
{
  "Pedals": {
    "Left": "WIN+1",
    "Middle": "WIN+2",
    "Right": "WIN+3",
    "Top": ""
  },
  "Browsers": {
    "Chrome": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "Firefox": "C:\\Program Files\\Mozilla Firefox\\firefox.exe",
    "Edge": "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "Brave": "C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe"
  }
}
```

> **Note**: Use double backslashes (`\\`) in JSON for Windows paths.

### Default Configuration

- **Left Pedal**: `WIN+1` (Open first taskbar application)
- **Middle Pedal**: `WIN+2` (Open second taskbar application)
- **Right Pedal**: `WIN+3` (Open third taskbar application)
- **Top Pedal**: Empty (disabled)

## 🔍 Troubleshooting

### Common Issues

**Capture button not working?**
- Edit config.json directly and type the hotkey manually
- Click "Save" in Settings window to reload

**Windows key combinations not working?**
- Make sure to capture them with the Capture button
- Press the combination quickly
- Try editing config.json directly if capture fails

**RUN command fails?**
- Use full path to the executable
- Wrap paths with spaces in quotes: `RUN:"C:\Program Files\app.exe"`
- Check if file exists at the specified path
- View Log to see exact error message

**URL not opening?**
- Include `http://` or `https://` in the address
- Check browser name spelling (chrome, firefox, edge, brave)
- Verify browser paths in config.json

**Icon shows red but pedal is plugged in?**
- Unplug and replug the USB cable
- Press a pedal button to reactivate detection
- Check Device Manager for "OLYMPUS IMAGING CORP. HID FootSwitch RS"

**Settings not saving?**
- Make sure to click "Save" button before closing
- Use "Validate Config" from tray menu to check for JSON errors
- Check if `%APPDATA%\FootPedalApp` folder is writable

### Debugging

**View Logs**:
- Right-click tray icon → "View Log"
- Log location: `%APPDATA%\FootPedalApp\debug.log`
- Each pedal press and command execution is logged

**Validate Configuration**:
- Right-click tray icon → "Validate Config"
- Checks JSON syntax and structure
- Reports any configuration errors

## 🔒 Technical Details

- **Device**: OLYMPUS IMAGING CORP. HID FootSwitch RS Series
- **VID/PID**: VID_07B4&PID_025F
- **HID Usage**: Consumer Control (0x0C/0x01)
- **Interface**: USB HID
- **Monitoring**: Windows WM_DEVICECHANGE events for plug/unplug detection
- **Framework**: .NET Framework 4.8, WPF
- **Dependencies**: Newtonsoft.Json 13.0.1

## 📝 Building from Source

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- Windows SDK

### Build Steps
```bash
# Clone the repository
git clone https://github.com/MarkGzero/FootPedal.git
cd FootPedal

# Open in Visual Studio
start footpedalapp.sln

# Or build via command line
msbuild footpedalapp.sln /p:Configuration=Release
```

The compiled executable will be in `bin/Release/net48/`

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## 📄 License

This project is provided as-is. Please check the repository for license information.

## 🙏 Acknowledgments

- Designed for use with OLYMPUS IMAGING CORP. HID FootSwitch RS Series foot pedals
- Built with WPF and Windows Forms for system tray integration

---

**Note**: This application is specifically designed for Windows and requires the compatible foot pedal hardware to function.
