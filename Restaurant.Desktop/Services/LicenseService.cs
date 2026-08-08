using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Restaurant.Desktop.Core;
using Restaurant.Desktop.Services.IServices;

namespace Restaurant.Desktop.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _licenseFilePath;

        public LicenseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.lic");
        }

        public string GetMachineFingerprint()
        {
            try
            {
                var mbs = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard");
                var cpu = new ManagementObjectSearcher("Select ProcessorId From Win32_Processor");

                string boardSerial = mbs.Get().Cast<ManagementBaseObject>().FirstOrDefault()?["SerialNumber"]?.ToString() ?? "UNKNOWN_BOARD";
                string cpuId = cpu.Get().Cast<ManagementBaseObject>().FirstOrDefault()?["ProcessorId"]?.ToString() ?? "UNKNOWN_CPU";

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(boardSerial + cpuId));
                return Convert.ToBase64String(hashBytes).Substring(0, 32);
            }
            catch (Exception)
            {
                // Fallback in case WMI is not available or fails
                return "UNKNOWN_MACHINE_FINGERPRINT_1234";
            }
        }

        public async Task<(bool Success, string? Error)> ActivateLicenseAsync(string licenseKey)
        {
            try
            {
                var fingerprint = GetMachineFingerprint();
                var machineName = Environment.MachineName;

                var request = new
                {
                    productCode = AppSettings.Instance.ProductCode,
                    licenseKey = licenseKey,
                    machineFingerprint = fingerprint,
                    machineName = machineName
                };

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{AppSettings.Instance.LicenseHubUrl.TrimEnd('/')}/api/v1/client/activation");
                requestMessage.Headers.Add("X-Api-Key", AppSettings.Instance.OrganizationApiKey);
                requestMessage.Content = JsonContent.Create(request);

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var jsonStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("fileContentBase64", out var base64El) && base64El.ValueKind == JsonValueKind.String)
                    {
                        var b64 = base64El.GetString();
                        if (!string.IsNullOrEmpty(b64))
                        {
                            var decodedFileContentBytes = Convert.FromBase64String(b64);
                            await File.WriteAllBytesAsync(_licenseFilePath, decodedFileContentBytes);
                            return (true, null);
                        }
                    }
                    return (false, "لم يتم العثور على محتوى الرخصة في استجابة الخادم.");
                }

                // Handle error
                string errorContent = "";
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        errorContent = msgProp.GetString() ?? errorContent;
                    }
                }
                catch
                {
                    // ignored
                }

                if (string.IsNullOrWhiteSpace(errorContent))
                {
                    errorContent = $"فشل التفعيل. رمز الخطأ: {response.StatusCode}";
                }

                return (false, errorContent);
            }
            catch (HttpRequestException)
            {
                return (false, "لا يوجد اتصال بالإنترنت أو الخادم غير متاح.");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ غير متوقع: {ex.Message}");
            }
        }

        public bool IsLicenseValid()
        {
            try
            {
                if (!File.Exists(_licenseFilePath))
                {
                    File.WriteAllText("license_debug.txt", "File does not exist.");
                    return false;
                }

                var licContent = File.ReadAllText(_licenseFilePath, Encoding.UTF8);
                
                if (!licContent.StartsWith("v1."))
                {
                    File.WriteAllText("license_debug.txt", "File does not start with v1.");
                    return false;
                }

                var parts = licContent.Substring(3).Split('.');
                if (parts.Length != 2)
                {
                    File.WriteAllText("license_debug.txt", "File does not have 2 parts.");
                    return false;
                }

                var payloadB64 = parts[0];
                var signatureB64 = parts[1];
                
                if (string.IsNullOrEmpty(signatureB64)) 
                {
                    File.WriteAllText("license_debug.txt", "Signature is empty.");
                    return false;
                }

                var payloadBytes = Convert.FromBase64String(payloadB64);
                var signatureBytes = Convert.FromBase64String(signatureB64);

                var publicKeyBase64 = AppSettings.Instance.LicensePublicKey;
                
                if (string.IsNullOrWhiteSpace(publicKeyBase64))
                {
                    File.WriteAllText("license_debug.txt", "Public key is empty.");
                    return false; 
                }

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);

                bool isSignatureValid = ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256);
                if (!isSignatureValid)
                {
                    File.WriteAllText("license_debug.txt", "Signature is NOT valid.");
                    return false;
                }

                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                var licenseData = JsonSerializer.Deserialize<LicenseData>(payloadJson);
                if (licenseData == null)
                {
                    File.WriteAllText("license_debug.txt", "Deserialized LicenseData is null.");
                    return false;
                }

                var fingerprint = GetMachineFingerprint();
                if (licenseData.Device == null || licenseData.Device.FingerprintHash != fingerprint)
                {
                    File.WriteAllText("license_debug.txt", $"Fingerprint mismatch. Device null? {licenseData.Device == null}. Expected: {fingerprint}, Actual: {licenseData.Device?.FingerprintHash}");
                    return false;
                }

                if (licenseData.ExpiresAt.HasValue && DateTime.UtcNow > licenseData.ExpiresAt.Value)
                {
                    File.WriteAllText("license_debug.txt", $"License expired. Expiry: {licenseData.ExpiresAt.Value}");
                    return false;
                }

                File.WriteAllText("license_debug.txt", "License is valid!");
                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText("license_debug.txt", $"Exception: {ex}");
                return false;
            }
        }
    }
}
