using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevFolders.Folders;
using DevFolders.Icons;
using DevFolders.Library;
using Microsoft.Win32;

namespace DevFolders.Views;

public sealed record FoundIcon(string Label, BadgeArtwork Artwork, ImageSource Preview);

public sealed record LibraryIcon(string Id, BadgeArtwork Artwork)
{
    public ImageSource Glyph => Artwork.ForSize(48);
}

public partial class IconPickerWindow : Window
{
    private static readonly Color[] BadgeColors =
    [
        Color.FromRgb(0x53, 0x4A, 0xB7),
        Color.FromRgb(0x1D, 0x9E, 0x75),
        Color.FromRgb(0xD8, 0x5A, 0x30),
        Color.FromRgb(0x18, 0x5F, 0xA5),
        Color.FromRgb(0xD4, 0x53, 0x7E),
        Color.FromRgb(0x2C, 0x2C, 0x2A),
    ];

    private readonly string _folder;
    private readonly ObservableCollection<FoundIcon> _found = [];
    private readonly ObservableCollection<LibraryIcon> _library = [];
    private readonly IconifyClient _iconify = new();
    private CancellationTokenSource? _search;
    private Badge? _badge;

    public IconPickerWindow(string folder, IReadOnlyList<IconCandidate> candidates)
    {
        InitializeComponent();
        _folder = folder;

        string name = Path.GetFileName(folder);
        Title = $"Set icon for {name}";
        Icon = AppIcon.Window;
        SmallPreviewName.Text = name;

        foreach (var candidate in candidates)
        {
            _found.Add(CreateFoundIcon(candidate.RelativePath, candidate.Artwork));
        }

        FoundList.ItemsSource = _found;
        LibraryList.ItemsSource = _library;
        ColorList.ItemsSource = BadgeColors.Select(color => new SolidColorBrush(color)).ToList();
        ColorList.SelectedIndex = 0;

        FoundHeader.Text = candidates.Count > 0
            ? $"Found in this folder ({candidates.Count})"
            : "No icon found in this folder. Search the library or browse for a file.";
        FoundList.Visibility = candidates.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_found.Count > 0)
        {
            FoundList.SelectedIndex = 0;
        }

        Loaded += (_, _) =>
        {
            UpdatePreview();
            if (_found.Count == 0)
            {
                SearchBox.Focus();
            }
        };
        DpiChanged += (_, _) => UpdatePreview();
    }

    private Color SelectedColor => BadgeColors[Math.Max(0, ColorList.SelectedIndex)];

    private void OnFoundSelected(object sender, SelectionChangedEventArgs e)
    {
        if (FoundList.SelectedItem is FoundIcon item)
        {
            LibraryList.SelectedItem = null;
            SetBadge(new ImageBadge(item.Artwork));
        }
    }

    private void OnLibrarySelected(object sender, SelectionChangedEventArgs e)
    {
        if (LibraryList.SelectedItem is LibraryIcon item)
        {
            FoundList.SelectedItem = null;
            SetBadge(new GlyphBadge(item.Artwork, SelectedColor));
        }
    }

    private void OnColorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_badge is GlyphBadge glyph)
        {
            SetBadge(glyph with { Background = SelectedColor });
        }
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _ = SearchAsync();
        }
    }

    private void OnSearch(object sender, RoutedEventArgs e) => _ = SearchAsync();

    /// <summary>
    /// Runs a library search and fills the results as icons finish downloading. Starting a new
    /// search cancels the previous one.
    /// </summary>
    private async Task SearchAsync()
    {
        string query = SearchBox.Text.Trim();
        if (query.Length == 0)
        {
            return;
        }

        _search?.Cancel();
        var search = _search = new CancellationTokenSource();
        _library.Clear();
        LibraryStatus.Text = "Searching...";

        try
        {
            var ids = await _iconify.SearchAsync(query, search.Token);
            LibraryStatus.Text = ids.Count == 0 ? $"No icons found for \"{query}\"." : "";

            var downloads = ids.Select(id => (Id: id, Download: _iconify.DownloadAsync(id, search.Token))).ToList();
            foreach (var (id, download) in downloads)
            {
                string? path = await DownloadOrSkip(download);
                if (search.IsCancellationRequested)
                {
                    return;
                }

                if (path != null && BadgeArtwork.TryLoad(path) is { } artwork)
                {
                    _library.Add(new LibraryIcon(id, artwork));
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpRequestException)
        {
            if (!search.IsCancellationRequested)
            {
                LibraryStatus.Text = "Couldn't reach the icon library. Check your connection and try again.";
            }
        }
    }

    private static async Task<string?> DownloadOrSkip(Task<string> download)
    {
        try
        {
            return await download;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose an icon",
            Filter = "Icons and images|*.ico;*.png;*.svg;*.bmp;*.jpg;*.jpeg;*.gif;*.exe|All files|*.*",
            InitialDirectory = _folder,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (BadgeArtwork.TryLoad(dialog.FileName) is not { } artwork)
        {
            MessageBox.Show(this, "That file couldn't be read as an image.", "DevFolders", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var item = CreateFoundIcon(Path.GetFileName(dialog.FileName), artwork);
        _found.Add(item);
        FoundList.Visibility = Visibility.Visible;
        FoundList.SelectedItem = item;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (_badge == null)
        {
            return;
        }

        FolderIcons.Apply(_folder, FolderKind.Project, FolderIconRenderer.CreateIcon(FolderStyle.Standard, _badge));
        Close();
    }

    private void SetBadge(Badge badge)
    {
        _badge = badge;
        ApplyButton.IsEnabled = true;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        LargePreview.Source = FolderIconRenderer.Render(FolderStyle.Standard, _badge, 256);
        SmallPreview.Source = FolderIconRenderer.Render(FolderStyle.Standard, _badge, (int)Math.Round(16 * scale));
    }

    private static FoundIcon CreateFoundIcon(string label, BadgeArtwork artwork) =>
        new(label, artwork, FolderIconRenderer.Render(FolderStyle.Standard, new ImageBadge(artwork), 128));
}
