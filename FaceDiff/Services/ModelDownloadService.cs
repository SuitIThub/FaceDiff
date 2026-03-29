using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FaceDiff.Services
{
    public class ModelDownloadService
    {
        private static readonly string ModelsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceDiff", "models");

        public static string HaarCascadePath => Path.Combine(ModelsDir, "haarcascade_frontalface_default.xml");
        public static string DnnProtoPath => Path.Combine(ModelsDir, "deploy.prototxt");
        public static string DnnModelPath => Path.Combine(ModelsDir, "res10_300x300_ssd_iter_140000.caffemodel");

        private static readonly (string Url, string FileName)[] ModelFiles =
        {
            ("https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml",
             "haarcascade_frontalface_default.xml"),
            ("https://raw.githubusercontent.com/opencv/opencv/master/samples/dnn/face_detector/deploy.prototxt",
             "deploy.prototxt"),
            ("https://raw.githubusercontent.com/opencv/opencv_3rdparty/dnn_samples_face_detector_20170830/res10_300x300_ssd_iter_140000.caffemodel",
             "res10_300x300_ssd_iter_140000.caffemodel")
        };

        public bool AllModelsPresent()
        {
            foreach (var (_, fileName) in ModelFiles)
            {
                if (!File.Exists(Path.Combine(ModelsDir, fileName)))
                    return false;
            }
            return true;
        }

        public async Task DownloadModelsAsync(IProgress<(string FileName, double Progress)> progress,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(ModelsDir);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);

                foreach (var (url, fileName) in ModelFiles)
                {
                    var destPath = Path.Combine(ModelsDir, fileName);
                    if (File.Exists(destPath))
                    {
                        progress?.Report((fileName, 1.0));
                        continue;
                    }

                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1;
                        long received = 0;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                                received += bytesRead;
                                if (totalBytes > 0)
                                    progress?.Report((fileName, (double)received / totalBytes));
                            }
                        }
                    }
                    progress?.Report((fileName, 1.0));
                }
            }
        }
    }
}
