using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevFolders.Folders;
using DevFolders.Icons;
using DevFolders.Shell;

namespace DevFolders.Views;

public sealed record CustomizedFolder(string Folder, ImageSource? Preview);

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Icon = AppIcon.Window;
        Logo.Source = FolderIconRenderer.Render(FolderIcon.DevFolder, 96);
        Refresh();
    }

    private void Refresh()
    {
        bool installed = ExplorerMenu.IsInstalled;
        MenuStatus.Text = installed
            ? "DevFolders is in the folder right-click menu."
            : "DevFolders isn't in the folder right-click menu yet.";
        MenuButton.Content = installed ? "Remove from menu" : "Add to menu";

        var folders = StateStore.Read().Folders
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new CustomizedFolder(entry.Key, BadgeArtwork.TryLoad(entry.Value.IconFile)?.ForSize(64)))
            .ToList();

        FolderList.ItemsSource = folders;
        FoldersHeader.Text = $"Customized folders ({folders.Count})";
        EmptyText.Visibility = folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnToggleMenu(object sender, RoutedEventArgs e)
    {
        if (ExplorerMenu.IsInstalled)
        {
            ExplorerMenu.Uninstall();
        }
        else
        {
            ExplorerMenu.Install(Environment.ProcessPath!);
        }

        Refresh();
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string folder })
        {
            return;
        }

        if (System.IO.Directory.Exists(folder) && !Elevation.CanModify(folder))
        {
            Elevation.Relaunch(["reset", folder]);
            return;
        }

        FolderIcons.Reset(folder);
        Refresh();
    }
}
