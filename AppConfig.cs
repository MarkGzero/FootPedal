using System;
using System.IO;
using Newtonsoft.Json;

namespace FootPedalApp
{
    public class AppConfig
    {
        public static PedalConfig Pedals { get; private set; } = new PedalConfig();
        public static BrowserConfig Browsers { get; private set; } = new BrowserConfig();

        public static void Load()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FootPedalApp", "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ConfigRoot>(json);
                    if (config != null)
                    {
                        Pedals = config.Pedals ?? new PedalConfig();
                        Browsers = config.Browsers ?? new BrowserConfig();
                        Logger.Log("Config loaded successfully");
                    }
                }
                else
                {
                    Logger.Log("Config file not found, using defaults");
                }
            }
            catch (JsonException ex)
            {
                Logger.Log($"ERROR: Invalid JSON in config.json: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error loading config.json:\n\n{ex.Message}\n\nUsing default configuration.\n\nPlease fix the JSON syntax or delete the file to reset.",
                    "Configuration Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                // Use default config if loading fails
            }
            catch (Exception ex)
            {
                Logger.LogException("AppConfig.Load", ex);
                // Use default config if loading fails
            }
        }

        public static void Save()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FootPedalApp", "config.json");
                var directory = Path.GetDirectoryName(configPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var config = new ConfigRoot
                {
                    Pedals = Pedals,
                    Browsers = Browsers
                };
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Logger.Log("Config saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogException("AppConfig.Save", ex);
                System.Windows.MessageBox.Show(
                    $"Error saving config.json:\n\n{ex.Message}\n\nSettings could not be saved.",
                    "Save Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public static string Validate()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FootPedalApp", "config.json");
                if (!File.Exists(configPath))
                {
                    return "Config file does not exist. Using default configuration.";
                }

                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<ConfigRoot>(json);
                
                if (config == null)
                {
                    return "ERROR: Config file is empty or null.";
                }

                // Validate structure
                if (config.Pedals == null)
                {
                    return "ERROR: Missing 'Pedals' section in config.json.";
                }

                if (config.Browsers == null)
                {
                    return "ERROR: Missing 'Browsers' section in config.json.";
                }

                // Check for required pedal properties
                var pedalType = config.Pedals.GetType();
                if (pedalType.GetProperty("Left") == null ||
                    pedalType.GetProperty("Middle") == null ||
                    pedalType.GetProperty("Right") == null ||
                    pedalType.GetProperty("Top") == null)
                {
                    return "ERROR: Missing pedal properties (Left, Middle, Right, Top).";
                }

                return "Config file is valid.";
            }
            catch (JsonException ex)
            {
                return $"ERROR: Invalid JSON syntax:\n{ex.Message}";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }
    }

    public class ConfigRoot
    {
        public PedalConfig Pedals { get; set; } = new PedalConfig();
        public BrowserConfig Browsers { get; set; } = new BrowserConfig();
    }

    public class PedalConfig
    {
        public string Left { get; set; } = "WIN+1";
        public string Middle { get; set; } = "WIN+2";
        public string Right { get; set; } = "WIN+3";
        public string Top { get; set; } = "";
    }

    public class BrowserConfig
    {
        public string Chrome { get; set; } = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        public string Firefox { get; set; } = @"C:\Program Files\Mozilla Firefox\firefox.exe";
        public string Edge { get; set; } = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        public string Brave { get; set; } = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe";
    }
}
