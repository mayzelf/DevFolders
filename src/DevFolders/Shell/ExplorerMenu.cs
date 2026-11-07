using Microsoft.Win32;

namespace DevFolders.Shell;

/// <summary>
/// Registers the DevFolders submenu on the folder right-click menu for the current user.
/// </summary>
public static class ExplorerMenu
{
    private const string MenuKey = @"Software\Classes\Directory\shell\DevFolders";
    private const int SeparatorBefore = 0x20;

    private static readonly (string Key, string Label, string Command, bool Separator)[] Items =
    [
        ("1-apply", "Use project icon", "apply", false),
        ("2-choose", "Choose icon...", "choose", false),
        ("3-mark-dev", "Mark as dev folder", "mark-dev", false),
        ("4-reset", "Reset to default", "reset", true),
    ];

    public static bool IsInstalled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(MenuKey);
            return key != null;
        }
    }

    public static void Install(string executable)
    {
        Registry.CurrentUser.DeleteSubKeyTree(MenuKey, throwOnMissingSubKey: false);

        using (var menu = Registry.CurrentUser.CreateSubKey(MenuKey))
        {
            menu.SetValue("MUIVerb", "DevFolders");
            menu.SetValue("Icon", $"{executable},0");
            menu.SetValue("SubCommands", "");

            foreach (var item in Items)
            {
                using var verb = menu.CreateSubKey($@"shell\{item.Key}");
                verb.SetValue("MUIVerb", item.Label);
                if (item.Separator)
                {
                    verb.SetValue("CommandFlags", SeparatorBefore, RegistryValueKind.DWord);
                }

                using var command = verb.CreateSubKey("command");
                command.SetValue("", $"\"{executable}\" {item.Command} \"%1\"");
            }
        }

        ShellNotify.AssociationsChanged();
    }

    public static void Uninstall()
    {
        Registry.CurrentUser.DeleteSubKeyTree(MenuKey, throwOnMissingSubKey: false);
        ShellNotify.AssociationsChanged();
    }
}
