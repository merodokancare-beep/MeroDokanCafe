using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace MeroDokan
{
    public static class LicenseManager
    {
        private const string LicenseFileName = "license.lic";
        private const string SecretSalt = "MeroDokanLicensingSecureSalt2026#@!";

        // Generates the unique Hardware ID of the machine
        public static string GetHardwareId()
        {
            try
            {
                string macAddress = GetActiveMacAddress();
                string machineName = Environment.MachineName;
                string rawId = $"MeroDokan-{macAddress}-{machineName}";

                // Generate SHA-256 hash of the raw machine data
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        sb.Append(bytes[i].ToString("X2"));
                    }

                    // Take the first 16 characters and format it as MDKN-XXXX-XXXX-XXXX
                    string hash = sb.ToString();
                    return $"MDKN-{hash.Substring(0, 4)}-{hash.Substring(4, 4)}-{hash.Substring(8, 4)}-{hash.Substring(12, 4)}";
                }
            }
            catch
            {
                // Fallback in case of networking issues
                return "MDKN-FALL-BACK-SAFE-1234";
            }
        }

        // Helper to find the MAC address of the active network card, or fallback to any physical card
        private static string GetActiveMacAddress()
        {
            try
            {
                // First, try to find an active (Up) network card
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up && 
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            return mac;
                        }
                    }
                }

                // If offline or no active network, find the first physical network card (even if Down)
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                        nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        string mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            return mac;
                        }
                    }
                }
            }
            catch { }
            return "NOMACADDRESS";
        }

        // Helper to retrieve all physical MAC addresses on the system for candidate hardware ID checking
        private static System.Collections.Generic.List<string> GetAllMacAddresses()
        {
            var macs = new System.Collections.Generic.List<string>();
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    string mac = nic.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(mac) && !macs.Contains(mac))
                    {
                        macs.Add(mac);
                    }
                }
            }
            catch { }

            // Ensure NOMACADDRESS fallback is always present in candidate list
            if (!macs.Contains("NOMACADDRESS"))
            {
                macs.Add("NOMACADDRESS");
            }
            return macs;
        }

        // Generates all candidate Hardware IDs for the current machine based on all available network cards
        public static System.Collections.Generic.List<string> GetCandidateHardwareIds()
        {
            var ids = new System.Collections.Generic.List<string>();
            string machineName = Environment.MachineName;
            
            foreach (string mac in GetAllMacAddresses())
            {
                string rawId = $"MeroDokan-{mac}-{machineName}";
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        sb.Append(bytes[i].ToString("X2"));
                    }

                    string hash = sb.ToString();
                    string hwid = $"MDKN-{hash.Substring(0, 4)}-{hash.Substring(4, 4)}-{hash.Substring(8, 4)}-{hash.Substring(12, 4)}";
                    if (!ids.Contains(hwid))
                    {
                        ids.Add(hwid);
                    }
                }
            }
            return ids;
        }

        // Generates a Product Key for a specific Hardware ID and Expiry Date (For Developer Use)
        public static string GenerateProductKey(string hardwareId, string expiryCode)
        {
            // Clean inputs
            hardwareId = hardwareId.Replace(" ", "").Replace("-", "").ToUpper();
            expiryCode = expiryCode.Replace(" ", "").Replace("-", "").ToUpper(); // "LIFE" or "YYYYMMDD"

            string rawToSign = $"{hardwareId}|{expiryCode}|{SecretSalt}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToSign));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("X2"));
                }

                // Take first 12 characters of the hash
                string sig = sb.ToString().Substring(0, 12);
                
                // Format signature as XXXX-XXXX-XXXX
                string formattedSig = $"{sig.Substring(0, 4)}-{sig.Substring(4, 4)}-{sig.Substring(8, 4)}";
                
                return $"{expiryCode}-{formattedSig}";
            }
        }

        // Validates a Product Key for the CURRENT machine
        public static bool ValidateProductKey(string productKey, out string message, out DateTime expiryDate)
        {
            message = "Invalid key format.";
            expiryDate = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(productKey))
            {
                message = "Product key is empty.";
                return false;
            }

            // Clean the product key
            productKey = productKey.Trim().ToUpper();
            string[] parts = productKey.Split('-');
            
            if (parts.Length != 4)
            {
                message = "Invalid product key format. Key should be in 4 blocks (e.g., LIFE-XXXX-XXXX-XXXX).";
                return false;
            }

            string expiryCode = parts[0];
            string sigBlock1 = parts[1];
            string sigBlock2 = parts[2];
            string sigBlock3 = parts[3];
            string sigPart = sigBlock1 + sigBlock2 + sigBlock3;

            // Determine expiry date
            if (expiryCode == "LIFE")
            {
                expiryDate = DateTime.MaxValue;
            }
            else
            {
                if (expiryCode.Length == 8 && 
                    DateTime.TryParseExact(expiryCode, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    expiryDate = parsedDate;
                }
                else
                {
                    message = "Invalid expiry code inside key.";
                    return false;
                }
            }

            // Verify signature against all candidate hardware IDs
            var candidateIds = GetCandidateHardwareIds();
            bool anyMatch = false;

            foreach (string candidateId in candidateIds)
            {
                string cleanHwid = candidateId.Replace("-", "").ToUpper();
                string rawToSign = $"{cleanHwid}|{expiryCode}|{SecretSalt}";
                string expectedSig = "";
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToSign));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        sb.Append(bytes[i].ToString("X2"));
                    }
                    expectedSig = sb.ToString().Substring(0, 12);
                }

                if (sigPart == expectedSig)
                {
                    anyMatch = true;
                    break;
                }
            }

            if (!anyMatch)
            {
                message = "License key signature verification failed. The key is invalid or for another machine.";
                return false;
            }

            // Check if expired
            if (DateTime.Today > expiryDate)
            {
                message = $"The license expired on {expiryDate.ToString("yyyy-MM-dd")}.";
                return false;
            }

            message = "License is valid and active.";
            return true;
        }

        // Helper to get the correct path to the license file, supporting AppData and legacy base directory
        private static string GetLicensePath()
        {
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MeroDokan"
            );
            
            string primaryPath = Path.Combine(appDataFolder, LicenseFileName);
            string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);

            // Copy legacy file to LocalApplicationData if it exists there but not in AppData yet
            if (File.Exists(legacyPath) && !File.Exists(primaryPath))
            {
                try
                {
                    if (!Directory.Exists(appDataFolder))
                    {
                        Directory.CreateDirectory(appDataFolder);
                    }
                    File.Copy(legacyPath, primaryPath, true);
                }
                catch { }
            }

            return File.Exists(primaryPath) ? primaryPath : legacyPath;
        }

        // Checks if a valid license is saved locally
        public static bool IsLicenseValid()
        {
            string licensePath = GetLicensePath();
            if (!File.Exists(licensePath))
            {
                return false;
            }

            try
            {
                string key = File.ReadAllText(licensePath).Trim();
                return ValidateProductKey(key, out _, out _);
            }
            catch
            {
                return false;
            }
        }

        // Saves the valid license key
        public static void SaveLicenseKey(string productKey)
        {
            // Primary save: AppData folder
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MeroDokan"
            );
            try
            {
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }
                string appDataPath = Path.Combine(appDataFolder, LicenseFileName);
                File.WriteAllText(appDataPath, productKey.Trim());
            }
            catch { }

            // Secondary save: Base Directory (legacy, backup, ignore permission errors)
            try
            {
                string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
                File.WriteAllText(legacyPath, productKey.Trim());
            }
            catch { }
        }

        // Clears the saved license (useful for reset or changing keys)
        public static void ClearLicense()
        {
            // Clear AppData license
            try
            {
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MeroDokan"
                );
                string appDataPath = Path.Combine(appDataFolder, LicenseFileName);
                if (File.Exists(appDataPath))
                {
                    File.Delete(appDataPath);
                }
            }
            catch { }

            // Clear legacy license
            try
            {
                string legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
                if (File.Exists(legacyPath))
                {
                    File.Delete(legacyPath);
                }
            }
            catch { }
        }
    }
}
