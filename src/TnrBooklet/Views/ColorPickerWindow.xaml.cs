using System.Windows;
using Focus.ViewModels;

namespace Focus.Views;

public partial class ColorPickerWindow : Window
{
    private readonly ColorPickerViewModel _vm;

    public ColorPickerWindow(ColorPickerViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
    }

    public string SelectedHex => _vm.Hex;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
