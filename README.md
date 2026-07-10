# FOCUS

Local-first Windows todo and reminder app. Sharp dark UI, folders/tags/smart views, deep theming, and reminders that still fire when the app is closed (via Windows Task Scheduler).

**No voice. No cloud.** All data stays on your machine under `%LocalAppData%\Focus\`.

## Requirements

- Windows 10 or 11
- [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) SDK (to build) or .NET 9 desktop runtime (to run a framework-dependent publish)

## Build

```powershell
dotnet build Focus.sln
```

## Run

```powershell
dotnet run --project src/Focus/Focus.csproj
```

## Test

```powershell
dotnet test Focus.sln
```

## Publish

Framework-dependent (smaller; needs runtime installed):

```powershell
dotnet publish src/Focus/Focus.csproj -c Release -r win-x64 --self-contained false -o publish
```

Self-contained:

```powershell
dotnet publish src/Focus/Focus.csproj -c Release -r win-x64 --self-contained true -o publish
```

Run `publish\Focus.exe`.

## Data folder

| Path | Purpose |
|------|---------|
| `%LocalAppData%\Focus\focus.db` | SQLite tasks, folders, tags |
| `%LocalAppData%\Focus\settings.json` | App settings |
| `%LocalAppData%\Focus\themes.json` | Named themes |
| `%LocalAppData%\Focus\last-fire-*.txt` | Reminder de-dupe markers |

## Task Scheduler

When Task Scheduler sync is enabled (default), FOCUS registers one-shot tasks under:

**Task Scheduler Library → Focus**

Each entry re-launches:

```text
Focus.exe --remind {taskId}
```

That path shows a toast (if enabled), advances recurring reminders, resyncs the next fire, and optionally messages Telegram/Discord.

## Features

- Folders (Work / Personal seeds + custom) with per-folder colors
- Tags
- Smart views: All, Today, Upcoming, Overdue, Completed
- One-shot and recurring reminders (daily / weekly weekdays / monthly / every N days)
- Toast, sound, popup-focus, tray, close-to-tray, pause notifications
- Theme editor with live apply (Focus Dark defaults)
- Export / import full JSON bundle
- Optional Telegram + Discord messaging (today’s list + reminder lines)
- Single-instance UI; `--remind` can run headless

## Messaging setup

### Telegram

1. In Telegram, talk to [@BotFather](https://t.me/BotFather) → `/newbot` → copy the **bot token**.
2. Start a chat with your bot and send `/start`.
3. Open `https://api.telegram.org/bot<TOKEN>/getUpdates` in a browser.
4. Find `"chat":{"id": ...}` — that number is your **chat id**.
5. In FOCUS → **Settings**: enable Telegram, paste token + chat id → **Save** → **Test send**.

### Discord

1. In a Discord channel: **Edit channel → Integrations → Webhooks → New Webhook**.
2. Copy the webhook URL.
3. In FOCUS → **Settings**: enable Discord, paste URL → **Save** → **Test send**.

Toolbar **Send list** posts today’s open tasks via every enabled channel.

## Project layout

```text
src/Focus.Core   domain, SQLite, recurrence, scheduler, messaging
src/Focus        WPF UI, tray, toasts, composition root
tests/Focus.Tests
```

## License

Local project — use and modify freely for personal use.
