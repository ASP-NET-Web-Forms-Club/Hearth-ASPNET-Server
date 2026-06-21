namespace HearthPortableWebServer.Launcher
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

        private System.Windows.Forms.GroupBox grpConfig;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.Label lblRoot;
        private System.Windows.Forms.TextBox txtRoot;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.GroupBox grpServer;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnBrowser;
        private System.Windows.Forms.GroupBox grpService;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.Button btnStartSvc;
        private System.Windows.Forms.Button btnStopSvc;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnMinimize;
        private System.Windows.Forms.Timer statusTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.grpConfig = new System.Windows.Forms.GroupBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.lblRoot = new System.Windows.Forms.Label();
            this.txtRoot = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.grpServer = new System.Windows.Forms.GroupBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnBrowser = new System.Windows.Forms.Button();
            this.grpService = new System.Windows.Forms.GroupBox();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.btnStartSvc = new System.Windows.Forms.Button();
            this.btnStopSvc = new System.Windows.Forms.Button();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnMinimize = new System.Windows.Forms.Button();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.grpConfig.SuspendLayout();
            this.grpServer.SuspendLayout();
            this.grpService.SuspendLayout();
            this.SuspendLayout();
            //
            // grpConfig
            //
            this.grpConfig.Controls.Add(this.lblPort);
            this.grpConfig.Controls.Add(this.numPort);
            this.grpConfig.Controls.Add(this.lblRoot);
            this.grpConfig.Controls.Add(this.txtRoot);
            this.grpConfig.Controls.Add(this.btnBrowse);
            this.grpConfig.Location = new System.Drawing.Point(12, 12);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(444, 100);
            this.grpConfig.TabIndex = 0;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "Configuration";
            //
            // lblPort
            //
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(16, 32);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(32, 13);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "Port:";
            //
            // numPort
            //
            this.numPort.Location = new System.Drawing.Point(110, 30);
            this.numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(90, 20);
            this.numPort.TabIndex = 1;
            this.numPort.Value = new decimal(new int[] { 8080, 0, 0, 0 });
            //
            // lblRoot
            //
            this.lblRoot.AutoSize = true;
            this.lblRoot.Location = new System.Drawing.Point(16, 66);
            this.lblRoot.Name = "lblRoot";
            this.lblRoot.Size = new System.Drawing.Size(56, 13);
            this.lblRoot.TabIndex = 2;
            this.lblRoot.Text = "Web root:";
            //
            // txtRoot
            //
            this.txtRoot.Location = new System.Drawing.Point(110, 63);
            this.txtRoot.Name = "txtRoot";
            this.txtRoot.Size = new System.Drawing.Size(280, 20);
            this.txtRoot.TabIndex = 3;
            //
            // btnBrowse
            //
            this.btnBrowse.Location = new System.Drawing.Point(396, 62);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(32, 23);
            this.btnBrowse.TabIndex = 4;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            //
            // grpServer
            //
            this.grpServer.Controls.Add(this.btnStart);
            this.grpServer.Controls.Add(this.btnStop);
            this.grpServer.Controls.Add(this.btnBrowser);
            this.grpServer.Location = new System.Drawing.Point(12, 120);
            this.grpServer.Name = "grpServer";
            this.grpServer.Size = new System.Drawing.Size(444, 72);
            this.grpServer.TabIndex = 1;
            this.grpServer.TabStop = false;
            this.grpServer.Text = "Web Server";
            //
            // btnStart
            //
            this.btnStart.Location = new System.Drawing.Point(16, 28);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(130, 30);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start Web Server";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            //
            // btnStop
            //
            this.btnStop.Location = new System.Drawing.Point(156, 28);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(130, 30);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop Web Server";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            //
            // btnBrowser
            //
            this.btnBrowser.Location = new System.Drawing.Point(296, 28);
            this.btnBrowser.Name = "btnBrowser";
            this.btnBrowser.Size = new System.Drawing.Size(130, 30);
            this.btnBrowser.TabIndex = 2;
            this.btnBrowser.Text = "Browse Web App";
            this.btnBrowser.UseVisualStyleBackColor = true;
            this.btnBrowser.Click += new System.EventHandler(this.btnBrowser_Click);
            //
            // grpService
            //
            this.grpService.Controls.Add(this.btnInstall);
            this.grpService.Controls.Add(this.btnUninstall);
            this.grpService.Controls.Add(this.btnStartSvc);
            this.grpService.Controls.Add(this.btnStopSvc);
            this.grpService.Controls.Add(this.lblServiceStatus);
            this.grpService.Location = new System.Drawing.Point(12, 200);
            this.grpService.Name = "grpService";
            this.grpService.Size = new System.Drawing.Size(444, 118);
            this.grpService.TabIndex = 2;
            this.grpService.TabStop = false;
            this.grpService.Text = "Auto-Start Windows Service";
            //
            // btnInstall
            //
            this.btnInstall.Location = new System.Drawing.Point(16, 26);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(200, 28);
            this.btnInstall.TabIndex = 0;
            this.btnInstall.Text = "Install Service (auto-start)";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            //
            // btnUninstall
            //
            this.btnUninstall.Location = new System.Drawing.Point(228, 26);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(200, 28);
            this.btnUninstall.TabIndex = 1;
            this.btnUninstall.Text = "Uninstall Service";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            //
            // btnStartSvc
            //
            this.btnStartSvc.Location = new System.Drawing.Point(16, 60);
            this.btnStartSvc.Name = "btnStartSvc";
            this.btnStartSvc.Size = new System.Drawing.Size(130, 28);
            this.btnStartSvc.TabIndex = 2;
            this.btnStartSvc.Text = "Start Service";
            this.btnStartSvc.UseVisualStyleBackColor = true;
            this.btnStartSvc.Click += new System.EventHandler(this.btnStartSvc_Click);
            //
            // btnStopSvc
            //
            this.btnStopSvc.Location = new System.Drawing.Point(156, 60);
            this.btnStopSvc.Name = "btnStopSvc";
            this.btnStopSvc.Size = new System.Drawing.Size(130, 28);
            this.btnStopSvc.TabIndex = 3;
            this.btnStopSvc.Text = "Stop Service";
            this.btnStopSvc.UseVisualStyleBackColor = true;
            this.btnStopSvc.Click += new System.EventHandler(this.btnStopSvc_Click);
            //
            // lblServiceStatus
            //
            this.lblServiceStatus.AutoSize = true;
            this.lblServiceStatus.Location = new System.Drawing.Point(16, 96);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(75, 13);
            this.lblServiceStatus.TabIndex = 4;
            this.lblServiceStatus.Text = "Service: ...";
            //
            // lblStatus
            //
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Location = new System.Drawing.Point(0, 331);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(468, 24);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Stopped";
            //
            // btnMinimize
            //
            this.btnMinimize.Location = new System.Drawing.Point(12, 326);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Size = new System.Drawing.Size(444, 30);
            this.btnMinimize.TabIndex = 4;
            this.btnMinimize.Text = "Minimize this program to task bar";
            this.btnMinimize.UseVisualStyleBackColor = true;
            this.btnMinimize.Click += new System.EventHandler(this.btnMinimize_Click);
            //
            // statusTimer
            //
            this.statusTimer.Interval = 1000;
            this.statusTimer.Tick += new System.EventHandler(this.statusTimer_Tick);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(468, 396);
            this.Controls.Add(this.grpConfig);
            this.Controls.Add(this.grpServer);
            this.Controls.Add(this.grpService);
            this.Controls.Add(this.btnMinimize);
            this.Controls.Add(this.lblStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hearth Portable ASP.NET Web Server";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.grpConfig.ResumeLayout(false);
            this.grpConfig.PerformLayout();
            this.grpServer.ResumeLayout(false);
            this.grpService.ResumeLayout(false);
            this.grpService.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
