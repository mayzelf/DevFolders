using System.Windows.Media;
using DevFolders.Icons;

namespace DevFolders.Views;

internal static class AppIcon
{
    private static ImageSource? _window;

    public static ImageSource Window => _window ??= FolderIconRenderer.Render(FolderStyle.DevFolder, null, 64);
}
