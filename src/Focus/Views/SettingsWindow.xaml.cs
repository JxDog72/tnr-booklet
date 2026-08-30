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
}
