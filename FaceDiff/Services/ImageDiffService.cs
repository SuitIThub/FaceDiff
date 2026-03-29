using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    public class ImageDiffService
    {
        /// <param name="threshold">
        /// Euclidean RGB distance threshold. 0 = exact match; values up to ~441 (max distance).
        /// </param>
        public Task<string> GenerateDiffAsync(
            string baseImagePath,
            string comparisonImagePath,
            OvalRegion oval,
            double threshold,
            string destinationDir,
            string outputFileName)
        {
            return Task.Run(() =>
            {
                using (var baseBmp = new Bitmap(baseImagePath))
                using (var compBmp = new Bitmap(comparisonImagePath))
                {
                    int w = baseBmp.Width;
                    int h = baseBmp.Height;

                    using (var scaledComp = new Bitmap(compBmp, w, h))
                    using (var result = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                    {
                        var baseData = baseBmp.LockBits(
                            new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        var compData = scaledComp.LockBits(
                            new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                        var resData = result.LockBits(
                            new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                        int stride = baseData.Stride;
                        int byteCount = stride * h;
                        var basePixels = new byte[byteCount];
                        var compPixels = new byte[byteCount];
                        var resPixels = new byte[byteCount];

                        Marshal.Copy(baseData.Scan0, basePixels, 0, byteCount);
                        Marshal.Copy(compData.Scan0, compPixels, 0, byteCount);

                        double threshSq = threshold * threshold;

                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                int idx = y * stride + x * 4;

                                if (!oval.ContainsPoint(x, y))
                                {
                                    resPixels[idx + 3] = 0; // transparent
                                    continue;
                                }

                                int db = basePixels[idx] - compPixels[idx];
                                int dg = basePixels[idx + 1] - compPixels[idx + 1];
                                int dr = basePixels[idx + 2] - compPixels[idx + 2];
                                double distSq = dr * dr + dg * dg + db * db;

                                if (distSq > threshSq)
                                {
                                    resPixels[idx] = compPixels[idx];       // B
                                    resPixels[idx + 1] = compPixels[idx + 1]; // G
                                    resPixels[idx + 2] = compPixels[idx + 2]; // R
                                    resPixels[idx + 3] = 255;                 // A
                                }
                                else
                                {
                                    resPixels[idx + 3] = 0; // transparent
                                }
                            }
                        }

                        Marshal.Copy(resPixels, 0, resData.Scan0, byteCount);

                        baseBmp.UnlockBits(baseData);
                        scaledComp.UnlockBits(compData);
                        result.UnlockBits(resData);

                        Directory.CreateDirectory(destinationDir);
                        string outputPath = Path.Combine(destinationDir, outputFileName);
                        result.Save(outputPath, ImageFormat.Png);
                        return outputPath;
                    }
                }
            });
        }
    }
}
