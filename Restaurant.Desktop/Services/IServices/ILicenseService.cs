using System.Threading.Tasks;

namespace Restaurant.Desktop.Services.IServices
{
    public interface ILicenseService
    {
        string GetMachineFingerprint();
        Task<(bool Success, string? Error)> ActivateLicenseAsync(string licenseKey);
        bool IsLicenseValid();
    }
}
