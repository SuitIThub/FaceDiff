using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public static class ThumbnailService
    {
        private const int ThumbnailSize = 150;

        public static Task<BitmapImage> CreateThumbnailAsync(string imagePath, int size = ThumbnailSize)
        {
            return Task.Run(() =>
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(imagePath, UriKind.Absolute);
                bi.DecodePixelWidth = size;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                return bi;
            });
        }

        public static Task<BitmapImage> CreateFaceThumbnailAsync(string imagePath, OvalRegion oval, int size = ThumbnailSize)
        {
            return Task.Run(() =>
            {
                var source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri(imagePath, UriKind.Absolute);
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.EndInit();
                source.Freeze();

                double left = Math.Max(0, oval.CenterX - oval.RadiusX * 1.3);
                double top = Math.Max(0, oval.CenterY - oval.RadiusY * 1.3);
                double width = Math.Min(source.PixelWidth - left, oval.RadiusX * 2.6);
                double height = Math.Min(source.PixelHeight - top, oval.RadiusY * 2.6);

                if (width <= 0 || height <= 0)
                    return source;

                var crop = new CroppedBitmap(source, new Int32Rect(
                    (int)left, (int)top, (int)width, (int)height));
                crop.Freeze();

                var drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawImage(crop, new Rect(0, 0, size, size));
                    var ovalCx = (oval.CenterX - left) / width * size;
                    var ovalCy = (oval.CenterY - top) / height * size;
                    var ovalRx = oval.RadiusX / width * size;
                    var ovalRy = oval.RadiusY / height * size;
                    dc.DrawEllipse(null,
                        new Pen(Brushes.LimeGreen, 2),
                        new Point(ovalCx, ovalCy), ovalRx, ovalRy);
                }

                var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(drawingVisual);
                rtb.Freeze();

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Position = 0;
                    var result = new BitmapImage();
                    result.BeginInit();
                    result.StreamSource = ms;
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.EndInit();
                    result.Freeze();
                    return result;
                }
            });
        }
    }
}
