using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HearthPortableWebServer.Launcher
{
    public partial class MainForm : Form
    {
        // Persisted launcher state (port, web root, service name) stored next to the exe.
        private LauncherSettings _settings = new LauncherSettings();

        public MainForm()
        {
            InitializeComponent();
        }

        private int Port
        {
            get { return (int)numPort.Value; }
        }

        private string Root
        {
            get { return txtRoot.Text.Trim(); }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Restore persisted state. Port and web root come from Settings.txt next to
            // the exe; the stored root is relative and resolved to an absolute path here.
            _settings = LauncherSettings.Load();
            numPort.Value = ClampToRange(_settings.Port, (int)numPort.Minimum, (int)numPort.Maximum);
            txtRoot.Text = _settings.ResolveRootAbsolute();

            statusTimer.Start();
            RefreshStatus();
        }

        private static int ClampToRange(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the web application root folder";
                if (Directory.Exists(Root))
                {
                    dialog.SelectedPath = Root;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtRoot.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Root))
                {
                    MessageBox.Show(this, "Please specify a web root folder.", "Hearth Portable ASP.NET Web Server",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                HostProcessManager.StartServer(Port, Root);

                // Persist the port and (relativized) web root the user actually launched
                // with, so reopening the launcher re-attaches to this same server.
                _settings.Port = Port;
                _settings.SetRootFromAbsolute(Root);
                _settings.Save();

                SetStatus("Starting server on port " + Port + " ...", Color.DarkGoldenrod);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to start server:\r\n" + ex.Message, "Hearth Portable ASP.NET Web Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshStatus();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (!HostProcessManager.StopServer(Port))
            {
                MessageBox.Show(this, "No running server found on port " + Port + ".", "Hearth Portable ASP.NET Web Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SetStatus("Stop signal sent ...", Color.DarkGoldenrod);
            }
            RefreshStatus();
        }

        private void btnBrowser_Click(object sender, EventArgs e)
        {
            if (!HostProcessManager.IsRunning(Port))
            {
                if (MessageBox.Show(this,
                        "The server does not appear to be running. Open the browser anyway?",
                        "Hearth Portable ASP.NET Web Server", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                HostProcessManager.OpenBrowser(Port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to open browser:\r\n" + ex.Message, "Hearth Portable ASP.NET Web Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Root))
            {
                MessageBox.Show(this, "Please specify a web root folder.", "Hearth Portable ASP.NET Web Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = ServiceManager.Install(Port, Root);
            if (ok)
            {
                // Record the port, web root, and service name used for this install so a
                // later uninstall (Stage 2: port-based names) can target the right one.
                _settings.Port = Port;
                _settings.SetRootFromAbsolute(Root);
                _settings.ServiceName = HearthPortableWebServer.Common.IpcNames.ServiceName(Port);
                _settings.Save();
            }
            MessageBox.Show(this,
                ok ? "Service installed. It will auto-start with Windows." : "Service installation did not complete.",
                "Hearth Portable ASP.NET Web Server", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshStatus();
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            bool ok = ServiceManager.Uninstall(Port);
            if (ok)
            {
                // Clear the recorded service name now that it's gone.
                _settings.ServiceName = string.Empty;
                _settings.Save();
            }
            MessageBox.Show(this,
                ok ? "Service uninstalled." : "Service removal did not complete.",
                "Hearth Portable ASP.NET Web Server", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshStatus();
        }

        private void btnStartSvc_Click(object sender, EventArgs e)
        {
            ServiceManager.StartService(Port);
            RefreshStatus();
        }

        private void btnStopSvc_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService(Port);
            RefreshStatus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // The foreground server is tied to this launcher. If it is running when the
            // user closes the window, offer to stop it or to cancel the close. For a
            // server that should outlive the launcher, the user installs it as a Windows
            // Service instead (see the service panel).
            if (e.CloseReason == CloseReason.UserClosing
                && HostProcessManager.IsRunning(Port))
            {
                DialogResult choice = MessageBox.Show(
                    this,
                    "The web server is currently running. Closing the launcher will stop it.\r\n\r\n" +
                    "[Yes]  Stop the web server and close.\r\n" +
                    "[No]   Do not close the launcher.\r\n\r\n" +
                    "To keep a server running in the background after closing, install it " +
                    "as a Windows Service from the service panel instead.",
                    "Web server is running",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (choice != DialogResult.Yes)
                {
                    // Abort the close entirely; keep the launcher open.
                    e.Cancel = true;
                    base.OnFormClosing(e);
                    return;
                }

                // [Yes] -> stop the server, then let the window close.
                HostProcessManager.StopServer(Port);
            }

            base.OnFormClosing(e);
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool running = HostProcessManager.IsRunning(Port);
            if (running)
            {
                SetStatus("Running  -  http://localhost:" + Port + "/", Color.Green);
            }
            else
            {
                SetStatus("Stopped", Color.Maroon);
            }

            btnStart.Enabled = !running;
            btnStop.Enabled = running;

            // Freeze the port and web root while a server is running on this port: they
            // must not change out from under a live server (that is what caused the
            // launcher to "lose track" of the port). They unfreeze automatically once
            // the server stops.
            numPort.Enabled = !running;
            txtRoot.Enabled = !running;
            btnBrowse.Enabled = !running;

            lblServiceStatus.Text = "Service: " + ServiceManager.ServiceStatusText(Port);
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }
    }
}