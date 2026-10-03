using System;
using System.IO;
using System.Text.Json;
using Restaurant.Models.Enums;

namespace Restaurant.Desktop.Core
{
    public class AppSettings
    {
        public string ApiBaseUrl { get; set; } = "https://localhost:7040/";
        public OrderStatus DefaultOrderStatus { get; set; } = OrderStatus.Preparing;
        public int? DefaultCashboxId { get; set; } = null;
        public int ReceiptPaperWidth { get; set; } = 80;
        public int KitchenPaperWidth { get; set; } = 80;
        public string ProductCode { get; set; } = "RESTAURANT_POS_V2";
        public string LicenseHubUrl { get; set; } = string.Empty;
        public string OrganizationApiKey { get; set; } = string.Empty;
        public string LicensePublicKey { get; set; } = string.Empty;

        private static AppSettings? _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public void Save()
        {
            try
            {
                string configPath = GetUserConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static AppSettings Load()
        {
            try
            {
                string userConfigPath = GetUserConfigPath();
                string bundledConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "appsettings.json");
                string configPath = File.Exists(userConfigPath) ? userConfigPath : bundledConfigPath;
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            return new AppSettings();
        }

        private static string GetUserConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Restaurant.Desktop",
                "appsettings.json");
        }
    }
}
