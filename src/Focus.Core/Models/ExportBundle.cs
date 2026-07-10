namespace Focus.Core.Models;

public sealed class ExportBundle
{
    public int Version { get; set; } = 1;
    public List<Folder> Folders { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
    public List<ThemeDefinition> Themes { get; set; } = new();
}
