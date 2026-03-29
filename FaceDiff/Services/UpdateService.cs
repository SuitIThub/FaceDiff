using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;
using FaceDiff.ViewModels;

namespace FaceDiff.Services
{
    public static class UpdateService
    {
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/SuitIThub/FaceDiff/releases/latest";

        private static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.UserAgent.ParseAdd("FaceDiff/" + GetCurrentVersion().ToString(3));
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            c.Timeout = TimeSpan.FromMinutes(15);
            return c;
        }

        private static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
        }

        private static bool TryParseTagVersion(string tagName, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tagName))
                return false;
            var s = tagName.Trim().TrimStart('v', 'V');
            return Version.TryParse(s, out version);
        }

        public static async Task CheckOnStartupAsync(Window owner, MainViewModel mainVm)
        {
            await Task.Delay(1500).ConfigureAwait(false);

            GitHubReleaseDto release;
            try
            {
                var json = await Http.GetStringAsync(LatestReleaseApiUrl).ConfigureAwait(false);
                release = JsonSerializer.Deserialize<GitHubReleaseDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return;
            }

            if (release?.Assets == null || release.Assets.Length == 0)
                return;

            if (!TryParseTagVersion(release.TagName, out var remoteVersion))
                return;

            var current = GetCurrentVersion();
            if (remoteVersion <= current)
                return;

            var settings = mainVm.Settings;
            if (!string.IsNullOrEmpty(settings.SuppressedUpdateVersion) &&
                Version.TryParse(settings.SuppressedUpdateVersion, out var suppressed) &&
                remoteVersion.Equals(suppressed))
                return;

            var zipAsset = release.Assets.FirstOrDefault(a =>
                a?.Name != null &&
                a.Name.StartsWith("FaceDiff-", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (zipAsset == null || string.IsNullOrEmpty(zipAsset.BrowserDownloadUrl))
                return;

            var answer = await owner.Dispatcher.InvokeAsync(() =>
                WinForms.MessageBox.Show(
                    new WindowInteropAdapter(owner),
                    $"A new version of FaceDiff is available ({remoteVersion}).\n\n" +
                    "Download and install it now? The application will close and restart.",
                    "FaceDiff update",
                    WinForms.MessageBoxButtons.YesNo,
                    WinForms.MessageBoxIcon.Question));

            if (answer != WinForms.DialogResult.Yes)
            {
                settings.SuppressedUpdateVersion = remoteVersion.ToString();
                mainVm.SaveSettings();
                return;
            }

            await owner.Dispatcher.InvokeAsync(() => { Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait; });

            try
            {
                await DownloadAndApplyAsync(zipAsset.BrowserDownloadUrl, owner).ConfigureAwait(false);
            }
            finally
            {
                await owner.Dispatcher.InvokeAsync(() => { Mouse.OverrideCursor = null; });
            }
        }

        private static async Task DownloadAndApplyAsync(string downloadUrl, Window owner)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "FaceDiff-update-" + Guid.NewGuid().ToString("N"));
            string zipPath = Path.Combine(tempRoot, "release.zip");
            Directory.CreateDirectory(tempRoot);

            try
            {
                using (var response = await Http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await response.Content.CopyToAsync(fs).ConfigureAwait(false);
                    }
                }

                string extractDir = Path.Combine(tempRoot, "extract");
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                string stagedExe = Path.Combine(extractDir, "FaceDiff.exe");
                if (!File.Exists(stagedExe))
                {
                    await owner.Dispatcher.InvokeAsync(() =>
                        WinForms.MessageBox.Show(new WindowInteropAdapter(owner),
                            "The downloaded update does not contain FaceDiff.exe. Update aborted.",
                            "FaceDiff update",
                            WinForms.MessageBoxButtons.OK,
                            WinForms.MessageBoxIcon.Warning));
                    return;
                }

                string appDir = Path.GetDirectoryName(Environment.ProcessPath);
                if (string.IsNullOrEmpty(appDir))
                {
                    await owner.Dispatcher.InvokeAsync(() =>
                        WinForms.MessageBox.Show(new WindowInteropAdapter(owner),
                            "Could not determine the application folder. Update aborted.",
                            "FaceDiff update",
                            WinForms.MessageBoxButtons.OK,
                            WinForms.MessageBoxIcon.Warning));
                    return;
                }

                string batchPath = Path.Combine(tempRoot, "FaceDiff-apply-update.bat");
                string batchBody = BuildBatchFile(appDir, extractDir);
                File.WriteAllText(batchPath, batchBody, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                var psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    WorkingDirectory = tempRoot
                };
                Process.Start(psi);

                await owner.Dispatcher.InvokeAsync(() => System.Windows.Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                await owner.Dispatcher.InvokeAsync(() =>
                    WinForms.MessageBox.Show(new WindowInteropAdapter(owner),
                        "Update failed:\n" + ex.Message,
                        "FaceDiff update",
                        WinForms.MessageBoxButtons.OK,
                        WinForms.MessageBoxIcon.Error));
            }
        }

        private static string BuildBatchFile(string targetDir, string stagingDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");
            sb.AppendLine("setlocal");
            sb.AppendLine($"set \"TARGET={EscapeBatchPath(targetDir)}\"");
            sb.AppendLine($"set \"STAGING={EscapeBatchPath(stagingDir)}\"");
            sb.AppendLine("timeout /t 3 /nobreak >nul");
            sb.AppendLine("xcopy /E /Y /I \"%STAGING%\\*\" \"%TARGET%\\\"");
            sb.AppendLine("if errorlevel 1 exit /b 1");
            sb.AppendLine("start \"\" \"%TARGET%\\FaceDiff.exe\"");
            sb.AppendLine("del \"%~f0\"");
            return sb.ToString();
        }

        private static string EscapeBatchPath(string path)
        {
            return path.Replace("%", "%%");
        }

        private sealed class WindowInteropAdapter : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;

            public WindowInteropAdapter(Window window)
            {
                _handle = new WindowInteropHelper(window).EnsureHandle();
            }

            public IntPtr Handle => _handle;
        }

        private sealed class GitHubReleaseDto
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }

            [JsonPropertyName("assets")]
            public GitHubAssetDto[] Assets { get; set; }
        }

        private sealed class GitHubAssetDto
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
