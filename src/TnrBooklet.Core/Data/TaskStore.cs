using Focus.Core.Models;
using Microsoft.Data.Sqlite;

namespace Focus.Core.Data;

public sealed class TaskStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    private const string LocalDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
    private const string TimeFormat = "HH:mm";

    public TaskStore(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true
        }.ToString());
        _connection.Open();
    }

    public void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS folders (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              color TEXT NOT NULL,
              sort_order INTEGER NOT NULL,
              created_at_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS tags (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL UNIQUE,
              color TEXT
            );
            CREATE TABLE IF NOT EXISTS tasks (
              id TEXT PRIMARY KEY,
              title TEXT NOT NULL,
              notes TEXT NOT NULL DEFAULT '',
              folder_id TEXT NOT NULL,
              status INTEGER NOT NULL,
              priority INTEGER NOT NULL,
              due_at_local TEXT,
              reminder_at_local TEXT,
              completed_at_utc TEXT,
              created_at_utc TEXT NOT NULL,
              updated_at_utc TEXT NOT NULL,
              rec_kind INTEGER NOT NULL DEFAULT 0,
              rec_weekdays INTEGER NOT NULL DEFAULT 0,
              rec_time TEXT NOT NULL DEFAULT '09:00',
              rec_interval INTEGER NOT NULL DEFAULT 1,
              rec_next_fire_local TEXT,
              kind INTEGER NOT NULL DEFAULT 0,
              FOREIGN KEY(folder_id) REFERENCES folders(id)
            );
            CREATE TABLE IF NOT EXISTS task_tags (
              task_id TEXT NOT NULL,
              tag_id TEXT NOT NULL,
              PRIMARY KEY(task_id, tag_id)
            );
            """;
        cmd.ExecuteNonQuery();
        EnsureColumn("tasks", "kind", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn("tasks", "progress", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn("tasks", "sort_order", "INTEGER NOT NULL DEFAULT 0");
        BackfillProgressAndSort();

        if (GetFolders().Count == 0)
        {
            UpsertFolder(new Folder
            {
                Name = "Work",
                Color = "#A78BFA",
                SortOrder = 0
            });
            UpsertFolder(new Folder
            {
                Name = "Personal",
                Color = "#34D399",
                SortOrder = 1
            });
        }
    }

    public IReadOnlyList<Folder> GetFolders()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, color, sort_order, created_at_utc
            FROM folders
            ORDER BY sort_order, name;
            """;

        var list = new List<Folder>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Folder
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Color = reader.GetString(2),
                SortOrder = reader.GetInt32(3),
                CreatedAtUtc = ParseUtc(reader.GetString(4))
            });
        }
        return list;
    }

    public void UpsertFolder(Folder folder)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO folders (id, name, color, sort_order, created_at_utc)
            VALUES ($id, $name, $color, $sort, $created)
            ON CONFLICT(id) DO UPDATE SET
              name = excluded.name,
              color = excluded.color,
              sort_order = excluded.sort_order;
            """;
        cmd.Parameters.AddWithValue("$id", folder.Id);
        cmd.Parameters.AddWithValue("$name", folder.Name);
        cmd.Parameters.AddWithValue("$color", folder.Color);
        cmd.Parameters.AddWithValue("$sort", folder.SortOrder);
        cmd.Parameters.AddWithValue("$created", FormatUtc(folder.CreatedAtUtc));
        cmd.ExecuteNonQuery();
    }

    public void DeleteFolder(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM folders WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<Tag> GetTags()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, color FROM tags ORDER BY name;";

        var list = new List<Tag>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Tag
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Color = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return list;
    }

    public void UpsertTag(Tag tag)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tags (id, name, color)
            VALUES ($id, $name, $color)
            ON CONFLICT(id) DO UPDATE SET
              name = excluded.name,
              color = excluded.color;
            """;
        cmd.Parameters.AddWithValue("$id", tag.Id);
        cmd.Parameters.AddWithValue("$name", tag.Name);
        cmd.Parameters.AddWithValue("$color", (object?)tag.Color ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTag(string id)
    {
        using var tx = _connection.BeginTransaction();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM task_tags WHERE tag_id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM tags WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public TaskItem? GetTask(string id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, title, notes, folder_id, status, priority, kind, progress, sort_order,
                   due_at_local, reminder_at_local, completed_at_utc,
                   created_at_utc, updated_at_utc,
                   rec_kind, rec_weekdays, rec_time, rec_interval, rec_next_fire_local
            FROM tasks
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var task = ReadTask(reader);
        reader.Close();
        task.TagIds = GetTagIdsForTask(id);
        return task;
    }

    public void UpsertTask(TaskItem task)
    {
        using var tx = _connection.BeginTransaction();

        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO tasks (
                  id, title, notes, folder_id, status, priority, kind, progress, sort_order,
                  due_at_local, reminder_at_local, completed_at_utc,
                  created_at_utc, updated_at_utc,
                  rec_kind, rec_weekdays, rec_time, rec_interval, rec_next_fire_local
                ) VALUES (
                  $id, $title, $notes, $folder_id, $status, $priority, $kind, $progress, $sort,
                  $due, $reminder, $completed,
                  $created, $updated,
                  $rec_kind, $rec_weekdays, $rec_time, $rec_interval, $rec_next
                )
                ON CONFLICT(id) DO UPDATE SET
                  title = excluded.title,
                  notes = excluded.notes,
                  folder_id = excluded.folder_id,
                  status = excluded.status,
                  priority = excluded.priority,
                  kind = excluded.kind,
                  progress = excluded.progress,
                  sort_order = excluded.sort_order,
                  due_at_local = excluded.due_at_local,
                  reminder_at_local = excluded.reminder_at_local,
                  completed_at_utc = excluded.completed_at_utc,
                  created_at_utc = excluded.created_at_utc,
                  updated_at_utc = excluded.updated_at_utc,
                  rec_kind = excluded.rec_kind,
                  rec_weekdays = excluded.rec_weekdays,
                  rec_time = excluded.rec_time,
                  rec_interval = excluded.rec_interval,
                  rec_next_fire_local = excluded.rec_next_fire_local;
                """;
            cmd.Parameters.AddWithValue("$id", task.Id);
            cmd.Parameters.AddWithValue("$title", task.Title);
            cmd.Parameters.AddWithValue("$notes", task.Notes);
            cmd.Parameters.AddWithValue("$folder_id", task.FolderId);
            cmd.Parameters.AddWithValue("$status", (int)task.Status);
            cmd.Parameters.AddWithValue("$priority", (int)task.Priority);
            cmd.Parameters.AddWithValue("$kind", (int)task.Kind);
            cmd.Parameters.AddWithValue("$progress", TaskProgress.Clamp(task.Progress));
            cmd.Parameters.AddWithValue("$sort", task.SortOrder);
            cmd.Parameters.AddWithValue("$due", ToDbLocal(task.DueAtLocal));
            cmd.Parameters.AddWithValue("$reminder", ToDbLocal(task.ReminderAtLocal));
            cmd.Parameters.AddWithValue("$completed", ToDbUtc(task.CompletedAtUtc));
            cmd.Parameters.AddWithValue("$created", FormatUtc(task.CreatedAtUtc));
            cmd.Parameters.AddWithValue("$updated", FormatUtc(task.UpdatedAtUtc));
            cmd.Parameters.AddWithValue("$rec_kind", (int)task.Recurrence.Kind);
            cmd.Parameters.AddWithValue("$rec_weekdays", task.Recurrence.WeekdaysMask);
            cmd.Parameters.AddWithValue("$rec_time", task.Recurrence.TimeOfDay.ToString(TimeFormat));
            cmd.Parameters.AddWithValue("$rec_interval", task.Recurrence.IntervalN);
            cmd.Parameters.AddWithValue("$rec_next", ToDbLocal(task.Recurrence.NextFireAtLocal));
            cmd.ExecuteNonQuery();
        }

        using (var del = _connection.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM task_tags WHERE task_id = $id;";
            del.Parameters.AddWithValue("$id", task.Id);
            del.ExecuteNonQuery();
        }

        foreach (var tagId in task.TagIds.Distinct())
        {
            using var ins = _connection.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = "INSERT INTO task_tags (task_id, tag_id) VALUES ($task_id, $tag_id);";
            ins.Parameters.AddWithValue("$task_id", task.Id);
            ins.Parameters.AddWithValue("$tag_id", tagId);
            ins.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void DeleteTask(string id)
    {
        using var tx = _connection.BeginTransaction();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM task_tags WHERE task_id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM tasks WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>
    /// Deletes all task_tags, tasks, tags, and folders. Does not re-seed defaults.
    /// </summary>
    public void ClearAll()
    {
        using var tx = _connection.BeginTransaction();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM task_tags;";
            cmd.ExecuteNonQuery();
        }
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM tasks;";
            cmd.ExecuteNonQuery();
        }
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM tags;";
            cmd.ExecuteNonQuery();
        }
        using (var cmd = _connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM folders;";
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public IReadOnlyList<TaskItem> QueryTasks(SmartView view, string? folderId, string? tagId)
    {
        var today = DateTime.Today;
        var todayStart = today.ToString(LocalDateTimeFormat);
        var todayEnd = today.AddDays(1).AddTicks(-1).ToString(LocalDateTimeFormat);
        var todayDate = today.ToString("yyyy-MM-dd");

        var sql = """
            SELECT DISTINCT t.id, t.title, t.notes, t.folder_id, t.status, t.priority, t.kind,
                   t.progress, t.sort_order,
                   t.due_at_local, t.reminder_at_local, t.completed_at_utc,
                   t.created_at_utc, t.updated_at_utc,
                   t.rec_kind, t.rec_weekdays, t.rec_time, t.rec_interval, t.rec_next_fire_local
            FROM tasks t
            """;

        if (!string.IsNullOrEmpty(tagId))
            sql += " INNER JOIN task_tags tt ON tt.task_id = t.id AND tt.tag_id = $tagId";

        sql += " WHERE 1=1";

        switch (view)
        {
            case SmartView.Today:
                sql += """
                     AND t.status = $open
                     AND (
                       substr(t.due_at_local, 1, 10) = $todayDate
                       OR substr(t.reminder_at_local, 1, 10) = $todayDate
                       OR substr(t.rec_next_fire_local, 1, 10) = $todayDate
                     )
                    """;
                break;
            case SmartView.Upcoming:
                // Open items still in the future (later today or another day).
                sql += """
                     AND t.status = $open
                     AND (
                       t.due_at_local > $now
                       OR t.reminder_at_local > $now
                       OR t.rec_next_fire_local > $now
                     )
                    """;
                break;
            case SmartView.Overdue:
                // Open items whose due/reminder/next fire is already in the past.
                sql += """
                     AND t.status = $open
                     AND (
                       (t.due_at_local IS NOT NULL AND t.due_at_local < $now)
                       OR (t.reminder_at_local IS NOT NULL AND t.reminder_at_local < $now)
                       OR (t.rec_next_fire_local IS NOT NULL AND t.rec_next_fire_local < $now)
                     )
                    """;
                break;
            case SmartView.Completed:
                sql += " AND t.status = $done";
                break;
            case SmartView.All:
            default:
                break;
        }

        if (!string.IsNullOrEmpty(folderId))
            sql += " AND t.folder_id = $folderId";

        sql += " ORDER BY t.sort_order, t.created_at_utc;";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$open", (int)FocusTaskStatus.Open);
        cmd.Parameters.AddWithValue("$done", (int)FocusTaskStatus.Done);
        cmd.Parameters.AddWithValue("$todayDate", todayDate);
        cmd.Parameters.AddWithValue("$todayStart", todayStart);
        cmd.Parameters.AddWithValue("$todayEnd", todayEnd);
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString(LocalDateTimeFormat));
        if (!string.IsNullOrEmpty(folderId))
            cmd.Parameters.AddWithValue("$folderId", folderId);
        if (!string.IsNullOrEmpty(tagId))
            cmd.Parameters.AddWithValue("$tagId", tagId);

        var list = new List<TaskItem>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                list.Add(ReadTask(reader));
        }

        foreach (var task in list)
            task.TagIds = GetTagIdsForTask(task.Id);

        return list;
    }

    private List<string> GetTagIdsForTask(string taskId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT tag_id FROM task_tags WHERE task_id = $id;";
        cmd.Parameters.AddWithValue("$id", taskId);

        var ids = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            ids.Add(reader.GetString(0));
        return ids;
    }

    public int NextSortOrder()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(sort_order), -1) FROM tasks;";
        var value = cmd.ExecuteScalar();
        var max = value is long l ? (int)l : Convert.ToInt32(value ?? -1);
        return max + 1;
    }

    public void ReorderVisible(IReadOnlyList<string> idsInNewOrder)
    {
        if (idsInNewOrder.Count == 0)
            return;

        var current = new List<(string Id, int Sort)>();
        foreach (var id in idsInNewOrder)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT sort_order FROM tasks WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            var value = cmd.ExecuteScalar();
            if (value is null or DBNull)
                continue;
            current.Add((id, Convert.ToInt32(value)));
        }

        if (current.Count == 0)
            return;

        var slots = current.Select(x => x.Sort).OrderBy(x => x).ToList();
        if (slots.Distinct().Count() <= 1)
            slots = Enumerable.Range(0, current.Count).ToList();

        using var tx = _connection.BeginTransaction();
        for (var i = 0; i < current.Count; i++)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE tasks SET sort_order = $s, updated_at_utc = $u WHERE id = $id;";
            cmd.Parameters.AddWithValue("$s", slots[i]);
            cmd.Parameters.AddWithValue("$u", FormatUtc(DateTime.UtcNow));
            cmd.Parameters.AddWithValue("$id", current[i].Id);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    private void BackfillProgressAndSort()
    {
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "UPDATE tasks SET progress = 10 WHERE status = 1 AND progress < 10;";
            cmd.ExecuteNonQuery();
        }

        using var countCmd = _connection.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(*), COALESCE(SUM(CASE WHEN sort_order != 0 THEN 1 ELSE 0 END), 0)
            FROM tasks;
            """;
        using var reader = countCmd.ExecuteReader();
        if (!reader.Read())
            return;
        var count = reader.GetInt32(0);
        var nonzero = reader.GetInt32(1);
        reader.Close();
        if (count == 0 || nonzero > 0)
            return;

        var ids = new List<string>();
        using (var listCmd = _connection.CreateCommand())
        {
            listCmd.CommandText = "SELECT id FROM tasks ORDER BY created_at_utc;";
            using var idReader = listCmd.ExecuteReader();
            while (idReader.Read())
                ids.Add(idReader.GetString(0));
        }

        for (var i = 0; i < ids.Count; i++)
        {
            using var upd = _connection.CreateCommand();
            upd.CommandText = "UPDATE tasks SET sort_order = $s WHERE id = $id;";
            upd.Parameters.AddWithValue("$s", i);
            upd.Parameters.AddWithValue("$id", ids[i]);
            upd.ExecuteNonQuery();
        }
    }

    private static int ReadProgress(SqliteDataReader reader)
    {
        try
        {
            var ordinal = reader.GetOrdinal("progress");
            if (reader.IsDBNull(ordinal))
                return TaskProgress.Min;
            return TaskProgress.Clamp(reader.GetInt32(ordinal));
        }
        catch (IndexOutOfRangeException)
        {
            return TaskProgress.Min;
        }
    }

    private static int ReadSortOrder(SqliteDataReader reader)
    {
        try
        {
            var ordinal = reader.GetOrdinal("sort_order");
            if (reader.IsDBNull(ordinal))
                return 0;
            return reader.GetInt32(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return 0;
        }
    }

    private void EnsureColumn(string table, string column, string definition)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table});";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return;
        }
        reader.Close();

        using var alter = _connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
    }

    private static TaskItem ReadTask(SqliteDataReader reader)
    {
        string GetStr(string name) => reader.GetString(reader.GetOrdinal(name));
        int GetInt(string name) => reader.GetInt32(reader.GetOrdinal(name));
        bool IsNull(string name) => reader.IsDBNull(reader.GetOrdinal(name));
        string? GetStrOrNull(string name) => IsNull(name) ? null : GetStr(name);

        var recTime = GetStr("rec_time");
        if (!TimeOnly.TryParse(recTime, out var timeOfDay))
            timeOfDay = new TimeOnly(9, 0);

        var kind = ItemKind.Todo;
        try
        {
            kind = (ItemKind)GetInt("kind");
        }
        catch (IndexOutOfRangeException)
        {
            // Pre-migration rows / older snapshots
        }

        return new TaskItem
        {
            Id = GetStr("id"),
            Title = GetStr("title"),
            Notes = GetStr("notes"),
            FolderId = GetStr("folder_id"),
            Status = (FocusTaskStatus)GetInt("status"),
            Priority = (TaskPriority)GetInt("priority"),
            Kind = kind,
            Progress = ReadProgress(reader),
            SortOrder = ReadSortOrder(reader),
            DueAtLocal = ParseLocalNullable(GetStrOrNull("due_at_local")),
            ReminderAtLocal = ParseLocalNullable(GetStrOrNull("reminder_at_local")),
            CompletedAtUtc = ParseUtcNullable(GetStrOrNull("completed_at_utc")),
            CreatedAtUtc = ParseUtc(GetStr("created_at_utc")),
            UpdatedAtUtc = ParseUtc(GetStr("updated_at_utc")),
            Recurrence = new RecurrenceRule
            {
                Kind = (RecurrenceKind)GetInt("rec_kind"),
                WeekdaysMask = GetInt("rec_weekdays"),
                TimeOfDay = timeOfDay,
                IntervalN = GetInt("rec_interval"),
                NextFireAtLocal = ParseLocalNullable(GetStrOrNull("rec_next_fire_local"))
            }
        };
    }

    private static object ToDbLocal(DateTime? value) =>
        value.HasValue ? value.Value.ToString(LocalDateTimeFormat) : DBNull.Value;

    private static object ToDbUtc(DateTime? value) =>
        value.HasValue ? FormatUtc(value.Value) : DBNull.Value;

    private static string FormatUtc(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        if (value.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc.ToString("o");
    }

    private static DateTime ParseUtc(string value)
    {
        var dt = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);
    }

    private static DateTime? ParseUtcNullable(string? value) =>
        string.IsNullOrEmpty(value) ? null : ParseUtc(value);

    private static DateTime? ParseLocalNullable(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        var dt = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AssumeLocal);
        return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
    }

    public void BackupTo(string destDbPath)
    {
        var directory = Path.GetDirectoryName(destDbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var dest = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destDbPath,
            ForeignKeys = true
        }.ToString());
        dest.Open();
        _connection.BackupDatabase(dest);
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Best-effort so the .db file can be copied after close.
        }

        _connection.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
