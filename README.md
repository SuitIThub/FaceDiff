# FaceDiff

A WPF application for generating image diffs focused on facial regions. Compare sets of images against base images, with automatic face detection and oval-based region selection.

## Requirements

- .NET Framework 4.8
- Windows 10 or later
- Visual Studio 2019+ (for building)

## NuGet Dependencies

- **Emgu.CV** (4.9.0) - OpenCV wrapper for Haar cascade and DNN-based face detection
- **Emgu.CV.runtime.windows** - Native OpenCV binaries
- **DlibDotNet** (19.21.0) - HOG-based face detection

## First Run

On first launch, the application will download face detection model files (~10MB) to `%LocalAppData%\FaceDiff\models\`. An internet connection is required for this step.

## Workflow

### Step 1: Image Selection
- Select folders for base and comparison images
- Apply substring filters to narrow down base images
- Use regex with capture groups to match comparison images to base images
- Matched pairs are highlighted with color coding

### Step 2: Face Detection
- Automatic face detection using three algorithms (Haar, DNN SSD, Dlib HOG)
- The best result across all detectors is selected automatically
- Manual oval override for images where detection fails or needs adjustment
- Interactive oval editor with zoom-to-oval precision mode

### Step 3: Diff Generation
- Set output destination folder
- Configure pixel difference threshold (0 = exact, up to 100)
- Review generated diff images per base image
- Accept to save diffs or deny to skip
- Output is transparent PNG (only differing pixels within the oval are visible)

### Step 4: Finished
- Summary statistics (total, accepted, denied)
- Option to retry denied base images

## Building

```
dotnet restore FaceDiff\FaceDiff.csproj
msbuild FaceDiff\FaceDiff.csproj /p:Configuration=Release
```