namespace ColizeumReports;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppDb.Initialize();
        Application.Run(new MainForm());
    }
}
