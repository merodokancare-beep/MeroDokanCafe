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
        private Label lblTender;
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
        private FlowLayoutPanel cashFlow;

        // Split Payment Controls
        private Panel panelSplit;
        private TextBox txtSplitCash;
        private TextBox txtSplitOnline;
        private TextBox txtSplitCashTendered;
        private Button btnSplitHalf;
        private Button btnBalanceOnline;
        private Button btnBalanceCash;
        private Button btnExactTender;
        private Label lblSplitStatus;
        private bool isUpdatingSplit = false;

        public decimal NetTotal { get; private set; }
        public decimal AmountPaid { get; private set; }
        public decimal TenderedAmount { get; private set; }
        public decimal ChangeDue { get; private set; }
        public decimal CashAmount { get; private set; }
        public decimal OnlineAmount { get; private set; }
        public string PaymentMethod { get; private set; } = "Cash";
        public string CustomerName { get; private set; } = "Walk-in Guest";
        public string CustomerPhone { get; private set; } = "";
        public bool ShouldPrintReceipt { get; private set; } = true;
        private string initialPaymentMethod = "Cash";

        public SettlePaymentDialog(decimal netTotal, string custName = "", string custPhone = "", string defaultMethod = "Cash")
        {
            NetTotal = Math.Round(netTotal, 0); // Rounded invoice value
            CustomerName = string.IsNullOrEmpty(custName) ? "Walk-in Guest" : custName;
            CustomerPhone = custPhone ?? "";
            initialPaymentMethod = string.IsNullOrEmpty(defaultMethod) ? "Cash" : defaultMethod;

            InitializeComponent();
            ApplyInitialPaymentMethod();
            CalculateChange();

            this.Shown += (s, e) => {
                if (panelSplit != null && panelSplit.Visible)
                {
                    txtSplitCash?.Focus();
                    txtSplitCash?.SelectAll();
                }
                else if (txtPaidAmount != null && txtPaidAmount.Visible)
                {
                    txtPaidAmount.Focus();
                    txtPaidAmount.SelectAll();
                }
            };
        }

        private void InitializeComponent()
        {
            this.Text = "Settle & Finalize Bill";
            this.Size = new Size(560, 570);
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
                Height = 60,
                BackColor = Theme.Primary
            };
            Label lblTitle = new Label
            {
                Text = "💳 Payment Settlement",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(18, 16),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            // Bill Total Banner
            Panel banner = new Panel
            {
                Location = new Point(20, 72),
                Size = new Size(505, 75),
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
                Location = new Point(290, 12),
                AutoSize = true
            };
            banner.Controls.Add(lblChgTitle);

            lblChangeDue = new Label
            {
                Text = "₹0.00",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Theme.Success,
                Location = new Point(288, 30),
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
                Location = new Point(20, 158),
                AutoSize = true
            };
            this.Controls.Add(lblPayMode);

            cmbPaymentMethod = new ComboBox
            {
                Location = new Point(20, 182),
                Size = new Size(235, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "UPI / QR Pay", "Card", "Split", "Due / Credit" });
            cmbPaymentMethod.SelectedIndexChanged += (s, e) => OnPaymentMethodChanged();
            this.Controls.Add(cmbPaymentMethod);

            // Standard Single Tender Amount
            lblTender = new Label
            {
                Text = "Amount Tendered (₹):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(275, 158),
                AutoSize = true
            };
            this.Controls.Add(lblTender);

            txtPaidAmount = new TextBox
            {
                Location = new Point(275, 182),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                Text = NetTotal.ToString("0")
            };
            txtPaidAmount.TextChanged += (s, e) => CalculateChange();
            this.Controls.Add(txtPaidAmount);

            // Quick Cash Tender Buttons (For single Cash mode)
            cashFlow = new FlowLayoutPanel
            {
                Location = new Point(20, 222),
                Size = new Size(505, 42),
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

            // Split Payment Card Panel
            BuildSplitPaymentPanel();

            // Customer Info
            Label lblCust = new Label
            {
                Text = "Guest Name:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(20, 355),
                AutoSize = true
            };
            this.Controls.Add(lblCust);

            txtCustomerName = new TextBox
            {
                Location = new Point(20, 377),
                Size = new Size(235, 26),
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
                Location = new Point(275, 355),
                AutoSize = true
            };
            this.Controls.Add(lblPhone);

            txtCustomerPhone = new TextBox
            {
                Location = new Point(275, 377),
                Size = new Size(250, 26),
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
                Location = new Point(20, 415),
                AutoSize = true,
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight
            };
            this.Controls.Add(chkPrintBill);

            // Action Buttons
            btnSettleAndPrint = new Button
            {
                Text = "🖨️ Settle & Print Receipt",
                Location = new Point(20, 450),
                Size = new Size(215, 48),
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
                Location = new Point(245, 450),
                Size = new Size(145, 48),
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
                Location = new Point(400, 450),
                Size = new Size(125, 48),
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

        private void BuildSplitPaymentPanel()
        {
            panelSplit = new Panel
            {
                Location = new Point(20, 220),
                Size = new Size(505, 124),
                BackColor = Color.FromArgb(20, 27, 45),
                Visible = false
            };
            panelSplit.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.Accent, 1.5f))
                    e.Graphics.DrawRectangle(p, 0, 0, panelSplit.Width - 1, panelSplit.Height - 1);
            };

            // Cash Portion Input
            Label lblSplitCash = new Label
            {
                Text = "💵 Cash Part (₹):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(10, 8),
                AutoSize = true
            };
            panelSplit.Controls.Add(lblSplitCash);

            txtSplitCash = new TextBox
            {
                Location = new Point(10, 28),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                Text = "0"
            };
            txtSplitCash.TextChanged += (s, e) => {
                if (isUpdatingSplit) return;
                isUpdatingSplit = true;
                try
                {
                    if (decimal.TryParse(txtSplitCash.Text.Trim(), out decimal c))
                    {
                        decimal cash = Math.Max(0, c);
                        decimal remOnline = Math.Max(0, NetTotal - cash);
                        txtSplitOnline.Text = remOnline.ToString("0");

                        // If cash tendered is empty or was tracking cash, update it
                        if (!decimal.TryParse(txtSplitCashTendered.Text.Trim(), out decimal ct) || ct < cash)
                        {
                            txtSplitCashTendered.Text = cash.ToString("0");
                        }
                    }
                }
                finally
                {
                    isUpdatingSplit = false;
                }
                CalculateChange();
            };
            panelSplit.Controls.Add(txtSplitCash);

            // Online / UPI Portion Input
            Label lblSplitOnline = new Label
            {
                Text = "📱 Online / UPI / Card (₹):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(175, 8),
                AutoSize = true
            };
            panelSplit.Controls.Add(lblSplitOnline);

            txtSplitOnline = new TextBox
            {
                Location = new Point(175, 28),
                Size = new Size(155, 28),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                Text = NetTotal.ToString("0")
            };
            txtSplitOnline.TextChanged += (s, e) => {
                if (isUpdatingSplit) return;
                CalculateChange();
            };
            panelSplit.Controls.Add(txtSplitOnline);

            // Cash Tendered Input (for customer handing note for cash portion)
            Label lblSplitTender = new Label
            {
                Text = "💵 Cash Given (₹):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(345, 8),
                AutoSize = true
            };
            panelSplit.Controls.Add(lblSplitTender);

            txtSplitCashTendered = new TextBox
            {
                Location = new Point(345, 28),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                Text = "0"
            };
            txtSplitCashTendered.TextChanged += (s, e) => {
                if (isUpdatingSplit) return;
                CalculateChange();
            };
            panelSplit.Controls.Add(txtSplitCashTendered);

            // Split Helper Buttons Row
            btnSplitHalf = CreateSplitHelperBtn("½ 50/50", new Point(10, 62), new Size(82, 25));
            btnSplitHalf.Click += (s, e) => {
                isUpdatingSplit = true;
                decimal half = Math.Floor(NetTotal / 2m);
                txtSplitCash.Text = half.ToString("0");
                txtSplitOnline.Text = (NetTotal - half).ToString("0");
                txtSplitCashTendered.Text = half.ToString("0");
                isUpdatingSplit = false;
                CalculateChange();
            };
            panelSplit.Controls.Add(btnSplitHalf);

            btnBalanceOnline = CreateSplitHelperBtn("📱 Auto Balance Online", new Point(98, 62), new Size(145, 25));
            btnBalanceOnline.Click += (s, e) => {
                isUpdatingSplit = true;
                decimal.TryParse(txtSplitCash.Text.Trim(), out decimal c);
                decimal remOnline = Math.Max(0, NetTotal - Math.Max(0, c));
                txtSplitOnline.Text = remOnline.ToString("0");
                isUpdatingSplit = false;
                CalculateChange();
            };
            panelSplit.Controls.Add(btnBalanceOnline);

            btnBalanceCash = CreateSplitHelperBtn("💵 Auto Balance Cash", new Point(249, 62), new Size(140, 25));
            btnBalanceCash.Click += (s, e) => {
                isUpdatingSplit = true;
                decimal.TryParse(txtSplitOnline.Text.Trim(), out decimal o);
                decimal remCash = Math.Max(0, NetTotal - Math.Max(0, o));
                txtSplitCash.Text = remCash.ToString("0");
                txtSplitCashTendered.Text = remCash.ToString("0");
                isUpdatingSplit = false;
                CalculateChange();
            };
            panelSplit.Controls.Add(btnBalanceCash);

            btnExactTender = CreateSplitHelperBtn("Exact Cash", new Point(395, 62), new Size(100, 25));
            btnExactTender.Click += (s, e) => {
                decimal.TryParse(txtSplitCash.Text.Trim(), out decimal c);
                txtSplitCashTendered.Text = Math.Max(0, c).ToString("0");
                CalculateChange();
            };
            panelSplit.Controls.Add(btnExactTender);

            // Status label showing breakdown and balance indicator
            lblSplitStatus = new Label
            {
                Location = new Point(10, 94),
                Size = new Size(485, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.Success,
                Text = $"✓ Split Balanced: Cash ₹0 + Online ₹{NetTotal:0} = ₹{NetTotal:0}"
            };
            panelSplit.Controls.Add(lblSplitStatus);

            this.Controls.Add(panelSplit);
        }

        private Button CreateSplitHelperBtn(string text, Point loc, Size sz)
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.CardBorder;
            return btn;
        }

        private void ApplyInitialPaymentMethod()
        {
            int selIdx = 0;
            if (initialPaymentMethod.IndexOf("UPI", StringComparison.OrdinalIgnoreCase) >= 0) selIdx = 1;
            else if (initialPaymentMethod.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0) selIdx = 2;
            else if (initialPaymentMethod.IndexOf("Split", StringComparison.OrdinalIgnoreCase) >= 0) selIdx = 3;
            else if (initialPaymentMethod.IndexOf("Due", StringComparison.OrdinalIgnoreCase) >= 0 || initialPaymentMethod.IndexOf("Credit", StringComparison.OrdinalIgnoreCase) >= 0) selIdx = 4;
            cmbPaymentMethod.SelectedIndex = selIdx;
            OnPaymentMethodChanged();
        }

        private void OnPaymentMethodChanged()
        {
            PaymentMethod = cmbPaymentMethod.SelectedItem?.ToString() ?? "Cash";

            if (PaymentMethod == "Split")
            {
                panelSplit.Visible = true;
                cashFlow.Visible = false;
                lblTender.Visible = false;
                txtPaidAmount.Visible = false;

                if (txtSplitOnline != null && (txtSplitOnline.Text == "0" || string.IsNullOrWhiteSpace(txtSplitOnline.Text)))
                {
                    isUpdatingSplit = true;
                    txtSplitCash.Text = "0";
                    txtSplitOnline.Text = NetTotal.ToString("0");
                    txtSplitCashTendered.Text = "0";
                    isUpdatingSplit = false;
                }
            }
            else if (PaymentMethod == "Cash")
            {
                panelSplit.Visible = false;
                cashFlow.Visible = true;
                lblTender.Visible = true;
                lblTender.Text = "Amount Tendered (₹):";
                txtPaidAmount.Visible = true;
                txtPaidAmount.Enabled = true;
                if (string.IsNullOrWhiteSpace(txtPaidAmount.Text) || txtPaidAmount.Text == "0")
                {
                    txtPaidAmount.Text = NetTotal.ToString("0");
                }
            }
            else if (PaymentMethod == "Due / Credit")
            {
                panelSplit.Visible = false;
                cashFlow.Visible = false;
                lblTender.Visible = true;
                lblTender.Text = "Amount Tendered (₹):";
                txtPaidAmount.Visible = true;
                txtPaidAmount.Text = "0";
                txtPaidAmount.Enabled = false;
            }
            else // UPI or Card
            {
                panelSplit.Visible = false;
                cashFlow.Visible = false;
                lblTender.Visible = true;
                lblTender.Text = "Amount Tendered (₹):";
                txtPaidAmount.Visible = true;
                txtPaidAmount.Text = NetTotal.ToString("0");
                txtPaidAmount.Enabled = false;
            }

            CalculateChange();
        }

        private Button CreateQuickCashBtn(string text, decimal val)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(76, 32),
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
            if (PaymentMethod == "Split")
            {
                decimal.TryParse(txtSplitCash?.Text?.Trim() ?? "0", out decimal splitCash);
                decimal.TryParse(txtSplitOnline?.Text?.Trim() ?? "0", out decimal splitOnline);
                decimal.TryParse(txtSplitCashTendered?.Text?.Trim() ?? "0", out decimal splitTender);

                CashAmount = Math.Max(0, splitCash);
                OnlineAmount = Math.Max(0, splitOnline);
                decimal totalSplit = CashAmount + OnlineAmount;
                AmountPaid = totalSplit;

                // Change due calculation: if cash given exceeds cash portion
                if (splitTender > CashAmount && CashAmount > 0)
                {
                    ChangeDue = splitTender - CashAmount;
                    lblChangeDue.Text = $"₹{ChangeDue:0.00}";
                    TenderedAmount = splitTender + OnlineAmount;
                }
                else
                {
                    ChangeDue = 0m;
                    lblChangeDue.Text = "₹0.00";
                    TenderedAmount = totalSplit;
                }

                if (lblSplitStatus != null)
                {
                    if (totalSplit == NetTotal)
                    {
                        lblSplitStatus.ForeColor = Theme.Success;
                        lblSplitStatus.Text = $"✓ Split Balanced: Cash ₹{CashAmount:0} + Online ₹{OnlineAmount:0} = ₹{NetTotal:0}";
                    }
                    else if (totalSplit < NetTotal)
                    {
                        lblSplitStatus.ForeColor = Theme.Accent; // Warning amber
                        lblSplitStatus.Text = $"⚠️ Remaining Due: ₹{NetTotal - totalSplit:0} (Cash ₹{CashAmount:0} + Online ₹{OnlineAmount:0} = ₹{totalSplit:0})";
                    }
                    else
                    {
                        lblSplitStatus.ForeColor = Color.FromArgb(239, 68, 68); // Red
                        lblSplitStatus.Text = $"⚠️ Split exceeds total by ₹{totalSplit - NetTotal:0} (Cash ₹{CashAmount:0} + Online ₹{OnlineAmount:0} = ₹{totalSplit:0})";
                    }
                }
                return;
            }

            if (decimal.TryParse(txtPaidAmount.Text.Trim(), out decimal tender))
            {
                TenderedAmount = Math.Max(0, tender);
                if (PaymentMethod == "Due / Credit")
                {
                    AmountPaid = 0;
                    ChangeDue = 0;
                    CashAmount = 0;
                    OnlineAmount = 0;
                    lblChangeDue.Text = "₹0.00";
                }
                else if (PaymentMethod == "Cash")
                {
                    OnlineAmount = 0;
                    if (TenderedAmount >= NetTotal)
                    {
                        AmountPaid = NetTotal;
                        CashAmount = NetTotal;
                        ChangeDue = TenderedAmount - NetTotal;
                        lblChangeDue.Text = $"₹{ChangeDue:0.00}";
                    }
                    else
                    {
                        AmountPaid = TenderedAmount;
                        CashAmount = TenderedAmount;
                        ChangeDue = 0;
                        lblChangeDue.Text = "₹0.00";
                    }
                }
                else // UPI or Card
                {
                    AmountPaid = NetTotal;
                    CashAmount = 0;
                    OnlineAmount = NetTotal;
                    ChangeDue = 0;
                    lblChangeDue.Text = "₹0.00";
                }
            }
            else
            {
                TenderedAmount = 0;
                AmountPaid = 0;
                CashAmount = 0;
                OnlineAmount = 0;
                ChangeDue = 0;
                lblChangeDue.Text = "₹0.00";
            }
        }

        private void FinishSettlement()
        {
            if (PaymentMethod == "Split")
            {
                decimal totalSplit = CashAmount + OnlineAmount;
                if (totalSplit > NetTotal)
                {
                    MessageBox.Show($"Split payment (Cash: ₹{CashAmount:0} + Online: ₹{OnlineAmount:0} = ₹{totalSplit:0}) exceeds net total of ₹{NetTotal:0}.\n\nPlease adjust the split amounts before finalizing.", "Invalid Split Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (totalSplit < NetTotal)
                {
                    decimal dueBal = NetTotal - totalSplit;
                    var res = MessageBox.Show($"Split total (₹{totalSplit:0}) is less than net total (₹{NetTotal:0}).\n\nMark remaining balance of ₹{dueBal:0} as Due?", "Split Underpayment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res != DialogResult.Yes) return;
                }
            }
            else if (PaymentMethod == "Cash" && TenderedAmount < NetTotal)
            {
                decimal dueBal = NetTotal - TenderedAmount;
                var res = MessageBox.Show($"Tendered cash (₹{TenderedAmount:N2}) is less than net total (₹{NetTotal:N2}).\n\nMark remaining balance of ₹{dueBal:N2} as Due?", "Underpayment", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes) return;
            }

            CustomerName = txtCustomerName.Text.Trim();
            CustomerPhone = txtCustomerPhone.Text.Trim();
            PaymentMethod = cmbPaymentMethod.SelectedItem?.ToString() ?? "Cash";
            ShouldPrintReceipt = ShouldPrintReceipt && (chkPrintBill?.Checked ?? false);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
