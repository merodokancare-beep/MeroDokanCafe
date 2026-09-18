using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class LoginForm : Form
    {
        private Panel leftPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label userLabel;
        private TextBox txtUsername;
        private Panel userTxtPanel;
        private Label passLabel;
        private TextBox txtPassword;
        private Panel passTxtPanel;
        private Button btnLogin;
        private Button btnExit;
        private Label errLabel;

        public LoginForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
            this.Load += (s, e) => txtUsername.Focus();
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Secret developer key combination: Ctrl + Shift + L
            if (e.Control && e.Shift && e.KeyCode == Keys.L)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                using (DeveloperKeyGenForm keyGen = new DeveloperKeyGenForm())
                {
                    keyGen.ShowDialog(this);
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Mero Dokan - Login";
            this.ClientSize = new Size(450, 500); // Guarantees exact client area dimension!
            this.AutoScaleMode = AutoScaleMode.Dpi; // Enables robust DPI scaling auto-resizing!
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Primary;
            this.Icon = Theme.AppIcon;

            // Enable double buffering for smooth rendering
            this.DoubleBuffered = true;

            // Left panel indicator
            leftPanel = new Panel();
            leftPanel.Size = new Size(12, this.Height);
            leftPanel.Location = new Point(0, 0);
            leftPanel.BackColor = Theme.Accent;
            this.Controls.Add(leftPanel);

            // Title
            titleLabel = new Label();
            titleLabel.Text = "Mero Dokan";
            titleLabel.Location = new Point(50, 60);
            titleLabel.AutoSize = true;
            Theme.StyleLabel(titleLabel, Theme.TextLight, new Font("Segoe UI Semibold", 28F, FontStyle.Bold));
            this.Controls.Add(titleLabel);

            // Subtitle
            subtitleLabel = new Label();
            subtitleLabel.Text = "Shop Management System";
            subtitleLabel.Location = new Point(54, 115);
            subtitleLabel.AutoSize = true;
            Theme.StyleLabel(subtitleLabel, Theme.TextDark, new Font("Segoe UI", 11F, FontStyle.Regular));
            this.Controls.Add(subtitleLabel);

            // Username Label
            userLabel = new Label();
            userLabel.Text = "Username";
            userLabel.Location = new Point(50, 175);
            userLabel.AutoSize = true;
            Theme.StyleLabel(userLabel, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(userLabel);

            // Username Input container panel
            userTxtPanel = new Panel();
            userTxtPanel.Size = new Size(350, 42);
            userTxtPanel.Location = new Point(50, 200);
            userTxtPanel.BackColor = Theme.InputBg;
            userTxtPanel.Padding = new Padding(10, 10, 10, 10);
            userTxtPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.InputBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, userTxtPanel.Width - 1, userTxtPanel.Height - 1);
                }
            };
            
            txtUsername = new TextBox();
            txtUsername.BorderStyle = BorderStyle.None;
            txtUsername.BackColor = Theme.InputBg;
            txtUsername.ForeColor = Theme.TextWhite;
            txtUsername.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            txtUsername.Dock = DockStyle.Fill;
            txtUsername.Text = "admin"; // Default helper
            userTxtPanel.Controls.Add(txtUsername);
            this.Controls.Add(userTxtPanel);

            // Password Label
            passLabel = new Label();
            passLabel.Text = "Password";
            passLabel.Location = new Point(50, 255);
            passLabel.AutoSize = true;
            Theme.StyleLabel(passLabel, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(passLabel);

            // Password Input container panel
            passTxtPanel = new Panel();
            passTxtPanel.Size = new Size(350, 42);
            passTxtPanel.Location = new Point(50, 280);
            passTxtPanel.BackColor = Theme.InputBg;
            passTxtPanel.Padding = new Padding(10, 10, 10, 10);
            passTxtPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.InputBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, passTxtPanel.Width - 1, passTxtPanel.Height - 1);
                }
            };

            txtPassword = new TextBox();
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.BackColor = Theme.InputBg;
            txtPassword.ForeColor = Theme.TextWhite;
            txtPassword.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.Text = "admin"; // Default helper
            passTxtPanel.Controls.Add(txtPassword);
            this.Controls.Add(passTxtPanel);

            // Error Label
            errLabel = new Label();
            errLabel.Text = "";
            errLabel.Location = new Point(50, 335);
            errLabel.Size = new Size(350, 20);
            errLabel.TextAlign = ContentAlignment.MiddleLeft;
            Theme.StyleLabel(errLabel, Theme.Danger, Theme.MainFont);
            this.Controls.Add(errLabel);

            // Login Button
            btnLogin = new Button();
            btnLogin.Text = "LOG IN";
            btnLogin.Size = new Size(350, 45);
            btnLogin.Location = new Point(50, 365);
            Theme.StylePrimaryButton(btnLogin);
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            // Exit Button
            btnExit = new Button();
            btnExit.Text = "EXIT APPLICATION";
            btnExit.Size = new Size(350, 40);
            btnExit.Location = new Point(50, 420);
            Theme.StyleDangerButton(btnExit);
            btnExit.Click += BtnExit_Click;
            this.Controls.Add(btnExit);

            // Database Connection Link
            LinkLabel lnkDbSettings = new LinkLabel();
            lnkDbSettings.Text = "⚙️ Configure Database Connection";
            lnkDbSettings.Location = new Point(50, 468);
            lnkDbSettings.Size = new Size(350, 20);
            lnkDbSettings.TextAlign = ContentAlignment.MiddleCenter;
            lnkDbSettings.ActiveLinkColor = Theme.AccentHover;
            lnkDbSettings.LinkColor = Theme.TextDark;
            lnkDbSettings.VisitedLinkColor = Theme.TextDark;
            lnkDbSettings.Font = Theme.MainFont;
            lnkDbSettings.LinkBehavior = LinkBehavior.HoverUnderline;
            lnkDbSettings.LinkClicked += (s, e) => {
                using (var configForm = new DatabaseConfigForm())
                {
                    configForm.ShowDialog(this);
                }
            };
            this.Controls.Add(lnkDbSettings);

            // Enter key binding
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                errLabel.Text = "Please enter both Username and Password.";
                return;
            }

            try
            {
                string hashedInput = DatabaseHelper.HashPassword(password);
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT Id, Username, FullName, Role 
                        FROM Users 
                        WHERE Username = @username AND PasswordHash = @password", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", hashedInput);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Session.UserId = reader.GetInt32(0);
                                Session.Username = reader.GetString(1);
                                Session.FullName = reader.GetString(2);
                                Session.Role = reader.GetString(3);

                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                errLabel.Text = "Invalid username or password.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errLabel.Text = "Database connection failed. Check LocalDB.";
                MessageBox.Show($"Connection Error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
