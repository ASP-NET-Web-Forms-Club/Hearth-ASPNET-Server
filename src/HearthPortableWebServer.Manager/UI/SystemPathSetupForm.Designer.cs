namespace HearthPortableWebServer.Manager.UI
{
    partial class SystemPathSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.Label lblExplanation;
        private System.Windows.Forms.Panel pnlOrphanWarning;
        private System.Windows.Forms.Label lblOrphanText;
        private System.Windows.Forms.Button btnCleanOrphans;
        private System.Windows.Forms.Panel pnlNoBundlesInfo;
        private System.Windows.Forms.Label lblNoBundlesText;
        private System.Windows.Forms.GroupBox grpTargetScope;
        private System.Windows.Forms.RadioButton rbUserPath;
        private System.Windows.Forms.RadioButton rbSystemPath;
        private System.Windows.Forms.ListView lvRuntimes;
        private System.Windows.Forms.ColumnHeader colRuntime;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnDeselectAll;
        private System.Windows.Forms.Button btnAddSelected;
        private System.Windows.Forms.Button btnRemoveSelected;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTip;

        private void InitializeComponent()
        {
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lblExplanation = new System.Windows.Forms.Label();
            this.pnlOrphanWarning = new System.Windows.Forms.Panel();
            this.lblOrphanText = new System.Windows.Forms.Label();
            this.btnCleanOrphans = new System.Windows.Forms.Button();
            this.pnlNoBundlesInfo = new System.Windows.Forms.Panel();
            this.lblNoBundlesText = new System.Windows.Forms.Label();
            this.grpTargetScope = new System.Windows.Forms.GroupBox();
            this.rbUserPath = new System.Windows.Forms.RadioButton();
            this.rbSystemPath = new System.Windows.Forms.RadioButton();
            this.lvRuntimes = new System.Windows.Forms.ListView();
            this.colRuntime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnDeselectAll = new System.Windows.Forms.Button();
            this.btnAddSelected = new System.Windows.Forms.Button();
            this.btnRemoveSelected = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTip = new System.Windows.Forms.Label();
            this.pnlOrphanWarning.SuspendLayout();
            this.pnlNoBundlesInfo.SuspendLayout();
            this.grpTargetScope.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.lblHeaderTitle.Location = new System.Drawing.Point(16, 14);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(910, 24);
            this.lblHeaderTitle.TabIndex = 0;
            this.lblHeaderTitle.Text = "Hearth Bundled Runtimes Environment PATH Manager";
            // 
            // lblExplanation
            // 
            this.lblExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblExplanation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblExplanation.Location = new System.Drawing.Point(17, 40);
            this.lblExplanation.Name = "lblExplanation";
            this.lblExplanation.Size = new System.Drawing.Size(910, 42);
            this.lblExplanation.TabIndex = 1;
            this.lblExplanation.Text = "What is this for?\r\nAdding bundled runtimes into Windows PATH allows AI agents and terminals (cmd, PowerShell, VS Code) to run \'python\', \'pip\', and \'node\' directly. Hearth checks your existing environment first to ensure existing system installations are never overwritten or conflicted.";
            // 
            // pnlOrphanWarning
            // 
            this.pnlOrphanWarning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlOrphanWarning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.pnlOrphanWarning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOrphanWarning.Controls.Add(this.btnCleanOrphans);
            this.pnlOrphanWarning.Controls.Add(this.lblOrphanText);
            this.pnlOrphanWarning.Location = new System.Drawing.Point(20, 86);
            this.pnlOrphanWarning.Name = "pnlOrphanWarning";
            this.pnlOrphanWarning.Size = new System.Drawing.Size(907, 44);
            this.pnlOrphanWarning.TabIndex = 2;
            this.pnlOrphanWarning.Visible = false;
            // 
            // lblOrphanText
            // 
            this.lblOrphanText.Font = new System.Drawing.Font("Segoe UI", 8.75F, System.Drawing.FontStyle.Bold);
            this.lblOrphanText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(27)))), ((int)(((byte)(27)))));
            this.lblOrphanText.Location = new System.Drawing.Point(10, 4);
            this.lblOrphanText.Name = "lblOrphanText";
            this.lblOrphanText.Size = new System.Drawing.Size(660, 34);
            this.lblOrphanText.TabIndex = 0;
            this.lblOrphanText.Text = "⚠ Hearth was moved or deleted from an earlier location. Outdated/broken Hearth paths are still registered in your PATH.";
            // 
            // btnCleanOrphans
            // 
            this.btnCleanOrphans.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCleanOrphans.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(38)))), ((int)(((byte)(38)))));
            this.btnCleanOrphans.ForeColor = System.Drawing.Color.White;
            this.btnCleanOrphans.Location = new System.Drawing.Point(685, 7);
            this.btnCleanOrphans.Name = "btnCleanOrphans";
            this.btnCleanOrphans.Size = new System.Drawing.Size(210, 28);
            this.btnCleanOrphans.TabIndex = 1;
            this.btnCleanOrphans.Text = "🧹 Clean Outdated Hearth Paths";
            this.btnCleanOrphans.UseVisualStyleBackColor = false;
            this.btnCleanOrphans.Click += new System.EventHandler(this.btnCleanOrphans_Click);
            // 
            // pnlNoBundlesInfo
            // 
            this.pnlNoBundlesInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlNoBundlesInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.pnlNoBundlesInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlNoBundlesInfo.Controls.Add(this.lblNoBundlesText);
            this.pnlNoBundlesInfo.Location = new System.Drawing.Point(20, 86);
            this.pnlNoBundlesInfo.Name = "pnlNoBundlesInfo";
            this.pnlNoBundlesInfo.Size = new System.Drawing.Size(907, 44);
            this.pnlNoBundlesInfo.TabIndex = 3;
            this.pnlNoBundlesInfo.Visible = false;
            // 
            // lblNoBundlesText
            // 
            this.lblNoBundlesText.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblNoBundlesText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(12)))), ((int)(((byte)(74)))), ((int)(((byte)(110)))));
            this.lblNoBundlesText.Location = new System.Drawing.Point(10, 5);
            this.lblNoBundlesText.Name = "lblNoBundlesText";
            this.lblNoBundlesText.Size = new System.Drawing.Size(885, 32);
            this.lblNoBundlesText.TabIndex = 0;
            this.lblNoBundlesText.Text = "ℹ Standard ASP.NET Edition: This release does not include bundled Python/Node.js runtimes. To use bundled runtimes, download the 'All-Bundled' release of Hearth.";
            // 
            // grpTargetScope
            // 
            this.grpTargetScope.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpTargetScope.Controls.Add(this.rbUserPath);
            this.grpTargetScope.Controls.Add(this.rbSystemPath);
            this.grpTargetScope.Location = new System.Drawing.Point(20, 136);
            this.grpTargetScope.Name = "grpTargetScope";
            this.grpTargetScope.Size = new System.Drawing.Size(907, 50);
            this.grpTargetScope.TabIndex = 4;
            this.grpTargetScope.TabStop = false;
            this.grpTargetScope.Text = "Installation Scope";
            // 
            // rbUserPath
            // 
            this.rbUserPath.AutoSize = true;
            this.rbUserPath.Checked = true;
            this.rbUserPath.Location = new System.Drawing.Point(20, 20);
            this.rbUserPath.Name = "rbUserPath";
            this.rbUserPath.Size = new System.Drawing.Size(262, 19);
            this.rbUserPath.TabIndex = 0;
            this.rbUserPath.TabStop = true;
            this.rbUserPath.Text = "Current User PATH (Safe, No Admin needed)";
            this.rbUserPath.UseVisualStyleBackColor = true;
            // 
            // rbSystemPath
            // 
            this.rbSystemPath.AutoSize = true;
            this.rbSystemPath.Location = new System.Drawing.Point(340, 20);
            this.rbSystemPath.Name = "rbSystemPath";
            this.rbSystemPath.Size = new System.Drawing.Size(196, 19);
            this.rbSystemPath.TabIndex = 1;
            this.rbSystemPath.Text = "System PATH (All User Accounts)";
            this.rbSystemPath.UseVisualStyleBackColor = true;
            // 
            // lvRuntimes
            // 
            this.lvRuntimes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvRuntimes.CheckBoxes = true;
            this.lvRuntimes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colRuntime,
            this.colStatus,
            this.colPath});
            this.lvRuntimes.FullRowSelect = true;
            this.lvRuntimes.GridLines = true;
            this.lvRuntimes.HideSelection = false;
            this.lvRuntimes.Location = new System.Drawing.Point(20, 194);
            this.lvRuntimes.Name = "lvRuntimes";
            this.lvRuntimes.ShowItemToolTips = true;
            this.lvRuntimes.Size = new System.Drawing.Size(907, 175);
            this.lvRuntimes.TabIndex = 5;
            this.lvRuntimes.UseCompatibleStateImageBehavior = false;
            this.lvRuntimes.View = System.Windows.Forms.View.Details;
            this.lvRuntimes.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lvRuntimes_ItemCheck);
            // 
            // colRuntime
            // 
            this.colRuntime.Text = "Runtime Tool";
            this.colRuntime.Width = 230;
            // 
            // colStatus
            // 
            this.colStatus.Text = "Environment Status (Collision Detection)";
            this.colStatus.Width = 270;
            // 
            // colPath
            // 
            this.colPath.Text = "Hearth Bundled Folder";
            this.colPath.Width = 380;
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSelectAll.Location = new System.Drawing.Point(20, 380);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(125, 28);
            this.btnSelectAll.TabIndex = 6;
            this.btnSelectAll.Text = "Select Installable";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // btnDeselectAll
            // 
            this.btnDeselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDeselectAll.Location = new System.Drawing.Point(155, 380);
            this.btnDeselectAll.Name = "btnDeselectAll";
            this.btnDeselectAll.Size = new System.Drawing.Size(95, 28);
            this.btnDeselectAll.TabIndex = 7;
            this.btnDeselectAll.Text = "Deselect All";
            this.btnDeselectAll.UseVisualStyleBackColor = true;
            this.btnDeselectAll.Click += new System.EventHandler(this.btnDeselectAll_Click);
            // 
            // btnAddSelected
            // 
            this.btnAddSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddSelected.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddSelected.ForeColor = System.Drawing.Color.DarkGreen;
            this.btnAddSelected.Location = new System.Drawing.Point(498, 378);
            this.btnAddSelected.Name = "btnAddSelected";
            this.btnAddSelected.Size = new System.Drawing.Size(175, 32);
            this.btnAddSelected.TabIndex = 8;
            this.btnAddSelected.Text = "➕ Add Selected to PATH";
            this.btnAddSelected.UseVisualStyleBackColor = true;
            this.btnAddSelected.Click += new System.EventHandler(this.btnAddSelected_Click);
            // 
            // btnRemoveSelected
            // 
            this.btnRemoveSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveSelected.ForeColor = System.Drawing.Color.Maroon;
            this.btnRemoveSelected.Location = new System.Drawing.Point(683, 378);
            this.btnRemoveSelected.Name = "btnRemoveSelected";
            this.btnRemoveSelected.Size = new System.Drawing.Size(165, 32);
            this.btnRemoveSelected.TabIndex = 9;
            this.btnRemoveSelected.Text = "Remove Hearth from PATH";
            this.btnRemoveSelected.UseVisualStyleBackColor = true;
            this.btnRemoveSelected.Click += new System.EventHandler(this.btnRemoveSelected_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(856, 378);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(71, 32);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblTip
            // 
            this.lblTip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblTip.Location = new System.Drawing.Point(17, 420);
            this.lblTip.Name = "lblTip";
            this.lblTip.Size = new System.Drawing.Size(910, 20);
            this.lblTip.TabIndex = 11;
            this.lblTip.Text = "Origin Protection: Removing Hearth runtimes will never alter or delete external runtimes (such as C:\\Python) installed on your system.";
            // 
            // SystemPathSetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 450);
            this.Controls.Add(this.lblTip);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnRemoveSelected);
            this.Controls.Add(this.btnAddSelected);
            this.Controls.Add(this.btnDeselectAll);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.lvRuntimes);
            this.Controls.Add(this.grpTargetScope);
            this.Controls.Add(this.pnlNoBundlesInfo);
            this.Controls.Add(this.pnlOrphanWarning);
            this.Controls.Add(this.lblExplanation);
            this.Controls.Add(this.lblHeaderTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SystemPathSetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Bundled Runtimes to System PATH";
            this.Load += new System.EventHandler(this.SystemPathSetupForm_Load);
            this.pnlOrphanWarning.ResumeLayout(false);
            this.pnlNoBundlesInfo.ResumeLayout(false);
            this.grpTargetScope.ResumeLayout(false);
            this.grpTargetScope.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
