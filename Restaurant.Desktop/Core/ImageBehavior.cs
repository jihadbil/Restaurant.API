using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Restaurant.Desktop.Core
{
    public static class ImageBehavior
    {
        public static readonly DependencyProperty ImageUrlProperty =
            DependencyProperty.RegisterAttached(
                "ImageUrl",
                typeof(string),
                typeof(ImageBehavior),
                new PropertyMetadata(string.Empty, OnImageUrlChanged));

        public static string GetImageUrl(DependencyObject obj)
        {
            return (string)obj.GetValue(ImageUrlProperty);
        }

        public static void SetImageUrl(DependencyObject obj, string value)
        {
            obj.SetValue(ImageUrlProperty, value);
        }

        private static async void OnImageUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image image)
            {
                string? url = e.NewValue as string;
                if (string.IsNullOrWhiteSpace(url))
                {
                    image.Source = null;
                    return;
                }

                string absoluteUrl = url;
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    absoluteUrl = $"{AppSettings.Instance.ApiBaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
                }

                try
                {
                    // Create an SSL-bypassing handler for local dev certificates
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };

                    using (var client = new HttpClient(handler))
                    {
                        var bytes = await client.GetByteArrayAsync(absoluteUrl);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Prevent thread-affinity issues
                            image.Source = bitmap;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load image from {absoluteUrl}: {ex.Message}");
                    image.Source = null;
                }
            }
        }
    }
}
