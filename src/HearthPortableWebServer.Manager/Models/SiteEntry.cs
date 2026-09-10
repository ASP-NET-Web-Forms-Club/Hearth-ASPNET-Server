using System;

namespace HearthPortableWebServer.Manager.Models
{
    public sealed class SiteEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Port { get; set; }
        public string Root { get; set; }
        public bool AutoStart { get; set; }

        public SiteEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "New Website";
            Port = 8080;
            Root = "wwwroot";
            AutoStart = false;
        }

        public SiteEntry(string name, int port, string root, bool autoStart = false)
        {
            Id = Guid.NewGuid().ToString("N");
            Name = string.IsNullOrEmpty(name) ? "New Website" : name;
            Port = port;
            Root = string.IsNullOrEmpty(root) ? "wwwroot" : root;
            AutoStart = autoStart;
        }

        public SiteEntry Clone()
        {
            return new SiteEntry
            {
                Id = this.Id,
                Name = this.Name,
                Port = this.Port,
                Root = this.Root,
                AutoStart = this.AutoStart
            };
        }
    }
}
