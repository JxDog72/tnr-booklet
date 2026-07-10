using Focus.Core.Themes;
using FluentAssertions;
using Xunit;

namespace Focus.Tests;

public class ThemeCatalogTests
{
    [Fact]
    public void CreateFocusDark_uses_focus_dark_id_and_accent()
    {
        var theme = ThemeCatalog.CreateFocusDark();
        theme.Id.Should().Be("focus-dark");
        theme.Id.Should().Be(ThemeCatalog.FocusDarkId);
        theme.Name.Should().Be("Focus Dark");
        theme.Colors.Accent.Should().Be("#A78BFA");
    }

    [Fact]
    public void EnsureDefaults_null_returns_focus_dark()
    {
        var theme = ThemeCatalog.EnsureDefaults(null);
        theme.Id.Should().Be(ThemeCatalog.FocusDarkId);
        theme.Colors.Accent.Should().Be("#A78BFA");
    }
}
