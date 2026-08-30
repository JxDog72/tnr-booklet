using System.Windows;
using Focus.Core.Services;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Focus.Services;

public sealed class TrayService : IDisposable
{
    private readonly AppServices _services;
    private readonly Action _showMain;
    private readonly Action _exitApp;
    private Forms.NotifyIcon? _icon;
    private bool _disposed;

    public TrayService(AppServices services, Action showMain, Action exitApp)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _showMain = showMain ?? throw new ArgumentNullException(nameof(showMain));
        _exitApp = exitApp ?? throw new ArgumentNullException(nameof(exitApp));
    }

    public void Initialize()
    {
        if (_icon is not null)
            return;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open TNR-Booklet", null, (_, _) => _showMain());
        menu.Items.Add(new Forms.ToolStripSeparator());

        var pauseItem = new Forms.ToolStripMenuItem("Pause notifications")
        {
            CheckOnClick = true,
            Checked = _services.Settings.Current.NotificationsPaused
        };
        pauseItem.CheckedChanged += (_, _) =>
        {
            _services.Settings.Current.NotificationsPaused = pauseItem.Checked;
            _services.Settings.Save();
        };
        menu.Items.Add(pauseItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _exitApp());

        _icon = new Forms.NotifyIcon
        {
            Text = "TNR-Booklet",
            Visible = _services.Settings.Current.TrayEnabled,
            ContextMenuStrip = menu,
            Icon = CreateIcon()
        };
        _icon.DoubleClick += (_, _) => _showMain();
    }

    public void SetVisible(bool visible)
    {
        if (_icon is not null)
            _icon.Visible = visible;
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_icon is not null)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _icon = null;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static Drawing.Icon CreateIcon()
    {
        // Simple purple square icon so we don't need an .ico asset.
        var bmp = new Drawing.Bitmap(16, 16);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(Drawing.Color.FromArgb(0xA7, 0x8B, 0xFA));
            using var pen = new Drawing.Pen(Drawing.Color.FromArgb(0x0A, 0x0A, 0x0C), 1);
            g.DrawRectangle(pen, 0, 0, 15, 15);
        }
        var hIcon = bmp.GetHicon();
        var icon = Drawing.Icon.FromHandle(hIcon);
        // Clone so we own a managed copy independent of the bitmap lifetime.
        var clone = (Drawing.Icon)icon.Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return clone;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
