using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace HearthPortableWebServer.Manager.Services
{
    public class RuntimeItem
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string CommandName { get; set; } // e.g. "python.exe", "pip.exe", "node.exe"
        public string BundledDirectoryPath { get; set; }
        public string BundledExePath { get; set; }
        public bool ExistsInHearthBundle { get; set; }

        // Detection of Hearth's specific bundle in PATH
        public bool IsHearthInUserPath { get; set; }
        public bool IsHearthInSystemPath { get; set; }

        // Detection of any existing installation elsewhere on system
        public string ExternalDetectedPath { get; set; }
        public bool HasExternalInstallation { get; set; }

        public bool IsHearthInstalledInAnyPath => IsHearthInUserPath || IsHearthInSystemPath;
    }

    public class OrphanPathEntry
    {
        public string RawPath { get; set; }
        public EnvironmentVariableTarget Target { get; set; }
        public string Reason { get; set; } // e.g. "Previous Hearth Node.js path (folder no longer exists)"
    }

    public static class PathEnvironmentHelper
    {
        public static string GetRuntimesRootDir()
        {
            string appDir = ManagerConfig.GetAppDirectory();
            return Path.Combine(appDir, "runtimes");
        }

        public static bool HasAnyBundledRuntimesOnDisk()
        {
            string runtimesRoot = GetRuntimesRootDir();
            if (!Directory.Exists(runtimesRoot)) return false;

            string pythonExe = Path.Combine(runtimesRoot, "python", "python.exe");
            string nodeExe = Path.Combine(runtimesRoot, "node", "node.exe");

            return File.Exists(pythonExe) || File.Exists(nodeExe);
        }

        public static List<RuntimeItem> DetectBundledRuntimes()
        {
            string runtimesRoot = GetRuntimesRootDir();
            List<RuntimeItem> list = new List<RuntimeItem>();

            // 1. Python Runtime
            string pythonDir = Path.Combine(runtimesRoot, "python");
            string pythonExe = Path.Combine(pythonDir, "python.exe");
            list.Add(new RuntimeItem
            {
                Key = "python",
                Name = "Python 3 Interpreter",
                CommandName = "python.exe",
                BundledDirectoryPath = pythonDir,
                BundledExePath = pythonExe,
                ExistsInHearthBundle = File.Exists(pythonExe)
            });

            // 2. Python Scripts / Pip
            string pythonScriptsDir = Path.Combine(pythonDir, "Scripts");
            string pipExe = Path.Combine(pythonScriptsDir, "pip.exe");
            list.Add(new RuntimeItem
            {
                Key = "python_scripts",
                Name = "Python Scripts (pip & CLI packages)",
                CommandName = "pip.exe",
                BundledDirectoryPath = pythonScriptsDir,
                BundledExePath = pipExe,
                ExistsInHearthBundle = Directory.Exists(pythonScriptsDir) && File.Exists(pipExe)
            });

            // 3. Node.js & npm
            string nodeDir = Path.Combine(runtimesRoot, "node");
            string nodeExe = Path.Combine(nodeDir, "node.exe");
            list.Add(new RuntimeItem
            {
                Key = "node",
                Name = "Node.js & npm package manager",
                CommandName = "node.exe",
                BundledDirectoryPath = nodeDir,
                BundledExePath = nodeExe,
                ExistsInHearthBundle = File.Exists(nodeExe)
            });

            // Refresh PATH and external collisions
            RefreshPathStatuses(list);

            return list;
        }

        public static void RefreshPathStatuses(IEnumerable<RuntimeItem> items)
        {
            string userPathRaw = GetRawEnvironmentPath(EnvironmentVariableTarget.User);
            string systemPathRaw = GetRawEnvironmentPath(EnvironmentVariableTarget.Machine);

            string[] userSegments = SplitPath(userPathRaw);
            string[] systemSegments = SplitPath(systemPathRaw);

            var allSegments = userSegments.Concat(systemSegments).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (RuntimeItem item in items)
            {
                item.IsHearthInUserPath = ContainsDirectory(userSegments, item.BundledDirectoryPath);
                item.IsHearthInSystemPath = ContainsDirectory(systemSegments, item.BundledDirectoryPath);

                string externalFound = FindExternalCommandPath(item.CommandName, item.BundledDirectoryPath, allSegments);
                if (!string.IsNullOrEmpty(externalFound))
                {
                    item.HasExternalInstallation = true;
                    item.ExternalDetectedPath = externalFound;
                }
                else
                {
                    item.HasExternalInstallation = false;
                    item.ExternalDetectedPath = null;
                }
            }
        }

        /// <summary>
        /// Detects broken/orphan paths that match Hearth patterns (e.g. \runtimes\python or \runtimes\node)
        /// that no longer exist on disk (indicating Hearth was moved or deleted).
        /// </summary>
        public static List<OrphanPathEntry> DetectOrphanHearthPaths()
        {
            List<OrphanPathEntry> orphans = new List<OrphanPathEntry>();
            string currentAppDir = NormalizePath(ManagerConfig.GetAppDirectory());

            CheckOrphansInTarget(EnvironmentVariableTarget.User, currentAppDir, orphans);
            CheckOrphansInTarget(EnvironmentVariableTarget.Machine, currentAppDir, orphans);

            return orphans;
        }

        private static void CheckOrphansInTarget(EnvironmentVariableTarget target, string currentAppDir, List<OrphanPathEntry> list)
        {
            string raw = GetRawEnvironmentPath(target);
            string[] segments = SplitPath(raw);

            foreach (string seg in segments)
            {
                if (string.IsNullOrWhiteSpace(seg)) continue;
                string norm = NormalizePath(seg);

                // Is it a runtime subpath (runtimes\python, runtimes\node, runtimes\php)?
                bool isRuntimeSubpath = norm.EndsWith(@"\runtimes\python", StringComparison.OrdinalIgnoreCase) ||
                                       norm.EndsWith(@"\runtimes\python\scripts", StringComparison.OrdinalIgnoreCase) ||
                                       norm.EndsWith(@"\runtimes\node", StringComparison.OrdinalIgnoreCase) ||
                                       norm.EndsWith(@"\runtimes\php", StringComparison.OrdinalIgnoreCase);

                if (isRuntimeSubpath)
                {
                    // Check if it belongs to an old Hearth location (not under the current directory) and does not exist
                    bool exists = Directory.Exists(norm);
                    bool isUnderCurrent = norm.StartsWith(currentAppDir, StringComparison.OrdinalIgnoreCase);

                    if (!exists || !isUnderCurrent)
                    {
                        // Even if it exists elsewhere, if it's an old Hearth folder from a past move, it's outdated
                        string desc = !exists 
                            ? "Folder no longer exists on disk (orphaned by move/delete)" 
                            : "Points to a different Hearth folder location";

                        list.Add(new OrphanPathEntry
                        {
                            RawPath = seg,
                            Target = target,
                            Reason = desc
                        });
                    }
                }
            }
        }

        public static bool CleanOrphanPaths(IEnumerable<OrphanPathEntry> orphans, out string error)
        {
            error = null;
            try
            {
                var userOrphans = orphans.Where(o => o.Target == EnvironmentVariableTarget.User).Select(o => o.RawPath).ToList();
                var sysOrphans = orphans.Where(o => o.Target == EnvironmentVariableTarget.Machine).Select(o => o.RawPath).ToList();

                if (userOrphans.Count > 0)
                {
                    string err;
                    RemoveHearthPaths(userOrphans, EnvironmentVariableTarget.User, out err);
                }

                if (sysOrphans.Count > 0)
                {
                    string err;
                    RemoveHearthPaths(sysOrphans, EnvironmentVariableTarget.Machine, out err);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string FindExternalCommandPath(string commandName, string hearthDir, IEnumerable<string> pathSegments)
        {
            string normHearthDir = NormalizePath(hearthDir);

            foreach (string seg in pathSegments)
            {
                if (string.IsNullOrWhiteSpace(seg)) continue;
                string normSeg = NormalizePath(seg);
                if (string.Equals(normSeg, normHearthDir, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    string candidate = Path.Combine(seg, commandName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch { }
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("where.exe", commandName)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(1500);
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string line in lines)
                            {
                                string trimmed = line.Trim();
                                if (!string.IsNullOrEmpty(trimmed) && File.Exists(trimmed))
                                {
                                    string dir = Path.GetDirectoryName(trimmed);
                                    if (!string.Equals(NormalizePath(dir), normHearthDir, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return trimmed;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public static string GetRawEnvironmentPath(EnvironmentVariableTarget target)
        {
            try
            {
                if (target == EnvironmentVariableTarget.User)
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Environment", false))
                    {
                        if (key != null)
                        {
                            return (key.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string) ?? string.Empty;
                        }
                    }
                }
                else if (target == EnvironmentVariableTarget.Machine)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", false))
                    {
                        if (key != null)
                        {
                            return (key.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string) ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                return Environment.GetEnvironmentVariable("Path", target) ?? string.Empty;
            }
            return string.Empty;
        }

        public static void SetRawEnvironmentPath(EnvironmentVariableTarget target, string newPath)
        {
            if (target == EnvironmentVariableTarget.User)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Environment", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Path", newPath ?? string.Empty, RegistryValueKind.ExpandString);
                    }
                }
            }
            else if (target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Path", newPath ?? string.Empty, RegistryValueKind.ExpandString);
                    }
                }
            }

            BroadcastSettingChange();
        }

        public static bool AddPaths(IEnumerable<string> pathsToAdd, EnvironmentVariableTarget target, out string error)
        {
            error = null;
            try
            {
                string raw = GetRawEnvironmentPath(target);
                List<string> segments = SplitPath(raw).ToList();

                bool changed = false;
                foreach (string p in pathsToAdd)
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    if (!ContainsDirectory(segments, p))
                    {
                        segments.Add(p);
                        changed = true;
                    }
                }

                if (changed)
                {
                    string joined = string.Join(";", segments.Where(s => !string.IsNullOrWhiteSpace(s)));
                    SetRawEnvironmentPath(target, joined);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool RemoveHearthPaths(IEnumerable<string> hearthPathsToRemove, EnvironmentVariableTarget target, out string error)
        {
            error = null;
            try
            {
                string raw = GetRawEnvironmentPath(target);
                string[] segments = SplitPath(raw);

                Dictionary<string, bool> removeMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (string p in hearthPathsToRemove)
                {
                    removeMap[NormalizePath(p)] = true;
                }

                List<string> remaining = new List<string>();
                foreach (string s in segments)
                {
                    if (!removeMap.ContainsKey(NormalizePath(s)))
                    {
                        remaining.Add(s);
                    }
                }

                if (remaining.Count != segments.Length)
                {
                    string joined = string.Join(";", remaining.Where(s => !string.IsNullOrWhiteSpace(s)));
                    SetRawEnvironmentPath(target, joined);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string[] SplitPath(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return new string[0];
            return raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            try
            {
                return Path.GetFullPath(path.Trim().TrimEnd('\\', '/'));
            }
            catch
            {
                return path.Trim().TrimEnd('\\', '/');
            }
        }

        private static bool ContainsDirectory(IEnumerable<string> list, string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return false;
            string normTarget = NormalizePath(dir);
            foreach (string s in list)
            {
                if (string.Equals(NormalizePath(s), normTarget, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint msg,
            UIntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        private const int HWND_BROADCAST = 0xffff;
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        public static void BroadcastSettingChange()
        {
            try
            {
                UIntPtr result;
                SendMessageTimeout(
                    (IntPtr)HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    "Environment",
                    SMTO_ABORTIFHUNG,
                    2000,
                    out result);
            }
            catch { }
        }
    }
}
