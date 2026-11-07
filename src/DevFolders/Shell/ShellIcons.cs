using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DevFolders.Shell;

internal static class ShellIcons
{
    public static BitmapSource? Extract(string file, int index, int size)
    {
        var handles = new IntPtr[1];
        var ids = new uint[1];
        uint count = NativeMethods.PrivateExtractIcons(file, index, size, size, handles, ids, 1, 0);
        if (count == 0 || count == uint.MaxValue || handles[0] == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var bitmap = Imaging.CreateBitmapSourceFromHIcon(handles[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            NativeMethods.DestroyIcon(handles[0]);
        }
    }

    public static BitmapSource? Folder(int size)
    {
        var info = new NativeMethods.SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<NativeMethods.SHSTOCKICONINFO>() };
        if (NativeMethods.SHGetStockIconInfo(NativeMethods.SIID_FOLDER, NativeMethods.SHGSI_ICONLOCATION, ref info) != 0)
        {
            return null;
        }

        return Extract(info.szPath, info.iIcon, size);
    }
}
