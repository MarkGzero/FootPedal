using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace FootPedalApp
{
    public partial class SettingsWindow : Window
    {
        private TextBox captureTarget = null;
        private bool isModifying = false;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            SetupTextBoxes();
            
            // Set window icon to match tray icon
            try
            {
                var icon = TrayApp.CreateFootPedalIcon(32, true); // Use connected state for settings window
                this.Icon = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set window icon: {ex.Message}");
            }
            
            // Prevent window from losing focus when Win key is pressed
            this.Deactivated += (s, e) => {
                if (isModifying && captureTarget != null)
                {
                    this.Activate();
                }
            };
        }

        private void SetupTextBoxes()
        {
            txtLeft.PreviewKeyDown += TextBox_PreviewKeyDown;
            txtMiddle.PreviewKeyDown += TextBox_PreviewKeyDown;
            txtRight.PreviewKeyDown += TextBox_PreviewKeyDown;
            txtTop.PreviewKeyDown += TextBox_PreviewKeyDown;
        }

        private void TextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && captureTarget == null)
            {
                var editor = new TextEditorWindow(textBox.Text, $"Edit {textBox.Name.Replace("txt", "")} Pedal Command");
                editor.Owner = this;
                if (editor.ShowDialog() == true)
                {
                    textBox.Text = editor.EditedText;
                }
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (captureTarget != null && captureTarget != sender)
            {
                // Another textbox is already capturing, stop it
                ResetCaptureState();
            }
        }

        private void StartCapture(TextBox textBox)
        {
            captureTarget = textBox;
            textBox.Text = "Press key combination...";
            textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));
            textBox.IsReadOnly = true;
        }

        private void ResetCaptureState()
        {
            if (captureTarget != null)
            {
                captureTarget.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                captureTarget.IsReadOnly = false;
                captureTarget = null;
            }
        }

        private void CaptureLeftButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCaptureState();
            StartCapture(txtLeft);
            txtLeft.Focus();
        }

        private void CaptureMiddleButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCaptureState();
            StartCapture(txtMiddle);
            txtMiddle.Focus();
        }

        private void CaptureRightButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCaptureState();
            StartCapture(txtRight);
            txtRight.Focus();
        }

        private void CaptureTopButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCaptureState();
            StartCapture(txtTop);
            txtTop.Focus();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (captureTarget == null) return;

            e.Handled = true;

            var textBox = sender as TextBox;
            if (textBox != captureTarget) return;

            // Build the key combination string
            var keys = new List<string>();
            
            // Check for Windows key specifically (it's often Key.LWin or Key.RWin as the actual key)
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            
            bool hasWinKey = (key == Key.LWin || key == Key.RWin) || (Keyboard.Modifiers & ModifierKeys.Windows) != 0;
            
            if (hasWinKey)
                keys.Add("WIN");
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                keys.Add("CTRL");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                keys.Add("ALT");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                keys.Add("SHIFT");

            // Get the main key (not a modifier)
            if (key != Key.LeftCtrl && key != Key.RightCtrl &&
                key != Key.LeftAlt && key != Key.RightAlt &&
                key != Key.LeftShift && key != Key.RightShift &&
                key != Key.LWin && key != Key.RWin &&
                key != Key.System)
            {
                string keyString = GetKeyString(key);
                if (!string.IsNullOrEmpty(keyString))
                {
                    keys.Add(keyString);
                    
                    // Complete combination captured
                    if (keys.Count > 1)
                    {
                        textBox.Text = string.Join("+", keys);
                        textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                        textBox.IsReadOnly = false;
                        captureTarget = null;
                        return;
                    }
                }
            }

            if (keys.Count > 0)
            {
                textBox.Text = string.Join("+", keys);
                textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
        }

        private string GetKeyString(Key key)
        {
            // Handle number keys
            if (key >= Key.D0 && key <= Key.D9)
                return (key - Key.D0).ToString();
            
            // Handle function keys
            if (key >= Key.F1 && key <= Key.F24)
                return key.ToString();
            
            // Handle letters
            if (key >= Key.A && key <= Key.Z)
                return key.ToString();

            // Handle special keys
            switch (key)
            {
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.Tab: return "Tab";
                case Key.Escape: return "Escape";
                case Key.Back: return "Backspace";
                case Key.Delete: return "Delete";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "PageUp";
                case Key.PageDown: return "PageDown";
                case Key.Left: return "Left";
                case Key.Right: return "Right";
                case Key.Up: return "Up";
                case Key.Down: return "Down";
                case Key.OemPlus: return "+";
                case Key.OemMinus: return "-";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                default: return key.ToString();
            }
        }

        private void LoadSettings()
        {
            txtLeft.Text = AppConfig.Pedals.Left;
            txtMiddle.Text = AppConfig.Pedals.Middle;
            txtRight.Text = AppConfig.Pedals.Right;
            txtTop.Text = AppConfig.Pedals.Top;
            
            // Enable top pedal only if it has a value
            chkEnableTop.IsChecked = !string.IsNullOrWhiteSpace(AppConfig.Pedals.Top);
            UpdateTopPedalControls();
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            // Not needed anymore - textboxes are always editable for typing
            // Capture buttons are used for hotkey capture
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HELP.txt");
                if (System.IO.File.Exists(helpPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", helpPath);
                }
                else
                {
                    MessageBox.Show("Help file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening help: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FootPedalApp",
                    "config.json");

                if (System.IO.File.Exists(configPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", configPath);
                }
                else
                {
                    MessageBox.Show("Config file not found. It will be created when you save settings.", 
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TopPedalCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTopPedalControls();
        }

        private void UpdateTopPedalControls()
        {
            bool enabled = chkEnableTop.IsChecked == true;
            txtTop.IsEnabled = enabled;
            lblTop.IsEnabled = enabled;
            btnCaptureTop.IsEnabled = enabled;
            
            if (!enabled)
            {
                txtTop.Text = "";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.Pedals.Left = txtLeft.Text;
            AppConfig.Pedals.Middle = txtMiddle.Text;
            AppConfig.Pedals.Right = txtRight.Text;
            AppConfig.Pedals.Top = chkEnableTop.IsChecked == true ? txtTop.Text : "";
            AppConfig.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
