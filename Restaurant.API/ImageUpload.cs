using Microsoft.AspNetCore.Http;

namespace Restaurant.API;

internal static class ImageUpload
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    public static async Task<(string? Url, string? Error)> SaveAsync(
        IFormFile file,
        string webRootPath,
        string folderName)
    {
        if (file == null || file.Length == 0)
        {
            return (null, "لم يتم رفع أي ملف.");
        }

        if (file.Length > MaxFileSize)
        {
            return (null, "حجم الصورة يجب ألا يتجاوز 5 ميجابايت.");
        }

        var header = new byte[8];
        await using (var input = file.OpenReadStream())
        {
            var bytesRead = await input.ReadAsync(header.AsMemory(0, header.Length));
            if (bytesRead < 3)
            {
                return (null, "ملف الصورة غير صالح.");
            }
        }

        string? extension = null;
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            extension = ".jpg";
        }
        else if (header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            extension = ".png";
        }

        if (extension == null)
        {
            return (null, "يسمح فقط بصور JPEG وPNG الصالحة.");
        }

        var uploadsFolder = Path.Combine(webRootPath, "images", folderName);
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(output);
        }

        return ($"/images/{folderName}/{fileName}", null);
    }
}
