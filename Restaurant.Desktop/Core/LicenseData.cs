using System;
using System.Text.Json.Serialization;

namespace Restaurant.Desktop.Core
{
    public class LicenseData
    {
        [JsonPropertyName("LicenseId")]
        public string LicenseId { get; set; } = string.Empty;

        [JsonPropertyName("ProductCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("ExpiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("Device")]
        public LicenseDeviceData? Device { get; set; }
    }

    public class LicenseDeviceData
    {
        [JsonPropertyName("FingerprintHash")]
        public string FingerprintHash { get; set; } = string.Empty;
    }
}
