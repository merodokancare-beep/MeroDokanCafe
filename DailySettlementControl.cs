using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class DailySettlementControl : UserControl
    {
        private DateTimePicker dtpSettlementDate;
        private Button btnRefresh;

        // UI Controls
        private Label lblTotalSaleToday;
        private Label lblDueToday;
        private Label lblPrevDueRepayments;
        private Label lblOpeningCashText;
        private Label lblCardPayment;
        private Label lblQRPayment;
        private Label lblTotalReturn;
        private Label lblExpectedCash;
        private TextBox txtActualCash;
        private TextBox txtRemarks;
        private Label lblStatusMessage;
        private Button btnSaveSettlement;

        // History
        private DataGridView gridHistory;

        // State calculations
        private decimal cashSales = 0;
        private decimal prevDueRepayments = 0;
        private decimal todayDueRepayments = 0;
        private decimal duesCreated = 0;
        private decimal expectedCash = 0;
        private decimal totalSaleToday = 0;
        private decimal dueTodayUnpaid = 0;
        private decimal openingCash = 0;
        private decimal totalReturns = 0;
        private decimal cashRefunds = 0;
        private decimal cardSales = 0;
        private decimal qrSales = 0;
        private decimal cardDueRepayments = 0;
        private decimal qrDueRepayments = 0;
        private decimal totalCardPayment = 0;
        private decimal totalQRPayment = 0;
        private decimal totalOnlinePayment = 0;

        public DailySettlementControl()
        {
            InitializeComponent();
            LoadOpeningCash();
            LoadTodayMetrics();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 680);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Daily Cash Register & Settlement";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Top bar - Date Selection
            Panel topBar = new Panel();
            topBar.Size = new Size(910, 45);
            topBar.Location = new Point(20, 60);
            topBar.BackColor = Color.Transparent;

            Label lblDate = new Label();
            lblDate.Text = "Settlement Date:";
            lblDate.Location = new Point(0, 12);
            lblDate.AutoSize = true;
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            topBar.Controls.Add(lblDate);

            dtpSettlementDate = new DateTimePicker();
            dtpSettlementDate.Size = new Size(150, 28);
            dtpSettlementDate.Location = new Point(190, 8);
            dtpSettlementDate.Font = Theme.MainFont;
            dtpSettlementDate.Format = DateTimePickerFormat.Short;
            dtpSettlementDate.Value = DateTime.Today;
            dtpSettlementDate.ValueChanged += (s, e) => { LoadOpeningCash(); LoadTodayMetrics(); };
            topBar.Controls.Add(dtpSettlementDate);

            btnRefresh = new Button();
            btnRefresh.Text = "🔄 Refresh Metrics";
            btnRefresh.Size = new Size(180, 32);
            btnRefresh.Location = new Point(370, 6);
            Theme.StylePrimaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => { LoadOpeningCash(); LoadTodayMetrics(); };
            topBar.Controls.Add(btnRefresh);

            this.Controls.Add(topBar);

            // Unified Card for Settlement Fields
            Panel mainPanel = new Panel();
            mainPanel.Size = new Size(910, 290);
            mainPanel.Location = new Point(20, 115);
            mainPanel.BackColor = Color.Transparent;
            this.Controls.Add(mainPanel);

            Panel cardMain = Theme.CreateCard(910, 290);
            cardMain.Dock = DockStyle.Fill;
            cardMain.BackColor = Color.FromArgb(17, 24, 39);
            mainPanel.Controls.Add(cardMain);

            // COLUMN 1: Daily Figures & Dues
            Label lblCol1Header = new Label();
            lblCol1Header.Text = "DAILY REVENUE & CASH SUMMARY";
            lblCol1Header.Location = new Point(20, 15);
            lblCol1Header.AutoSize = true;
            Theme.StyleLabel(lblCol1Header, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardMain.Controls.Add(lblCol1Header);

            // 1. Total Sale Today
            Label lblSaleTitle = new Label();
            lblSaleTitle.Text = "Total Sale Today:";
            lblSaleTitle.Location = new Point(20, 42);
            lblSaleTitle.AutoSize = true;
            Theme.StyleLabel(lblSaleTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblSaleTitle);

            lblTotalSaleToday = new Label();
            lblTotalSaleToday.Text = "Rs. 0.00";
            lblTotalSaleToday.Location = new Point(280, 42);
            lblTotalSaleToday.AutoSize = true;
            Theme.StyleLabel(lblTotalSaleToday, Theme.Accent, Theme.BoldFont);
            cardMain.Controls.Add(lblTotalSaleToday);

            // 2. Due Today (Unpaid)
            Label lblDueTitle = new Label();
            lblDueTitle.Text = "Due Today (Unpaid):";
            lblDueTitle.Location = new Point(20, 72);
            lblDueTitle.AutoSize = true;
            Theme.StyleLabel(lblDueTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblDueTitle);

            lblDueToday = new Label();
            lblDueToday.Text = "Rs. 0.00";
            lblDueToday.Location = new Point(280, 72);
            lblDueToday.AutoSize = true;
            Theme.StyleLabel(lblDueToday, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblDueToday);

            // 3. Previous Due Repayments
            Label lblPrevDueTitle = new Label();
            lblPrevDueTitle.Text = "Prev Date Due Repayment:";
            lblPrevDueTitle.Location = new Point(20, 102);
            lblPrevDueTitle.AutoSize = true;
            Theme.StyleLabel(lblPrevDueTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblPrevDueTitle);

            lblPrevDueRepayments = new Label();
            lblPrevDueRepayments.Text = "Rs. 0.00";
            lblPrevDueRepayments.Location = new Point(280, 102);
            lblPrevDueRepayments.AutoSize = true;
            Theme.StyleLabel(lblPrevDueRepayments, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblPrevDueRepayments);

            // Opening Cash (Hidden)
            lblOpeningCashText = new Label();
            lblOpeningCashText.Text = "Rs. 0.00";
            lblOpeningCashText.Location = new Point(280, 132);
            lblOpeningCashText.AutoSize = true;
            lblOpeningCashText.Visible = false;
            Theme.StyleLabel(lblOpeningCashText, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblOpeningCashText);

            // 4. Card Payment
            Label lblCardTitle = new Label();
            lblCardTitle.Text = "Card Payment:";
            lblCardTitle.Location = new Point(20, 132);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblCardTitle);

            lblCardPayment = new Label();
            lblCardPayment.Text = "Rs. 0.00";
            lblCardPayment.Location = new Point(280, 132);
            lblCardPayment.AutoSize = true;
            Theme.StyleLabel(lblCardPayment, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblCardPayment);

            // 5. QR / UPI Payment
            Label lblQRTitle = new Label();
            lblQRTitle.Text = "QR / UPI Payment:";
            lblQRTitle.Location = new Point(20, 162);
            lblQRTitle.AutoSize = true;
            Theme.StyleLabel(lblQRTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblQRTitle);

            lblQRPayment = new Label();
            lblQRPayment.Text = "Rs. 0.00";
            lblQRPayment.Location = new Point(280, 162);
            lblQRPayment.AutoSize = true;
            Theme.StyleLabel(lblQRPayment, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblQRPayment);

            // 6. Total Return / Refunds
            Label lblReturnTitle = new Label();
            lblReturnTitle.Text = "Total Return:";
            lblReturnTitle.Location = new Point(20, 192);
            lblReturnTitle.AutoSize = true;
            Theme.StyleLabel(lblReturnTitle, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblReturnTitle);

            lblTotalReturn = new Label();
            lblTotalReturn.Text = "Rs. 0.00";
            lblTotalReturn.Location = new Point(280, 192);
            lblTotalReturn.AutoSize = true;
            Theme.StyleLabel(lblTotalReturn, Theme.Danger, Theme.BoldFont);
            cardMain.Controls.Add(lblTotalReturn);

            // 7. Total Cash in Drawer
            Label lblExpectedTitle = new Label();
            lblExpectedTitle.Text = "Total Cash in Drawer:";
            lblExpectedTitle.Location = new Point(20, 226);
            lblExpectedTitle.AutoSize = true;
            Theme.StyleLabel(lblExpectedTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblExpectedTitle);

            lblExpectedCash = new Label();
            lblExpectedCash.Text = "Rs. 0.00";
            lblExpectedCash.Location = new Point(280, 226);
            lblExpectedCash.AutoSize = true;
            lblExpectedCash.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            Theme.StyleLabel(lblExpectedCash, Theme.Success, lblExpectedCash.Font);
            cardMain.Controls.Add(lblExpectedCash);


            // Middle vertical separator
            Panel sepCol = new Panel();
            sepCol.Size = new Size(1, 245);
            sepCol.Location = new Point(440, 20);
            sepCol.BackColor = Theme.AlternateRow;
            cardMain.Controls.Add(sepCol);


            // COLUMN 2: Verification Input
            Label lblCol2Header = new Label();
            lblCol2Header.Text = "SETTLEMENT RECONCILIATION";
            lblCol2Header.Location = new Point(470, 15);
            lblCol2Header.AutoSize = true;
            Theme.StyleLabel(lblCol2Header, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardMain.Controls.Add(lblCol2Header);

            // Enter Cash in Hand
            Label lblActTitle = new Label();
            lblActTitle.Text = "Enter Cash In Hand *:";
            lblActTitle.Location = new Point(470, 48);
            lblActTitle.AutoSize = true;
            Theme.StyleLabel(lblActTitle, Theme.TextLight, Theme.BoldFont);
            cardMain.Controls.Add(lblActTitle);

            txtActualCash = new TextBox();
            txtActualCash.Size = new Size(150, 26);
            txtActualCash.Location = new Point(660, 45);
            Theme.StyleTextBox(txtActualCash);
            txtActualCash.Text = "0.00";
            txtActualCash.TextChanged += (s, e) => UpdateCalculations();
            cardMain.Controls.Add(txtActualCash);

            // Remarks
            Label lblRemarks = new Label();
            lblRemarks.Text = "Remarks / Notes:";
            lblRemarks.Location = new Point(470, 88);
            lblRemarks.AutoSize = true;
            Theme.StyleLabel(lblRemarks, Theme.TextLight, Theme.MainFont);
            cardMain.Controls.Add(lblRemarks);

            txtRemarks = new TextBox();
            txtRemarks.Size = new Size(340, 26);
            txtRemarks.Location = new Point(470, 112);
            Theme.StyleTextBox(txtRemarks);
            txtRemarks.Text = "Daily settlement reconciliation completed.";
            cardMain.Controls.Add(txtRemarks);

            // Status message
            lblStatusMessage = new Label();
            lblStatusMessage.Text = "Reconciliation: Enter counted cash.";
            lblStatusMessage.Location = new Point(470, 150);
            lblStatusMessage.Size = new Size(410, 48);
            Theme.StyleLabel(lblStatusMessage, Theme.TextDark, new Font("Segoe UI Semibold", 9.5F, FontStyle.Italic | FontStyle.Bold));
            cardMain.Controls.Add(lblStatusMessage);

            // Save Settlement Button
            btnSaveSettlement = new Button();
            btnSaveSettlement.Text = "🔒 Save & Close Register";
            btnSaveSettlement.Size = new Size(210, 42);
            btnSaveSettlement.Location = new Point(470, 210);
            Theme.StyleSuccessButton(btnSaveSettlement);
            btnSaveSettlement.Click += BtnSaveSettlement_Click;
            cardMain.Controls.Add(btnSaveSettlement);


            // BOTTOM PANEL: Historical Log
            Label lblHistoryHeader = new Label();
            lblHistoryHeader.Text = "Historical Reconciliation Log Book";
            lblHistoryHeader.Location = new Point(20, 415);
            lblHistoryHeader.AutoSize = true;
            Theme.StyleLabel(lblHistoryHeader, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblHistoryHeader);

            gridHistory = new DataGridView();
            gridHistory.Location = new Point(20, 450);
            gridHistory.Size = new Size(910, 185);
            gridHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Theme.StyleGrid(gridHistory);
            this.Controls.Add(gridHistory);
        }

        private void LoadOpeningCash()
        {
            openingCash = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    // Load Closing/Actual Cash of the latest settlement as opening cash
                    string query = "SELECT TOP 1 ActualCash FROM DailySettlements ORDER BY SettlementDate DESC, Id DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            openingCash = Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving opening cash rollforward: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            lblOpeningCashText.Text = $"Rs. {openingCash:N2}";
        }

        private void LoadTodayMetrics()
        {
            DateTime selectDate = dtpSettlementDate.Value.Date;
            decimal grossSales = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 0. Gross Total Sales Today (Both Services and Products)
                    string grossSalesSql = "SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales WHERE CAST(SaleDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(grossSalesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        grossSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 1. Cash Sales Today (Direct Cash + Split Cash portion)
                    string salesSql = @"
                        SELECT ISNULL(SUM(
                            CASE 
                                WHEN PaymentMethod = 'Cash' THEN AmountPaid 
                                WHEN PaymentMethod = 'Split' THEN ISNULL(CashAmount, 0)
                                ELSE 0 
                            END), 0) 
                        FROM Sales 
                        WHERE CAST(SaleDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(salesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        cashSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 1b. Card Sales Today (Direct Card)
                    string cardSalesSql = @"
                        SELECT ISNULL(SUM(
                            CASE 
                                WHEN PaymentMethod = 'Card' THEN AmountPaid 
                                ELSE 0 
                            END), 0) 
                        FROM Sales 
                        WHERE CAST(SaleDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(cardSalesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        cardSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 1c. QR / UPI / Online Sales Today (Direct QR/UPI/Online + Split Online portion)
                    string qrSalesSql = @"
                        SELECT ISNULL(SUM(
                            CASE 
                                WHEN PaymentMethod NOT IN ('Cash', 'Card', 'Split') THEN AmountPaid 
                                WHEN PaymentMethod = 'Split' THEN ISNULL(OnlineAmount, 0)
                                ELSE 0 
                            END), 0) 
                        FROM Sales 
                        WHERE CAST(SaleDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(qrSalesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        qrSales = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 2. New Dues Created Today
                    string duesCreatedSql = "SELECT ISNULL(SUM(DueAmount), 0) FROM Sales WHERE CAST(SaleDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(duesCreatedSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        duesCreated = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 3. Previous Due Repayments (Cash)
                    string prevDuesSql = @"
                        SELECT ISNULL(SUM(cp.Amount), 0)
                        FROM CustomerPayments cp
                        LEFT JOIN Sales s ON cp.SaleId = s.Id
                        WHERE CAST(cp.PaymentDate as DATE) = @selectDate
                          AND cp.PaymentMethod = 'Cash'
                          AND (CAST(s.SaleDate as DATE) < @selectDate OR cp.SaleId IS NULL)";

                    using (SqlCommand cmd = new SqlCommand(prevDuesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        prevDueRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 4. Today's Due Repayments (Cash)
                    string totalRepaymentsSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @selectDate AND PaymentMethod = 'Cash'";
                    decimal totalRepayments = 0;
                    using (SqlCommand cmd = new SqlCommand(totalRepaymentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        totalRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                    todayDueRepayments = totalRepayments - prevDueRepayments;
                    if (todayDueRepayments < 0) todayDueRepayments = 0;

                    // 4b. Card Due Repayments Today
                    string cardRepaymentsSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @selectDate AND PaymentMethod = 'Card'";
                    using (SqlCommand cmd = new SqlCommand(cardRepaymentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        cardDueRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 4c. QR / UPI Due Repayments Today
                    string qrRepaymentsSql = "SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @selectDate AND PaymentMethod NOT IN ('Cash', 'Card')";
                    using (SqlCommand cmd = new SqlCommand(qrRepaymentsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        qrDueRepayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 5. Total Returns & Cash Refunds Today
                    string refundsSql = "SELECT ISNULL(SUM(TotalRefund), 0), ISNULL(SUM(CashRefund), 0) FROM SalesReturns WHERE CAST(ReturnDate as DATE) = @selectDate";
                    using (SqlCommand cmd = new SqlCommand(refundsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@selectDate", selectDate);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                totalReturns = Convert.ToDecimal(rdr[0]);
                                cashRefunds = Convert.ToDecimal(rdr[1]);
                            }
                        }
                    }
                }

                // Calculations
                totalSaleToday = grossSales - totalReturns;
                dueTodayUnpaid = duesCreated - todayDueRepayments;
                if (dueTodayUnpaid < 0) dueTodayUnpaid = 0;

                totalCardPayment = cardSales + cardDueRepayments;
                totalQRPayment = qrSales + qrDueRepayments;
                totalOnlinePayment = totalCardPayment + totalQRPayment;

                lblTotalSaleToday.Text = $"Rs. {totalSaleToday:N2}";
                lblDueToday.Text = $"Rs. {dueTodayUnpaid:N2}";
                lblPrevDueRepayments.Text = $"Rs. {prevDueRepayments:N2}";
                lblCardPayment.Text = $"Rs. {totalCardPayment:N2}";
                lblQRPayment.Text = $"Rs. {totalQRPayment:N2}";
                lblTotalReturn.Text = $"Rs. {totalReturns:N2}";

                UpdateCalculations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily financials: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCalculations()
        {
            expectedCash = cashSales + prevDueRepayments + todayDueRepayments - cashRefunds;
            lblExpectedCash.Text = $"Rs. {expectedCash:N2}";

            decimal actualCash = 0;
            decimal.TryParse(txtActualCash.Text.Trim(), out actualCash);

            decimal variance = actualCash - expectedCash;

            if (Math.Abs(variance) < 0.01m)
            {
                lblStatusMessage.ForeColor = Theme.Success;
                lblStatusMessage.Text = $"Settlement Closed & Matched! ✓\n(Rs. {actualCash:N2} Matches Rs. {expectedCash:N2})";
            }
            else if (variance < 0)
            {
                lblStatusMessage.ForeColor = Theme.Danger;
                lblStatusMessage.Text = $"Cash Shortage! Unmatched by Rs. {Math.Abs(variance):N2}\n(Expected: Rs. {expectedCash:N2} | Actual: Rs. {actualCash:N2})";
            }
            else
            {
                lblStatusMessage.ForeColor = Theme.Warning;
                lblStatusMessage.Text = $"Cash Surplus! Unmatched by Rs. {variance:N2}\n(Expected: Rs. {expectedCash:N2} | Actual: Rs. {actualCash:N2})";
            }
        }

        private void BtnSaveSettlement_Click(object sender, EventArgs e)
        {
            decimal actualCash = 0;
            if (!decimal.TryParse(txtActualCash.Text.Trim(), out actualCash) || actualCash < 0)
            {
                MessageBox.Show("Please enter a valid cash amount physically counted.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal variance = actualCash - expectedCash;
            string remarks = txtRemarks.Text.Trim();
            DateTime selectDate = dtpSettlementDate.Value;

            // Check if settlement already exists for this date to prevent double logs
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", selectDate.Date);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            DialogResult confirm = MessageBox.Show($"A settlement has already been saved for {selectDate:yyyy-MM-dd}.\nDo you want to overwrite it?", "Confirm Settlement Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (confirm != DialogResult.Yes)
                            {
                                return;
                            }
                            
                            // Delete old record to overwrite
                            using (SqlCommand delCmd = new SqlCommand("DELETE FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date", conn))
                            {
                                delCmd.Parameters.AddWithValue("@date", selectDate.Date);
                                delCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Save
                    string query = @"
                        INSERT INTO DailySettlements (SettlementDate, OpeningCash, CashSales, DueCollections, CardQRSales, CardSales, QRSales, DuesCreated, ExpectedCash, ActualCash, Variance, SettlementBy, Remarks, Refunds)
                        VALUES (@date, @opening, @sales, @collections, @cardQRSales, @cardSales, @qrSales, @dues, @expected, @actual, @variance, @user, @remarks, @refunds)";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", selectDate);
                        cmd.Parameters.AddWithValue("@opening", openingCash);
                        cmd.Parameters.AddWithValue("@sales", cashSales);
                        cmd.Parameters.AddWithValue("@collections", prevDueRepayments + todayDueRepayments);
                        cmd.Parameters.AddWithValue("@cardQRSales", totalOnlinePayment);
                        cmd.Parameters.AddWithValue("@cardSales", totalCardPayment);
                        cmd.Parameters.AddWithValue("@qrSales", totalQRPayment);
                        cmd.Parameters.AddWithValue("@dues", duesCreated);
                        cmd.Parameters.AddWithValue("@expected", expectedCash);
                        cmd.Parameters.AddWithValue("@actual", actualCash);
                        cmd.Parameters.AddWithValue("@variance", variance);
                        cmd.Parameters.AddWithValue("@user", Session.UserId);
                        cmd.Parameters.AddWithValue("@remarks", remarks);
                        cmd.Parameters.AddWithValue("@refunds", cashRefunds);
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Register settlement for {selectDate:yyyy-MM-dd} saved successfully!", "Settlement Logged", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settlement ledger: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.SettlementDate as [Date],
                               s.OpeningCash as [Opening Cash],
                               s.CashSales as [Cash Sales],
                               s.DueCollections as [Dues Collections],
                               ISNULL(s.CardSales, 0.00) as [Card Payment],
                               ISNULL(s.QRSales, ISNULL(s.CardQRSales, 0.00) - ISNULL(s.CardSales, 0.00)) as [QR / UPI Payment],
                               s.Refunds as [Cash Refunds],
                               s.ExpectedCash as [Expected Cash],
                               s.ActualCash as [Actual Cash],
                               s.Variance as [Variance],
                               u.FullName as [Settled By],
                               s.Remarks as [Remarks]
                        FROM DailySettlements s
                        LEFT JOIN Users u ON s.SettlementBy = u.Id
                        ORDER BY s.SettlementDate DESC, s.Id DESC";

                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridHistory.DataSource = dt;

                        // Formatting
                        if (gridHistory.Columns["Opening Cash"] != null) gridHistory.Columns["Opening Cash"].Visible = false;
                        if (gridHistory.Columns["Cash Sales"] != null) gridHistory.Columns["Cash Sales"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Dues Collections"] != null) gridHistory.Columns["Dues Collections"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Card Payment"] != null) gridHistory.Columns["Card Payment"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["QR / UPI Payment"] != null) gridHistory.Columns["QR / UPI Payment"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Cash Refunds"] != null) gridHistory.Columns["Cash Refunds"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Expected Cash"] != null) gridHistory.Columns["Expected Cash"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Actual Cash"] != null) gridHistory.Columns["Actual Cash"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Variance"] != null) gridHistory.Columns["Variance"].DefaultCellStyle.Format = "N2";

                        if (gridHistory.Columns["Date"] != null) gridHistory.Columns["Date"].FillWeight = 100;
                        if (gridHistory.Columns["Opening Cash"] != null) gridHistory.Columns["Opening Cash"].FillWeight = 80;
                        if (gridHistory.Columns["Cash Sales"] != null) gridHistory.Columns["Cash Sales"].FillWeight = 85;
                        if (gridHistory.Columns["Dues Collections"] != null) gridHistory.Columns["Dues Collections"].FillWeight = 85;
                        if (gridHistory.Columns["Card Payment"] != null) gridHistory.Columns["Card Payment"].FillWeight = 90;
                        if (gridHistory.Columns["QR / UPI Payment"] != null) gridHistory.Columns["QR / UPI Payment"].FillWeight = 95;
                        if (gridHistory.Columns["Cash Refunds"] != null) gridHistory.Columns["Cash Refunds"].FillWeight = 85;
                        if (gridHistory.Columns["Expected Cash"] != null) gridHistory.Columns["Expected Cash"].FillWeight = 90;
                        if (gridHistory.Columns["Actual Cash"] != null) gridHistory.Columns["Actual Cash"].FillWeight = 90;
                        if (gridHistory.Columns["Variance"] != null) gridHistory.Columns["Variance"].FillWeight = 80;
                        if (gridHistory.Columns["Settled By"] != null) gridHistory.Columns["Settled By"].FillWeight = 95;
                        if (gridHistory.Columns["Remarks"] != null) gridHistory.Columns["Remarks"].FillWeight = 140;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settlement history register: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
