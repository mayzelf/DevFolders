using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DevFolders.Folders;
using DevFolders.Icons;

namespace DevFolders.Views;

public sealed class ProjectRow : INotifyPropertyChanged
{
    private bool _isChecked;

    public ProjectRow(ProjectEntry entry)
    {
        Entry = entry;
        Name = Path.GetFileName(entry.Folder);
        Detail = entry.Icon?.RelativePath ?? "No icon found";
        Preview = FolderIconRenderer.Render(FolderIcon.WithBadge(entry.Icon is { } icon ? new ImageBadge(icon.Artwork) : null), 64);
        _isChecked = HasIcon;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProjectEntry Entry { get; }

    public string Name { get; }

    public string Detail { get; }

    public ImageSource Preview { get; }

    public bool HasIcon => Entry.Icon != null;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }
}

public partial class DevFolderWindow : Window
{
    private readonly List<ProjectRow> _rows;

    public DevFolderWindow(string folder, IReadOnlyList<ProjectEntry> projects)
    {
        InitializeComponent();
        Icon = AppIcon.Window;
        HeaderText.Text = $"{Path.GetFileName(folder)} is now a dev folder";
        FolderPreview.Source = FolderIconRenderer.Render(FolderIcon.DevFolder, 128);

        _rows = projects.Select(project => new ProjectRow(project)).ToList();
        _rows.ForEach(row => row.PropertyChanged += (_, _) => UpdateApplyButton());
        ProjectList.ItemsSource = _rows;
        UpdateApplyButton();
    }

    private void UpdateApplyButton()
    {
        int count = _rows.Count(row => row.IsChecked);
        ApplyButton.Content = count == 1 ? "Apply to 1 project" : $"Apply to {count} projects";
        ApplyButton.IsEnabled = count > 0;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows.Where(row => row is { IsChecked: true, Entry.Icon: not null }))
        {
            var icon = FolderIconRenderer.CreateIcon(FolderIcon.WithBadge(new ImageBadge(row.Entry.Icon!.Artwork)));
            FolderIcons.Apply(row.Entry.Folder, FolderKind.Project, icon);
        }

        Close();
    }
}
