using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace FootPedalApp
{
    public class TrayApp : IDisposable
    {
        private NotifyIcon trayIcon;
        private RawInputFootPedalListener pedal;
        private static Icon footPedalIcon;
        private bool isDeviceConnected = false;

        public void Initialize()
        {
            Logger.Log("TrayApp: Initialize started");
            try
            {
                AppConfig.Load();
                Logger.Log("TrayApp: Config loaded");

                trayIcon = new NotifyIcon();
                trayIcon.Icon = CreateFootPedalIcon(16, true); // Start with connected (sky blue)
                trayIcon.Text = "Foot Pedal Controller (Connected)";
                trayIcon.DoubleClick += (s, e) => OpenSettings();

                var menu = new ContextMenuStrip();
                menu.Items.Add("Settings", null, (s, e) => OpenSettings());
                menu.Items.Add("Validate Config", null, (s, e) => ValidateConfig());
                menu.Items.Add("View Log", null, (s, e) => ViewLog());
                menu.Items.Add("Exit", null, (s, e) => ExitApp());
                trayIcon.ContextMenuStrip = menu;

                // IMPORTANT: Set visible AFTER everything else is configured
                trayIcon.Visible = true;
                Logger.Log("TrayApp: Tray icon created and visible");

                pedal = new RawInputFootPedalListener();
                pedal.PedalPressed += OnPedalPressed;
                pedal.DeviceStatusChanged += OnDeviceStatusChanged;
                isDeviceConnected = true; // Initial state matches icon
                Logger.Log("TrayApp: Pedal listener initialized");
            }
            catch (Exception ex)
            {
                Logger.LogException("TrayApp.Initialize", ex);
                System.Windows.MessageBox.Show($"TrayApp Initialize Error: {ex.Message}\n\n{ex.StackTrace}", "Error");
                throw;
            }
        }

        private void OnPedalPressed(string button)
        {
            Logger.Log($"OnPedalPressed: button={button}");
            
            string command = null;
            switch (button)
            {
                case "Left":   command = AppConfig.Pedals.Left; break;
                case "Middle": command = AppConfig.Pedals.Middle; break;
                case "Right":  command = AppConfig.Pedals.Right; break;
                case "Top":    command = AppConfig.Pedals.Top; break;
            }
            
            if (!string.IsNullOrEmpty(command))
            {
                Logger.Log($"OnPedalPressed: Executing command for {button}: {command}");
                ExecuteCommand(command);
            }
        }

        private void OnDeviceStatusChanged(bool connected)
        {
            Logger.Log($"OnDeviceStatusChanged: Device {(connected ? "CONNECTED" : "DISCONNECTED")}");
            
            isDeviceConnected = connected;
            
            // Update tray icon
            trayIcon.Icon = CreateFootPedalIcon(16, connected);
            trayIcon.Text = connected ? "Foot Pedal Controller (Connected)" : "Foot Pedal Controller (Disconnected)";
            
            // Show notification ONLY on disconnect
            if (!connected)
            {
                trayIcon.BalloonTipTitle = "Foot Pedal Disconnected";
                trayIcon.BalloonTipText = "Foot pedal device has been disconnected.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                Logger.Log($"OnDeviceStatusChanged: Showing balloon tip - {trayIcon.BalloonTipTitle}");
                trayIcon.ShowBalloonTip(3000); // Show for 3 seconds
            }
        }

        private void ExecuteCommand(string command)
        {
            try
            {
                // Check for RUN: prefix
                if (command.StartsWith("RUN:", StringComparison.OrdinalIgnoreCase))
                {
                    string processCmd = command.Substring(4).Trim();
                    Logger.Log($"ExecuteCommand: Running process: {processCmd}");
                    
                    // Parse executable and arguments, handling quotes
                    string executable, arguments;
                    
                    if (processCmd.StartsWith("\""))
                    {
                        // Quoted path: "C:\Program Files\app.exe" args
                        int endQuote = processCmd.IndexOf('"', 1);
                        if (endQuote > 0)
                        {
                            executable = processCmd.Substring(1, endQuote - 1);
                            arguments = processCmd.Substring(endQuote + 1).Trim();
                        }
                        else
                        {
                            executable = processCmd.Trim('"');
                            arguments = "";
                        }
                    }
                    else
                    {
                        // Unquoted path: app.exe args
                        int spaceIndex = processCmd.IndexOf(' ');
                        if (spaceIndex > 0)
                        {
                            executable = processCmd.Substring(0, spaceIndex);
                            arguments = processCmd.Substring(spaceIndex + 1).Trim();
                        }
                        else
                        {
                            executable = processCmd;
                            arguments = "";
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        System.Diagnostics.Process.Start(executable, arguments);
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(executable);
                    }
                }
                // Check for URL: prefix
                else if (command.StartsWith("URL:", StringComparison.OrdinalIgnoreCase))
                {
                    string urlCmd = command.Substring(4).Trim();
                    Logger.Log($"ExecuteCommand: Opening URL: {urlCmd}");
                    
                    // Check if specific browser is specified
                    int spaceIndex = urlCmd.IndexOf(' ');
                    if (spaceIndex > 0 && !urlCmd.StartsWith("http"))
                    {
                        // Format: URL:chrome https://google.com
                        string browser = urlCmd.Substring(0, spaceIndex).ToLower();
                        string url = urlCmd.Substring(spaceIndex + 1).Trim();
                        
                        string browserPath = GetBrowserPath(browser);
                        if (!string.IsNullOrEmpty(browserPath))
                        {
                            System.Diagnostics.Process.Start(browserPath, url);
                        }
                        else
                        {
                            Logger.Log($"ExecuteCommand: Browser '{browser}' not found, using default");
                            System.Diagnostics.Process.Start(url);
                        }
                    }
                    else
                    {
                        // Default browser
                        System.Diagnostics.Process.Start(urlCmd);
                    }
                }
                // Otherwise treat as hotkey
                else
                {
                    Logger.Log($"ExecuteCommand: Sending hotkey: {command}");
                    HotkeySender.Send(command);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("ExecuteCommand", ex);
            }
        }

        private string GetBrowserPath(string browser)
        {
            try
            {
                string path = null;
                switch (browser.ToLower())
                {
                    case "chrome":
                        path = AppConfig.Browsers.Chrome;
                        break;
                    case "firefox":
                        path = AppConfig.Browsers.Firefox;
                        break;
                    case "edge":
                        path = AppConfig.Browsers.Edge;
                        break;
                    case "brave":
                        path = AppConfig.Browsers.Brave;
                        break;
                }

                // Check if file exists
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    return path;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void OpenSettings()
        {
            Logger.Log("TrayApp: Opening settings");
            var win = new SettingsWindow();
            win.ShowDialog();
            AppConfig.Load();
        }

        private void ViewLog()
        {
            Logger.Log("TrayApp: Opening log file");
            try
            {
                System.Diagnostics.Process.Start("notepad.exe", Logger.GetLogPath());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open log file:\n{Logger.GetLogPath()}\n\nError: {ex.Message}", "Error");
            }
        }

        private void ValidateConfig()
        {
            Logger.Log("TrayApp: Validating config file");
            string result = AppConfig.Validate();
            
            var icon = result.StartsWith("ERROR") 
                ? System.Windows.MessageBoxImage.Error 
                : System.Windows.MessageBoxImage.Information;
            
            System.Windows.MessageBox.Show(
                result,
                "Config Validation",
                System.Windows.MessageBoxButton.OK,
                icon);
        }

        private void ExitApp()
        {
            if (trayIcon != null)
                trayIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        public static Icon GetFootPedalIcon()
        {
            if (footPedalIcon == null)
            {
                footPedalIcon = CreateFootPedalIcon(16, true); // Default to connected
            }
            return footPedalIcon;
        }

        public static Icon CreateFootPedalIcon(int size, bool connected)
        {
            // Create a bitmap for the icon
            var bitmap = new System.Drawing.Bitmap(size, size);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // Background color based on connection status
                var backgroundColor = connected 
                    ? System.Drawing.Color.FromArgb(135, 206, 235) // Sky blue
                    : System.Drawing.Color.FromArgb(220, 20, 60);   // Crimson red
                
                g.Clear(backgroundColor);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Scale factors
                float scale = size / 16f;

                // Draw three pedal rectangles - darker color for contrast
                var pedalBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(40, 40, 40)); // Dark gray
                var pedalPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(20, 20, 20), Math.Max(1, scale));

                // Left pedal
                g.FillRectangle(pedalBrush, 1 * scale, 8 * scale, 3 * scale, 6 * scale);
                g.DrawRectangle(pedalPen, 1 * scale, 8 * scale, 3 * scale, 6 * scale);

                // Middle pedal
                g.FillRectangle(pedalBrush, 6 * scale, 8 * scale, 3 * scale, 6 * scale);
                g.DrawRectangle(pedalPen, 6 * scale, 8 * scale, 3 * scale, 6 * scale);

                // Right pedal
                g.FillRectangle(pedalBrush, 11 * scale, 8 * scale, 3 * scale, 6 * scale);
                g.DrawRectangle(pedalPen, 11 * scale, 8 * scale, 3 * scale, 6 * scale);

                // Draw base
                g.FillRectangle(System.Drawing.Brushes.Black, 0, 14 * scale, size, 2 * scale);

                pedalBrush.Dispose();
                pedalPen.Dispose();
            }

            return Icon.FromHandle(bitmap.GetHicon());
        }

        public void Dispose()
        {
            if (trayIcon != null)
                trayIcon.Dispose();
            if (pedal != null)
                pedal.Dispose();
        }
    }
}
