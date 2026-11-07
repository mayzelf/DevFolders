using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace DevFolders.Shell;

internal static class Elevation
{
    private const int UserCancelled = 1223;

    public static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public static bool CanModify(string folder)
    {
        try
        {
            File.SetAttributes(folder, File.GetAttributes(folder));
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Relaunch(IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            Process.Start(start);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == UserCancelled)
        {
        }
    }
}
