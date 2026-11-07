namespace DevFolders.Shell;

internal static class ShellNotify
{
    public static void ItemUpdated(string path) =>
        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_UPDATEITEM, NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSH, path, IntPtr.Zero);

    public static void AssociationsChanged() =>
        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, null, IntPtr.Zero);
}
