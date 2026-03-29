# FaceDiff

A WPF application for generating image diffs focused on facial regions. Compare sets of images against base images, with automatic face detection and oval-based region selection.

Repository: [github.com/SuitIThub/FaceDiff](https://github.com/SuitIThub/FaceDiff)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (for building)
- Windows 10 or later (x64)
- Visual Studio 2022 or another editor with C# support (optional; `dotnet` CLI is enough to build)

## NuGet dependencies

- **Emgu.CV** (4.9.0) — OpenCV wrapper for Haar cascade and DNN-based face detection
- **Emgu.CV.runtime.windows** — native OpenCV binaries
- **DlibDotNet** (19.21.0) — HOG-based face detection

## First run

On first launch, the application downloads face detection model files (~10 MB) to `%LocalAppData%\FaceDiff\models\`. An internet connection is required for that step.

User settings are stored under `%LocalAppData%\FaceDiff\` (for example `settings.json`).

## Updates

If a newer release is published on GitHub, FaceDiff can prompt to download the release zip and replace the current install folder (portable layout: `FaceDiff.exe` next to its dependencies). Choosing **No** on the prompt skips that version until a newer one appears.

## Workflow

### Step 1: Image selection

- Select folders for base and comparison images
- Apply substring filters to narrow down base images
- Use regex with capture groups to match comparison images to base images
- Matched pairs are highlighted with color coding

### Step 2: Face detection

- Automatic face detection using three algorithms (Haar, DNN SSD, Dlib HOG)
- The best result across all detectors is selected automatically
- Manual oval override for images where detection fails or needs adjustment
- Interactive oval editor with zoom-to-oval precision mode

### Step 3: Diff generation

- Set output destination folder
- Configure pixel difference threshold (0 = exact, up to 100)
- By default, diffs are saved for each base image without a confirmation step; enable **Confirm each base image before saving** to pause after each base for Accept or Deny
- Output is transparent PNG (only differing pixels within the oval are visible)

### Step 4: Finished

- Summary statistics (total, accepted, denied)
- Option to retry denied base images

### Step 5: Alignment

- Optional step to visually align a diff over a base/outfit preview and export positioning hints (e.g. for Ren’Py-style layer overrides)

## Building

From the repository root:

```bash
dotnet restore FaceDiff/FaceDiff.csproj
dotnet build FaceDiff/FaceDiff.csproj -c Release
```

The build output is under `FaceDiff/bin/Release/net8.0-windows/`.

## License

This project is licensed under the [MIT License](LICENSE).
