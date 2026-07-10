# FOCUS — Windows Todo / Reminders App Design

**Date:** 2026-07-10  
**Status:** Approved for implementation planning  
**Location:** `C:\Users\JimmyB\OneDrive\GrokBuild\todoReminders`

## 1. Summary

FOCUS is a local-first Windows desktop todo and reminder app for work + personal use. It prioritizes low idle resource use, strong organization (folders, tags, smart views), deep visual customization, and reminders that fire even when the app is not running (via Windows Task Scheduler).

**Working product name:** FOCUS (renameable before ship).

## 2. Goals and non-goals

### Goals (v1)

- Fast capture and management of tasks with folders, categories (folders), tags, priorities, due times, and timed reminders
- Mix of one-shot and recurring reminders (including weekdays + time of day)
- Collapsible left sidebar + top toolbar main chrome
- Dark default theme with sharp green / purple / red accents on black
- Full theme color map + named themes + per-folder accent colors
- Configurable notification channels: Windows toast, sound, popup focus, system tray
- Local SQLite storage + JSON export/import
- Reminders work when the app is closed (Task Scheduler)
- Efficient single-process desktop app (no Electron, no cloud, no voice)

### Non-goals (v1)

- Voice input / speech commands (explicitly removed)
- Cloud sync or multi-device accounts
- Mobile clients
- Full cron / “Nth weekday of month” recurrence
- Multi-user or multi-tenant data
- Browser-based UI

## 3. Primary use case

**Work + personal on one PC:** separate folders (e.g. Work, Personal, Finance), tags and smart views, reliable local reminders without leaving the app running 24/7.

## 4. Architecture

### Stack

| Layer | Choice |
|-------|--------|
| Language / UI | C# + WPF (.NET 8) |
| Data | SQLite (Microsoft.Data.Sqlite) |
| Settings / themes | JSON files beside the DB |
| Background fire | Windows Task Scheduler (user-level tasks) |
| Packaging | Framework-dependent or self-contained single-folder publish |

### Process model

- Single instance for normal UI launches (second launch activates existing window).
- Exception: `--remind {taskId}` launches notification flow; coordinates with running instance when present to avoid duplicate toasts.
- Optional close-to-tray so the process can stay resident when desired; not required for reminders (scheduler handles closed state).

### Components

```
FOCUS.exe
├── UI
│   ├── MainWindow (toolbar + collapsible sidebar + task list)
│   ├── Task editor (dialog or detail panel)
│   ├── Settings
│   └── Theme editor
├── Services
│   ├── TaskStore (SQLite CRUD, queries for views)
│   ├── ReminderScheduler (sync Task Scheduler jobs + next_fire_at)
│   ├── NotificationService (toast / sound / focus / tray)
│   ├── ThemeService (load/apply/save themes)
│   └── ExportImportService
└── Data
    ├── %LocalAppData%\Focus\focus.db
    ├── %LocalAppData%\Focus\settings.json
    └── %LocalAppData%\Focus\themes.json
```

### Efficiency principles

- No cloud, voice, or embedded browser engine
- Scheduler uses OS Task Scheduler for wake-ups; in-process work only when UI is open or `--remind` runs
- SQLite access on load, user actions, and reminder fire — not busy polling
- Lightweight list virtualization if task counts grow large

## 5. Data model

### Folder

| Field | Notes |
|-------|--------|
| id | GUID/text PK |
| name | Display name |
| color | Hex accent for list strip + sidebar |
| sort_order | Integer |
| created_at | UTC |

Default seed: **Work**, **Personal**.

### Tag

| Field | Notes |
|-------|--------|
| id | PK |
| name | Unique (case-insensitive) |
| color | Optional hex |

### Task

| Field | Notes |
|-------|--------|
| id | PK |
| title | Required |
| notes | Optional long text |
| folder_id | FK |
| status | `open` \| `done` |
| priority | `none` \| `low` \| `medium` \| `high` |
| due_at | Nullable UTC/local-stored instant |
| reminder_at | Nullable; next/one-shot reminder instant |
| completed_at | Nullable |
| created_at / updated_at | |

### Recurrence (1:1 with task, or embedded columns)

| Field | Notes |
|-------|--------|
| kind | `none` \| `daily` \| `weekly` \| `monthly` \| `every_n_days` |
| weekdays | Bitmask or set for Mon–Sun (weekly) |
| time_of_day | Local time for fire |
| interval_n | For every_n_days (and monthly day-of-month as needed) |
| next_fire_at | Cached next occurrence for scheduler |

**Behavior:**

- **One-shot:** reminder fires once; Task Scheduler job removed after fire; task remains `open` until the user completes it → `done`.
- **Recurring — on fire:** compute next occurrence, update `next_fire_at` / due fields, reschedule Task Scheduler job immediately (so the OS always has the next run even if the user dismisses the toast).
- **Recurring — on complete:** treat as “done for this occurrence”: advance due/reminder to the next occurrence, keep status `open`, reschedule job. (Habit-style; not a permanent archive unless the user deletes the task or clears recurrence.)
- **Weekly example:** Mon/Wed/Fri at 09:00 local.

### TaskTag

Many-to-many join: `task_id`, `tag_id`.

### Settings (JSON)

Examples: notification channel flags, close-to-tray, pause-notifications, Task Scheduler enabled, wake-computer-to-run, default folder id, sound path, window bounds, sidebar collapsed state.

### Themes (JSON)

Named themes; each is a full color map. Active theme id in settings.

### Export / import

Single JSON bundle containing folders, tags, tasks, recurrence, themes, and settings. Import validates fully before applying; no partial destructive apply on validation failure. Import strategy for v1: replace or merge — **replace-with-confirm** for full restore; optional merge deferred if time-boxed.

## 6. UI design

### Main window

- **Top toolbar:** sidebar collapse control, quick-add field (Enter creates task in current folder/view context), + Task (full editor), Search, Export/Import entry, Settings
- **Left sidebar (collapsible to icon rail):**
  - Folders (colored)
  - Smart views: Today, Upcoming, Overdue, Completed (and All)
  - Tags
  - Context menus: rename, recolor, delete folder/tag
- **Task list:** checkbox complete; left border = folder color; subtitle shows due/reminder/recurrence/tags; overdue styling uses danger/red tokens
- **Task editor:** title, notes, folder, priority, due, reminder, recurrence controls, tags

### Settings

- Channels: toast, sound, popup focus, tray (independent toggles)
- Task Scheduler registration on/off (default on)
- Wake computer to run reminder (optional)
- Close to tray
- Pause notifications
- Theme editor launch
- Open data folder / export / import

### Theme editor

- Edit full color map with live preview
- Save as named theme; switch themes
- Default theme **Focus Dark**: near-black backgrounds; purple brand; green success/today; red overdue/danger; muted secondary text

### System tray

- Icon when tray channel or close-to-tray enabled
- Menu: Open, Today, Add task, Pause notifications, Exit
- Double-click opens main window

### Voice

None. No mic controls or speech APIs.

## 7. Reminders and notifications

### Source of truth

Windows Task Scheduler jobs under folder `\Focus\` (e.g. `\Focus\remind-{taskId}`), user-level (no admin required for normal use).

### Lifecycle

1. User saves task with reminder/recurrence → persist SQLite → create/update scheduled task.
2. At fire time → `FOCUS.exe --remind {taskId}`.
3. Load task; if missing → log and exit quietly.
4. Apply enabled channels (toast, sound, popup focus, tray).
5. Recurring → compute `next_fire_at`, update DB + reschedule job (task stays open).
6. One-shot → remove scheduled job; leave task open until user completes.
7. Delete task / clear reminder → delete scheduled job.

### Concurrency

- Dedupe notifications for the same task id within a short fire window so open-app + scheduler do not double-alert.
- Single-instance mutex for UI; `--remind` uses a short-lived notify path.

### Missed reminders

If the PC was off past fire time, on next app start surface overdue / missed items in the Overdue view (and optional startup summary if any missed).

### Pause

While paused: suppress toast/sound/focus (quiet). Jobs may still run; UI remains quiet until unpaused. Exact behavior: **quiet suppress**, not “skip and never show.”

## 8. Theming model

Color tokens (minimum set):

- `bg.app`, `bg.sidebar`, `bg.toolbar`, `bg.surface`, `bg.surfaceAlt`
- `border.default`, `border.focus`
- `text.primary`, `text.secondary`, `text.muted`
- `accent` (purple default), `success` (green), `warning`, `danger` / `overdue` (red)
- `selection.bg`, `selection.fg`

Per-folder `color` overrides list accent only; does not replace global theme.

Applied at runtime via WPF `ResourceDictionary` updates.

## 9. Error handling

| Situation | Behavior |
|-----------|----------|
| DB open/migrate failure | Modal error with data path; do not wipe DB |
| Task Scheduler register failure | Task still saved; UI banner + retry action |
| Invalid import file | Reject with validation messages; no apply |
| `--remind` unknown id | Quiet exit |
| Sound file missing | Skip sound; still toast if enabled |
| Theme file corrupt | Fall back to built-in Focus Dark |

## 10. Testing strategy

- **Unit:** recurrence next-fire calculation (daily, weekly weekdays+time, monthly, every N days); theme default merge; export/import round-trip serialization
- **Integration / manual:** creating a reminder creates a Task Scheduler entry; `--remind` triggers configured channels; sidebar collapse; close-to-tray; pause notifications; folder colors
- **Performance smoke:** idle with tray; ensure no high CPU timer loops

## 11. Project layout (implementation target)

```
todoReminders/
  docs/superpowers/specs/     # this design
  src/Focus/                  # WPF app
  src/Focus.Core/             # domain, recurrence, models (testable)
  tests/Focus.Tests/          # unit tests
  README.md
```

Exact solution structure may adjust slightly during implementation planning; keep UI and pure logic separable so recurrence and import validate without STA UI.

## 12. Decisions log

| Decision | Choice |
|----------|--------|
| Platform | Windows desktop only |
| UI stack | C# WPF (.NET 8) |
| Use case | Work + personal |
| Layout | Top toolbar + collapsible sidebar |
| Theme default | Dark, green/purple/red on black |
| Voice | Out of scope |
| Data | Local SQLite + export/import JSON |
| Recurrence | Simple: daily / weekly(weekdays+time) / monthly / every N days + one-shot |
| Notifications | Toast, sound, popup focus, tray — all optional |
| Background | Windows Task Scheduler |
| Cloud | Not in v1 |
| Theming | Full color map + named themes + per-folder colors |

## 13. Open items for implementation plan (not blockers)

- App display name finalization (FOCUS vs user rename)
- Installer vs portable zip (prefer portable folder publish for v1)
- Task editor: modal dialog for v1 (detail panel can wait)

## 14. Success criteria

- User can organize work vs personal with folders, tags, and smart views
- One-shot and weekday+time recurring reminders fire with app closed
- Notification channels independently configurable
- Theme fully recolorable; folders have custom accents
- Export/import restores data
- Idle resource use remains modest (no Electron-class footprint)

## 15. Optional messaging bridges (Telegram / Discord)

Added mid-implementation at user request. **Opt-in only; off by default.**

### Purpose
Push a todo/reminder summary (or a single reminder) to the user via Telegram and/or Discord without cloud sync of the database.

### How it works (technical reality)

| Channel | Mechanism | Addressing |
|---------|-----------|------------|
| **Telegram** | Official Bot API sendMessage over HTTPS | Requires a **bot token** (from @BotFather) and a **chat id**. The user must press **Start** on the bot once. Plain @username alone is not reliable for private DMs until the bot has a chat id. |
| **Discord** | Incoming **webhook** URL to a channel | Webhook posts into a channel the user controls (e.g. private #focus-reminders). True DMs-by-username need a full bot + numeric user id; v1 uses webhooks for simplicity and low compute. |

### Settings (in AppSettings / Settings UI)
- TelegramEnabled, TelegramBotToken, TelegramChatId
- DiscordEnabled, DiscordWebhookUrl
- MessagingOnReminder � also send when a reminder fires
- Manual actions: **Send today's list**, **Send test message**

### Security
- Tokens stored only in local settings.json under %LocalAppData%\Focus\
- Never log full tokens
- No inbound servers; app only makes outbound HTTPS when sending
- Failures show a non-fatal UI banner (task still saved / toast still works)

### Non-goals for messaging v1
- Multi-account team bots
- Receiving commands from Telegram/Discord to create tasks (can be a later feature)
- Guaranteed delivery if offline (queue optional later)
