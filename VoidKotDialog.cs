using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class VoidKotDialog : Form
    {
        private ComboBox cmbReason;
        private TextBox txtComment;
        private CheckBox chkPrintVoidSlip;
        private Button btnConfirm;
        private Button btnCancel;

        public string SelectedReason { get; private set; }
        public string Comment { get; private set; }
        public bool ShouldPrintSlip { get; private set; } = true;

        public VoidKotDialog(string itemName = null, int qty = 1)
        {
            InitializeComponent(itemName, qty);
        }

        private void InitializeComponent(string itemName, int qty)
        {
            this.Text = "Void Item / KOT Cancellation";
            this.Size = new Size(440, 340);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(185, 28, 28) // Danger Red
            };
            Label lblTitle = new Label
            {
                Text = "⚠️ Void Item / Cancel Order",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 16),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            if (!string.IsNullOrEmpty(itemName))
            {
                Label lblItem = new Label
                {
                    Text = $"Item to Void: {itemName} (Qty: {qty})",
                    Font = Theme.BoldFont,
                    ForeColor = Theme.Accent,
                    Location = new Point(24, 70),
                    AutoSize = true
                };
                this.Controls.Add(lblItem);
            }

            Label lblReason = new Label
            {
                Text = "Reason for Cancellation / Void:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 100),
                AutoSize = true
            };
            this.Controls.Add(lblReason);

            cmbReason = new ComboBox
            {
                Location = new Point(24, 125),
                Size = new Size(375, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.MainFont,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbReason.Items.AddRange(new object[] {
                "Guest changed order / mind",
                "Wrong item punched by steward",
                "Kitchen out of ingredients",
                "Item delay / Guest left",
                "Food quality issue",
                "Other / Manager discretion"
            });
            cmbReason.SelectedIndex = 0;
            this.Controls.Add(cmbReason);

            Label lblComment = new Label
            {
                Text = "Void Note / Comment (e.g. 450 / Table Note):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 160),
                AutoSize = true
            };
            this.Controls.Add(lblComment);

            txtComment = new TextBox
            {
                Location = new Point(24, 185),
                Size = new Size(375, 26),
                Font = Theme.MainFont,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            this.Controls.Add(txtComment);

            chkPrintVoidSlip = new CheckBox
            {
                Text = "Print Void KOT Slip to Kitchen printer",
                Checked = true,
                Location = new Point(24, 220),
                AutoSize = true,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight
            };
            this.Controls.Add(chkPrintVoidSlip);

            btnConfirm = new Button
            {
                Text = "Confirm Void",
                Location = new Point(175, 255),
                Size = new Size(130, 36),
                BackColor = Theme.Danger,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(315, 255),
                Size = new Size(85, 36),
                BackColor = Color.FromArgb(50, 60, 80),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            SelectedReason = cmbReason.SelectedItem?.ToString() ?? "Voided";
            Comment = txtComment.Text.Trim();
            if (string.IsNullOrEmpty(Comment))
            {
                Comment = SelectedReason;
            }
            ShouldPrintSlip = chkPrintVoidSlip.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
