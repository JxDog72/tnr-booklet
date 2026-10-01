using System.Windows;
using Focus.Core.Recurrence;
using WpfControls = System.Windows.Controls;

namespace Focus.Views;

public partial class ReminderAlertWindow : Window
{
    private static readonly int[] HourChoices = [0, 1, 2, 3, 4, 6, 8, 12, 24];
    private static readonly int[] MinuteChoices = [0, 5, 10, 15, 20, 30, 45];

    private int _hours;
    private int _minutes = 5;

    public int? SnoozeMinutes { get; private set; }

    public ReminderAlertWindow(string title, string body)
    {
        InitializeComponent();
        TitleText.Text = string.IsNullOrWhiteSpace(title) ? "TNR-Booklet reminder" : title;
        BodyText.Text = string.IsNullOrWhiteSpace(body) ? "Reminder" : body;
        BuildChips();
        RefreshSnoozeUi();
        Loaded += (_, _) => HourPlus.Focus();
    }

    private void BuildChips()
    {
        HourChips.Children.Clear();
        foreach (var hours in HourChoices)
        {
            var button = MakeChip($"{hours}h", hours, HourChip_Click);
            button.ToolTip = hours == 1 ? "1 hour" : $"{hours} hours";
            HourChips.Children.Add(button);
        }

        MinuteChips.Children.Clear();
        foreach (var minutes in MinuteChoices)
        {
            var button = MakeChip($"{minutes}m", minutes, MinuteChip_Click);
            button.ToolTip = minutes == 1 ? "1 minute" : $"{minutes} minutes";
            MinuteChips.Children.Add(button);
        }
    }

    private WpfControls.Button MakeChip(string label, int value, RoutedEventHandler click)
    {
        var button = new WpfControls.Button
        {
            Content = label,
            Tag = value,
            Style = (Style)FindResource("ChipButton"),
        };
        button.Click += click;
        return button;
    }

    private void HourChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfControls.Button { Tag: int hours })
            SetHours(hours);
    }

    private void MinuteChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfControls.Button { Tag: int minutes })
            SetMinutes(minutes);
    }

    private void HourMinus_Click(object sender, RoutedEventArgs e) => SetHours(_hours - 1);

    private void HourPlus_Click(object sender, RoutedEventArgs e) => SetHours(_hours + 1);

    private void MinuteMinus_Click(object sender, RoutedEventArgs e) => SetMinutes(_minutes - 1);

    private void MinutePlus_Click(object sender, RoutedEventArgs e) => SetMinutes(_minutes + 1);

    private void SetHours(int hours)
    {
        _hours = Math.Clamp(hours, 0, ReminderAdvance.MaxSnoozeHours);
        if (_hours == ReminderAdvance.MaxSnoozeHours)
            _minutes = 0;
        RefreshSnoozeUi();
    }

    private void SetMinutes(int minutes)
    {
        _minutes = Math.Clamp(minutes, 0, 59);
        if (_hours == ReminderAdvance.MaxSnoozeHours)
            _minutes = 0;
        RefreshSnoozeUi();
    }

    private void RefreshSnoozeUi()
    {
        HourValue.Text = _hours.ToString();
        MinuteValue.Text = _minutes.ToString();
        SnoozeSummary.Text = ReminderAdvance.FormatSnoozeDuration(_hours, _minutes);
        MarkChips(HourChips, _hours);
        MarkChips(MinuteChips, _minutes);
        HourMinus.IsEnabled = _hours > 0;
        HourPlus.IsEnabled = _hours < ReminderAdvance.MaxSnoozeHours;
        MinuteMinus.IsEnabled = _minutes > 0 && _hours < ReminderAdvance.MaxSnoozeHours;
        MinutePlus.IsEnabled = _minutes < 59 && _hours < ReminderAdvance.MaxSnoozeHours;
        SnoozeError.Visibility = Visibility.Collapsed;
    }

    private void MarkChips(WpfControls.Panel panel, int selected)
    {
        var selectedStyle = (Style)FindResource("ChipButtonSelected");
        var idleStyle = (Style)FindResource("ChipButton");
        foreach (var child in panel.Children)
        {
            if (child is not WpfControls.Button button || button.Tag is not int value)
                continue;
            button.Style = value == selected ? selectedStyle : idleStyle;
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
        if (!ReminderAdvance.TryGetSnoozeMinutes(_hours, _minutes, out var minutes))
        {
            SnoozeError.Text =
                $"Pick a delay from 1 minute up to {ReminderAdvance.MaxSnoozeHours} hours.";
            SnoozeError.Visibility = Visibility.Visible;
            return;
        }

        SnoozeMinutes = minutes;
        DialogResult = true;
        Close();
    }
}
