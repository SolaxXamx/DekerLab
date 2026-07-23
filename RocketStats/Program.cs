using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace RocketStats;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();
        SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
        using var form = new RocketStatsForm();
        Application.Run(form);
    }
    [DllImport("shcore.dll")]
    private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
    private enum PROCESS_DPI_AWARENESS { PROCESS_DPI_UNAWARE = 0, PROCESS_SYSTEM_DPI_AWARE = 1, PROCESS_PER_MONITOR_DPI_AWARE = 2 }
}

public static class ApplicationConfiguration
{
    public static void Initialize()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.ThreadException += (sender, e) => MessageBox.Show(e.Exception.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
