# FOCUS — Drop-down colors, progress ticks, and reorder

**Date:** 2026-08-24  
**Status:** Approved (approach 1; user asked to implement)

## Summary

Make Type / Folder / Priority ComboBoxes readable and independently themable via swatches. Style list priority labels. Add a 1–10 progress tick strip on each row (10 = done). Allow drag-reorder with a left grip.

## Data

- `tasks.progress` INTEGER 1–10, default 1. Existing Done rows backfill to 10.
- `tasks.sort_order` INTEGER. Existing all-zero rows backfill by `created_at_utc`.
- Query order: `sort_order, created_at_utc`.
- Export/import: fields live on `TaskItem` (JSON already serializes the model).

## Progress rules

- Tick N sets progress N.
- 10 runs the existing complete path (one-shot → Done; recurring → advance and stay Open with progress 1).
- Tick 1–9 on a Done item reopens it at that progress.
- Checkbox on → same as progress 10. Checkbox off → progress 9, Open (non-recurring).

## Theme

New `ThemeColors` (defaults readable, independently pickable):

- ComboTypeBg `#2A1F4A` / ComboTypeFg `#E5E5E5`
- ComboFolderBg `#0F2A22` / ComboFolderFg `#D1FAE5`
- ComboPriorityBg `#2A2208` / ComboPriorityFg `#FDE68A`

Theme editor: swatch + existing color picker (no hex typing). Recurrence ComboBox uses the shared readable template only.

List priority labels (not theme knobs): **HIGH** bold `#F87171`; **Med** `#FB923C`; **Low** `#E5E5E5`.

## UI

- Full ComboBox ControlTemplate so the closed bar and popup inherit Background/Foreground.
- 10 ticks to the right of the title; clickable; green when complete.
- `⋮⋮` grip left of the row; drag to reorder; Alt+Up / Alt+Down. Reorder redistributes `sort_order` among the currently visible list.

## Tests

Store round-trip for progress/sort; query order; progress/complete sync helper; theme default combo colors.
