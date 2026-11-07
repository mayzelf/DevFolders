using System.IO;
using System.Security.Cryptography;
using DevFolders.Shell;

namespace DevFolders.Folders;

/// <summary>
/// Applies and removes custom folder icons through desktop.ini. Everything changed on a folder is
/// recorded in the state file so a reset restores the folder exactly as it was.
/// </summary>
public static class FolderIcons
{
    private const string DesktopIni = "desktop.ini";
    private const string Section = ".ShellClassInfo";
    private const string IconResourceKey = "IconResource";
    private static readonly string[] IconKeys = [IconResourceKey, "IconFile", "IconIndex"];

    public static void Apply(string folder, FolderKind kind, byte[] icon)
    {
        folder = AppPaths.NormalizeFolder(folder);
        string iconFile = StoreIcon(icon);
        string iniPath = Path.Combine(folder, DesktopIni);

        StateStore.Update(state =>
        {
            var ini = File.Exists(iniPath) ? IniDocument.Load(iniPath) : new IniDocument();
            bool isNew = false;
            if (!state.Folders.TryGetValue(folder, out var record))
            {
                record = AdoptUntracked(folder, ini);
                if (record == null)
                {
                    record = NewRecord(ini, createdDesktopIni: !File.Exists(iniPath));
                    isNew = true;
                }
            }

            foreach (string key in IconKeys)
            {
                ini.Remove(Section, key);
            }

            ini.Set(Section, IconResourceKey, $"{iconFile},0");
            WriteHidden(iniPath, ini);

            if (isNew)
            {
                record.SetReadOnly = MarkReadOnly(folder);
                record.GitExcludeEntry = GitExclude.Add(folder, DesktopIni);
            }

            string previousIcon = record.IconFile;
            record.IconFile = iconFile;
            record.Kind = kind;
            state.Folders[folder] = record;
            DeleteIconIfUnused(state, previousIcon);
        });

        ShellNotify.ItemUpdated(folder);
    }

    public static void Reset(string folder)
    {
        folder = AppPaths.NormalizeFolder(folder);
        StateStore.Update(state =>
        {
            string iniPath = Path.Combine(folder, DesktopIni);
            if (!state.Folders.Remove(folder, out var record) && File.Exists(iniPath))
            {
                record = AdoptUntracked(folder, IniDocument.Load(iniPath));
            }

            if (record != null)
            {
                Restore(folder, record);
                DeleteIconIfUnused(state, record.IconFile);
            }
        });

        ShellNotify.ItemUpdated(folder);
    }

    public static void ResetAll()
    {
        var folders = new List<string>();
        StateStore.Update(state =>
        {
            foreach (var (folder, record) in state.Folders)
            {
                Restore(folder, record);
                folders.Add(folder);
            }

            var icons = state.Folders.Values.Select(record => record.IconFile).ToList();
            state.Folders.Clear();
            icons.ForEach(icon => DeleteIconIfUnused(state, icon));
        });

        folders.ForEach(ShellNotify.ItemUpdated);
    }

    private static void Restore(string folder, FolderRecord record)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        string iniPath = Path.Combine(folder, DesktopIni);
        if (File.Exists(iniPath))
        {
            var ini = IniDocument.Load(iniPath);
            ini.Remove(Section, IconResourceKey);
            foreach (var (key, value) in record.OriginalValues)
            {
                ini.Set(Section, key, value);
            }

            if (record.CreatedDesktopIni && ini.ValueCount == 0)
            {
                File.SetAttributes(iniPath, FileAttributes.Normal);
                File.Delete(iniPath);
            }
            else
            {
                WriteHidden(iniPath, ini);
            }
        }

        if (record.SetReadOnly)
        {
            File.SetAttributes(folder, File.GetAttributes(folder) & ~FileAttributes.ReadOnly);
        }

        if (record.GitExcludeEntry != null)
        {
            GitExclude.Remove(folder, record.GitExcludeEntry);
        }
    }

    private static FolderRecord NewRecord(IniDocument ini, bool createdDesktopIni)
    {
        var record = new FolderRecord { CreatedDesktopIni = createdDesktopIni };
        foreach (string key in IconKeys)
        {
            if (ini.Get(Section, key) is { } value)
            {
                record.OriginalValues[key] = value;
            }
        }

        return record;
    }

    /// <summary>
    /// Builds a record for a folder that points at one of our icons but has no entry in the state
    /// file, so it can still be updated and reset cleanly.
    /// </summary>
    private static FolderRecord? AdoptUntracked(string folder, IniDocument ini)
    {
        string? resource = ini.Get(Section, IconResourceKey);
        int separator = resource?.LastIndexOf(',') ?? -1;
        if (resource == null || separator < 0 || !resource.StartsWith(AppPaths.IconsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new FolderRecord
        {
            IconFile = resource[..separator],
            CreatedDesktopIni = ini.ValueCount == 1,
            SetReadOnly = true,
            GitExcludeEntry = GitExclude.Find(folder, DesktopIni),
        };
    }

    private static string StoreIcon(byte[] icon)
    {
        string name = Convert.ToHexString(SHA256.HashData(icon))[..16].ToLowerInvariant();
        string path = Path.Combine(AppPaths.IconsDirectory, $"{name}.ico");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(AppPaths.IconsDirectory);
            File.WriteAllBytes(path, icon);
        }

        return path;
    }

    private static void DeleteIconIfUnused(AppState state, string iconFile)
    {
        bool inUse = state.Folders.Values.Any(record => record.IconFile.Equals(iconFile, StringComparison.OrdinalIgnoreCase));
        if (!inUse && iconFile.Length > 0 && File.Exists(iconFile))
        {
            File.Delete(iconFile);
        }
    }

    private static void WriteHidden(string path, IniDocument ini)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }

        ini.Save(path);
        File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
    }

    private static bool MarkReadOnly(string folder)
    {
        var attributes = File.GetAttributes(folder);
        if (attributes.HasFlag(FileAttributes.ReadOnly))
        {
            return false;
        }

        File.SetAttributes(folder, attributes | FileAttributes.ReadOnly);
        return true;
    }
}
