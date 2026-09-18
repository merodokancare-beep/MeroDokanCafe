using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class DiscountDialog : Form
    {
        private decimal _grossTotal;
        public decimal DiscountAmount { get; private set; }
        public string DiscountReason { get; private set; }
        public bool IsDiscountRemoved { get; private set; }

        private RadioButton radPercent;
        private RadioButton radFlat;
        private TextBox txtValue;
        private ComboBox cmbReason;
        private Label lblPreviewDiscount;
        private Label lblPreviewPayable;
        private Button btnApply;
        private Button btnClear;
        private Button btnCancel;

        public DiscountDialog(decimal grossTotal, decimal currentDiscount = 0, string currentReason = "")
        {
            _grossTotal = Math.Max(0, grossTotal);
            DiscountAmount = currentDiscount;
            DiscountReason = currentReason;

            InitializeComponent();
            SetInitialValues(currentDiscount, currentReason);
            Recalculate();
        }

        private void InitializeComponent()
        {
            this.Text = "Apply Discount / Special Offer";
            this.Size = new Size(460, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;

            // 1. Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Primary,
                Padding = new Padding(16, 10, 16, 10)
            };
            Label lblTitle = new Label
            {
                Text = "🏷️ Apply Bill Discount",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(14, 10),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = $"Current Bill Total: ₹{_grossTotal:N2}",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(16, 34),
                AutoSize = true
            };
            header.Controls.Add(lblSubtitle);
            this.Controls.Add(header);

            // 2. Quick Discount Presets
            Label lblPresets = new Label
            {
                Text = "Quick Presets:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(24, 75),
                AutoSize = true
            };
            this.Controls.Add(lblPresets);

            FlowLayoutPanel presetFlow = new FlowLayoutPanel
            {
                Location = new Point(24, 98),
                Size = new Size(400, 40),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            presetFlow.Controls.Add(CreatePresetBtn("5%", () => SetPreset(true, 5)));
            presetFlow.Controls.Add(CreatePresetBtn("10%", () => SetPreset(true, 10)));
            presetFlow.Controls.Add(CreatePresetBtn("15%", () => SetPreset(true, 15)));
            presetFlow.Controls.Add(CreatePresetBtn("20%", () => SetPreset(true, 20)));
            presetFlow.Controls.Add(CreatePresetBtn("₹50 Off", () => SetPreset(false, 50)));
            presetFlow.Controls.Add(CreatePresetBtn("₹100 Off", () => SetPreset(false, 100)));
            this.Controls.Add(presetFlow);

            // 3. Discount Mode Selector
            Label lblMode = new Label
            {
                Text = "Discount Type:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 148),
                AutoSize = true
            };
            this.Controls.Add(lblMode);

            radPercent = new RadioButton
            {
                Text = "Percentage (%)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(26, 172),
                Size = new Size(140, 24),
                Checked = true
            };
            radPercent.CheckedChanged += (s, e) => Recalculate();
            this.Controls.Add(radPercent);

            radFlat = new RadioButton
            {
                Text = "Flat Amount (₹)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(170, 172),
                Size = new Size(150, 24)
            };
            radFlat.CheckedChanged += (s, e) => Recalculate();
            this.Controls.Add(radFlat);

            // 4. Value Input
            Label lblVal = new Label
            {
                Text = "Discount Value:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 208),
                AutoSize = true
            };
            this.Controls.Add(lblVal);

            txtValue = new TextBox
            {
                Location = new Point(24, 230),
                Size = new Size(395, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtValue.TextChanged += (s, e) => Recalculate();
            this.Controls.Add(txtValue);

            // 5. Reason / Remarks
            Label lblReason = new Label
            {
                Text = "Reason / Offer Name:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 272),
                AutoSize = true
            };
            this.Controls.Add(lblReason);

            cmbReason = new ComboBox
            {
                Location = new Point(24, 294),
                Size = new Size(395, 28),
                Font = Theme.BoldFont,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cmbReason.Items.AddRange(new object[] {
                "Special Offer",
                "Customer Loyalty",
                "Staff / Employee Discount",
                "Manager Privilege",
                "Festival Offer",
                "Happy Hours"
            });
            this.Controls.Add(cmbReason);

            // 6. Live Summary Box
            Panel previewBox = new Panel
            {
                Location = new Point(24, 335),
                Size = new Size(395, 52),
                BackColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblPreviewDiscount = new Label
            {
                Text = "Discount: -₹0.00",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11),
                Location = new Point(10, 8),
                AutoSize = true
            };
            previewBox.Controls.Add(lblPreviewDiscount);

            lblPreviewPayable = new Label
            {
                Text = $"Net Payable: ₹{_grossTotal:0.00}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.Success,
                Location = new Point(10, 28),
                AutoSize = true
            };
            previewBox.Controls.Add(lblPreviewPayable);
            this.Controls.Add(previewBox);

            // 7. Action Buttons
            btnApply = new Button
            {
                Text = "🏷️ Apply Discount",
                Location = new Point(130, 400),
                Size = new Size(140, 36),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += BtnApply_Click;
            this.Controls.Add(btnApply);

            btnClear = new Button
            {
                Text = "✖ Remove",
                Location = new Point(278, 400),
                Size = new Size(85, 36),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => {
                DiscountAmount = 0;
                DiscountReason = "";
                IsDiscountRemoved = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnClear);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(370, 400),
                Size = new Size(50, 36),
                BackColor = Color.FromArgb(45, 55, 75),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancel);
        }

        private Button CreatePresetBtn(string text, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                Padding = new Padding(8, 0, 8, 0),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetPreset(bool isPercent, decimal val)
        {
            if (isPercent)
            {
                radPercent.Checked = true;
                txtValue.Text = val.ToString("0");
            }
            else
            {
                radFlat.Checked = true;
                txtValue.Text = val.ToString("0");
            }
            Recalculate();
        }

        private void SetInitialValues(decimal currentDiscount, string currentReason)
        {
            if (currentDiscount > 0)
            {
                radFlat.Checked = true;
                txtValue.Text = currentDiscount.ToString("0.##");
                cmbReason.Text = currentReason;
            }
            else
            {
                txtValue.Text = "10";
                cmbReason.Text = "Special Offer";
            }
        }

        private void Recalculate()
        {
            if (lblPreviewDiscount == null) return;

            decimal val = 0;
            decimal.TryParse(txtValue?.Text?.Trim(), out val);

            decimal disc = 0;
            if (radPercent.Checked)
            {
                val = Math.Min(100, Math.Max(0, val));
                disc = Math.Round(_grossTotal * (val / 100.0m), 2);
            }
            else
            {
                disc = Math.Min(_grossTotal, Math.Max(0, val));
            }

            decimal payable = Math.Max(0, _grossTotal - disc);
            lblPreviewDiscount.Text = $"Discount: -₹{disc:N2} " + (radPercent.Checked ? $"({val:0}%)" : "");
            lblPreviewPayable.Text = $"Net Payable: ₹{payable:N2}";
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            decimal val = 0;
            if (!decimal.TryParse(txtValue.Text.Trim(), out val) || val < 0)
            {
                MessageBox.Show("Please enter a valid positive discount value.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal disc = 0;
            string reason = cmbReason.Text.Trim();

            if (radPercent.Checked)
            {
                val = Math.Min(100, val);
                disc = Math.Round(_grossTotal * (val / 100.0m), 2);
                if (string.IsNullOrEmpty(reason)) reason = $"{val:0}% Off";
                else reason += $" ({val:0}%)";
            }
            else
            {
                disc = Math.Min(_grossTotal, val);
                if (string.IsNullOrEmpty(reason)) reason = $"Flat ₹{disc:0} Off";
            }

            DiscountAmount = disc;
            DiscountReason = reason;
            IsDiscountRemoved = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
