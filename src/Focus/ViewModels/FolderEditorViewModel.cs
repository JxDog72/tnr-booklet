using Focus.Core.Models;
using Focus.Themes;
using Media = System.Windows.Media;

namespace Focus.ViewModels;

public sealed class FolderEditorViewModel : ViewModelBase
{
    private string _name;
    private string _colorHex;

    public FolderEditorViewModel(Folder? existing)
    {
        IsNew = existing is null;
        SourceId = existing?.Id;
        _name = existing?.Name ?? "";
        _colorHex = existing?.Color ?? "#A78BFA";
        SortOrder = existing?.SortOrder ?? 0;
        CreatedAtUtc = existing?.CreatedAtUtc ?? DateTime.UtcNow;
    }

    public bool IsNew { get; }
    public string? SourceId { get; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; }
    public string WindowTitle => IsNew ? "New folder" : "Edit folder";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (SetProperty(ref _colorHex, value))
                RaisePropertyChanged(nameof(ColorBrush));
        }
    }

    public Media.Brush ColorBrush
    {
        get
        {
            if (ThemeApplicator.TryParseColor(ColorHex, out var c))
                return new Media.SolidColorBrush(c);
            return new Media.SolidColorBrush(Media.Color.FromRgb(0xA7, 0x8B, 0xFA));
        }
    }

    public Folder ToFolder() => new()
    {
        Id = SourceId ?? Guid.NewGuid().ToString("N"),
        Name = Name.Trim(),
        Color = string.IsNullOrWhiteSpace(ColorHex) ? "#A78BFA" : ColorHex.Trim(),
        SortOrder = SortOrder,
        CreatedAtUtc = CreatedAtUtc
    };
}
