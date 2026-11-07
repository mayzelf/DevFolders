using System.IO;
using System.Security.Cryptography;

namespace DevFolders.Icons;

public sealed record IconCandidate(string Path, string RelativePath, BadgeArtwork Artwork);

/// <summary>
/// Searches a folder for files that make a good badge: favicons, app icons, logos, images named
/// after the folder, and executables named after the folder (for games and apps). Build output and
/// dependency folders are skipped. Results are ranked best first and duplicates are removed.
/// </summary>
public static class IconCandidateFinder
{
    private const int MaxDepth = 4;
    private const int MaxFilesScanned = 50_000;
    private const int MaxFilesLoaded = 40;
    private const long MaxImageBytes = 4 * 1024 * 1024;

    private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", "bin", "obj", "dist", "build", "out", "target", "vendor", "packages",
        "venv", "env", "__pycache__", "coverage", "Library", "Temp", "Logs", "Intermediate",
        "Saved", "DerivedDataCache",
    };

    private static readonly EnumerationOptions Enumeration = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
    };

    public static IReadOnlyList<IconCandidate> Find(string folder, int maxResults = 12)
    {
        string folderKey = Normalize(System.IO.Path.GetFileName(folder));
        var ranked = Scan(folder, folderKey)
            .OrderByDescending(entry => entry.Score)
            .Take(MaxFilesLoaded);

        var results = new List<IconCandidate>();
        var seen = new HashSet<string>();
        foreach (var (file, _) in ranked)
        {
            if (results.Count >= maxResults)
            {
                break;
            }

            var artwork = BadgeArtwork.TryLoad(file.FullName);
            if (artwork is not { IsUsableAsBadge: true } || !seen.Add(Fingerprint(file)))
            {
                continue;
            }

            results.Add(new IconCandidate(file.FullName, System.IO.Path.GetRelativePath(folder, file.FullName), artwork));
        }

        return results;
    }

    private static List<(FileInfo File, int Score)> Scan(string folder, string folderKey)
    {
        var scored = new List<(FileInfo, int)>();
        var pending = new Queue<(DirectoryInfo Directory, int Depth)>();
        pending.Enqueue((new DirectoryInfo(folder), 0));
        int scanned = 0;

        while (pending.Count > 0 && scanned < MaxFilesScanned)
        {
            var (directory, depth) = pending.Dequeue();
            foreach (var entry in directory.EnumerateFileSystemInfos("*", Enumeration))
            {
                if (entry is DirectoryInfo child)
                {
                    if (depth < MaxDepth && !child.Name.StartsWith('.') && !SkippedDirectories.Contains(child.Name))
                    {
                        pending.Enqueue((child, depth + 1));
                    }

                    continue;
                }

                scanned++;
                if (entry is FileInfo file && Score(file, folderKey, depth) is int score)
                {
                    scored.Add((file, score));
                }
            }
        }

        return scored;
    }

    private static int? Score(FileInfo file, string folderKey, int depth)
    {
        string extension = file.Extension.ToLowerInvariant();
        string name = Normalize(System.IO.Path.GetFileNameWithoutExtension(file.Name));
        bool matchesFolder = folderKey.Length >= 3 && name.Contains(folderKey);

        int? score = extension switch
        {
            ".exe" => matchesFolder ? 45 : null,
            ".ico" or ".png" or ".svg" when file.Length <= MaxImageBytes => ImageNameScore(name, extension, matchesFolder, file.Directory?.Name),
            _ => null,
        };

        if (score == null)
        {
            return null;
        }

        int formatBonus = extension switch { ".ico" => 6, ".svg" => 5, ".png" => 3, _ => 0 };
        return score + formatBonus - depth * 3;
    }

    private static int? ImageNameScore(string name, string extension, bool matchesFolder, string? parentName)
    {
        if (name.StartsWith("favicon")) return 100;
        if (matchesFolder) return 90;
        if (name.StartsWith("appicon") || name.StartsWith("icon") || name == "app") return 80;
        if (name.StartsWith("appletouchicon") || name.StartsWith("androidchrome")) return 70;
        if (Normalize(parentName ?? "") is "icons" or "icon") return 65;
        if (name.StartsWith("logo")) return 60;
        if (extension == ".ico") return 50;
        return null;
    }

    private static string Fingerprint(FileInfo file)
    {
        if (file.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return file.FullName;
        }

        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file.FullName)));
    }

    internal static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
