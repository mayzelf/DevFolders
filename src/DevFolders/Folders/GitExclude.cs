using System.IO;

namespace DevFolders.Folders;

/// <summary>
/// Keeps desktop.ini out of git by adding it to the repository's local .git/info/exclude,
/// which is never committed, instead of touching the project's .gitignore.
/// </summary>
internal static class GitExclude
{
    public static string? Add(string folder, string fileName)
    {
        if (FindRepository(folder) is not (string root, string gitDirectory))
        {
            return null;
        }

        string entry = "/" + Path.GetRelativePath(root, Path.Combine(folder, fileName)).Replace('\\', '/');
        string excludeFile = Path.Combine(gitDirectory, "info", "exclude");
        var lines = File.Exists(excludeFile) ? File.ReadAllLines(excludeFile).ToList() : [];
        if (lines.Contains(entry))
        {
            return null;
        }

        lines.Add(entry);
        Directory.CreateDirectory(Path.GetDirectoryName(excludeFile)!);
        File.WriteAllLines(excludeFile, lines);
        return entry;
    }

    public static void Remove(string folder, string entry)
    {
        if (FindRepository(folder) is not (_, string gitDirectory))
        {
            return;
        }

        string excludeFile = Path.Combine(gitDirectory, "info", "exclude");
        if (File.Exists(excludeFile))
        {
            File.WriteAllLines(excludeFile, File.ReadAllLines(excludeFile).Where(line => line != entry));
        }
    }

    private static (string Root, string GitDirectory)? FindRepository(string folder)
    {
        for (var directory = new DirectoryInfo(folder); directory != null; directory = directory.Parent)
        {
            string git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git))
            {
                return (directory.FullName, git);
            }

            if (File.Exists(git))
            {
                return null;
            }
        }

        return null;
    }
}
