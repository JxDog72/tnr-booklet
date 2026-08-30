namespace Focus.Core.Models;

public sealed class Folder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#A78BFA";
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
