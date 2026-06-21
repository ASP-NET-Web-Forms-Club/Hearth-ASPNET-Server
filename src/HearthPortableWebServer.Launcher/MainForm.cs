using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HearthPortableWebServer.Launcher
{
    public partial class MainForm : Form
    {
        // True once this UI instance has started the server, so we know whether to
        // prompt about it when the window is closed.
        private bool _serverStartedHere;

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
            // Default web root = "<application startup path>\wwwroot".
            txtRoot.Text = Path.Combine(Application.StartupPath, "wwwroot");
            statusTimer.Start();
            RefreshStatus();
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
                _serverStartedHere = true;
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
            MessageBox.Show(this,
                ok ? "Service installed. It will auto-start with Windows." : "Service installation did not complete.",
                "Hearth Portable ASP.NET Web Server", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshStatus();
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            bool ok = ServiceManager.Uninstall();
            MessageBox.Show(this,
                ok ? "Service uninstalled." : "Service removal did not complete.",
                "Hearth Portable ASP.NET Web Server", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshStatus();
        }

        private void btnStartSvc_Click(object sender, EventArgs e)
        {
            ServiceManager.StartService();
            RefreshStatus();
        }

        private void btnStopSvc_Click(object sender, EventArgs e)
        {
            ServiceManager.StopService();
            RefreshStatus();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            // Minimize to the Windows taskbar. The app stays a clickable taskbar button
            // and can be restored at any time; the web server is unaffected.
            this.WindowState = FormWindowState.Minimized;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only prompt when the user is closing the window, this UI started the
            // server, and the server is actually still running.
            if (e.CloseReason == CloseReason.UserClosing
                && _serverStartedHere
                && HostProcessManager.IsRunning(Port))
            {
                DialogResult choice = MessageBox.Show(
                    this,
                    "The web server is currently running. Do you want to stop the web server?\r\n\r\n" +
                    "[Yes]  Stop the web server.\r\n" +
                    "[No]   Let the web server keep running in the background.\r\n\r\n" +
                    "If the server is running in the background, you can always run this " +
                    "program again and press the [Stop] button to stop the server.",
                    "Web server is running",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    HostProcessManager.StopServer(Port);
                }
                // [No] -> fall through and let the window close; the detached server
                // process keeps running in the background.
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

            lblServiceStatus.Text = "Service: " + ServiceManager.ServiceStatusText();
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }
    }
}
