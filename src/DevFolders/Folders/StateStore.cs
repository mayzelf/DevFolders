using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevFolders.Folders;

public enum FolderKind
{
    Project,
    DevFolder,
}

public sealed class FolderRecord
{
    public string IconFile { get; set; } = "";

    public FolderKind Kind { get; set; }

    public bool CreatedDesktopIni { get; set; }

    public bool SetReadOnly { get; set; }

    public string? GitExcludeEntry { get; set; }

    public Dictionary<string, string> OriginalValues { get; set; } = [];
}

public sealed class AppState
{
    public Dictionary<string, FolderRecord> Folders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Persists which folders DevFolders has changed and how to undo each change. Access goes through
/// a named mutex because Explorer starts one process per selected folder.
/// </summary>
public static class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static AppState Read()
    {
        using var _ = new StateLock();
        return Load();
    }

    public static void Update(Action<AppState> change)
    {
        using var _ = new StateLock();
        var state = Load();
        change(state);
        Directory.CreateDirectory(AppPaths.DataDirectory);
        string temporary = AppPaths.StateFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporary, AppPaths.StateFile, overwrite: true);
    }

    private static AppState Load()
    {
        if (!File.Exists(AppPaths.StateFile))
        {
            return new AppState();
        }

        var loaded = JsonSerializer.Deserialize<AppState>(File.ReadAllText(AppPaths.StateFile), JsonOptions) ?? new AppState();
        return new AppState { Folders = new(loaded.Folders, StringComparer.OrdinalIgnoreCase) };
    }

    private sealed class StateLock : IDisposable
    {
        private readonly Mutex _mutex = new(false, @"Local\DevFolders.State");

        public StateLock()
        {
            try
            {
                _mutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
            }
        }

        public void Dispose()
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
