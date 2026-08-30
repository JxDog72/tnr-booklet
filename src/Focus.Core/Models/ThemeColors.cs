namespace Focus.Core.Models;

public sealed class ThemeColors
{
    public string BgApp { get; set; } = "#0A0A0C";
    public string BgSidebar { get; set; } = "#0E0E12";
    public string BgToolbar { get; set; } = "#121216";
    public string BgSurface { get; set; } = "#121216";
    public string BgSurfaceAlt { get; set; } = "#1A1A22";
    public string BorderDefault { get; set; } = "#1F1F24";
    public string BorderFocus { get; set; } = "#A78BFA";
    public string TextPrimary { get; set; } = "#E5E5E5";
    public string TextSecondary { get; set; } = "#9CA3AF";
    public string TextMuted { get; set; } = "#6B7280";
    public string Accent { get; set; } = "#A78BFA";
    public string Success { get; set; } = "#34D399";
    public string Warning { get; set; } = "#FBBF24";
    public string Danger { get; set; } = "#F87171";
    public string Overdue { get; set; } = "#F87171";
    public string SelectionBg { get; set; } = "#1A1528";
    public string SelectionFg { get; set; } = "#A78BFA";
    public string ComboTypeBg { get; set; } = "#2A1F4A";
    public string ComboTypeFg { get; set; } = "#E5E5E5";
    public string ComboFolderBg { get; set; } = "#0F2A22";
    public string ComboFolderFg { get; set; } = "#D1FAE5";
    public string ComboPriorityBg { get; set; } = "#2A2208";
    public string ComboPriorityFg { get; set; } = "#FDE68A";
}

public sealed class ThemeDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Custom";
    public ThemeColors Colors { get; set; } = new();
}
