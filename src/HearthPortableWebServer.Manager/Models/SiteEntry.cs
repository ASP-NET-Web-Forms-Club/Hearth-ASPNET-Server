using System;

namespace HearthPortableWebServer.Manager.Models
{
    public sealed class SiteEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Engine { get; set; }
        public int Port { get; set; }
        public string Root { get; set; }
        public string Script { get; set; }
        public string Executable { get; set; }
        public bool AutoStart { get; set; }
        public bool AutoRestart { get; set; }

        public SiteEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "New Website";
            Engine = "ASP.NET";
            Port = 8080;
            Root = "wwwroot";
            Script = string.Empty;
            Executable = string.Empty;
            AutoStart = false;
            AutoRestart = false;
        }

        public SiteEntry(string name, int port, string root, bool autoStart = false, string engine = "ASP.NET", string script = "", string executable = "", bool autoRestart = false)
        {
            Id = Guid.NewGuid().ToString("N");
            Name = string.IsNullOrEmpty(name) ? "New Website" : name;
            Engine = string.IsNullOrEmpty(engine) ? "ASP.NET" : engine;
            Port = port;
            Root = string.IsNullOrEmpty(root) ? "wwwroot" : root;
            Script = script ?? string.Empty;
            Executable = executable ?? string.Empty;
            AutoStart = autoStart;
            AutoRestart = autoRestart;
        }

        public SiteEntry Clone()
        {
            return new SiteEntry
            {
                Id = this.Id,
                Name = this.Name,
                Engine = string.IsNullOrEmpty(this.Engine) ? "ASP.NET" : this.Engine,
                Port = this.Port,
                Root = this.Root,
                Script = this.Script ?? string.Empty,
                Executable = this.Executable ?? string.Empty,
                AutoStart = this.AutoStart,
                AutoRestart = this.AutoRestart
            };
        }
    }
}
