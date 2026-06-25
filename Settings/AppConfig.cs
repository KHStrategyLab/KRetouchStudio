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
