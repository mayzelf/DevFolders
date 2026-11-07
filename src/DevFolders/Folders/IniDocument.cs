using System.IO;
using System.Text;

namespace DevFolders.Folders;

/// <summary>
/// Minimal line-based INI editor that changes individual keys and leaves everything else in the
/// file untouched. Saved as UTF-16 with BOM, which Explorer reads for desktop.ini files.
/// </summary>
internal sealed class IniDocument
{
    private readonly List<string> _lines;

    public IniDocument(IEnumerable<string>? lines = null) => _lines = lines?.ToList() ?? [];

    public bool HasValues => _lines.Any(IsValueLine);

    public static IniDocument Load(string path) => new(File.ReadAllLines(path));

    public void Save(string path) => File.WriteAllLines(path, _lines, Encoding.Unicode);

    public string? Get(string section, string key)
    {
        int index = FindKey(section, key);
        return index < 0 ? null : _lines[index][(_lines[index].IndexOf('=') + 1)..].Trim();
    }

    public void Set(string section, string key, string value)
    {
        string line = $"{key}={value}";
        int index = FindKey(section, key);
        if (index >= 0)
        {
            _lines[index] = line;
            return;
        }

        var (header, end) = FindSection(section);
        if (header < 0)
        {
            if (_lines.Count > 0 && _lines[^1].Trim().Length > 0)
            {
                _lines.Add("");
            }

            _lines.Add($"[{section}]");
            _lines.Add(line);
            return;
        }

        _lines.Insert(end, line);
    }

    public void Remove(string section, string key)
    {
        int index = FindKey(section, key);
        if (index >= 0)
        {
            _lines.RemoveAt(index);
        }
    }

    private int FindKey(string section, string key)
    {
        var (header, end) = FindSection(section);
        for (int i = header + 1; header >= 0 && i < end; i++)
        {
            int separator = _lines[i].IndexOf('=');
            if (separator > 0 && _lines[i][..separator].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private (int Header, int End) FindSection(string section)
    {
        int header = _lines.FindIndex(line => SectionName(line)?.Equals(section, StringComparison.OrdinalIgnoreCase) == true);
        if (header < 0)
        {
            return (-1, -1);
        }

        int end = header + 1;
        for (int i = header + 1; i < _lines.Count && SectionName(_lines[i]) == null; i++)
        {
            if (_lines[i].Trim().Length > 0)
            {
                end = i + 1;
            }
        }

        return (header, end);
    }

    private static string? SectionName(string line)
    {
        string trimmed = line.Trim();
        return trimmed.Length > 1 && trimmed[0] == '[' && trimmed[^1] == ']' ? trimmed[1..^1].Trim() : null;
    }

    private static bool IsValueLine(string line)
    {
        string trimmed = line.TrimStart();
        return trimmed.Length > 0 && trimmed[0] is not (';' or '#') && trimmed.IndexOf('=') > 0;
    }
}
