namespace HearthPortableWebServer.Manager.UI
{
    partial class MainForm
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

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton btnAddSite;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnStartAll;
        private System.Windows.Forms.ToolStripButton btnStopAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnServiceSetup;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ListView lvSites;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colEngine;
        private System.Windows.Forms.ColumnHeader colPort;
        private System.Windows.Forms.ColumnHeader colAutoStart;
        private System.Windows.Forms.ColumnHeader colRoot;
        private System.Windows.Forms.GroupBox grpDetails;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.ComboBox cmbEngine;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label lblRoot;
        private System.Windows.Forms.TextBox txtRoot;
        private System.Windows.Forms.Button btnBrowseRoot;
        private System.Windows.Forms.Label lblScript;
        private System.Windows.Forms.TextBox txtScript;
        private System.Windows.Forms.Label lblExecutable;
        private System.Windows.Forms.TextBox txtExecutable;
        private System.Windows.Forms.Button btnBrowseExe;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkAutoRestart;
        private System.Windows.Forms.Button btnSaveSite;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatusSummary;
        private System.Windows.Forms.ToolStripStatusLabel lblServiceStatus;
        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenu;
        private System.Windows.Forms.ToolStripMenuItem menuOpenDashboard;
        private System.Windows.Forms.ToolStripSeparator menuSep1;
        private System.Windows.Forms.ToolStripMenuItem menuStartAll;
        private System.Windows.Forms.ToolStripMenuItem menuStopAll;
        private System.Windows.Forms.ToolStripSeparator menuSep2;
        private System.Windows.Forms.ToolStripMenuItem menuExit;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.btnAddSite = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnStartAll = new System.Windows.Forms.ToolStripButton();
            this.btnStopAll = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnServiceSetup = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lvSites = new System.Windows.Forms.ListView();
            this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEngine = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPort = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colAutoStart = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRoot = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.grpDetails = new System.Windows.Forms.GroupBox();
            this.btnSaveSite = new System.Windows.Forms.Button();
            this.chkAutoRestart = new System.Windows.Forms.CheckBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.btnBrowseExe = new System.Windows.Forms.Button();
            this.txtExecutable = new System.Windows.Forms.TextBox();
            this.lblExecutable = new System.Windows.Forms.Label();
            this.txtScript = new System.Windows.Forms.TextBox();
            this.lblScript = new System.Windows.Forms.Label();
            this.btnBrowseRoot = new System.Windows.Forms.Button();
            this.txtRoot = new System.Windows.Forms.TextBox();
            this.lblRoot = new System.Windows.Forms.Label();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.lblPort = new System.Windows.Forms.Label();
            this.cmbEngine = new System.Windows.Forms.ComboBox();
            this.lblEngine = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatusSummary = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblServiceStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuOpenDashboard = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuStartAll = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStopAll = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.grpDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.trayMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnAddSite,
            this.toolStripSeparator1,
            this.btnStartAll,
            this.btnStopAll,
            this.toolStripSeparator2,
            this.btnServiceSetup,
            this.toolStripSeparator3,
            this.btnRefresh});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(6, 4, 1, 4);
            this.toolStrip.Size = new System.Drawing.Size(1200, 31);
            this.toolStrip.TabIndex = 0;
            // 
            // btnAddSite
            // 
            this.btnAddSite.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddSite.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAddSite.Name = "btnAddSite";
            this.btnAddSite.Size = new System.Drawing.Size(93, 20);
            this.btnAddSite.Text = "+ Add Website";
            this.btnAddSite.Click += new System.EventHandler(this.btnAddSite_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 23);
            // 
            // btnStartAll
            // 
            this.btnStartAll.Name = "btnStartAll";
            this.btnStartAll.Size = new System.Drawing.Size(65, 20);
            this.btnStartAll.Text = "▶ Start All";
            this.btnStartAll.Click += new System.EventHandler(this.btnStartAll_Click);
            // 
            // btnStopAll
            // 
            this.btnStopAll.Name = "btnStopAll";
            this.btnStopAll.Size = new System.Drawing.Size(67, 20);
            this.btnStopAll.Text = "⏹ Stop All";
            this.btnStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 23);
            // 
            // btnServiceSetup
            // 
            this.btnServiceSetup.Name = "btnServiceSetup";
            this.btnServiceSetup.Size = new System.Drawing.Size(151, 20);
            this.btnServiceSetup.Text = "⚙ Windows Service (Boot)";
            this.btnServiceSetup.Click += new System.EventHandler(this.btnServiceSetup_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 23);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(63, 20);
            this.btnRefresh.Text = "⟳ Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer.Location = new System.Drawing.Point(0, 31);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.lvSites);
            this.splitContainer.Panel1.Padding = new System.Windows.Forms.Padding(8, 8, 4, 8);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.grpActions);
            this.splitContainer.Panel2.Controls.Add(this.grpDetails);
            this.splitContainer.Panel2.Padding = new System.Windows.Forms.Padding(4, 8, 8, 8);
            this.splitContainer.Size = new System.Drawing.Size(1200, 662);
            this.splitContainer.SplitterDistance = 770;
            this.splitContainer.TabIndex = 1;
            // 
            // lvSites
            // 
            this.lvSites.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colStatus,
            this.colName,
            this.colEngine,
            this.colPort,
            this.colAutoStart,
            this.colRoot});
            this.lvSites.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvSites.FullRowSelect = true;
            this.lvSites.GridLines = true;
            this.lvSites.HideSelection = false;
            this.lvSites.Location = new System.Drawing.Point(8, 8);
            this.lvSites.MultiSelect = false;
            this.lvSites.Name = "lvSites";
            this.lvSites.ShowItemToolTips = true;
            this.lvSites.Size = new System.Drawing.Size(758, 646);
            this.lvSites.TabIndex = 0;
            this.lvSites.UseCompatibleStateImageBehavior = false;
            this.lvSites.View = System.Windows.Forms.View.Details;
            this.lvSites.SelectedIndexChanged += new System.EventHandler(this.lvSites_SelectedIndexChanged);
            // 
            // colStatus
            // 
            this.colStatus.Text = "Status";
            this.colStatus.Width = 90;
            // 
            // colName
            // 
            this.colName.Text = "Site Name";
            this.colName.Width = 140;
            // 
            // colEngine
            // 
            this.colEngine.Text = "Type";
            this.colEngine.Width = 95;
            // 
            // colPort
            // 
            this.colPort.Text = "Port";
            this.colPort.Width = 65;
            // 
            // colAutoStart
            // 
            this.colAutoStart.Text = "Auto-Start";
            this.colAutoStart.Width = 75;
            // 
            // colRoot
            // 
            this.colRoot.Text = "Web Root Folder";
            this.colRoot.Width = 310;
            // 
            // grpActions
            // 
            this.grpActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpActions.Controls.Add(this.btnDelete);
            this.grpActions.Controls.Add(this.btnOpenFolder);
            this.grpActions.Controls.Add(this.btnBrowse);
            this.grpActions.Controls.Add(this.btnStop);
            this.grpActions.Controls.Add(this.btnStart);
            this.grpActions.Location = new System.Drawing.Point(4, 385);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(414, 215);
            this.grpActions.TabIndex = 1;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "Site Actions";
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.ForeColor = System.Drawing.Color.Maroon;
            this.btnDelete.Location = new System.Drawing.Point(16, 168);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(382, 30);
            this.btnDelete.TabIndex = 4;
            this.btnDelete.Text = "🗑 Delete Website Entry";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenFolder.Location = new System.Drawing.Point(16, 126);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(382, 30);
            this.btnOpenFolder.TabIndex = 3;
            this.btnOpenFolder.Text = "📁 Open Web Root in Explorer";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowse.Location = new System.Drawing.Point(16, 82);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(382, 32);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "🌐 Browse in Browser";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStop.ForeColor = System.Drawing.Color.Maroon;
            this.btnStop.Location = new System.Drawing.Point(212, 30);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(186, 38);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "⏹ Stop Server";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStart.ForeColor = System.Drawing.Color.DarkGreen;
            this.btnStart.Location = new System.Drawing.Point(16, 30);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(186, 38);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "▶ Start Server";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // grpDetails
            // 
            this.grpDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpDetails.Controls.Add(this.btnSaveSite);
            this.grpDetails.Controls.Add(this.chkAutoRestart);
            this.grpDetails.Controls.Add(this.chkAutoStart);
            this.grpDetails.Controls.Add(this.btnBrowseExe);
            this.grpDetails.Controls.Add(this.txtExecutable);
            this.grpDetails.Controls.Add(this.lblExecutable);
            this.grpDetails.Controls.Add(this.txtScript);
            this.grpDetails.Controls.Add(this.lblScript);
            this.grpDetails.Controls.Add(this.btnBrowseRoot);
            this.grpDetails.Controls.Add(this.txtRoot);
            this.grpDetails.Controls.Add(this.lblRoot);
            this.grpDetails.Controls.Add(this.numPort);
            this.grpDetails.Controls.Add(this.lblPort);
            this.grpDetails.Controls.Add(this.cmbEngine);
            this.grpDetails.Controls.Add(this.lblEngine);
            this.grpDetails.Controls.Add(this.txtName);
            this.grpDetails.Controls.Add(this.lblName);
            this.grpDetails.Location = new System.Drawing.Point(4, 8);
            this.grpDetails.Name = "grpDetails";
            this.grpDetails.Size = new System.Drawing.Size(414, 370);
            this.grpDetails.TabIndex = 0;
            this.grpDetails.TabStop = false;
            this.grpDetails.Text = "Configuration";
            // 
            // btnSaveSite
            // 
            this.btnSaveSite.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveSite.Location = new System.Drawing.Point(95, 324);
            this.btnSaveSite.Name = "btnSaveSite";
            this.btnSaveSite.Size = new System.Drawing.Size(303, 34);
            this.btnSaveSite.TabIndex = 16;
            this.btnSaveSite.Text = "💾 Save Configuration";
            this.btnSaveSite.UseVisualStyleBackColor = true;
            this.btnSaveSite.Click += new System.EventHandler(this.btnSaveSite_Click);
            // 
            // chkAutoRestart
            // 
            this.chkAutoRestart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAutoRestart.Location = new System.Drawing.Point(118, 290);
            this.chkAutoRestart.Name = "chkAutoRestart";
            this.chkAutoRestart.Size = new System.Drawing.Size(280, 26);
            this.chkAutoRestart.TabIndex = 15;
            this.chkAutoRestart.Text = "Live Reload: Auto-restart on file change";
            this.chkAutoRestart.UseVisualStyleBackColor = true;
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAutoStart.Location = new System.Drawing.Point(118, 260);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(280, 26);
            this.chkAutoStart.TabIndex = 14;
            this.chkAutoStart.Text = "Auto-start on machine boot / service launch";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // btnBrowseExe
            // 
            this.btnBrowseExe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseExe.Location = new System.Drawing.Point(368, 226);
            this.btnBrowseExe.Name = "btnBrowseExe";
            this.btnBrowseExe.Size = new System.Drawing.Size(30, 25);
            this.btnBrowseExe.TabIndex = 13;
            this.btnBrowseExe.Text = "...";
            this.btnBrowseExe.UseVisualStyleBackColor = true;
            this.btnBrowseExe.Click += new System.EventHandler(this.btnBrowseExe_Click);
            // 
            // txtExecutable
            // 
            this.txtExecutable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtExecutable.Location = new System.Drawing.Point(118, 227);
            this.txtExecutable.Name = "txtExecutable";
            this.txtExecutable.Size = new System.Drawing.Size(244, 23);
            this.txtExecutable.TabIndex = 12;
            // 
            // lblExecutable
            // 
            this.lblExecutable.AutoSize = true;
            this.lblExecutable.Location = new System.Drawing.Point(12, 230);
            this.lblExecutable.Name = "lblExecutable";
            this.lblExecutable.Size = new System.Drawing.Size(67, 15);
            this.lblExecutable.TabIndex = 11;
            this.lblExecutable.Text = "Executable:";
            // 
            // txtScript
            // 
            this.txtScript.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtScript.Location = new System.Drawing.Point(118, 193);
            this.txtScript.Name = "txtScript";
            this.txtScript.Size = new System.Drawing.Size(280, 23);
            this.txtScript.TabIndex = 10;
            // 
            // lblScript
            // 
            this.lblScript.AutoSize = true;
            this.lblScript.Location = new System.Drawing.Point(12, 196);
            this.lblScript.Name = "lblScript";
            this.lblScript.Size = new System.Drawing.Size(69, 15);
            this.lblScript.TabIndex = 9;
            this.lblScript.Text = "Script / File:";
            // 
            // btnBrowseRoot
            // 
            this.btnBrowseRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseRoot.Location = new System.Drawing.Point(368, 159);
            this.btnBrowseRoot.Name = "btnBrowseRoot";
            this.btnBrowseRoot.Size = new System.Drawing.Size(30, 25);
            this.btnBrowseRoot.TabIndex = 8;
            this.btnBrowseRoot.Text = "...";
            this.btnBrowseRoot.UseVisualStyleBackColor = true;
            this.btnBrowseRoot.Click += new System.EventHandler(this.btnBrowseRoot_Click);
            // 
            // txtRoot
            // 
            this.txtRoot.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRoot.Location = new System.Drawing.Point(118, 160);
            this.txtRoot.Name = "txtRoot";
            this.txtRoot.Size = new System.Drawing.Size(244, 23);
            this.txtRoot.TabIndex = 7;
            // 
            // lblRoot
            // 
            this.lblRoot.AutoSize = true;
            this.lblRoot.Location = new System.Drawing.Point(12, 163);
            this.lblRoot.Name = "lblRoot";
            this.lblRoot.Size = new System.Drawing.Size(62, 15);
            this.lblRoot.TabIndex = 6;
            this.lblRoot.Text = "Web Root:";
            // 
            // numPort
            // 
            this.numPort.Location = new System.Drawing.Point(118, 126);
            this.numPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(72, 23);
            this.numPort.TabIndex = 5;
            this.numPort.Value = new decimal(new int[] {
            8080,
            0,
            0,
            0});
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(12, 128);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(32, 15);
            this.lblPort.TabIndex = 4;
            this.lblPort.Text = "Port:";
            // 
            // cmbEngine
            // 
            this.cmbEngine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEngine.FormattingEnabled = true;
            this.cmbEngine.Items.AddRange(new object[] {
            "ASP.NET",
            ".NET Core",
            "Python",
            "Node.js",
            "PHP",
            "Other"});
            this.cmbEngine.Location = new System.Drawing.Point(118, 92);
            this.cmbEngine.Name = "cmbEngine";
            this.cmbEngine.Size = new System.Drawing.Size(280, 23);
            this.cmbEngine.TabIndex = 3;
            this.cmbEngine.SelectedIndexChanged += new System.EventHandler(this.cmbEngine_SelectedIndexChanged);
            // 
            // lblEngine
            // 
            this.lblEngine.AutoSize = true;
            this.lblEngine.Location = new System.Drawing.Point(12, 95);
            this.lblEngine.Name = "lblEngine";
            this.lblEngine.Size = new System.Drawing.Size(34, 15);
            this.lblEngine.TabIndex = 2;
            this.lblEngine.Text = "Type:";
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(118, 58);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(280, 23);
            this.txtName.TabIndex = 1;
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 61);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(64, 15);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Site Name:";
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatusSummary,
            this.lblServiceStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 693);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.TabIndex = 2;
            // 
            // lblStatusSummary
            // 
            this.lblStatusSummary.Name = "lblStatusSummary";
            this.lblStatusSummary.Size = new System.Drawing.Size(202, 17);
            this.lblStatusSummary.Text = "Total Sites: 0 | Running: 0 | Stopped: 0";
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(983, 17);
            this.lblServiceStatus.Spring = true;
            this.lblServiceStatus.Text = "Service: Checking...";
            this.lblServiceStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // statusTimer
            // 
            this.statusTimer.Interval = 1000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayMenu;
            this.trayIcon.Text = "Hearth Multi-Site Manager";
            this.trayIcon.Visible = true;
            this.trayIcon.DoubleClick += new System.EventHandler(this.trayIcon_DoubleClick);
            // 
            // trayMenu
            // 
            this.trayMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOpenDashboard,
            this.menuSep1,
            this.menuStartAll,
            this.menuStopAll,
            this.menuSep2,
            this.menuExit});
            this.trayMenu.Name = "trayMenu";
            this.trayMenu.Size = new System.Drawing.Size(167, 104);
            // 
            // menuOpenDashboard
            // 
            this.menuOpenDashboard.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.menuOpenDashboard.Name = "menuOpenDashboard";
            this.menuOpenDashboard.Size = new System.Drawing.Size(166, 22);
            this.menuOpenDashboard.Text = "Open Dashboard";
            this.menuOpenDashboard.Click += new System.EventHandler(this.menuOpenDashboard_Click);
            // 
            // menuSep1
            // 
            this.menuSep1.Name = "menuSep1";
            this.menuSep1.Size = new System.Drawing.Size(163, 6);
            // 
            // menuStartAll
            // 
            this.menuStartAll.Name = "menuStartAll";
            this.menuStartAll.Size = new System.Drawing.Size(166, 22);
            this.menuStartAll.Text = "▶ Start All Sites";
            this.menuStartAll.Click += new System.EventHandler(this.btnStartAll_Click);
            // 
            // menuStopAll
            // 
            this.menuStopAll.Name = "menuStopAll";
            this.menuStopAll.Size = new System.Drawing.Size(166, 22);
            this.menuStopAll.Text = "⏹ Stop All Sites";
            this.menuStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // menuSep2
            // 
            this.menuSep2.Name = "menuSep2";
            this.menuSep2.Size = new System.Drawing.Size(163, 6);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(166, 22);
            this.menuExit.Text = "Exit";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 715);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(950, 560);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hearth Multi-Site Server Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.grpDetails.ResumeLayout(false);
            this.grpDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.trayMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
