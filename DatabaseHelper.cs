using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;

namespace MeroDokan
{
    public static class DatabaseHelper
    {
        public class DbConfig
        {
            public string Server { get; set; } = "(localdb)\\MSSQLLocalDB";
            public string Database { get; set; } = "MeroDokanCafeDB";
            public bool IntegratedSecurity { get; set; } = true;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public int ConnectionTimeout { get; set; } = 30;
            public int ConnectRetryCount { get; set; } = 3;
            public int ConnectRetryInterval { get; set; } = 10;
        }

        private static DbConfig _cachedConfig = null;
        private static string _cachedLocalDbServer = null;
        private static string _cachedLocalDbPipe = null;
        private static DateTime _lastResolvedTime = DateTime.MinValue;

        public static DbConfig GetConfig()
        {
            return GetCachedConfig();
        }

        private static DbConfig GetCachedConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = LoadConfig();
            }
            return _cachedConfig;
        }

        public static string ConnectionString
        {
            get
            {
                return BuildConnectionString(GetCachedConfig());
            }
            set
            {
            }
        }

        public static string MasterConnectionString
        {
            get
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(ConnectionString);
                    builder.InitialCatalog = "master";
                    return builder.ConnectionString;
                }
                catch
                {
                    return "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;";
                }
            }
            set
            {
            }
        }

        public static string GetConfigFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localFile = Path.Combine(appDir, "dbconfig.txt");
            try
            {
                // Test write permissions
                string testFile = Path.Combine(appDir, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return localFile;
            }
            catch
            {
                // Fallback to LocalApplicationData
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeroDokan");
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
                return Path.Combine(appDataDir, "dbconfig.txt");
            }
        }

        public static DbConfig LoadConfig()
        {
            var config = new DbConfig();
            string path = GetConfigFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || !line.Contains("="))
                            continue;
                        
                        int idx = line.IndexOf('=');
                        string key = line.Substring(0, idx).Trim();
                        string val = line.Substring(idx + 1).Trim();

                        if (key.Equals("Server", StringComparison.OrdinalIgnoreCase)) config.Server = val;
                        else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase)) config.Database = val;
                        else if (key.Equals("IntegratedSecurity", StringComparison.OrdinalIgnoreCase)) config.IntegratedSecurity = bool.Parse(val);
                        else if (key.Equals("Username", StringComparison.OrdinalIgnoreCase)) config.Username = val;
                        else if (key.Equals("Password", StringComparison.OrdinalIgnoreCase)) config.Password = val;
                        else if (key.Equals("ConnectionTimeout", StringComparison.OrdinalIgnoreCase)) config.ConnectionTimeout = int.Parse(val);
                        else if (key.Equals("ConnectRetryCount", StringComparison.OrdinalIgnoreCase)) config.ConnectRetryCount = int.Parse(val);
                        else if (key.Equals("ConnectRetryInterval", StringComparison.OrdinalIgnoreCase)) config.ConnectRetryInterval = int.Parse(val);
                    }
                }
                catch { }
            }
            else
            {
                config.Server = ResolveFirstRunServer();
                SaveConfig(config);
            }
            return config;
        }

        public static void SaveConfig(DbConfig config)
        {
            try
            {
                string path = GetConfigFilePath();
                var sb = new StringBuilder();
                sb.AppendLine("Server=" + config.Server);
                sb.AppendLine("Database=" + config.Database);
                sb.AppendLine("IntegratedSecurity=" + config.IntegratedSecurity.ToString());
                sb.AppendLine("Username=" + config.Username);
                sb.AppendLine("Password=" + config.Password);
                sb.AppendLine("ConnectionTimeout=" + config.ConnectionTimeout.ToString());
                sb.AppendLine("ConnectRetryCount=" + config.ConnectRetryCount.ToString());
                sb.AppendLine("ConnectRetryInterval=" + config.ConnectRetryInterval.ToString());
                File.WriteAllText(path, sb.ToString());
                
                _cachedConfig = config;

                // Discard stale connections
                SqlConnection.ClearAllPools();
            }
            catch { }
        }

        private static void LoadConnectionString()
        {
            GetCachedConfig();
        }

        private static bool? _isConnectRetrySupported = null;
        public static bool IsConnectRetrySupported
        {
            get
            {
                if (!_isConnectRetrySupported.HasValue)
                {
                    _isConnectRetrySupported = TestKeywordSupport("Connect Retry Count");
                }
                return _isConnectRetrySupported.Value;
            }
        }

        private static bool TestKeywordSupport(string keyword)
        {
            try
            {
                using (var conn = new SqlConnection("Server=dummy;" + keyword + "=1;"))
                {
                    string s = conn.ConnectionString;
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static string BuildConnectionString(DbConfig config)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder();
                builder.DataSource = ResolveLocalDbServerName(config.Server);
                builder.InitialCatalog = config.Database;
                builder.IntegratedSecurity = config.IntegratedSecurity;
                if (!config.IntegratedSecurity)
                {
                    builder.UserID = config.Username;
                    builder.Password = config.Password;
                }
                builder.ConnectTimeout = config.ConnectionTimeout;
                
                string connStr = builder.ConnectionString;
                if (!connStr.EndsWith(";"))
                    connStr += ";";
                
                connStr += "Encrypt=False;TrustServerCertificate=True;";
                
                if (IsConnectRetrySupported)
                {
                    connStr += "Connect Retry Count=" + config.ConnectRetryCount + ";";
                    connStr += "Connect Retry Interval=" + config.ConnectRetryInterval + ";";
                }
                
                return connStr;
            }
            catch
            {
                return "Server=(localdb)\\MSSQLLocalDB;Database=MeroDokanCafeDB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
            }
        }

        private static string FindSqlLocalDBPath()
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    proc.WaitForExit(1000);
                    return "sqllocaldb";
                }
            }
            catch { }

            var searchFolders = new System.Collections.Generic.List<string>();
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            string[] versions = { "160", "150", "140", "130", "120", "110" };
            foreach (var ver in versions)
            {
                if (!string.IsNullOrEmpty(pf))
                {
                    searchFolders.Add(Path.Combine(pf, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
                if (!string.IsNullOrEmpty(pf86))
                {
                    searchFolders.Add(Path.Combine(pf86, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
            }

            foreach (var folder in searchFolders)
            {
                string fullPath = Path.Combine(folder, "SqlLocalDB.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static System.Collections.Generic.List<string> GetLocalDBInstances(string localDbPath)
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("Microsoft"))
                            {
                                list.Add(trimmed);
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static void GetLocalDBInfo(string localDbPath, string instanceName, out string state, out string pipeName)
        {
            state = "Stopped";
            pipeName = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info \"" + instanceName + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("State:", StringComparison.OrdinalIgnoreCase))
                            {
                                int idx = trimmed.IndexOf(':');
                                if (idx != -1)
                                {
                                    state = trimmed.Substring(idx + 1).Trim();
                                }
                            }
                            int pipeIdx = trimmed.IndexOf("np:", StringComparison.OrdinalIgnoreCase);
                            if (pipeIdx != -1)
                            {
                                pipeName = trimmed.Substring(pipeIdx).Trim();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static string GetLocalDBDiagnostics()
        {
            var sb = new StringBuilder();
            string localDbPath = FindSqlLocalDBPath();
            if (string.IsNullOrEmpty(localDbPath))
            {
                sb.AppendLine("sqllocaldb utility not found in PATH or standard Program Files folders.");
                return sb.ToString();
            }

            sb.AppendLine("sqllocaldb executable path: " + localDbPath);

            // Run sqllocaldb -v
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(2000);
                    sb.AppendLine("Version: " + stdout.Trim() + " " + stderr.Trim());
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to run version check: " + ex.Message);
            }

            // Run sqllocaldb info
            System.Collections.Generic.List<string> instances = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(2000);
                    sb.AppendLine("Instances:\n" + stdout.Trim() + " " + stderr.Trim());

                    instances = new System.Collections.Generic.List<string>();
                    string[] lines = stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("Microsoft"))
                        {
                            instances.Add(trimmed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to run info check: " + ex.Message);
            }

            // Run sqllocaldb info <instance>
            if (instances != null && instances.Count > 0)
            {
                foreach (var inst in instances)
                {
                    try
                    {
                        var info = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = localDbPath,
                            Arguments = "info \"" + inst + "\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        using (var proc = System.Diagnostics.Process.Start(info))
                        {
                            string stdout = proc.StandardOutput.ReadToEnd();
                            string stderr = proc.StandardError.ReadToEnd();
                            proc.WaitForExit(2000);
                            sb.AppendLine("\nInstance Details (" + inst + "):\n" + stdout.Trim() + " " + stderr.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("Failed to run info for " + inst + ": " + ex.Message);
                    }
                }
            }

            return sb.ToString();
        }

        private static void TryStartLocalDB()
        {
            string localDbPath = FindSqlLocalDBPath();
            if (string.IsNullOrEmpty(localDbPath))
            {
                return;
            }

            var instances = GetLocalDBInstances(localDbPath);
            
            // Ensure default instances are created if not present
            string[] defaultInstances = { "MSSQLLocalDB", "v11.0" };
            foreach (var defaultInst in defaultInstances)
            {
                bool exists = false;
                foreach (var inst in instances)
                {
                    if (string.Equals(inst, defaultInst, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    try
                    {
                        var createInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = localDbPath,
                            Arguments = "create " + defaultInst,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        using (var proc = System.Diagnostics.Process.Start(createInfo))
                        {
                            proc.WaitForExit(5000);
                        }
                    }
                    catch { }
                }
            }

            // Refresh instances list
            instances = GetLocalDBInstances(localDbPath);

            foreach (var instance in instances)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = localDbPath,
                        Arguments = "start \"" + instance + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };
                    using (var proc = System.Diagnostics.Process.Start(startInfo))
                    {
                        proc.WaitForExit(3000);
                    }
                }
                catch
                {
                }
            }
        }

        public static void ResolveConnectionStrings()
        {
            _cachedLocalDbPipe = null;
            _lastResolvedTime = DateTime.MinValue;
            GetCachedConfig();
        }

        public static string ResolveFirstRunServer()
        {
            TryStartLocalDB();

            var serverList = new System.Collections.Generic.List<string>();

            // 1. Add dynamically discovered LocalDB instances first
            string localDbPath = FindSqlLocalDBPath();
            if (!string.IsNullOrEmpty(localDbPath))
            {
                var instances = GetLocalDBInstances(localDbPath);
                foreach (var inst in instances)
                {
                    string srvName = "(localdb)\\" + inst;
                    if (!serverList.Contains(srvName))
                    {
                        serverList.Add(srvName);
                    }
                }
            }

            // 2. Add standard static server names
            string[] standardServers = {
                "(localdb)\\MSSQLLocalDB",
                "(localdb)\\v11.0",
                ".\\SQLEXPRESS",
                "localhost\\SQLEXPRESS",
                "(local)\\SQLEXPRESS",
                "localhost",
                "."
            };

            foreach (var srv in standardServers)
            {
                if (!serverList.Contains(srv))
                {
                    serverList.Add(srv);
                }
            }

            // 3. Probe each server connection
            foreach (string server in serverList)
            {
                string testServer = server;
                if (server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
                {
                    string instanceName = server.Substring(10).Trim();
                    string state;
                    string pipeName;
                    GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);
                    if (!string.IsNullOrEmpty(pipeName))
                    {
                        testServer = pipeName;
                    }
                }

                string masterTest = "Server=" + testServer + ";Database=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=2;";
                try
                {
                    using (SqlConnection conn = new SqlConnection(masterTest))
                    {
                        conn.Open();
                        return server;
                    }
                }
                catch
                {
                    // Try next
                }
            }

            // Fallback: use the first server we probed, or (localdb)\MSSQLLocalDB if none
            return serverList.Count > 0 ? serverList[0] : "(localdb)\\MSSQLLocalDB";
        }

        public static string ResolveLocalDbServerName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
                return serverName;

            if (serverName.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
            {
                if (serverName.Equals(_cachedLocalDbServer, StringComparison.OrdinalIgnoreCase) && 
                    (DateTime.UtcNow - _lastResolvedTime).TotalSeconds < 10 &&
                    !string.IsNullOrEmpty(_cachedLocalDbPipe))
                {
                    return _cachedLocalDbPipe;
                }

                string instanceName = serverName.Substring(10).Trim();
                string localDbPath = FindSqlLocalDBPath();
                if (!string.IsNullOrEmpty(localDbPath))
                {
                    string state = "Stopped";
                    string pipeName = null;

                    // 1. Get initial state
                    GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);

                    // 2. If it is stopped or starting, trigger start command and clear connection pools
                    bool wasStopped = !state.Equals("Running", StringComparison.OrdinalIgnoreCase);
                    if (wasStopped)
                    {
                        try
                        {
                            SqlConnection.ClearAllPools();
                        }
                        catch { }

                        try
                        {
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = localDbPath,
                                Arguments = "start \"" + instanceName + "\"",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            };
                            using (var proc = System.Diagnostics.Process.Start(startInfo))
                            {
                                proc.WaitForExit(3000);
                            }
                        }
                        catch { }
                    }

                    // 3. Poll until state is "Running" and pipeName is available (up to 10 seconds timeout)
                    int attempts = 0;
                    while (attempts < 20) // 20 * 500ms = 10 seconds
                    {
                        GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);
                        if (state.Equals("Running", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pipeName))
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(500);
                        attempts++;
                    }

                    if (!string.IsNullOrEmpty(pipeName))
                    {
                        _cachedLocalDbServer = serverName;
                        _cachedLocalDbPipe = pipeName;
                        _lastResolvedTime = DateTime.UtcNow;
                        return pipeName;
                    }
                }
            }
            return serverName;
        }

        public static void InitializeDatabase()
        {
            try
            {
                var config = GetConfig();
                string targetDb = !string.IsNullOrWhiteSpace(config.Database) ? config.Database : "MeroDokanCafeDB";

                // 1. Create Database if it doesn't exist
                using (SqlConnection masterConn = new SqlConnection(MasterConnectionString))
                {
                    masterConn.Open();
                    bool dbExists = false;
                    using (SqlCommand cmd = new SqlCommand("SELECT database_id FROM sys.databases WHERE name = @dbname", masterConn))
                    {
                        cmd.Parameters.AddWithValue("@dbname", targetDb);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            dbExists = true;
                        }
                    }

                    if (!dbExists)
                    {
                        // Sanitize db name for safe identifier
                        string safeDbName = targetDb.Replace("]", "").Replace("[", "").Replace("'", "");
                        using (SqlCommand cmd = new SqlCommand($"CREATE DATABASE [{safeDbName}]", masterConn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // 2. Create Tables inside target Cafe database
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Users Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE Users (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Username NVARCHAR(50) NOT NULL UNIQUE,
                                PasswordHash NVARCHAR(255) NOT NULL,
                                FullName NVARCHAR(100) NOT NULL,
                                Role NVARCHAR(20) NOT NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Customers Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
                        BEGIN
                            CREATE TABLE Customers (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL,
                                Phone NVARCHAR(20) NULL,
                                Email NVARCHAR(100) NULL,
                                Address NVARCHAR(200) NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Suppliers Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
                        BEGIN
                            CREATE TABLE Suppliers (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL,
                                ContactPerson NVARCHAR(100) NULL,
                                Phone NVARCHAR(20) NULL,
                                Email NVARCHAR(100) NULL,
                                Address NVARCHAR(200) NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Categories Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                        BEGIN
                            CREATE TABLE Categories (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) UNIQUE NOT NULL,
                                Type NVARCHAR(20) NOT NULL DEFAULT 'Product',
                                HsnSacCode NVARCHAR(50) NULL DEFAULT '996331',
                                GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00
                            )
                        END", conn);

                    // Products Table (Dishes & Menu Inventory)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                        BEGIN
                            CREATE TABLE Products (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Name NVARCHAR(150) NOT NULL,
                                Description NVARCHAR(500) NULL,
                                Category NVARCHAR(100) NULL,
                                PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                SalesPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Stock INT NOT NULL DEFAULT 0,
                                MinStockLevel INT NOT NULL DEFAULT 5,
                                HSNCode NVARCHAR(50) NOT NULL DEFAULT '2106',
                                GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00,
                                Barcode NVARCHAR(50) NULL,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Services Table (Salon treatments, haircuts, spas, styling)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Services')
                        BEGIN
                            CREATE TABLE Services (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Name NVARCHAR(150) NOT NULL,
                                Category NVARCHAR(100) NULL,
                                Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DurationMinutes INT NOT NULL DEFAULT 30,
                                Description NVARCHAR(500) NULL,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Stylist Roles Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StylistRoles')
                        BEGIN
                            CREATE TABLE StylistRoles (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                RoleName NVARCHAR(100) NOT NULL UNIQUE,
                                Description NVARCHAR(500) NULL,
                                DefaultCommissionRate DECIMAL(5,2) NOT NULL DEFAULT 10.00,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            );

                            INSERT INTO StylistRoles (RoleName, Description, DefaultCommissionRate, IsActive) VALUES
                            ('Head Chef', 'Executive chef overseeing kitchen preparation and quality', 0.00, 1),
                            ('Sous Chef / Cook', 'Cooking, food preparation, continental & oriental dishes', 0.00, 1),
                            ('Senior Steward / Captain', 'Table management, guest greeting and order oversight', 0.00, 1),
                            ('Steward', 'Order taking, table service, KOT serving and customer care', 0.00, 1),
                            ('Barista & Beverage Master', 'Coffee brewing, shakes, mocktails and iced teas', 0.00, 1),
                            ('Pastry & Bakery Chef', 'Tibetan breads, bakery items and desserts', 0.00, 1),
                            ('Cashier & Front Desk', 'Billing counter settlement and customer reception', 0.00, 1),
                            ('Kitchen Helper / Busser', 'Kitchen assistance and table clearance', 0.00, 1);
                        END", conn);

                    // Staff / Stylists Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Staff')
                        BEGIN
                            CREATE TABLE Staff (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Name NVARCHAR(100) NOT NULL,
                                Phone NVARCHAR(50) NULL,
                                Email NVARCHAR(100) NULL,
                                Role NVARCHAR(50) NOT NULL DEFAULT 'Stylist',
                                CommissionRate DECIMAL(5,2) NOT NULL DEFAULT 10.00,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Appointments Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments')
                        BEGIN
                            CREATE TABLE Appointments (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                AppointmentNumber NVARCHAR(50) NOT NULL UNIQUE,
                                CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
                                StaffId INT NULL FOREIGN KEY REFERENCES Staff(Id) ON DELETE SET NULL,
                                ServiceId INT NULL FOREIGN KEY REFERENCES Services(Id) ON DELETE SET NULL,
                                ServiceIds NVARCHAR(500) NULL,
                                ServiceNames NVARCHAR(1000) NULL,
                                AppointmentDate DATE NOT NULL,
                                AppointmentTime NVARCHAR(100) NOT NULL,
                                Status NVARCHAR(30) NOT NULL DEFAULT 'Booked',
                                Notes NVARCHAR(500) NULL,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // Purchases Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
                        BEGIN
                            CREATE TABLE Purchases (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                PurchaseNumber NVARCHAR(50) NOT NULL UNIQUE,
                                SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
                                PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
                                TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END", conn);

                    // PurchaseDetails Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseDetails')
                        BEGIN
                            CREATE TABLE PurchaseDetails (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                PurchaseId INT FOREIGN KEY REFERENCES Purchases(Id) ON DELETE CASCADE,
                                ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                Quantity INT NOT NULL,
                                PurchasePrice DECIMAL(18,2) NOT NULL
                            )
                        END", conn);

                    // Sales Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
                        BEGIN
                            CREATE TABLE Sales (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                                CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
                                SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
                                SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Tax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                GrandTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'AmountPaid')
                            BEGIN
                                ALTER TABLE Sales ADD AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DueAmount')
                            BEGIN
                                ALTER TABLE Sales ADD DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                        END", conn);

                    // SaleDetails Table (Supports both Products and Services with assigned Stylist)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleDetails')
                        BEGIN
                            CREATE TABLE SaleDetails (
                                  Id INT PRIMARY KEY IDENTITY(1,1),
                                  SaleId INT FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                                  ItemType NVARCHAR(20) NOT NULL DEFAULT 'Product',
                                  ProductId INT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                  ServiceId INT NULL FOREIGN KEY REFERENCES Services(Id) ON DELETE SET NULL,
                                  StaffId INT NULL FOREIGN KEY REFERENCES Staff(Id) ON DELETE SET NULL,
                                  Quantity INT NOT NULL,
                                  UnitPrice DECIMAL(18,2) NOT NULL,
                                  Total DECIMAL(18,2) NOT NULL,
                                  PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00
                            )
                        END
                        ELSE
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'ItemType')
                            BEGIN
                                ALTER TABLE SaleDetails ADD ItemType NVARCHAR(20) NOT NULL DEFAULT 'Product';
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'ServiceId')
                            BEGIN
                                ALTER TABLE SaleDetails ADD ServiceId INT NULL FOREIGN KEY REFERENCES Services(Id) ON DELETE SET NULL;
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'StaffId')
                            BEGIN
                                ALTER TABLE SaleDetails ADD StaffId INT NULL FOREIGN KEY REFERENCES Staff(Id) ON DELETE SET NULL;
                            END
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'PurchaseCostAtSale')
                            BEGIN
                                ALTER TABLE SaleDetails ADD PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            END
                        END", conn);

                    // ProductPriceHistory Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductPriceHistory')
                        BEGIN
                            CREATE TABLE ProductPriceHistory (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                                OldPurchasePrice DECIMAL(18,2) NOT NULL,
                                NewPurchasePrice DECIMAL(18,2) NOT NULL,
                                OldSalesPrice DECIMAL(18,2) NOT NULL,
                                NewSalesPrice DECIMAL(18,2) NOT NULL,
                                ChangeDate DATETIME NOT NULL DEFAULT GETDATE(),
                                ChangedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                Source NVARCHAR(100) NOT NULL
                            )
                        END", conn);

                    // SalesReturns Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturns')
                        BEGIN
                            CREATE TABLE SalesReturns (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ReturnNumber NVARCHAR(50) UNIQUE NOT NULL,
                                SaleId INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                                ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
                                TotalRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CashRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                            )
                        END", conn);

                    // SalesReturnDetails Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturnDetails')
                        BEGIN
                            CREATE TABLE SalesReturnDetails (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                ReturnId INT NOT NULL FOREIGN KEY REFERENCES SalesReturns(Id) ON DELETE CASCADE,
                                ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id),
                                Quantity INT NOT NULL,
                                RefundPrice DECIMAL(18,2) NOT NULL,
                                Total DECIMAL(18,2) NOT NULL,
                                ItemCondition NVARCHAR(50) NOT NULL
                            )
                        END", conn);

                    // CustomerPayments Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerPayments')
                        BEGIN
                            CREATE TABLE CustomerPayments (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE CASCADE,
                                PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
                                Amount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                                Remarks NVARCHAR(200) NULL,
                                CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                SaleId INT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE SET NULL
                            )
                        END", conn);

                    // DailySettlements Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailySettlements')
                        BEGIN
                            CREATE TABLE DailySettlements (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                SettlementDate DATETIME NOT NULL DEFAULT GETDATE(),
                                OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CashSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DueCollections DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CardQRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                CardSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                QRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                DuesCreated DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                ExpectedCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                ActualCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Variance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                SettlementBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                                Remarks NVARCHAR(500) NULL,
                                Refunds DECIMAL(18,2) NOT NULL DEFAULT 0.00
                            )
                        END", conn);

                    // AppProfile Configuration Table
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppProfile')
                        BEGIN
                            CREATE TABLE AppProfile (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                OwnerName NVARCHAR(100) NOT NULL DEFAULT 'Cafe Manager',
                                ShopName NVARCHAR(150) NOT NULL DEFAULT 'The Local Cafe',
                                Phone NVARCHAR(50) NOT NULL DEFAULT '9971592652',
                                Email NVARCHAR(100) NOT NULL DEFAULT 'contact@thelocalcafe.com',
                                Address NVARCHAR(200) NOT NULL DEFAULT 'vajra world Mall Balwa khani, Gangtok Sikkim 737101',
                                LogoPath NVARCHAR(500) NULL,
                                ProfilePicPath NVARCHAR(500) NULL,
                                ThemePreset NVARCHAR(50) NOT NULL DEFAULT 'Dark Slate',
                                FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium',
                                BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroDokanCafe\DailyDatabaseBackup',
                                GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec',
                                GSTIN NVARCHAR(50) NULL,
                                UPIId NVARCHAR(100) NULL,
                                UPIName NVARCHAR(100) NULL,
                                AutoShowQROnUPI BIT NOT NULL DEFAULT 1,
                                PrintQROnReceipt BIT NOT NULL DEFAULT 1
                            )
                        END", conn);

                    // HsnSacMaster Table (Harmonized System of Nomenclature & Services Accounting Code)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HsnSacMaster')
                        BEGIN
                            CREATE TABLE HsnSacMaster (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Type NVARCHAR(20) NOT NULL DEFAULT 'HSN',
                                Description NVARCHAR(500) NOT NULL,
                                GSTRate DECIMAL(5,2) NOT NULL DEFAULT 18.00,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // RawMaterials Table (Cafe Inward / Outward Raw Materials & Ingredients)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RawMaterials')
                        BEGIN
                            CREATE TABLE RawMaterials (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Name NVARCHAR(150) NOT NULL,
                                Category NVARCHAR(100) NOT NULL,
                                Unit NVARCHAR(30) NOT NULL DEFAULT 'Kg',
                                CurrentStock DECIMAL(18,3) NOT NULL DEFAULT 0.000,
                                MinStockLevel DECIMAL(18,3) NOT NULL DEFAULT 5.000,
                                UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            )
                        END", conn);

                    // StockMovements Table (Daily Raw Material Inward, Outward Usage & Wastage)
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
                        BEGIN
                            CREATE TABLE StockMovements (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                MaterialId INT NOT NULL FOREIGN KEY REFERENCES RawMaterials(Id) ON DELETE CASCADE,
                                TransactionType NVARCHAR(30) NOT NULL, -- 'IN_PURCHASE', 'OUT_USAGE', 'OUT_WASTAGE', 'ADJUSTMENT'
                                Quantity DECIMAL(18,3) NOT NULL,
                                UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                TotalCost DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Department NVARCHAR(50) NULL DEFAULT 'Kitchen', -- 'Kitchen', 'Barista', 'Bakery', 'Store'
                                SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
                                ReferenceNo NVARCHAR(100) NULL,
                                Remarks NVARCHAR(500) NULL,
                                TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
                                CreatedBy NVARCHAR(100) NULL
                            )
                        END", conn);

                    // 3. Seed Default Admin User if none exists
                    int userCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                    {
                        userCount = (int)cmd.ExecuteScalar();
                    }

                    if (userCount == 0)
                    {
                        string adminPassHash = HashPassword("admin");
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Users (Username, PasswordHash, FullName, Role) 
                            VALUES (@username, @password, @fullname, @role)", conn))
                        {
                            cmd.Parameters.AddWithValue("@username", "admin");
                            cmd.Parameters.AddWithValue("@password", adminPassHash);
                            cmd.Parameters.AddWithValue("@fullname", "System Administrator");
                            cmd.Parameters.AddWithValue("@role", "Admin");
                            cmd.ExecuteNonQuery();
                        }

                        // Seed default customers and suppliers for Cafe POS
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Customers (Name, Phone, Email, Address) VALUES 
                            ('Walk-in Guest', '0000000000', 'guest@thelocalcafe.com', 'Local'),
                            ('Tenzing Norbu', '9971511223', 'tenzing@gmail.com', 'Gangtok'),
                            ('Doma Bhutia', '9971522334', 'doma@yahoo.com', 'Balwakhani');
                            
                            INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address) VALUES 
                            ('Himalayan Fresh Dairy', 'Tashi Wangyal', '9971501122', 'dairy@himalayanfresh.com', 'Gangtok, Sikkim'),
                            ('Sikkim Organic Grocery Suppliers', 'Pemba Sherpa', '9971502233', 'orders@sikkimorganics.com', 'Tadong, Gangtok'),
                            ('Sunrise Bakery & Cafe Packaging', 'Karma Lepcha', '9971503344', 'packaging@sunrisebakery.com', 'Deorali, Gangtok');", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 4. Seed Cafe Categories if none exist
                    int categoryCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Categories", conn))
                    {
                        categoryCount = (int)cmd.ExecuteScalar();
                    }

                    if (categoryCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Categories (Name, Type, HsnSacCode, GSTRate) VALUES 
                            ('Korean Specials', 'Product', '2106', 5.00),
                            ('Laphing & Soups', 'Product', '2104', 5.00),
                            ('Pasta NonVeg', 'Product', '1902', 5.00),
                            ('Pasta Veg', 'Product', '1902', 5.00),
                            ('Pizza NonVeg', 'Product', '1905', 5.00),
                            ('Pizza Veg', 'Product', '1905', 5.00),
                            ('Breakfast & Bread', 'Product', '1905', 5.00),
                            ('Drinks & Beverages', 'Product', '2202', 5.00),
                            ('Coffee & Hot Brews', 'Product', '0901', 5.00),
                            ('Cakes & Desserts', 'Product', '1905', 5.00),
                            ('AddOn', 'Product', '2106', 5.00)", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 5. Seed Cafe Stewards & Staff if none exist
                    int staffCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Staff", conn))
                    {
                        staffCount = (int)cmd.ExecuteScalar();
                    }

                    if (staffCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Staff (Name, Phone, Email, Role, CommissionRate, IsActive) VALUES 
                            ('Tashi', '9971500001', 'tashi@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Pemba', '9971500002', 'pemba@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Karma', '9971500003', 'karma@thelocalcafe.com', 'Captain', 0.00, 1),
                            ('Dawa', '9971500004', 'dawa@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Passang', '9971500005', 'passang@thelocalcafe.com', 'Chef', 0.00, 1),
                            ('Choden', '9971500006', 'choden@thelocalcafe.com', 'Barista', 0.00, 1)", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 6. Seed Cafe Dishes & Menu Products if none exist
                    int productCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
                    {
                        productCount = (int)cmd.ExecuteScalar();
                    }

                    if (productCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Products (Code, Name, Description, Category, PurchasePrice, SalesPrice, Stock, MinStockLevel, HSNCode, GSTRate) VALUES 
                            ('CF-101', 'Kimbap table Veg', 'Authentic seasoned rice and vegetable rolled in roasted seaweed', 'Korean Specials', 180.00, 350.00, 100, 10, '2106', 5.00),
                            ('CF-102', 'Veg Laphing Soupy', 'Cold Tibetan street mung bean noodles in savory garlic chili soup', 'Laphing & Soups', 50.00, 120.00, 100, 10, '2104', 5.00),
                            ('CF-103', 'PASTA AL FUNGI CHICKEN', 'Creamy mushroom garlic sauce with tender grilled chicken chunks', 'Pasta NonVeg', 200.00, 390.00, 100, 10, '1902', 5.00),
                            ('CF-104', 'PASTA AL FUNGI VEG', 'Rich creamy wild mushroom herbs sauce with penne', 'Pasta Veg', 160.00, 320.00, 100, 10, '1902', 5.00),
                            ('CF-105', 'PASTA ARRIBIATA CHICKEN', 'Spicy rich tomato basil sauce with seasoned chicken pieces', 'Pasta NonVeg', 190.00, 380.00, 100, 10, '1902', 5.00),
                            ('CF-106', 'PASTA ARRIBIATA VEG', 'Classic spicy garlic and tomato herb pasta', 'Pasta Veg', 150.00, 310.00, 100, 10, '1902', 5.00),
                            ('CF-107', 'Grilled chicken spaghetti', 'Olive oil garlic tossed spaghetti with herb grilled chicken breast', 'Pasta NonVeg', 200.00, 390.00, 100, 10, '1902', 5.00),
                            ('CF-108', 'Shanghai Pasta Chicken', 'Wok tossed fusion pasta with Oriental sauces and chicken', 'Pasta NonVeg', 200.00, 390.00, 100, 10, '1902', 5.00),
                            ('CF-109', 'Shanghai Amdo Pasta Chicken', 'Traditional Amdo style seasoned thick pasta with shredded chicken', 'Pasta NonVeg', 210.00, 400.00, 100, 10, '1902', 5.00),
                            ('CF-110', 'Shanghai pasta veg', 'Wok tossed pasta with bell peppers, mushrooms and chili soya', 'Pasta Veg', 160.00, 320.00, 100, 10, '1902', 5.00),
                            ('CF-111', 'Shanghai Amdo Pasta Veg', 'Amdo handmade rustic pasta with stir fried seasonal vegetables', 'Pasta Veg', 170.00, 330.00, 100, 10, '1902', 5.00),
                            ('CF-112', 'Bang bang noodles non veg', 'Spicy Sichuan chili oil sesame noodles with shredded chicken', 'Korean Specials', 180.00, 350.00, 100, 10, '2106', 5.00),
                            ('CF-113', 'Bang bang noodles veg', 'Handmade flat noodles in zesty chili sesame scallion dressing', 'Korean Specials', 140.00, 280.00, 100, 10, '2106', 5.00),
                            ('CF-114', 'Tibetan Bread', 'Fresh fluffy pan-fried traditional Himalayan bread', 'Breakfast & Bread', 30.00, 80.00, 100, 10, '1905', 5.00),
                            ('CF-115', 'Peach Ice Tea', 'Refreshing artisanal black tea infused with sweet peach essence and lemon', 'Drinks & Beverages', 40.00, 140.00, 100, 10, '2202', 5.00),
                            ('CF-116', 'HIMALAYAN BREAKFAST', 'Tibetan bread, eggs to order, butter, honey, and local sausage', 'Breakfast & Bread', 120.00, 280.00, 100, 10, '1905', 5.00),
                            ('CF-117', 'VEG PIZZA', 'Woodfired thin crust pizza loaded with mozzarella, peppers and olives', 'Pizza Veg', 160.00, 350.00, 100, 10, '1905', 5.00),
                            ('CF-118', 'ADD ON EXTRA CHICKEN/EGG', 'Extra portion of succulent grilled chicken or sunny side egg', 'AddOn', 25.00, 60.00, 100, 10, '2106', 5.00),
                            ('CF-119', 'Espresso / Americano', 'Freshly brewed single origin Arabica coffee shot', 'Coffee & Hot Brews', 35.00, 110.00, 100, 10, '0901', 5.00),
                            ('CF-120', 'Cafe Latte / Cappuccino', 'Velvety steamed milk with rich espresso shot and silky crema', 'Coffee & Hot Brews', 45.00, 150.00, 100, 10, '0901', 5.00)", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 7. Seed Default AppProfile if none exists
                    int profileCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AppProfile", conn))
                    {
                        profileCount = (int)cmd.ExecuteScalar();
                    }

                    if (profileCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO AppProfile (OwnerName, ShopName, Phone, Email, Address, ThemePreset, BackupFolderPath, GoogleDriveAddress, GSTIN, DefaultGSTRate, ReceiptFooterText, DefaultPackingCharge) 
                             VALUES ('Cafe Manager', 'The Local Cafe', '9971592652', 'contact@thelocalcafe.com', 'vajra world Mall Balwa khani, Gangtok Sikkim 737101', 'Warm Amber', 'D:\MeroDokanCafe\DailyDatabaseBackup', '', '11BIDPB3498K1ZD', 5.00, 'Tashi Delek! Thukje Che!', 40.00)", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 9. Seed Default HSN & SAC Masters if none exist
                    int hsnSacCount = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM HsnSacMaster", conn))
                    {
                        hsnSacCount = (int)cmd.ExecuteScalar();
                    }

                    if (hsnSacCount == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO HsnSacMaster (Code, Type, Description, GSTRate, IsActive) VALUES 
                            ('996331', 'SAC', 'Restaurant, cafe and local dining food serving services (Air-conditioned & indoor seating)', 5.00, 1),
                            ('996332', 'SAC', 'Takeaway, packaging counter and home delivery food / beverage services', 5.00, 1),
                            ('996333', 'SAC', 'Outdoor cafe catering and private event beverage food serving services', 5.00, 1),
                            ('996339', 'SAC', 'Other food and beverage preparation, barista brews and hospitality dining services', 5.00, 1),
                            ('0901', 'HSN', 'Coffee beans, roasted coffee, ground espresso blends, filter coffee and beans', 5.00, 1),
                            ('0902', 'HSN', 'Tea leaves, green tea, Darjeeling brew, organic herbal infusions and specialty teas', 5.00, 1),
                            ('1902', 'HSN', 'Pasta, spaghetti, macaroni, noodles, chowmein and soupy laphing preparations', 12.00, 1),
                            ('1905', 'HSN', 'Bakery products, cakes, pastries, croissants, toasted bread, cookies, Tibetan bread', 18.00, 1),
                            ('2106', 'HSN', 'Ready food preparations, momos, pizzas, sandwiches, snacks, sauces and cafe dishes', 5.00, 1),
                            ('2202', 'HSN', 'Non-alcoholic beverages, mocktails, iced teas, fruit drinks, craft coolers and sodas', 18.00, 1),
                            ('0401', 'HSN', 'Fresh milk, dairy cream and milk beverages for coffee & shakes', 5.00, 1),
                            ('0406', 'HSN', 'Cheese (mozzarella, cheddar, parmesan) for pizzas, sandwiches and pasta', 12.00, 1),
                            ('2009', 'HSN', 'Fresh fruit juices, vegetable smoothies and cold-pressed drinks', 12.00, 1),
                            ('4819', 'HSN', 'Food packaging containers, takeaway boxes, beverage cups, paper bags', 18.00, 1)", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Run column migrations for existing databases
                    ExecuteNonQuery(@"
                        -- AppProfile migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'GSTIN')
                            ALTER TABLE AppProfile ADD GSTIN NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'StateName')
                            ALTER TABLE AppProfile ADD StateName NVARCHAR(100) NOT NULL DEFAULT 'Delhi';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'StateCode')
                            ALTER TABLE AppProfile ADD StateCode NVARCHAR(10) NOT NULL DEFAULT '07';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'IsTaxInclusive')
                            ALTER TABLE AppProfile ADD IsTaxInclusive BIT NOT NULL DEFAULT 1;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'DefaultBillType')
                            ALTER TABLE AppProfile ADD DefaultBillType NVARCHAR(50) NOT NULL DEFAULT 'GST';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'DefaultGSTRate')
                            ALTER TABLE AppProfile ADD DefaultGSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'UPIId')
                            ALTER TABLE AppProfile ADD UPIId NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'UPIName')
                            ALTER TABLE AppProfile ADD UPIName NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'AutoShowQROnUPI')
                            ALTER TABLE AppProfile ADD AutoShowQROnUPI BIT NOT NULL DEFAULT 1;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'PrintQROnReceipt')
                            ALTER TABLE AppProfile ADD PrintQROnReceipt BIT NOT NULL DEFAULT 1;

                        -- Services migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Services') AND name = 'SACCode')
                            ALTER TABLE Services ADD SACCode NVARCHAR(50) NOT NULL DEFAULT '996331';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Services') AND name = 'GSTRate')
                            ALTER TABLE Services ADD GSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00;

                        -- Products migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'HSNCode')
                            ALTER TABLE Products ADD HSNCode NVARCHAR(50) NOT NULL DEFAULT '2106';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'GSTRate')
                            ALTER TABLE Products ADD GSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00;

                        -- Customers migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'GSTIN')
                            ALTER TABLE Customers ADD GSTIN NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'StateName')
                            ALTER TABLE Customers ADD StateName NVARCHAR(100) NOT NULL DEFAULT 'Delhi';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'StateCode')
                            ALTER TABLE Customers ADD StateCode NVARCHAR(10) NOT NULL DEFAULT '07';

                        -- Sales migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsGSTBill')
                            ALTER TABLE Sales ADD IsGSTBill BIT NOT NULL DEFAULT 1;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TaxableAmount')
                            ALTER TABLE Sales ADD TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CGSTAmount')
                            ALTER TABLE Sales ADD CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'SGSTAmount')
                            ALTER TABLE Sales ADD SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IGSTAmount')
                            ALTER TABLE Sales ADD IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CustomerGSTIN')
                            ALTER TABLE Sales ADD CustomerGSTIN NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PlaceOfSupply')
                            ALTER TABLE Sales ADD PlaceOfSupply NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsInterState')
                            ALTER TABLE Sales ADD IsInterState BIT NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CashAmount')
                            ALTER TABLE Sales ADD CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'OnlineAmount')
                            ALTER TABLE Sales ADD OnlineAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;

                        -- SaleDetails migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'HSNSAC')
                            ALTER TABLE SaleDetails ADD HSNSAC NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'GSTRate')
                            ALTER TABLE SaleDetails ADD GSTRate DECIMAL(18,2) NOT NULL DEFAULT 18.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'TaxableAmount')
                            ALTER TABLE SaleDetails ADD TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'CGSTAmount')
                            ALTER TABLE SaleDetails ADD CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'SGSTAmount')
                            ALTER TABLE SaleDetails ADD SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'IGSTAmount')
                            ALTER TABLE SaleDetails ADD IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        -- Appointments migrations
                        ALTER TABLE Appointments ALTER COLUMN AppointmentTime NVARCHAR(100) NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ServiceStaffIds')
                            ALTER TABLE Appointments ADD ServiceStaffIds NVARCHAR(1000) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'SaleId')
                            ALTER TABLE Appointments ADD SaleId INT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE SET NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'AppointmentId')
                            ALTER TABLE Sales ADD AppointmentId INT NULL FOREIGN KEY REFERENCES Appointments(Id) ON DELETE SET NULL;
                        -- DailySettlements migrations
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DailySettlements') AND name = 'CardSales')
                            ALTER TABLE DailySettlements ADD CardSales DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DailySettlements') AND name = 'QRSales')
                            ALTER TABLE DailySettlements ADD QRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00;

                        -- ================= CAFE POS SCHEMA MIGRATIONS =================
                        -- Cafe Tables & Floor Areas
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CafeTables')
                        BEGIN
                            CREATE TABLE CafeTables (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                TableNumber NVARCHAR(50) NOT NULL UNIQUE,
                                TableName NVARCHAR(100) NOT NULL,
                                Section NVARCHAR(50) NOT NULL DEFAULT 'Main Dining',
                                Capacity INT NOT NULL DEFAULT 4,
                                Status NVARCHAR(30) NOT NULL DEFAULT 'Available', -- Available, Running, Printed, Reserved
                                CurrentBillAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                OrderStartTime DATETIME NULL,
                                BilledTime DATETIME NULL,
                                ActiveKotNumbers NVARCHAR(200) NULL,
                                ActiveSaleId INT NULL,
                                CurrentSteward NVARCHAR(100) NULL,
                                IsActive BIT NOT NULL DEFAULT 1
                            );

                            -- Seed Tables 1 to 10 and Waiting 1 to 5
                            INSERT INTO CafeTables (TableNumber, TableName, Section, Capacity, Status) VALUES
                            ('1', 'Table 1', 'Main Dining', 2, 'Available'),
                            ('2', 'Table 2', 'Main Dining', 4, 'Available'),
                            ('3', 'Table 3', 'Main Dining', 4, 'Available'),
                            ('4', 'Table 4', 'Main Dining', 4, 'Available'),
                            ('5', 'Table 5', 'Main Dining', 6, 'Available'),
                            ('6', 'Table 6', 'Main Dining', 2, 'Available'),
                            ('7', 'Table 7', 'Main Dining', 4, 'Available'),
                            ('8', 'Table 8', 'Main Dining', 4, 'Available'),
                            ('9', 'Table 9', 'Main Dining', 6, 'Available'),
                            ('10', 'Table 10', 'Main Dining', 8, 'Available'),
                            ('Waiting 1', 'Waiting Token 1', 'Takeaway & Waiting', 1, 'Available'),
                            ('Waiting 2', 'Waiting Token 2', 'Takeaway & Waiting', 1, 'Available'),
                            ('Waiting 3', 'Waiting Token 3', 'Takeaway & Waiting', 1, 'Available'),
                            ('Waiting 4', 'Waiting Token 4', 'Takeaway & Waiting', 1, 'Available'),
                            ('Waiting 5', 'Waiting Token 5', 'Takeaway & Waiting', 1, 'Available');
                        END

                        -- Kitchen Order Tickets (KOT Master)
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KOTMaster')
                        BEGIN
                            CREATE TABLE KOTMaster (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                KOTNumber INT NOT NULL,
                                TableNumber NVARCHAR(50) NOT NULL,
                                OrderType NVARCHAR(50) NOT NULL DEFAULT 'DINING', -- DINING, Take Away, Delivery
                                Steward NVARCHAR(100) NULL,
                                Status NVARCHAR(30) NOT NULL DEFAULT 'Active', -- Active, Served, Billed, Voided
                                CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                                KotComment NVARCHAR(500) NULL,
                                IsVoided BIT NOT NULL DEFAULT 0,
                                SaleId INT NULL
                            );
                        END

                        -- KOT Details (Itemized Kitchen Orders)
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KOTDetails')
                        BEGIN
                            CREATE TABLE KOTDetails (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                KOTId INT NOT NULL FOREIGN KEY REFERENCES KOTMaster(Id) ON DELETE CASCADE,
                                ProductId INT NULL,
                                ItemName NVARCHAR(150) NOT NULL,
                                Quantity INT NOT NULL DEFAULT 1,
                                Rate DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Amount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                                Instructions NVARCHAR(200) NULL,
                                IsVoided BIT NOT NULL DEFAULT 0,
                                VoidReason NVARCHAR(300) NULL,
                                VoidedAt DATETIME NULL,
                                CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                            );
                        END

                        -- Cafe Fields for Sales Table
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'OrderType')
                            ALTER TABLE Sales ADD OrderType NVARCHAR(50) NOT NULL DEFAULT 'DINING';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TableNumber')
                            ALTER TABLE Sales ADD TableNumber NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'KotNumbers')
                            ALTER TABLE Sales ADD KotNumbers NVARCHAR(200) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PackingCharges')
                            ALTER TABLE Sales ADD PackingCharges DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'StewardName')
                            ALTER TABLE Sales ADD StewardName NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'RoundOff')
                            ALTER TABLE Sales ADD RoundOff DECIMAL(18,2) NOT NULL DEFAULT 0.00;

                        -- Cafe Fields for AppProfile
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'ReceiptFooterText')
                            ALTER TABLE AppProfile ADD ReceiptFooterText NVARCHAR(500) NOT NULL DEFAULT 'Tashi Delek! Thukje Che!';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'DefaultPackingCharge')
                            ALTER TABLE AppProfile ADD DefaultPackingCharge DECIMAL(18,2) NOT NULL DEFAULT 40.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'KitchenPrinterName')
                            ALTER TABLE AppProfile ADD KitchenPrinterName NVARCHAR(200) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'BillingPrinterName')
                            ALTER TABLE AppProfile ADD BillingPrinterName NVARCHAR(200) NULL;

                        -- Update AppProfile Default Brand Name to 'The Local Cafe'
                        EXEC sp_executesql N'
                        UPDATE AppProfile 
                        SET ShopName = ''The Local Cafe'', 
                            Address = ''vajra world Mall Balwa khani, Gangtok Sikkim 737101'',
                            Phone = ''9971592652'',
                            GSTIN = ''11BIDPB3498K1ZD'',
                            DefaultGSTRate = 5.00,
                            ReceiptFooterText = ''Tashi Delek! Thukje Che!''
                        WHERE ShopName LIKE ''%Saloon%'' OR ShopName = ''Mero Dokan Saloon & Spa'' OR ShopName IS NULL;
                        ';

                        -- Seed Cafe Categories & Menu Items if none exist or only default salon categories exist
                        IF NOT EXISTS (SELECT * FROM Categories WHERE Name = 'Korean Specials')
                        BEGIN
                            INSERT INTO Categories (Name, Type, HsnSacCode, GSTRate) VALUES 
                            ('AddOn', 'Product', '2106', 5.00),
                            ('Cakes & Desserts', 'Product', '1905', 5.00),
                            ('Coffee & Hot Brews', 'Product', '0901', 5.00),
                            ('Drinks & Beverages', 'Product', '2202', 5.00),
                            ('Korean Specials', 'Product', '2106', 5.00),
                            ('Laphing & Soups', 'Product', '2104', 5.00),
                            ('Pasta NonVeg', 'Product', '1902', 5.00),
                            ('Pasta Veg', 'Product', '1902', 5.00),
                            ('Pizza NonVeg', 'Product', '1905', 5.00),
                            ('Pizza Veg', 'Product', '1905', 5.00),
                            ('Breakfast & Bread', 'Product', '1905', 5.00);

                            -- Seed Real Cafe Products & Dishes using dynamic SQL
                            EXEC sp_executesql N'
                            INSERT INTO Products (Code, Name, Description, Category, PurchasePrice, SalesPrice, Stock, MinStockLevel, HSNCode, GSTRate) VALUES
                            (''CF-101'', ''Kimbap table Veg'', ''Authentic seasoned rice and vegetable rolled in roasted seaweed'', ''Korean Specials'', 180.00, 350.00, 100, 10, ''2106'', 5.00),
                            (''CF-102'', ''Veg Laphing Soupy'', ''Cold Tibetan street mung bean noodles in savory garlic chili soup'', ''Laphing & Soups'', 50.00, 120.00, 100, 10, ''2104'', 5.00),
                            (''CF-103'', ''PASTA AL FUNGI CHICKEN'', ''Creamy mushroom garlic sauce with tender grilled chicken chunks'', ''Pasta NonVeg'', 200.00, 390.00, 100, 10, ''1902'', 5.00),
                            (''CF-104'', ''PASTA AL FUNGI VEG'', ''Rich creamy wild mushroom herbs sauce with penne'', ''Pasta Veg'', 160.00, 320.00, 100, 10, ''1902'', 5.00),
                            (''CF-105'', ''PASTA ARRIBIATA CHICKEN'', ''Spicy rich tomato basil sauce with seasoned chicken pieces'', ''Pasta NonVeg'', 190.00, 380.00, 100, 10, ''1902'', 5.00),
                            (''CF-106'', ''PASTA ARRIBIATA VEG'', ''Classic spicy garlic and tomato herb pasta'', ''Pasta Veg'', 150.00, 310.00, 100, 10, ''1902'', 5.00),
                            (''CF-107'', ''Grilled chicken spaghetti'', ''Olive oil garlic tossed spaghetti with herb grilled chicken breast'', ''Pasta NonVeg'', 200.00, 390.00, 100, 10, ''1902'', 5.00),
                            (''CF-108'', ''Shanghai Pasta Chicken'', ''Wok tossed fusion pasta with Oriental sauces and chicken'', ''Pasta NonVeg'', 200.00, 390.00, 100, 10, ''1902'', 5.00),
                            (''CF-109'', ''Shanghai Amdo Pasta Chicken'', ''Traditional Amdo style seasoned thick pasta with shredded chicken'', ''Pasta NonVeg'', 210.00, 400.00, 100, 10, ''1902'', 5.00),
                            (''CF-110'', ''Shanghai pasta veg'', ''Wok tossed pasta with bell peppers, mushrooms and chili soya'', ''Pasta Veg'', 160.00, 320.00, 100, 10, ''1902'', 5.00),
                            (''CF-111'', ''Shanghai Amdo Pasta Veg'', ''Amdo handmade rustic pasta with stir fried seasonal vegetables'', ''Pasta Veg'', 170.00, 330.00, 100, 10, ''1902'', 5.00),
                            (''CF-112'', ''Bang bang noodles non veg'', ''Spicy Sichuan chili oil sesame noodles with shredded chicken'', ''Korean Specials'', 180.00, 350.00, 100, 10, ''2106'', 5.00),
                            (''CF-113'', ''Bang bang noodles veg'', ''Handmade flat noodles in zesty chili sesame scallion dressing'', ''Korean Specials'', 140.00, 280.00, 100, 10, ''2106'', 5.00),
                            (''CF-114'', ''Tibetan Bread'', ''Fresh fluffy pan-fried traditional Himalayan bread'', ''Breakfast & Bread'', 30.00, 80.00, 100, 10, ''1905'', 5.00),
                            (''CF-115'', ''Peach Ice Tea'', ''Refreshing artisanal black tea infused with sweet peach essence and lemon'', ''Drinks & Beverages'', 40.00, 140.00, 100, 10, ''2202'', 5.00),
                            (''CF-116'', ''HIMALAYAN BREAKFAST'', ''Tibetan bread, eggs to order, butter, honey, and local sausage'', ''Breakfast & Bread'', 120.00, 280.00, 100, 10, ''1905'', 5.00),
                            (''CF-117'', ''VEG PIZZA'', ''Woodfired thin crust pizza loaded with mozzarella, peppers and olives'', ''Pizza Veg'', 160.00, 350.00, 100, 10, ''1905'', 5.00),
                            (''CF-118'', ''ADD ON EXTRA CHICKEN/EGG'', ''Extra portion of succulent grilled chicken or sunny side egg'', ''AddOn'', 25.00, 60.00, 100, 10, ''2106'', 5.00),
                            (''CF-119'', ''Espresso / Americano'', ''Freshly brewed single origin Arabica coffee shot'', ''Coffee & Hot Brews'', 35.00, 110.00, 100, 10, ''0901'', 5.00),
                            (''CF-120'', ''Cafe Latte / Cappuccino'', ''Velvety steamed milk with rich espresso shot and silky crema'', ''Coffee & Hot Brews'', 45.00, 150.00, 100, 10, ''0901'', 5.00);
                            ';
                        END

                        -- Seed Stewards / Waiters if only default salon staff exist
                        IF NOT EXISTS (SELECT * FROM Staff WHERE Name IN ('Tashi', 'Pemba', 'Karma'))
                        BEGIN
                            INSERT INTO Staff (Name, Phone, Email, Role, CommissionRate, IsActive) VALUES
                            ('Tashi', '9971500001', 'tashi@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Pemba', '9971500002', 'pemba@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Karma', '9971500003', 'karma@thelocalcafe.com', 'Captain', 0.00, 1),
                            ('Dawa', '9971500004', 'dawa@thelocalcafe.com', 'Steward', 0.00, 1),
                            ('Passang', '9971500005', 'passang@thelocalcafe.com', 'Chef', 0.00, 1);
                        END
                    ", conn);

                    // Clean up any old legacy salon rows from MeroDokanCafeDB to ensure 100% pure Cafe content
                    ExecuteNonQuery(@"
                        -- 1. Remove old Salon products
                        DELETE FROM Products 
                        WHERE Category IN ('Hair Care Products', 'Skin Care Products', 'Grooming Accessories', 'Hair Services', 'Beard & Grooming', 'Facial & Skin Care', 'Hair Spa & Treatments', 'Body Massage & Spa', 'Manicure & Pedicure') 
                           OR Code LIKE 'PRD-%' 
                           OR Name LIKE '%Serum%' 
                           OR Name LIKE '%Shampoo%' 
                           OR Name LIKE '%Clay Wax%' 
                           OR Name LIKE '%Beard Oil%' 
                           OR Name LIKE '%Face Wash%';

                        -- 2. Remove old Salon categories
                        DELETE FROM Categories 
                        WHERE Name IN ('Hair Services', 'Beard & Grooming', 'Facial & Skin Care', 'Hair Spa & Treatments', 'Body Massage & Spa', 'Manicure & Pedicure', 'Hair Care Products', 'Skin Care Products', 'Grooming Accessories');

                        -- 3. Remove old Salon staff
                        DELETE FROM Staff 
                        WHERE Name IN ('Rahul Sharma', 'Priya Thapa', 'Alex Shrestha', 'Maya Gurung');

                        -- 4. Clean StylistRoles and replace with Cafe roles
                        DELETE FROM StylistRoles WHERE RoleName LIKE '%Stylist%' OR RoleName LIKE '%Barber%' OR RoleName LIKE '%Beautician%' OR RoleName LIKE '%Colorist%' OR RoleName LIKE '%Salon%' OR RoleName LIKE '%Spa%' OR RoleName LIKE '%Nail%' OR RoleName LIKE '%Apprentice%';

                        IF NOT EXISTS (SELECT * FROM StylistRoles WHERE RoleName = 'Steward')
                        BEGIN
                            INSERT INTO StylistRoles (RoleName, Description, DefaultCommissionRate, IsActive) VALUES
                            ('Head Chef', 'Executive chef overseeing kitchen preparation and quality', 0.00, 1),
                            ('Sous Chef / Cook', 'Cooking, food preparation, continental & oriental dishes', 0.00, 1),
                            ('Senior Steward / Captain', 'Table management, guest greeting and order oversight', 0.00, 1),
                            ('Steward', 'Order taking, table service, KOT serving and customer care', 0.00, 1),
                            ('Barista & Beverage Master', 'Coffee brewing, shakes, mocktails and iced teas', 0.00, 1),
                            ('Pastry & Bakery Chef', 'Tibetan breads, bakery items and desserts', 0.00, 1),
                            ('Cashier & Front Desk', 'Billing counter settlement and customer reception', 0.00, 1),
                            ('Kitchen Helper / Busser', 'Kitchen assistance and table clearance', 0.00, 1);
                        END

                        -- 5. Update any old salon supplier or customer records
                        DELETE FROM Suppliers WHERE Name LIKE '%L''Oreal%' OR Name LIKE '%Beauty & Spa%';
                        DELETE FROM Customers WHERE Email LIKE '%merosaloon.com%';

                        -- 6. Purge any Salon HSN / SAC codes from HsnSacMaster and re-seed pure Cafe GST records
                        DELETE FROM HsnSacMaster 
                        WHERE Code IN ('999721', '999722', '999729', '999723', '998399', '3305', '3304', '3307', '3303', '3401', '8214', '8516', '9615', '3004', '4818')
                           OR Description LIKE '%salon%' 
                           OR Description LIKE '%hair%' 
                           OR Description LIKE '%beauty%' 
                           OR Description LIKE '%barber%' 
                           OR Description LIKE '%manicure%' 
                           OR Description LIKE '%facial%' 
                           OR Description LIKE '%spa%' 
                           OR Description LIKE '%massage%';

                        IF NOT EXISTS (SELECT * FROM HsnSacMaster WHERE Code = '996331')
                        BEGIN
                            INSERT INTO HsnSacMaster (Code, Type, Description, GSTRate, IsActive) VALUES 
                            ('996331', 'SAC', 'Restaurant, cafe and local dining food serving services (Air-conditioned & indoor seating)', 5.00, 1),
                            ('996332', 'SAC', 'Takeaway, packaging counter and home delivery food / beverage services', 5.00, 1),
                            ('996333', 'SAC', 'Outdoor cafe catering and private event beverage food serving services', 5.00, 1),
                            ('996339', 'SAC', 'Other food and beverage preparation, barista brews and hospitality dining services', 5.00, 1),
                            ('0901', 'HSN', 'Coffee beans, roasted coffee, ground espresso blends, filter coffee and beans', 5.00, 1),
                            ('0902', 'HSN', 'Tea leaves, green tea, Darjeeling brew, organic herbal infusions and specialty teas', 5.00, 1),
                            ('1902', 'HSN', 'Pasta, spaghetti, macaroni, noodles, chowmein and soupy laphing preparations', 12.00, 1),
                            ('1905', 'HSN', 'Bakery products, cakes, pastries, croissants, toasted bread, cookies, Tibetan bread', 18.00, 1),
                            ('2106', 'HSN', 'Ready food preparations, momos, pizzas, sandwiches, snacks, sauces and cafe dishes', 5.00, 1),
                            ('2202', 'HSN', 'Non-alcoholic beverages, mocktails, iced teas, fruit drinks, craft coolers and sodas', 18.00, 1),
                            ('0401', 'HSN', 'Fresh milk, dairy cream and milk beverages for coffee & shakes', 5.00, 1),
                            ('0406', 'HSN', 'Cheese (mozzarella, cheddar, parmesan) for pizzas, sandwiches and pasta', 12.00, 1),
                            ('2009', 'HSN', 'Fresh fruit juices, vegetable smoothies and cold-pressed drinks', 12.00, 1),
                            ('4819', 'HSN', 'Food packaging containers, takeaway boxes, beverage cups, paper bags', 18.00, 1);
                        END

                        -- 7. Fix any Categories or Products that had legacy salon codes
                        UPDATE Categories SET HsnSacCode = '996331', GSTRate = 5.00 WHERE HsnSacCode IN ('999721', '999722', '999729', '3305', '3304', '8214') OR HsnSacCode IS NULL;
                        UPDATE Products SET HSNCode = '2106', GSTRate = 5.00 WHERE HSNCode IN ('3305', '3304', '8214', '3401', '3303') OR HSNCode IS NULL;

                        -- 8. Cleanse AppProfile table from legacy salon references
                        UPDATE AppProfile SET 
                            Email = 'contact@thelocalcafe.com',
                            BackupFolderPath = 'D:\MeroDokanCafe\DailyDatabaseBackup',
                            OwnerName = 'Cafe Manager',
                            ShopName = 'The Local Cafe'
                        WHERE Email LIKE '%merosaloon%' OR BackupFolderPath LIKE '%Saloon%' OR OwnerName LIKE '%Saloon%' OR ShopName LIKE '%Saloon%';

                        -- 9. Seed Standard Cafe Raw Materials / Kitchen Inventory if none exist
                        IF NOT EXISTS (SELECT * FROM RawMaterials)
                        BEGIN
                            INSERT INTO RawMaterials (Code, Name, Category, Unit, CurrentStock, MinStockLevel, UnitPrice) VALUES
                            ('RAW-001', 'Amul Taaza Fresh Milk 1L', 'Dairy & Milk', 'Litre', 50.000, 10.000, 64.00),
                            ('RAW-002', 'Arabica Dark Roast Coffee Beans', 'Coffee & Tea', 'Kg', 15.000, 3.000, 950.00),
                            ('RAW-003', 'Darjeeling Special Tea Leaves', 'Coffee & Tea', 'Kg', 8.000, 2.000, 480.00),
                            ('RAW-004', 'Mozzarella Diced Pizza Cheese', 'Dairy & Milk', 'Kg', 20.000, 5.000, 450.00),
                            ('RAW-005', 'Amul Salted Butter Block', 'Dairy & Milk', 'Kg', 12.000, 3.000, 520.00),
                            ('RAW-006', 'Fresh Chicken Boneless Breast', 'Meats & Non-Veg', 'Kg', 25.000, 5.000, 280.00),
                            ('RAW-007', 'Refined Wheat Flour (Maida 00)', 'Pantry & Grains', 'Kg', 40.000, 10.000, 45.00),
                            ('RAW-008', 'Granulated White Sugar', 'Pantry & Grains', 'Kg', 30.000, 5.000, 44.00),
                            ('RAW-009', 'Durum Wheat Penne Pasta', 'Pantry & Grains', 'Kg', 18.000, 4.000, 140.00),
                            ('RAW-010', 'San Marzano Tomato Pizza Sauce', 'Pantry & Grains', 'Kg', 15.000, 3.000, 180.00),
                            ('RAW-011', 'Vanilla & Caramel Flavor Syrups', 'Beverages & Syrups', 'Litre', 8.000, 2.000, 380.00),
                            ('RAW-012', 'Refined Sunflower Cooking Oil', 'Pantry & Grains', 'Litre', 30.000, 5.000, 135.00),
                            ('RAW-013', 'Kraft Takeaway Paper Meal Boxes', 'Packaging & Disposables', 'Pcs', 200.000, 50.000, 6.50),
                            ('RAW-014', 'Hot Beverage Paper Cups 250ml', 'Packaging & Disposables', 'Pcs', 300.000, 50.000, 4.00);
                        END
                    ", conn);

                    // Run chronological payments allocation migration
                    MigratePaymentsToSales();

                    // Backfill legacy Sales records to make them mathematically consistent in reports
                    ExecuteNonQuery(@"
                        UPDATE Sales 
                        SET AmountPaid = GrandTotal 
                        WHERE AmountPaid = 0.00 AND DueAmount = 0.00 AND GrandTotal > 0.00;

                        UPDATE Sales 
                        SET CashAmount = AmountPaid 
                        WHERE CashAmount = 0.00 AND OnlineAmount = 0.00 AND (PaymentMethod = 'Cash' OR PaymentMethod IS NULL);

                        UPDATE Sales 
                        SET OnlineAmount = AmountPaid 
                        WHERE CashAmount = 0.00 AND OnlineAmount = 0.00 AND (PaymentMethod IN ('Card', 'QR Pay', 'UPI', 'Wallet', 'Online'));

                        -- Auto-link legacy Billed appointments to Sales by CustomerId and Date if not already linked
                        UPDATE a
                        SET a.SaleId = s.Id
                        FROM Appointments a
                        CROSS APPLY (
                            SELECT TOP 1 Id FROM Sales 
                            WHERE CustomerId = a.CustomerId 
                              AND CAST(SaleDate AS DATE) = a.AppointmentDate 
                            ORDER BY Id DESC
                        ) s
                        WHERE a.Status = 'Billed' AND a.SaleId IS NULL;

                        UPDATE s
                        SET s.AppointmentId = a.Id
                        FROM Sales s
                        INNER JOIN Appointments a ON a.SaleId = s.Id
                        WHERE s.AppointmentId IS NULL;
                    ", conn);
                }
            }
            catch (SqlException ex)
            {
                string localDbPath = FindSqlLocalDBPath();
                if (string.IsNullOrEmpty(localDbPath))
                {
                    throw new Exception("Microsoft SQL Server LocalDB is not installed on this machine.\n\n" +
                                        "Please download and install Microsoft SQL Server LocalDB (v11.0 or newer, e.g. SQL Server 2019/2022 LocalDB) to run the application.\n" +
                                        "You can obtain the installer from Microsoft's SQL Server Express download page.\n\n" +
                                        "Error Details: " + ex.Message, ex);
                }
                else
                {
                    string diagnostics = GetLocalDBDiagnostics();
                    throw new Exception("Microsoft SQL Server LocalDB is installed, but the connection could not be established.\n\n" +
                                        "Please try resetting your LocalDB instance by running these commands in Command Prompt:\n" +
                                        "1. sqllocaldb stop MSSQLLocalDB\n" +
                                        "2. sqllocaldb delete MSSQLLocalDB\n" +
                                        "3. sqllocaldb create MSSQLLocalDB\n" +
                                        "4. sqllocaldb start MSSQLLocalDB\n" +
                                        "(Replace 'MSSQLLocalDB' with your actual instance name, such as 'v11.0', if different)\n\n" +
                                        "---------------------------------------\n" +
                                        "LOCALDB DIAGNOSTIC SYSTEM INFO:\n" +
                                        "---------------------------------------\n" +
                                        diagnostics + "\n" +
                                        "---------------------------------------\n\n" +
                                        "Error Details: " + ex.Message, ex);
                }
            }
        }

        private class SaleDueInfo
        {
            public int Id { get; set; }
            public decimal InitialDue { get; set; }
        }

        private class PaymentInfo
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Method { get; set; }
            public string Remarks { get; set; }
            public int? User { get; set; }
        }

        public static void MigratePaymentsToSales()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // Check if there are any payments without SaleId
                    string checkSql = "SELECT COUNT(*) FROM CustomerPayments WHERE SaleId IS NULL";
                    int unlinkedPayments = 0;
                    using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                    {
                        unlinkedPayments = (int)cmd.ExecuteScalar();
                    }

                    if (unlinkedPayments == 0) return;

                    // Fetch all customers who have unlinked payments
                    var customerIds = new System.Collections.Generic.List<int>();
                    string getCustsSql = "SELECT DISTINCT CustomerId FROM CustomerPayments WHERE SaleId IS NULL";
                    using (SqlCommand cmd = new SqlCommand(getCustsSql, conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                customerIds.Add(rdr.GetInt32(0));
                            }
                        }
                    }

                    foreach (int custId in customerIds)
                    {
                        // Start a transaction for each customer
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            try
                            {
                                // Get all sales for this customer with their original due amount (GrandTotal - AmountPaid)
                                // ordered by date/id
                                var sales = new System.Collections.Generic.List<SaleDueInfo>();
                                string salesSql = "SELECT Id, GrandTotal, AmountPaid FROM Sales WHERE CustomerId = @custId ORDER BY SaleDate ASC, Id ASC";
                                using (SqlCommand cmd = new SqlCommand(salesSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@custId", custId);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        while (rdr.Read())
                                        {
                                            int saleId = rdr.GetInt32(0);
                                            decimal grand = rdr.GetDecimal(1);
                                            decimal paid = rdr.GetDecimal(2);
                                            decimal initialDue = grand - paid;
                                            if (initialDue > 0)
                                            {
                                                sales.Add(new SaleDueInfo { Id = saleId, InitialDue = initialDue });
                                            }
                                        }
                                    }
                                }

                                // Get all payments for this customer where SaleId is null
                                // ordered by payment date/id
                                var payments = new System.Collections.Generic.List<PaymentInfo>();
                                string paymentsSql = "SELECT Id, Amount, PaymentDate, PaymentMethod, Remarks, CreatedBy FROM CustomerPayments WHERE CustomerId = @custId AND SaleId IS NULL ORDER BY PaymentDate ASC, Id ASC";
                                using (SqlCommand cmd = new SqlCommand(paymentsSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@custId", custId);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        while (rdr.Read())
                                        {
                                            payments.Add(new PaymentInfo {
                                                Id = rdr.GetInt32(0),
                                                Amount = rdr.GetDecimal(1),
                                                Date = rdr.GetDateTime(2),
                                                Method = rdr.GetString(3),
                                                Remarks = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                                                User = rdr.IsDBNull(5) ? (int?)null : rdr.GetInt32(5)
                                            });
                                        }
                                    }
                                }

                                // Match payments to sales
                                int saleIdx = 0;
                                foreach (var pay in payments)
                                {
                                    decimal remainingPay = pay.Amount;
                                    bool isFirstAlloc = true;

                                    while (remainingPay > 0 && saleIdx < sales.Count)
                                    {
                                        var activeSale = sales[saleIdx];
                                        
                                        // Load how much has been allocated to this sale so far from database
                                        decimal allocatedSoFar = 0;
                                        string getAllocSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE SaleId = @saleId";
                                        using (SqlCommand cmd = new SqlCommand(getAllocSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                            allocatedSoFar = Convert.ToDecimal(cmd.ExecuteScalar());
                                        }

                                        decimal remainingDue = activeSale.InitialDue - allocatedSoFar;
                                        if (remainingDue <= 0)
                                        {
                                            saleIdx++;
                                            continue;
                                        }

                                        decimal alloc = Math.Min(remainingPay, remainingDue);
                                        
                                        if (isFirstAlloc)
                                        {
                                            // Update the first matching record in CustomerPayments
                                            string updatePaySql = "UPDATE CustomerPayments SET SaleId = @saleId, Amount = @amount WHERE Id = @payId";
                                            using (SqlCommand cmd = new SqlCommand(updatePaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                                cmd.Parameters.AddWithValue("@amount", alloc);
                                                cmd.Parameters.AddWithValue("@payId", pay.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                            isFirstAlloc = false;
                                        }
                                        else
                                        {
                                            // Insert a split payment record for the remainder
                                            string insertPaySql = @"
                                                INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                                VALUES (@custId, @date, @amount, @method, @remarks, @user, @saleId)";
                                            using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@custId", custId);
                                                cmd.Parameters.AddWithValue("@date", pay.Date);
                                                cmd.Parameters.AddWithValue("@amount", alloc);
                                                cmd.Parameters.AddWithValue("@method", pay.Method);
                                                cmd.Parameters.AddWithValue("@remarks", pay.Remarks);
                                                cmd.Parameters.AddWithValue("@user", (object)pay.User ?? DBNull.Value);
                                                cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }

                                        remainingPay -= alloc;
                                    }

                                    // If there is still payment left over after matching all sales (overpayment)
                                    if (remainingPay > 0)
                                    {
                                        if (isFirstAlloc)
                                        {
                                            // It remains unlinked (SaleId = null)
                                            string updatePaySql = "UPDATE CustomerPayments SET SaleId = NULL, Amount = @amount WHERE Id = @payId";
                                            using (SqlCommand cmd = new SqlCommand(updatePaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@amount", remainingPay);
                                                cmd.Parameters.AddWithValue("@payId", pay.Id);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            // Insert split payment record with null SaleId
                                            string insertPaySql = @"
                                                INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                                VALUES (@custId, @date, @amount, @method, @remarks, @user, NULL)";
                                            using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@custId", custId);
                                                cmd.Parameters.AddWithValue("@date", pay.Date);
                                                cmd.Parameters.AddWithValue("@amount", remainingPay);
                                                cmd.Parameters.AddWithValue("@method", pay.Method);
                                                cmd.Parameters.AddWithValue("@remarks", pay.Remarks);
                                                cmd.Parameters.AddWithValue("@user", (object)pay.User ?? DBNull.Value);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }

                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                System.Diagnostics.Debug.WriteLine("Customer transaction failed: " + ex.Message);
                                throw;
                            }
                        }
                    }
                    // ========================================================
                    // INDIAN GST SCHEMA MIGRATIONS (Non-Breaking)
                    // ========================================================
                    // 1. AppProfile GST fields
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'StateName')
                            ALTER TABLE AppProfile ADD StateName NVARCHAR(100) NOT NULL DEFAULT 'Delhi';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'StateCode')
                            ALTER TABLE AppProfile ADD StateCode NVARCHAR(10) NOT NULL DEFAULT '07';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'IsTaxInclusive')
                            ALTER TABLE AppProfile ADD IsTaxInclusive BIT NOT NULL DEFAULT 1;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'DefaultBillType')
                            ALTER TABLE AppProfile ADD DefaultBillType NVARCHAR(20) NOT NULL DEFAULT 'GST';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'DefaultGSTRate')
                            ALTER TABLE AppProfile ADD DefaultGSTRate DECIMAL(5,2) NOT NULL DEFAULT 18.00;
                    ", conn);

                    // 2. Services SAC & GST Slab
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Services') AND name = 'SACCode')
                            ALTER TABLE Services ADD SACCode NVARCHAR(20) NOT NULL DEFAULT '999721';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Services') AND name = 'GSTRate')
                            ALTER TABLE Services ADD GSTRate DECIMAL(5,2) NOT NULL DEFAULT 18.00;
                    ", conn);

                    // 3. Products HSN & GST Slab
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'HSNCode')
                            ALTER TABLE Products ADD HSNCode NVARCHAR(20) NOT NULL DEFAULT '3305';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'GSTRate')
                            ALTER TABLE Products ADD GSTRate DECIMAL(5,2) NOT NULL DEFAULT 18.00;
                    ", conn);

                    // 4. Customers GSTIN & State
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'GSTIN')
                            ALTER TABLE Customers ADD GSTIN NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'StateName')
                            ALTER TABLE Customers ADD StateName NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'StateCode')
                            ALTER TABLE Customers ADD StateCode NVARCHAR(10) NULL;
                    ", conn);

                    // 5. Sales GST Breakdown Fields
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsGSTBill')
                            ALTER TABLE Sales ADD IsGSTBill BIT NOT NULL DEFAULT 1;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TaxableAmount')
                            ALTER TABLE Sales ADD TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CGSTAmount')
                            ALTER TABLE Sales ADD CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'SGSTAmount')
                            ALTER TABLE Sales ADD SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IGSTAmount')
                            ALTER TABLE Sales ADD IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'CustomerGSTIN')
                            ALTER TABLE Sales ADD CustomerGSTIN NVARCHAR(50) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PlaceOfSupply')
                            ALTER TABLE Sales ADD PlaceOfSupply NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsInterState')
                            ALTER TABLE Sales ADD IsInterState BIT NOT NULL DEFAULT 0;
                    ", conn);

                    // 6. SaleDetails Line-Item GST Fields
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'HSNSAC')
                            ALTER TABLE SaleDetails ADD HSNSAC NVARCHAR(20) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'GSTRate')
                            ALTER TABLE SaleDetails ADD GSTRate DECIMAL(5,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'TaxableAmount')
                            ALTER TABLE SaleDetails ADD TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'CGSTAmount')
                            ALTER TABLE SaleDetails ADD CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'SGSTAmount')
                            ALTER TABLE SaleDetails ADD SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'IGSTAmount')
                            ALTER TABLE SaleDetails ADD IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                    ", conn);

                    // 7. HsnSacMaster table check & seed
                    ExecuteNonQuery(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HsnSacMaster')
                        BEGIN
                            CREATE TABLE HsnSacMaster (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Code NVARCHAR(50) NOT NULL UNIQUE,
                                Type NVARCHAR(20) NOT NULL DEFAULT 'HSN',
                                Description NVARCHAR(500) NOT NULL,
                                GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00,
                                IsActive BIT NOT NULL DEFAULT 1,
                                CreatedAt DATETIME DEFAULT GETDATE()
                            );

                            INSERT INTO HsnSacMaster (Code, Type, Description, GSTRate, IsActive) VALUES 
                            ('996331', 'SAC', 'Restaurant, cafe and local dining food serving services (Air-conditioned & indoor seating)', 5.00, 1),
                            ('996332', 'SAC', 'Takeaway, packaging counter and home delivery food / beverage services', 5.00, 1),
                            ('996333', 'SAC', 'Outdoor cafe catering and private event beverage food serving services', 5.00, 1),
                            ('996339', 'SAC', 'Other food and beverage preparation, barista brews and hospitality dining services', 5.00, 1),
                            ('0901', 'HSN', 'Coffee beans, roasted coffee, ground espresso blends, filter coffee and beans', 5.00, 1),
                            ('0902', 'HSN', 'Tea leaves, green tea, Darjeeling brew, organic herbal infusions and specialty teas', 5.00, 1),
                            ('1902', 'HSN', 'Pasta, spaghetti, macaroni, noodles, chowmein and soupy laphing preparations', 12.00, 1),
                            ('1905', 'HSN', 'Bakery products, cakes, pastries, croissants, toasted bread, cookies, Tibetan bread', 18.00, 1),
                            ('2106', 'HSN', 'Ready food preparations, momos, pizzas, sandwiches, snacks, sauces and cafe dishes', 5.00, 1),
                            ('2202', 'HSN', 'Non-alcoholic beverages, mocktails, iced teas, fruit drinks, craft coolers and sodas', 18.00, 1),
                            ('0401', 'HSN', 'Fresh milk, dairy cream and milk beverages for coffee & shakes', 5.00, 1),
                            ('0406', 'HSN', 'Cheese (mozzarella, cheddar, parmesan) for pizzas, sandwiches and pasta', 12.00, 1),
                            ('2009', 'HSN', 'Fresh fruit juices, vegetable smoothies and cold-pressed drinks', 12.00, 1),
                            ('4819', 'HSN', 'Food packaging containers, takeaway boxes, beverage cups, paper bags', 18.00, 1);
                        END
                    ", conn);

                    // Categories Schema Migration (Separated batches for SQL Server compiler)
                    ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'Type') ALTER TABLE Categories ADD Type NVARCHAR(20) NOT NULL DEFAULT 'Product';", conn);
                    ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'HsnSacCode') ALTER TABLE Categories ADD HsnSacCode NVARCHAR(50) NULL DEFAULT '996331';", conn);
                    ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'GSTRate') ALTER TABLE Categories ADD GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00;", conn);

                    ExecuteNonQuery(@"
                        UPDATE Categories SET Type = 'Product', HsnSacCode = '996331', GSTRate = 5.00 WHERE HsnSacCode IN ('999721', '999722', '999729', '3305', '3304', '8214') OR HsnSacCode IS NULL;
                    ", conn);

                    // Appointments Multi-Service Migration
                    ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ServiceIds') ALTER TABLE Appointments ADD ServiceIds NVARCHAR(500) NULL;", conn);
                    ExecuteNonQuery("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ServiceNames') ALTER TABLE Appointments ADD ServiceNames NVARCHAR(1000) NULL;", conn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Migration failed: " + ex.Message);
            }
        }

        private static void ExecuteNonQuery(string sql, SqlConnection conn)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool LogRawMaterialUsage(int materialId, decimal quantity, string department, string remarks, string user, DateTime? date = null)
        {
            if (quantity <= 0) return false;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        // 1. Fetch current price and available stock
                        decimal unitPrice = 0;
                        decimal currentStock = 0;
                        using (SqlCommand cmdPrice = new SqlCommand("SELECT CurrentStock, UnitPrice FROM RawMaterials WHERE Id = @id", conn, trans))
                        {
                            cmdPrice.Parameters.AddWithValue("@id", materialId);
                            using (SqlDataReader rdr = cmdPrice.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    if (rdr["CurrentStock"] != DBNull.Value) currentStock = Convert.ToDecimal(rdr["CurrentStock"]);
                                    if (rdr["UnitPrice"] != DBNull.Value) unitPrice = Convert.ToDecimal(rdr["UnitPrice"]);
                                }
                            }
                        }

                        // Prevent negative stock: deduction cannot exceed available in-hand stock
                        if (quantity > currentStock)
                        {
                            trans.Rollback();
                            return false;
                        }

                        decimal totalCost = quantity * unitPrice;
                        DateTime txDate = date ?? DateTime.Now;

                        // 2. Insert Stock Movement
                        using (SqlCommand cmdMove = new SqlCommand(@"
                            INSERT INTO StockMovements (MaterialId, TransactionType, Quantity, UnitCost, TotalCost, Department, Remarks, TransactionDate, CreatedBy)
                            VALUES (@mId, 'OUT_USAGE', @qty, @uCost, @totCost, @dept, @remarks, @txDate, @user)", conn, trans))
                        {
                            cmdMove.Parameters.AddWithValue("@mId", materialId);
                            cmdMove.Parameters.AddWithValue("@qty", quantity);
                            cmdMove.Parameters.AddWithValue("@uCost", unitPrice);
                            cmdMove.Parameters.AddWithValue("@totCost", totalCost);
                            cmdMove.Parameters.AddWithValue("@dept", string.IsNullOrEmpty(department) ? "Kitchen" : department);
                            cmdMove.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? "Daily Kitchen Usage" : remarks);
                            cmdMove.Parameters.AddWithValue("@txDate", txDate);
                            cmdMove.Parameters.AddWithValue("@user", string.IsNullOrEmpty(user) ? "System" : user);
                            cmdMove.ExecuteNonQuery();
                        }

                        // 3. Deduct Current Stock
                        using (SqlCommand cmdStock = new SqlCommand("UPDATE RawMaterials SET CurrentStock = CurrentStock - @qty WHERE Id = @id", conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@qty", quantity);
                            cmdStock.Parameters.AddWithValue("@id", materialId);
                            cmdStock.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public static bool InwardRawMaterial(int materialId, decimal quantity, decimal unitCost, int? supplierId, string refNo, string remarks, string user, DateTime? date = null)
        {
            if (quantity <= 0) return false;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        decimal totalCost = quantity * unitCost;
                        DateTime txDate = date ?? DateTime.Now;

                        // 1. Insert Stock Movement
                        using (SqlCommand cmdMove = new SqlCommand(@"
                            INSERT INTO StockMovements (MaterialId, TransactionType, Quantity, UnitCost, TotalCost, SupplierId, ReferenceNo, Remarks, TransactionDate, CreatedBy)
                            VALUES (@mId, 'IN_PURCHASE', @qty, @uCost, @totCost, @suppId, @refNo, @remarks, @txDate, @user)", conn, trans))
                        {
                            cmdMove.Parameters.AddWithValue("@mId", materialId);
                            cmdMove.Parameters.AddWithValue("@qty", quantity);
                            cmdMove.Parameters.AddWithValue("@uCost", unitCost);
                            cmdMove.Parameters.AddWithValue("@totCost", totalCost);
                            cmdMove.Parameters.AddWithValue("@suppId", supplierId.HasValue ? (object)supplierId.Value : DBNull.Value);
                            cmdMove.Parameters.AddWithValue("@refNo", string.IsNullOrEmpty(refNo) ? DBNull.Value : (object)refNo);
                            cmdMove.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? "Stock Inward / Delivery" : remarks);
                            cmdMove.Parameters.AddWithValue("@txDate", txDate);
                            cmdMove.Parameters.AddWithValue("@user", string.IsNullOrEmpty(user) ? "System" : user);
                            cmdMove.ExecuteNonQuery();
                        }

                        // 2. Increment Current Stock and update last unit purchase price
                        using (SqlCommand cmdStock = new SqlCommand("UPDATE RawMaterials SET CurrentStock = CurrentStock + @qty, UnitPrice = CASE WHEN @uCost > 0 THEN @uCost ELSE UnitPrice END WHERE Id = @id", conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@qty", quantity);
                            cmdStock.Parameters.AddWithValue("@uCost", unitCost);
                            cmdStock.Parameters.AddWithValue("@id", materialId);
                            cmdStock.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public static bool AdjustRawMaterialStock(int materialId, decimal newStock, string remarks, string user)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        decimal currentStock = 0;
                        decimal unitPrice = 0;
                        using (SqlCommand cmdGet = new SqlCommand("SELECT CurrentStock, UnitPrice FROM RawMaterials WHERE Id = @id", conn, trans))
                        {
                            cmdGet.Parameters.AddWithValue("@id", materialId);
                            using (SqlDataReader rdr = cmdGet.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    currentStock = Convert.ToDecimal(rdr["CurrentStock"]);
                                    unitPrice = Convert.ToDecimal(rdr["UnitPrice"]);
                                }
                            }
                        }

                        decimal diff = newStock - currentStock;
                        decimal totalCost = Math.Abs(diff) * unitPrice;

                        // Insert adjustment movement
                        using (SqlCommand cmdMove = new SqlCommand(@"
                            INSERT INTO StockMovements (MaterialId, TransactionType, Quantity, UnitCost, TotalCost, Remarks, TransactionDate, CreatedBy)
                            VALUES (@mId, 'ADJUSTMENT', @qty, @uCost, @totCost, @remarks, GETDATE(), @user)", conn, trans))
                        {
                            cmdMove.Parameters.AddWithValue("@mId", materialId);
                            cmdMove.Parameters.AddWithValue("@qty", diff);
                            cmdMove.Parameters.AddWithValue("@uCost", unitPrice);
                            cmdMove.Parameters.AddWithValue("@totCost", totalCost);
                            cmdMove.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? $"Stock adjusted from {currentStock} to {newStock}" : remarks);
                            cmdMove.Parameters.AddWithValue("@user", string.IsNullOrEmpty(user) ? "System" : user);
                            cmdMove.ExecuteNonQuery();
                        }

                        // Update RawMaterial
                        using (SqlCommand cmdUpdate = new SqlCommand("UPDATE RawMaterials SET CurrentStock = @newStock WHERE Id = @id", conn, trans))
                        {
                            cmdUpdate.Parameters.AddWithValue("@newStock", newStock);
                            cmdUpdate.Parameters.AddWithValue("@id", materialId);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public static bool ReturnKitchenRawMaterial(int materialId, decimal quantity, string reason, string user, DateTime? date = null)
        {
            if (quantity <= 0) return false;
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        decimal unitPrice = 0;
                        using (SqlCommand cmdGet = new SqlCommand("SELECT UnitPrice FROM RawMaterials WHERE Id = @id", conn, trans))
                        {
                            cmdGet.Parameters.AddWithValue("@id", materialId);
                            object res = cmdGet.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                unitPrice = Convert.ToDecimal(res);
                            }
                        }

                        decimal totalCost = quantity * unitPrice;
                        DateTime txDate = date ?? DateTime.Now;

                        // 1. Insert Stock Movement
                        using (SqlCommand cmdMove = new SqlCommand(@"
                            INSERT INTO StockMovements (MaterialId, TransactionType, Quantity, UnitCost, TotalCost, Department, Remarks, TransactionDate, CreatedBy)
                            VALUES (@mId, 'IN_RETURN', @qty, @uCost, @totCost, 'Kitchen / Store Return', @remarks, @txDate, @user)", conn, trans))
                        {
                            cmdMove.Parameters.AddWithValue("@mId", materialId);
                            cmdMove.Parameters.AddWithValue("@qty", quantity);
                            cmdMove.Parameters.AddWithValue("@uCost", unitPrice);
                            cmdMove.Parameters.AddWithValue("@totCost", totalCost);
                            cmdMove.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(reason) ? "Kitchen Evening Return to Stock" : reason);
                            cmdMove.Parameters.AddWithValue("@txDate", txDate);
                            cmdMove.Parameters.AddWithValue("@user", string.IsNullOrEmpty(user) ? "System" : user);
                            cmdMove.ExecuteNonQuery();
                        }

                        // 2. Increment Current Stock
                        using (SqlCommand cmdStock = new SqlCommand("UPDATE RawMaterials SET CurrentStock = CurrentStock + @qty WHERE Id = @id", conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@qty", quantity);
                            cmdStock.Parameters.AddWithValue("@id", materialId);
                            cmdStock.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        public static bool DeleteStockMovement(int movementId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        int materialId = 0;
                        string txType = "";
                        decimal qty = 0;

                        using (SqlCommand cmdGet = new SqlCommand("SELECT MaterialId, TransactionType, Quantity FROM StockMovements WHERE Id = @id", conn, trans))
                        {
                            cmdGet.Parameters.AddWithValue("@id", movementId);
                            using (SqlDataReader rdr = cmdGet.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    materialId = Convert.ToInt32(rdr["MaterialId"]);
                                    txType = rdr["TransactionType"].ToString();
                                    qty = Convert.ToDecimal(rdr["Quantity"]);
                                }
                            }
                        }

                        if (materialId == 0) return false;

                        // Reverse stock
                        if (txType == "OUT_USAGE" || txType == "OUT_WASTAGE")
                        {
                            using (SqlCommand cmdRev = new SqlCommand("UPDATE RawMaterials SET CurrentStock = CurrentStock + @qty WHERE Id = @id", conn, trans))
                            {
                                cmdRev.Parameters.AddWithValue("@qty", qty);
                                cmdRev.Parameters.AddWithValue("@id", materialId);
                                cmdRev.ExecuteNonQuery();
                            }
                        }
                        else if (txType == "IN_PURCHASE" || txType == "IN_RETURN")
                        {
                            using (SqlCommand cmdRev = new SqlCommand("UPDATE RawMaterials SET CurrentStock = CurrentStock - @qty WHERE Id = @id", conn, trans))
                            {
                                cmdRev.Parameters.AddWithValue("@qty", qty);
                                cmdRev.Parameters.AddWithValue("@id", materialId);
                                cmdRev.ExecuteNonQuery();
                            }
                        }

                        // Delete movement
                        using (SqlCommand cmdDel = new SqlCommand("DELETE FROM StockMovements WHERE Id = @id", conn, trans))
                        {
                            cmdDel.Parameters.AddWithValue("@id", movementId);
                            cmdDel.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                }
            }
            catch { return false; }
        }
    }

    public class GSTState
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public override string ToString() => $"{Code} - {Name}";
    }

    public static class IndianGSTHelper
    {
        public static System.Collections.Generic.List<GSTState> GetIndianStates()
        {
            return new System.Collections.Generic.List<GSTState>
            {
                new GSTState { Code = "01", Name = "Jammu and Kashmir" },
                new GSTState { Code = "02", Name = "Himachal Pradesh" },
                new GSTState { Code = "03", Name = "Punjab" },
                new GSTState { Code = "04", Name = "Chandigarh" },
                new GSTState { Code = "05", Name = "Uttarakhand" },
                new GSTState { Code = "06", Name = "Haryana" },
                new GSTState { Code = "07", Name = "Delhi" },
                new GSTState { Code = "08", Name = "Rajasthan" },
                new GSTState { Code = "09", Name = "Uttar Pradesh" },
                new GSTState { Code = "10", Name = "Bihar" },
                new GSTState { Code = "11", Name = "Sikkim" },
                new GSTState { Code = "12", Name = "Arunachal Pradesh" },
                new GSTState { Code = "13", Name = "Nagaland" },
                new GSTState { Code = "14", Name = "Manipur" },
                new GSTState { Code = "15", Name = "Mizoram" },
                new GSTState { Code = "16", Name = "Tripura" },
                new GSTState { Code = "17", Name = "Meghalaya" },
                new GSTState { Code = "18", Name = "Assam" },
                new GSTState { Code = "19", Name = "West Bengal" },
                new GSTState { Code = "20", Name = "Jharkhand" },
                new GSTState { Code = "21", Name = "Odisha" },
                new GSTState { Code = "22", Name = "Chhattisgarh" },
                new GSTState { Code = "23", Name = "Madhya Pradesh" },
                new GSTState { Code = "24", Name = "Gujarat" },
                new GSTState { Code = "26", Name = "Dadra & Nagar Haveli and Daman & Diu" },
                new GSTState { Code = "27", Name = "Maharashtra" },
                new GSTState { Code = "29", Name = "Karnataka" },
                new GSTState { Code = "30", Name = "Goa" },
                new GSTState { Code = "31", Name = "Lakshadweep" },
                new GSTState { Code = "32", Name = "Kerala" },
                new GSTState { Code = "33", Name = "Tamil Nadu" },
                new GSTState { Code = "34", Name = "Puducherry" },
                new GSTState { Code = "35", Name = "Andaman and Nicobar Islands" },
                new GSTState { Code = "36", Name = "Telangana" },
                new GSTState { Code = "37", Name = "Andhra Pradesh" },
                new GSTState { Code = "38", Name = "Ladakh" },
                new GSTState { Code = "97", Name = "Other Territory" }
            };
        }

        public static string AmountToWords(decimal amount)
        {
            if (amount == 0) return "Rupees Zero Only";
            if (amount < 0) return "Minus " + AmountToWords(Math.Abs(amount));

            long wholePart = (long)Math.Truncate(amount);
            int paisePart = (int)Math.Round((amount - wholePart) * 100);

            string words = "Rupees " + ConvertNumberToWords(wholePart);
            if (paisePart > 0)
            {
                words += " and " + ConvertNumberToWords(paisePart) + " Paise";
            }
            words += " Only";
            return words;
        }

        private static string ConvertNumberToWords(long number)
        {
            if (number == 0) return "Zero";

            string[] unitsMap = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tensMap = { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += ConvertNumberToWords(number / 10000000) + " Crore ";
                number %= 10000000;
            }

            if ((number / 100000) > 0)
            {
                words += ConvertNumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if ((number / 1000) > 0)
            {
                words += ConvertNumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += ConvertNumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += " " + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }
    }
}

