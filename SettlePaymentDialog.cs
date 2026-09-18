using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class SettlePaymentDialog : Form
    {
        private Label lblTotalAmount;
        private Label lblChangeDue;
        private TextBox txtPaidAmount;
        private ComboBox cmbPaymentMethod;
        private TextBox txtCustomerName;
        private TextBox txtCustomerPhone;
        private Button btnExact;
        private Button btn100;
        private Button btn200;
        private Button btn500;
        private Button btn1000;
        private Button btn2000;
        private Button btnSettleAndPrint;
        private Button btnSettleOnly;
        private Button btnCancel;
        private CheckBox chkPrintBill;

        public decimal NetTotal { get; private set; }
        public decimal AmountPaid { get; private set; }
        public decimal ChangeDue { get; private set; }
        public string PaymentMethod { get; private set; } = "Cash";
        public string CustomerName { get; private set; } = "Walk-in Guest";
        public string CustomerPhone { get; private set; } = "";
        public bool ShouldPrintReceipt { get; private set; } = true;

        public SettlePaymentDialog(decimal netTotal, string custName = "", string custPhone = "")
        {
            NetTotal = Math.Round(netTotal, 0); // Rounded invoice value
            CustomerName = string.IsNullOrEmpty(custName) ? "Walk-in Guest" : custName;
            CustomerPhone = custPhone ?? "";

            InitializeComponent();
            CalculateChange();
        }

        private void InitializeComponent()
        {
            this.Text = "Settle & Finalize Bill";
            this.Size = new Size(540, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;

            // Top Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.Primary
            };
            Label lblTitle = new Label
            {
                Text = "💳 Payment Settlement",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(18, 18),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            // Bill Total Banner
            Panel banner = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(485, 75),
                BackColor = Color.FromArgb(15, 23, 42)
            };
            banner.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.Accent, 2))
                    e.Graphics.DrawRectangle(p, 0, 0, banner.Width - 1, banner.Height - 1);
            };

            Label lblTotTitle = new Label
            {
                Text = "NET INVOICE VALUE:",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 12),
                AutoSize = true
            };
            banner.Controls.Add(lblTotTitle);

            lblTotalAmount = new Label
            {
                Text = $"₹{NetTotal:0}",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(14, 28),
                AutoSize = true
            };
            banner.Controls.Add(lblTotalAmount);

            Label lblChgTitle = new Label
            {
                Text = "CHANGE DUE:",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(280, 12),
                AutoSize = true
            };
            banner.Controls.Add(lblChgTitle);

            lblChangeDue = new Label
            {
                Text = "₹0.00",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Theme.Success,
                Location = new Point(278, 32),
                AutoSize = true
            };
            banner.Controls.Add(lblChangeDue);

            this.Controls.Add(banner);

            // Payment Mode Dropdown
            Label lblPayMode = new Label
            {
                Text = "Payment Method:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(20, 170),
                AutoSize = true
            };
            this.Controls.Add(lblPayMode);

            cmbPaymentMethod = new ComboBox
            {
                Location = new Point(20, 195),
                Size = new Size(230, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "UPI / QR Pay", "Card", "Split Payment", "Due / Credit" });
            cmbPaymentMethod.SelectedIndex = 0;
            cmbPaymentMethod.SelectedIndexChanged += (s, e) => {
                PaymentMethod = cmbPaymentMethod.SelectedItem.ToString();
                if (PaymentMethod != "Cash")
                {
                    txtPaidAmount.Text = NetTotal.ToString("0");
                }
            };
            this.Controls.Add(cmbPaymentMethod);

            // Amount Received / Tendered
            Label lblTender = new Label
            {
                Text = "Amount Tendered (₹):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(275, 170),
                AutoSize = true
            };
            this.Controls.Add(lblTender);

            txtPaidAmount = new TextBox
            {
                Location = new Point(275, 195),
                Size = new Size(230, 30),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                Text = NetTotal.ToString("0")
            };
            txtPaidAmount.TextChanged += (s, e) => CalculateChange();
            this.Controls.Add(txtPaidAmount);

            // Quick Cash Tender Buttons
            FlowLayoutPanel cashFlow = new FlowLayoutPanel
            {
                Location = new Point(20, 235),
                Size = new Size(485, 45),
                FlowDirection = FlowDirection.LeftToRight
            };

            btnExact = CreateQuickCashBtn("Exact (₹" + NetTotal.ToString("0") + ")", NetTotal);
            btn100 = CreateQuickCashBtn("₹100", 100);
            btn200 = CreateQuickCashBtn("₹200", 200);
            btn500 = CreateQuickCashBtn("₹500", 500);
            btn1000 = CreateQuickCashBtn("₹1000", 1000);
            btn2000 = CreateQuickCashBtn("₹2000", 2000);

            cashFlow.Controls.Add(btnExact);
            cashFlow.Controls.Add(btn100);
            cashFlow.Controls.Add(btn200);
            cashFlow.Controls.Add(btn500);
            cashFlow.Controls.Add(btn1000);
            cashFlow.Controls.Add(btn2000);
            this.Controls.Add(cashFlow);

            // Customer Info
            Label lblCust = new Label
            {
                Text = "Guest Name:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(20, 290),
                AutoSize = true
            };
            this.Controls.Add(lblCust);

            txtCustomerName = new TextBox
            {
                Location = new Point(20, 315),
                Size = new Size(230, 26),
                Font = Theme.MainFont,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                Text = CustomerName
            };
            this.Controls.Add(txtCustomerName);

            Label lblPhone = new Label
            {
                Text = "Mobile No:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(275, 290),
                AutoSize = true
            };
            this.Controls.Add(lblPhone);

            txtCustomerPhone = new TextBox
            {
                Location = new Point(275, 315),
                Size = new Size(230, 26),
                Font = Theme.MainFont,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                Text = CustomerPhone
            };
            this.Controls.Add(txtCustomerPhone);

            // Checkbox Print
            chkPrintBill = new CheckBox
            {
                Text = "Print 80mm Thermal Receipt Immediately",
                Checked = true,
                Location = new Point(20, 360),
                AutoSize = true,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight
            };
            this.Controls.Add(chkPrintBill);

            // Action Buttons
            btnSettleAndPrint = new Button
            {
                Text = "🖨️ Settle & Print Receipt",
                Location = new Point(20, 405),
                Size = new Size(210, 48),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSettleAndPrint.FlatAppearance.BorderSize = 0;
            btnSettleAndPrint.Click += (s, e) => {
                ShouldPrintReceipt = true;
                FinishSettlement();
            };
            this.Controls.Add(btnSettleAndPrint);

            btnSettleOnly = new Button
            {
                Text = "✓ Settle Only",
                Location = new Point(245, 405),
                Size = new Size(140, 48),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSettleOnly.FlatAppearance.BorderSize = 0;
            btnSettleOnly.Click += (s, e) => {
                ShouldPrintReceipt = false;
                FinishSettlement();
            };
            this.Controls.Add(btnSettleOnly);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(400, 405),
                Size = new Size(105, 48),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private Button CreateQuickCashBtn(string text, decimal val)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(74, 32),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderColor = Theme.CardBorder;
            btn.Click += (s, e) => {
                txtPaidAmount.Text = val.ToString("0");
            };
            return btn;
        }

        private void CalculateChange()
        {
            if (decimal.TryParse(txtPaidAmount.Text.Trim(), out decimal paid))
            {
                AmountPaid = paid;
                ChangeDue = Math.Max(0, paid - NetTotal);
                lblChangeDue.Text = $"₹{ChangeDue:0.00}";
            }
            else
            {
                AmountPaid = 0;
                ChangeDue = 0;
                lblChangeDue.Text = "₹0.00";
            }
        }

        private void FinishSettlement()
        {
            if (AmountPaid < NetTotal && PaymentMethod == "Cash")
            {
                var res = MessageBox.Show($"Tendered cash (₹{AmountPaid}) is less than net total (₹{NetTotal}). Mark balance as Due?", "Underpayment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
            }

            CustomerName = txtCustomerName.Text.Trim();
            CustomerPhone = txtCustomerPhone.Text.Trim();
            PaymentMethod = cmbPaymentMethod.SelectedItem.ToString();
            ShouldPrintReceipt = chkPrintBill.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
