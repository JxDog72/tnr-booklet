# TNR-Booklet

Local Windows app for todos, notes, and reminders. Nothing is uploaded. No account.

Reminders still fire when the window is closed (Windows Task Scheduler). Optional Telegram and Discord messages are outbound only; tokens stay on this PC.

**Windows 10 or 11 only.** License: [MIT](LICENSE).

---

## Run

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or the .NET 9 desktop runtime if you already have a built exe).
2. Double-click `Run-TNR-Booklet.bat`.

That builds a Release build if the SDK is present, then launches TNR-Booklet.

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

- Folders and tags (Work / Personal seeds plus your own)
- Todos and notes (notes skip due dates and reminders)
- Views: All, Today, Upcoming, Overdue, Completed
- One-shot and recurring reminders
- Toast, sound, tray, close-to-tray
- Theme editor
- JSON export / import (bot tokens and webhook URLs are not written into exports)
- Optional Telegram + Discord

---

## Data (this PC only)

| Path | Purpose |
|------|---------|
| `%LocalAppData%\TnrBooklet\tnr-booklet.db` | Tasks, folders, tags |
| `%LocalAppData%\TnrBooklet\settings.json` | Settings and local messaging secrets |
| `%LocalAppData%\TnrBooklet\themes.json` | Themes |

If you previously used an older install, existing data under `%LocalAppData%\Focus\` is moved into this folder on first launch.

Scheduled reminders live in **Task Scheduler Library → TNR-Booklet**.

---

## Messaging (optional)

Secrets stay in `%LocalAppData%\TnrBooklet\settings.json`.

**Telegram:** [@BotFather](https://t.me/BotFather) → `/newbot` → copy the token. Message your bot `/start`, then open `https://api.telegram.org/bot<TOKEN>/getUpdates` and copy `"chat":{"id": ...}`. In **Settings**, paste token + chat id → **Save** → **Test send**.

**Discord:** Channel → **Edit channel → Integrations → Webhooks**. Paste the URL in **Settings** → **Save** → **Test send**.

**Send list** on the toolbar posts today’s open tasks on every enabled channel.
