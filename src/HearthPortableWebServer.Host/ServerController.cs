using System;
using System.IO;
using System.Web.Hosting;
using HearthPortableWebServer.Hosting;

namespace HearthPortableWebServer.Host
{
    /// <summary>
    /// Lifecycle manager for the isolated ASP.NET worker. Prepares the web root,
    /// deploys the hosting assembly into the application's <c>bin</c> folder (so the new
    /// AppDomain can load it), then creates and starts the <see cref="AspNetHost"/>.
    /// </summary>
    public sealed class ServerController
    {
        private AspNetHost _host;

        public int Port { get; private set; }
        public string Root { get; private set; }
        public string BoundPrefix { get; private set; }

        public void Start(int port, string root)
        {
            Port = port;
            Root = NormalizeRoot(root);

            Directory.CreateDirectory(Root);
            SampleSite.EnsureContent(Root);
            DeployHostingAssembly(Root);

            // Creating the application host spins up a dedicated ASP.NET AppDomain
            // (the single "worker process"), configured with the web root as its base.
            _host = (AspNetHost)ApplicationHost.CreateApplicationHost(
                typeof(AspNetHost), "/", Root);

            _host.Start(port, Root, "/");
            BoundPrefix = _host.BoundPrefix;
        }

        public void Stop()
        {
            if (_host != null)
            {
                try
                {
                    _host.Stop();
                }
                catch (Exception)
                {
                }
                _host = null;
            }
        }

        public static string NormalizeRoot(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            }
            else if (!Path.IsPathRooted(root))
            {
                root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, root);
            }
            return Path.GetFullPath(root);
        }

        private static void DeployHostingAssembly(string root)
        {
            string binDir = Path.Combine(root, "bin");
            Directory.CreateDirectory(binDir);

            string source = typeof(AspNetHost).Assembly.Location;
            CopyIfPossible(source, binDir);

            // Copy the matching .pdb too, when present, for nicer diagnostics.
            string pdb = Path.ChangeExtension(source, ".pdb");
            if (File.Exists(pdb))
            {
                CopyIfPossible(pdb, binDir);
            }
        }

        private static void CopyIfPossible(string sourceFile, string targetDir)
        {
            try
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, dest, true);
            }
            catch (IOException)
            {
                // Already loaded / locked by a previous run; the existing copy is fine.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
