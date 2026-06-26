using Microsoft.Win32;
using KRetouchStudio.Tabs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private PhotoItem? _selectedPhoto;
    private ImageSource? _localWorkbenchImage;
    private ImageSource? _localWorkbenchApplyMaskImage;
    private ImageSource? _localWorkbenchProtectMaskImage;
    private ImageSource? _localWorkbenchBlockMaskImage;
    private ImageSource? _localWorkbenchWorkMaskImage;
    private string _localWorkbenchTitle = "Local Proxy Workbench";
    private string _localWorkbenchInfo = string.Empty;
    private string _localWorkbenchStatusText = "Preview only. Image changes are not applied yet.";
    private Visibility _localWorkbenchVisibility = Visibility.Collapsed;
    private LocalWorkbenchState? _localWorkbenchState;
    private double _localWorkbenchGuideLeft;
    private double _localWorkbenchGuideTop;
    private double _localWorkbenchGuideWidth;
    private double _localWorkbenchGuideHeight;
    private double _previewZoomPercent = 100;
    private double _previewImageLeft;
    private double _previewImageTop;
    private double _previewImageWidth;
    private double _previewImageHeight;
    private PhotoItem? _selectionAnchor;
    private AppConfig _appConfig = new();
    private static readonly TimeSpan WorkAreaRefreshCooldown = TimeSpan.FromSeconds(1);
    private const double PhotoListNormalWidth = 260;
    private const double PhotoListCompactWidth = 120;
    private const double PhotoListCompactStartWidth = 1280;
    private const double PhotoListCompactEndWidth = 900;
    private const double ToolboxSingleColumnWidth = 56;
    private const double ToolboxDoubleColumnWidth = 100;
    private const double ToolboxDoubleColumnMaxHeight = 1120;
    private DateTimeOffset _lastWorkAreaRefreshAt = DateTimeOffset.MinValue;
    private bool _isRefreshingWorkArea;
    private int _jpegSaveQuality = 12;
    private bool _jpegSaveEmbedColorProfile = true;
    private bool _isPhotoListPanelVisible = true;
    private bool _isEditModeUpperPrewarmQueued;
    private bool _isEditModeUpperPrewarmStarted;
    private string _activeToolId = "select";
    private enum ToolBucket
    {
        None,
        CursorResetOnLeave
    }

    private static readonly Dictionary<ToolBucket, HashSet<string>> ToolBuckets = new()
    {
        {
            ToolBucket.CursorResetOnLeave,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "brush","fill","eraser","lasso","stamp","healing","blursharp","dodgeburn","historybrush",
                "pathselect","rectangle","path","type","freetransform","liquify","hand","zoom","ruler","sampler","magic"
            }
        },
    };
    private static readonly string AppConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KRetouchStudio");
    private static readonly string AppConfigPath = Path.Combine(AppConfigDirectory, "config.json");
    private static readonly string EditorHistoryDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "EditorHistory");
    private static readonly string LegacyWorkAreaSettingsPath = Path.Combine(AppConfigDirectory, "work-area.txt");
    private System.Windows.Threading.DispatcherTimer? _toolboxDefaultsSaveTimer;
    private bool _isLoadingToolboxDefaults;
    private bool _isSpacePressed;
    private bool _isSinglePreviewPanDragging;
    private System.Windows.Point _singlePreviewPanStartPoint;
    private double _singlePreviewImageLeftStart;
    private double _singlePreviewImageTopStart;
    private PhotoItem? _draggingPreviewTile;
    private System.Windows.Point _previewTilePanStartPoint;
    private double _previewTilePanStartOffsetX;
    private double _previewTilePanStartOffsetY;
    private bool _isMultiPreviewGroupPanDragging;
    private readonly Dictionary<PhotoItem, (double X, double Y)> _groupPreviewPanStartOffsets = new();
    private bool _isFrameSelectionDragging;
    private bool _isFrameSelectionMoving;
    private System.Windows.Point _frameSelectionStartPoint;
    private System.Windows.Point _frameSelectionMoveStartPoint;
    private double _frameSelectionMoveStartLeft;
    private double _frameSelectionMoveStartTop;
    private double _frameSelectionLeft;
    private double _frameSelectionTop;
    private double _frameSelectionWidth;
    private double _frameSelectionHeight;
    private Visibility _frameSelectionVisibility = Visibility.Collapsed;
    private string _frameSelectionShape = "rectangle";
    private bool _isRectangleSelectionCreating;
    private bool _isRectangleSelectionMoving;
    private bool _isRectangleSelectionResizing;
    private bool _isRectangleSelectionRotating;
    private System.Windows.Point _rectangleSelectionStartPoint;
    private System.Windows.Point _rectangleSelectionMoveStartPoint;
    private RectangleSelectionHitZone _rectangleSelectionResizeHitZone = RectangleSelectionHitZone.None;
    private double _rectangleSelectionRotateStartAngle;
    private double _rectangleSelectionRotateStartRotationAngle;
    private double _rectangleSelectionLeft;
    private double _rectangleSelectionTop;
    private double _rectangleSelectionWidth;
    private double _rectangleSelectionHeight;
    private Visibility _rectangleSelectionVisibility = Visibility.Collapsed;
    private double _rectangleSelectionImageX;
    private double _rectangleSelectionImageY;
    private double _rectangleSelectionImageWidth;
    private double _rectangleSelectionImageHeight;
    private double _cropRotationAngle;
    private bool _cropRotateImageEnabled;
    private double _cropPresetWidth = 3.5;
    private double _cropPresetHeight = 4.5;
    private string _cropPresetUnit = "cm";
    private int _cropPresetDpi = 300;
    private double _cropRotateCursorLeft;
    private double _cropRotateCursorTop;
    private Visibility _cropRotateCursorVisibility = Visibility.Collapsed;
    private double _rectangleSelectionFeather;
    private int _rectanglePolygonSides = 4;
    private double _rectangleSelectionStartLeft;
    private double _rectangleSelectionStartTop;
    private double _rectangleSelectionStartWidth;
    private double _rectangleSelectionStartHeight;
    private ManualAnchorPoint? _draggingPathAnchorPoint;
    private double _pathToolFeather;
    private bool _pathToolClosed;
    private Geometry? _pathToolGeometry;
    private Visibility _pathToolVisibility = Visibility.Collapsed;
    private string _pathToolStatusText = "No path";
    private PreviewTextItem? _selectedTypeTextItem;
    private HistoryPanelItem? _selectedHistoryPanelItem;
    private PreviewTextItem? _draggingTypeTextItem;
    private PreviewTextItem? _creatingTypeTextItem;
    private bool _isTypeTextCreating;
    private bool _isTypeTextDragging;
    private bool _isTypeToolSettingsSyncing;
    private string _typeTextEditSnapshot = string.Empty;
    private System.Windows.Point _typeTextCreateStartPoint;
    private double _typeTextCreateStartImageX;
    private double _typeTextCreateStartImageY;
    private System.Windows.Point _typeTextDragStartPoint;
    private double _typeTextDragStartX;
    private double _typeTextDragStartY;
    private string _typeToolFontFamily = "Malgun Gothic";
    private double _typeToolFontSize = 36;
    private string _typeToolColorHex = "#FFFFFF";
    private System.Windows.Media.Brush _typeToolColorPreview = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
    private double _typeToolOpacity = 100;
    private bool _typeToolBold;
    private bool _typeToolItalic;
    private TextAlignment _typeToolTextAlignment = TextAlignment.Left;
    private string _typeToolStatusText = "No text";
    private double _brushSize = 80;
    private double _brushSoftness = 50;
    private bool _showBrushCircle = true;
    private System.Windows.Media.Brush _brushColorPreview = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 211, 90));
    private double _brushCircleLeft;
    private double _brushCircleTop;
    private double _brushCircleSize = 80;
    private Visibility _brushCircleVisibility = Visibility.Collapsed;
    private bool _isBrushDragging;
    private System.Windows.Point _brushLastImagePoint;
    private double _eraserSize = 80;
    private double _eraserSoftness = 50;
    private bool _showEraserCircle = true;
    private double _eraserCircleLeft;
    private double _eraserCircleTop;
    private double _eraserCircleSize = 80;
    private Visibility _eraserCircleVisibility = Visibility.Collapsed;
    private bool _isEraserDragging;
    private double _stampSize = 80;
    private double _stampSoftness = 50;
    private bool _showStampCircle = true;
    private string _stampSourceText = "Source: Not Set";
    private double _stampCircleLeft;
    private double _stampCircleTop;
    private double _stampCircleSize = 80;
    private Visibility _stampCircleVisibility = Visibility.Collapsed;
    private bool _isStampDragging;
    private bool _hasStampSource;
    private System.Windows.Point _stampSourceImagePoint;
    private System.Windows.Point _stampStrokeStartSourcePoint;
    private System.Windows.Point _stampStrokeStartTargetPoint;
    private System.Windows.Point _stampLastImagePoint;
    private BitmapSource? _stampSourceBitmap;
    private byte[]? _sourceCopyStrokeBasePixels;
    private byte[]? _sourceCopyStrokeCoverage;
    private int _sourceCopyStrokeStride;
    private int _sourceCopyStrokeWidth;
    private int _sourceCopyStrokeHeight;
    private double _healingSize = 80;
    private double _healingSoftness = 50;
    private double _healingStrength = 50;
    private bool _showHealingCircle = true;
    private double _healingCircleLeft;
    private double _healingCircleTop;
    private double _healingCircleSize = 80;
    private Visibility _healingCircleVisibility = Visibility.Collapsed;
    private string _healingMode = "healing";
    private bool _isHealingDragging;
    private bool _hasHealingSource;
    private System.Windows.Point _healingSourceImagePoint;
    private System.Windows.Point _healingStrokeStartSourcePoint;
    private System.Windows.Point _healingStrokeStartTargetPoint;
    private System.Windows.Point _healingLastImagePoint;
    private BitmapSource? _healingSourceBitmap;
    private string _blurSharpMode = "blur";
    private double _blurSharpSize = 80;
    private double _blurSharpSoftness = 50;
    private double _blurSharpStrength = 50;
    private double _blurSharpRadius = 8;
    private bool _showBlurSharpCircle = true;
    private double _blurSharpCircleLeft;
    private double _blurSharpCircleTop;
    private double _blurSharpCircleSize = 80;
    private Visibility _blurSharpCircleVisibility = Visibility.Collapsed;
    private bool _isBlurSharpDragging;
    private string _dodgeBurnMode = "dodge";
    private double _dodgeBurnSize = 96;
    private double _dodgeBurnSoftness = 55;
    private double _dodgeBurnStrength = 22;
    private bool _showDodgeBurnCircle = true;
    private double _dodgeBurnCircleLeft;
    private double _dodgeBurnCircleTop;
    private double _dodgeBurnCircleSize = 96;
    private Visibility _dodgeBurnCircleVisibility = Visibility.Collapsed;
    private string _dodgeBurnStatusText = "Ready";
    private bool _isDodgeBurnDragging;
    private System.Windows.Point _dodgeBurnLastImagePoint;
    private PhotoItem? _dodgeBurnSessionPhoto;
    private BitmapSource? _dodgeBurnSessionBaseImage;
    private WriteableBitmap? _dodgeBurnWorkingBitmap;
    private double _historyBrushSize = 80;
    private double _historyBrushSoftness = 50;
    private double _historyBrushStrength = 50;
    private bool _showHistoryBrushCircle = true;
    private double _historyBrushCircleLeft;
    private double _historyBrushCircleTop;
    private double _historyBrushCircleSize = 80;
    private Visibility _historyBrushCircleVisibility = Visibility.Collapsed;
    private bool _isHistoryBrushDragging;
    private bool _isZoomSelectionDragging;
    private bool _isTemporaryZoomSelectionDragging;
    private System.Windows.Point _zoomSelectionStartPoint;
    private double _zoomSelectionLeft;
    private double _zoomSelectionTop;
    private double _zoomSelectionWidth;
    private double _zoomSelectionHeight;
    private Visibility _zoomSelectionVisibility = Visibility.Collapsed;
    private bool _isRulerDragging;
    private System.Windows.Point _rulerStartPoint;
    private double _rulerX1;
    private double _rulerY1;
    private double _rulerX2;
    private double _rulerY2;
    private double _rulerLabelLeft;
    private double _rulerLabelTop;
    private string _rulerMeasurementText = "0 px / 0 deg";
    private Visibility _rulerVisibility = Visibility.Collapsed;
    private double _sampleRange = 5;
    private string _sampleResultText = "No sample";
    private System.Windows.Media.Brush _sampleColorPreview = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
    private double _sampleRegionLeft;
    private double _sampleRegionTop;
    private double _sampleRegionWidth;
    private double _sampleRegionHeight;
    private double _sampleLabelLeft;
    private double _sampleLabelTop;
    private Visibility _sampleRegionVisibility = Visibility.Collapsed;
    private double _magicTolerance = 60;
    private double _magicSampleRange = 5;
    private string _magicSelectionInfoText = "No selection";
    private ImageSource? _magicSelectionOverlayImage;
    private bool[]? _magicSelectionMask;
    private int _magicSelectionMaskWidth;
    private int _magicSelectionMaskHeight;
    private int _magicSelectionCount;
    private Visibility _magicSelectionVisibility = Visibility.Collapsed;
    private double _liquifySize = 96;
    private double _liquifySoftness = 55;
    private double _liquifyStrength = 65;
    private bool _showLiquifyCircle = true;
    private double _liquifyCircleLeft;
    private double _liquifyCircleTop;
    private double _liquifyCircleSize = 96;
    private Visibility _liquifyCircleVisibility = Visibility.Collapsed;
    private string _liquifyStatusText = "Ready";
    private bool _isLiquifyDragging;
    private System.Windows.Point _liquifyLastImagePoint;
    private PhotoItem? _liquifySessionPhoto;
    private BitmapSource? _liquifySessionBaseImage;
    private WriteableBitmap? _liquifyWorkingBitmap;
    private byte[]? _liquifyBasePixels;
    private int _liquifyBaseStride;
    private float[]? _liquifyMapX;
    private float[]? _liquifyMapY;
    private const int MaxEditorHistoryEntries = 40;
    private readonly List<EditorHistoryState> _editorUndoHistory = new();
    private readonly List<EditorHistoryState> _editorRedoHistory = new();
    private readonly Dictionary<string, EditorHistorySession> _editorHistorySessionsByPath = new(StringComparer.OrdinalIgnoreCase);
    private bool _isRestoringEditorHistory;
    private CancellationTokenSource? _toneCurvePreviewRenderCancellation;
    private int _toneCurvePreviewRenderVersion;
    private const int ToneCurveFastPreviewLongSide = 1200;
    private BitmapSource? _toneCurveFastPreviewBaseSource;
    private BitmapSource? _toneCurveFastPreviewSource;
    private const string DevelopmentPackagePassword = "1234";
    private const string ManualUpdateUrl = "https://drive.google.com/drive/folders/1ndHczaHgBvHan_S0isOOtIoJRZtDs2TK";
    private const string AutoUpdateVersionJsonUrl = "KRetouchStudio_update.json";
    private static readonly string RepositoryRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static readonly DevelopmentPackageService _developmentPackageService = new(RepositoryRootPath);
    private static readonly string[] SupportedImageExtensions = [".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".gif", ".raw"];
    private readonly object _workAreaWatcherSync = new();
    private readonly HashSet<string> _pendingWorkAreaImports = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selfSavedOutputPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _previewProxy1200BuildSync = new();
    private readonly Queue<PreviewProxy1200BuildItem> _previewProxy1200BuildQueue = new();
    private readonly HashSet<PhotoItem> _queuedPreviewProxy1200Photos = new();
    private FileSystemWatcher? _workAreaWatcher;
    private bool _isPreviewProxy1200BuildQueueRunning;
    private int _previewProxy1200BuildGeneration;

    public MainWindow()
        : this(Array.Empty<string>())
    {
    }

    public MainWindow(IReadOnlyList<string> startupImagePaths)
    {
        InitializeComponent();
#if DEBUG
        DevelopmentPackageSeparator.Visibility = Visibility.Visible;
        DevelopmentPackageMenuItem.Visibility = Visibility.Visible;
#else
        DevelopmentPackageSeparator.Visibility = Visibility.Collapsed;
        DevelopmentPackageMenuItem.Visibility = Visibility.Collapsed;
#endif
        DataContext = this;
        AddHandler(Expander.CollapsedEvent, new RoutedEventHandler(RetouchExpander_Collapsed));
        PhotoAdjustRetouchTab.CurvePreviewChanged += PhotoAdjustRetouchTab_CurvePreviewChanged;
        FaceShapeRetouchTab.FaceShapeAdjustmentCommitted += FaceShapeRetouchTab_FaceShapeAdjustmentCommitted;
        FaceShapeRetouchTab.FaceShapeControlAdjustmentPreviewChanged += FaceShapeRetouchTab_FaceShapeControlAdjustmentPreviewChanged;
        FaceShapeRetouchTab.SymmetrizeAdjustmentPreviewChanged += FaceShapeRetouchTab_SymmetrizeAdjustmentPreviewChanged;
        FaceShapeRetouchTab.HeadPoseAdjustmentPreviewChanged += FaceShapeRetouchTab_HeadPoseAdjustmentPreviewChanged;
        FaceShapeRetouchTab.HeadPoseAdjustmentCommitted += FaceShapeRetouchTab_FaceShapeAdjustmentCommitted;
        BackgroundRetouchTab.BackgroundTabOpened += BackgroundRetouchTab_BackgroundTabOpened;
        BackgroundRetouchTab.BackgroundReplacementRequested += BackgroundRetouchTab_BackgroundReplacementRequested;
        BackgroundRetouchTab.BackgroundReplacementAdjustmentCommitted += BackgroundRetouchTab_BackgroundReplacementAdjustmentCommitted;
        BackgroundRetouchTab.BackgroundReplacementPreviewChanged += BackgroundRetouchTab_BackgroundReplacementPreviewChanged;
        BackgroundRetouchTab.BackgroundImageImportRequested += BackgroundRetouchTab_BackgroundImageImportRequested;
        BackgroundRetouchTab.BackgroundImageSelected += BackgroundRetouchTab_BackgroundImageSelected;
        BackgroundRetouchTab.BackgroundImageRemoved += BackgroundRetouchTab_BackgroundImageRemoved;
        HistoryPanelItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HistoryPanelListVisibility));
            OnPropertyChanged(nameof(HistoryPanelEmptyVisibility));
            OnPropertyChanged(nameof(HistoryPanelStatusText));
        };
        LoadAppConfig();
        UpdateToolboxSelection();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        if (startupImagePaths.Count > 0)
        {
            AddPhotos(startupImagePaths);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PhotoItem> Photos { get; } = new();

    public ObservableCollection<PhotoItem> SelectedPreviewPhotos { get; } = new();

    public ObservableCollection<ManualAnchorPoint> PathAnchorPoints { get; } = new();

    public ObservableCollection<PreviewTextItem> TypeTextItems { get; } = new();

    public ObservableCollection<HistoryPanelItem> HistoryPanelItems { get; } = new();

    public PhotoItem? SelectedPhoto
    {
        get => _selectedPhoto;
        private set
        {
            if (ReferenceEquals(_selectedPhoto, value))
            {
                return;
            }

            PhotoItem? previousPhoto = _selectedPhoto;
            StoreCurrentEditorHistorySession(previousPhoto, persistToDisk: true);

            if (_selectedPhoto is not null)
            {
                _selectedPhoto.PropertyChanged -= SelectedPhoto_PropertyChanged;
            }

            _selectedPhoto = value;
            if (_selectedPhoto is not null)
            {
                _selectedPhoto.PropertyChanged += SelectedPhoto_PropertyChanged;
                QueuePreviewProxy1200Build(_selectedPhoto);
            }

            CancelToneCurvePreviewRender();
            ClearToneCurveFastPreviewCache();
            PhotoAdjustRetouchTab?.RefreshForPhoto(_selectedPhoto is null ? null : GetCurrentDisplayBitmapSource(_selectedPhoto));
            ResetNonBackgroundRetouchControlsForPhotoChange();
            ClearPhotoDependentToolState();
            ClearMagicSelection();
            ClearDodgeBurnSession(false);
            ClearLiquifySession(false);
            ClearFaceShapeSymmetrySession();
            ClearRectangleSelection();
            ClearPathTool();
            ClearTypeTextTool();
            ClearMediaPipePreviewOverlay();
            ClearBackgroundPreview();
            ResetBackgroundAdjustmentSliders();
            LoadEditorHistoryForSelectedPhoto();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SinglePreviewImageSource));
            OnPropertyChanged(nameof(PhotoSelectionText));
            OnPropertyChanged(nameof(SelectedPhotoStatusText));
            OnPropertyChanged(nameof(CanSaveCurrentPhoto));
            RaiseCropTelemetryPropertyChanged();
        }
    }

    private void ResetNonBackgroundRetouchControlsForPhotoChange()
    {
        SkinRetouchTab?.ResetForPhotoChange();
        WrinkleRetouchTab?.ResetForPhotoChange();
        FaceShapeRetouchTab?.ResetForPhotoChange();
        MouthRetouchTab?.ResetForPhotoChange();
        BodyRetouchTab?.ResetForPhotoChange();
        EyesRetouchTab?.ResetForPhotoChange();
        NoseRetouchTab?.ResetForPhotoChange();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeProjectionDebugOverlay();
    }

    private void ClearPhotoDependentToolState()
    {
        _isStampDragging = false;
        _hasStampSource = false;
        _stampSourceBitmap = null;
        StampSourceText = "Source: Not Set";

        _isHealingDragging = false;
        _hasHealingSource = false;
        _healingSourceBitmap = null;

        EndSourceCopyStroke();
        Mouse.Capture(null);
    }

    public ImageSource? SinglePreviewImageSource => SelectedPhoto is PhotoItem photo
        ? GetSinglePreviewBitmapSource(photo)
        : null;

    private BitmapSource GetSinglePreviewBitmapSource(PhotoItem photo)
    {
        if (IsCropSourcePreviewActive)
        {
            return GetCropSourceBitmapSource(photo);
        }

        if (TryGetFaceShapeHeadPoseDragPreviewBitmapSource(photo, out BitmapSource faceShapePreview))
        {
            return faceShapePreview;
        }

        return TryGetBackgroundPreviewBitmapSource(photo, out BitmapSource backgroundPreview)
            ? backgroundPreview
            : GetCurrentDisplayBitmapSource(photo);
    }

    private bool IsCropSourcePreviewActive =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        (RectangleSelectionVisibility == Visibility.Visible ||
         _isRectangleSelectionCreating ||
         _isRectangleSelectionMoving ||
         _isRectangleSelectionResizing ||
         _isRectangleSelectionRotating);

    private void SelectedPhoto_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            string.Equals(e.PropertyName, nameof(PhotoItem.Image), StringComparison.Ordinal))
        {
            ClearFaceShapeHeadPoseDragPreview();
            ClearBackgroundPreview();
            OnPropertyChanged(nameof(SinglePreviewImageSource));
            if (sender is PhotoItem photo)
            {
                QueuePreviewProxy1200Build(photo);
            }
        }
    }

    public string PhotoSelectionText => $"{SelectedPreviewPhotos.Count} / {Photos.Count} selected";

    public string PhotoListToggleText => _isPhotoListPanelVisible ? "목록 접기" : "목록 열기";

    public string ActiveToolId
    {
        get => _activeToolId;
        private set
        {
            bool wasTypeTool = string.Equals(_activeToolId, "type", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(_activeToolId, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (wasTypeTool && !string.Equals(value, "type", StringComparison.OrdinalIgnoreCase))
            {
                CommitTypeTextEdit();
            }

            _activeToolId = value;
            if (string.Equals(_activeToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(RectangleShapeMode, "rectangle", StringComparison.OrdinalIgnoreCase))
            {
                RectangleShapeMode = "rectangle";
                UpdateRectangleShapeModeSelection();
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(SinglePreviewImageSource));
            OnPropertyChanged(nameof(SelectToolOptionsVisibility));
            OnPropertyChanged(nameof(PathSelectionToolOptionsVisibility));
            OnPropertyChanged(nameof(CropToolOptionsVisibility));
            OnPropertyChanged(nameof(SinglePreviewImageRotationAngle));
            OnPropertyChanged(nameof(RectangleSelectionRotationAngle));
            OnPropertyChanged(nameof(FrameToolOptionsVisibility));
            OnPropertyChanged(nameof(LassoToolOptionsVisibility));
            OnPropertyChanged(nameof(RectangleToolOptionsVisibility));
            OnPropertyChanged(nameof(PathToolOptionsVisibility));
            OnPropertyChanged(nameof(TypeToolOptionsVisibility));
            OnPropertyChanged(nameof(FreeTransformToolOptionsVisibility));
            OnPropertyChanged(nameof(FreeTransformOverlayVisibility));
            OnPropertyChanged(nameof(BrushToolOptionsVisibility));
            OnPropertyChanged(nameof(FillToolOptionsVisibility));
            OnPropertyChanged(nameof(EraserToolOptionsVisibility));
            OnPropertyChanged(nameof(StampToolOptionsVisibility));
            OnPropertyChanged(nameof(HealingToolOptionsVisibility));
            OnPropertyChanged(nameof(BlurSharpToolOptionsVisibility));
            OnPropertyChanged(nameof(DodgeBurnToolOptionsVisibility));
            OnPropertyChanged(nameof(HistoryBrushToolOptionsVisibility));
            OnPropertyChanged(nameof(HandToolOptionsVisibility));
            OnPropertyChanged(nameof(ZoomToolOptionsVisibility));
            OnPropertyChanged(nameof(RulerToolOptionsVisibility));
            OnPropertyChanged(nameof(SamplerToolOptionsVisibility));
            OnPropertyChanged(nameof(MagicToolOptionsVisibility));
            OnPropertyChanged(nameof(LiquifyToolOptionsVisibility));
            HideCropRotateCursor();
            RaiseRectangleSelectionHandlePropertyChanged();
            RaiseCropTelemetryPropertyChanged();
            UpdateMagicSelectionVisibility();
            UpdateDodgeBurnCircleVisibility();
            UpdateLiquifyCircleVisibility();
            UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
            RebuildPathToolGeometry();
            UpdatePathToolStatus();
            UpdateTypeToolVisualState();
            UpdateTypeToolAlignmentSelection();
        }
    }

    public string AppVersionDisplayText => $"v{DevelopmentPackageService.FormatVersion(DevelopmentPackageService.NormalizeVersion(GetCurrentAppVersion()))}";

    public Visibility SinglePreviewVisibility => SelectedPreviewPhotos.Count == 1
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility MultiPreviewVisibility => SelectedPreviewPhotos.Count > 1
        ? Visibility.Visible
        : Visibility.Collapsed;

    private bool IsMultiPreviewToolLockActive => SelectedPreviewPhotos.Count > 1;

    public int PreviewGridColumns
    {
        get
        {
            int count = Math.Max(1, SelectedPreviewPhotos.Count);
            return count <= 1 ? 1 : (int)Math.Ceiling(Math.Sqrt(count));
        }
    }

    public string WorkAreaDisplayText => string.IsNullOrWhiteSpace(_appConfig.WorkAreaFolderPath)
        ? "Work Area: not pinned"
        : "Work Area: " + _appConfig.WorkAreaFolderPath;

    public bool IsColorManagementAutomatic => _appConfig.ColorManagementMode == ColorManagementMode.Automatic;

    public bool IsColorManagementManual => _appConfig.ColorManagementMode == ColorManagementMode.Manual;

    public bool IsColorManagementDisabled => _appConfig.ColorManagementMode == ColorManagementMode.Disabled;

    public bool CanClearManualColorManagementProfile => !string.IsNullOrWhiteSpace(_appConfig.ManualDisplayColorProfilePath);

    public string ManualColorManagementProfileText => string.IsNullOrWhiteSpace(_appConfig.ManualDisplayColorProfilePath)
        ? "Manual Profile: none"
        : "Manual Profile: " + Path.GetFileName(_appConfig.ManualDisplayColorProfilePath);

    public string ColorManagementStatusText => _appConfig.ColorManagementMode switch
    {
        ColorManagementMode.Manual when !string.IsNullOrWhiteSpace(_appConfig.ManualDisplayColorProfilePath)
            => "CM: Manual (" + Path.GetFileName(_appConfig.ManualDisplayColorProfilePath) + ")",
        ColorManagementMode.Manual => "CM: Manual",
        ColorManagementMode.Disabled => "CM: Disabled",
        _ => "CM: Automatic"
    };

    public string SelectedPhotoStatusText => SelectedPhoto is null
        ? ColorManagementStatusText
        : $"{SelectedPhoto.DisplayInfo} | {ColorManagementStatusText}";

    public bool AutoCheckUpdatesAtStartup
    {
        get => _appConfig.EnableAutoCheckUpdatesAtStartup;
        set
        {
            if (_appConfig.EnableAutoCheckUpdatesAtStartup == value)
            {
                return;
            }

            _appConfig.EnableAutoCheckUpdatesAtStartup = value;
            SaveAppConfig();
            OnPropertyChanged();
        }
    }

    public RuntimeWorkMode CurrentRuntimeWorkMode => ResolveRuntimeWorkMode();

    public string RuntimeWorkModeDisplayText => CurrentRuntimeWorkMode switch
    {
        RuntimeWorkMode.Viewer => "Current: Viewer Mode",
        RuntimeWorkMode.Edit => "Current: Edit Mode",
        RuntimeWorkMode.Multi => "Current: Multi Mode",
        _ => "Current: Unknown"
    };

    public bool RetouchPanelAvailable => SelectedPreviewPhotos.Count == 1;

    public bool RetouchPanelEditingEnabled => CurrentRuntimeWorkMode == RuntimeWorkMode.Edit;

    public bool CanSaveCurrentPhoto => SelectedPreviewPhotos.Count == 1 && SelectedPhoto is not null;

    public bool ShowHistoryPanel
    {
        get => _appConfig.ShowHistoryPanel;
        set
        {
            if (_appConfig.ShowHistoryPanel == value)
            {
                return;
            }

            _appConfig.ShowHistoryPanel = value;
            SaveAppConfig();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HistoryPanelVisibility));
        }
    }

    public bool EnableEditorHistoryPersistence
    {
        get => _appConfig.EnableEditorHistoryPersistence;
        set
        {
            if (_appConfig.EnableEditorHistoryPersistence == value)
            {
                return;
            }

            _appConfig.EnableEditorHistoryPersistence = value;
            SaveAppConfig();
            OnPropertyChanged();
        }
    }

    public Visibility HistoryPanelVisibility => ShowHistoryPanel
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility HistoryPanelListVisibility => HistoryPanelItems.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility HistoryPanelEmptyVisibility => HistoryPanelItems.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string HistoryPanelStatusText => HistoryPanelItems.Count == 0
        ? "No history yet"
        : HistoryPanelItems.Count == 1
            ? "1 step"
            : $"{HistoryPanelItems.Count} steps";

    public HistoryPanelItem? SelectedHistoryPanelItem
    {
        get => _selectedHistoryPanelItem;
        set
        {
            if (ReferenceEquals(_selectedHistoryPanelItem, value))
            {
                return;
            }

            _selectedHistoryPanelItem = value;
            OnPropertyChanged();
        }
    }

    public double PreviewZoomPercent
    {
        get => _previewZoomPercent;
        set
        {
            double clamped = Math.Clamp(value, 25, 300);
            if (Math.Abs(_previewZoomPercent - clamped) < 0.01)
            {
                return;
            }

            _previewZoomPercent = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewZoomDisplayText));
            OnPropertyChanged(nameof(PreviewZoomScale));
            UpdatePreviewLayout();
        }
    }

    public string PreviewZoomDisplayText => $"{PreviewZoomPercent:0}%";

    public double PreviewZoomScale => PreviewZoomPercent / 100.0;

    public double PreviewImageLeft
    {
        get => _previewImageLeft;
        private set
        {
            _previewImageLeft = value;
            OnPropertyChanged();
        }
    }

    public double PreviewImageTop
    {
        get => _previewImageTop;
        private set
        {
            _previewImageTop = value;
            OnPropertyChanged();
        }
    }

    public double PreviewImageWidth
    {
        get => _previewImageWidth;
        private set
        {
            _previewImageWidth = value;
            OnPropertyChanged();
        }
    }

    public double PreviewImageHeight
    {
        get => _previewImageHeight;
        private set
        {
            _previewImageHeight = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? LocalWorkbenchImage
    {
        get => _localWorkbenchImage;
        private set
        {
            _localWorkbenchImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? LocalWorkbenchApplyMaskImage
    {
        get => _localWorkbenchApplyMaskImage;
        private set
        {
            _localWorkbenchApplyMaskImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? LocalWorkbenchProtectMaskImage
    {
        get => _localWorkbenchProtectMaskImage;
        private set
        {
            _localWorkbenchProtectMaskImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? LocalWorkbenchBlockMaskImage
    {
        get => _localWorkbenchBlockMaskImage;
        private set
        {
            _localWorkbenchBlockMaskImage = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? LocalWorkbenchWorkMaskImage
    {
        get => _localWorkbenchWorkMaskImage;
        private set
        {
            _localWorkbenchWorkMaskImage = value;
            OnPropertyChanged();
        }
    }

    public string LocalWorkbenchTitle
    {
        get => _localWorkbenchTitle;
        private set
        {
            _localWorkbenchTitle = value;
            OnPropertyChanged();
        }
    }

    public string LocalWorkbenchInfo
    {
        get => _localWorkbenchInfo;
        private set
        {
            _localWorkbenchInfo = value;
            OnPropertyChanged();
        }
    }

    public string LocalWorkbenchStatusText
    {
        get => _localWorkbenchStatusText;
        private set
        {
            _localWorkbenchStatusText = value;
            OnPropertyChanged();
        }
    }

    public Visibility LocalWorkbenchVisibility
    {
        get => _localWorkbenchVisibility;
        private set
        {
            _localWorkbenchVisibility = value;
            OnPropertyChanged();
        }
    }

    public double LocalWorkbenchGuideLeft
    {
        get => _localWorkbenchGuideLeft;
        private set
        {
            _localWorkbenchGuideLeft = value;
            OnPropertyChanged();
        }
    }

    public double LocalWorkbenchGuideTop
    {
        get => _localWorkbenchGuideTop;
        private set
        {
            _localWorkbenchGuideTop = value;
            OnPropertyChanged();
        }
    }

    public double LocalWorkbenchGuideWidth
    {
        get => _localWorkbenchGuideWidth;
        private set
        {
            _localWorkbenchGuideWidth = value;
            OnPropertyChanged();
        }
    }

    public double LocalWorkbenchGuideHeight
    {
        get => _localWorkbenchGuideHeight;
        private set
        {
            _localWorkbenchGuideHeight = value;
            OnPropertyChanged();
        }
    }

    private void LoadPhotosButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp;*.gif;*.raw|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddPhotos(dialog.FileNames);
        }
    }

    private void SavePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPhotoToProtectedOutput();
    }

    private void SavePhotoAsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPhotoWithDialog();
    }

    private void SaveCurrentPhotoToProtectedOutput()
    {
        if (!CanSaveCurrentPhoto ||
            SelectedPhoto is not PhotoItem photo ||
            GetCurrentSaveBitmapSource(photo) is not BitmapSource image)
        {
            return;
        }

        string? sourceDirectory = Path.GetDirectoryName(photo.Path);
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return;
        }

        string sourceExtension = Path.GetExtension(photo.Path);
        string outputExtension = GetSaveOutputExtension(sourceExtension);
        string outputPath = GetNextAvailableSavePath(
            sourceDirectory,
            Path.GetFileNameWithoutExtension(photo.FileName),
            outputExtension);

        try
        {
            MarkSelfSavedOutputPath(outputPath);
            BitmapEncoder encoder = CreateSaveEncoder(outputExtension, jpegQualityLevel: 100);
            encoder.Frames.Add(CreateSrgbSaveFrame(image, outputExtension, embedColorProfile: true));
            using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            UnmarkSelfSavedOutputPath(outputPath);
            System.Windows.MessageBox.Show(this, $"저장 실패: {ex.Message}", "저장", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveCurrentPhotoWithDialog()
    {
        if (!CanSaveCurrentPhoto ||
            SelectedPhoto is not PhotoItem photo ||
            GetCurrentSaveBitmapSource(photo) is not BitmapSource image)
        {
            return;
        }

        string sourceExtension = Path.GetExtension(photo.Path);
        string outputExtension = GetSaveOutputExtension(sourceExtension);
        Microsoft.Win32.SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = outputExtension,
            FileName = $"{Path.GetFileNameWithoutExtension(photo.FileName)}{outputExtension}",
            Filter = "JPEG image|*.jpg;*.jpeg|PNG image|*.png|All files|*.*",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string outputPath = dialog.FileName;
        string selectedExtension = Path.GetExtension(outputPath);
        outputExtension = GetSaveOutputExtension(selectedExtension);
        if (!IsSaveOutputExtension(selectedExtension))
        {
            outputPath = Path.ChangeExtension(outputPath, outputExtension);
        }

        int jpegQualityLevel = 100;
        bool embedColorProfile = true;
        if (IsJpegExtension(outputExtension) &&
            !TryShowJpegSaveOptionsDialog(out jpegQualityLevel, out embedColorProfile))
        {
            return;
        }

        try
        {
            MarkSelfSavedOutputPath(outputPath);
            BitmapEncoder encoder = CreateSaveEncoder(outputExtension, jpegQualityLevel);
            encoder.Frames.Add(CreateSrgbSaveFrame(image, outputExtension, embedColorProfile));
            using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            UnmarkSelfSavedOutputPath(outputPath);
            System.Windows.MessageBox.Show(this, $"저장 실패: {ex.Message}", "Save As", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private BitmapSource? GetCurrentSaveBitmapSource(PhotoItem photo)
    {
        return TryGetBackgroundPreviewBitmapSource(photo, out BitmapSource backgroundPreview)
            ? backgroundPreview
            : photo.Image as BitmapSource;
    }

    private static string GetSaveOutputExtension(string sourceExtension)
    {
        return sourceExtension.ToLowerInvariant() switch
        {
            ".jpg" => ".jpg",
            ".jpeg" => ".jpeg",
            ".png" => ".png",
            _ => ".png"
        };
    }

    private static string GetNextAvailableSavePath(string directory, string baseFileName, string outputExtension)
    {
        string safeBaseFileName = string.IsNullOrWhiteSpace(baseFileName)
            ? "image"
            : baseFileName.Trim();

        for (int suffix = 1; suffix < 10000; suffix++)
        {
            string candidatePath = Path.Combine(directory, $"{safeBaseFileName}_{suffix}{outputExtension}");
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new IOException("save_output_name_exhausted");
    }

    private bool TryShowJpegSaveOptionsDialog(out int jpegQualityLevel, out bool embedColorProfile)
    {
        int selectedQuality = Math.Clamp(_jpegSaveQuality, 1, 12);
        bool selectedEmbedProfile = _jpegSaveEmbedColorProfile;

        Window dialog = new()
        {
            Title = "JPEG Options",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Background = (System.Windows.Media.Brush)FindResource("SurfaceSecondary")
        };

        System.Windows.Controls.TextBlock qualityValueText = new()
        {
            Text = selectedQuality.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Foreground = (System.Windows.Media.Brush)FindResource("TextMain"),
            Width = 28,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        System.Windows.Controls.Slider qualitySlider = new()
        {
            Minimum = 1,
            Maximum = 12,
            Value = selectedQuality,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Width = 180,
            Margin = new Thickness(10, 0, 0, 0)
        };
        qualitySlider.ValueChanged += (_, _) =>
        {
            selectedQuality = Math.Clamp((int)Math.Round(qualitySlider.Value), 1, 12);
            qualityValueText.Text = selectedQuality.ToString(System.Globalization.CultureInfo.InvariantCulture);
        };

        System.Windows.Controls.CheckBox embedProfileCheckBox = new()
        {
            Content = "Embed ICC Profile: sRGB IEC61966-2.1",
            IsChecked = selectedEmbedProfile,
            Foreground = (System.Windows.Media.Brush)FindResource("TextMain"),
            Margin = new Thickness(0, 14, 0, 0)
        };

        System.Windows.Controls.Button okButton = new()
        {
            Content = "OK",
            IsDefault = true,
            MinWidth = 72,
            Margin = new Thickness(0, 16, 6, 0)
        };
        System.Windows.Controls.Button cancelButton = new()
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 72,
            Margin = new Thickness(6, 16, 0, 0)
        };

        System.Windows.Controls.StackPanel qualityRow = new()
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };
        qualityRow.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Quality",
            Foreground = (System.Windows.Media.Brush)FindResource("TextMain"),
            Width = 72,
            VerticalAlignment = VerticalAlignment.Center
        });
        qualityRow.Children.Add(qualityValueText);
        qualityRow.Children.Add(qualitySlider);

        System.Windows.Controls.StackPanel buttonRow = new()
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        buttonRow.Children.Add(okButton);
        buttonRow.Children.Add(cancelButton);

        System.Windows.Controls.StackPanel panel = new()
        {
            Margin = new Thickness(16),
            MinWidth = 330
        };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Image Options",
            FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)FindResource("TextMain"),
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(qualityRow);
        panel.Children.Add(embedProfileCheckBox);
        panel.Children.Add(buttonRow);

        dialog.Content = panel;
        okButton.Click += (_, _) => dialog.DialogResult = true;

        if (dialog.ShowDialog() != true)
        {
            jpegQualityLevel = 100;
            embedColorProfile = true;
            return false;
        }

        _jpegSaveQuality = selectedQuality;
        _jpegSaveEmbedColorProfile = embedProfileCheckBox.IsChecked == true;
        jpegQualityLevel = ConvertJpegQualityToEncoderLevel(_jpegSaveQuality);
        embedColorProfile = _jpegSaveEmbedColorProfile;
        return true;
    }

    private static int ConvertJpegQualityToEncoderLevel(int quality)
    {
        int clamped = Math.Clamp(quality, 1, 12);
        return Math.Clamp((int)Math.Round(clamped / 12.0 * 100.0), 1, 100);
    }

    private static BitmapEncoder CreateSaveEncoder(string outputExtension, int jpegQualityLevel)
    {
        if (outputExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            outputExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return new JpegBitmapEncoder
            {
                QualityLevel = Math.Clamp(jpegQualityLevel, 1, 100)
            };
        }

        return new PngBitmapEncoder();
    }

    private static BitmapFrame CreateSrgbSaveFrame(BitmapSource image, string outputExtension, bool embedColorProfile)
    {
        PixelFormat outputFormat = IsJpegExtension(outputExtension)
            ? PixelFormats.Rgb24
            : PixelFormats.Bgra32;
        BitmapSource outputImage = EnsureBitmapFormat(image, outputFormat);
        ReadOnlyCollection<ColorContext>? colorContexts = embedColorProfile
            ? new ReadOnlyCollection<ColorContext>(new[] { new ColorContext(outputFormat) })
            : null;
        return BitmapFrame.Create(outputImage, null, null, colorContexts);
    }

    private static bool IsJpegExtension(string extension)
    {
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSaveOutputExtension(string extension)
    {
        return IsJpegExtension(extension) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }

    private void WorkAreaMenuItem_Click(object sender, RoutedEventArgs e)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new()
        {
            Description = "작업폴더를 선택해줘.",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_appConfig.WorkAreaFolderPath) ? _appConfig.WorkAreaFolderPath : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        PinWorkArea(dialog.SelectedPath);
    }

    private void WorkAreaDisplayText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new()
        {
            Description = "작업폴더를 변경해줘.",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_appConfig.WorkAreaFolderPath)
                ? _appConfig.WorkAreaFolderPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        if (string.Equals(dialog.SelectedPath, _appConfig.WorkAreaFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PinWorkArea(dialog.SelectedPath);
    }

    private void RefreshWorkAreaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshingWorkArea)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastWorkAreaRefreshAt < WorkAreaRefreshCooldown)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_appConfig.WorkAreaFolderPath) || !Directory.Exists(_appConfig.WorkAreaFolderPath))
        {
            return;
        }

        try
        {
            _isRefreshingWorkArea = true;
            _lastWorkAreaRefreshAt = now;
            PruneEditorHistoryOutsideWorkArea(_appConfig.WorkAreaFolderPath);
            LoadPhotosFromWorkArea(_appConfig.WorkAreaFolderPath);
        }
        finally
        {
            _isRefreshingWorkArea = false;
        }
    }

    private void TogglePhotoListButton_Click(object sender, RoutedEventArgs e)
    {
        _isPhotoListPanelVisible = !_isPhotoListPanelVisible;
        ApplyPhotoListPanelState();
    }

    private void ApplyPhotoListPanelState()
    {
        double toolboxWidth = GetResponsiveToolboxWidth();
        ToolboxColumn.Width = new GridLength(toolboxWidth);
        PhotoListColumn.Width = _isPhotoListPanelVisible
            ? new GridLength(GetResponsivePhotoListWidth(toolboxWidth))
            : new GridLength(0);
        PhotoListPanel.Visibility = _isPhotoListPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        OnPropertyChanged(nameof(PhotoListToggleText));
        UpdatePreviewLayout();
    }

    private double GetResponsiveToolboxWidth()
    {
        double windowHeight = ActualHeight;
        if (double.IsNaN(windowHeight) || windowHeight <= 0)
        {
            return ToolboxSingleColumnWidth;
        }

        return windowHeight < ToolboxDoubleColumnMaxHeight
            ? ToolboxDoubleColumnWidth
            : ToolboxSingleColumnWidth;
    }

    private double GetResponsivePhotoListWidth(double toolboxWidth)
    {
        double windowWidth = ActualWidth;
        if (double.IsNaN(windowWidth) || windowWidth <= 0)
        {
            return PhotoListNormalWidth;
        }

        if (windowWidth >= PhotoListCompactStartWidth)
        {
            return PhotoListNormalWidth;
        }

        if (windowWidth <= PhotoListCompactEndWidth)
        {
            return PhotoListCompactWidth;
        }

        double t = (windowWidth - PhotoListCompactEndWidth) / (PhotoListCompactStartWidth - PhotoListCompactEndWidth);
        double baseWidth = PhotoListCompactWidth + (PhotoListNormalWidth - PhotoListCompactWidth) * t;
        double toolboxExtraWidth = Math.Max(0, toolboxWidth - ToolboxSingleColumnWidth);
        return Math.Max(PhotoListCompactWidth, baseWidth - toolboxExtraWidth);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyPhotoListPanelState();
    }

    private void ToolboxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string toolId || string.IsNullOrWhiteSpace(toolId))
        {
            return;
        }

        ActivateToolById(toolId);
        e.Handled = true;
    }

    private void ActivateToolById(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            return;
        }

        if (IsMultiPreviewToolLockActive &&
            !string.Equals(toolId, "hand", StringComparison.OrdinalIgnoreCase))
        {
            toolId = "hand";
        }

        if (string.Equals(toolId, "crop", StringComparison.OrdinalIgnoreCase))
        {
            RectangleShapeMode = "rectangle";
        }

        ActiveToolId = toolId;
        UpdatePreviewImageFrame();
        if (string.Equals(toolId, "freetransform", StringComparison.OrdinalIgnoreCase))
        {
            ResetFreeTransformShell();
        }

        UpdateToolboxSelection();
        UpdateFrameSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
        UpdateBrushCircleVisibility();
        UpdateEraserCircleVisibility();
        UpdateStampCircleVisibility();
        UpdateHealingCircleVisibility();
        UpdateBlurSharpCircleVisibility();
        UpdateDodgeBurnCircleVisibility();
        UpdateHistoryBrushCircleVisibility();
        UpdateLiquifyCircleVisibility();
    }

    private void LiquifyResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetLiquifyPreview();
    }

    private void UpdateToolboxSelection()
    {
        foreach (System.Windows.Controls.Button button in GetToolboxButtons())
        {
            string? toolId = button.Tag as string;
            bool isActive = toolId is not null &&
                            string.Equals(toolId, ActiveToolId, StringComparison.OrdinalIgnoreCase);
            bool isLockedOut = IsMultiPreviewToolLockActive &&
                               !string.Equals(toolId, "hand", StringComparison.OrdinalIgnoreCase);
            button.IsEnabled = !isLockedOut;
            button.Background = isActive
                ? (System.Windows.Media.Brush)FindResource("PanelSelectedBg")
                : (System.Windows.Media.Brush)FindResource("SurfacePrimary");
            button.BorderBrush = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("MenuBorder");
            button.Foreground = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("TextMain");
            button.Opacity = isLockedOut ? 0.42 : 1.0;
        }

        UpdateFrameShapeSelection();
        UpdateTypeToolAlignmentSelection();
        UpdatePathSelectionModeSelection();
        UpdateRectangleShapeModeSelection();
        UpdateBrushModeSelection();
        UpdateFillModeSelection();
        UpdateHealingModeSelection();
        UpdateBlurSharpModeSelection();
        UpdateMagicModeSelection();
        UpdateDodgeBurnModeSelection();
    }

    private IEnumerable<System.Windows.Controls.Button> GetToolboxButtons()
    {
        yield return SelectToolButton;
        yield return PathSelectionToolButton;
        yield return CropToolButton;
        yield return FrameToolButton;
        yield return LassoToolButton;
        yield return RectangleToolButton;
        yield return PathToolButton;
        yield return TypeToolButton;
        yield return FreeTransformToolButton;
        yield return BrushToolButton;
        yield return FillToolButton;
        yield return EraserToolButton;
        yield return StampToolButton;
        yield return HealingToolButton;
        yield return BlurSharpToolButton;
        yield return DodgeBurnToolButton;
        yield return HistoryBrushToolButton;
        yield return HandToolButton;
        yield return ZoomToolButton;
        yield return RulerToolButton;
        yield return SamplerToolButton;
        yield return MagicToolButton;
        yield return LiquifyToolButton;
    }

    private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] droppedItems)
        {
            return;
        }

        string[] imagePaths = GetDroppedImagePaths(droppedItems).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (imagePaths.Length == 0)
        {
            return;
        }

        int previousPhotoCount = Photos.Count;
        AddPhotos(imagePaths);
        if (Photos.Count > previousPhotoCount)
        {
            SelectOnly(Photos[previousPhotoCount]);
        }

        e.Handled = true;
    }

    private IEnumerable<string> GetDroppedImagePaths(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (File.Exists(path) && IsSupportedImagePath(path))
            {
                yield return path;
            }
        }
    }

    private static bool IsSupportedImagePath(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private void SortNameAscendingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SetPhotoListSortMode(PhotoListSortMode.NameAscending);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!AutoCheckUpdatesAtStartup)
        {
            return;
        }

        try
        {
            await CheckForUpdatesAsync();
        }
        catch
        {
        }
    }

    private async Task<bool> CheckForUpdatesAsync()
    {
        if (string.IsNullOrWhiteSpace(AutoUpdateVersionJsonUrl))
        {
            return false;
        }

        try
        {
            string json;
            if (Uri.TryCreate(AutoUpdateVersionJsonUrl, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using HttpClient client = new();
                using HttpResponseMessage response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                json = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string manifestPath = Path.IsPathRooted(AutoUpdateVersionJsonUrl)
                    ? AutoUpdateVersionJsonUrl
                    : Path.Combine(AppContext.BaseDirectory, AutoUpdateVersionJsonUrl);
                if (!File.Exists(manifestPath))
                {
                    return false;
                }

                json = await File.ReadAllTextAsync(manifestPath);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            AutoUpdateManifest? manifest = JsonSerializer.Deserialize<AutoUpdateManifest>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.LatestVersion))
            {
                return false;
            }

            string normalizedVersion = manifest.LatestVersion.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(normalizedVersion, out Version? latestVersion))
            {
                return false;
            }

            Version currentVersion = GetCurrentAppVersion();
            if (latestVersion <= currentVersion)
            {
                return false;
            }

            string notes = string.IsNullOrWhiteSpace(manifest.ReleaseNotes)
                ? string.Empty
                : $"{Environment.NewLine}{manifest.ReleaseNotes}";
            string message = $"현재 버전: {currentVersion}\n최신 버전: {manifest.LatestVersion}{notes}\n\n업데이트 파일을 지금 열까요?";
            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                message,
                "업데이트 알림",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                return false;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = manifest.DownloadUrl,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Version GetCurrentAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    }

    private sealed class AutoUpdateManifest
    {
        [JsonPropertyName("latestVersion")]
        public string? LatestVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("releaseNotes")]
        public string? ReleaseNotes { get; set; }
    }

    private void SortNameDescendingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SetPhotoListSortMode(PhotoListSortMode.NameDescending);
    }

    private void SortNewestFirstMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SetPhotoListSortMode(PhotoListSortMode.DateNewestFirst);
    }

    private void SortOldestFirstMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SetPhotoListSortMode(PhotoListSortMode.DateOldestFirst);
    }

    private void ColorManagementAutomaticMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ApplyColorManagementSettings(ColorManagementMode.Automatic);
    }

    private void ColorManagementManualMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string? manualProfilePath = _appConfig.ManualDisplayColorProfilePath;
        if (string.IsNullOrWhiteSpace(manualProfilePath) || !File.Exists(manualProfilePath))
        {
            manualProfilePath = PromptForColorManagementProfilePath();
            if (string.IsNullOrWhiteSpace(manualProfilePath))
            {
                RaiseColorManagementPropertyChanged();
                return;
            }
        }

        ApplyColorManagementSettings(ColorManagementMode.Manual, manualProfilePath);
    }

    private void LoadColorManagementProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string? manualProfilePath = PromptForColorManagementProfilePath();
        if (string.IsNullOrWhiteSpace(manualProfilePath))
        {
            return;
        }

        ApplyColorManagementSettings(ColorManagementMode.Manual, manualProfilePath);
    }

    private void ColorManagementDisabledMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ApplyColorManagementSettings(ColorManagementMode.Disabled);
    }

    private void ClearColorManagementProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_appConfig.ManualDisplayColorProfilePath))
        {
            return;
        }

        ColorManagementMode previousMode = _appConfig.ColorManagementMode;
        string? previousManualProfilePath = _appConfig.ManualDisplayColorProfilePath;

        _appConfig.ManualDisplayColorProfilePath = null;
        if (_appConfig.ColorManagementMode == ColorManagementMode.Manual)
        {
            _appConfig.ColorManagementMode = ColorManagementMode.Automatic;
        }

        SyncColorManagementSettingsFromConfig();
        SaveAppConfig();
        RaiseColorManagementPropertyChanged();

        if (previousMode != _appConfig.ColorManagementMode ||
            !string.Equals(previousManualProfilePath, _appConfig.ManualDisplayColorProfilePath, StringComparison.OrdinalIgnoreCase))
        {
            ReloadPhotosForColorManagement();
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AutoCheckUpdatesAtStartupMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            AutoCheckUpdatesAtStartup = menuItem.IsChecked;
        }
    }

    private void HistoryPanelMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            ShowHistoryPanel = menuItem.IsChecked;
        }
    }

    private void EditorHistoryPersistenceMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem)
        {
            EnableEditorHistoryPersistence = menuItem.IsChecked;
        }
    }

    private async void UpdatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        bool updateOpened = false;
        try
        {
            updateOpened = await CheckForUpdatesAsync();
        }
        catch
        {
        }

        if (updateOpened)
        {
            return;
        }

        MessageBoxResult result = System.Windows.MessageBox.Show(
            this,
            "최신 버전을 확인했거나 네트워크 상태에 따라 확인이 실패했을 수 있습니다.\n업데이트 페이지를 여시겠습니까?",
            "Update",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ManualUpdateUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            System.Windows.MessageBox.Show(this, "업데이트 페이지를 열 수 없습니다.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DevelopmentPackageMenuItem_Click(object sender, RoutedEventArgs e)
    {
#if !DEBUG
        return;
#else
        if (!ConfirmDevelopmentPackagePassword())
        {
            return;
        }

        try
        {
            string nextVersion = _developmentPackageService.GetNextDevelopmentPackageVersion(GetCurrentAppVersion());

            MessageBoxResult result = System.Windows.MessageBox.Show(
                this,
                $"Version {nextVersion} is ready.\nThe app will close, build, package, and reopen automatically.",
                "Dev Package",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
            {
                return;
            }

            _developmentPackageService.PrepareDevelopmentPackageVersion(nextVersion);
            string helperScriptPath = _developmentPackageService.WriteDevelopmentPackageHelperScript(
                Environment.ProcessId,
                Path.Combine(AppContext.BaseDirectory, "KRetouchStudio.exe"));

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{helperScriptPath}\"",
                UseShellExecute = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                this,
                $"Development package failed to start.\n{ex.Message}",
                "Dev Package",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
#endif
    }

    private bool ConfirmDevelopmentPackagePassword()
    {
        PasswordBox passwordBox = new()
        {
            Width = 180,
            Margin = new Thickness(0, 6, 0, 0)
        };

        System.Windows.Controls.Button okButton = new()
        {
            Content = "OK",
            IsDefault = true,
            MinWidth = 72,
            Margin = new Thickness(0, 12, 6, 0)
        };

        System.Windows.Controls.Button cancelButton = new()
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 72,
            Margin = new Thickness(6, 12, 0, 0)
        };

        StackPanel buttonPanel = new()
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        StackPanel panel = new()
        {
            Margin = new Thickness(16)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "Enter development package password.",
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(passwordBox);
        panel.Children.Add(buttonPanel);

        Window passwordDialog = new()
        {
            Title = "Dev Package",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = panel
        };

        okButton.Click += (_, _) => passwordDialog.DialogResult = true;
        passwordDialog.Loaded += (_, _) => passwordBox.Focus();

        if (passwordDialog.ShowDialog() != true)
        {
            return false;
        }

        if (string.Equals(passwordBox.Password, DevelopmentPackagePassword, StringComparison.Ordinal))
        {
            return true;
        }

        System.Windows.MessageBox.Show(
            this,
            "Wrong development package password.",
            "Dev Package",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = true;
        }

        if (e.Key == Key.Escape)
        {
            if (CanUseTypeTool() && _selectedTypeTextItem?.IsEditing == true)
            {
                CancelTypeTextEdit();
                e.Handled = true;
                return;
            }

            if (CanUsePathTool() && PathAnchorPoints.Count > 0)
            {
                ClearPathTool();
                e.Handled = true;
                return;
            }

            if (TryCancelCropSelectionInput())
            {
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Enter)
        {
            if (CanUseTypeTool() && _selectedTypeTextItem?.IsEditing == true)
            {
                CommitTypeTextEdit();
                e.Handled = true;
                return;
            }

            if (CanUsePathTool() && PathAnchorPoints.Count >= 3)
            {
                PathToolClosed = true;
                e.Handled = true;
                return;
            }

            if (string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
                !IsCropToolSettingInputFocused() &&
                TryApplyRectangleSelection())
            {
                e.Handled = true;
                return;
            }

            if (TryApplyRectangleSelection())
            {
                e.Handled = true;
                return;
            }

            return;
        }

        if (e.Key == Key.F2)
        {
            BeginRenameSelectedPhoto();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F5)
        {
            if (CanRefreshWorkAreaWithKeyboard())
            {
                RefreshWorkAreaButton_Click(sender, e);
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Delete)
        {
            if (CanUseTypeTool() && _selectedTypeTextItem is not null && _selectedTypeTextItem.IsEditing == false)
            {
                RemoveSelectedTypeTextItem();
                e.Handled = true;
                return;
            }

            if (TryDeleteSelectedPhotosFromList())
            {
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.Z && CanUseEditorHistoryShortcuts())
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                TryRedoEditorHistory();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                TryUndoEditorHistory();
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.S && CanSaveCurrentPhoto)
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SaveCurrentPhotoWithDialog();
                e.Handled = true;
                return;
            }

            if (modifiers == ModifierKeys.Control)
            {
                SaveCurrentPhotoToProtectedOutput();
                e.Handled = true;
                return;
            }
        }

        if (TryHandleToolScaleShortcut(e))
        {
            e.Handled = true;
            return;
        }

        if (TryHandleToolShortcut(e))
        {
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Up && e.Key != Key.Down)
        {
            return;
        }

        if (!CanNavigatePhotosWithKeyboard())
        {
            return;
        }

        int currentIndex = Photos.IndexOf(SelectedPhoto!);
        if (currentIndex < 0)
        {
            return;
        }

        int nextIndex = currentIndex + (e.Key == Key.Up ? -1 : 1);
        if (nextIndex < 0 || nextIndex >= Photos.Count)
        {
            return;
        }

        SelectOnly(Photos[nextIndex]);
        e.Handled = true;
    }

    private bool TryHandleToolScaleShortcut(System.Windows.Input.KeyEventArgs e)
    {
        if (!CanHandleToolShortcuts())
        {
            return false;
        }

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key != Key.Oem4 && key != Key.Oem6)
        {
            return false;
        }

        ModifierKeys modifiers = Keyboard.Modifiers;
        bool isShiftOnly = modifiers == ModifierKeys.Shift;
        bool hasNoModifiers = modifiers == ModifierKeys.None;
        if (!isShiftOnly && !hasNoModifiers)
        {
            return false;
        }

        int direction = key == Key.Oem4 ? -1 : 1;
        return isShiftOnly
            ? AdjustActiveToolFeatherShortcut(direction)
            : AdjustActiveToolScaleShortcut(direction);
    }

    private bool AdjustActiveToolScaleShortcut(int direction)
    {
        switch (ActiveToolId.ToLowerInvariant())
        {
            case "brush":
                BrushSize += direction * 4;
                return true;
            case "eraser":
                EraserSize += direction * 4;
                return true;
            case "stamp":
                StampSize += direction * 4;
                return true;
            case "healing":
                HealingSize += direction * 4;
                return true;
            case "blursharp":
                BlurSharpSize += direction * 4;
                return true;
            case "dodgeburn":
                DodgeBurnSize += direction * 4;
                return true;
            case "historybrush":
                HistoryBrushSize += direction * 4;
                return true;
            case "liquify":
                LiquifySize += direction * 4;
                return true;
            case "sampler":
                SampleRange += direction * 2;
                return true;
            case "magic":
                MagicSampleRange += direction * 2;
                return true;
            case "type":
                TypeToolFontSize += direction * 2;
                return true;
            default:
                return false;
        }
    }

    private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
        }
    }

    private bool AdjustActiveToolFeatherShortcut(int direction)
    {
        switch (ActiveToolId.ToLowerInvariant())
        {
            case "brush":
                BrushSoftness += direction * 5;
                return true;
            case "eraser":
                EraserSoftness += direction * 5;
                return true;
            case "stamp":
                StampSoftness += direction * 5;
                return true;
            case "healing":
                HealingSoftness += direction * 5;
                return true;
            case "blursharp":
                BlurSharpSoftness += direction * 5;
                return true;
            case "dodgeburn":
                DodgeBurnSoftness += direction * 5;
                return true;
            case "historybrush":
                HistoryBrushSoftness += direction * 5;
                return true;
            case "liquify":
                LiquifySoftness += direction * 5;
                return true;
            case "rectangle":
                RectangleSelectionFeather += direction * 5;
                return true;
            case "path":
                PathToolFeather += direction * 5;
                return true;
            default:
                return false;
        }
    }

    private bool TryHandleToolShortcut(System.Windows.Input.KeyEventArgs e)
    {
        if (!CanHandleToolShortcuts())
        {
            return false;
        }

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        ModifierKeys modifiers = Keyboard.Modifiers;

        if (IsMultiPreviewToolLockActive)
        {
            if (modifiers == ModifierKeys.None && key == Key.H)
            {
                ActivateToolById("hand");
                return true;
            }

            return false;
        }

        if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.X)
        {
            ActivateToolById("liquify");
            return true;
        }

        if (modifiers == ModifierKeys.Control && key == Key.T)
        {
            ActivateToolById("freetransform");
            return true;
        }

        bool isShiftOnly = modifiers == ModifierKeys.Shift;
        bool hasNoModifiers = modifiers == ModifierKeys.None;
        if (!isShiftOnly && !hasNoModifiers)
        {
            return false;
        }

        switch (key)
        {
            case Key.V:
                ActivateToolById("select");
                return true;
            case Key.A:
                ActivateToolById("pathselect");
                if (isShiftOnly)
                {
                    PathSelectionMode = string.Equals(PathSelectionMode, "directselect", StringComparison.OrdinalIgnoreCase)
                        ? "pathselect"
                        : "directselect";
                    UpdatePathSelectionModeSelection();
                    UpdatePathToolStatus();
                }

                return true;
            case Key.M:
                ActivateToolById("frame");
                if (isShiftOnly)
                {
                    _frameSelectionShape = string.Equals(_frameSelectionShape, "ellipse", StringComparison.OrdinalIgnoreCase)
                        ? "rectangle"
                        : "ellipse";
                    UpdateFrameShapeSelection();
                    OnPropertyChanged(nameof(RectangleFrameSelectionVisibility));
                    OnPropertyChanged(nameof(EllipseFrameSelectionVisibility));
                }

                return true;
            case Key.L:
                ActivateToolById("lasso");
                return true;
            case Key.W:
                ActivateToolById("magic");
                if (isShiftOnly)
                {
                    MagicToolMode = string.Equals(MagicToolMode, "quickselect", StringComparison.OrdinalIgnoreCase)
                        ? "wand"
                        : "quickselect";
                    UpdateMagicModeSelection();
                }

                return true;
            case Key.C:
                ActivateToolById("crop");
                return true;
            case Key.J:
                ActivateToolById("healing");
                if (isShiftOnly)
                {
                    HealingMode = HealingMode switch
                    {
                        "healing" => "patch",
                        "patch" => "spot",
                        _ => "healing"
                    };
                    UpdateHealingModeSelection();
                }

                return true;
            case Key.B:
                ActivateToolById("brush");
                if (isShiftOnly)
                {
                    BrushMode = string.Equals(BrushMode, "pencil", StringComparison.OrdinalIgnoreCase)
                        ? "brush"
                        : "pencil";
                    UpdateBrushModeSelection();
                }

                return true;
            case Key.S:
                ActivateToolById("stamp");
                return true;
            case Key.E:
                ActivateToolById("eraser");
                return true;
            case Key.G:
                ActivateToolById("fill");
                if (isShiftOnly)
                {
                    FillToolMode = string.Equals(FillToolMode, "gradient", StringComparison.OrdinalIgnoreCase)
                        ? "bucket"
                        : "gradient";
                    UpdateFillModeSelection();
                }

                return true;
            case Key.R:
                ActivateToolById("blursharp");
                if (isShiftOnly)
                {
                    BlurSharpMode = string.Equals(BlurSharpMode, "sharpen", StringComparison.OrdinalIgnoreCase)
                        ? "blur"
                        : "sharpen";
                    UpdateBlurSharpModeSelection();
                }

                return true;
            case Key.O:
                ActivateToolById("dodgeburn");
                if (isShiftOnly)
                {
                    DodgeBurnMode = string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
                        ? "dodge"
                        : "burn";
                    UpdateDodgeBurnModeSelection();
                }

                return true;
            case Key.P:
                ActivateToolById("path");
                return true;
            case Key.T:
                ActivateToolById("type");
                return true;
            case Key.U:
                ActivateToolById("rectangle");
                if (isShiftOnly)
                {
                    RectangleShapeMode = RectangleShapeMode switch
                    {
                        "rectangle" => "ellipse",
                        "ellipse" => "polygon",
                        _ => "rectangle"
                    };
                    UpdateRectangleShapeModeSelection();
                }

                return true;
            case Key.I:
                if (isShiftOnly)
                {
                    ActivateToolById(string.Equals(ActiveToolId, "ruler", StringComparison.OrdinalIgnoreCase)
                        ? "sampler"
                        : "ruler");
                }
                else
                {
                    ActivateToolById("sampler");
                }

                return true;
            case Key.H:
                ActivateToolById("hand");
                return true;
            case Key.Z:
                ActivateToolById("zoom");
                return true;
            default:
                return false;
        }
    }

    private bool CanHandleToolShortcuts()
    {
        if (CanUseTypeTool() && _selectedTypeTextItem?.IsEditing == true)
        {
            return false;
        }

        IInputElement? focusedElement = Keyboard.FocusedElement;
        return focusedElement is not System.Windows.Controls.Primitives.TextBoxBase
            and not System.Windows.Controls.PasswordBox
            and not System.Windows.Controls.ComboBox
            and not System.Windows.Controls.ComboBoxItem;
    }

    private void ToolboxSettingTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        FocusPreviewSurfaceForToolInput();
        e.Handled = true;
    }

    private void FocusPreviewSurfaceForToolInput()
    {
        if (Keyboard.FocusedElement is System.Windows.Controls.TextBox textBox)
        {
            textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        }

        Keyboard.Focus(PreviewSurface);
    }

    private bool CanUseEditorHistoryShortcuts()
    {
        if (SelectedPhoto is null || !CanHandleToolShortcuts())
        {
            return false;
        }

        return !_isDodgeBurnDragging &&
               !_isLiquifyDragging &&
               !_isBrushDragging &&
               !_isEraserDragging &&
               !_isStampDragging &&
               !_isHealingDragging &&
               !_isBlurSharpDragging &&
               !_isFillGradientDragging &&
               !_isHistoryBrushDragging &&
               !_isTypeTextDragging &&
               !_isTypeTextCreating &&
               !_isSinglePreviewPanDragging &&
               _draggingPreviewTile is null;
    }

    private bool CanRefreshWorkAreaWithKeyboard()
    {
        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            return false;
        }

        IInputElement? focusedElement = Keyboard.FocusedElement;
        if (focusedElement is null || !IsElementInPhotoList(focusedElement))
        {
            return false;
        }

        return focusedElement is not System.Windows.Controls.Primitives.TextBoxBase
            and not System.Windows.Controls.PasswordBox
            and not System.Windows.Controls.ComboBox
            and not System.Windows.Controls.ComboBoxItem;
    }

    private bool CanDeleteSelectedPhotosWithKeyboard()
    {
        if (Keyboard.Modifiers != ModifierKeys.None || SelectedPreviewPhotos.Count == 0)
        {
            return false;
        }

        if (_isSinglePreviewPanDragging ||
            _draggingPreviewTile is not null ||
            _draggingTypeTextItem is not null ||
            _isTypeTextCreating)
        {
            return false;
        }

        IInputElement? focusedElement = Keyboard.FocusedElement;
        if (focusedElement is null || !IsElementInPhotoList(focusedElement))
        {
            return false;
        }

        return focusedElement is not System.Windows.Controls.Primitives.ScrollBar
            and not System.Windows.Controls.Primitives.RangeBase
            and not System.Windows.Controls.Primitives.Thumb
            and not System.Windows.Controls.Primitives.ButtonBase
            and not System.Windows.Controls.Primitives.TextBoxBase
            and not System.Windows.Controls.PasswordBox
            and not System.Windows.Controls.ComboBox
            and not System.Windows.Controls.ComboBoxItem;
    }

    private bool TryDeleteSelectedPhotosFromList()
    {
        if (!CanDeleteSelectedPhotosWithKeyboard())
        {
            return false;
        }

        List<PhotoItem> photosToDelete = SelectedPreviewPhotos
            .Where(photo => File.Exists(photo.Path))
            .ToList();
        if (photosToDelete.Count == 0)
        {
            return false;
        }

        string confirmationMessage = CreateDeleteSelectedPhotosConfirmationMessage(photosToDelete);
        MessageBoxResult result = System.Windows.MessageBox.Show(
            this,
            confirmationMessage,
            "파일 삭제",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.OK)
        {
            return true;
        }

        List<string> failedFileNames = new();
        foreach (PhotoItem photo in photosToDelete)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    photo.Path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                RemoveWorkAreaPhoto(photo.Path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                failedFileNames.Add($"{photo.FileName}: {ex.Message}");
            }
        }

        if (failedFileNames.Count > 0)
        {
            System.Windows.MessageBox.Show(
                this,
                "삭제하지 못한 파일이 있어.\n\n" + string.Join("\n", failedFileNames.Take(5)),
                "파일 삭제",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        PhotoListItemsControl?.Focus();
        return true;
    }

    private static string CreateDeleteSelectedPhotosConfirmationMessage(IReadOnlyList<PhotoItem> photos)
    {
        if (photos.Count == 1)
        {
            PhotoItem photo = photos[0];
            return
                $"파일: {photo.FileName}\n" +
                $"크기: {photo.DisplayInfo}\n" +
                $"위치: {Path.GetDirectoryName(photo.Path)}\n\n" +
                "이파일을 휴지통으로 버리시겠습니까?";
        }

        StringBuilder builder = new();
        builder.AppendLine($"선택 파일: {photos.Count}개");
        foreach (PhotoItem photo in photos.Take(5))
        {
            builder.AppendLine($"- {photo.FileName}  {photo.DisplayInfo}");
        }

        if (photos.Count > 5)
        {
            builder.AppendLine($"- 외 {photos.Count - 5}개");
        }

        builder.AppendLine();
        builder.Append("선택한 파일들을 휴지통으로 버리시겠습니까?");
        return builder.ToString();
    }

    private bool CanNavigatePhotosWithKeyboard()
    {
        if (SelectedPhoto is null)
        {
            return false;
        }

        if (SelectedPreviewPhotos.Count != 1)
        {
            return false;
        }

        if (_isSinglePreviewPanDragging || _draggingPreviewTile is not null || _draggingTypeTextItem is not null || _isTypeTextCreating)
        {
            return false;
        }

        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            return false;
        }

        IInputElement? focusedElement = Keyboard.FocusedElement;
        if (focusedElement is null || !IsElementInPhotoList(focusedElement))
        {
            return false;
        }

        return focusedElement is not System.Windows.Controls.Primitives.ScrollBar
            and not System.Windows.Controls.Primitives.RangeBase
            and not System.Windows.Controls.Primitives.Thumb
            and not System.Windows.Controls.Primitives.ButtonBase
            and not System.Windows.Controls.Primitives.TextBoxBase
            and not System.Windows.Controls.PasswordBox
            and not System.Windows.Controls.ComboBox
            and not System.Windows.Controls.ComboBoxItem;
    }

    private bool IsElementInPhotoList(IInputElement element)
    {
        if (element is not DependencyObject dependencyObject)
        {
            return false;
        }

        DependencyObject current = dependencyObject;
        while (current is not null)
        {
            if (ReferenceEquals(current, PhotoListItemsControl))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void PhotoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is PhotoItem photo)
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            bool isAddSelectionPressed = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isRangeSelectionPressed = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isRangeSelectionPressed && _selectionAnchor is not null)
            {
                SelectRange(_selectionAnchor, photo, isAddSelectionPressed);
            }
            else if (isAddSelectionPressed)
            {
                TogglePhotoSelection(photo);
            }
            else
            {
                SelectOnly(photo);
            }

            _selectionAnchor = photo;
            PhotoListItemsControl?.Focus();
            e.Handled = true;
        }
    }

    private void PhotoItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is PhotoItem clickedPhoto)
        {
            SelectOnly(clickedPhoto);
            e.Handled = true;
        }
    }

    private void PreviewTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PhotoItem photo)
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            if (CanUseSelectTool())
            {
                return;
            }

            SelectOnly(photo);
            e.Handled = true;
            return;
        }

        if (CanUseSelectTool())
        {
            HandleSelectToolPreviewTileMouseDown(photo, e);
            return;
        }

        if (sender is FrameworkElement tile && SelectedPreviewPhotos.Count > 1)
        {
            bool isGroupMove = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
                == (ModifierKeys.Control | ModifierKeys.Shift);
            if (isGroupMove)
            {
                StartPreviewTileGroupPan(photo, tile, e.GetPosition(PreviewSurface));
            }
            else
            {
                StartPreviewTilePan(photo, tile, e.GetPosition(PreviewSurface));
            }

            e.Handled = true;
        }
    }

    private void PreviewTile_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_draggingPreviewTile is null)
        {
            return;
        }

        if (_draggingPreviewTile.IsSelected is false)
        {
            _draggingPreviewTile = null;
            Mouse.Capture(null);
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            StopPreviewTilePan();
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is not PhotoItem draggedPhoto
            || !ReferenceEquals(draggedPhoto, _draggingPreviewTile)
            || sender is not FrameworkElement tile)
        {
            return;
        }

        System.Windows.Point currentPoint = e.GetPosition(PreviewSurface);
        Vector delta = currentPoint - _previewTilePanStartPoint;
        if (_isMultiPreviewGroupPanDragging)
        {
            foreach (PhotoItem selectedPhoto in SelectedPreviewPhotos)
            {
                if (_groupPreviewPanStartOffsets.TryGetValue(selectedPhoto, out (double x, double y) startOffset))
                {
                    UpdatePreviewTilePan(
                        selectedPhoto,
                        tile,
                        startOffset.x + delta.X,
                        startOffset.y + delta.Y);
                }
            }
        }
        else
        {
            UpdatePreviewTilePan(
                draggedPhoto,
                tile,
                _previewTilePanStartOffsetX + delta.X,
                _previewTilePanStartOffsetY + delta.Y);
        }

        e.Handled = true;
    }

    private void PreviewTile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingPreviewTile is null)
        {
            return;
        }

        StopPreviewTilePan();
        e.Handled = true;
    }

    private void StartSinglePreviewPan(System.Windows.Point startPoint)
    {
        if (SelectedPreviewPhotos.Count != 1)
        {
            return;
        }

        _isSinglePreviewPanDragging = true;
        _singlePreviewPanStartPoint = startPoint;
        _singlePreviewImageLeftStart = PreviewImageLeft;
        _singlePreviewImageTopStart = PreviewImageTop;
        PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
        Mouse.Capture(PreviewSurface);
    }

    private void StopSinglePreviewPan()
    {
        if (!_isSinglePreviewPanDragging)
        {
            return;
        }

        _isSinglePreviewPanDragging = false;
        Mouse.Capture(null);
        if (CanUseHandPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Hand;
        }
    }

    private void StartPreviewTilePan(PhotoItem photo, FrameworkElement tile, System.Windows.Point startPoint)
    {
        _draggingPreviewTile = photo;
        _isMultiPreviewGroupPanDragging = false;
        _previewTilePanStartPoint = startPoint;
        _previewTilePanStartOffsetX = photo.MultiPreviewOffsetX;
        _previewTilePanStartOffsetY = photo.MultiPreviewOffsetY;
        _groupPreviewPanStartOffsets.Clear();
        Mouse.Capture(tile);
    }

    private void StartPreviewTileGroupPan(PhotoItem photo, FrameworkElement tile, System.Windows.Point startPoint)
    {
        _draggingPreviewTile = photo;
        _isMultiPreviewGroupPanDragging = true;
        _previewTilePanStartPoint = startPoint;
        _previewTilePanStartOffsetX = photo.MultiPreviewOffsetX;
        _previewTilePanStartOffsetY = photo.MultiPreviewOffsetY;
        _groupPreviewPanStartOffsets.Clear();

        foreach (PhotoItem selectedPhoto in SelectedPreviewPhotos)
        {
            _groupPreviewPanStartOffsets[selectedPhoto] = (selectedPhoto.MultiPreviewOffsetX, selectedPhoto.MultiPreviewOffsetY);
        }

        Mouse.Capture(tile);
    }

    private void StopPreviewTilePan()
    {
        if (_draggingPreviewTile is null)
        {
            return;
        }

        _draggingPreviewTile = null;
        _isMultiPreviewGroupPanDragging = false;
        _groupPreviewPanStartOffsets.Clear();
        Mouse.Capture(null);
    }

    private void UpdatePreviewTilePan(PhotoItem photo, FrameworkElement tile, double targetOffsetX, double targetOffsetY)
    {
        double tileWidth = tile.ActualWidth;
        double tileHeight = tile.ActualHeight;
        if (tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        double imageWidth = photo.BaseImage.PixelWidth;
        double imageHeight = photo.BaseImage.PixelHeight;
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return;
        }

        double scale = Math.Min(tileWidth / imageWidth, tileHeight / imageHeight) * photo.MultiPreviewZoomScale;
        double displayedWidth = imageWidth * scale;
        double displayedHeight = imageHeight * scale;
        photo.UseOriginalForMultiPreview = Math.Max(displayedWidth, displayedHeight) > PhotoItem.PreviewProxyLongSide;

        double minLeft;
        double maxLeft;
        double overflowX = displayedWidth - tileWidth;
        if (overflowX <= 0)
        {
            minLeft = 0;
            maxLeft = 0;
        }
        else
        {
            double halfOverflowX = overflowX * 0.5;
            minLeft = -halfOverflowX;
            maxLeft = halfOverflowX;
        }

        double minTop;
        double maxTop;
        double overflowY = displayedHeight - tileHeight;
        if (overflowY <= 0)
        {
            minTop = 0;
            maxTop = 0;
        }
        else
        {
            double halfOverflowY = overflowY * 0.5;
            minTop = -halfOverflowY;
            maxTop = halfOverflowY;
        }

        photo.MultiPreviewOffsetX = Math.Clamp(targetOffsetX, minLeft, maxLeft);
        photo.MultiPreviewOffsetY = Math.Clamp(targetOffsetY, minTop, maxTop);
    }

    private void OpenDoubleChinWorkbench_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        SliderToolSpec spec = new(
            "double_chin",
            "Double Chin",
            "chin-centered under-jaw and upper-neck local workbench",
            ProxyPolicy.LocalWorkAreaProxy,
            0,
            100,
            35);
        _localWorkbenchState = LocalProxyBuilder.BuildDisplayOnlyState(SelectedPhoto, spec);
        LocalWorkbenchImage = _localWorkbenchState.LocalProxySource;
        LocalWorkbenchApplyMaskImage = _localWorkbenchState.LocalMaskSet.ApplyMaskOverlay;
        LocalWorkbenchProtectMaskImage = _localWorkbenchState.LocalMaskSet.ProtectMaskOverlay;
        LocalWorkbenchBlockMaskImage = _localWorkbenchState.LocalMaskSet.BlockMaskOverlay;
        LocalWorkbenchWorkMaskImage = _localWorkbenchState.LocalMaskSet.WorkMaskOverlay;
        LocalWorkbenchTitle = "DoubleChin Workbench Preview";
        LocalWorkbenchInfo = _localWorkbenchState.CoordinateMap.ToDisplayText();
        LocalWorkbenchStatusText = "Preview only. Work area and mask routing are shown, but image apply is not connected yet.";
        LocalWorkbenchVisibility = Visibility.Visible;
        UpdateLocalWorkbenchGuide();
    }

    private void OpenNoseWorkbench_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        SliderToolSpec spec = new(
            "nose_shape",
            "Nose Shape",
            "nose-centered bridge-tip-alar local workbench",
            ProxyPolicy.LocalWorkAreaProxy,
            0,
            100,
            28);
        _localWorkbenchState = LocalProxyBuilder.BuildDisplayOnlyState(SelectedPhoto, spec);
        LocalWorkbenchImage = _localWorkbenchState.LocalProxySource;
        LocalWorkbenchApplyMaskImage = _localWorkbenchState.LocalMaskSet.ApplyMaskOverlay;
        LocalWorkbenchProtectMaskImage = _localWorkbenchState.LocalMaskSet.ProtectMaskOverlay;
        LocalWorkbenchBlockMaskImage = _localWorkbenchState.LocalMaskSet.BlockMaskOverlay;
        LocalWorkbenchWorkMaskImage = _localWorkbenchState.LocalMaskSet.WorkMaskOverlay;
        LocalWorkbenchTitle = "Nose Workbench Preview";
        LocalWorkbenchInfo = _localWorkbenchState.CoordinateMap.ToDisplayText();
        LocalWorkbenchStatusText = "Preview only. The nose work area is shown, but image apply is not connected yet.";
        LocalWorkbenchVisibility = Visibility.Visible;
        UpdateLocalWorkbenchGuide();
    }

    private void CancelWorkbench_Click(object sender, RoutedEventArgs e)
    {
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();
    }

    private void AddPhotos(IEnumerable<string> fileNames)
    {
        List<PhotoItem> addedPhotos = [];
        foreach (string fileName in fileNames.Where(File.Exists))
        {
            try
            {
                PhotoItem photo = PhotoItem.Load(fileName);
                Photos.Add(photo);
                addedPhotos.Add(photo);
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException)
            {
            }
        }

        QueuePreviewProxy1200Builds(addedPhotos);

        if (SelectedPhoto is null && Photos.Count > 0)
        {
            SelectOnly(Photos[0]);
        }

        OnPropertyChanged(nameof(PhotoSelectionText));
    }

    private void QueuePreviewProxy1200Builds(IEnumerable<PhotoItem> photos)
    {
        foreach (PhotoItem photo in photos)
        {
            QueuePreviewProxy1200Build(photo);
        }
    }

    private void QueuePreviewProxy1200Build(PhotoItem photo)
    {
        if (photo.PreviewProxy1200State != PreviewProxy1200State.Missing)
        {
            return;
        }

        bool shouldStartWorker = false;
        lock (_previewProxy1200BuildSync)
        {
            if (!_queuedPreviewProxy1200Photos.Add(photo))
            {
                return;
            }

            _previewProxy1200BuildQueue.Enqueue(new PreviewProxy1200BuildItem(photo, _previewProxy1200BuildGeneration));
            photo.SetPreviewProxy1200Building(true);
            if (!_isPreviewProxy1200BuildQueueRunning)
            {
                _isPreviewProxy1200BuildQueueRunning = true;
                shouldStartWorker = true;
            }
        }

        if (shouldStartWorker)
        {
            _ = ProcessPreviewProxy1200BuildQueueAsync();
        }
    }

    private void ResetPreviewProxy1200BuildQueue()
    {
        lock (_previewProxy1200BuildSync)
        {
            _previewProxy1200BuildGeneration++;
            _previewProxy1200BuildQueue.Clear();
            _queuedPreviewProxy1200Photos.Clear();
        }
    }

    private async Task ProcessPreviewProxy1200BuildQueueAsync()
    {
        while (true)
        {
            PreviewProxy1200BuildItem item;
            lock (_previewProxy1200BuildSync)
            {
                if (_previewProxy1200BuildQueue.Count == 0)
                {
                    _isPreviewProxy1200BuildQueueRunning = false;
                    return;
                }

                item = _previewProxy1200BuildQueue.Dequeue();
                _queuedPreviewProxy1200Photos.Remove(item.Photo);
            }

            if (!IsPreviewProxy1200GenerationCurrent(item.Generation))
            {
                continue;
            }

            PreviewProxy1200BuildSource? buildSource = await Dispatcher.InvokeAsync(() =>
            {
                if (!IsPreviewProxy1200GenerationCurrent(item.Generation) ||
                    !Photos.Contains(item.Photo))
                {
                    return null;
                }

                ImageSource imageIdentity = item.Photo.Image;
                BitmapSource source = imageIdentity as BitmapSource ?? item.Photo.BaseImage;
                BitmapSource safeSource = source.IsFrozen ? source : CloneBitmapSource(source);
                return new PreviewProxy1200BuildSource(item.Photo, item.Generation, imageIdentity, safeSource);
            });

            if (buildSource is null)
            {
                continue;
            }

            BitmapSource? proxy;
            try
            {
                proxy = await Task.Run(() => PhotoItem.CreatePreviewProxy1200(buildSource.Source));
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (IsPreviewProxy1200GenerationCurrent(buildSource.Generation) &&
                        Photos.Contains(buildSource.Photo) &&
                        ReferenceEquals(buildSource.Photo.Image, buildSource.ImageIdentity))
                    {
                        buildSource.Photo.SetPreviewProxy1200Building(false);
                    }
                });
                continue;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                if (!IsPreviewProxy1200GenerationCurrent(buildSource.Generation) ||
                    !Photos.Contains(buildSource.Photo) ||
                    !ReferenceEquals(buildSource.Photo.Image, buildSource.ImageIdentity))
                {
                    return;
                }

                buildSource.Photo.SetPreviewProxy1200(proxy);
            });

            await Task.Delay(35);
        }
    }

    private bool IsPreviewProxy1200GenerationCurrent(int generation)
    {
        lock (_previewProxy1200BuildSync)
        {
            return generation == _previewProxy1200BuildGeneration;
        }
    }

    private void LoadAppConfig()
    {
        _appConfig = ReadAppConfig();
        SyncColorManagementSettingsFromConfig();
        LoadBackgroundSettingsFromConfig();
        LoadCropPresetSettingsFromConfig();
        LoadToolboxDefaultsFromConfig();
        if (string.IsNullOrWhiteSpace(_appConfig.WorkAreaFolderPath) &&
            File.Exists(LegacyWorkAreaSettingsPath))
        {
            string legacyPath = File.ReadAllText(LegacyWorkAreaSettingsPath).Trim();
            if (Directory.Exists(legacyPath))
            {
                _appConfig.WorkAreaFolderPath = legacyPath;
                SaveAppConfig();
            }
        }

        if (Directory.Exists(_appConfig.WorkAreaFolderPath))
        {
            LoadPhotosFromWorkArea(_appConfig.WorkAreaFolderPath);
        }
        else
        {
            StopWorkAreaWatcher();
        }

        OnPropertyChanged(nameof(WorkAreaDisplayText));
        RaiseColorManagementPropertyChanged();
        OnPropertyChanged(nameof(AutoCheckUpdatesAtStartup));
        RaiseRuntimeWorkModePropertyChanged();
        OnPropertyChanged(nameof(ShowHistoryPanel));
        OnPropertyChanged(nameof(EnableEditorHistoryPersistence));
        OnPropertyChanged(nameof(HistoryPanelVisibility));
        RaiseCropPresetPropertyChanged();
    }

    private void SyncColorManagementSettingsFromConfig()
    {
        ColorManagementSettings.Mode = _appConfig.ColorManagementMode;
        ColorManagementSettings.ManualDisplayProfilePath = string.IsNullOrWhiteSpace(_appConfig.ManualDisplayColorProfilePath)
            ? null
            : _appConfig.ManualDisplayColorProfilePath;
    }

    private void LoadToolboxDefaultsFromConfig()
    {
        _isLoadingToolboxDefaults = true;
        ToolboxDefaultSettings settings = _appConfig.ToolboxDefaults ??= new ToolboxDefaultSettings();

        _brushMode = NormalizeToolMode(settings.BrushMode, "brush", "brush", "pencil");
        _brushSize = Math.Clamp(settings.BrushSize, 1, 600);
        _brushSoftness = Math.Clamp(settings.BrushSoftness, 0, 100);
        _showBrushCircle = settings.ShowBrushCircle;
        _brushCircleSize = _brushSize;

        _fillToolMode = NormalizeToolMode(settings.FillToolMode, "bucket", "bucket", "gradient");
        _fillToolOpacity = Math.Clamp(Math.Round(settings.FillToolOpacity), 0, 100);

        _eraserSize = Math.Clamp(settings.EraserSize, 1, 600);
        _eraserSoftness = Math.Clamp(settings.EraserSoftness, 0, 100);
        _showEraserCircle = settings.ShowEraserCircle;
        _eraserCircleSize = _eraserSize;

        _stampSize = Math.Clamp(settings.StampSize, 1, 600);
        _stampSoftness = Math.Clamp(settings.StampSoftness, 0, 100);
        _showStampCircle = settings.ShowStampCircle;
        _stampCircleSize = _stampSize;

        _healingMode = NormalizeToolMode(settings.HealingMode, "healing", "healing", "patch", "spot");
        _healingSize = Math.Clamp(settings.HealingSize, 1, 600);
        _healingSoftness = Math.Clamp(settings.HealingSoftness, 0, 100);
        _healingStrength = Math.Clamp(settings.HealingStrength, 0, 100);
        _showHealingCircle = settings.ShowHealingCircle;
        _healingCircleSize = _healingSize;

        _blurSharpMode = NormalizeToolMode(settings.BlurSharpMode, "blur", "blur", "sharpen");
        _blurSharpSize = Math.Clamp(settings.BlurSharpSize, 1, 600);
        _blurSharpSoftness = Math.Clamp(settings.BlurSharpSoftness, 0, 100);
        _blurSharpStrength = Math.Clamp(settings.BlurSharpStrength, 0, 100);
        _blurSharpRadius = Math.Clamp(settings.BlurSharpRadius, 0.1, 100);
        _showBlurSharpCircle = settings.ShowBlurSharpCircle;
        _blurSharpCircleSize = _blurSharpSize;

        _dodgeBurnMode = NormalizeToolMode(settings.DodgeBurnMode, "dodge", "dodge", "burn");
        _dodgeBurnSize = Math.Clamp(Math.Round(settings.DodgeBurnSize), 4, 512);
        _dodgeBurnSoftness = Math.Clamp(Math.Round(settings.DodgeBurnSoftness), 0, 100);
        _dodgeBurnStrength = Math.Clamp(Math.Round(settings.DodgeBurnStrength), 1, 100);
        _showDodgeBurnCircle = settings.ShowDodgeBurnCircle;
        _dodgeBurnCircleSize = _dodgeBurnSize;

        _historyBrushSize = Math.Clamp(settings.HistoryBrushSize, 1, 600);
        _historyBrushSoftness = Math.Clamp(settings.HistoryBrushSoftness, 0, 100);
        _historyBrushStrength = Math.Clamp(settings.HistoryBrushStrength, 0, 100);
        _showHistoryBrushCircle = settings.ShowHistoryBrushCircle;
        _historyBrushCircleSize = _historyBrushSize;

        _sampleRange = Math.Clamp(Math.Round(settings.SampleRange), 1, 501);

        _magicToolMode = NormalizeToolMode(settings.MagicToolMode, "wand", "wand", "quickselect");
        _magicTolerance = Math.Clamp(Math.Round(settings.MagicTolerance), 0, 765);
        _magicSampleRange = Math.Clamp(Math.Round(settings.MagicSampleRange), 1, 101);

        _liquifySize = Math.Clamp(Math.Round(settings.LiquifySize), 4, 512);
        _liquifySoftness = Math.Clamp(Math.Round(settings.LiquifySoftness), 0, 100);
        _liquifyStrength = Math.Clamp(Math.Round(settings.LiquifyStrength), 1, 100);
        _showLiquifyCircle = settings.ShowLiquifyCircle;
        _liquifyCircleSize = _liquifySize;

        _isLoadingToolboxDefaults = false;
        RaiseToolboxDefaultsPropertyChanged();
    }

    private static string NormalizeToolMode(string? value, string fallback, params string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return allowedValues.Any(allowed => string.Equals(allowed, value, StringComparison.OrdinalIgnoreCase))
            ? value
            : fallback;
    }

    private void SaveToolboxDefaults()
    {
        if (_isLoadingToolboxDefaults)
        {
            return;
        }

        ToolboxDefaultSettings settings = _appConfig.ToolboxDefaults ??= new ToolboxDefaultSettings();
        settings.BrushMode = BrushMode;
        settings.BrushSize = BrushSize;
        settings.BrushSoftness = BrushSoftness;
        settings.ShowBrushCircle = ShowBrushCircle;
        settings.FillToolMode = FillToolMode;
        settings.FillToolOpacity = FillToolOpacity;
        settings.EraserSize = EraserSize;
        settings.EraserSoftness = EraserSoftness;
        settings.ShowEraserCircle = ShowEraserCircle;
        settings.StampSize = StampSize;
        settings.StampSoftness = StampSoftness;
        settings.ShowStampCircle = ShowStampCircle;
        settings.HealingMode = HealingMode;
        settings.HealingSize = HealingSize;
        settings.HealingSoftness = HealingSoftness;
        settings.HealingStrength = HealingStrength;
        settings.ShowHealingCircle = ShowHealingCircle;
        settings.BlurSharpMode = BlurSharpMode;
        settings.BlurSharpSize = BlurSharpSize;
        settings.BlurSharpSoftness = BlurSharpSoftness;
        settings.BlurSharpStrength = BlurSharpStrength;
        settings.BlurSharpRadius = BlurSharpRadius;
        settings.ShowBlurSharpCircle = ShowBlurSharpCircle;
        settings.DodgeBurnMode = DodgeBurnMode;
        settings.DodgeBurnSize = DodgeBurnSize;
        settings.DodgeBurnSoftness = DodgeBurnSoftness;
        settings.DodgeBurnStrength = DodgeBurnStrength;
        settings.ShowDodgeBurnCircle = ShowDodgeBurnCircle;
        settings.HistoryBrushSize = HistoryBrushSize;
        settings.HistoryBrushSoftness = HistoryBrushSoftness;
        settings.HistoryBrushStrength = HistoryBrushStrength;
        settings.ShowHistoryBrushCircle = ShowHistoryBrushCircle;
        settings.SampleRange = SampleRange;
        settings.MagicToolMode = MagicToolMode;
        settings.MagicTolerance = MagicTolerance;
        settings.MagicSampleRange = MagicSampleRange;
        settings.LiquifySize = LiquifySize;
        settings.LiquifySoftness = LiquifySoftness;
        settings.LiquifyStrength = LiquifyStrength;
        settings.ShowLiquifyCircle = ShowLiquifyCircle;

        QueueToolboxDefaultsSave();
    }

    private void QueueToolboxDefaultsSave()
    {
        if (_toolboxDefaultsSaveTimer is null)
        {
            _toolboxDefaultsSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _toolboxDefaultsSaveTimer.Tick += ToolboxDefaultsSaveTimer_Tick;
        }

        _toolboxDefaultsSaveTimer.Stop();
        _toolboxDefaultsSaveTimer.Start();
    }

    private void ToolboxDefaultsSaveTimer_Tick(object? sender, EventArgs e)
    {
        FlushToolboxDefaultsSave();
    }

    private void FlushToolboxDefaultsSave()
    {
        if (_toolboxDefaultsSaveTimer is null || !_toolboxDefaultsSaveTimer.IsEnabled)
        {
            return;
        }

        _toolboxDefaultsSaveTimer.Stop();
        SaveAppConfig();
    }

    private void RaiseToolboxDefaultsPropertyChanged()
    {
        OnPropertyChanged(nameof(BrushMode));
        OnPropertyChanged(nameof(BrushSize));
        OnPropertyChanged(nameof(BrushSoftness));
        OnPropertyChanged(nameof(ShowBrushCircle));
        OnPropertyChanged(nameof(FillToolMode));
        OnPropertyChanged(nameof(FillToolOpacity));
        OnPropertyChanged(nameof(EraserSize));
        OnPropertyChanged(nameof(EraserSoftness));
        OnPropertyChanged(nameof(ShowEraserCircle));
        OnPropertyChanged(nameof(StampSize));
        OnPropertyChanged(nameof(StampSoftness));
        OnPropertyChanged(nameof(ShowStampCircle));
        OnPropertyChanged(nameof(HealingMode));
        OnPropertyChanged(nameof(HealingModeHintText));
        OnPropertyChanged(nameof(HealingSize));
        OnPropertyChanged(nameof(HealingSoftness));
        OnPropertyChanged(nameof(HealingStrength));
        OnPropertyChanged(nameof(ShowHealingCircle));
        OnPropertyChanged(nameof(BlurSharpMode));
        OnPropertyChanged(nameof(BlurSharpSize));
        OnPropertyChanged(nameof(BlurSharpSoftness));
        OnPropertyChanged(nameof(BlurSharpStrength));
        OnPropertyChanged(nameof(BlurSharpRadius));
        OnPropertyChanged(nameof(ShowBlurSharpCircle));
        OnPropertyChanged(nameof(DodgeBurnMode));
        OnPropertyChanged(nameof(DodgeBurnCircleStroke));
        OnPropertyChanged(nameof(DodgeBurnSize));
        OnPropertyChanged(nameof(DodgeBurnSoftness));
        OnPropertyChanged(nameof(DodgeBurnStrength));
        OnPropertyChanged(nameof(ShowDodgeBurnCircle));
        OnPropertyChanged(nameof(HistoryBrushSize));
        OnPropertyChanged(nameof(HistoryBrushSoftness));
        OnPropertyChanged(nameof(HistoryBrushStrength));
        OnPropertyChanged(nameof(ShowHistoryBrushCircle));
        OnPropertyChanged(nameof(SampleRange));
        OnPropertyChanged(nameof(MagicToolMode));
        OnPropertyChanged(nameof(MagicTolerance));
        OnPropertyChanged(nameof(MagicSampleRange));
        OnPropertyChanged(nameof(LiquifySize));
        OnPropertyChanged(nameof(LiquifySoftness));
        OnPropertyChanged(nameof(LiquifyStrength));
        OnPropertyChanged(nameof(ShowLiquifyCircle));
    }

    private void RaiseColorManagementPropertyChanged()
    {
        OnPropertyChanged(nameof(IsColorManagementAutomatic));
        OnPropertyChanged(nameof(IsColorManagementManual));
        OnPropertyChanged(nameof(IsColorManagementDisabled));
        OnPropertyChanged(nameof(CanClearManualColorManagementProfile));
        OnPropertyChanged(nameof(ManualColorManagementProfileText));
        OnPropertyChanged(nameof(ColorManagementStatusText));
        OnPropertyChanged(nameof(SelectedPhotoStatusText));
    }

    private void ApplyColorManagementSettings(ColorManagementMode mode, string? manualProfilePath = null)
    {
        ColorManagementMode previousMode = _appConfig.ColorManagementMode;
        string? previousManualProfilePath = _appConfig.ManualDisplayColorProfilePath;

        _appConfig.ColorManagementMode = mode;
        if (!string.IsNullOrWhiteSpace(manualProfilePath))
        {
            _appConfig.ManualDisplayColorProfilePath = manualProfilePath;
        }

        SyncColorManagementSettingsFromConfig();
        SaveAppConfig();
        RaiseColorManagementPropertyChanged();

        if (previousMode != _appConfig.ColorManagementMode ||
            !string.Equals(previousManualProfilePath, _appConfig.ManualDisplayColorProfilePath, StringComparison.OrdinalIgnoreCase))
        {
            ReloadPhotosForColorManagement();
        }
    }

    private string? PromptForColorManagementProfilePath()
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Filter = "ICC profile files|*.icc;*.icm|All files|*.*",
            Title = "Select display profile"
        };

        return dialog.ShowDialog(this) == true
            ? dialog.FileName
            : null;
    }

    private void PinWorkArea(string folderPath)
    {
        _appConfig.WorkAreaFolderPath = folderPath;
        SaveAppConfig();
        OnPropertyChanged(nameof(WorkAreaDisplayText));
        LoadPhotosFromWorkArea(folderPath);
    }

    private void LoadPhotosFromWorkArea(string folderPath)
    {
        string[] imageFiles = Directory.EnumerateFiles(folderPath)
            .Where(IsSupportedImagePath)
            .ApplySort(_appConfig.PhotoListSortMode)
            .ToArray();

        ResetPreviewProxy1200BuildQueue();
        ClearFaceShapeLandmarkCache();
        Photos.Clear();
        SelectedPhoto = null;
        SelectedPreviewPhotos.Clear();
        _selectionAnchor = null;
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();
        AddPhotos(imageFiles);
        CollapseAllRetouchTabs();
        StartWorkAreaWatcher(folderPath);
    }

    private void ReloadPhotosForColorManagement()
    {
        if (Photos.Count == 0)
        {
            return;
        }

        ResetPreviewProxy1200BuildQueue();
        ClearFaceShapeLandmarkCache();
        string[] photoPaths = Photos.Select(photo => photo.Path).ToArray();
        HashSet<string> selectedPaths = SelectedPreviewPhotos
            .Select(photo => photo.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Photos.Clear();
        SelectedPhoto = null;
        SelectedPreviewPhotos.Clear();
        _selectionAnchor = null;
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();

        AddPhotos(photoPaths);

        if (selectedPaths.Count == 0)
        {
            return;
        }

        foreach (PhotoItem photo in Photos)
        {
            photo.IsSelected = selectedPaths.Contains(photo.Path);
        }

        RefreshSelectedPreviewPhotos();
        SelectedPhoto = SelectedPreviewPhotos.Count == 1
            ? SelectedPreviewPhotos[0]
            : null;

        UpdatePreviewLayout();
        OnPropertyChanged(nameof(PhotoSelectionText));
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        StoreCurrentEditorHistorySession(SelectedPhoto, persistToDisk: true);
        FlushToolboxDefaultsSave();
        CancelToneCurvePreviewRender();
        ResetPreviewProxy1200BuildQueue();
        MediaPipeConnectionService.ShutdownWorker();
        BiRefNetMattingService.ShutdownWorker();
        StopWorkAreaWatcher();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        StartMediaPipeWarmup();
        StartBiRefNetWarmup();
    }

    private void StartWorkAreaWatcher(string folderPath)
    {
        StopWorkAreaWatcher();

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        FileSystemWatcher watcher = new(folderPath)
        {
            Filter = "*.*",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite
        };

        watcher.Created += WorkAreaWatcher_FileCreatedOrRenamed;
        watcher.Renamed += WorkAreaWatcher_FileCreatedOrRenamed;
        watcher.Deleted += WorkAreaWatcher_FileDeleted;
        watcher.EnableRaisingEvents = true;
        _workAreaWatcher = watcher;
    }

    private void StopWorkAreaWatcher()
    {
        lock (_workAreaWatcherSync)
        {
            _pendingWorkAreaImports.Clear();
            _selfSavedOutputPaths.Clear();
        }

        if (_workAreaWatcher is null)
        {
            return;
        }

        _workAreaWatcher.EnableRaisingEvents = false;
        _workAreaWatcher.Created -= WorkAreaWatcher_FileCreatedOrRenamed;
        _workAreaWatcher.Renamed -= WorkAreaWatcher_FileCreatedOrRenamed;
        _workAreaWatcher.Deleted -= WorkAreaWatcher_FileDeleted;
        _workAreaWatcher.Dispose();
        _workAreaWatcher = null;
    }

    private void WorkAreaWatcher_FileCreatedOrRenamed(object sender, FileSystemEventArgs e)
    {
        if (e is RenamedEventArgs renamedEventArgs)
        {
            QueueWorkAreaFileRemoval(renamedEventArgs.OldFullPath);
        }

        QueueWorkAreaFileImport(e.FullPath);
    }

    private void WorkAreaWatcher_FileDeleted(object sender, FileSystemEventArgs e)
    {
        QueueWorkAreaFileRemoval(e.FullPath);
    }

    private void QueueWorkAreaFileImport(string path)
    {
        if (!IsSupportedImagePath(path))
        {
            return;
        }

        if (IsSelfSavedOutputPath(path))
        {
            return;
        }

        string? workAreaPath = _appConfig.WorkAreaFolderPath;
        string? directoryPath = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(workAreaPath) ||
            string.IsNullOrWhiteSpace(directoryPath) ||
            !string.Equals(workAreaPath, directoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lock (_workAreaWatcherSync)
        {
            if (!_pendingWorkAreaImports.Add(path))
            {
                return;
            }
        }

        _ = HandlePendingWorkAreaFileImportAsync(path);
    }

    private void QueueWorkAreaFileRemoval(string path)
    {
        if (!IsSupportedImagePath(path))
        {
            return;
        }

        lock (_workAreaWatcherSync)
        {
            _pendingWorkAreaImports.Remove(path);
        }

        _ = Dispatcher.InvokeAsync(() => RemoveWorkAreaPhoto(path));
    }

    private async Task HandlePendingWorkAreaFileImportAsync(string path)
    {
        try
        {
            if (!await WaitForWorkAreaFileReadyAsync(path))
            {
                return;
            }

            await Dispatcher.InvokeAsync(() => TryAddWorkAreaPhotoAndFocus(path));
        }
        finally
        {
            lock (_workAreaWatcherSync)
            {
                _pendingWorkAreaImports.Remove(path);
            }
        }
    }

    private static async Task<bool> WaitForWorkAreaFileReadyAsync(string path)
    {
        long previousLength = -1;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (!File.Exists(path))
            {
                await Task.Delay(200);
                continue;
            }

            try
            {
                FileInfo info = new(path);
                long currentLength = info.Length;
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (currentLength > 0 && currentLength == previousLength)
                {
                    return true;
                }

                previousLength = currentLength;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            await Task.Delay(200);
        }

        return false;
    }

    private void TryAddWorkAreaPhotoAndFocus(string path)
    {
        string? workAreaPath = _appConfig.WorkAreaFolderPath;
        string? directoryPath = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(workAreaPath) ||
            string.IsNullOrWhiteSpace(directoryPath) ||
            !string.Equals(workAreaPath, directoryPath, StringComparison.OrdinalIgnoreCase) ||
            IsSelfSavedOutputPath(path) ||
            !File.Exists(path) ||
            !IsSupportedImagePath(path))
        {
            return;
        }

        PhotoItem? existingPhoto = Photos.FirstOrDefault(photo => string.Equals(photo.Path, path, StringComparison.OrdinalIgnoreCase));
        if (existingPhoto is not null)
        {
            if (ShouldFocusImportedWorkAreaPhoto())
            {
                SelectOnly(existingPhoto);
            }

            return;
        }

        try
        {
            PhotoItem photo = PhotoItem.Load(path);
            Photos.Insert(GetWorkAreaPhotoInsertIndex(path), photo);
            QueuePreviewProxy1200Build(photo);
            if (ShouldFocusImportedWorkAreaPhoto())
            {
                SelectOnly(photo);
            }

            OnPropertyChanged(nameof(PhotoSelectionText));
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException)
        {
        }
    }

    private void RemoveWorkAreaPhoto(string path)
    {
        int removedIndex = -1;
        PhotoItem? removedPhoto = null;
        for (int i = 0; i < Photos.Count; i++)
        {
            if (string.Equals(Photos[i].Path, path, StringComparison.OrdinalIgnoreCase))
            {
                removedIndex = i;
                removedPhoto = Photos[i];
                break;
            }
        }

        if (removedPhoto is null)
        {
            return;
        }

        bool removedWasSelected = removedPhoto.IsSelected || ReferenceEquals(SelectedPhoto, removedPhoto);
        Photos.RemoveAt(removedIndex);
        if (ReferenceEquals(_selectionAnchor, removedPhoto))
        {
            _selectionAnchor = null;
        }

        RefreshSelectedPreviewPhotos();
        if (removedWasSelected && SelectedPreviewPhotos.Count == 0 && Photos.Count > 0)
        {
            SelectOnly(Photos[Math.Min(removedIndex, Photos.Count - 1)]);
            return;
        }

        SelectedPhoto = SelectedPreviewPhotos.Count == 1
            ? SelectedPreviewPhotos[0]
            : null;

        if (removedWasSelected)
        {
            LocalWorkbenchVisibility = Visibility.Collapsed;
            _localWorkbenchState = null;
            ClearLocalWorkbenchImages();
            ClearLocalWorkbenchGuide();
            CollapseAllRetouchTabs();
            UpdatePreviewLayout();
        }

        OnPropertyChanged(nameof(PhotoSelectionText));
    }

    private bool ShouldFocusImportedWorkAreaPhoto()
    {
        return CurrentRuntimeWorkMode == RuntimeWorkMode.Viewer &&
               SelectedPreviewPhotos.Count <= 1;
    }

    private void MarkSelfSavedOutputPath(string path)
    {
        string normalizedPath = NormalizeFilePath(path);
        lock (_workAreaWatcherSync)
        {
            _selfSavedOutputPaths.Add(normalizedPath);
        }
    }

    private void UnmarkSelfSavedOutputPath(string path)
    {
        string normalizedPath = NormalizeFilePath(path);
        lock (_workAreaWatcherSync)
        {
            _selfSavedOutputPaths.Remove(normalizedPath);
        }
    }

    private bool IsSelfSavedOutputPath(string path)
    {
        string normalizedPath = NormalizeFilePath(path);
        lock (_workAreaWatcherSync)
        {
            return _selfSavedOutputPaths.Contains(normalizedPath);
        }
    }

    private static string NormalizeFilePath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Path.GetFullPath(path);
    }

    private int GetWorkAreaPhotoInsertIndex(string path)
    {
        for (int i = 0; i < Photos.Count; i++)
        {
            if (ComparePhotoListPathOrder(path, Photos[i].Path) < 0)
            {
                return i;
            }
        }

        return Photos.Count;
    }

    private int ComparePhotoListPathOrder(string leftPath, string rightPath)
    {
        string leftName = Path.GetFileName(leftPath);
        string rightName = Path.GetFileName(rightPath);
        DateTime leftWriteTimeUtc = File.GetLastWriteTimeUtc(leftPath);
        DateTime rightWriteTimeUtc = File.GetLastWriteTimeUtc(rightPath);

        return _appConfig.PhotoListSortMode switch
        {
            PhotoListSortMode.NameDescending =>
                string.Compare(rightName, leftName, StringComparison.OrdinalIgnoreCase),
            PhotoListSortMode.DateNewestFirst =>
                rightWriteTimeUtc != leftWriteTimeUtc
                    ? rightWriteTimeUtc.CompareTo(leftWriteTimeUtc)
                    : string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase),
            PhotoListSortMode.DateOldestFirst =>
                leftWriteTimeUtc != rightWriteTimeUtc
                    ? leftWriteTimeUtc.CompareTo(rightWriteTimeUtc)
                    : string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase),
            _ =>
                string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase)
        };
    }

    private void SetPhotoListSortMode(PhotoListSortMode sortMode)
    {
        _appConfig.PhotoListSortMode = sortMode;
        SaveAppConfig();
        if (Directory.Exists(_appConfig.WorkAreaFolderPath))
        {
            LoadPhotosFromWorkArea(_appConfig.WorkAreaFolderPath);
        }
        else if (Photos.Count > 1)
        {
            string[] currentFiles = Photos.Select(photo => photo.Path).ApplySort(sortMode).ToArray();
            ResetPreviewProxy1200BuildQueue();
            Photos.Clear();
            SelectedPhoto = null;
            SelectedPreviewPhotos.Clear();
            _selectionAnchor = null;
            AddPhotos(currentFiles);
        }
    }

    private static AppConfig ReadAppConfig()
    {
        if (!File.Exists(AppConfigPath))
        {
            return new AppConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(AppConfigPath)) ?? new AppConfig();
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
    }

    private void SaveAppConfig()
    {
        Directory.CreateDirectory(AppConfigDirectory);
        JsonSerializerOptions options = new() { WriteIndented = true };
        File.WriteAllText(AppConfigPath, JsonSerializer.Serialize(_appConfig, options));
    }

    private void SelectOnly(PhotoItem photo)
    {
        foreach (PhotoItem item in Photos)
        {
            item.IsSelected = ReferenceEquals(item, photo);
        }

        _selectionAnchor = photo;
        RefreshSelectedPreviewPhotos();
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();
        SelectedPhoto = photo;
        CollapseAllRetouchTabs();
        UpdatePreviewLayout();
    }

    private void TogglePhotoSelection(PhotoItem photo)
    {
        if (photo.IsSelected && SelectedPreviewPhotos.Count > 1)
        {
            photo.IsSelected = false;
        }
        else if (!photo.IsSelected && Photos.Count(item => item.IsSelected) < 16)
        {
            photo.IsSelected = true;
        }
        else if (!photo.IsSelected)
        {
            return;
        }

        RefreshSelectedPreviewPhotos();
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();

        SelectedPhoto = SelectedPreviewPhotos.Count == 1 ? SelectedPreviewPhotos[0] : null;
        _isEditModeUpperPrewarmQueued = false;
        _isEditModeUpperPrewarmStarted = false;
        CollapseAllRetouchTabs();
        UpdatePreviewLayout();
    }

    private void SelectRange(PhotoItem anchor, PhotoItem target, bool preserveExistingSelection)
    {
        int anchorIndex = Photos.IndexOf(anchor);
        int targetIndex = Photos.IndexOf(target);
        if (anchorIndex < 0 || targetIndex < 0)
        {
            SelectOnly(target);
            return;
        }

        int start = Math.Min(anchorIndex, targetIndex);
        int end = Math.Max(anchorIndex, targetIndex);
        if (!preserveExistingSelection)
        {
            foreach (PhotoItem item in Photos)
            {
                item.IsSelected = false;
            }
        }

        int selectedCount = Photos.Count(item => item.IsSelected);
        for (int i = start; i <= end && selectedCount < 16; i++)
        {
            if (!Photos[i].IsSelected)
            {
                Photos[i].IsSelected = true;
                selectedCount++;
            }
        }

        RefreshSelectedPreviewPhotos();
        LocalWorkbenchVisibility = Visibility.Collapsed;
        _localWorkbenchState = null;
        ClearLocalWorkbenchImages();
        ClearLocalWorkbenchGuide();

        SelectedPhoto = SelectedPreviewPhotos.Count == 1 ? SelectedPreviewPhotos[0] : null;
        CollapseAllRetouchTabs();
        UpdatePreviewLayout();
    }

    private void CollapseAllRetouchTabs()
    {
        SkinRetouchTab?.Collapse();
        WrinkleRetouchTab?.Collapse();
        FaceShapeRetouchTab?.Collapse();
        MouthRetouchTab?.Collapse();
        BodyRetouchTab?.Collapse();
        BackgroundRetouchTab?.Collapse();
        PhotoAdjustRetouchTab?.Collapse();
        EyesRetouchTab?.Collapse();
        NoseRetouchTab?.Collapse();
        RaiseRuntimeWorkModePropertyChanged();
    }

    private void CollapseRetouchTabsExcept(object? expandedTab)
    {
        if (!ReferenceEquals(expandedTab, SkinRetouchTab))
        {
            SkinRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, WrinkleRetouchTab))
        {
            WrinkleRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, FaceShapeRetouchTab))
        {
            FaceShapeRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, MouthRetouchTab))
        {
            MouthRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, BodyRetouchTab))
        {
            BodyRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, BackgroundRetouchTab))
        {
            BackgroundRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, PhotoAdjustRetouchTab))
        {
            PhotoAdjustRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, EyesRetouchTab))
        {
            EyesRetouchTab?.Collapse();
        }

        if (!ReferenceEquals(expandedTab, NoseRetouchTab))
        {
            NoseRetouchTab?.Collapse();
        }
    }

    private void ResetSinglePreviewToFitCanvas()
    {
        if (SelectedPreviewPhotos.Count != 1 || SelectedPhoto is null)
        {
            return;
        }

        if (Math.Abs(PreviewZoomPercent - 100) > 0.01)
        {
            PreviewZoomPercent = 100;
            return;
        }

        UpdatePreviewImageFrame();
    }

    public void NotifyRetouchTabExpanded(object expandedTab)
    {
        if (SelectedPreviewPhotos.Count != 1)
        {
            CollapseAllRetouchTabs();
            return;
        }

        CollapseRetouchTabsExcept(expandedTab);
        RaiseRuntimeWorkModePropertyChanged();
        QueueEditModeUpperPrewarm();
    }

    private void QueueEditModeUpperPrewarm()
    {
        if (_isEditModeUpperPrewarmQueued || _isEditModeUpperPrewarmStarted || SelectedPhoto is null)
        {
            return;
        }

        _isEditModeUpperPrewarmQueued = true;
        _ = Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(450);
            _isEditModeUpperPrewarmQueued = false;
            if (_isEditModeUpperPrewarmStarted || SelectedPhoto is null || CurrentRuntimeWorkMode != RuntimeWorkMode.Edit)
            {
                return;
            }

            _isEditModeUpperPrewarmStarted = true;
            try
            {
                await PrewarmFaceShapeUpperAsync();
            }
            catch (Exception ex)
            {
                MediaPipeStatusText = $"Face Upper warmup failed: {ex.Message}";
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void RetouchExpander_Collapsed(object sender, RoutedEventArgs e)
    {
        if (IsRetouchPanelEventSource(e.OriginalSource))
        {
            RaiseRuntimeWorkModePropertyChanged();
        }
    }

    private void RefreshSelectedPreviewPhotos()
    {
        StopPreviewTilePan();
        StopSinglePreviewPan();
        SelectedPreviewPhotos.Clear();
        foreach (PhotoItem photo in Photos.Where(photo => photo.IsSelected).Take(16))
        {
            SelectedPreviewPhotos.Add(photo);
        }

        OnPropertyChanged(nameof(PhotoSelectionText));
        OnPropertyChanged(nameof(SinglePreviewVisibility));
        OnPropertyChanged(nameof(MultiPreviewVisibility));
        OnPropertyChanged(nameof(PreviewGridColumns));
        OnPropertyChanged(nameof(CanSaveCurrentPhoto));
        RaiseRuntimeWorkModePropertyChanged();
        ApplyMultiPreviewToolLock();

        if (SelectedPreviewPhotos.Count == 1)
        {
            SelectedPhoto = SelectedPreviewPhotos[0];
            ResetSinglePreviewToFitCanvas();
        }
        else
        {
            SelectedPhoto = null;
            CollapseAllRetouchTabs();
        }
    }

    private void ApplyMultiPreviewToolLock()
    {
        if (IsMultiPreviewToolLockActive &&
            !string.Equals(ActiveToolId, "hand", StringComparison.OrdinalIgnoreCase))
        {
            ActiveToolId = "hand";
        }

        UpdateToolboxSelection();
    }

    private void ClearLocalWorkbenchImages()
    {
        LocalWorkbenchImage = null;
        LocalWorkbenchApplyMaskImage = null;
        LocalWorkbenchProtectMaskImage = null;
        LocalWorkbenchBlockMaskImage = null;
        LocalWorkbenchWorkMaskImage = null;
        LocalWorkbenchStatusText = "Preview only. Image changes are not applied yet.";
    }

    private void PreviewSurface_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewLayout();
    }

    private void PreviewSurface_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double step = e.Delta > 0 ? 10 : -10;
        if (SelectedPreviewPhotos.Count > 1)
        {
            bool isGroupZoom = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
                == (ModifierKeys.Control | ModifierKeys.Shift);
            if (isGroupZoom)
            {
                foreach (PhotoItem photo in SelectedPreviewPhotos)
                {
                    photo.MultiPreviewZoomPercent = Math.Round((photo.MultiPreviewZoomPercent + step) / 5) * 5;
                }

                ReapplyMultiPreviewTilePanClamps();
                e.Handled = true;
                return;
            }

            if (TryFindPreviewTileFromSource(e.OriginalSource as DependencyObject, out PhotoItem? targetPhoto, out FrameworkElement? targetTile)
                && targetPhoto is not null
                && targetTile is not null)
            {
                targetPhoto.MultiPreviewZoomPercent = Math.Round((targetPhoto.MultiPreviewZoomPercent + step) / 5) * 5;
                UpdatePreviewTilePan(targetPhoto, targetTile, targetPhoto.MultiPreviewOffsetX, targetPhoto.MultiPreviewOffsetY);
            }

            e.Handled = true;
            return;
        }

        if (SelectedPhoto is not null && SelectedPreviewPhotos.Count == 1)
        {
            double nextZoom = Math.Round((PreviewZoomPercent + step) / 5) * 5;
            PreviewZoomPercent = nextZoom;
        }

        e.Handled = true;
    }

    private bool TryFindPreviewTileFromSource(DependencyObject? source, out PhotoItem? photo, out FrameworkElement? tile)
    {
        photo = null;
        tile = null;

        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.DataContext is PhotoItem candidatePhoto)
            {
                photo = candidatePhoto;
                if (element is Border border && SelectedPreviewPhotos.Contains(candidatePhoto))
                {
                    tile = border;
                    return true;
                }

                tile ??= element;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return photo is not null && tile is not null && SelectedPreviewPhotos.Contains(photo);
    }

    private void BeginRenameSelectedPhoto()
    {
        if (SelectedPhoto is null || SelectedPreviewPhotos.Count != 1)
        {
            return;
        }

        if (_isSinglePreviewPanDragging || _draggingPreviewTile is not null)
        {
            return;
        }

        string currentFilePath = SelectedPhoto.Path;
        string directory = System.IO.Path.GetDirectoryName(currentFilePath) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        string currentFileName = Path.GetFileNameWithoutExtension(SelectedPhoto.FileName);
        string extension = Path.GetExtension(SelectedPhoto.FileName);
        string newFileName = Microsoft.VisualBasic.Interaction.InputBox(
            "새 파일 이름을 입력하세요.\r\n(확장자는 유지됩니다)",
            "이름 바꾸기",
            currentFileName);

        newFileName = newFileName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newFileName))
        {
            return;
        }

        string baseNameInput = Path.GetFileNameWithoutExtension(newFileName);
        baseNameInput = baseNameInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseNameInput))
        {
            return;
        }

        if (string.Equals(baseNameInput, currentFileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (baseNameInput.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            System.Windows.MessageBox.Show(this, "사용할 수 없는 문자가 포함되어 있습니다.", "이름 바꾸기", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string targetPath = System.IO.Path.Combine(directory, baseNameInput + extension);
        if (string.Equals(targetPath, currentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(targetPath))
        {
            System.Windows.MessageBox.Show(this, "같은 이름의 파일이 이미 존재합니다.", "이름 바꾸기", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            File.Move(currentFilePath, targetPath);
            SelectedPhoto.Rename(targetPath);
            OnPropertyChanged(nameof(SelectedPhoto));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"이름 바꾸기 실패: {ex.Message}", "이름 바꾸기", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PreviewSurface_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (CanUseSelectTool())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Arrow;
            return;
        }

        if (CanUseTypeTool())
        {
            if (_isTypeTextCreating && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateTypeTextCreation(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.IBeam;
            return;
        }

        if (CanUseBrushPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateBrushCircle(previewPoint);
            if (_isBrushDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueBrushStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseFillPreview())
        {
            if (_isFillGradientDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueFillGradient(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseEraserPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateEraserCircle(previewPoint);
            if (_isEraserDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueEraserStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseLassoTool())
        {
            if (_isLassoToolDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueLassoTool(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseStampPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateStampCircle(previewPoint);
            if (_isStampDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueStampStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseHealingPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateHealingCircle(previewPoint);
            if (_isHealingDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueHealingStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseBlurSharpPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateBlurSharpCircle(previewPoint);
            if (_isBlurSharpDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueBlurSharpStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseDodgeBurnPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateDodgeBurnCircle(previewPoint);
            if (_isDodgeBurnDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueDodgeBurnStroke(previewPoint);
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseHistoryBrushPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateHistoryBrushCircle(previewPoint);
            if (_isHistoryBrushDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueHistoryBrushStroke(previewPoint);
                e.Handled = true;
                return;
            }
        }

        if (CanUseRectangleSelectionTool())
        {
            if (_isRectangleSelectionRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                RotateRectangleSelection(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            if (_isRectangleSelectionMoving && e.LeftButton == MouseButtonState.Pressed)
            {
                MoveRectangleSelection(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            if (_isRectangleSelectionResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                ResizeRectangleSelection(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            if (_isRectangleSelectionCreating && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateRectangleSelection(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            UpdateRectangleSelectionHoverCursor(e.GetPosition(PreviewSurface));
            return;
        }

        if (CanUsePathSelectionTool())
        {
            if (_isPathSelectionDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinuePathSelectionMove(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = string.Equals(PathSelectionMode, "directselect", StringComparison.OrdinalIgnoreCase)
                ? System.Windows.Input.Cursors.Cross
                : System.Windows.Input.Cursors.SizeAll;
            return;
        }

        if (CanUseLiquifyPreview())
        {
            System.Windows.Point previewPoint = e.GetPosition(PreviewSurface);
            UpdateLiquifyCircle(previewPoint);
            if (_isLiquifyDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                ContinueLiquifyStroke(previewPoint);
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseZoomPreview() || CanUseSelectTemporaryZoomPreview() || _isZoomSelectionDragging)
        {
            if (_isZoomSelectionDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateZoomSelection(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseRulerPreview())
        {
            if (_isRulerDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateRulerMeasurement(e.GetPosition(PreviewSurface));
                e.Handled = true;
                return;
            }

            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseBackgroundColorPickPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseSamplerPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseMagicPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
            return;
        }

        if (CanUseHandPreview() && !_isSinglePreviewPanDragging)
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Hand;
        }

        if (_isFrameSelectionMoving && e.LeftButton == MouseButtonState.Pressed && SelectedPreviewPhotos.Count == 1)
        {
            MoveFrameSelection(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (_isFrameSelectionDragging && e.LeftButton == MouseButtonState.Pressed && SelectedPreviewPhotos.Count == 1)
        {
            UpdateFrameSelection(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        UpdateFrameSelectionHoverCursor(e.GetPosition(PreviewSurface));

        if (_isSinglePreviewPanDragging && e.LeftButton == MouseButtonState.Pressed && SelectedPreviewPhotos.Count == 1)
        {
            System.Windows.Point currentPoint = e.GetPosition(PreviewSurface);
            Vector delta = currentPoint - _singlePreviewPanStartPoint;
            if (delta.Length > 0)
            {
                double nextLeft = _singlePreviewImageLeftStart + delta.X;
                double nextTop = _singlePreviewImageTopStart + delta.Y;
                UpdateSinglePreviewPan(nextLeft, nextTop);
            }

            e.Handled = true;
            return;
        }
    }

    private void PreviewSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        StopTypeTextCreation();
        StopBrushStroke();
        StopEraserStroke();
        StopStampStroke();
        StopHealingStroke();
        StopBlurSharpStroke();
        StopFillGradient();
        StopDodgeBurnStroke();
        StopHistoryBrushStroke();
        StopLassoTool();
        StopPathSelectionMove();
        StopLiquifyStroke();
        StopRectangleSelectionMove();
        StopRectangleSelectionResize();
        StopRectangleSelectionRotate();
        StopRectangleSelection();
        StopFrameSelectionMove();
        StopFrameSelection();
        StopZoomSelection();
        StopRulerMeasurement();
        StopSinglePreviewPan();
        StopPreviewTilePan();
    }

    private void PreviewSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isSinglePreviewPanDragging || _draggingPreviewTile is not null || _isFrameSelectionDragging || _isFrameSelectionMoving || _isZoomSelectionDragging || _isRulerDragging || _isLassoToolDragging || _isPathSelectionDragging || _isBrushDragging || _isEraserDragging || _isStampDragging || _isHealingDragging || _isBlurSharpDragging || _isFillGradientDragging || _isDodgeBurnDragging || _isHistoryBrushDragging || _isLiquifyDragging || _isRectangleSelectionCreating || _isRectangleSelectionMoving || _isRectangleSelectionResizing || _isRectangleSelectionRotating || _isTypeTextCreating || _isTypeTextDragging)
        {
            return;
        }

        FocusPreviewSurfaceForToolInput();

        if (CanUseBackgroundColorPickPreview())
        {
            ApplyBackgroundPickedColorAtPreviewPoint(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseTypeTool())
        {
            BeginTypeTextCreation(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseBrushPreview())
        {
            StartBrushStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseFillPreview())
        {
            StartFillToolAtPreviewPoint(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseEraserPreview())
        {
            StartEraserStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseLassoTool())
        {
            StartLassoTool(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseStampPreview())
        {
            StartStampStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseHealingPreview())
        {
            StartHealingStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseBlurSharpPreview())
        {
            StartBlurSharpStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseDodgeBurnPreview())
        {
            StartDodgeBurnStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseHistoryBrushPreview())
        {
            StartHistoryBrushStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseLiquifyPreview())
        {
            StartLiquifyStroke(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseSelectTemporaryZoomPreview())
        {
            StartZoomSelection(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseZoomPreview())
        {
            StartZoomSelection(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseRulerPreview())
        {
            StartRulerMeasurement(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseSamplerPreview())
        {
            SampleColorAtPreviewPoint(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUseMagicPreview())
        {
            bool addToSelection = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            ApplyMagicSelectAtPreviewPoint(e.GetPosition(PreviewSurface), addToSelection);
            e.Handled = true;
            return;
        }

        if (CanUsePathTool())
        {
            AddPathAnchorAtPreviewPoint(e.GetPosition(PreviewSurface));
            e.Handled = true;
            return;
        }

        if (CanUsePathSelectionTool())
        {
            if (TryStartPathSelectionMove(e.GetPosition(PreviewSurface)))
            {
                e.Handled = true;
            }

            return;
        }

        if (CanUseRectangleSelectionTool())
        {
            System.Windows.Point startPoint = e.GetPosition(PreviewSurface);
            RectangleSelectionHitZone hitZone = GetRectangleSelectionHitZone(startPoint);
            if (hitZone is RectangleSelectionHitZone.RotateTopLeft or
                RectangleSelectionHitZone.RotateTopRight or
                RectangleSelectionHitZone.RotateBottomLeft or
                RectangleSelectionHitZone.RotateBottomRight)
            {
                StartRectangleSelectionRotate(startPoint);
            }
            else if (hitZone == RectangleSelectionHitZone.Inside)
            {
                StartRectangleSelectionMove(startPoint);
            }
            else if (hitZone != RectangleSelectionHitZone.None)
            {
                StartRectangleSelectionResize(startPoint, hitZone);
            }
            else
            {
                StartRectangleSelection(startPoint);
            }

            e.Handled = true;
            return;
        }

        if (CanStartFrameSelection())
        {
            System.Windows.Point startPoint = e.GetPosition(PreviewSurface);
            if (IsPointNearFrameSelectionOutline(startPoint))
            {
                StartFrameSelectionMove(startPoint);
            }
            else
            {
                StartFrameSelection(startPoint);
            }

            e.Handled = true;
            return;
        }

        if (CanUseHandPreview())
        {
            StartSinglePreviewPan(e.GetPosition(PreviewSurface));
            e.Handled = true;
        }
    }

    private void PreviewSurface_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        BrushCircleVisibility = Visibility.Collapsed;
        EraserCircleVisibility = Visibility.Collapsed;
        StampCircleVisibility = Visibility.Collapsed;
        HealingCircleVisibility = Visibility.Collapsed;
        BlurSharpCircleVisibility = Visibility.Collapsed;
        DodgeBurnCircleVisibility = Visibility.Collapsed;
        HistoryBrushCircleVisibility = Visibility.Collapsed;
        LiquifyCircleVisibility = Visibility.Collapsed;
        HideCropRotateCursor();
        if (IsToolInBucket(ActiveToolId, ToolBucket.CursorResetOnLeave))
        {
            PreviewSurface.Cursor = null;
        }
    }

    private bool CanUseSinglePreviewTool()
    {
        return SelectedPhoto is not null &&
               SelectedPreviewPhotos.Count == 1;
    }

    private RuntimeWorkMode ResolveRuntimeWorkMode()
    {
        return SelectedPreviewPhotos.Count switch
        {
            <= 0 => RuntimeWorkMode.Viewer,
            1 => HasExpandedRetouchTab() ? RuntimeWorkMode.Edit : RuntimeWorkMode.Viewer,
            _ => RuntimeWorkMode.Multi
        };
    }

    private void RaiseRuntimeWorkModePropertyChanged()
    {
        OnPropertyChanged(nameof(CurrentRuntimeWorkMode));
        OnPropertyChanged(nameof(RuntimeWorkModeDisplayText));
        OnPropertyChanged(nameof(RetouchPanelAvailable));
        OnPropertyChanged(nameof(RetouchPanelEditingEnabled));
        OnPropertyChanged(nameof(CanSaveCurrentPhoto));
    }

    private bool HasExpandedRetouchTab()
    {
        if (RetouchPanelStack is null)
        {
            return false;
        }

        return FindVisualDescendants<Expander>(RetouchPanelStack)
            .Any(expander => expander.IsExpanded);
    }

    private bool IsRetouchPanelEventSource(object? source)
    {
        return source is DependencyObject sourceObject &&
               RetouchPanelStack is not null &&
               IsVisualDescendantOf(sourceObject, RetouchPanelStack);
    }

    private static bool IsVisualDescendantOf(DependencyObject source, DependencyObject ancestor)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
        }

        return false;
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in FindVisualDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private bool IsToolInBucket(string toolId, ToolBucket bucket)
    {
        if (!ToolBuckets.TryGetValue(bucket, out HashSet<string>? values))
        {
            return false;
        }

        return values.Contains(toolId);
    }
    private bool TryPreviewPointToImagePoint(System.Windows.Point previewPoint, out System.Windows.Point imagePoint)
    {
        imagePoint = default;
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        if (!TryGetPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out double scale) ||
            scale <= 0)
        {
            return false;
        }

        System.Windows.Point transformedPreviewPoint = IsCropImageRotationActive
            ? RotatePoint(previewPoint, GetPreviewImageCenter(), -CropRotationAngle)
            : previewPoint;
        double imageX = (transformedPreviewPoint.X - offsetX) / scale;
        double imageY = (transformedPreviewPoint.Y - offsetY) / scale;
        if (imageX < 0 || imageX > source.PixelWidth || imageY < 0 || imageY > source.PixelHeight)
        {
            return false;
        }

        imagePoint = new System.Windows.Point(
            Math.Clamp(imageX, 0, Math.Max(0, source.PixelWidth - 1)),
            Math.Clamp(imageY, 0, Math.Max(0, source.PixelHeight - 1)));
        return true;
    }

    private bool TryPreviewPointToImagePixel(System.Windows.Point previewPoint, out int pixelX, out int pixelY)
    {
        pixelX = 0;
        pixelY = 0;
        if (SelectedPhoto is not PhotoItem photo ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            return false;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        System.Windows.Point transformedPreviewPoint = IsCropImageRotationActive
            ? RotatePoint(previewPoint, GetPreviewImageCenter(), -CropRotationAngle)
            : previewPoint;
        double relativeX = (transformedPreviewPoint.X - PreviewImageLeft) / PreviewImageWidth;
        double relativeY = (transformedPreviewPoint.Y - PreviewImageTop) / PreviewImageHeight;
        if (relativeX < 0 || relativeX > 1 || relativeY < 0 || relativeY > 1)
        {
            return false;
        }

        pixelX = Math.Clamp((int)Math.Floor(relativeX * source.PixelWidth), 0, source.PixelWidth - 1);
        pixelY = Math.Clamp((int)Math.Floor(relativeY * source.PixelHeight), 0, source.PixelHeight - 1);
        return true;
    }

    private System.Windows.Point ClampPointToPreviewImage(System.Windows.Point point)
    {
        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        if (PreviewImageWidth <= 0 || PreviewImageHeight <= 0)
        {
            return point;
        }

        if (IsCropImageRotationActive)
        {
            System.Windows.Point unrotatedPoint = RotatePoint(point, GetPreviewImageCenter(), -CropRotationAngle);
            System.Windows.Point clampedUnrotatedPoint = new(
                Math.Clamp(unrotatedPoint.X, imageLeft, imageRight),
                Math.Clamp(unrotatedPoint.Y, imageTop, imageBottom));
            return RotatePoint(clampedUnrotatedPoint, GetPreviewImageCenter(), CropRotationAngle);
        }

        return new System.Windows.Point(
            Math.Clamp(point.X, imageLeft, imageRight),
            Math.Clamp(point.Y, imageTop, imageBottom));
    }

    private System.Windows.Point GetPreviewImageCenter()
    {
        return new System.Windows.Point(
            PreviewImageLeft + (PreviewImageWidth * 0.5),
            PreviewImageTop + (PreviewImageHeight * 0.5));
    }

    private void UpdateSinglePreviewPan(double left, double top)
    {
        double imageWidth = PreviewImageWidth;
        double imageHeight = PreviewImageHeight;
        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;

        if (surfaceWidth <= 0 || surfaceHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return;
        }

        double minLeft = surfaceWidth - imageWidth;
        double maxLeft = 0;
        double minTop = surfaceHeight - imageHeight;
        double maxTop = 0;

        if (imageWidth <= surfaceWidth)
        {
            minLeft = maxLeft = (surfaceWidth - imageWidth) * 0.5;
        }

        if (imageHeight <= surfaceHeight)
        {
            minTop = maxTop = (surfaceHeight - imageHeight) * 0.5;
        }

        PreviewImageLeft = Math.Clamp(left, minLeft, maxLeft);
        PreviewImageTop = Math.Clamp(top, minTop, maxTop);
        UpdateMediaPipePreviewOverlay();
        UpdateFaceShapeProjectionDebugOverlay();
    }

    private void UpdatePreviewLayout()
    {
        UpdateLocalWorkbenchGuide();
        UpdatePreviewImageFrame();
        UpdateRectangleSelectionOverlayFromImageRect();
        UpdateMagicSelectionVisibility();
        UpdatePathAnchorPointPositions();
        RebuildPathToolGeometry();
        RebuildLassoToolGeometry();
        UpdateTypeTextItemPositions();
        UpdateTypeToolVisualState();

        if (SelectedPreviewPhotos.Count > 1)
        {
            ReapplyMultiPreviewTilePanClamps();
        }
    }

    private void PhotoAdjustRetouchTab_CurvePreviewChanged(object? sender, TonePreviewChangedEventArgs e)
    {
        ApplyToneCurvePreview(e.UseFastPreview);
    }

    private void ApplyToneCurvePreview(bool useFastPreview = false)
    {
        ApplyToneCurveOnlyPreview(useFastPreview);
    }

    private void ApplyToneCurveOnlyPreview(bool useFastPreview = false)
    {
        if (SelectedPhoto is not PhotoItem photo ||
            PhotoAdjustRetouchTab?.CurvePreviewBaseSource is not BitmapSource baseSource)
        {
            return;
        }

        CancelToneCurvePreviewRender();

        if (!PhotoAdjustRetouchTab.CurveState.HasEffectiveAdjustment() &&
            !PhotoAdjustRetouchTab.HasEffectiveToneQuickAdjustment)
        {
            photo.SetAdjustedImage(baseSource);
            UpdatePreviewLayout();
            PhotoAdjustRetouchTab.QueueCurveHistogramRefresh(baseSource);
            return;
        }

        BitmapSource renderSource = GetToneCurveRenderSource(baseSource, useFastPreview);
        ToneCurveEditorState curveState = PhotoAdjustRetouchTab.CurveState;
        byte[] allLut = curveState.BuildCurveLookupTable(CurveChannel.All);
        byte[] redLut = curveState.BuildCurveLookupTable(CurveChannel.Red);
        byte[] greenLut = curveState.BuildCurveLookupTable(CurveChannel.Green);
        byte[] blueLut = curveState.BuildCurveLookupTable(CurveChannel.Blue);
        double amount = curveState.Value / 100.0;
        double exposure = PhotoAdjustRetouchTab.ToneExposureValue;
        double contrast = PhotoAdjustRetouchTab.ToneContrastValue;
        double saturation = PhotoAdjustRetouchTab.ToneSaturationValue;
        double whiteBalance = PhotoAdjustRetouchTab.ToneWhiteBalanceValue;
        double sharpness = PhotoAdjustRetouchTab.ToneSharpnessValue;

        CancellationTokenSource cancellation = new();
        CancellationToken cancellationToken = cancellation.Token;
        _toneCurvePreviewRenderCancellation = cancellation;
        int renderVersion = Interlocked.Increment(ref _toneCurvePreviewRenderVersion);

        _ = Task.Run(async () =>
        {
            try
            {
                BitmapSource preview = ApplyToneCurveToBitmap(
                    renderSource,
                    allLut,
                    redLut,
                    greenLut,
                    blueLut,
                    amount,
                    exposure,
                    contrast,
                    saturation,
                    whiteBalance,
                    sharpness,
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested ||
                        renderVersion != _toneCurvePreviewRenderVersion ||
                        !ReferenceEquals(SelectedPhoto, photo))
                    {
                        return;
                    }

                    photo.SetAdjustedImage(preview);
                    UpdatePreviewLayout();
                    PhotoAdjustRetouchTab.QueueCurveHistogramRefresh(preview);
                });
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                try
                {
                    if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (ReferenceEquals(_toneCurvePreviewRenderCancellation, cancellation))
                            {
                                _toneCurvePreviewRenderCancellation = null;
                            }
                        });
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                cancellation.Dispose();
            }
        });
    }

    private BitmapSource GetToneCurveRenderSource(BitmapSource baseSource, bool useFastPreview)
    {
        BitmapSource safeSource = baseSource.IsFrozen
            ? baseSource
            : CloneBitmapSource(baseSource);

        if (!useFastPreview)
        {
            return safeSource;
        }

        int longSide = Math.Max(safeSource.PixelWidth, safeSource.PixelHeight);
        if (longSide <= ToneCurveFastPreviewLongSide)
        {
            return safeSource;
        }

        if (ReferenceEquals(_toneCurveFastPreviewBaseSource, baseSource) &&
            _toneCurveFastPreviewSource is not null)
        {
            return _toneCurveFastPreviewSource;
        }

        double scale = ToneCurveFastPreviewLongSide / (double)longSide;
        TransformedBitmap previewSource = new(safeSource, new ScaleTransform(scale, scale));
        previewSource.Freeze();
        _toneCurveFastPreviewBaseSource = baseSource;
        _toneCurveFastPreviewSource = previewSource;
        return previewSource;
    }

    private void ClearToneCurveFastPreviewCache()
    {
        _toneCurveFastPreviewBaseSource = null;
        _toneCurveFastPreviewSource = null;
    }

    private void CancelToneCurvePreviewRender()
    {
        if (_toneCurvePreviewRenderCancellation is null)
        {
            return;
        }

        _toneCurvePreviewRenderCancellation.Cancel();
        _toneCurvePreviewRenderCancellation = null;
        Interlocked.Increment(ref _toneCurvePreviewRenderVersion);
    }

    private static BitmapSource ApplyToneCurveToBitmap(
        BitmapSource source,
        ToneCurveEditorState curveState,
        double exposure,
        double contrast,
        double saturation,
        double whiteBalance,
        double sharpness)
    {
        return ApplyToneCurveToBitmap(
            source,
            curveState.BuildCurveLookupTable(CurveChannel.All),
            curveState.BuildCurveLookupTable(CurveChannel.Red),
            curveState.BuildCurveLookupTable(CurveChannel.Green),
            curveState.BuildCurveLookupTable(CurveChannel.Blue),
            curveState.Value / 100.0,
            exposure,
            contrast,
            saturation,
            whiteBalance,
            sharpness,
            CancellationToken.None);
    }

    private static BitmapSource ApplyToneCurveToBitmap(
        BitmapSource source,
        byte[] allLut,
        byte[] redLut,
        byte[] greenLut,
        byte[] blueLut,
        double amount,
        double exposure,
        double contrast,
        double saturation,
        double whiteBalance,
        double sharpness,
        CancellationToken cancellationToken)
    {
        BitmapSource bgraSource = EnsureBgraBitmapSource(source);
        int stride = bgraSource.PixelWidth * 4;
        byte[] pixels = new byte[stride * bgraSource.PixelHeight];
        bgraSource.CopyPixels(pixels, stride, 0);

        for (int y = 0; y < bgraSource.PixelHeight; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int rowStart = y * stride;
            for (int index = rowStart; index < rowStart + stride; index += 4)
            {
                byte originalBlue = pixels[index];
                byte originalGreen = pixels[index + 1];
                byte originalRed = pixels[index + 2];

                byte adjustedBlue = blueLut[allLut[originalBlue]];
                byte adjustedGreen = greenLut[allLut[originalGreen]];
                byte adjustedRed = redLut[allLut[originalRed]];

                double blue = BlendToneCurveChannel(originalBlue, adjustedBlue, amount);
                double green = BlendToneCurveChannel(originalGreen, adjustedGreen, amount);
                double red = BlendToneCurveChannel(originalRed, adjustedRed, amount);
                ApplyToneQuickControls(ref red, ref green, ref blue, exposure, contrast, saturation, whiteBalance);

                pixels[index] = ClampToneChannel(blue);
                pixels[index + 1] = ClampToneChannel(green);
                pixels[index + 2] = ClampToneChannel(red);
            }
        }

        if (Math.Abs(sharpness) > 0.001)
        {
            ApplyToneSharpness(pixels, bgraSource.PixelWidth, bgraSource.PixelHeight, stride, sharpness, cancellationToken);
        }

        WriteableBitmap adjusted = new(
            bgraSource.PixelWidth,
            bgraSource.PixelHeight,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null);
        adjusted.WritePixels(new Int32Rect(0, 0, bgraSource.PixelWidth, bgraSource.PixelHeight), pixels, stride, 0);
        adjusted.Freeze();
        return adjusted;
    }

    private static byte BlendToneCurveChannel(byte originalValue, byte adjustedValue, double amount)
    {
        return (byte)Math.Clamp((int)Math.Round(originalValue + ((adjustedValue - originalValue) * amount)), 0, 255);
    }

    private static void ApplyToneQuickControls(
        ref double red,
        ref double green,
        ref double blue,
        double exposure,
        double contrast,
        double saturation,
        double whiteBalance)
    {
        if (Math.Abs(exposure) > 0.001)
        {
            double exposureOffset = exposure * 2.55;
            red += exposureOffset;
            green += exposureOffset;
            blue += exposureOffset;
        }

        if (Math.Abs(contrast) > 0.001)
        {
            double contrastFactor = 1.0 + (contrast / 100.0);
            red = ((red - 128.0) * contrastFactor) + 128.0;
            green = ((green - 128.0) * contrastFactor) + 128.0;
            blue = ((blue - 128.0) * contrastFactor) + 128.0;
        }

        if (Math.Abs(saturation) > 0.001)
        {
            double saturationFactor = Math.Max(0, 1.0 + (saturation / 100.0));
            double luminance = (red * 0.2126) + (green * 0.7152) + (blue * 0.0722);
            red = luminance + ((red - luminance) * saturationFactor);
            green = luminance + ((green - luminance) * saturationFactor);
            blue = luminance + ((blue - luminance) * saturationFactor);
        }

        if (Math.Abs(whiteBalance) > 0.001)
        {
            double shift = whiteBalance * 0.32;
            red += shift;
            blue -= shift;
        }
    }

    private static void ApplyToneSharpness(
        byte[] pixels,
        int pixelWidth,
        int pixelHeight,
        int stride,
        double sharpness,
        CancellationToken cancellationToken = default)
    {
        byte[] sourcePixels = (byte[])pixels.Clone();
        double amount = Math.Clamp(sharpness / 100.0, -1.0, 1.0);

        for (int y = 1; y < pixelHeight - 1; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 1; x < pixelWidth - 1; x++)
            {
                int index = (y * stride) + (x * 4);
                for (int channel = 0; channel < 3; channel++)
                {
                    int sum = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        int rowOffset = (y + ky) * stride;
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            sum += sourcePixels[rowOffset + ((x + kx) * 4) + channel];
                        }
                    }

                    double original = sourcePixels[index + channel];
                    double blurred = sum / 9.0;
                    double adjusted = amount > 0
                        ? original + ((original - blurred) * amount * 1.2)
                        : original + ((blurred - original) * -amount);
                    pixels[index + channel] = ClampToneChannel(adjusted);
                }
            }
        }
    }

    private static byte ClampToneChannel(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private void ReapplyMultiPreviewTilePanClamps()
    {
        if (MultiPreviewItemsControl is null || SelectedPreviewPhotos.Count <= 1)
        {
            return;
        }

        MultiPreviewItemsControl.UpdateLayout();

        foreach (PhotoItem photo in SelectedPreviewPhotos)
        {
            if (!TryGetMultiPreviewTile(photo, out FrameworkElement? tile) || tile is null)
            {
                continue;
            }

            UpdatePreviewTilePan(photo, tile, photo.MultiPreviewOffsetX, photo.MultiPreviewOffsetY);
        }
    }

    private bool TryGetMultiPreviewTile(PhotoItem photo, out FrameworkElement? tile)
    {
        tile = null;
        if (MultiPreviewItemsControl is null)
        {
            return false;
        }

        DependencyObject? container = MultiPreviewItemsControl.ItemContainerGenerator.ContainerFromItem(photo);
        if (container is null)
        {
            return false;
        }

        Queue<DependencyObject> queue = new();
        queue.Enqueue(container);
        while (queue.Count > 0)
        {
            DependencyObject current = queue.Dequeue();

            if (current is Border border && ReferenceEquals(border.DataContext, photo))
            {
                tile = border;
                return true;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
            }
        }

        return false;
    }

    private void UpdatePreviewImageFrame()
    {
        if (SelectedPhoto is null)
        {
            PreviewImageLeft = 0;
            PreviewImageTop = 0;
            PreviewImageWidth = 0;
            PreviewImageHeight = 0;
            return;
        }

        BitmapSource source = GetSinglePreviewBitmapSource(SelectedPhoto);
        double imageWidth = source.PixelWidth;
        double imageHeight = source.PixelHeight;
        if (TryGetFaceShapeHeadPoseDragPreviewFrameSize(SelectedPhoto, out double faceShapeFrameWidth, out double faceShapeFrameHeight))
        {
            imageWidth = faceShapeFrameWidth;
            imageHeight = faceShapeFrameHeight;
        }
        else if (TryGetBackgroundPreviewFrameSize(SelectedPhoto, out double backgroundFrameWidth, out double backgroundFrameHeight))
        {
            imageWidth = backgroundFrameWidth;
            imageHeight = backgroundFrameHeight;
        }

        if (!TryGetPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            return;
        }

        PreviewImageLeft = offsetX;
        PreviewImageTop = offsetY;
        PreviewImageWidth = imageWidth * scale;
        PreviewImageHeight = imageHeight * scale;
        UpdateMediaPipePreviewOverlay();
        UpdateFaceShapeProjectionDebugOverlay();
    }

    private void UpdateLocalWorkbenchGuide()
    {
        if (_localWorkbenchState is null || SelectedPhoto is null)
        {
            ClearLocalWorkbenchGuide();
            return;
        }

        LocalWorkbenchCoordinateMap map = _localWorkbenchState.CoordinateMap;
        double imageWidth = map.OriginalImageRect.Width;
        double imageHeight = map.OriginalImageRect.Height;
        if (!TryGetPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            ClearLocalWorkbenchGuide();
            return;
        }
        Int32Rect workArea = map.WorkAreaRectOriginal;

        LocalWorkbenchGuideLeft = offsetX + workArea.X * scale;
        LocalWorkbenchGuideTop = offsetY + workArea.Y * scale;
        LocalWorkbenchGuideWidth = workArea.Width * scale;
        LocalWorkbenchGuideHeight = workArea.Height * scale;
    }

    private void ClearLocalWorkbenchGuide()
    {
        LocalWorkbenchGuideLeft = 0;
        LocalWorkbenchGuideTop = 0;
        LocalWorkbenchGuideWidth = 0;
        LocalWorkbenchGuideHeight = 0;
    }

    private bool TryGetPreviewImageTransform(
        double imageWidth,
        double imageHeight,
        out double offsetX,
        out double offsetY,
        out double scale)
    {
        offsetX = 0;
        offsetY = 0;
        scale = 1;

        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;
        if (surfaceWidth <= 0 || surfaceHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        scale = Math.Min(surfaceWidth / imageWidth, surfaceHeight / imageHeight) * (PreviewZoomPercent / 100.0);
        double displayedWidth = imageWidth * scale;
        double displayedHeight = imageHeight * scale;
        offsetX = (surfaceWidth - displayedWidth) * 0.5;
        offsetY = (surfaceHeight - displayedHeight) * 0.5;
        return true;
    }

    private void LoadEditorHistoryForSelectedPhoto()
    {
        _editorUndoHistory.Clear();
        _editorRedoHistory.Clear();
        HistoryPanelItems.Clear();
        SelectedHistoryPanelItem = null;

        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        string normalizedPath = NormalizeFilePath(photo.Path);
        if (EnableEditorHistoryPersistence &&
            TryGetEditorHistorySession(normalizedPath, out EditorHistorySession? session) &&
            session is { UndoHistory.Count: > 0 })
        {
            _editorUndoHistory.AddRange(session.UndoHistory);
            _editorRedoHistory.AddRange(session.RedoHistory);
            RestoreEditorHistoryState(_editorUndoHistory[^1]);
            RefreshEditorHistoryPanel();
            return;
        }

        PushEditorHistorySnapshot("Open Photo", $"Session started for {photo.FileName}");
    }

    private bool TryGetEditorHistorySession(string normalizedPath, out EditorHistorySession? session)
    {
        if (_editorHistorySessionsByPath.TryGetValue(normalizedPath, out session))
        {
            return true;
        }

        if (TryLoadEditorHistorySessionFromDisk(normalizedPath, out session) &&
            session is not null)
        {
            _editorHistorySessionsByPath[normalizedPath] = session;
            return true;
        }

        session = null;
        return false;
    }

    private void StoreCurrentEditorHistorySession(PhotoItem? photo, bool persistToDisk)
    {
        if (!EnableEditorHistoryPersistence ||
            photo is null ||
            _editorUndoHistory.Count == 0)
        {
            return;
        }

        string normalizedPath = NormalizeFilePath(photo.Path);
        EditorHistorySession session = new(
            _editorUndoHistory.ToList(),
            _editorRedoHistory.ToList());
        _editorHistorySessionsByPath[normalizedPath] = session;

        if (!persistToDisk)
        {
            return;
        }

        try
        {
            SaveEditorHistorySessionToDisk(normalizedPath, photo.Path, session);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
        }
    }

    private void PushEditorHistorySnapshot(string title, string detail)
    {
        if (_isRestoringEditorHistory || SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        EditorHistoryState snapshot = CaptureEditorHistoryState(photo, title, detail);
        _editorUndoHistory.Add(snapshot);
        if (_editorUndoHistory.Count > MaxEditorHistoryEntries)
        {
            _editorUndoHistory.RemoveAt(0);
        }

        _editorRedoHistory.Clear();
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(photo, persistToDisk: false);
    }

    private EditorHistoryState CaptureEditorHistoryState(PhotoItem photo, string title, string detail)
    {
        BitmapSource? adjustedImage = photo.Image is BitmapSource image && !ReferenceEquals(image, photo.BaseImage)
            ? CloneBitmapSource(image)
            : null;

        List<PreviewTextItemSnapshot> textItems = new(TypeTextItems.Count);
        foreach (PreviewTextItem item in TypeTextItems)
        {
            textItems.Add(new PreviewTextItemSnapshot(
                item.Id,
                item.Text,
                item.OriginalX,
                item.OriginalY,
                item.OriginalWidth,
                item.OriginalHeight,
                item.IsPointText,
                item.FontFamilyName,
                item.FontSize,
                item.FontColor,
                item.OpacityPercent,
                item.IsBold,
                item.IsItalic,
                item.TextAlignment));
        }

        return new EditorHistoryState(photo.Path, adjustedImage, textItems, title, detail, DateTime.Now);
    }

    private void TryUndoEditorHistory()
    {
        if (_editorUndoHistory.Count <= 1 || SelectedPhoto is null)
        {
            return;
        }

        EditorHistoryState current = _editorUndoHistory[^1];
        _editorUndoHistory.RemoveAt(_editorUndoHistory.Count - 1);
        _editorRedoHistory.Add(current);
        RestoreEditorHistoryState(_editorUndoHistory[^1]);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(SelectedPhoto, persistToDisk: false);
    }

    private void TryRedoEditorHistory()
    {
        if (_editorRedoHistory.Count == 0 || SelectedPhoto is null)
        {
            return;
        }

        EditorHistoryState snapshot = _editorRedoHistory[^1];
        _editorRedoHistory.RemoveAt(_editorRedoHistory.Count - 1);
        _editorUndoHistory.Add(snapshot);
        RestoreEditorHistoryState(snapshot);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(SelectedPhoto, persistToDisk: false);
    }

    private void HistoryPanelListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (FindHistoryPanelItemFromOriginalSource(e.OriginalSource) is not HistoryPanelItem item)
        {
            return;
        }

        RestoreEditorHistoryAt(item.HistoryIndex);
        e.Handled = true;
    }

    private void RestoreEditorHistoryAt(int historyIndex)
    {
        if (SelectedPhoto is null ||
            historyIndex < 0 ||
            historyIndex >= _editorUndoHistory.Count)
        {
            return;
        }

        int currentIndex = _editorUndoHistory.Count - 1;
        if (historyIndex == currentIndex)
        {
            return;
        }

        for (int i = currentIndex; i > historyIndex; i--)
        {
            _editorRedoHistory.Add(_editorUndoHistory[i]);
            _editorUndoHistory.RemoveAt(i);
        }

        RestoreEditorHistoryState(_editorUndoHistory[^1]);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(SelectedPhoto, persistToDisk: false);
    }

    private static HistoryPanelItem? FindHistoryPanelItemFromOriginalSource(object originalSource)
    {
        DependencyObject? current = originalSource as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: HistoryPanelItem item })
            {
                return item;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void SaveEditorHistorySessionToDisk(
        string normalizedPath,
        string photoPath,
        EditorHistorySession session)
    {
        string sessionDirectory = GetEditorHistorySessionDirectory(normalizedPath);
        if (Directory.Exists(sessionDirectory))
        {
            Directory.Delete(sessionDirectory, recursive: true);
        }

        Directory.CreateDirectory(sessionDirectory);

        PersistedEditorHistoryDocument document = new()
        {
            PhotoPath = photoPath,
            NormalizedPath = normalizedPath,
            SavedAtUtc = DateTime.UtcNow,
            UndoHistory = PersistEditorHistoryStates(session.UndoHistory, sessionDirectory, "undo"),
            RedoHistory = PersistEditorHistoryStates(session.RedoHistory, sessionDirectory, "redo")
        };

        JsonSerializerOptions options = new() { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(sessionDirectory, "history.json"),
            JsonSerializer.Serialize(document, options),
            Encoding.UTF8);
    }

    private static List<PersistedEditorHistoryState> PersistEditorHistoryStates(
        IReadOnlyList<EditorHistoryState> states,
        string sessionDirectory,
        string prefix)
    {
        List<PersistedEditorHistoryState> persistedStates = new(states.Count);
        for (int i = 0; i < states.Count; i++)
        {
            EditorHistoryState state = states[i];
            string? adjustedImageFileName = null;
            if (state.AdjustedImage is not null)
            {
                adjustedImageFileName = $"{prefix}_{i:000}.png";
                SaveBitmapSourceAsPng(state.AdjustedImage, Path.Combine(sessionDirectory, adjustedImageFileName));
            }

            persistedStates.Add(new PersistedEditorHistoryState
            {
                Title = state.Title,
                Detail = state.Detail,
                Timestamp = state.Timestamp,
                AdjustedImageFile = adjustedImageFileName,
                TextItems = state.TextItems.Select(PersistPreviewTextItemSnapshot).ToList()
            });
        }

        return persistedStates;
    }

    private static PersistedPreviewTextItemSnapshot PersistPreviewTextItemSnapshot(PreviewTextItemSnapshot snapshot)
    {
        return new PersistedPreviewTextItemSnapshot
        {
            Id = snapshot.Id,
            Text = snapshot.Text,
            OriginalX = snapshot.OriginalX,
            OriginalY = snapshot.OriginalY,
            OriginalWidth = snapshot.OriginalWidth,
            OriginalHeight = snapshot.OriginalHeight,
            IsPointText = snapshot.IsPointText,
            FontFamilyName = snapshot.FontFamilyName,
            FontSize = snapshot.FontSize,
            FontColor = snapshot.FontColor,
            OpacityPercent = snapshot.OpacityPercent,
            IsBold = snapshot.IsBold,
            IsItalic = snapshot.IsItalic,
            TextAlignment = snapshot.TextAlignment
        };
    }

    private static bool TryLoadEditorHistorySessionFromDisk(string normalizedPath, out EditorHistorySession? session)
    {
        session = null;
        string sessionDirectory = GetEditorHistorySessionDirectory(normalizedPath);
        string historyPath = Path.Combine(sessionDirectory, "history.json");
        if (!File.Exists(historyPath))
        {
            return false;
        }

        try
        {
            PersistedEditorHistoryDocument? document = JsonSerializer.Deserialize<PersistedEditorHistoryDocument>(
                File.ReadAllText(historyPath, Encoding.UTF8));
            if (document is null ||
                !string.Equals(document.NormalizedPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            session = new EditorHistorySession(
                LoadEditorHistoryStates(document.PhotoPath, document.UndoHistory, sessionDirectory),
                LoadEditorHistoryStates(document.PhotoPath, document.RedoHistory, sessionDirectory));
            return session.UndoHistory.Count > 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
            session = null;
            return false;
        }
    }

    private static List<EditorHistoryState> LoadEditorHistoryStates(
        string photoPath,
        IReadOnlyList<PersistedEditorHistoryState> persistedStates,
        string sessionDirectory)
    {
        List<EditorHistoryState> states = new(persistedStates.Count);
        foreach (PersistedEditorHistoryState persistedState in persistedStates)
        {
            BitmapSource? adjustedImage = null;
            if (!string.IsNullOrWhiteSpace(persistedState.AdjustedImageFile))
            {
                string adjustedImagePath = Path.Combine(sessionDirectory, persistedState.AdjustedImageFile);
                if (File.Exists(adjustedImagePath))
                {
                    adjustedImage = LoadBitmapSourceFromFile(adjustedImagePath);
                }
            }

            states.Add(new EditorHistoryState(
                photoPath,
                adjustedImage,
                persistedState.TextItems.Select(RestorePreviewTextItemSnapshot).ToList(),
                persistedState.Title,
                persistedState.Detail,
                persistedState.Timestamp));
        }

        return states;
    }

    private static PreviewTextItemSnapshot RestorePreviewTextItemSnapshot(PersistedPreviewTextItemSnapshot snapshot)
    {
        return new PreviewTextItemSnapshot(
            snapshot.Id,
            snapshot.Text,
            snapshot.OriginalX,
            snapshot.OriginalY,
            snapshot.OriginalWidth,
            snapshot.OriginalHeight,
            snapshot.IsPointText,
            snapshot.FontFamilyName,
            snapshot.FontSize,
            snapshot.FontColor,
            snapshot.OpacityPercent,
            snapshot.IsBold,
            snapshot.IsItalic,
            snapshot.TextAlignment);
    }

    private static void SaveBitmapSourceAsPng(BitmapSource source, string path)
    {
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }

    private static string GetEditorHistorySessionDirectory(string normalizedPath)
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath)));
        return Path.Combine(EditorHistoryDirectory, hash);
    }

    private void PruneEditorHistoryOutsideWorkArea(string workAreaPath)
    {
        try
        {
            HashSet<string> currentWorkAreaPaths = Directory.EnumerateFiles(workAreaPath)
                .Where(IsSupportedImagePath)
                .Select(NormalizeFilePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string cachedPath in _editorHistorySessionsByPath.Keys.ToArray())
            {
                if (!currentWorkAreaPaths.Contains(cachedPath))
                {
                    _editorHistorySessionsByPath.Remove(cachedPath);
                }
            }

            if (!Directory.Exists(EditorHistoryDirectory))
            {
                return;
            }

            foreach (string sessionDirectory in Directory.EnumerateDirectories(EditorHistoryDirectory))
            {
                string historyPath = Path.Combine(sessionDirectory, "history.json");
                if (!TryReadPersistedHistoryPath(historyPath, out string? normalizedPath) ||
                    normalizedPath is null ||
                    !currentWorkAreaPaths.Contains(normalizedPath))
                {
                    Directory.Delete(sessionDirectory, recursive: true);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
        }
    }

    private static bool TryReadPersistedHistoryPath(string historyPath, out string? normalizedPath)
    {
        normalizedPath = null;
        if (!File.Exists(historyPath))
        {
            return false;
        }

        PersistedEditorHistoryDocument? document = JsonSerializer.Deserialize<PersistedEditorHistoryDocument>(
            File.ReadAllText(historyPath, Encoding.UTF8));
        normalizedPath = document?.NormalizedPath;
        return !string.IsNullOrWhiteSpace(normalizedPath);
    }

    private void RestoreEditorHistoryState(EditorHistoryState snapshot)
    {
        if (SelectedPhoto is not PhotoItem photo ||
            !string.Equals(photo.Path, snapshot.PhotoPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _isRestoringEditorHistory = true;
        try
        {
            ClearDodgeBurnSession(false);
            ClearLiquifySession(false);
            ClearFaceShapeSymmetrySession();

            if (snapshot.AdjustedImage is null)
            {
                photo.ResetAdjustedImage();
            }
            else
            {
                photo.SetAdjustedImage(CloneBitmapSource(snapshot.AdjustedImage));
            }

            TypeTextItems.Clear();
            _selectedTypeTextItem = null;
            _draggingTypeTextItem = null;
            _creatingTypeTextItem = null;
            _isTypeTextCreating = false;
            _isTypeTextDragging = false;
            _typeTextEditSnapshot = string.Empty;
            Mouse.Capture(null);

            foreach (PreviewTextItemSnapshot textSnapshot in snapshot.TextItems)
            {
                PreviewTextItem item = new(textSnapshot.Id, textSnapshot.Text, textSnapshot.OriginalX, textSnapshot.OriginalY)
                {
                    IsPointText = textSnapshot.IsPointText,
                    FontFamilyName = textSnapshot.FontFamilyName,
                    FontSize = textSnapshot.FontSize,
                    FontColor = textSnapshot.FontColor,
                    OpacityPercent = textSnapshot.OpacityPercent,
                    IsBold = textSnapshot.IsBold,
                    IsItalic = textSnapshot.IsItalic,
                    TextAlignment = textSnapshot.TextAlignment
                };
                item.ResizeOriginal(textSnapshot.OriginalWidth, textSnapshot.OriginalHeight);
                TypeTextItems.Add(item);
            }

            UpdatePreviewLayout();
            UpdateTypeToolVisualState();
        }
        finally
        {
            _isRestoringEditorHistory = false;
        }
    }

    private void RefreshEditorHistoryPanel()
    {
        HistoryPanelItems.Clear();
        SelectedHistoryPanelItem = null;

        for (int i = 0; i < _editorUndoHistory.Count; i++)
        {
            EditorHistoryState snapshot = _editorUndoHistory[i];
            HistoryPanelItem item = new()
            {
                HistoryIndex = i,
                Title = snapshot.Title,
                Detail = snapshot.Detail,
                TimeLabel = snapshot.Timestamp.ToString("HH:mm:ss"),
                IsCurrent = i == _editorUndoHistory.Count - 1
            };
            HistoryPanelItems.Add(item);
            if (item.IsCurrent)
            {
                SelectedHistoryPanelItem = item;
            }
        }
    }

    private sealed class EditorHistorySession
    {
        public EditorHistorySession(
            List<EditorHistoryState> undoHistory,
            List<EditorHistoryState> redoHistory)
        {
            UndoHistory = undoHistory;
            RedoHistory = redoHistory;
        }

        public List<EditorHistoryState> UndoHistory { get; }

        public List<EditorHistoryState> RedoHistory { get; }
    }

    private sealed class PersistedEditorHistoryDocument
    {
        public string PhotoPath { get; set; } = string.Empty;

        public string NormalizedPath { get; set; } = string.Empty;

        public DateTime SavedAtUtc { get; set; }

        public List<PersistedEditorHistoryState> UndoHistory { get; set; } = [];

        public List<PersistedEditorHistoryState> RedoHistory { get; set; } = [];
    }

    private sealed class PersistedEditorHistoryState
    {
        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string? AdjustedImageFile { get; set; }

        public List<PersistedPreviewTextItemSnapshot> TextItems { get; set; } = [];
    }

    private sealed class PersistedPreviewTextItemSnapshot
    {
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public double OriginalX { get; set; }

        public double OriginalY { get; set; }

        public double OriginalWidth { get; set; }

        public double OriginalHeight { get; set; }

        public bool IsPointText { get; set; }

        public string FontFamilyName { get; set; } = "Malgun Gothic";

        public double FontSize { get; set; }

        public string FontColor { get; set; } = "#FFFFFF";

        public double OpacityPercent { get; set; }

        public bool IsBold { get; set; }

        public bool IsItalic { get; set; }

        public TextAlignment TextAlignment { get; set; }
    }

    private sealed class EditorHistoryState
    {
        public EditorHistoryState(
            string photoPath,
            BitmapSource? adjustedImage,
            IReadOnlyList<PreviewTextItemSnapshot> textItems,
            string title,
            string detail,
            DateTime timestamp)
        {
            PhotoPath = photoPath;
            AdjustedImage = adjustedImage;
            TextItems = textItems;
            Title = title;
            Detail = detail;
            Timestamp = timestamp;
        }

        public string PhotoPath { get; }

        public BitmapSource? AdjustedImage { get; }

        public IReadOnlyList<PreviewTextItemSnapshot> TextItems { get; }

        public string Title { get; }

        public string Detail { get; }

        public DateTime Timestamp { get; }
    }

    private sealed class PreviewTextItemSnapshot
    {
        public PreviewTextItemSnapshot(
            string id,
            string text,
            double originalX,
            double originalY,
            double originalWidth,
            double originalHeight,
            bool isPointText,
            string fontFamilyName,
            double fontSize,
            string fontColor,
            double opacityPercent,
            bool isBold,
            bool isItalic,
            TextAlignment textAlignment)
        {
            Id = id;
            Text = text;
            OriginalX = originalX;
            OriginalY = originalY;
            OriginalWidth = originalWidth;
            OriginalHeight = originalHeight;
            IsPointText = isPointText;
            FontFamilyName = fontFamilyName;
            FontSize = fontSize;
            FontColor = fontColor;
            OpacityPercent = opacityPercent;
            IsBold = isBold;
            IsItalic = isItalic;
            TextAlignment = textAlignment;
        }

        public string Id { get; }

        public string Text { get; }

        public double OriginalX { get; }

        public double OriginalY { get; }

        public double OriginalWidth { get; }

        public double OriginalHeight { get; }

        public bool IsPointText { get; }

        public string FontFamilyName { get; }

        public double FontSize { get; }

        public string FontColor { get; }

        public double OpacityPercent { get; }

        public bool IsBold { get; }

        public bool IsItalic { get; }

        public TextAlignment TextAlignment { get; }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void FaceShapeRetouchTab_FaceShapeControlAdjustmentPreviewChanged(object? sender, EventArgs e)
    {
        try
        {
            await ApplyFaceShapeControlDragPreviewAsync();
        }
        catch (Exception ex)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"Face Shape preview failed: {ex.Message}";
        }
    }

    private async void FaceShapeRetouchTab_SymmetrizeAdjustmentPreviewChanged(object? sender, EventArgs e)
    {
        try
        {
            await ApplyFaceShapeSymmetrizeDragPreviewAsync();
        }
        catch (Exception ex)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"Face Sym preview failed: {ex.Message}";
        }
    }

    private sealed record PreviewProxy1200BuildItem(PhotoItem Photo, int Generation);

    private sealed record PreviewProxy1200BuildSource(
        PhotoItem Photo,
        int Generation,
        ImageSource ImageIdentity,
        BitmapSource Source);
}

public sealed class PreviewTextItem : INotifyPropertyChanged
{
    private string _text;
    private double _originalX;
    private double _originalY;
    private double _originalWidth;
    private double _originalHeight;
    private double _displayLeft;
    private double _displayTop;
    private double _displayWidth;
    private double _displayHeight;
    private bool _isPointText = true;
    private string _fontFamilyName = "Malgun Gothic";
    private double _fontSize = 36;
    private string _fontColor = "#FFFFFF";
    private double _opacityPercent = 100;
    private bool _isBold;
    private bool _isItalic;
    private TextAlignment _textAlignment = TextAlignment.Left;
    private bool _isSelected;
    private bool _isEditing;
    private Visibility _selectionVisibility = Visibility.Collapsed;
    private Visibility _editorVisibility = Visibility.Collapsed;
    private Visibility _displayVisibility = Visibility.Visible;

    public PreviewTextItem(string id, string text, double originalX, double originalY)
    {
        Id = id;
        _text = text;
        _originalX = originalX;
        _originalY = originalY;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (string.Equals(_text, value, StringComparison.Ordinal))
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
        }
    }

    public double OriginalX
    {
        get => _originalX;
        private set
        {
            if (Math.Abs(_originalX - value) < 0.01)
            {
                return;
            }

            _originalX = value;
            OnPropertyChanged();
        }
    }

    public double OriginalY
    {
        get => _originalY;
        private set
        {
            if (Math.Abs(_originalY - value) < 0.01)
            {
                return;
            }

            _originalY = value;
            OnPropertyChanged();
        }
    }

    public double OriginalWidth
    {
        get => _originalWidth;
        private set
        {
            if (Math.Abs(_originalWidth - value) < 0.01)
            {
                return;
            }

            _originalWidth = value;
            OnPropertyChanged();
        }
    }

    public double OriginalHeight
    {
        get => _originalHeight;
        private set
        {
            if (Math.Abs(_originalHeight - value) < 0.01)
            {
                return;
            }

            _originalHeight = value;
            OnPropertyChanged();
        }
    }

    public double DisplayLeft
    {
        get => _displayLeft;
        private set
        {
            if (Math.Abs(_displayLeft - value) < 0.01)
            {
                return;
            }

            _displayLeft = value;
            OnPropertyChanged();
        }
    }

    public double DisplayTop
    {
        get => _displayTop;
        private set
        {
            if (Math.Abs(_displayTop - value) < 0.01)
            {
                return;
            }

            _displayTop = value;
            OnPropertyChanged();
        }
    }

    public bool IsPointText
    {
        get => _isPointText;
        set
        {
            if (_isPointText == value)
            {
                return;
            }

            _isPointText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EffectiveDisplayWidth));
            OnPropertyChanged(nameof(EffectiveDisplayHeight));
        }
    }

    public string FontFamilyName
    {
        get => _fontFamilyName;
        set
        {
            string next = string.IsNullOrWhiteSpace(value) ? "Malgun Gothic" : value.Trim();
            if (string.Equals(_fontFamilyName, next, StringComparison.Ordinal))
            {
                return;
            }

            _fontFamilyName = next;
            OnPropertyChanged();
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            double clamped = Math.Clamp(value, 6, 400);
            if (Math.Abs(_fontSize - clamped) < 0.01)
            {
                return;
            }

            _fontSize = clamped;
            OnPropertyChanged();
        }
    }

    public string FontColor
    {
        get => _fontColor;
        set
        {
            string next = string.IsNullOrWhiteSpace(value) ? "#FFFFFF" : value.Trim();
            if (string.Equals(_fontColor, next, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _fontColor = next;
            OnPropertyChanged();
        }
    }

    public double OpacityPercent
    {
        get => _opacityPercent;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_opacityPercent - clamped) < 0.01)
            {
                return;
            }

            _opacityPercent = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpacityValue));
        }
    }

    public bool IsBold
    {
        get => _isBold;
        set
        {
            if (_isBold == value)
            {
                return;
            }

            _isBold = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FontWeightValue));
        }
    }

    public bool IsItalic
    {
        get => _isItalic;
        set
        {
            if (_isItalic == value)
            {
                return;
            }

            _isItalic = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FontStyleValue));
        }
    }

    public TextAlignment TextAlignment
    {
        get => _textAlignment;
        set
        {
            if (_textAlignment == value)
            {
                return;
            }

            _textAlignment = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        private set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing == value)
            {
                return;
            }

            _isEditing = value;
            OnPropertyChanged();
        }
    }

    public Visibility SelectionVisibility
    {
        get => _selectionVisibility;
        private set
        {
            if (_selectionVisibility == value)
            {
                return;
            }

            _selectionVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility EditorVisibility
    {
        get => _editorVisibility;
        private set
        {
            if (_editorVisibility == value)
            {
                return;
            }

            _editorVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility DisplayVisibility
    {
        get => _displayVisibility;
        private set
        {
            if (_displayVisibility == value)
            {
                return;
            }

            _displayVisibility = value;
            OnPropertyChanged();
        }
    }

    public double EffectiveDisplayWidth => IsPointText ? double.NaN : Math.Max(40, _displayWidth);

    public double EffectiveDisplayHeight => IsPointText ? double.NaN : Math.Max(24, _displayHeight);

    public double OpacityValue => Math.Clamp(_opacityPercent / 100.0, 0, 1);

    public FontWeight FontWeightValue => _isBold ? FontWeights.Bold : FontWeights.Normal;

    public System.Windows.FontStyle FontStyleValue => _isItalic ? FontStyles.Italic : FontStyles.Normal;

    public void MoveOriginal(double originalX, double originalY)
    {
        OriginalX = originalX;
        OriginalY = originalY;
    }

    public void ResizeOriginal(double originalWidth, double originalHeight)
    {
        OriginalWidth = originalWidth;
        OriginalHeight = originalHeight;
    }

    public void UpdateDisplayBox(double displayLeft, double displayTop, double displayWidth, double displayHeight)
    {
        DisplayLeft = displayLeft;
        DisplayTop = displayTop;

        if (Math.Abs(_displayWidth - displayWidth) >= 0.01)
        {
            _displayWidth = displayWidth;
            OnPropertyChanged(nameof(EffectiveDisplayWidth));
        }

        if (Math.Abs(_displayHeight - displayHeight) >= 0.01)
        {
            _displayHeight = displayHeight;
            OnPropertyChanged(nameof(EffectiveDisplayHeight));
        }
    }

    public void SetVisualState(bool isSelected, bool isEditing, bool interactive)
    {
        IsSelected = isSelected;
        IsEditing = isEditing;
        SelectionVisibility = interactive && isSelected ? Visibility.Visible : Visibility.Collapsed;
        EditorVisibility = interactive && isEditing ? Visibility.Visible : Visibility.Collapsed;
        DisplayVisibility = interactive && isEditing ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal static class PhotoListSortExtensions
{
    public static IOrderedEnumerable<string> ApplySort(this IEnumerable<string> files, PhotoListSortMode sortMode)
    {
        return sortMode switch
        {
            PhotoListSortMode.NameDescending => files.OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase),
            PhotoListSortMode.DateNewestFirst => files.OrderByDescending(GetLastWriteTimeUtc).ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase),
            PhotoListSortMode.DateOldestFirst => files.OrderBy(GetLastWriteTimeUtc).ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase),
            _ => files.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static DateTime GetLastWriteTimeUtc(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (IOException)
        {
            return DateTime.MinValue;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }
}

public sealed class PreviewDebugRectOverlay
{
    public PreviewDebugRectOverlay(
        double left,
        double top,
        double width,
        double height,
        System.Windows.Media.Brush stroke,
        double strokeThickness,
        DoubleCollection? strokeDashArray = null)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        Stroke = stroke;
        StrokeThickness = strokeThickness;
        StrokeDashArray = strokeDashArray;
    }

    public double Left { get; }

    public double Top { get; }

    public double Width { get; }

    public double Height { get; }

    public System.Windows.Media.Brush Stroke { get; }

    public double StrokeThickness { get; }

    public DoubleCollection? StrokeDashArray { get; }
}

public sealed class PreviewDebugPolylineOverlay
{
    public PreviewDebugPolylineOverlay(PointCollection points, System.Windows.Media.Brush stroke, double strokeThickness)
    {
        Points = points;
        Stroke = stroke;
        StrokeThickness = strokeThickness;
    }

    public PointCollection Points { get; }

    public System.Windows.Media.Brush Stroke { get; }

    public double StrokeThickness { get; }
}

public sealed class PreviewDebugPointOverlay
{
    public PreviewDebugPointOverlay(
        double left,
        double top,
        double size,
        System.Windows.Media.Brush stroke,
        System.Windows.Media.Brush fill,
        double strokeThickness)
    {
        Left = left;
        Top = top;
        Size = size;
        Stroke = stroke;
        Fill = fill;
        StrokeThickness = strokeThickness;
    }

    public double Left { get; }

    public double Top { get; }

    public double Size { get; }

    public System.Windows.Media.Brush Stroke { get; }

    public System.Windows.Media.Brush Fill { get; }

    public double StrokeThickness { get; }
}
