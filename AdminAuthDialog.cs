using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class AdminAuthDialog : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Label lblError;
        private Button btnAuthorize;
        private Button btnCancel;
        private string actionDescription;

        public string AuthorizedUsername { get; private set; }
        public string AuthorizedFullName { get; private set; }

        public AdminAuthDialog(string reason = "modify or adjust this finalized bill")
        {
            this.actionDescription = reason;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🔒 Admin Security Verification";
            this.Size = new Size(460, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Theme.Secondary;
            this.ForeColor = Theme.TextLight;

            Panel card = Theme.CreateCard(420, 320);
            card.Location = new Point(12, 12);
            card.BackColor = Theme.CardBg;
            this.Controls.Add(card);

            // Header Icon & Title
            Label lblTitle = new Label();
            lblTitle.Text = "🛡️ Manager / Admin Authorization";
            lblTitle.Location = new Point(15, 14);
            lblTitle.Size = new Size(390, 26);
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Accent;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblTitle);

            // Subtitle Reason
            Label lblReason = new Label();
            lblReason.Text = $"Admin credentials required to {actionDescription}.";
            lblReason.Location = new Point(15, 42);
            lblReason.Size = new Size(390, 36);
            lblReason.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblReason.ForeColor = Theme.TextMuted;
            lblReason.TextAlign = ContentAlignment.TopCenter;
            card.Controls.Add(lblReason);

            // Username Label & Textbox
            Label lblUser = new Label();
            lblUser.Text = "Admin Username *";
            lblUser.Location = new Point(25, 86);
            lblUser.Size = new Size(370, 18);
            lblUser.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblUser.ForeColor = Theme.TextLight;
            card.Controls.Add(lblUser);

            txtUsername = new TextBox();
            txtUsername.Location = new Point(25, 106);
            txtUsername.Size = new Size(370, 26);
            txtUsername.Font = new Font("Segoe UI", 10F);
            txtUsername.BackColor = Theme.Secondary;
            txtUsername.ForeColor = Color.White;
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(txtUsername);

            // Password Label & Textbox
            Label lblPass = new Label();
            lblPass.Text = "Admin Password *";
            lblPass.Location = new Point(25, 142);
            lblPass.Size = new Size(370, 18);
            lblPass.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPass.ForeColor = Theme.TextLight;
            card.Controls.Add(lblPass);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(25, 162);
            txtPassword.Size = new Size(370, 26);
            txtPassword.Font = new Font("Segoe UI", 10F);
            txtPassword.BackColor = Theme.Secondary;
            txtPassword.ForeColor = Color.White;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.UseSystemPasswordChar = true;
            card.Controls.Add(txtPassword);

            // Error Label
            lblError = new Label();
            lblError.Location = new Point(25, 196);
            lblError.Size = new Size(370, 36);
            lblError.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblError.ForeColor = Theme.Danger;
            lblError.TextAlign = ContentAlignment.TopCenter;
            lblError.Text = "";
            card.Controls.Add(lblError);

            // Action Buttons
            btnAuthorize = new Button();
            btnAuthorize.Text = "🔓 Verify & Unlock";
            btnAuthorize.Size = new Size(180, 38);
            btnAuthorize.Location = new Point(215, 252);
            Theme.StylePrimaryButton(btnAuthorize);
            btnAuthorize.Click += BtnAuthorize_Click;
            card.Controls.Add(btnAuthorize);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(175, 38);
            btnCancel.Location = new Point(25, 252);
            Theme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            card.Controls.Add(btnCancel);

            this.AcceptButton = btnAuthorize;
            this.CancelButton = btnCancel;

            // Pre-fill if current logged-in user has Admin role
            if (!string.IsNullOrEmpty(Session.Username) && (Session.Role == "Admin" || Session.Role == "Administrator" || Session.Role == "Owner" || Session.Role == "Manager"))
            {
                txtUsername.Text = Session.Username;
                this.ActiveControl = txtPassword;
            }
            else
            {
                this.ActiveControl = txtUsername;
            }
        }

        private void BtnAuthorize_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter both Admin Username and Password.";
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
                        WHERE Username = @username 
                          AND PasswordHash = @password 
                          AND Role IN ('Admin', 'Administrator', 'Owner', 'Manager', 'SuperAdmin')", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", hashedInput);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                AuthorizedUsername = reader.GetString(1);
                                AuthorizedFullName = reader.GetString(2);
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                lblError.Text = "Invalid admin credentials or insufficient role permissions.";
                                txtPassword.SelectAll();
                                txtPassword.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"Database Error: {ex.Message}";
            }
        }

        public static bool VerifyAdmin(IWin32Window parent, string actionDescription = "modify or adjust this finalized bill")
        {
            using (var dlg = new AdminAuthDialog(actionDescription))
            {
                return dlg.ShowDialog(parent) == DialogResult.OK;
            }
        }
    }
}
