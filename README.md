# TNR-Booklet

Todos, notes, and reminders in one local Windows booklet.

Linux and macOS already ship decent Reminders / todo apps. Windows did not for this workflow: one local app for tasks, notes, and reminders that still fire when the window is closed. TNR-Booklet fills that gap. It is **Windows-only** (WPF). A Linux/macOS port is not planned — too much work for a problem those platforms already solved.

**No cloud. No accounts.** Data lives under `%LocalAppData%\Focus\`. Optional Telegram / Discord messages are outbound only; bot tokens and webhook URLs stay on this PC and are **not** written into JSON exports.

**License:** [MIT](LICENSE) — Copyright (c) 2026 JxDog72. Provided as-is, no warranty.

---

## Why

Wanted a Windows todo + notes + reminders app that:

- stays local (no cloud, no account)
- still reminds you when the app is closed (Windows Task Scheduler)
- keeps work and personal items in folders/tags without shipping them off-box

Built-in Windows reminders were not a good fit for that mix. TNR-Booklet is.

## What it does

- Folders (Work / Personal seeds + custom) with per-folder colors
- Tags and notes (notes skip due/reminder/scheduler)
- Smart views: All, Today, Upcoming, Overdue, Completed
- One-shot and recurring reminders (daily / weekly weekdays / monthly / every N days)
- Toast, sound, popup-focus, tray, close-to-tray, pause notifications
- Theme editor with live apply (Focus Dark defaults)
- Export / import a full JSON bundle (tokens omitted on export)
- Optional Telegram + Discord messaging (today’s list + reminder lines)
- Single-instance UI; `--remind` can run headless

## Requirements

- **Windows 10 or 11**
- [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) SDK to build, or the .NET 9 desktop runtime to run a framework-dependent publish

## Build / run / test / publish

From the repository root:

```powershell
dotnet build Focus.sln
```

```powershell
dotnet run --project src/Focus/Focus.csproj
```

```powershell
dotnet test Focus.sln
```

Framework-dependent (smaller; needs the runtime installed):

```powershell
dotnet publish src/Focus/Focus.csproj -c Release -r win-x64 --self-contained false -o publish
```

Self-contained:

```powershell
dotnet publish src/Focus/Focus.csproj -c Release -r win-x64 --self-contained true -o publish
```

Run `publish\Focus.exe`. Double-click `Run-TNR-Booklet.bat` to build (if the SDK is present) and launch the Release exe. The exe name stays `Focus.exe` so existing Task Scheduler reminders keep working.

## Data folder

| Path | Purpose |
|------|---------|
| `%LocalAppData%\Focus\focus.db` | SQLite tasks, folders, tags |
| `%LocalAppData%\Focus\settings.json` | App settings (including local messaging secrets) |
| `%LocalAppData%\Focus\themes.json` | Named themes |
| `%LocalAppData%\Focus\last-fire-*.txt` | Reminder de-dupe markers |

Nothing in this table is committed. `settings.json` stays on this machine; JSON **export does not include** Telegram bot tokens or Discord webhook URLs (enabled flags are kept).

## Task Scheduler

When Task Scheduler sync is enabled (default), TNR-Booklet registers one-shot tasks under:

**Task Scheduler Library → Focus**

Each entry re-launches:

```text
Focus.exe --remind {taskId}
```

That path shows a toast (if enabled), advances recurring reminders, resyncs the next fire, and optionally messages Telegram/Discord. The main window does not need to stay open.

## Messaging setup

Tokens and webhook URLs are stored only in `%LocalAppData%\Focus\settings.json` on this PC.

### Telegram

1. In Telegram, talk to [@BotFather](https://t.me/BotFather) → `/newbot` → copy the **bot token**.
2. Start a chat with your bot and send `/start`.
3. Open `https://api.telegram.org/bot<TOKEN>/getUpdates` in a browser.
4. Find `"chat":{"id": ...}` — that number is your **chat id**.
5. In TNR-Booklet → **Settings**: enable Telegram, paste token + chat id → **Save** → **Test send**.

### Discord

1. In a Discord channel: **Edit channel → Integrations → Webhooks → New Webhook**.
2. Copy the webhook URL.
3. In TNR-Booklet → **Settings**: enable Discord, paste URL → **Save** → **Test send**.

Toolbar **Send list** posts today’s open tasks via every enabled channel.

## Project layout

```text
src/Focus.Core   domain, SQLite, recurrence, scheduler, messaging
src/Focus        WPF UI, tray, toasts, composition root
tests/Focus.Tests
```

## License

[MIT](LICENSE). Copyright (c) 2026 JxDog72.

No warranty. Local data only — TNR-Booklet does not upload your todos, notes, or reminder settings.
