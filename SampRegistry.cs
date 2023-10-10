using Microsoft.Win32;
using System;
using System.IO;

namespace WpfApp1
{
    internal class SampRegistry
    {
        public string getPlayerNameFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\SAMP", RegistryKeyPermissionCheck.Default))
                {
                    if (key != null)
                    {
                        object name = key.GetValue("PlayerName");
                        if (name != null)
                        {
                            return name as string;
                        }
                    }
                }
            }
            catch (Exception ereg)
            {
                //MessageBox.Show(ereg.Message, "SA:MP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return "";
        }

        public string getGTAPathFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\SAMP", RegistryKeyPermissionCheck.Default))
                {
                    if (key != null)
                    {
                        object path = key.GetValue("gta_sa_exe");
                        if (path != null)
                        {
                            return path as string;
                        }
                    }
                }
            }
            catch (Exception ereg)
            {
                //MessageBox.Show(ereg.Message, "SA:MP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return "";
        }
    }
}
