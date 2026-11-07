using System.IO;

namespace DevFolders.Folders;

/// <summary>
/// Keeps desktop.ini out of git by adding it to the repository's local .git/info/exclude,
/// which is never committed, instead of touching the project's .gitignore.
/// </summary>
internal static class GitExclude
{
    /// <summary>Adds the entry and returns it, or returns null when it was already excluded or there is no repository.</summary>
    public static string? Add(string folder, string fileName)
    {
        if (Locate(folder, fileName) is not (string entry, string excludeFile))
        {
            return null;
        }

        var lines = ReadLines(excludeFile);
        if (lines.Contains(entry))
        {
            return null;
        }

        lines.Add(entry);
        Directory.CreateDirectory(Path.GetDirectoryName(excludeFile)!);
        File.WriteAllLines(excludeFile, lines);
        return entry;
    }

    public static string? Find(string folder, string fileName) =>
        Locate(folder, fileName) is (string entry, string excludeFile) && ReadLines(excludeFile).Contains(entry) ? entry : null;

    public static void Remove(string folder, string entry)
    {
        if (Locate(folder, "") is (_, string excludeFile) && File.Exists(excludeFile))
        {
            File.WriteAllLines(excludeFile, ReadLines(excludeFile).Where(line => line != entry));
        }
    }

    private static (string Entry, string ExcludeFile)? Locate(string folder, string fileName)
    {
        for (var directory = new DirectoryInfo(folder); directory != null; directory = directory.Parent)
        {
            string git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git))
            {
                string entry = "/" + Path.GetRelativePath(directory.FullName, Path.Combine(folder, fileName)).Replace('\\', '/');
                return (entry, Path.Combine(git, "info", "exclude"));
            }

            if (File.Exists(git))
            {
                return null;
            }
        }

        return null;
    }

    private static List<string> ReadLines(string file) => File.Exists(file) ? File.ReadAllLines(file).ToList() : [];
}
