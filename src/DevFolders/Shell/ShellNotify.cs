using System.IO;

namespace DevFolders.Shell;

internal static class ShellNotify
{
    public static void ItemUpdated(string path)
    {
        const uint flags = NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSH;
        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_UPDATEITEM, flags, path, IntPtr.Zero);

        if (Path.GetDirectoryName(path) is { } parent)
        {
            NativeMethods.SHChangeNotify(NativeMethods.SHCNE_UPDATEDIR, flags, parent, IntPtr.Zero);
        }
    }

    public static void AssociationsChanged() =>
        NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, null, IntPtr.Zero);
}
