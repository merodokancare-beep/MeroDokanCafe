using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class DeveloperKeyGenForm : Form
    {
        private Panel authPanel;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnAuth;
        private Button btnCancelAuth;

        private Panel genPanel;
        private Label lblClientHWID;
        private TextBox txtClientHWID;
        private Label lblDuration;
        private ComboBox cmbDuration;
        private DateTimePicker dtpCustomDate;
        private Label lblResultKey;
        private TextBox txtResultKey;
        private Button btnGenerate;
        private Button btnCopy;
        private Button btnClose;

        private const string DevPassword = "merodokandev2026"; // Secret developer password

        public DeveloperKeyGenForm()
        {
            InitializeComponent();
            this.Load += (s, e) => txtPassword.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Developer Key Generator";
            this.ClientSize = new Size(420, 360); // Guarantees exact client area dimension!
            this.AutoScaleMode = AutoScaleMode.Dpi; // Enables robust DPI layout auto-scaling!
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Primary;
            this.ForeColor = Theme.TextLight;
            this.Font = Theme.MainFont;

            // ==========================================
            // AUTH PANEL (Password Verification)
            // ==========================================
            authPanel = new Panel();
            authPanel.Dock = DockStyle.Fill;
            authPanel.BackColor = Theme.Primary;
            this.Controls.Add(authPanel);

            lblPassword = new Label();
            lblPassword.Text = "Enter Developer Password:";
            lblPassword.Location = new Point(30, 80);
            lblPassword.Size = new Size(350, 20);
            Theme.StyleLabel(lblPassword, Theme.TextLight, Theme.BoldFont);
            authPanel.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(30, 110);
            txtPassword.Size = new Size(350, 30);
            txtPassword.UseSystemPasswordChar = true;
            Theme.StyleTextBox(txtPassword);
            txtPassword.KeyPress += TxtPassword_KeyPress;
            authPanel.Controls.Add(txtPassword);

            btnAuth = new Button();
            btnAuth.Text = "Authenticate";
            btnAuth.Location = new Point(220, 170);
            btnAuth.Size = new Size(160, 40);
            Theme.StylePrimaryButton(btnAuth);
            btnAuth.Click += BtnAuth_Click;
            authPanel.Controls.Add(btnAuth);

            btnCancelAuth = new Button();
            btnCancelAuth.Text = "Cancel";
            btnCancelAuth.Location = new Point(30, 170);
            btnCancelAuth.Size = new Size(160, 40);
            Theme.StyleDangerButton(btnCancelAuth);
            btnCancelAuth.Click += (s, e) => this.Close();
            authPanel.Controls.Add(btnCancelAuth);

            // ==========================================
            // GENERATOR PANEL (Active after Auth)
            // ==========================================
            genPanel = new Panel();
            genPanel.Dock = DockStyle.Fill;
            genPanel.BackColor = Theme.Primary;
            genPanel.Visible = false;
            this.Controls.Add(genPanel);

            lblClientHWID = new Label();
            lblClientHWID.Text = "Client Hardware ID:";
            lblClientHWID.Location = new Point(25, 20);
            lblClientHWID.Size = new Size(350, 20);
            Theme.StyleLabel(lblClientHWID, Theme.TextLight, Theme.BoldFont);
            genPanel.Controls.Add(lblClientHWID);

            txtClientHWID = new TextBox();
            txtClientHWID.Location = new Point(25, 45);
            txtClientHWID.Size = new Size(350, 30);
            txtClientHWID.TextAlign = HorizontalAlignment.Center;
            Theme.StyleTextBox(txtClientHWID);
            genPanel.Controls.Add(txtClientHWID);

            lblDuration = new Label();
            lblDuration.Text = "License Duration:";
            lblDuration.Location = new Point(25, 95);
            lblDuration.Size = new Size(170, 20);
            Theme.StyleLabel(lblDuration, Theme.TextLight, Theme.BoldFont);
            genPanel.Controls.Add(lblDuration);

            cmbDuration = new ComboBox();
            cmbDuration.Location = new Point(25, 120);
            cmbDuration.Size = new Size(160, 30);
            cmbDuration.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDuration.Items.AddRange(new string[] { "Lifetime", "1 Year", "1 Month", "Custom" });
            cmbDuration.SelectedIndex = 0;
            Theme.StyleComboBox(cmbDuration);
            cmbDuration.SelectedIndexChanged += CmbDuration_SelectedIndexChanged;
            genPanel.Controls.Add(cmbDuration);

            dtpCustomDate = new DateTimePicker();
            dtpCustomDate.Location = new Point(210, 120);
            dtpCustomDate.Size = new Size(165, 25);
            dtpCustomDate.Font = Theme.MainFont;
            dtpCustomDate.Format = DateTimePickerFormat.Short;
            dtpCustomDate.Visible = false;
            dtpCustomDate.BackColor = Theme.Secondary;
            dtpCustomDate.ForeColor = Theme.TextLight;
            genPanel.Controls.Add(dtpCustomDate);

            lblResultKey = new Label();
            lblResultKey.Text = "Generated Product Key:";
            lblResultKey.Location = new Point(25, 175);
            lblResultKey.Size = new Size(350, 20);
            Theme.StyleLabel(lblResultKey, Theme.TextLight, Theme.BoldFont);
            genPanel.Controls.Add(lblResultKey);

            txtResultKey = new TextBox();
            txtResultKey.Location = new Point(25, 200);
            txtResultKey.Size = new Size(350, 30);
            txtResultKey.ReadOnly = true;
            txtResultKey.TextAlign = HorizontalAlignment.Center;
            txtResultKey.ForeColor = Theme.Success;
            Theme.StyleTextBox(txtResultKey);
            genPanel.Controls.Add(txtResultKey);

            btnGenerate = new Button();
            btnGenerate.Text = "Generate";
            btnGenerate.Location = new Point(25, 260);
            btnGenerate.Size = new Size(110, 40);
            Theme.StylePrimaryButton(btnGenerate);
            btnGenerate.Click += BtnGenerate_Click;
            genPanel.Controls.Add(btnGenerate);

            btnCopy = new Button();
            btnCopy.Text = "Copy Key";
            btnCopy.Location = new Point(145, 260);
            btnCopy.Size = new Size(110, 40);
            Theme.StyleSuccessButton(btnCopy);
            btnCopy.Click += BtnCopy_Click;
            genPanel.Controls.Add(btnCopy);

            btnClose = new Button();
            btnClose.Text = "Close";
            btnClose.Location = new Point(265, 260);
            btnClose.Size = new Size(110, 40);
            Theme.StyleSecondaryButton(btnClose);
            btnClose.Click += (s, e) => this.Close();
            genPanel.Controls.Add(btnClose);
        }

        private void BtnAuth_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text == DevPassword)
            {
                authPanel.Visible = false;
                genPanel.Visible = true;
                
                // Pre-fill the client HWID textbox with the CURRENT machine's ID to make testing/self-generation instant!
                txtClientHWID.Text = LicenseManager.GetHardwareId();
                txtClientHWID.Focus();
            }
            else
            {
                MessageBox.Show("Incorrect developer password.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnAuth_Click(sender, e);
            }
        }

        private void CmbDuration_SelectedIndexChanged(object sender, EventArgs e)
        {
            dtpCustomDate.Visible = (cmbDuration.SelectedItem.ToString() == "Custom");
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string hwid = txtClientHWID.Text.Trim();
            if (string.IsNullOrWhiteSpace(hwid) || !hwid.StartsWith("MDKN-"))
            {
                MessageBox.Show("Please enter a valid client Hardware ID (should start with MDKN-).", "Invalid Hardware ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string duration = cmbDuration.SelectedItem.ToString();
            string expiryCode = "LIFE";

            if (duration == "1 Year")
            {
                expiryCode = DateTime.Today.AddYears(1).ToString("yyyyMMdd");
            }
            else if (duration == "1 Month")
            {
                expiryCode = DateTime.Today.AddMonths(1).ToString("yyyyMMdd");
            }
            else if (duration == "Custom")
            {
                expiryCode = dtpCustomDate.Value.ToString("yyyyMMdd");
            }

            try
            {
                string key = LicenseManager.GenerateProductKey(hwid, expiryCode);
                txtResultKey.Text = key;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate product key: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResultKey.Text))
            {
                Clipboard.SetText(txtResultKey.Text);
                MessageBox.Show("Product key copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
