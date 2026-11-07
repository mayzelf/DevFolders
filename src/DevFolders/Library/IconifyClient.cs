using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace DevFolders.Library;

/// <summary>
/// Searches the Iconify icon library and downloads icons as SVG into a local cache,
/// so icons that were used once keep working offline.
/// </summary>
public sealed partial class IconifyClient
{
    private const string Collections = "mdi,material-symbols,tabler,lucide,ph,ri,carbon,fluent,simple-icons,devicon-plain,game-icons";
    private const int ResultLimit = 96;

    private static readonly HttpClient Http = CreateHttpClient();
    private readonly SemaphoreSlim _downloads = new(8);

    public async Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellation)
    {
        string url = $"search?query={Uri.EscapeDataString(query)}&limit={ResultLimit}&prefixes={Collections}";
        var result = await Http.GetFromJsonAsync<SearchResult>(url, cancellation);
        return result?.Icons ?? [];
    }

    public async Task<string> DownloadAsync(string iconId, CancellationToken cancellation)
    {
        var match = IconId().Match(iconId);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid icon id '{iconId}'.", nameof(iconId));
        }

        string prefix = match.Groups["prefix"].Value;
        string name = match.Groups["name"].Value;
        string path = Path.Combine(AppPaths.LibraryDirectory, prefix, $"{name}.svg");
        if (File.Exists(path))
        {
            return path;
        }

        await _downloads.WaitAsync(cancellation);
        try
        {
            string svg = await Http.GetStringAsync($"{prefix}/{name}.svg", cancellation);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, svg, cancellation);
            return path;
        }
        finally
        {
            _downloads.Release();
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { BaseAddress = new Uri("https://api.iconify.design/"), Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DevFolders");
        return client;
    }

    [GeneratedRegex("^(?<prefix>[a-z0-9-]+):(?<name>[a-z0-9-]+)$")]
    private static partial Regex IconId();

    private sealed record SearchResult(string[] Icons);
}
