using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace HearthPortableWebServer.Manager.UI
{
    /// <summary>Small "About" dialog for the Manager.</summary>
    public class AboutForm : Form
    {
        public const string HearthVersion = "1.6";
        private const string ProjectUrl = "https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server";
        private const string DownloadUrl = "https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server/releases";

        public AboutForm(Icon icon)
        {
            Text = "About";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F);
            if (icon != null) Icon = icon;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(16, 14, 16, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            layout.Controls.Add(MakeLabel("Hearth Portable Web Server v" + HearthVersion, new Font("Segoe UI", 12F, FontStyle.Bold), new Padding(0, 0, 0, 2)));
            layout.Controls.Add(MakeLabel("Open Source Freeware", null, new Padding(0, 0, 0, 10)));

            layout.Controls.Add(MakeLabel("Project Site:", null, new Padding(0)));
            layout.Controls.Add(MakeUrlRow(ProjectUrl));

            layout.Controls.Add(MakeLabel("Download:", null, new Padding(0, 6, 0, 0)));
            layout.Controls.Add(MakeUrlRow(DownloadUrl));

            layout.Controls.Add(MakeLabel(
                "A lightweight, high-performance, zero-install portable web server for Windows.",
                null, new Padding(0, 12, 0, 10)));

            layout.Controls.Add(MakeLabel("Support:", new Font("Segoe UI", 9F, FontStyle.Bold), new Padding(0, 0, 0, 4)));
            layout.Controls.Add(MakeLabel(
                "•  ASP.NET Web Forms (native support)" + Environment.NewLine +
                "•  .NET Core",
                null, new Padding(8, 0, 0, 6)));
            layout.Controls.Add(MakeLabel("(require runtime. Bundled or external)", new Font("Segoe UI", 9F, FontStyle.Italic), new Padding(0, 0, 0, 4)));
            layout.Controls.Add(MakeLabel(
                "•  PHP" + Environment.NewLine +
                "•  Python" + Environment.NewLine +
                "•  Node.js",
                null, new Padding(8, 0, 0, 10)));

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right,
                Size = new Size(88, 28)
            };
            layout.Controls.Add(btnOk);
            AcceptButton = btnOk;
            CancelButton = btnOk;

            // Start with focus on OK so no URL text box opens fully selected / scrolled.
            Shown += (s, e) => btnOk.Focus();

            Controls.Add(layout);
            ClientSize = new Size(640, 10);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private static Label MakeLabel(string text, Font font, Padding margin)
        {
            Label l = new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Margin = margin
            };
            if (font != null) l.Font = font;
            return l;
        }

        private Control MakeUrlRow(string url)
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(600, 0),
                Margin = new Padding(0, 2, 0, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            TextBox tb = new TextBox
            {
                Text = url,
                ReadOnly = true,
                BackColor = SystemColors.Window,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 6, 0)
            };
            Button open = new Button
            {
                Text = "Open",
                AutoSize = true,
                Margin = new Padding(0)
            };
            open.Click += (s, e) =>
            {
                try
                {
                    using (Process p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })) { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not open browser: " + ex.Message, "About",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            row.Controls.Add(tb, 0, 0);
            row.Controls.Add(open, 1, 0);
            return row;
        }
    }
}
