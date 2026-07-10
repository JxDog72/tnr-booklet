using Focus.Core.Data;

namespace Focus.ViewModels;

public enum SidebarItemKind
{
    SmartView,
    Folder,
    Tag
}

public sealed class SidebarItemVm : ViewModelBase
{
    private bool _isSelected;

    public SidebarItemKind Kind { get; init; }
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Color { get; init; }
    public SmartView? View { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
