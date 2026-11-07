using System.Windows;
using System.Windows.Threading;

namespace DevFolders;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        try
        {
            if (!Commands.Run(e.Args))
            {
                Shutdown();
            }
        }
        catch (Exception exception)
        {
            ShowError(exception);
            Shutdown(1);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ShowError(e.Exception);
        Shutdown(1);
    }

    private static void ShowError(Exception exception)
    {
        string message = exception is UnauthorizedAccessException
            ? "Windows didn't allow changes to this folder."
            : exception.Message;
        MessageBox.Show(message, "DevFolders", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
