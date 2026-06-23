namespace KRetouchStudio;

public sealed class HistoryPanelItem
{
    public int HistoryIndex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string TimeLabel { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }
}
