using Microsoft.Data.SqlClient;

namespace SqlScriptRunner;

public partial class MainForm : Form
{
    private const string ScriptsFolderName = "Scripts";
    private string ScriptsFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScriptsFolderName);

    public MainForm()
    {
        InitializeComponent();
        EnsureScriptsFolder();
        LoadScriptList();
    }

    private void EnsureScriptsFolder()
    {
        if (!Directory.Exists(ScriptsFolderPath))
            Directory.CreateDirectory(ScriptsFolderPath);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "SQL Script Runner";
        Size = new Size(700, 520);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 450);

        // Server section
        var lblServer = new Label
        {
            Text = "SQL Server:",
            Location = new Point(12, 15),
            AutoSize = true
        };
        var txtServer = new TextBox
        {
            Name = "txtServer",
            Location = new Point(120, 12),
            Size = new Size(280, 23),
            PlaceholderText = "e.g. localhost or . or server\\instance"
        };
        var btnLoadDatabases = new Button
        {
            Name = "btnLoadDatabases",
            Text = "Load Databases",
            Location = new Point(410, 11),
            Size = new Size(120, 26)
        };
        btnLoadDatabases.Click += BtnLoadDatabases_Click;

        // Databases section
        var lblDatabases = new Label
        {
            Text = "Databases:",
            Location = new Point(12, 48),
            AutoSize = true
        };
        var chkSelectAll = new CheckBox
        {
            Name = "chkSelectAll",
            Text = "Select all",
            Location = new Point(120, 46),
            AutoSize = true,
            Checked = true
        };
        chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

        var lstDatabases = new CheckedListBox
        {
            Name = "lstDatabases",
            Location = new Point(12, 72),
            Size = new Size(330, 220),
            CheckOnClick = true
        };
        lstDatabases.ItemCheck += LstDatabases_ItemCheck;

        var btnSelectNone = new Button
        {
            Name = "btnSelectNone",
            Text = "Select None",
            Location = new Point(230, 46),
            Size = new Size(90, 24)
        };
        btnSelectNone.Click += (_, _) => SetAllDatabasesChecked(false);

        // Script section
        var lblScript = new Label
        {
            Text = "T-SQL Script:",
            Location = new Point(360, 48),
            AutoSize = true
        };
        var cboScripts = new ComboBox
        {
            Name = "cboScripts",
            Location = new Point(360, 72),
            Size = new Size(310, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var btnRefreshScripts = new Button
        {
            Text = "Refresh",
            Location = new Point(560, 71),
            Size = new Size(70, 24)
        };
        btnRefreshScripts.Click += (_, _) => LoadScriptList();

        var txtScriptPreview = new TextBox
        {
            Name = "txtScriptPreview",
            Location = new Point(360, 102),
            Size = new Size(310, 190),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            WordWrap = false,
            Font = new Font("Consolas", 9F)
        };
        cboScripts.SelectedIndexChanged += (s, _) =>
        {
            if (cboScripts.SelectedItem is ScriptItem si)
                txtScriptPreview.Text = si.Content;
        };

        // Execute
        var btnExecute = new Button
        {
            Name = "btnExecute",
            Text = "Execute",
            Location = new Point(12, 305),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnExecute.Click += BtnExecute_Click;

        // Log / output
        var lblLog = new Label
        {
            Text = "Output:",
            Location = new Point(12, 348),
            AutoSize = true
        };
        var txtLog = new TextBox
        {
            Name = "txtLog",
            Location = new Point(12, 368),
            Size = new Size(658, 100),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            WordWrap = false,
            Font = new Font("Consolas", 9F),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.Add(lblServer);
        Controls.Add(txtServer);
        Controls.Add(btnLoadDatabases);
        Controls.Add(lblDatabases);
        Controls.Add(chkSelectAll);
        Controls.Add(btnSelectNone);
        Controls.Add(lstDatabases);
        Controls.Add(lblScript);
        Controls.Add(cboScripts);
        Controls.Add(btnRefreshScripts);
        Controls.Add(txtScriptPreview);
        Controls.Add(btnExecute);
        Controls.Add(lblLog);
        Controls.Add(txtLog);

        lstDatabases.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        txtScriptPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        ResumeLayout(false);
    }

    private void ChkSelectAll_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkSelectAll is { Checked: var check })
            SetAllDatabasesChecked(check);
    }

    private void LstDatabases_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // Keep "Select all" in sync after the check is applied
        void SyncSelectAll()
        {
            if (lstDatabases.Items.Count == 0) return;
            var allChecked = true;
            for (int i = 0; i < lstDatabases.Items.Count; i++)
            {
                var willBeChecked = i == e.Index ? e.NewValue == CheckState.Checked : lstDatabases.GetItemChecked(i);
                if (!willBeChecked) { allChecked = false; break; }
            }
            chkSelectAll.CheckedChanged -= ChkSelectAll_CheckedChanged;
            chkSelectAll.Checked = allChecked;
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
        }
        BeginInvoke(SyncSelectAll);
    }

    private void SetAllDatabasesChecked(bool check)
    {
        chkSelectAll.CheckedChanged -= ChkSelectAll_CheckedChanged;
        for (int i = 0; i < lstDatabases.Items.Count; i++)
            lstDatabases.SetItemChecked(i, check);
        chkSelectAll.Checked = check;
        chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
    }

    private void LoadScriptList()
    {
        var scripts = new List<ScriptItem>();
        if (Directory.Exists(ScriptsFolderPath))
        {
            foreach (var path in Directory.GetFiles(ScriptsFolderPath, "*.sql", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    scripts.Add(new ScriptItem(Path.GetFileName(path), path, content));
                }
                catch (Exception ex)
                {
                    Log($"Could not load script {Path.GetFileName(path)}: {ex.Message}");
                }
            }
        }
        var selected = cboScripts.SelectedItem as ScriptItem;
        cboScripts.Items.Clear();
        foreach (var s in scripts)
            cboScripts.Items.Add(s);
        if (scripts.Count > 0)
        {
            var idx = selected != null ? scripts.FindIndex(x => x.FilePath == selected.FilePath) : 0;
            cboScripts.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private async void BtnLoadDatabases_Click(object? sender, EventArgs e)
    {
        var server = txtServer.Text.Trim();
        if (string.IsNullOrEmpty(server))
        {
            MessageBox.Show("Please enter a SQL Server name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        btnLoadDatabases.Enabled = false;
        txtLog.Clear();
        Log($"Connecting to {server}...");
        try
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };
            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            Log("Connected. Fetching databases...");
            var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb') ORDER BY name", conn);
            var names = new List<string>();
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    names.Add(r.GetString(0));
            }
            lstDatabases.Items.Clear();
            foreach (var n in names)
                lstDatabases.Items.Add(n, true);
            chkSelectAll.Checked = true;
            Log($"Found {names.Count} database(s).");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnLoadDatabases.Enabled = true;
        }
    }

    private async void BtnExecute_Click(object? sender, EventArgs e)
    {
        var server = txtServer.Text.Trim();
        if (string.IsNullOrEmpty(server))
        {
            MessageBox.Show("Please enter a SQL Server name.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (cboScripts.SelectedItem is not ScriptItem script)
        {
            MessageBox.Show("Please select a T-SQL script.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var selectedDbs = new List<string>();
        for (int i = 0; i < lstDatabases.Items.Count; i++)
        {
            if (lstDatabases.GetItemChecked(i))
                selectedDbs.Add(lstDatabases.Items[i]!.ToString()!);
        }
        if (selectedDbs.Count == 0)
        {
            MessageBox.Show("Please select at least one database.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnExecute.Enabled = false;
        txtLog.Clear();
        Log($"Executing '{script.DisplayName}' on {selectedDbs.Count} database(s)...");
        var errors = 0;
        foreach (var db in selectedDbs)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = db,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true
                };
                await using var conn = new SqlConnection(builder.ConnectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(script.Content, conn) { CommandTimeout = 300 };
                var rows = await cmd.ExecuteNonQueryAsync();
                Log($"[{db}] OK (rows affected: {rows})");
            }
            catch (Exception ex)
            {
                errors++;
                Log($"[{db}] Error: {ex.Message}");
            }
        }
        Log(errors == 0 ? "Done." : $"Done with {errors} error(s).");
        btnExecute.Enabled = true;
    }

    private void Log(string message)
    {
        txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
    }

    private sealed class ScriptItem
    {
        public string DisplayName { get; }
        public string FilePath { get; }
        public string Content { get; }
        public ScriptItem(string displayName, string filePath, string content)
        {
            DisplayName = displayName;
            FilePath = filePath;
            Content = content;
        }
        public override string ToString() => DisplayName;
    }

    private CheckBox chkSelectAll => (CheckBox)Controls.Find("chkSelectAll", true).First();
    private CheckedListBox lstDatabases => (CheckedListBox)Controls.Find("lstDatabases", true).First();
    private Button btnLoadDatabases => (Button)Controls.Find("btnLoadDatabases", true).First();
    private TextBox txtServer => (TextBox)Controls.Find("txtServer", true).First();
    private ComboBox cboScripts => (ComboBox)Controls.Find("cboScripts", true).First();
    private TextBox txtScriptPreview => (TextBox)Controls.Find("txtScriptPreview", true).First();
    private TextBox txtLog => (TextBox)Controls.Find("txtLog", true).First();
    private Button btnExecute => (Button)Controls.Find("btnExecute", true).First();
}
