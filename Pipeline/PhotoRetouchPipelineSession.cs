namespace KRetouchStudio.Pipeline;

public readonly record struct RetouchRenderTicket(
    string PhotoPath,
    RetouchRenderQuality Quality,
    RetouchStageId FirstDirtyStage,
    long StateVersion,
    long RenderVersion,
    CancellationToken CancellationToken);

public sealed class PhotoRetouchPipelineSession : IDisposable
{
    private readonly Dictionary<RetouchStageId, long> _stageStateRevisions = [];
    private CancellationTokenSource? _previewRenderCancellation;
    private CancellationTokenSource? _fullResolutionRenderCancellation;
    private long _previewRenderVersion;
    private long _fullResolutionRenderVersion;
    private bool _isDisposed;

    public PhotoRetouchPipelineSession(string photoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoPath);
        PhotoPath = photoPath;
        foreach (RetouchStageId stage in RetouchStageCatalog.RasterStages)
        {
            _stageStateRevisions[stage] = 0;
        }
    }

    public string PhotoPath { get; }

    public long StateVersion { get; private set; }

    public long BaseRevision { get; private set; }

    public long GeometryRevision { get; private set; }

    public long AnalysisRevision { get; private set; }

    public PhotoRetouchStageCache Cache { get; } = new();

    public long GetStageStateRevision(RetouchStageId stage)
    {
        return _stageStateRevisions.TryGetValue(stage, out long revision)
            ? revision
            : 0;
    }

    public RetouchDirtyRange MarkStageChanged(RetouchStageId stage)
    {
        ThrowIfDisposed();
        if (stage == RetouchStageId.TextOverlay)
        {
            StateVersion = checked(StateVersion + 1);
            return RetouchDirtyRange.FromChangedStage(stage);
        }

        if (stage < RetouchStageId.WorkingBase || stage > RetouchStageId.Background)
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "The stage is not a pipeline raster stage.");
        }

        StateVersion = checked(StateVersion + 1);
        _stageStateRevisions[stage] = checked(GetStageStateRevision(stage) + 1);
        if (stage == RetouchStageId.WorkingBase)
        {
            BaseRevision = checked(BaseRevision + 1);
        }

        Cache.InvalidateFrom(stage);
        CancelOutstandingRenders();
        return RetouchDirtyRange.FromChangedStage(stage);
    }

    public long AdvanceGeometryRevision()
    {
        ThrowIfDisposed();
        GeometryRevision = checked(GeometryRevision + 1);
        AnalysisRevision = 0;
        return GeometryRevision;
    }

    public long PublishAnalysisRevision()
    {
        ThrowIfDisposed();
        AnalysisRevision = checked(AnalysisRevision + 1);
        return AnalysisRevision;
    }

    public RetouchRenderTicket BeginRender(
        RetouchRenderQuality quality,
        RetouchStageId firstDirtyStage)
    {
        ThrowIfDisposed();
        CancellationTokenSource cancellation = new();
        long renderVersion;
        if (quality == RetouchRenderQuality.Preview)
        {
            _previewRenderCancellation?.Cancel();
            _previewRenderCancellation?.Dispose();
            _previewRenderCancellation = cancellation;
            renderVersion = checked(++_previewRenderVersion);
        }
        else
        {
            _fullResolutionRenderCancellation?.Cancel();
            _fullResolutionRenderCancellation?.Dispose();
            _fullResolutionRenderCancellation = cancellation;
            renderVersion = checked(++_fullResolutionRenderVersion);
        }

        return new RetouchRenderTicket(
            PhotoPath,
            quality,
            firstDirtyStage,
            StateVersion,
            renderVersion,
            cancellation.Token);
    }

    public bool IsCurrent(RetouchRenderTicket ticket)
    {
        if (_isDisposed ||
            ticket.CancellationToken.IsCancellationRequested ||
            !string.Equals(ticket.PhotoPath, PhotoPath, StringComparison.OrdinalIgnoreCase) ||
            ticket.StateVersion != StateVersion)
        {
            return false;
        }

        return ticket.Quality == RetouchRenderQuality.Preview
            ? ticket.RenderVersion == _previewRenderVersion
            : ticket.RenderVersion == _fullResolutionRenderVersion;
    }

    public void CancelOutstandingRenders()
    {
        _previewRenderCancellation?.Cancel();
        _fullResolutionRenderCancellation?.Cancel();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelOutstandingRenders();
        _previewRenderCancellation?.Dispose();
        _fullResolutionRenderCancellation?.Dispose();
        _previewRenderCancellation = null;
        _fullResolutionRenderCancellation = null;
        Cache.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
