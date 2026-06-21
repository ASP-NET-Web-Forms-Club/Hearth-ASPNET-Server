using System.IO;
using System.Text;

namespace HearthPortableWebServer.Host
{
    /// <summary>
    /// Writes a small demo ASP.NET Web Forms site into the web root the first time the
    /// server runs against an empty folder. Existing files are never overwritten.
    /// </summary>
    internal static class SampleSite
    {
        public static void EnsureContent(string root)
        {
            WriteIfMissing(Path.Combine(root, "Web.config"), WebConfig);
            WriteIfMissing(Path.Combine(root, "Default.aspx"), DefaultAspx);
            WriteIfMissing(Path.Combine(root, "api.aspx"), ApiAspx);
            WriteIfMissing(Path.Combine(root, "index.html"), IndexHtml);
        }

        private static void WriteIfMissing(string path, string content)
        {
            if (File.Exists(path))
            {
                return;
            }

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private const string WebConfig =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.web>
    <compilation debug=""true"" targetFramework=""4.8"" />
    <httpRuntime targetFramework=""4.8"" maxRequestLength=""102400"" />
    <pages controlRenderingCompatibilityVersion=""4.0"" clientIDMode=""AutoID"" />
  </system.web>
</configuration>
";

        private const string DefaultAspx =
@"<%@ Page Language=""C#"" %>
<!DOCTYPE html>
<script runat=""server"">
    protected void Page_Load(object sender, System.EventArgs e)
    {
        lblTime.Text = System.DateTime.Now.ToString(""yyyy-MM-dd HH:mm:ss"");
        if (!IsPostBack)
        {
            lblMsg.Text = ""Welcome! This page is rendered by ASP.NET Web Forms."";
        }
    }

    protected void Greet_Click(object sender, System.EventArgs e)
    {
        string name = txtName.Text.Trim();
        lblMsg.Text = ""Hello, "" + (name.Length > 0 ? name : ""world"") +
            ""! Handled server-side at "" + System.DateTime.Now.ToString(""HH:mm:ss"") + ""."";
    }
</script>
<html>
<head>
    <title>Hearth Portable ASP.NET Web Server</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 40px; color: #222; }
        h1 { color: #2b5797; }
        .box { border: 1px solid #ccc; border-radius: 6px; padding: 16px; max-width: 520px; }
        input[type=text] { padding: 6px; }
        .msg { color: #107c10; font-weight: bold; }
    </style>
</head>
<body>
    <h1>Hearth Portable ASP.NET Web Server</h1>
    <p>Server time (dynamic): <strong><asp:Label id=""lblTime"" runat=""server"" /></strong></p>
    <div class=""box"">
        <form id=""form1"" runat=""server"">
            <p>Your name:
                <asp:TextBox id=""txtName"" runat=""server"" />
                <asp:Button id=""btnGreet"" runat=""server"" Text=""Greet"" OnClick=""Greet_Click"" />
            </p>
            <p class=""msg""><asp:Label id=""lblMsg"" runat=""server"" /></p>
        </form>
    </div>
    <p><a href=""index.html"">Static page</a> &bull; <a href=""api.aspx?name=tester"">API endpoint</a></p>
</body>
</html>
";

        private const string ApiAspx =
@"<%@ Page Language=""C#"" %><%
    Response.ContentType = ""text/plain"";
    Response.Write(""method="" + Request.HttpMethod + ""\n"");
    Response.Write(""query_name="" + (Request.QueryString[""name""] ?? """") + ""\n"");
    Response.Write(""form_value="" + (Request.Form[""value""] ?? """") + ""\n"");
    Response.Write(""time="" + System.DateTime.Now.ToString(""HH:mm:ss.fff"") + ""\n"");
    Response.Write(""ok=true\n"");
%>";

        private const string IndexHtml =
@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8"" /><title>Static Page</title></head>
<body style=""font-family:Segoe UI,Arial,sans-serif;margin:40px;"">
    <h1>Static content</h1>
    <p>This file (index.html) is served straight from disk by the portable web server.</p>
    <p><a href=""Default.aspx"">Back to the Web Forms page</a></p>
</body>
</html>
";
    }
}
