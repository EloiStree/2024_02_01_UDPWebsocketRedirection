using System.Diagnostics;
using System.Runtime.InteropServices;

public class HideWindow
{

    // Import the ShowWindow function from user32.dll
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MINIMIZE = 6;
    public static void MinimizeConsoleWindow()
    {
        IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(handle, SW_MINIMIZE);
    }
}
