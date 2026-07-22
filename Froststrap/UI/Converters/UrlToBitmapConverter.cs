using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Collections.Concurrent;

namespace Froststrap.UI.Converters
{
    public class UrlToBitmapConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, Bitmap?> _imageCache = new();

        static UrlToBitmapConverter()
        {
            if (App.HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
                App.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Roblox/Froststrap");
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string url || string.IsNullOrEmpty(url))
                return null;

            try
            {
                if (_imageCache.TryGetValue(url, out var cachedBitmap))
                    return cachedBitmap;

                // Never block the UI thread on HttpClient (.Result can deadlock Avalonia).
                Bitmap? bitmap = Task.Run(async () =>
                {
                    using var response = await App.HttpClient.GetAsync(url).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                    memoryStream.Position = 0;
                    return new Bitmap(memoryStream);
                }).GetAwaiter().GetResult();

                _imageCache.TryAdd(url, bitmap);
                return bitmap;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("UrlToBitmapConverter", $"Failed to load image from {url}: {ex.Message}");
                _imageCache.TryAdd(url, null);
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
