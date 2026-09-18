using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class DatabaseConfigForm : Form
    {
        private TextBox txtServer;
        private TextBox txtDatabase;
        private ComboBox comboAuth;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtTimeout;
        private TextBox txtRetryCount;
        private TextBox txtRetryInterval;
        
        private Label lblUsername;
        private Label lblPassword;
        
        private Button btnTest;
        private Button btnSave;
        private Button btnCancel;
        
        private DatabaseHelper.DbConfig currentConfig;

        public DatabaseConfigForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
            UpdateAuthFieldsVisibility();
        }

        private void InitializeComponent()
        {
            this.Text = "Database Connection Configuration";
            this.Size = new Size(500, 620);
            this.Font = Theme.MainFont;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Primary;
            this.Icon = Theme.AppIcon;

            // Header Label
            Label lblHeader = new Label();
            lblHeader.Text = "Database Configuration";
            lblHeader.Location = new Point(20, 20);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            int startY = 70;
            int gapY = 55;

            // Server
            Label lblServer = new Label();
            lblServer.Text = "SQL Server Address *";
            lblServer.Location = new Point(20, startY);
            lblServer.AutoSize = true;
            Theme.StyleLabel(lblServer, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblServer);

            txtServer = new TextBox();
            txtServer.Size = new Size(440, 28);
            txtServer.Location = new Point(20, startY + 20);
            Theme.StyleTextBox(txtServer);
            this.Controls.Add(txtServer);

            // Database
            Label lblDatabase = new Label();
            lblDatabase.Text = "Database Name *";
            lblDatabase.Location = new Point(20, startY + gapY);
            lblDatabase.AutoSize = true;
            Theme.StyleLabel(lblDatabase, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblDatabase);

            txtDatabase = new TextBox();
            txtDatabase.Size = new Size(440, 28);
            txtDatabase.Location = new Point(20, startY + gapY + 20);
            Theme.StyleTextBox(txtDatabase);
            this.Controls.Add(txtDatabase);

            // Authentication Mode
            Label lblAuth = new Label();
            lblAuth.Text = "Authentication Mode *";
            lblAuth.Location = new Point(20, startY + gapY * 2);
            lblAuth.AutoSize = true;
            Theme.StyleLabel(lblAuth, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblAuth);

            comboAuth = new ComboBox();
            comboAuth.Size = new Size(440, 28);
            comboAuth.Location = new Point(20, startY + gapY * 2 + 20);
            comboAuth.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAuth.Items.AddRange(new object[] { "Windows Authentication", "SQL Server Authentication" });
            comboAuth.SelectedIndex = 0;
            comboAuth.SelectedIndexChanged += ComboAuth_SelectedIndexChanged;
            Theme.StyleComboBox(comboAuth);
            this.Controls.Add(comboAuth);

            // Username
            lblUsername = new Label();
            lblUsername.Text = "SQL Server Username *";
            lblUsername.Location = new Point(20, startY + gapY * 3);
            lblUsername.AutoSize = true;
            Theme.StyleLabel(lblUsername, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblUsername);

            txtUsername = new TextBox();
            txtUsername.Size = new Size(440, 28);
            txtUsername.Location = new Point(20, startY + gapY * 3 + 20);
            Theme.StyleTextBox(txtUsername);
            this.Controls.Add(txtUsername);

            // Password
            lblPassword = new Label();
            lblPassword.Text = "SQL Server Password *";
            lblPassword.Location = new Point(20, startY + gapY * 4);
            lblPassword.AutoSize = true;
            Theme.StyleLabel(lblPassword, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Size = new Size(440, 28);
            txtPassword.Location = new Point(20, startY + gapY * 4 + 20);
            txtPassword.UseSystemPasswordChar = true;
            Theme.StyleTextBox(txtPassword);
            this.Controls.Add(txtPassword);

            // Resiliency Panel Group (Connection Timeout / Retry)
            GroupBox grpResiliency = new GroupBox();
            grpResiliency.Text = "Connection Resilience Settings (Optional)";
            grpResiliency.Size = new Size(440, 85);
            grpResiliency.Location = new Point(20, startY + gapY * 5);
            grpResiliency.ForeColor = Theme.TextLight;
            grpResiliency.BackColor = Color.Transparent;
            this.Controls.Add(grpResiliency);

            // Timeout (s)
            Label lblTimeout = new Label();
            lblTimeout.Text = "Timeout (s)";
            lblTimeout.Location = new Point(15, 25);
            lblTimeout.Size = new Size(120, 20);
            Theme.StyleLabel(lblTimeout, Theme.TextDark, Theme.MainFont);
            grpResiliency.Controls.Add(lblTimeout);

            txtTimeout = new TextBox();
            txtTimeout.Size = new Size(110, 25);
            txtTimeout.Location = new Point(15, 45);
            Theme.StyleTextBox(txtTimeout);
            grpResiliency.Controls.Add(txtTimeout);

            // Retry Count
            Label lblRetryCount = new Label();
            lblRetryCount.Text = "Retry Count";
            lblRetryCount.Location = new Point(150, 25);
            lblRetryCount.Size = new Size(120, 20);
            Theme.StyleLabel(lblRetryCount, Theme.TextDark, Theme.MainFont);
            grpResiliency.Controls.Add(lblRetryCount);

            txtRetryCount = new TextBox();
            txtRetryCount.Size = new Size(110, 25);
            txtRetryCount.Location = new Point(150, 45);
            Theme.StyleTextBox(txtRetryCount);
            grpResiliency.Controls.Add(txtRetryCount);

            // Retry Interval (s)
            Label lblRetryInterval = new Label();
            lblRetryInterval.Text = "Retry Interval (s)";
            lblRetryInterval.Location = new Point(285, 25);
            lblRetryInterval.Size = new Size(140, 20);
            Theme.StyleLabel(lblRetryInterval, Theme.TextDark, Theme.MainFont);
            grpResiliency.Controls.Add(lblRetryInterval);

            txtRetryInterval = new TextBox();
            txtRetryInterval.Size = new Size(140, 25);
            txtRetryInterval.Location = new Point(285, 45);
            Theme.StyleTextBox(txtRetryInterval);
            grpResiliency.Controls.Add(txtRetryInterval);

            // Action Buttons
            btnTest = new Button();
            btnTest.Text = "🔌 Test Connection";
            btnTest.Size = new Size(140, 40);
            btnTest.Location = new Point(20, startY + gapY * 5 + 105);
            Theme.StyleSecondaryButton(btnTest);
            btnTest.Click += BtnTest_Click;
            this.Controls.Add(btnTest);

            btnSave = new Button();
            btnSave.Text = "💾 Save Config";
            btnSave.Size = new Size(140, 40);
            btnSave.Location = new Point(170, startY + gapY * 5 + 105);
            Theme.StyleSuccessButton(btnSave);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button();
            btnCancel.Text = "❌ Cancel";
            btnCancel.Size = new Size(140, 40);
            btnCancel.Location = new Point(320, startY + gapY * 5 + 105);
            Theme.StyleDangerButton(btnCancel);
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        private void LoadCurrentSettings()
        {
            currentConfig = DatabaseHelper.LoadConfig();
            txtServer.Text = currentConfig.Server;
            txtDatabase.Text = currentConfig.Database;
            comboAuth.SelectedIndex = currentConfig.IntegratedSecurity ? 0 : 1;
            txtUsername.Text = currentConfig.Username;
            txtPassword.Text = currentConfig.Password;
            txtTimeout.Text = currentConfig.ConnectionTimeout.ToString();
            txtRetryCount.Text = currentConfig.ConnectRetryCount.ToString();
            txtRetryInterval.Text = currentConfig.ConnectRetryInterval.ToString();
        }

        private void ComboAuth_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAuthFieldsVisibility();
        }

        private void UpdateAuthFieldsVisibility()
        {
            bool sqlAuth = comboAuth.SelectedIndex == 1;
            txtUsername.Enabled = sqlAuth;
            txtPassword.Enabled = sqlAuth;
            
            if (sqlAuth)
            {
                txtUsername.BackColor = Theme.Secondary;
                txtPassword.BackColor = Theme.Secondary;
            }
            else
            {
                txtUsername.BackColor = Color.FromArgb(45, 55, 72);
                txtPassword.BackColor = Color.FromArgb(45, 55, 72);
            }
        }

        private DatabaseHelper.DbConfig GetConfigFromUI()
        {
            var config = new DatabaseHelper.DbConfig();
            config.Server = txtServer.Text.Trim();
            config.Database = txtDatabase.Text.Trim();
            config.IntegratedSecurity = comboAuth.SelectedIndex == 0;
            config.Username = txtUsername.Text.Trim();
            config.Password = txtPassword.Text;
            
            int timeout;
            if (int.TryParse(txtTimeout.Text, out timeout))
                config.ConnectionTimeout = timeout;
            
            int retryCount;
            if (int.TryParse(txtRetryCount.Text, out retryCount))
                config.ConnectRetryCount = retryCount;

            int retryInterval;
            if (int.TryParse(txtRetryInterval.Text, out retryInterval))
                config.ConnectRetryInterval = retryInterval;

            return config;
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServer.Text.Trim()) || string.IsNullOrEmpty(txtDatabase.Text.Trim()))
            {
                MessageBox.Show("Please enter both Server Address and Database Name.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "Connecting...";
            this.Cursor = Cursors.WaitCursor;

            string testConnString = DatabaseHelper.BuildConnectionString(GetConfigFromUI());
            
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                string errorMessage = null;
                bool success = false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(testConnString))
                    {
                        conn.Open();
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.Cursor = Cursors.Default;
                    btnTest.Enabled = true;
                    btnTest.Text = "🔌 Test Connection";

                    if (success)
                    {
                        MessageBox.Show("Database connection test succeeded!", "Test Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Database connection test failed:\n\n" + errorMessage, "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServer.Text.Trim()) || string.IsNullOrEmpty(txtDatabase.Text.Trim()))
            {
                MessageBox.Show("Please enter both Server Address and Database Name.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboAuth.SelectedIndex == 1 && string.IsNullOrEmpty(txtUsername.Text.Trim()))
            {
                MessageBox.Show("Please enter a username for SQL Server Authentication.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newConfig = GetConfigFromUI();
            DatabaseHelper.SaveConfig(newConfig);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
