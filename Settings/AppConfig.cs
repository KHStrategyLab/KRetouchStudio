namespace KRetouchStudio;

public sealed class AppConfig
{
    public string? WorkAreaFolderPath { get; set; }

    public PhotoListSortMode PhotoListSortMode { get; set; } = PhotoListSortMode.NameAscending;

    public bool EnableAutoCheckUpdatesAtStartup { get; set; }

    public bool ShowHistoryPanel { get; set; } = true;

    public bool EnableEditorHistoryPersistence { get; set; } = true;

    public ColorManagementMode ColorManagementMode { get; set; } = ColorManagementMode.Automatic;

    public string? ManualDisplayColorProfilePath { get; set; }

    public CropPresetSettings CropPreset { get; set; } = new();

    public BackgroundSettings Background { get; set; } = new();

    public MediaPipeSettings MediaPipe { get; set; } = new();

    public ToolboxDefaultSettings ToolboxDefaults { get; set; } = new();
}

public sealed class ToolboxDefaultSettings
{
    public string BrushMode { get; set; } = "brush";
    public double BrushSize { get; set; } = 80;
    public double BrushSoftness { get; set; } = 50;
    public double BrushOpacity { get; set; } = 100;
    public bool ShowBrushCircle { get; set; } = true;

    public string FillToolMode { get; set; } = "bucket";
    public double FillToolOpacity { get; set; } = 100;

    public double EraserSize { get; set; } = 80;
    public double EraserSoftness { get; set; } = 50;
    public double EraserOpacity { get; set; } = 100;
    public bool ShowEraserCircle { get; set; } = true;

    public double StampSize { get; set; } = 80;
    public double StampSoftness { get; set; } = 50;
    public double StampOpacity { get; set; } = 100;
    public bool ShowStampCircle { get; set; } = true;

    public string HealingMode { get; set; } = "healing";
    public double HealingSize { get; set; } = 80;
    public double HealingSoftness { get; set; } = 50;
    public double HealingStrength { get; set; } = 50;
    public bool ShowHealingCircle { get; set; } = true;

    public string BlurSharpMode { get; set; } = "blur";
    public double BlurSharpSize { get; set; } = 80;
    public double BlurSharpSoftness { get; set; } = 50;
    public double BlurSharpStrength { get; set; } = 50;
    public double BlurSharpRadius { get; set; } = 8;
    public bool ShowBlurSharpCircle { get; set; } = true;

    public string DodgeBurnMode { get; set; } = "dodge";
    public double DodgeBurnSize { get; set; } = 96;
    public double DodgeBurnSoftness { get; set; } = 55;
    public double DodgeBurnStrength { get; set; } = 22;
    public bool ShowDodgeBurnCircle { get; set; } = true;

    public double HistoryBrushSize { get; set; } = 80;
    public double HistoryBrushSoftness { get; set; } = 50;
    public double HistoryBrushStrength { get; set; } = 50;
    public bool ShowHistoryBrushCircle { get; set; } = true;

    public double SampleRange { get; set; } = 5;

    public string MagicToolMode { get; set; } = "wand";
    public double MagicTolerance { get; set; } = 60;
    public double MagicSampleRange { get; set; } = 5;

    public double LiquifySize { get; set; } = 96;
    public double LiquifySoftness { get; set; } = 55;
    public double LiquifyStrength { get; set; } = 65;
    public bool ShowLiquifyCircle { get; set; } = true;
}

public sealed class BackgroundSettings
{
    public List<string> BackgroundImagePaths { get; set; } = new();

    public string? SelectedBackgroundImagePath { get; set; }
}

public sealed class CropPresetSettings
{
    public string SelectedPresetKey { get; set; } = "free";

    public double SavedWidth { get; set; } = 3.5;

    public double SavedHeight { get; set; } = 4.5;

    public string SavedUnit { get; set; } = "cm";

    public int SavedDpi { get; set; } = 300;
}

public sealed class MediaPipeSettings
{
    public bool EnableMediaPipeDetector { get; set; }

    public bool EnableMediaPipeSegmentation { get; set; }

    public bool EnableMediaPipeFacePose { get; set; }

    public string FaceDetectorModelPath { get; set; } = "Assets/AiModels/MediaPipe/face_detector.tflite";

    public string ImageSegmenterModelPath { get; set; } = "Assets/AiModels/MediaPipe/image_segmenter.tflite";

    public string FaceLandmarkerModelPath { get; set; } = "Assets/AiModels/MediaPipe/face_landmarker.task";

    public string HelperRuntime { get; set; } = "python";
}

public enum PhotoListSortMode
{
    NameAscending,
    NameDescending,
    DateNewestFirst,
    DateOldestFirst
}

public enum RuntimeWorkMode
{
    Viewer,
    Edit,
    Multi
}
