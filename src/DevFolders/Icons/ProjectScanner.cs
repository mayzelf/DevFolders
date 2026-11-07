using System.IO;

namespace DevFolders.Icons;

public sealed record ProjectEntry(string Folder, IconCandidate? Icon);

public static class ProjectScanner
{
    private static readonly string[] MarkerFiles =
    [
        "package.json", "deno.json", "Cargo.toml", "pyproject.toml", "requirements.txt", "go.mod",
        "pom.xml", "build.gradle", "build.gradle.kts", "settings.gradle", "composer.json", "Gemfile",
        "pubspec.yaml", "CMakeLists.txt", "Makefile", "mix.exs", "Package.swift", "project.godot",
    ];

    private static readonly string[] MarkerPatterns = ["*.sln", "*.slnx", "*.csproj", "*.fsproj", "*.vcxproj", "*.uproject"];

    private static readonly string[] MarkerDirectories = [".git", "ProjectSettings"];

    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
    };

    /// <summary>
    /// Lists the direct subfolders of a dev folder that are projects or contain an icon,
    /// each with its best icon candidate (if any).
    /// </summary>
    public static IReadOnlyList<ProjectEntry> FindProjects(string devFolder)
    {
        var projects = new List<ProjectEntry>();

        foreach (var folder in Directory.EnumerateDirectories(devFolder, "*", Options).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (Path.GetFileName(folder).StartsWith('.'))
            {
                continue;
            }

            var icon = IconCandidateFinder.Find(folder, maxResults: 1).FirstOrDefault();
            if (icon != null || IsProject(folder))
            {
                projects.Add(new ProjectEntry(folder, icon));
            }
        }

        return projects;
    }

    public static bool IsProject(string folder) =>
        MarkerFiles.Any(name => File.Exists(Path.Combine(folder, name)))
        || MarkerDirectories.Any(name => Directory.Exists(Path.Combine(folder, name)))
        || MarkerPatterns.Any(pattern => Directory.EnumerateFiles(folder, pattern, Options).Any());
}
