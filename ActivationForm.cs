using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class ActivationForm : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblHardwareId;
        private TextBox txtHardwareId;
        private Button btnCopy;
        
        private Label lblProductKey;
        private TextBox txtProductKey;
        private Button btnActivate;
        private Button btnExit;
        private Label lblStatus;

        public ActivationForm()
        {
            InitializeComponent();
            LoadHardwareId();
            this.KeyPreview = true;
            this.KeyDown += ActivationForm_KeyDown;
            this.Load += (s, e) => txtProductKey.Focus();
        }

        private void ActivationForm_KeyDown(object sender, KeyEventArgs e)
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
            this.Text = "Mero Dokan - License Activation";
            this.ClientSize = new Size(560, 430); // Sets inner client area precisely
            this.AutoScaleMode = AutoScaleMode.Dpi; // Auto-scales controls for high DPI laptop monitors!
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Primary;
            this.Icon = Theme.AppIcon;
            this.ForeColor = Theme.TextLight;
            this.Font = Theme.MainFont;

            // Form Title Panel (Dark Slate Accent)
            Panel headerPanel = new Panel();
            headerPanel.Size = new Size(560, 70); // Taller header panel
            headerPanel.Location = new Point(0, 0);
            headerPanel.BackColor = Theme.Secondary;
            this.Controls.Add(headerPanel);

            lblTitle = new Label();
            lblTitle.Text = "MERO DOKAN - ACTIVATION";
            lblTitle.Location = new Point(25, 20);
            lblTitle.Size = new Size(500, 30);
            Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.HeaderFont);
            headerPanel.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Your copy of Mero Dokan is unregistered. Please activate to proceed.";
            lblSubtitle.Location = new Point(25, 85);
            lblSubtitle.Size = new Size(510, 20);
            Theme.StyleLabel(lblSubtitle, Theme.TextDark, Theme.MainFont);
            this.Controls.Add(lblSubtitle);

            // Hardware ID Label
            lblHardwareId = new Label();
            lblHardwareId.Text = "Step 1: Copy your unique Hardware ID and send it to the developer";
            lblHardwareId.Location = new Point(25, 120);
            lblHardwareId.Size = new Size(510, 20);
            Theme.StyleLabel(lblHardwareId, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(lblHardwareId);

            // Hardware ID TextBox (Large font for highly visible text)
            txtHardwareId = new TextBox();
            txtHardwareId.Location = new Point(25, 145);
            txtHardwareId.Size = new Size(370, 32);
            txtHardwareId.ReadOnly = true;
            txtHardwareId.TextAlign = HorizontalAlignment.Center;
            Theme.StyleTextBox(txtHardwareId);
            txtHardwareId.Font = new Font("Segoe UI", 12F, FontStyle.Regular); // 12pt font makes it larger & clearer
            this.Controls.Add(txtHardwareId);

            // Copy Button (Wider to prevent text cutting off)
            btnCopy = new Button();
            btnCopy.Text = "Copy ID";
            btnCopy.Location = new Point(410, 143);
            btnCopy.Size = new Size(120, 32); // Wider and matches textbox height perfectly
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Click += BtnCopy_Click;
            this.Controls.Add(btnCopy);

            // Product Key Label
            lblProductKey = new Label();
            lblProductKey.Text = "Step 2: Enter the Product Key provided by the developer";
            lblProductKey.Location = new Point(25, 195);
            lblProductKey.Size = new Size(510, 20);
            Theme.StyleLabel(lblProductKey, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(lblProductKey);

            // Product Key TextBox (Large font for easy typing and visibility)
            txtProductKey = new TextBox();
            txtProductKey.Location = new Point(25, 220);
            txtProductKey.Size = new Size(505, 32);
            txtProductKey.TextAlign = HorizontalAlignment.Center;
            Theme.StyleTextBox(txtProductKey);
            txtProductKey.Font = new Font("Segoe UI", 12F, FontStyle.Regular); // Taller & larger text
            txtProductKey.KeyPress += TxtProductKey_KeyPress;
            this.Controls.Add(txtProductKey);

            // Status Label
            lblStatus = new Label();
            lblStatus.Text = "Waiting for activation key...";
            lblStatus.Location = new Point(25, 265);
            lblStatus.Size = new Size(505, 50);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            Theme.StyleLabel(lblStatus, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblStatus);

            // Exit Button
            btnExit = new Button();
            btnExit.Text = "Close App";
            btnExit.Location = new Point(85, 330);
            btnExit.Size = new Size(185, 40); // Bigger buttons for premium look
            Theme.StyleDangerButton(btnExit);
            btnExit.Click += BtnExit_Click;
            this.Controls.Add(btnExit);

            // Activate Button
            btnActivate = new Button();
            btnActivate.Text = "Activate";
            btnActivate.Location = new Point(290, 330);
            btnActivate.Size = new Size(185, 40); // Same dimensions, neatly aligned
            Theme.StylePrimaryButton(btnActivate);
            btnActivate.Click += BtnActivate_Click;
            this.Controls.Add(btnActivate);
        }

        private void LoadHardwareId()
        {
            txtHardwareId.Text = LicenseManager.GetHardwareId();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtHardwareId.Text))
            {
                Clipboard.SetText(txtHardwareId.Text);
                lblStatus.Text = "Hardware ID copied to clipboard!";
                lblStatus.ForeColor = Theme.Success;
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            string key = txtProductKey.Text.Trim();
            if (LicenseManager.ValidateProductKey(key, out string message, out DateTime expiryDate))
            {
                LicenseManager.SaveLicenseKey(key);
                
                string expiryMsg = expiryDate == DateTime.MaxValue ? "Lifetime License!" : $"valid until {expiryDate.ToString("yyyy-MM-dd")}";
                
                MessageBox.Show($"Mero Dokan activated successfully!\n\nLicense Type: {expiryMsg}", 
                                "Activation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.Text = $"Activation Failed:\n{message}";
                lblStatus.ForeColor = Theme.Danger;
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void TxtProductKey_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnActivate_Click(sender, e);
            }
        }
    }
}
