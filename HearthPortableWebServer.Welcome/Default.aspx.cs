using System;
using System.Diagnostics;
using System.Web.UI;

namespace HearthPortableWebServer.Welcome
{
    public partial class Default : System.Web.UI.Page
    {
        // Values rendered into the page at request time (see Default.aspx).
        protected string ServerTime;
        protected string HostAuthority;
        protected string MachineName;
        protected string WorkerProcess;
        protected int ProcessorCount;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Captured fresh on every request — this is what proves the page is dynamic.
            ServerTime = DateTime.Now.ToString("ddd, dd MMM yyyy  HH:mm:ss");
            HostAuthority = Request.Url.Authority;            // e.g. localhost:8080
            MachineName = Environment.MachineName;
            ProcessorCount = Environment.ProcessorCount;

            // The hosting process. Under Hearth this reads "HearthPortableWebServer.Host.exe"
            // — note the absence of w3wp.exe / IIS.
            WorkerProcess = Process.GetCurrentProcess().ProcessName + ".exe";

            if (!IsPostBack)
            {
                ViewState["roundTrips"] = 0;
                PingLabel.Text = "No round-trips yet — give the button a tap.";
            }
        }

        protected void Ping_Click(object sender, EventArgs e)
        {
            // Read the counter back out of ViewState, bump it, store it again.
            int trips = (int)ViewState["roundTrips"] + 1;
            ViewState["roundTrips"] = trips;

            PingLabel.Text = string.Format(
                "Round-trip #{0} handled at {1} — and ViewState survived the journey.",
                trips,
                DateTime.Now.ToString("HH:mm:ss.fff"));
        }
    }
}
