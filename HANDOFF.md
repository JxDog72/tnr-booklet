# FOCUS — Session Handoff

**Last updated:** 2026-07-10  
**Branch:** `master`  
**Status:** Usable v1 Windows desktop app. User paused for the night.

---

## What this project is

**FOCUS** is a local-first Windows **todo / notes / reminders** app:

- C# **WPF** + **.NET 9** (`net9.0-windows`)
- **SQLite** under `%LocalAppData%\Focus\`
- **Windows Task Scheduler** so reminders fire when the app is closed
- Dark theme (green / purple / red accents), folders, tags, smart views
- Optional **Telegram** + **Discord** outbound messaging
- No voice, no cloud sync

**Repo layout**

```
todoReminders/
  Focus.sln
  Run-FOCUS.bat              # double-click launcher
  README.md
  HANDOFF.md                # this file
  docs/superpowers/specs/   # design spec
  docs/superpowers/plans/   # implementation plan
  src/Focus/                # WPF UI
  src/Focus.Core/           # models, SQLite, recurrence, messaging, scheduler
  tests/Focus.Tests/        # xUnit + FluentAssertions (~27 tests)
```

---

## How to run

```powershell
cd <repo-root>
dotnet build src\Focus\Focus.csproj -c Release
# or double-click:
.\Run-FOCUS.bat
```

Data lives **outside** the repo:

| Path | Purpose |
|------|---------|
| `%LocalAppData%\Focus\focus.db` | Tasks, folders, tags |
| `%LocalAppData%\Focus\settings.json` | Notifications, Telegram/Discord credentials |
| `%LocalAppData%\Focus\themes.json` | Themes |

**Do not commit** those files (gitignored). Tokens stay local only.

---

## What’s implemented (v1)

### Core
- Domain models: folders, tags, tasks, recurrence, themes, settings, export bundle
- Recurrence calculator (daily / weekly weekdays+time / monthly / every N days)
- `TaskStore` SQLite + smart views (Today, Upcoming, Overdue, Completed, All)
- Settings + themes JSON, export/import
- Task Scheduler bridge (`--remind {id}`)
- `ReminderAdvance` on fire / complete
- Messaging: Telegram bot API + Discord webhook + list formatter

### UI
- Main window: toolbar, collapsible sidebar, task list
- **+ Todo** / **+ Note** (notes skip due/reminder/scheduler)
- Task editor with type selector
- Settings cards: notifications, scheduler, Telegram, Discord
- Theme editor (hex fields)
- **Folder color picker**: presets + RGB sliders + Windows color wheel
- Tray icon, close-to-tray, single-instance
- Toast / sound / popup focus toggles

### Known gaps / polish
1. **Toast** on unpackaged desktop may need AUMID / notification permission on some PCs; fallback MessageBox exists
2. **Sidebar collapse** narrows width; labels may not fully icon-rail yet
3. **Theme editor** still uses hex text fields (folder picker is the nicer UX)
4. **Git worktree** may exist at `.worktrees/focus-app` (ignored) — optional cleanup
5. Notes vs todos: no dedicated “Notes only” smart view yet
6. Messaging: Telegram needs **chat id** (not username alone); Discord is **webhook** (channel), not DM-by-username
7. No installer; portable/`dotnet run` only

---

## Design decisions (locked)

- Stack: C# WPF, not Electron/Python
- Local SQLite + JSON export/import; no cloud
- Recurring advance on fire and on complete (habit-style for recurring)
- Reminder channels independently toggleable
- Full theme map + per-folder colors
- Voice: out of scope

Docs:

- Spec: `docs/superpowers/specs/2026-07-10-focus-todo-reminders-design.md`
- Plan: `docs/superpowers/plans/2026-07-10-focus-todo-reminders.md`

---

## Suggested next sessions

Priority ideas if you continue later:

1. **Notes smart view** + filter chips (Todos only / Notes only)
2. Reuse **color picker** in theme editor
3. **Installer** or single-folder `dotnet publish` script
4. Improve **toast** reliability (package identity / AUMID)
5. Right-click context menus on tasks/folders
6. Optional daily digest to Telegram/Discord on a schedule
7. Unit test for `ItemKind` + note scheduler skip

---

## Commands cheatsheet

```powershell
dotnet test Focus.sln -c Release
dotnet build Focus.sln -c Release
dotnet run --project src\Focus\Focus.csproj -c Release

# Publish self-contained (optional)
dotnet publish src\Focus\Focus.csproj -c Release -r win-x64 --self-contained true -o publish
```

Task Scheduler folder in Windows: **`Focus`** (`remind-{taskId}` tasks).

---

## Privacy / GitHub notes

- App **settings and DB are not in git** (local AppData only)
- Mutex / event names use generic `Focus.App.*` (no personal name)
- Design docs avoid absolute user profile paths
- Test tokens are fake (`test-token`, `12345`)
- Prefer a **private** GitHub repo if the remote was created that way

If you ever paste real bot tokens into chat or screenshots, rotate them in BotFather / Discord.

---

## Session context (for the next agent)

User (Windows) built FOCUS collaboratively via design → plan → implementation. Recent UX work:

- Color picker for folders
- Settings visibility (cards + dark checkboxes)
- Todo vs Note

They stopped for the night and asked for this handoff + GitHub push with a PII scrub.

**Resume by:** reading this file + `README.md`, running `dotnet test`, launching `Run-FOCUS.bat`, then picking a “Suggested next sessions” item.
