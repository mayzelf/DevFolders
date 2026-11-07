using System.IO;
using System.Windows;
using DevFolders.Folders;
using DevFolders.Icons;
using DevFolders.Shell;
using DevFolders.Views;

namespace DevFolders;

/// <summary>
/// Entry points for the command line, which is how the Explorer menu calls the app:
/// <c>apply</c>, <c>choose</c>, <c>mark-dev</c> and <c>reset</c> take a folder path;
/// <c>install</c> and <c>uninstall</c> manage the menu. Without arguments the main window opens.
/// </summary>
internal static class Commands
{
    /// <summary>Runs a command. Returns true when a window was opened and the app should keep running.</summary>
    public static bool Run(string[] args)
    {
        if (args.Length == 0)
        {
            return Show(new MainWindow());
        }

        string command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "install":
                ExplorerMenu.Install(Environment.ProcessPath!);
                return false;
            case "uninstall":
                ExplorerMenu.Uninstall();
                FolderIcons.ResetAll();
                return false;
        }

        if (args.Length < 2 || !Directory.Exists(args[1]))
        {
            throw new ArgumentException($"Usage: DevFolders {command} <folder>");
        }

        string folder = AppPaths.NormalizeFolder(args[1]);
        if (!Elevation.CanModify(folder))
        {
            if (Elevation.IsElevated)
            {
                throw new UnauthorizedAccessException();
            }

            Elevation.Relaunch([command, folder]);
            return false;
        }

        return command switch
        {
            "apply" => UseProjectIcon(folder),
            "choose" => Show(new IconPickerWindow(folder, IconCandidateFinder.Find(folder))),
            "mark-dev" => MarkAsDevFolder(folder),
            "reset" => Reset(folder),
            _ => throw new ArgumentException($"Unknown command '{args[0]}'."),
        };
    }

    private static bool UseProjectIcon(string folder)
    {
        var candidates = IconCandidateFinder.Find(folder);
        if (candidates.Count != 1)
        {
            return Show(new IconPickerWindow(folder, candidates));
        }

        var icon = FolderIconRenderer.CreateIcon(FolderStyle.Standard, new ImageBadge(candidates[0].Artwork));
        FolderIcons.Apply(folder, FolderKind.Project, icon);
        return false;
    }

    private static bool MarkAsDevFolder(string folder)
    {
        FolderIcons.Apply(folder, FolderKind.DevFolder, FolderIconRenderer.CreateIcon(FolderStyle.DevFolder, null));

        var projects = ProjectScanner.FindProjects(folder);
        return projects.Count > 0 && Show(new DevFolderWindow(folder, projects));
    }

    private static bool Reset(string folder)
    {
        FolderIcons.Reset(folder);
        return false;
    }

    private static bool Show(Window window)
    {
        Application.Current.MainWindow = window;
        window.Show();
        return true;
    }
}
