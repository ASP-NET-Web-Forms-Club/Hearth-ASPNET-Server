using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;
using HearthPortableWebServer.Manager.Services;

namespace HearthPortableWebServer.Manager.UI
{
    public partial class SystemPathSetupForm : Form
    {
        private List<RuntimeItem> _runtimes;
        private List<OrphanPathEntry> _orphans;
        private bool _isElevated;
        private bool _isInternalCheckChange;

        public SystemPathSetupForm()
        {
            InitializeComponent();
            Icon icon = MainForm.LoadAppIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        private void SystemPathSetupForm_Load(object sender, EventArgs e)
        {
            _isElevated = CheckIsElevated();
            if (_isElevated)
            {
                rbSystemPath.Checked = true;
            }
            else
            {
                rbUserPath.Checked = true;
                rbSystemPath.Text += " (Requires Run as Administrator)";
            }

            ReloadRuntimes();
        }

        private static bool CheckIsElevated()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private void ReloadRuntimes()
        {
            _runtimes = PathEnvironmentHelper.DetectBundledRuntimes();
            _orphans = PathEnvironmentHelper.DetectOrphanHearthPaths();

            bool hasBundlesOnDisk = PathEnvironmentHelper.HasAnyBundledRuntimesOnDisk();

            // 1. Orphan / Moved Folder Banner
            if (_orphans.Count > 0)
            {
                pnlOrphanWarning.Visible = true;
                lblOrphanText.Text = string.Format(
                    "⚠ Moved / Outdated Hearth paths detected ({0} entry in PATH). These point to an old or non-existent folder.",
                    _orphans.Count);
                pnlNoBundlesInfo.Visible = false;
            }
            else
            {
                pnlOrphanWarning.Visible = false;
                // 2. No-bundle ASP.NET Edition Banner
                pnlNoBundlesInfo.Visible = !hasBundlesOnDisk;
            }

            _isInternalCheckChange = true;
            lvRuntimes.BeginUpdate();
            lvRuntimes.Items.Clear();

            int installableCount = 0;
            int hearthInstalledCount = 0;

            foreach (RuntimeItem r in _runtimes)
            {
                ListViewItem item = new ListViewItem(r.Name);
                item.Tag = r;

                string statusText;
                Color statusColor;

                if (!r.ExistsInHearthBundle)
                {
                    statusText = "Not included in this edition (ASP.NET Only)";
                    statusColor = Color.Gray;
                    item.Checked = false;
                }
                else if (r.IsHearthInSystemPath && r.IsHearthInUserPath)
                {
                    statusText = "Configured (System & User PATH)";
                    statusColor = Color.DarkGreen;
                    item.Checked = false;
                    hearthInstalledCount++;
                }
                else if (r.IsHearthInSystemPath)
                {
                    statusText = "Configured (System PATH)";
                    statusColor = Color.DarkGreen;
                    item.Checked = false;
                    hearthInstalledCount++;
                }
                else if (r.IsHearthInUserPath)
                {
                    statusText = "Configured (User PATH)";
                    statusColor = Color.DarkGreen;
                    item.Checked = false;
                    hearthInstalledCount++;
                }
                else if (r.HasExternalInstallation)
                {
                    statusText = "Already Present: " + r.ExternalDetectedPath;
                    statusColor = Color.SteelBlue;
                    item.Checked = false; // Intentionally unchecked & disabled from adding
                }
                else
                {
                    statusText = "Not in PATH (Ready to install)";
                    statusColor = Color.DarkOrange;
                    item.Checked = true; // Recommend for installation
                    installableCount++;
                }

                item.ForeColor = statusColor;
                item.SubItems.Add(statusText);
                item.SubItems.Add(r.BundledDirectoryPath);
                item.ToolTipText = "Bundled Path: " + r.BundledDirectoryPath + 
                    (r.HasExternalInstallation ? "\r\nExternal Detected: " + r.ExternalDetectedPath : "");

                lvRuntimes.Items.Add(item);
            }

            lvRuntimes.EndUpdate();
            _isInternalCheckChange = false;

            btnAddSelected.Enabled = (installableCount > 0);
            btnRemoveSelected.Enabled = (hearthInstalledCount > 0);
        }

        private void btnCleanOrphans_Click(object sender, EventArgs e)
        {
            if (_orphans == null || _orphans.Count == 0) return;

            string listStr = string.Join("\r\n• ", _orphans.ConvertAll(o => o.RawPath + " (" + o.Reason + ")"));
            DialogResult dr = MessageBox.Show(this,
                "Clean the following outdated/broken Hearth paths from your system PATH?\r\n\r\n• " + listStr +
                "\r\n\r\nYour current Hearth folder runtimes and any external tools will remain intact.",
                "Clean Outdated Hearth Paths", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            string err;
            if (PathEnvironmentHelper.CleanOrphanPaths(_orphans, out err))
            {
                MessageBox.Show(this, "Outdated Hearth paths were cleaned successfully!", "Cleaned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Failed to clean some paths: " + (err ?? "Unknown error"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ReloadRuntimes();
        }

        private void lvRuntimes_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_isInternalCheckChange) return;

            ListViewItem item = lvRuntimes.Items[e.Index];
            RuntimeItem r = item.Tag as RuntimeItem;
            if (r == null) return;

            // Prevent user from checking if not in bundle, already externally present, or already installed
            if (e.NewValue == CheckState.Checked)
            {
                if (!r.ExistsInHearthBundle)
                {
                    e.NewValue = CheckState.Unchecked;
                    MessageBox.Show(this,
                        "This runtime is not bundled with this edition of Hearth.\r\n\r\nIf you need bundled runtimes, please download the 'All-Bundled' edition of Hearth Portable Web Server.",
                        "Not Bundled in this Edition", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (r.HasExternalInstallation && !r.IsHearthInstalledInAnyPath)
                {
                    e.NewValue = CheckState.Unchecked;
                    MessageBox.Show(this,
                        string.Format("'{0}' is already installed and available on this computer at:\r\n\r\n{1}\r\n\r\nInstalling Hearth's duplicate runtime into PATH is not recommended and would cause command conflicts.",
                        r.CommandName, r.ExternalDetectedPath),
                        "Runtime Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (r.IsHearthInstalledInAnyPath)
                {
                    e.NewValue = CheckState.Unchecked;
                    MessageBox.Show(this,
                        "Hearth's bundled path for this runtime is already installed in your PATH.",
                        "Already Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private EnvironmentVariableTarget GetSelectedTarget()
        {
            return rbSystemPath.Checked ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
        }

        private void btnAddSelected_Click(object sender, EventArgs e)
        {
            List<string> selectedDirs = new List<string>();
            foreach (ListViewItem item in lvRuntimes.Items)
            {
                if (item.Checked)
                {
                    RuntimeItem r = item.Tag as RuntimeItem;
                    if (r != null && r.ExistsInHearthBundle && !r.HasExternalInstallation && !r.IsHearthInstalledInAnyPath)
                    {
                        selectedDirs.Add(r.BundledDirectoryPath);
                    }
                }
            }

            if (selectedDirs.Count == 0)
            {
                MessageBox.Show(this, "No new runtimes selected to install.\r\n(Runtimes already installed, detected on system, or not bundled are skipped).", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EnvironmentVariableTarget target = GetSelectedTarget();
            if (target == EnvironmentVariableTarget.Machine && !_isElevated)
            {
                DialogResult dr = MessageBox.Show(this,
                    "Modifying System-wide PATH requires Administrator rights.\r\n\r\nWould you like to switch to 'Current User PATH' instead?",
                    "Elevation Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    rbUserPath.Checked = true;
                    target = EnvironmentVariableTarget.User;
                }
                else
                {
                    return;
                }
            }

            string err;
            if (PathEnvironmentHelper.AddPaths(selectedDirs, target, out err))
            {
                string targetName = target == EnvironmentVariableTarget.Machine ? "System PATH" : "Current User PATH";
                MessageBox.Show(this,
                    "Successfully added selected Hearth runtime paths to " + targetName + "!\r\n\r\n" +
                    "Newly opened Command Prompt or PowerShell terminals will now recognize 'python', 'pip', and 'node' directly.",
                    "PATH Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Failed to update PATH: " + (err ?? "Unknown error"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ReloadRuntimes();
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            List<string> hearthDirsToRemove = new List<string>();
            foreach (RuntimeItem r in _runtimes)
            {
                if (r.IsHearthInstalledInAnyPath)
                {
                    hearthDirsToRemove.Add(r.BundledDirectoryPath);
                }
            }

            if (hearthDirsToRemove.Count == 0)
            {
                MessageBox.Show(this, "None of Hearth's bundled runtimes are currently registered in PATH.", "Nothing to Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "Are you sure you want to remove Hearth's bundled runtime paths from PATH?\r\n\r\n" +
                "Note: Any external runtimes on your system (such as C:\\Python or official Node.js) will be completely preserved and will NOT be touched.",
                "Confirm Hearth Runtime Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string errUser;
            PathEnvironmentHelper.RemoveHearthPaths(hearthDirsToRemove, EnvironmentVariableTarget.User, out errUser);

            if (_isElevated)
            {
                string errSys;
                PathEnvironmentHelper.RemoveHearthPaths(hearthDirsToRemove, EnvironmentVariableTarget.Machine, out errSys);
            }

            MessageBox.Show(this, "Hearth's bundled runtime paths were cleanly removed from PATH.", "PATH Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReloadRuntimes();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            _isInternalCheckChange = true;
            foreach (ListViewItem item in lvRuntimes.Items)
            {
                RuntimeItem r = item.Tag as RuntimeItem;
                if (r != null && r.ExistsInHearthBundle && !r.HasExternalInstallation && !r.IsHearthInstalledInAnyPath)
                {
                    item.Checked = true;
                }
                else
                {
                    item.Checked = false;
                }
            }
            _isInternalCheckChange = false;
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            _isInternalCheckChange = true;
            foreach (ListViewItem item in lvRuntimes.Items)
            {
                item.Checked = false;
            }
            _isInternalCheckChange = false;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
