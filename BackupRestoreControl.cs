using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SqlClient;
using System.Net;
using System.Net.NetworkInformation;

namespace MeroDokan
{
    public class BackupRestoreControl : UserControl
    {
        private Button btnBackup;
        private Button btnRestore;
        private Label lblStatus;
        private TextBox txtBackupPath;
        private Button btnBrowse;
        private string googleDriveAddress = "https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec";
        private string shopName = "MeroDokan";

        public BackupRestoreControl()
        {
            InitializeComponent();
            LoadBackupSettings();
        }

        private void LoadBackupSettings()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 BackupFolderPath, GoogleDriveAddress, ShopName FROM AppProfile", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                string path = rdr["BackupFolderPath"]?.ToString();
                                if (!string.IsNullOrEmpty(path))
                                {
                                    txtBackupPath.Text = path;
                                }

                                string drive = rdr["GoogleDriveAddress"]?.ToString();
                                if (!string.IsNullOrEmpty(drive))
                                {
                                    googleDriveAddress = drive;
                                }

                                string name = rdr["ShopName"]?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    shopName = name;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading backup settings: " + ex.Message);
            }
        }

        private bool IsInternetConnected()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                    return false;
                
                System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry("www.google.com");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FindLocalGoogleDrivePath()
        {
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.IsReady && drive.VolumeLabel.IndexOf("Google Drive", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string myDrive = Path.Combine(drive.RootDirectory.FullName, "My Drive");
                            if (Directory.Exists(myDrive))
                                return myDrive;

                            string gdFolder = Path.Combine(drive.RootDirectory.FullName, "Google Drive");
                            if (Directory.Exists(gdFolder))
                                return gdFolder;

                            return drive.RootDirectory.FullName;
                        }
                    }
                    catch { }
                }

                string[] drives = { "G:\\", "H:\\", "I:\\", "F:\\", "D:\\" };
                foreach (string d in drives)
                {
                    if (Directory.Exists(d))
                    {
                        string myDrive = Path.Combine(d, "My Drive");
                        if (Directory.Exists(myDrive))
                            return myDrive;
                        
                        string gdFolder = Path.Combine(d, "Google Drive");
                        if (Directory.Exists(gdFolder))
                            return gdFolder;
                    }
                }

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] commonPaths = {
                    Path.Combine(userProfile, "Google Drive", "My Drive"),
                    Path.Combine(userProfile, "Google Drive"),
                    Path.Combine(userProfile, "My Drive")
                };

                foreach (string p in commonPaths)
                {
                    if (Directory.Exists(p))
                        return p;
                }
            }
            catch { }
            return null;
        }

        private static void CompressFile(string sourceFile, string destinationFile)
        {
            using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream targetStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
                {
                    using (System.IO.Compression.GZipStream compressionStream = new System.IO.Compression.GZipStream(targetStream, System.IO.Compression.CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                    }
                }
            }
        }

#pragma warning disable SYSLIB0014
        private static string UploadToGoogleScript(string url, string jsonPayload)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
            }
            catch { }

            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            string currentUrl = url;
            string method = "POST";

            for (int i = 0; i < 5; i++)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(currentUrl);
                request.Method = method;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) MeroDokanClient";
                request.Timeout = 120000;
                request.AllowAutoRedirect = false;

                if (method == "POST")
                {
                    request.ContentType = "application/json";
                    request.ContentLength = data.Length;
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.Redirect || 
                            response.StatusCode == HttpStatusCode.MovedPermanently || 
                            response.StatusCode == HttpStatusCode.Found || 
                            response.StatusCode == HttpStatusCode.SeeOther || 
                            (int)response.StatusCode == 307)
                        {
                            string redirectUrl = response.Headers["Location"];
                            if (!string.IsNullOrEmpty(redirectUrl))
                            {
                                currentUrl = redirectUrl;
                                method = "GET";
                                continue;
                            }
                        }

                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (WebException webEx)
                {
                    if (webEx.Response is HttpWebResponse errResponse)
                    {
                        if (errResponse.StatusCode == HttpStatusCode.Redirect || 
                            errResponse.StatusCode == HttpStatusCode.MovedPermanently || 
                            errResponse.StatusCode == HttpStatusCode.Found || 
                            errResponse.StatusCode == HttpStatusCode.SeeOther || 
                            (int)errResponse.StatusCode == 307)
                        {
                            string redirectUrl = errResponse.Headers["Location"];
                            if (!string.IsNullOrEmpty(redirectUrl))
                            {
                                currentUrl = redirectUrl;
                                method = "GET";
                                continue;
                            }
                        }

                        using (StreamReader reader = new StreamReader(errResponse.GetResponseStream(), System.Text.Encoding.UTF8))
                        {
                            throw new Exception($"{webEx.Message} - Response: {reader.ReadToEnd()}");
                        }
                    }
                    throw;
                }
            }

            throw new Exception("Too many redirects");
        }
#pragma warning restore SYSLIB0014

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            bool isAdmin = string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            Label lblHeader = new Label();
            lblHeader.Text = "Database Maintenance & Backup Center";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Panel maintenanceCard = Theme.CreateCard(800, isAdmin ? 600 : 350);
            maintenanceCard.Location = new Point(20, 70);

            Label lblCardTitle = new System.Windows.Forms.Label();
            lblCardTitle.Text = "System Disaster Recovery Operations";
            lblCardTitle.Location = new Point(25, 25);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.SubHeaderFont);
            maintenanceCard.Controls.Add(lblCardTitle);

            Label lblDesc = new Label();
            lblDesc.Text = @"Database backups create an offline physical '.bak' copy of your complete store records. 
It is recommended to schedule weekly backups. Restoring a database will completely overwrite all existing database transactions, inventory listings, and sales with the selected backup snapshot.";
            lblDesc.Location = new Point(25, 75);
            lblDesc.Size = new Size(750, 60);
            Theme.StyleLabel(lblDesc, Theme.TextDark, Theme.MainFont);
            maintenanceCard.Controls.Add(lblDesc);

            Label lblStep1 = new Label();
            lblStep1.Text = "1. BACKUP DATABASE snapshot";
            lblStep1.Location = new Point(25, 160);
            Theme.StyleLabel(lblStep1, Theme.TextLight, Theme.BoldFont);
            maintenanceCard.Controls.Add(lblStep1);

            Label lblPath = new Label();
            lblPath.Text = "Choose target folder to write backup file:";
            lblPath.Location = new Point(25, 190);
            Theme.StyleLabel(lblPath, Theme.TextDark, Theme.MainFont);
            maintenanceCard.Controls.Add(lblPath);

            txtBackupPath = new TextBox();
            txtBackupPath.Size = new Size(580, 30);
            txtBackupPath.Location = new Point(25, 215);
            Theme.StyleTextBox(txtBackupPath);
            txtBackupPath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            maintenanceCard.Controls.Add(txtBackupPath);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Size = new Size(130, 36);
            btnBrowse.Location = new Point(620, 212);
            Theme.StyleSecondaryButton(btnBrowse);
            btnBrowse.Click += BtnBrowse_Click;
            maintenanceCard.Controls.Add(btnBrowse);

            btnBackup = new Button();
            btnBackup.Text = "💾 RUN DATABASE BACKUP";
            btnBackup.Size = new Size(270, 45);
            btnBackup.Location = new Point(25, 260);
            Theme.StyleSuccessButton(btnBackup);
            btnBackup.Click += BtnBackup_Click;
            maintenanceCard.Controls.Add(btnBackup);

            Button btnOpenFolder = new Button();
            btnOpenFolder.Text = "📂 Open Backup Folder";
            btnOpenFolder.Size = new Size(200, 45);
            btnOpenFolder.Location = new Point(310, 260);
            Theme.StyleSecondaryButton(btnOpenFolder);
            btnOpenFolder.Click += (s, e) =>
            {
                try
                {
                    string bDir = txtBackupPath.Text.Trim();
                    if (!Directory.Exists(bDir))
                    {
                        Directory.CreateDirectory(bDir);
                    }
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = bDir,
                        UseShellExecute = true
                    });
                }
                catch (Exception openEx)
                {
                    MessageBox.Show("Could not open folder: " + openEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            maintenanceCard.Controls.Add(btnOpenFolder);

            if (isAdmin)
            {
                Panel div = new Panel();
                div.Size = new Size(750, 1);
                div.Location = new Point(25, 335);
                div.BackColor = Theme.AlternateRow;
                maintenanceCard.Controls.Add(div);

                Label lblStep2 = new Label();
                lblStep2.Text = "2. RESTORE DATABASE from backup file (.bak)";
                lblStep2.Location = new Point(25, 360);
                Theme.StyleLabel(lblStep2, Theme.TextLight, Theme.BoldFont);
                maintenanceCard.Controls.Add(lblStep2);

                btnRestore = new Button();
                btnRestore.Text = "⏮️ SELECT & RESTORE DATABASE";
                btnRestore.Size = new Size(300, 45);
                btnRestore.Location = new Point(25, 400);
                Theme.StyleDangerButton(btnRestore);
                btnRestore.Click += BtnRestore_Click;
                maintenanceCard.Controls.Add(btnRestore);

                Panel div2 = new Panel();
                div2.Size = new Size(750, 1);
                div2.Location = new Point(25, 465);
                div2.BackColor = Theme.AlternateRow;
                maintenanceCard.Controls.Add(div2);

                Label lblStep3 = new Label();
                lblStep3.Text = "3. CLEAN RESET / RESET ALL TRANSACTIONS";
                lblStep3.Location = new Point(25, 485);
                Theme.StyleLabel(lblStep3, Theme.TextLight, Theme.BoldFont);
                maintenanceCard.Controls.Add(lblStep3);

                Button btnCleanReset = new Button();
                btnCleanReset.Text = "⚠️ RESET DATABASE TRANSACTIONS";
                btnCleanReset.Size = new Size(300, 45);
                btnCleanReset.Location = new Point(25, 520);
                Theme.StyleDangerButton(btnCleanReset);
                btnCleanReset.Click += BtnCleanReset_Click;
                maintenanceCard.Controls.Add(btnCleanReset);
            }

            lblStatus = new Label();
            lblStatus.Text = "System Status: Ready";
            lblStatus.Location = new Point(25, isAdmin ? 570 : 310);
            lblStatus.Size = new Size(750, 25);
            Theme.StyleLabel(lblStatus, Theme.Success, Theme.BoldFont);
            maintenanceCard.Controls.Add(lblStatus);

            this.Controls.Add(maintenanceCard);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select target directory for SQL database backup file";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            string backupDir = txtBackupPath.Text.Trim();
            if (string.IsNullOrEmpty(backupDir))
            {
                MessageBox.Show("Please specify a valid backup folder path.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string safeShopName = string.Join("_", shopName.Split(Path.GetInvalidFileNameChars()));
                safeShopName = safeShopName.Replace(" ", "_");

                string backupFileName = $"{safeShopName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullBackupPath = Path.Combine(backupDir, backupFileName);

                lblStatus.Text = "Status: Preparing database backup...";
                lblStatus.ForeColor = Theme.Warning;
                btnBackup.Enabled = false;
                if (btnRestore != null) btnRestore.Enabled = false;

                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    try
                    {
                        // Refresh connection strings to ensure LocalDB is running and the cached Named Pipe is valid
                        DatabaseHelper.ResolveConnectionStrings();

                        bool isLocal = true;
                        string serverBackupPath = "";
                        byte[] backupBytes = null;
                        bool downloadFailed = false;

                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();

                            try
                            {
                                using (SqlCommand cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('MachineName') AS VARCHAR(100))", conn))
                                {
                                    string serverMachine = cmd.ExecuteScalar()?.ToString();
                                    if (!string.IsNullOrEmpty(serverMachine))
                                    {
                                        isLocal = string.Equals(serverMachine, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
                                    }
                                }
                            }
                            catch { }

                            string currentDbName = DatabaseHelper.GetConfig()?.Database ?? "MeroDokanCafeDB";
                            if (isLocal)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = "Status: Backing up database locally...";
                                }));

                                string query = $"BACKUP DATABASE [{currentDbName}] TO DISK = @path WITH FORMAT, INIT";
                                using (SqlCommand cmd = new SqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", fullBackupPath);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = "Status: Running remote backup on server...";
                                }));

                                string serverBackupFolder = "";
                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand(@"
                                        DECLARE @BackupDir NVARCHAR(4000);
                                        EXEC master.dbo.xp_instance_regread
                                            N'HKEY_LOCAL_MACHINE',
                                            N'Software\Microsoft\MSSQLServer\MSSQLServer',
                                            N'BackupDirectory',
                                            @BackupDir OUTPUT;
                                        IF @BackupDir IS NULL
                                        BEGIN
                                            EXEC master.dbo.xp_instance_regread
                                                N'HKEY_LOCAL_MACHINE',
                                                N'Software\Microsoft\MSSQLServer\MSSQLServer\CurrentVersion',
                                                N'BackupDirectory',
                                                @BackupDir OUTPUT;
                                        END
                                        SELECT @BackupDir;", conn))
                                    {
                                        serverBackupFolder = cmd.ExecuteScalar()?.ToString();
                                    }
                                }
                                catch { }

                                if (string.IsNullOrEmpty(serverBackupFolder))
                                {
                                    try
                                    {
                                        using (SqlCommand cmd = new SqlCommand(@"
                                            SELECT physical_name 
                                            FROM sys.master_files 
                                            WHERE database_id = DB_ID(@dbname) AND type = 0;", conn))
                                        {
                                            cmd.Parameters.AddWithValue("@dbname", currentDbName);
                                            string mdfPath = cmd.ExecuteScalar()?.ToString();
                                            if (!string.IsNullOrEmpty(mdfPath))
                                            {
                                                serverBackupFolder = Path.GetDirectoryName(mdfPath);
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                if (string.IsNullOrEmpty(serverBackupFolder))
                                {
                                    serverBackupFolder = "C:\\Temp";
                                }

                                serverBackupPath = Path.Combine(serverBackupFolder, backupFileName);

                                string query = $"BACKUP DATABASE [{currentDbName}] TO DISK = @path WITH FORMAT, INIT";
                                using (SqlCommand cmd = new SqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", serverBackupPath);
                                    cmd.ExecuteNonQuery();
                                }

                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = "Status: Downloading backup from remote server...";
                                }));

                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand("SELECT BulkColumn FROM OPENROWSET(BULK @serverPath, SINGLE_BLOB) x", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@serverPath", serverBackupPath);
                                        cmd.CommandTimeout = 300;
                                        backupBytes = cmd.ExecuteScalar() as byte[];
                                    }
                                }
                                catch (Exception downloadEx)
                                {
                                    Console.WriteLine("Streaming backup failed: " + downloadEx.Message);
                                    downloadFailed = true;
                                }

                                if (!downloadFailed && backupBytes != null)
                                {
                                    File.WriteAllBytes(fullBackupPath, backupBytes);

                                    try
                                    {
                                        using (SqlCommand cmd = new SqlCommand(@"
                                            DECLARE @hr INT, @fso INT;
                                            EXEC @hr = sp_OACreate 'Scripting.FileSystemObject', @fso OUT;
                                            IF @hr = 0
                                            BEGIN
                                                EXEC sp_OAMethod @fso, 'DeleteFile', NULL, @filePath;
                                                EXEC sp_OADestroy @fso;
                                            END", conn))
                                        {
                                            cmd.Parameters.AddWithValue("@filePath", serverBackupPath);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }

                        if (downloadFailed)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = "Status: Backup saved on server (download blocked).";
                                lblStatus.ForeColor = Theme.Warning;

                                string msg = $"Database backup successfully compiled ON SERVER!\n\n" +
                                    $"📁 File Name: {backupFileName}\n" +
                                    $"📍 Server Location: {serverBackupPath}\n\n" +
                                    $"⚠️ Cloud Sync Alert:\n" +
                                    $"Because the database is running on a remote server, system security restrictions prevented downloading the backup directly to this client computer.\n\n" +
                                    $"To upload it to Google Drive:\n" +
                                    $"1. Copy the backup file from the server computer at the path shown above.\n" +
                                    $"2. Manually upload it to your Google Drive:\n" +
                                    $"   {googleDriveAddress}";

                                MessageBox.Show(msg, "Backup Saved on Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }));
                            return;
                        }

                        this.BeginInvoke(new Action(() =>
                        {
                            lblStatus.Text = "Status: Local file written. Verifying cloud folders...";
                        }));
                        System.Threading.Thread.Sleep(1000);

                        bool hasInternet = IsInternetConnected();
                        bool scriptUploaded = false;
                        string uploadErrorMessage = "";

                        if (hasInternet)
                        {
                            if (!string.IsNullOrEmpty(googleDriveAddress) && googleDriveAddress.StartsWith("https://script.google.com/macros/s/", StringComparison.OrdinalIgnoreCase))
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = "Status: Compressing database backup...";
                                }));

                                string gzippedPath = fullBackupPath + ".gz";
                                string gzippedFileName = backupFileName + ".gz";
                                
                                try
                                {
                                    CompressFile(fullBackupPath, gzippedPath);

                                    this.BeginInvoke(new Action(() =>
                                    {
                                        lblStatus.Text = "Status: Direct uploading to Google Drive...";
                                    }));

                                    byte[] fileBytes = File.ReadAllBytes(gzippedPath);
                                    string base64Data = Convert.ToBase64String(fileBytes);

                                    string jsonPayload = "{" +
                                        "\"filename\":\"" + gzippedFileName + "\"," +
                                        "\"mimeType\":\"application/gzip\"," +
                                        "\"bytes\":\"" + base64Data + "\"" +
                                        "}";

                                    string responseJson = UploadToGoogleScript(googleDriveAddress, jsonPayload);
                                    
                                    if (responseJson.Contains("\"status\":\"success\"") || responseJson.Contains("\"success\""))
                                    {
                                        scriptUploaded = true;
                                    }
                                    else
                                    {
                                        uploadErrorMessage = responseJson;
                                    }
                                }
                                catch (Exception uploadEx)
                                {
                                    uploadErrorMessage = uploadEx.Message;
                                }
                                finally
                                {
                                    try
                                    {
                                        if (File.Exists(gzippedPath))
                                        {
                                            File.Delete(gzippedPath);
                                        }
                                    }
                                    catch { }
                                }
                            }
                            
                            if (scriptUploaded)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    lblStatus.Text = "Status: Backup uploaded directly to Google Drive successfully!";
                                    lblStatus.ForeColor = Theme.Success;

                                    string successMsg = $"Database backup successfully uploaded directly to Google Drive!\n\n" +
                                        $"📁 File: {backupFileName}.gz\n" +
                                        $"📍 Local Copy Path: {backupDir}\n\n" +
                                        $"☁️ Cloud Upload Status: SUCCESS (Direct Upload)";

                                    MessageBox.Show(
                                        successMsg,
                                        "Google Drive Automated Backup",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                    );
                                }));
                                return;
                            }

                            this.BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = "Status: Cloud connection active. Checking local sync...";
                            }));
                            System.Threading.Thread.Sleep(1000);

                            string localGDrivePath = FindLocalGoogleDrivePath();
                            string automaticGdriveFile = "";
                            if (!string.IsNullOrEmpty(localGDrivePath))
                            {
                                try
                                {
                                    string targetDir = Path.Combine(localGDrivePath, "MeroDokanBackup");
                                    if (!Directory.Exists(targetDir))
                                    {
                                        Directory.CreateDirectory(targetDir);
                                    }
                                    automaticGdriveFile = Path.Combine(targetDir, backupFileName);
                                    File.Copy(fullBackupPath, automaticGdriveFile, true);
                                }
                                catch (Exception gDriveEx)
                                {
                                    Console.WriteLine("Failed to auto-copy to local GDrive: " + gDriveEx.Message);
                                }
                            }

                            this.BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = !string.IsNullOrEmpty(automaticGdriveFile)
                                    ? "Cloud Sync: Google Drive Desktop copy placed."
                                    : "Status: Database backup completed successfully!";
                                lblStatus.ForeColor = Theme.Success;

                                string successMsg = $"Database backup successfully created!\n\n" +
                                    $"📁 Local File: {backupFileName}\n" +
                                    $"📍 Local Path: {backupDir}\n\n";

                                if (!string.IsNullOrEmpty(automaticGdriveFile))
                                {
                                    successMsg += $"☁️ Google Drive Desktop Auto-Save: SUCCESS!\n" +
                                        $"A copy has been saved directly inside your Google Drive synced folder:\n" +
                                        $"    {automaticGdriveFile}\n\n" +
                                        $"Google Drive is automatically uploading this file to the cloud in the background.\n";
                                }
                                else
                                {
                                    successMsg += $"✅ Local backup is safely stored and ready.\n";
                                }

                                MessageBox.Show(
                                    successMsg,
                                    "Database Backup Completed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }));
                        }
                        else
                        {
                            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                            string systemDriveBackupDir = Path.Combine(systemDrive, "MeroDokan", "DailyDatabaseBackup");
                            string systemDriveBackupPath = Path.Combine(systemDriveBackupDir, backupFileName);
                            bool redundantSaved = false;

                            try
                            {
                                if (!string.Equals(Path.GetFullPath(backupDir).TrimEnd('\\'), Path.GetFullPath(systemDriveBackupDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!Directory.Exists(systemDriveBackupDir))
                                    {
                                        Directory.CreateDirectory(systemDriveBackupDir);
                                    }
                                    
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        lblStatus.Text = "Status: Connectivity issue detected. Saving copy to System Drive location...";
                                    }));
                                    System.Threading.Thread.Sleep(1200);

                                    File.Copy(fullBackupPath, systemDriveBackupPath, true);
                                    redundantSaved = true;
                                }
                            }
                            catch (Exception fileEx)
                            {
                                Console.WriteLine("System drive backup fallback failed: " + fileEx.Message);
                            }

                            string localGDrivePath = FindLocalGoogleDrivePath();
                            string automaticGdriveFile = "";
                            if (!string.IsNullOrEmpty(localGDrivePath))
                            {
                                try
                                {
                                    string targetDir = Path.Combine(localGDrivePath, "MeroDokanBackup");
                                    if (!Directory.Exists(targetDir))
                                    {
                                        Directory.CreateDirectory(targetDir);
                                    }
                                    automaticGdriveFile = Path.Combine(targetDir, backupFileName);
                                    File.Copy(fullBackupPath, automaticGdriveFile, true);
                                }
                                catch (Exception gDriveEx)
                                {
                                    Console.WriteLine("Failed to auto-copy to local GDrive offline: " + gDriveEx.Message);
                                }
                            }

                            this.BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = redundantSaved 
                                    ? $"Saved locally & redundant copy placed at {systemDriveBackupDir}"
                                    : $"Local backup complete. Cloud sync skipped (system offline).";
                                lblStatus.ForeColor = Theme.Warning;

                                string messageText = $"Database backup successfully compiled!\n\n" +
                                    $"📁 Configured Location: {backupFileName}\n" +
                                    $"📍 Path: {backupDir}\n\n" +
                                    $"☁️ Cloud Sync Status: SKIPPED (Offline)\n\n";

                                if (!string.IsNullOrEmpty(automaticGdriveFile))
                                {
                                    messageText += $"🛡️ Google Drive Offline Queue: SUCCESS!\n" +
                                        $"A backup copy has been placed in your local Google Drive folder:\n" +
                                        $"    {automaticGdriveFile}\n\n" +
                                        $"It will automatically sync to your Google Drive cloud storage the moment your internet connection is restored!\n\n";
                                }

                                if (redundantSaved)
                                {
                                    messageText += $"🛡️ Local Redundant Backup: copy successfully stored on System Drive at:\n    {systemDriveBackupPath}";
                                }
                                else
                                {
                                    messageText += $"⚠️ Warning: System is offline. Local backup created in the configured folder.";
                                }

                                MessageBox.Show(
                                    messageText,
                                    "Backup Complete (Offline Redundancy)",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            lblStatus.Text = "Status: Backup operation failed.";
                            lblStatus.ForeColor = Theme.Danger;
                            MessageBox.Show($"SQL Backup Error: {ex.Message}\nNote: SQL Server requires folders to be write-accessible by SQL Server engine.", "Disaster Recovery Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                    finally
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            btnBackup.Enabled = true;
                            if (btnRestore != null) btnRestore.Enabled = true;
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Status: Backup operation failed.";
                lblStatus.ForeColor = Theme.Danger;
                MessageBox.Show($"SQL Backup Error: {ex.Message}", "Disaster Recovery Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnBackup.Enabled = true;
                if (btnRestore != null) btnRestore.Enabled = true;
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Backup Files (*.bak)|*.bak";
                ofd.Title = "Select Database Backup Snapshot File";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string backupFile = ofd.FileName;

                    // Refresh connection strings to ensure LocalDB is running and the cached Named Pipe is valid
                    DatabaseHelper.ResolveConnectionStrings();

                    bool isLocal = true;
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlCommand cmd = new SqlCommand("SELECT CAST(SERVERPROPERTY('MachineName') AS VARCHAR(100))", conn))
                            {
                                string serverMachine = cmd.ExecuteScalar()?.ToString();
                                if (!string.IsNullOrEmpty(serverMachine))
                                {
                                    isLocal = string.Equals(serverMachine, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
                                }
                            }
                        }
                    }
                    catch { }

                    if (!isLocal)
                    {
                        MessageBox.Show(
                            "Because the database is running on a remote server, the database backup file must be restored directly on the server computer.\n\n" +
                            "1. Copy your backup file (.bak) to the server computer.\n" +
                            "2. Run the application directly on the server computer to perform the select & restore operation.",
                            "Remote Restore Restricted",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }

                    DialogResult confirm = MessageBox.Show(
                        "WARNING!\n\nRestoring will completely overwrite the existing database. Are you absolutely certain you want to proceed?",
                        "Confirm Database Restore",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (confirm == DialogResult.Yes)
                    {
                        lblStatus.Text = "Restoring database snapshot...";
                        lblStatus.ForeColor = Theme.Warning;

                        string masterConnString = DatabaseHelper.MasterConnectionString;
                        
                        try
                        {
                            SqlConnection.ClearAllPools();

                            using (SqlConnection conn = new SqlConnection(masterConnString))
                            {
                                conn.Open();
                                string currentDbName = DatabaseHelper.GetConfig()?.Database ?? "MeroDokanCafeDB";

                                // 1. Query current physical paths of the database if it exists
                                string currentMdfPath = null;
                                string currentLdfPath = null;
                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand("SELECT name, physical_name FROM sys.master_files WHERE database_id = DB_ID(@dbname)", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@dbname", currentDbName);
                                        using (SqlDataReader rdr = cmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                string name = rdr["name"].ToString();
                                                string physicalPath = rdr["physical_name"].ToString();
                                                if (physicalPath.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
                                                    currentMdfPath = physicalPath;
                                                else if (physicalPath.EndsWith(".ldf", StringComparison.OrdinalIgnoreCase))
                                                    currentLdfPath = physicalPath;
                                            }
                                        }
                                    }
                                }
                                catch { }

                                // If the database does not exist, get the directory of the master database files
                                if (string.IsNullOrEmpty(currentMdfPath) || string.IsNullOrEmpty(currentLdfPath))
                                {
                                    try
                                    {
                                        string masterPhysicalPath = null;
                                        using (SqlCommand cmd = new SqlCommand("SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID('master') AND file_id = 1", conn))
                                        {
                                            masterPhysicalPath = cmd.ExecuteScalar()?.ToString();
                                        }
                                        if (!string.IsNullOrEmpty(masterPhysicalPath))
                                        {
                                            string masterDir = Path.GetDirectoryName(masterPhysicalPath);
                                            currentMdfPath = Path.Combine(masterDir, $"{currentDbName}.mdf");
                                            currentLdfPath = Path.Combine(masterDir, $"{currentDbName}_log.ldf");
                                        }
                                    }
                                    catch { }
                                }

                                // Default fallback in case directory detection fails
                                if (string.IsNullOrEmpty(currentMdfPath))
                                {
                                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                    currentMdfPath = Path.Combine(userProfile, $"{currentDbName}.mdf");
                                    currentLdfPath = Path.Combine(userProfile, $"{currentDbName}_log.ldf");
                                }

                                // 2. Query file list from the backup file to get logical names and build MOVE clauses
                                var moveClauses = new System.Collections.Generic.List<string>();
                                using (SqlCommand cmd = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @path", conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", backupFile);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        int dataCount = 0;
                                        int logCount = 0;
                                        while (rdr.Read())
                                        {
                                            string logicalName = rdr["LogicalName"].ToString();
                                            string type = rdr["Type"].ToString(); // 'D', 'L', etc.
                                            
                                            string targetPath = null;
                                            if (type.Equals("D", StringComparison.OrdinalIgnoreCase))
                                            {
                                                string suffix = dataCount == 0 ? "" : dataCount.ToString();
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentMdfPath), $"{currentDbName}{suffix}.mdf");
                                                dataCount++;
                                            }
                                            else if (type.Equals("L", StringComparison.OrdinalIgnoreCase))
                                            {
                                                string suffix = logCount == 0 ? "" : logCount.ToString();
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentLdfPath), $"{currentDbName}{suffix}_log.ldf");
                                                logCount++;
                                            }
                                            else
                                            {
                                                // Other files, e.g. filestream
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentMdfPath), Path.GetFileName(rdr["PhysicalName"].ToString()));
                                            }

                                            moveClauses.Add($"MOVE '{logicalName}' TO '{targetPath}'");
                                        }
                                    }
                                }

                                // 3. Build restore query
                                string restoreSql = $"ALTER DATABASE [{currentDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\n";
                                restoreSql += $"RESTORE DATABASE [{currentDbName}] FROM DISK = @path WITH REPLACE";
                                if (moveClauses.Count > 0)
                                {
                                    restoreSql += ",\n" + string.Join(",\n", moveClauses);
                                }
                                restoreSql += $";\nALTER DATABASE [{currentDbName}] SET MULTI_USER;";

                                using (SqlCommand cmd = new SqlCommand(restoreSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", backupFile);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            lblStatus.Text = "Success: Database restored successfully.";
                            lblStatus.ForeColor = Theme.Success;
                            MessageBox.Show("Database snapshot restored successfully! The application will refresh connection details.", "Recovery Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            lblStatus.Text = "Status: Restore operation failed.";
                            lblStatus.ForeColor = Theme.Danger;
                            MessageBox.Show($"SQL Restore Error: {ex.Message}", "Disaster Recovery Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void BtnCleanReset_Click(object sender, EventArgs e)
        {
            DialogResult confirm1 = MessageBox.Show(
                "CRITICAL WARNING!\n\nThis operation will permanently delete ALL transactional data including Sales, Purchases, Returns, Payments, and Daily Settlements.\n\n" +
                "Your product catalog (names, codes), customer list, supplier list, and user accounts will be preserved, but stock levels will be reset to 0.\n\n" +
                "Are you absolutely sure you want to perform a Clean Reset?",
                "Database Reset Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop
            );

            if (confirm1 == DialogResult.Yes)
            {
                using (Form prompt = new Form())
                {
                    prompt.Width = 350;
                    prompt.Height = 180;
                    prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                    prompt.Text = "Confirm Database Reset";
                    prompt.StartPosition = FormStartPosition.CenterParent;
                    prompt.BackColor = Theme.Primary;

                    Label textLabel = new Label() { Left = 20, Top = 20, Width = 300, Height = 40, Text = "Please type 'RESET' (case-sensitive) to confirm the database wipe:" };
                    Theme.StyleLabel(textLabel, Theme.TextLight, Theme.BoldFont);
                    
                    TextBox textBox = new TextBox() { Left = 20, Top = 65, Width = 290 };
                    Theme.StyleTextBox(textBox);
                    
                    Button confirmation = new Button() { Text = "WIPE TRANSACTIONS", Left = 20, Width = 140, Top = 100, DialogResult = DialogResult.OK };
                    Theme.StyleDangerButton(confirmation);
                    
                    Button cancel = new Button() { Text = "Cancel", Left = 170, Width = 140, Top = 100, DialogResult = DialogResult.Cancel };
                    Theme.StyleSecondaryButton(cancel);

                    prompt.Controls.Add(textLabel);
                    prompt.Controls.Add(textBox);
                    prompt.Controls.Add(confirmation);
                    prompt.Controls.Add(cancel);
                    prompt.AcceptButton = confirmation;

                    if (prompt.ShowDialog() == DialogResult.OK)
                    {
                        if (textBox.Text.Trim() == "RESET")
                        {
                            try
                            {
                                // Refresh connection strings in case LocalDB went to sleep
                                DatabaseHelper.ResolveConnectionStrings();

                                lblStatus.Text = "Status: Performing database clean reset...";
                                lblStatus.ForeColor = Theme.Warning;

                                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                                {
                                    conn.Open();
                                    using (SqlTransaction trans = conn.BeginTransaction())
                                    {
                                        try
                                        {
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM SalesReturnDetails", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM SalesReturns", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM SaleDetails", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM CustomerPayments", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Sales", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Appointments", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM PurchaseDetails", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Purchases", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM DailySettlements", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM ProductPriceHistory", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM StockMovements", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM KOTDetails", conn, trans)) cmd.ExecuteNonQuery();
                                            using (SqlCommand cmd = new SqlCommand("DELETE FROM KOTMaster", conn, trans)) cmd.ExecuteNonQuery();

                                            // Reset all Cafe Tables to Available and remove shared sub-tables
                                            using (SqlCommand cmd = new SqlCommand(@"
                                                UPDATE CafeTables 
                                                SET Status = 'Available', CurrentBillAmount = 0.00, OrderStartTime = NULL, BilledTime = NULL,
                                                    ActiveKotNumbers = NULL, ActiveSaleId = NULL, CurrentSteward = NULL;
                                                DELETE FROM CafeTables WHERE CHARINDEX('-', TableNumber) > 0;
                                            ", conn, trans)) cmd.ExecuteNonQuery();

                                            using (SqlCommand cmd = new SqlCommand("UPDATE Products SET Stock = 0", conn, trans)) cmd.ExecuteNonQuery();

                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('Appointments', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('Sales', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('SaleDetails', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('Purchases', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('PurchaseDetails', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('DailySettlements', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('StockMovements', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('KOTDetails', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('KOTMaster', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('SalesReturns', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('SalesReturnDetails', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }
                                            try { using (SqlCommand cmd = new SqlCommand("DBCC CHECKIDENT ('CustomerPayments', RESEED, 0)", conn, trans)) cmd.ExecuteNonQuery(); } catch { }

                                            trans.Commit();
                                        }
                                        catch
                                        {
                                            trans.Rollback();
                                            throw;
                                        }
                                    }
                                }

                                lblStatus.Text = "Status: Database clean reset completed successfully.";
                                lblStatus.ForeColor = Theme.Success;
                                MessageBox.Show("Database transaction history has been wiped and reset successfully. Product stock levels are reset to 0.", "Reset Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                lblStatus.Text = "Status: Reset failed.";
                                lblStatus.ForeColor = Theme.Danger;
                                MessageBox.Show($"Error during clean reset: {ex.Message}", "Database Reset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Incorrect confirmation word. Reset aborted.", "Action Aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
        }
    }
}
