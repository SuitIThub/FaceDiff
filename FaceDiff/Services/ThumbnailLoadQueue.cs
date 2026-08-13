using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using FaceDiff.Models;

namespace FaceDiff.Services
{
    /// <summary>
    /// Low-concurrency lazy thumbnail loader to avoid disk/CPU storms on large folders.
    /// </summary>
    public static class ThumbnailLoadQueue
    {
        private const int MaxConcurrency = 2;
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(MaxConcurrency);
        private static readonly ConcurrentDictionary<string, byte> InFlight = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private static int _generation;

        public static void CancelPending()
        {
            Interlocked.Increment(ref _generation);
            InFlight.Clear();
        }

        public static void EnqueueBase(BaseImageModel model)
        {
            if (model == null || model.Thumbnail != null || string.IsNullOrEmpty(model.FilePath))
                return;
            if (!InFlight.TryAdd(model.FilePath, 0))
                return;

            int gen = Volatile.Read(ref _generation);
            _ = LoadBaseAsync(model, gen);
        }

        public static void EnqueueComparison(ComparisonImageModel model)
        {
            if (model == null || model.Thumbnail != null || string.IsNullOrEmpty(model.FilePath))
                return;
            if (!InFlight.TryAdd(model.FilePath, 0))
                return;

            int gen = Volatile.Read(ref _generation);
            _ = LoadComparisonAsync(model, gen);
        }

        private static async Task LoadBaseAsync(BaseImageModel model, int generation)
        {
            await Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (generation != Volatile.Read(ref _generation) || model.Thumbnail != null)
                    return;

                BitmapImage thumb;
                try
                {
                    thumb = await ThumbnailService.CreateThumbnailAsync(model.FilePath, 100).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                if (generation != Volatile.Read(ref _generation))
                    return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (generation == Volatile.Read(ref _generation) && model.Thumbnail == null)
                        model.Thumbnail = thumb;
                });
            }
            finally
            {
                InFlight.TryRemove(model.FilePath, out _);
                Gate.Release();
            }
        }

        private static async Task LoadComparisonAsync(ComparisonImageModel model, int generation)
        {
            await Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (generation != Volatile.Read(ref _generation) || model.Thumbnail != null)
                    return;

                BitmapImage thumb;
                try
                {
                    thumb = await ThumbnailService.CreateThumbnailAsync(model.FilePath, 100).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                if (generation != Volatile.Read(ref _generation))
                    return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (generation == Volatile.Read(ref _generation) && model.Thumbnail == null)
                        model.Thumbnail = thumb;
                });
            }
            finally
            {
                InFlight.TryRemove(model.FilePath, out _);
                Gate.Release();
            }
        }
    }
}
