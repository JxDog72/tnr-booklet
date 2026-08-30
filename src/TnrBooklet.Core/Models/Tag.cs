namespace Focus.Core.Models;

public sealed class Tag
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string? Color { get; set; }
}
