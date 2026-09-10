using System;
using System.Drawing;
using System.Windows.Forms;
using HearthPortableWebServer.Manager.Services;

namespace HearthPortableWebServer.Manager.UI
{
    public partial class ServiceSetupForm : Form
    {
        public ServiceSetupForm()
        {
            InitializeComponent();
            Icon icon = MainForm.LoadAppIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        private void ServiceSetupForm_Load(object sender, EventArgs e)
        {
            RefreshStatus();
            timerRefresh.Start();
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            bool installed = ServiceControllerHelper.IsServiceInstalled();
            string status = ServiceControllerHelper.GetServiceStatusText();

            lblCurrentStatus.Text = "Service: " + status;
            if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
            {
                lblCurrentStatus.ForeColor = Color.DarkGreen;
            }
            else if (installed)
            {
                lblCurrentStatus.ForeColor = Color.DarkGoldenrod;
            }
            else
            {
                lblCurrentStatus.ForeColor = Color.Gray;
            }

            btnInstall.Enabled = !installed;
            btnUninstall.Enabled = installed;
            btnStart.Enabled = installed && !string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase);
            btnStop.Enabled = installed && string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase);
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            string err;
            if (ServiceControllerHelper.InstallService(out err))
            {
                MessageBox.Show(this, "Hearth Manager Service installed successfully! It will start automatically when Windows boots.",
                    "Service Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Failed to install service:" + Environment.NewLine + (err ?? "Unknown error"),
                    "Service Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshStatus();
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            string err;
            if (ServiceControllerHelper.UninstallService(out err))
            {
                MessageBox.Show(this, "Hearth Manager Service uninstalled.",
                    "Service Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Failed to uninstall service:" + Environment.NewLine + (err ?? "Unknown error"),
                    "Service Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshStatus();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ServiceControllerHelper.StartService();
            RefreshStatus();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            ServiceControllerHelper.StopService();
            RefreshStatus();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
