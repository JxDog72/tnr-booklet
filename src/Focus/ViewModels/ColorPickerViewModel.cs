using System.Collections.ObjectModel;
using Focus.Themes;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Media = System.Windows.Media;

namespace Focus.ViewModels;

public sealed class ColorPickerViewModel : ViewModelBase
{
    private int _r = 0xA7;
    private int _g = 0x8B;
    private int _b = 0xFA;
    private string? _selectedPreset;

    public ColorPickerViewModel(string? initialHex = null)
    {
        Presets = new ObservableCollection<ColorPreset>(DefaultPresets);
        if (!string.IsNullOrWhiteSpace(initialHex) && ThemeApplicator.TryParseColor(initialHex, out var c))
        {
            _r = c.R;
            _g = c.G;
            _b = c.B;
        }

        SelectPresetCommand = new RelayCommand(p =>
        {
            if (p is ColorPreset preset)
                ApplyHex(preset.Hex);
        });
        OpenWheelCommand = new RelayCommand(OpenSystemColorWheel);
    }

    public ObservableCollection<ColorPreset> Presets { get; }
    public RelayCommand SelectPresetCommand { get; }
    public RelayCommand OpenWheelCommand { get; }

    public int R
    {
        get => _r;
        set
        {
            if (SetProperty(ref _r, Clamp(value)))
            {
                RaisePropertyChanged(nameof(Hex));
                RaisePropertyChanged(nameof(PreviewBrush));
                _selectedPreset = null;
                RaisePropertyChanged(nameof(SelectedPresetHex));
            }
        }
    }

    public int G
    {
        get => _g;
        set
        {
            if (SetProperty(ref _g, Clamp(value)))
            {
                RaisePropertyChanged(nameof(Hex));
                RaisePropertyChanged(nameof(PreviewBrush));
                _selectedPreset = null;
                RaisePropertyChanged(nameof(SelectedPresetHex));
            }
        }
    }

    public int B
    {
        get => _b;
        set
        {
            if (SetProperty(ref _b, Clamp(value)))
            {
                RaisePropertyChanged(nameof(Hex));
                RaisePropertyChanged(nameof(PreviewBrush));
                _selectedPreset = null;
                RaisePropertyChanged(nameof(SelectedPresetHex));
            }
        }
    }

    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    public string? SelectedPresetHex => _selectedPreset;

    public Media.Brush PreviewBrush => new Media.SolidColorBrush(Media.Color.FromRgb((byte)R, (byte)G, (byte)B));

    public void ApplyHex(string hex)
    {
        if (!ThemeApplicator.TryParseColor(hex, out var c))
            return;
        _r = c.R;
        _g = c.G;
        _b = c.B;
        _selectedPreset = Hex;
        RaisePropertyChanged(nameof(R));
        RaisePropertyChanged(nameof(G));
        RaisePropertyChanged(nameof(B));
        RaisePropertyChanged(nameof(Hex));
        RaisePropertyChanged(nameof(PreviewBrush));
        RaisePropertyChanged(nameof(SelectedPresetHex));
    }

    private void OpenSystemColorWheel()
    {
        using var dlg = new Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = Drawing.Color.FromArgb(R, G, B)
        };
        if (dlg.ShowDialog() == Forms.DialogResult.OK)
        {
            R = dlg.Color.R;
            G = dlg.Color.G;
            B = dlg.Color.B;
        }
    }

    private static int Clamp(int v) => Math.Clamp(v, 0, 255);

    private static IEnumerable<ColorPreset> DefaultPresets =>
    [
        new("Purple", "#A78BFA"),
        new("Green", "#34D399"),
        new("Red", "#F87171"),
        new("Amber", "#FBBF24"),
        new("Sky", "#38BDF8"),
        new("Pink", "#F472B6"),
        new("Indigo", "#818CF8"),
        new("Teal", "#2DD4BF"),
        new("Orange", "#FB923C"),
        new("Lime", "#A3E635"),
        new("Rose", "#FB7185"),
        new("Slate", "#94A3B8"),
        new("White", "#E5E5E5"),
        new("Violet", "#C084FC"),
        new("Cyan", "#22D3EE"),
        new("Emerald", "#10B981"),
    ];
}

public sealed class ColorPreset
{
    public ColorPreset(string name, string hex)
    {
        Name = name;
        Hex = hex;
    }

    public string Name { get; }
    public string Hex { get; }

    public Media.Brush Brush
    {
        get
        {
            if (ThemeApplicator.TryParseColor(Hex, out var c))
                return new Media.SolidColorBrush(c);
            return Media.Brushes.Gray;
        }
    }
}
