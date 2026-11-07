using System.IO;

namespace DevFolders;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevFolders");

    public static string IconsDirectory => Path.Combine(DataDirectory, "icons");

    public static string LibraryDirectory => Path.Combine(DataDirectory, "library");

    public static string StateFile => Path.Combine(DataDirectory, "state.json");

    public static string NormalizeFolder(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
