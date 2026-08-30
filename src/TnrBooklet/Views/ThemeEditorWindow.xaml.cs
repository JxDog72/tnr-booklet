using System.Windows;
using Focus.ViewModels;

namespace Focus.Views;

public partial class ThemeEditorWindow : Window
{
    private readonly ThemeEditorViewModel _vm;

    public ThemeEditorWindow(ThemeEditorViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: ColorFieldVm field })
            return;

        var pickerVm = new ColorPickerViewModel(field.Hex);
        var dlg = new ColorPickerWindow(pickerVm) { Owner = this };
        if (dlg.ShowDialog() == true)
            field.Hex = dlg.SelectedHex;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _vm.Save();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save theme: {ex.Message}", "TNR-Booklet", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Apply_Click(object sender, RoutedEventArgs e) => _vm.ApplyLive();

    private void Reset_Click(object sender, RoutedEventArgs e) => _vm.ResetToDefaults();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
