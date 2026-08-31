using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Focus.ViewModels;

namespace Focus.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        TelegramTokenBox.Password = _vm.TelegramBotToken;
        DiscordWebhookBox.Password = _vm.DiscordWebhookUrl;
    }

    private void TelegramToken_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
            _vm.TelegramBotToken = box.Password;
    }

    private void DiscordWebhook_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box)
            _vm.DiscordWebhookUrl = box.Password;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _vm.Save();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "TNR-Booklet", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.TestMessagingAsync();
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = "Test failed: " + ex.Message;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _vm.OpenDataFolder();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Could not open folder:\n" + ex.Message, "TNR-Booklet",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChangeDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose where TNR-Booklet saves todos, notes, and settings",
            Multiselect = false
        };
        try
        {
            dlg.InitialDirectory = _vm.DataFolderPath;
        }
        catch
        {
            // Ignore if the current path is gone.
        }

        if (dlg.ShowDialog(this) != true)
            return;

        var folder = dlg.FolderName;
        if (string.IsNullOrWhiteSpace(folder))
            return;

        if (_vm.RelocateTo(folder))
            RestartApp();
    }

    private void DefaultDataFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.ResetToDefault())
            RestartApp();
    }

    private static void RestartApp()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "Data folder updated, but the app could not restart automatically.\nClose and open TNR-Booklet.\n\n" + ex.Message,
                "TNR-Booklet",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        System.Windows.Application.Current?.Shutdown();
    }
}
