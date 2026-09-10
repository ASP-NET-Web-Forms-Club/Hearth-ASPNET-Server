using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HearthPortableWebServer.Manager.Models;
using HearthPortableWebServer.Manager.Services;

namespace HearthPortableWebServer.Manager.UI
{
    public partial class MainForm : Form
    {
        private ManagerConfig _config;
        private SiteEntry _selectedSite;
        private bool _isClosingExplicit;
        private System.Windows.Forms.Timer _windowSaveTimer;
        private bool _isLoaded;

        public MainForm()
        {
            InitializeComponent();

            Icon appIcon = LoadAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
                if (this.trayIcon != null)
                {
                    this.trayIcon.Icon = appIcon;
                }
            }

            _windowSaveTimer = new System.Windows.Forms.Timer();
            _windowSaveTimer.Interval = 3000;
            _windowSaveTimer.Tick += WindowSaveTimer_Tick;

            LoadWindowLayout();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadWindowLayout();

            _config = ManagerConfig.Load();
            PopulateSiteList();

            if (lvSites.Items.Count > 0)
            {
                lvSites.Items[0].Selected = true;
            }
            else
            {
                UpdateDetailsPanel(null);
            }

            statusTimer.Start();
            RefreshAllStatus();

            _isLoaded = true;
        }

        private void PopulateSiteList()
        {
            lvSites.BeginUpdate();
            lvSites.Items.Clear();

            foreach (SiteEntry site in _config.Sites)
            {
                ListViewItem item = CreateListViewItem(site);
                lvSites.Items.Add(item);
            }

            lvSites.EndUpdate();
        }

        private ListViewItem CreateListViewItem(SiteEntry site)
        {
            bool running = ProcessSupervisor.IsRunning(site.Port);
            ListViewItem item = new ListViewItem(running ? "● Running" : "○ Stopped");
            item.Tag = site;
            item.ForeColor = running ? Color.DarkGreen : Color.Maroon;
            item.SubItems.Add(site.Name);
            item.SubItems.Add(site.Port.ToString());
            item.SubItems.Add(site.AutoStart ? "Yes" : "No");
            item.SubItems.Add(site.Root);
            return item;
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            RefreshAllStatus();
        }

        private void RefreshAllStatus()
        {
            int runningCount = 0;
            int stoppedCount = 0;

            for (int i = 0; i < lvSites.Items.Count; i++)
            {
                ListViewItem item = lvSites.Items[i];
                SiteEntry site = item.Tag as SiteEntry;
                if (site == null) continue;

                bool isRunning = ProcessSupervisor.IsRunning(site.Port);
                string statusText = isRunning ? "● Running" : "○ Stopped";
                Color color = isRunning ? Color.DarkGreen : Color.Maroon;

                if (item.Text != statusText)
                {
                    item.Text = statusText;
                    item.ForeColor = color;
                }

                if (isRunning) runningCount++;
                else stoppedCount++;
            }

            lblStatusSummary.Text = string.Format("Total Sites: {0} | Running: {1} | Stopped: {2}",
                _config.Sites.Count, runningCount, stoppedCount);

            lblServiceStatus.Text = "Manager Service: " + ServiceControllerHelper.GetServiceStatusText();

            UpdateActionButtonsState();
        }

        private void UpdateActionButtonsState()
        {
            if (_selectedSite == null)
            {
                grpDetails.Enabled = false;
                grpActions.Enabled = false;
                return;
            }

            grpDetails.Enabled = true;
            grpActions.Enabled = true;

            bool isRunning = ProcessSupervisor.IsRunning(_selectedSite.Port);
            btnStart.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
            numPort.Enabled = !isRunning;
            txtRoot.Enabled = !isRunning;
            btnBrowseRoot.Enabled = !isRunning;
        }

        private void lvSites_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvSites.SelectedItems.Count > 0)
            {
                _selectedSite = lvSites.SelectedItems[0].Tag as SiteEntry;
            }
            else
            {
                _selectedSite = null;
            }
            UpdateDetailsPanel(_selectedSite);
            UpdateActionButtonsState();
        }

        private void UpdateDetailsPanel(SiteEntry site)
        {
            if (site == null)
            {
                txtName.Text = string.Empty;
                numPort.Value = 8080;
                txtRoot.Text = string.Empty;
                chkAutoStart.Checked = false;
                return;
            }

            txtName.Text = site.Name;
            numPort.Value = Math.Max(numPort.Minimum, Math.Min(numPort.Maximum, site.Port));
            txtRoot.Text = site.Root;
            chkAutoStart.Checked = site.AutoStart;
        }

        private void btnAddSite_Click(object sender, EventArgs e)
        {
            int nextPort = 8080;
            while (true)
            {
                bool portTaken = false;
                foreach (SiteEntry s in _config.Sites)
                {
                    if (s.Port == nextPort)
                    {
                        portTaken = true;
                        break;
                    }
                }
                if (!portTaken) break;
                nextPort++;
            }

            SiteEntry newSite = new SiteEntry("Website " + nextPort, nextPort, "wwwroot", false);
            _config.Sites.Add(newSite);
            _config.Save();

            ListViewItem item = CreateListViewItem(newSite);
            lvSites.Items.Add(item);
            item.Selected = true;
            txtName.Focus();
            txtName.SelectAll();
        }

        private void btnBrowseRoot_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select website application root directory";
                string currentAbs = ManagerConfig.ResolveRootAbsolute(txtRoot.Text.Trim());
                if (Directory.Exists(currentAbs))
                {
                    dialog.SelectedPath = currentAbs;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtRoot.Text = ManagerConfig.RelativizePath(dialog.SelectedPath);
                }
            }
        }

        private void btnSaveSite_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;

            string name = txtName.Text.Trim();
            int port = (int)numPort.Value;
            string root = txtRoot.Text.Trim();
            bool autoStart = chkAutoStart.Checked;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "Site name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if port conflicts with another site entry
            foreach (SiteEntry s in _config.Sites)
            {
                if (s != _selectedSite && s.Port == port)
                {
                    MessageBox.Show(this, "Port " + port + " is already assigned to site '" + s.Name + "'. Each site must have a unique port.",
                        "Port Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _selectedSite.Name = name;
            _selectedSite.Port = port;
            _selectedSite.Root = root;
            _selectedSite.AutoStart = autoStart;
            _config.Save();

            if (lvSites.SelectedItems.Count > 0)
            {
                ListViewItem item = lvSites.SelectedItems[0];
                item.SubItems[1].Text = name;
                item.SubItems[2].Text = port.ToString();
                item.SubItems[3].Text = autoStart ? "Yes" : "No";
                item.SubItems[4].Text = root;
            }

            MessageBox.Show(this, "Site configuration saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAllStatus();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;

            string err;
            if (!ProcessSupervisor.StartSite(_selectedSite, out err))
            {
                MessageBox.Show(this, "Failed to start site '" + _selectedSite.Name + "':" + Environment.NewLine + (err ?? "Unknown error"),
                    "Start Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshAllStatus();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;
            ProcessSupervisor.StopSite(_selectedSite.Port);
            RefreshAllStatus();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;
            if (!ProcessSupervisor.IsRunning(_selectedSite.Port))
            {
                if (MessageBox.Show(this, "The server is currently stopped. Open browser anyway?",
                    "Server Stopped", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }
            ProcessSupervisor.OpenBrowser(_selectedSite.Port);
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;
            ProcessSupervisor.OpenFolder(_selectedSite.Root);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedSite == null) return;

            if (ProcessSupervisor.IsRunning(_selectedSite.Port))
            {
                MessageBox.Show(this, "Please stop the server before deleting this site entry.",
                    "Site is Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "Are you sure you want to remove site entry '" + _selectedSite.Name + "' (Port " + _selectedSite.Port + ")?" + Environment.NewLine + Environment.NewLine + "(Note: files on disk will NOT be deleted)",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _config.Sites.Remove(_selectedSite);
                _config.Save();

                if (lvSites.SelectedItems.Count > 0)
                {
                    lvSites.Items.Remove(lvSites.SelectedItems[0]);
                }

                if (lvSites.Items.Count > 0)
                {
                    lvSites.Items[0].Selected = true;
                }
                else
                {
                    _selectedSite = null;
                    UpdateDetailsPanel(null);
                    UpdateActionButtonsState();
                }

                RefreshAllStatus();
            }
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            ProcessSupervisor.StartAll(_config.Sites);
            RefreshAllStatus();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            ProcessSupervisor.StopAll(_config.Sites);
            RefreshAllStatus();
        }

        private void btnServiceSetup_Click(object sender, EventArgs e)
        {
            using (ServiceSetupForm form = new ServiceSetupForm())
            {
                form.ShowDialog(this);
            }
            RefreshAllStatus();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshAllStatus();
        }

        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void menuOpenDashboard_Click(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            _isClosingExplicit = true;
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isClosingExplicit && e.CloseReason == CloseReason.UserClosing)
            {
                // Check if any servers are running
                bool anyRunning = false;
                foreach (SiteEntry s in _config.Sites)
                {
                    if (ProcessSupervisor.IsRunning(s.Port))
                    {
                        anyRunning = true;
                        break;
                    }
                }

                if (anyRunning)
                {
                    string message = "One or more web server instances are currently running." + Environment.NewLine + Environment.NewLine +
                        "[Yes]  Minimize to System Tray and keep servers running." + Environment.NewLine +
                        "[No]   Stop all running servers and exit completely." + Environment.NewLine +
                        "[Cancel] Stay on this window.";

                    DialogResult choice = MessageBox.Show(this,
                        message,
                        "Servers Running", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (choice == DialogResult.Yes)
                    {
                        e.Cancel = true;
                        this.Hide();
                        trayIcon.ShowBalloonTip(2000, "Hearth Manager", "Servers are still running in the background.", ToolTipIcon.Info);
                        return;
                    }
                    else if (choice == DialogResult.No)
                    {
                        ProcessSupervisor.StopAll(_config.Sites);
                    }
                    else
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            if (_windowSaveTimer != null)
            {
                if (_windowSaveTimer.Enabled)
                {
                    _windowSaveTimer.Stop();
                    SaveWindowLayout();
                }
                _windowSaveTimer.Dispose();
                _windowSaveTimer = null;
            }

            trayIcon.Visible = false;
        }

        private void WindowSaveTimer_Tick(object sender, EventArgs e)
        {
            _windowSaveTimer.Stop();
            SaveWindowLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            TriggerWindowSaveCountdown();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            TriggerWindowSaveCountdown();
        }

        private void TriggerWindowSaveCountdown()
        {
            if (!_isLoaded) return;
            if (this.WindowState == FormWindowState.Minimized) return;

            // When user continues adjusting location/size, cancel countdown and re-launch 3s timer
            if (_windowSaveTimer != null)
            {
                _windowSaveTimer.Stop();
                _windowSaveTimer.Start();
            }
        }

        public static string GetAppDirectory()
        {
            try
            {
                string asmLocation = typeof(MainForm).Assembly.Location;
                if (!string.IsNullOrEmpty(asmLocation))
                {
                    string dir = Path.GetDirectoryName(asmLocation);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        return dir;
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(Application.StartupPath))
            {
                return Application.StartupPath;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string WindowLayoutFilePath()
        {
            return Path.Combine(GetAppDirectory(), "window_layout.txt");
        }

        private void LoadWindowLayout()
        {
            try
            {
                string path = WindowLayoutFilePath();
                if (!File.Exists(path)) return;

                string[] lines = File.ReadAllLines(path);
                int x = this.Location.X;
                int y = this.Location.Y;
                int width = this.Width;
                int height = this.Height;
                FormWindowState state = FormWindowState.Normal;
                bool hasX = false, hasY = false, hasW = false, hasH = false;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    int parsed;
                    if (string.Equals(key, "X", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out parsed))
                    {
                        x = parsed;
                        hasX = true;
                    }
                    else if (string.Equals(key, "Y", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out parsed))
                    {
                        y = parsed;
                        hasY = true;
                    }
                    else if (string.Equals(key, "Width", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out parsed))
                    {
                        width = Math.Max(this.MinimumSize.Width, parsed);
                        hasW = true;
                    }
                    else if (string.Equals(key, "Height", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out parsed))
                    {
                        height = Math.Max(this.MinimumSize.Height, parsed);
                        hasH = true;
                    }
                    else if (string.Equals(key, "WindowState", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(val, "Maximized", StringComparison.OrdinalIgnoreCase))
                        {
                            state = FormWindowState.Maximized;
                        }
                    }
                }

                if (hasX && hasY && hasW && hasH)
                {
                    Rectangle targetRect = new Rectangle(x, y, width, height);
                    bool isVisible = false;
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        if (screen.WorkingArea.IntersectsWith(targetRect))
                        {
                            isVisible = true;
                            break;
                        }
                    }

                    if (isVisible)
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(x, y);
                        this.Size = new Size(width, height);
                    }
                }

                if (state == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Maximized;
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private void SaveWindowLayout()
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized) return;

                int x, y, width, height;
                string state = (this.WindowState == FormWindowState.Maximized) ? "Maximized" : "Normal";

                if (this.WindowState == FormWindowState.Maximized)
                {
                    Rectangle restore = this.RestoreBounds;
                    x = restore.X;
                    y = restore.Y;
                    width = restore.Width;
                    height = restore.Height;
                }
                else
                {
                    x = this.Location.X;
                    y = this.Location.Y;
                    width = this.Size.Width;
                    height = this.Size.Height;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("# Hearth Multi-Site Manager - Window Layout State");
                sb.AppendLine("# Automatically saved after 3 seconds of position/size inactivity.");
                sb.AppendLine("X=" + x);
                sb.AppendLine("Y=" + y);
                sb.AppendLine("Width=" + width);
                sb.AppendLine("Height=" + height);
                sb.AppendLine("WindowState=" + state);

                File.WriteAllText(WindowLayoutFilePath(), sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch
            {
                // Best effort
            }
        }

        public static Icon LoadAppIcon()
        {
            try
            {
                string dir = GetAppDirectory();

                string iconPath = Path.Combine(dir, "hearth.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }

                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hearth.ico");
                if (File.Exists(fallbackPath))
                {
                    return new Icon(fallbackPath);
                }

                string exePath = Application.ExecutablePath;
                if (File.Exists(exePath))
                {
                    Icon extracted = Icon.ExtractAssociatedIcon(exePath);
                    if (extracted != null) return extracted;
                }
            }
            catch { }
            return null;
        }
    }
}
