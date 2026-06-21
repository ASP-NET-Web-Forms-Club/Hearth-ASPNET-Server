using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace HearthPortableWebServer.Common
{
    /// <summary>
    /// Creates named Mutex / EventWaitHandle objects with an allow-everyone ACL so that
    /// a non-elevated Launcher can still signal a Host that ended up running elevated
    /// (or as a different user, e.g. the service account).
    /// </summary>
    public static class SyncHelper
    {
        private static MutexSecurity CreateMutexSecurity()
        {
            MutexSecurity security = new MutexSecurity();
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            security.AddAccessRule(new MutexAccessRule(everyone, MutexRights.FullControl, AccessControlType.Allow));
            return security;
        }

        private static EventWaitHandleSecurity CreateEventSecurity()
        {
            EventWaitHandleSecurity security = new EventWaitHandleSecurity();
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            security.AddAccessRule(new EventWaitHandleAccessRule(
                everyone, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            return security;
        }

        public static Mutex CreateMutex(string name, out bool createdNew)
        {
            return new Mutex(true, name, out createdNew, CreateMutexSecurity());
        }

        public static EventWaitHandle CreateEvent(string name, EventResetMode mode)
        {
            bool createdNew;
            return new EventWaitHandle(false, mode, name, out createdNew, CreateEventSecurity());
        }

        public static bool TrySignalEvent(string name)
        {
            EventWaitHandle handle;
            if (EventWaitHandle.TryOpenExisting(name, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize, out handle))
            {
                using (handle)
                {
                    handle.Set();
                    return true;
                }
            }
            return false;
        }

        public static bool MutexExists(string name)
        {
            Mutex mutex;
            if (Mutex.TryOpenExisting(name, MutexRights.Synchronize, out mutex))
            {
                mutex.Dispose();
                return true;
            }
            return false;
        }
    }
}
