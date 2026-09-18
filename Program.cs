using System;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Customizes application configurations (high DPI settings, default fonts, etc.)
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /* ================= WINDOWS 7 SP1 COMPATIBILITY CHANGE =================
               On Windows 7 + .NET Framework 4.8, TLS 1.2 is not enabled by default,
               so the Google Drive backup upload (HTTPS) would fail with
               "The request was aborted: Could not create SSL/TLS secure channel".
               The line below enables it. It is safe to keep when reverting to
               .NET 8 as well - simply delete this try/catch block if desired.
               ===================================================================== */
            try
            {
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            }
            catch { }
            /* ================= END WINDOWS 7 SP1 COMPATIBILITY CHANGE ================= */

            // Apply default theme first so the Activation UI is styled correctly
            Theme.ApplyThemePreset("Dark Slate");

            // Ensure database is initialized before license check, loading themes or starting login
            bool dbInitialized = false;
            while (!dbInitialized)
            {
                try
                {
                    DatabaseHelper.InitializeDatabase();
                    dbInitialized = true;
                }
                catch (Exception ex)
                {
                    var result = MessageBox.Show(
                        "Failed to connect and initialize database:\n" + ex.Message + "\n\nWould you like to configure the database connection settings?",
                        "Database Connection Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error
                    );

                    if (result == DialogResult.Yes)
                    {
                        using (var configForm = new DatabaseConfigForm())
                        {
                            if (configForm.ShowDialog() != DialogResult.OK)
                            {
                                return; // Terminate app if user cancels config
                            }
                        }
                    }
                    else
                    {
                        return; // Terminate app if user chooses No
                    }
                }
            }

            // Attempt to load the user's saved theme from database if it exists
            try
            {
                LoadSavedThemePreset();
            }
            catch { }

            // Verify License before loading the application or login screens
            if (!LicenseManager.IsLicenseValid())
            {
                using (ActivationForm activation = new ActivationForm())
                {
                    if (activation.ShowDialog() != DialogResult.OK)
                    {
                        return; // Terminate app if closed or failed activation
                    }
                }
            }

            // Restartable Login Loop
            bool restart = true;
            while (restart)
            {
                restart = false;
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        MainForm main = new MainForm();
                        Application.Run(main);
                        
                        // If user logged out, MainForm returns Retry - trigger login prompt again
                        if (main.DialogResult == DialogResult.Retry)
                        {
                            restart = true;
                        }
                    }
                }
            }
        }

        private static void LoadSavedThemePreset()
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT TOP 1 ThemePreset, FontSizePreset FROM AppProfile", conn))
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                string theme = rdr["ThemePreset"].ToString();
                                string fontSize = rdr["FontSizePreset"]?.ToString() ?? "Medium";

                                Theme.ApplyThemePreset(theme);
                                Theme.ApplyFontSizePreset(fontSize);
                            }
                        }
                    }
                }
            }
            catch { }
        }    
    }
}