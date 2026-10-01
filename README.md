# TNR-Booklet

**TNR** stands for **Todo / Notes / Reminders**.

Local Windows app for those three things in one booklet. Nothing is uploaded. No account.

![TNR-Booklet main window](https://raw.githubusercontent.com/JxDog72/tnr-booklet/screenshots/mainView.png)

Reminders still fire when the window is closed (Windows Task Scheduler). Optional Telegram and Discord messages go out only; tokens stay on this PC.

**Windows 10 or 11 only.** License: [MIT](LICENSE).

---

## Run

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or the .NET 9 desktop runtime if you already have a built exe).
2. Double-click `Run-TNR-Booklet.bat`.

That builds a Release copy if the SDK is present, then launches TNR-Booklet. After a successful build it also drops a **tnr-booklet.exe** shortcut on your Desktop and in the Start Menu so Windows Search finds it when you type `tnr-booklet` (same idea as the sysmonbar desktop shortcut).

From a terminal in this folder:

```powershell
dotnet build TnrBooklet.sln -c Release
dotnet run --project src/TnrBooklet/TnrBooklet.csproj -c Release
```

Publish a self-contained exe:

```powershell
dotnet publish src/TnrBooklet/TnrBooklet.csproj -c Release -r win-x64 --self-contained true -o publish
```

Then run `publish\TNR-Booklet.exe`.

---

## What it does

- Folders and tags (Work / Personal to start, plus your own)
- Todos and notes (notes skip due dates and reminders)
- List badges: **TODO** (amber), **NOTE** (teal), **REMINDER** (rose)
- Views: All (notes on top, todos below a purple divider), Today, Upcoming, Overdue, Completed
- One-shot and repeating reminders (daily, weekly, monthly, every N days, hourly)
- Toast, sound, tray, close-to-tray
- Theme editor
- JSON export / import (bot tokens and webhook URLs are not written into exports)
- Optional Telegram + Discord

---

## Reminders

Time is **24-hour** (`09:05` or `21:30`, not 9:05 PM). Date and time sit side by side on the todo editor.

While TNR-Booklet is open it watches the clock. If you close it, Windows Task Scheduler still starts it at that time. Look under **Task Scheduler Library → TNR-Booklet**. Turn this off in **Settings** if you only want alerts while the app is running.

When a reminder popup appears, **Dismiss** clears or advances it as before. **Remind me** postpones that same reminder: click hour and minute chips (or the + / − buttons). Default is 5 minutes. Recurring items keep their original schedule; only this fire is delayed. Delay is 1 minute through 7 days. The delay is also written to Task Scheduler when that option is on.

**Hourly:** set Recurrence to `Hourly` and Interval N to hours between alerts (`1` = every hour). The first fire is the reminder date/time you pick. Change Interval N or Recurrence later to slow it down or stop the loop (or delete the item).

---

## Data (this PC only)

Default folder:

| Path | Purpose |
|------|---------|
| `%LocalAppData%\TnrBooklet\tnr-booklet.db` | Tasks, folders, tags |
| `%LocalAppData%\TnrBooklet\settings.json` | Settings and local messaging secrets |
| `%LocalAppData%\TnrBooklet\themes.json` | Themes |

**Settings → Data folder** shows the live path, lets you open it, and lets you pick another folder (USB drive, second disk, and so on). The chosen path is stored in `%LocalAppData%\TnrBooklet\data-location.txt`; the database and settings then live in that folder.

If you used an older install, existing data under `%LocalAppData%\Focus\` is moved into the default folder on first launch.

---

## Messaging (optional)

Secrets stay in `%LocalAppData%\TnrBooklet\settings.json`.

**Telegram:** [@BotFather](https://t.me/BotFather) → `/newbot` → copy the token. Message your bot `/start`, then open `https://api.telegram.org/bot<TOKEN>/getUpdates` and copy `"chat":{"id": ...}`. In **Settings**, paste token + chat id → **Save** → **Test send**.

**Discord:** Channel → **Edit channel → Integrations → Webhooks**. Paste the URL in **Settings** → **Save** → **Test send**.

**Send list** on the toolbar posts today’s open tasks on every enabled channel.
