using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WindowsNotes
{
    public static class StartupManager
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "WindowsNotes";

        // call this once to enable startup
        public static void EnableRunAtStartup(string exePath)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: true))
            {
                key.SetValue(APP_NAME, $"\"{exePath}\"");
            }
        }

        // optional: disable autostart
        public static void DisableRunAtStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: true))
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
    }
}
