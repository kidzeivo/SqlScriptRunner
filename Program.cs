namespace SqlScriptRunner;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{ex.Message}\n\n{ex.StackTrace}",
                "SqlScriptRunner failed to start",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            throw;
        }
    }
}
