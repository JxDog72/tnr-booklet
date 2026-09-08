using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Focus.Core.Recurrence;

namespace Focus.Views;

public partial class ReminderAlertWindow : Window
{
    private static readonly Regex Digits = new("^[0-9]+$", RegexOptions.Compiled);

    public int? SnoozeMinutes { get; private set; }

    public ReminderAlertWindow(string title, string body)
    {
        InitializeComponent();
        TitleText.Text = string.IsNullOrWhiteSpace(title) ? "TNR-Booklet reminder" : title;
        BodyText.Text = string.IsNullOrWhiteSpace(body) ? "Reminder" : body;
        System.Windows.DataObject.AddPastingHandler(MinutesBox, MinutesBox_Pasting);
        Loaded += (_, _) =>
        {
            MinutesBox.Focus();
            MinutesBox.SelectAll();
        };
    }

    private void MinutesBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Digits.IsMatch(e.Text);
    }

    private static void MinutesBox_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(typeof(string)) as string ?? "";
        if (!Digits.IsMatch(text))
            e.CancelCommand();
    }

    private void MinutesBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TrySnooze();
            e.Handled = true;
        }
    }

    private void Snooze_Click(object sender, RoutedEventArgs e) => TrySnooze();

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        SnoozeMinutes = null;
        DialogResult = false;
        Close();
    }

    private void TrySnooze()
    {
        if (!ReminderAdvance.TryParseSnoozeMinutes(MinutesBox.Text, out var minutes))
        {
            SnoozeError.Text =
                $"Enter a whole number of minutes from {ReminderAdvance.MinSnoozeMinutes} to {ReminderAdvance.MaxSnoozeMinutes}.";
            SnoozeError.Visibility = Visibility.Visible;
            MinutesBox.Focus();
            MinutesBox.SelectAll();
            return;
        }

        SnoozeMinutes = minutes;
        DialogResult = true;
        Close();
    }
}
