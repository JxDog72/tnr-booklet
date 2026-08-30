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
        theme.Colors.ComboTypeBg.Should().Be("#2A1F4A");
        theme.Colors.ComboFolderFg.Should().Be("#D1FAE5");
        theme.Colors.ComboPriorityBg.Should().Be("#2A2208");
    }

    [Fact]
    public void EnsureDefaults_fills_empty_combo_colors()
    {
        var theme = new Focus.Core.Models.ThemeDefinition
        {
            Colors = new Focus.Core.Models.ThemeColors { ComboTypeBg = "" }
        };
        var filled = ThemeCatalog.EnsureDefaults(theme);
        filled.Colors.ComboTypeBg.Should().Be("#2A1F4A");
    }
}
