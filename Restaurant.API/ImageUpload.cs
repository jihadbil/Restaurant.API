using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace Restaurant.API;

internal static class ImageUpload
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MaxDimension = 8192;
    private const long MaxPixels = 16 * 1024 * 1024;

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

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            return (null, "يسمح فقط بصور JPEG وPNG الصالحة.");
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > MaxDimension || info.Height > MaxDimension
            || (long)info.Width * info.Height > MaxPixels)
            return (null, "أبعاد الصورة تتجاوز الحد المسموح: 8192 بكسل للضلع و16 ميجابكسل للصورة.");
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (bitmap.GetPixels() == IntPtr.Zero || codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            return (null, "ملف الصورة غير مكتمل أو تالف.");
        var extension = codec.EncodedFormat == SKEncodedImageFormat.Png ? ".png" : ".jpg";

        var uploadsFolder = Path.Combine(webRootPath, "images", folderName);
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await output.WriteAsync(bytes);
        }

        return ($"/images/{folderName}/{fileName}", null);
    }
}
