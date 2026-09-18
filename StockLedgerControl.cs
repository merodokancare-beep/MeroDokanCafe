using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class StockLedgerControl : UserControl
    {
        // Header Tabs
        private Panel tabHeaderPanel;
        private Panel tabContentPanel;
        private Panel panelLedger;
        private Panel panelItemwise;

        private Button btnTabLedger;
        private Button btnTabItemwise;

        // Daily Ledger Tab controls
        private TextBox txtSearch;
        private DataGridView gridLedger;
        private DateTimePicker dtpLedgerDate;
        private Label lblTotalSummary;

        // Itemwise Sales Count Tab controls
        private DateTimePicker itemwiseFromDate;
        private DateTimePicker itemwiseToDate;
        private ComboBox comboItemwiseDateFilter;
        private TextBox txtItemwiseSearch;
        private Button btnItemwiseSearch;
        private DataGridView gridItemwiseSales;
        private Label lblItemwiseSummary;

        public StockLedgerControl()
        {
            InitializeComponent();
            LoadLedger();
            LoadItemwiseSales();

            this.Load += (s, e) => {
                if (txtSearch != null)
                {
                    txtSearch.Focus();
                }
            };
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Stock Ledger & Item Sales Analytics";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Tab Header Panel
            tabHeaderPanel = new Panel();
            tabHeaderPanel.Location = new Point(20, 65);
            tabHeaderPanel.Size = new Size(910, 45);
            tabHeaderPanel.BackColor = Color.Transparent;
            tabHeaderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(tabHeaderPanel);

            btnTabLedger = new Button();
            btnTabLedger.Text = "Daily Stock Ledger Book";
            btnTabLedger.Size = new Size(220, 40);
            btnTabLedger.Location = new Point(0, 0);
            btnTabLedger.Click += (s, e) => ShowTab(panelLedger, btnTabLedger);
            tabHeaderPanel.Controls.Add(btnTabLedger);

            btnTabItemwise = new Button();
            btnTabItemwise.Text = "Itemwise Sales Count";
            btnTabItemwise.Size = new Size(200, 40);
            btnTabItemwise.Location = new Point(225, 0);
            btnTabItemwise.Click += (s, e) => ShowTab(panelItemwise, btnTabItemwise);
            tabHeaderPanel.Controls.Add(btnTabItemwise);

            // Main Tab Content Container
            tabContentPanel = new Panel();
            tabContentPanel.Location = new Point(20, 115);
            tabContentPanel.Size = new Size(910, 510);
            tabContentPanel.BackColor = Theme.Secondary;
            tabContentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(tabContentPanel);

            panelLedger = new Panel();
            panelLedger.Dock = DockStyle.Fill;
            panelLedger.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelLedger);

            panelItemwise = new Panel();
            panelItemwise.Dock = DockStyle.Fill;
            panelItemwise.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelItemwise);

            InitializeLedgerTab(panelLedger);
            InitializeItemwiseSalesTab(panelItemwise);

            ShowTab(panelLedger, btnTabLedger);
        }

        private void ShowTab(Panel selectedPanel, Button activeBtn)
        {
            panelLedger.Visible = false;
            panelItemwise.Visible = false;

            selectedPanel.Visible = true;

            StyleTabButton(btnTabLedger, btnTabLedger == activeBtn);
            StyleTabButton(btnTabItemwise, btnTabItemwise == activeBtn);

            if (selectedPanel == panelLedger && txtSearch != null)
            {
                txtSearch.Focus();
            }
            else if (selectedPanel == panelItemwise && txtItemwiseSearch != null)
            {
                txtItemwiseSearch.Focus();
            }
        }

        private void StyleTabButton(Button btn, bool isActive)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = Theme.BoldFont;
            btn.Cursor = Cursors.Hand;
            btn.Padding = new Padding(8, 4, 8, 4);

            if (isActive)
            {
                btn.BackColor = Theme.Accent;
                btn.ForeColor = Theme.TextLight;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }
            else
            {
                btn.BackColor = Color.FromArgb(17, 24, 39);
                btn.ForeColor = Theme.TextDark;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.AlternateRow;
                btn.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            }
        }

        private void InitializeLedgerTab(Panel page)
        {
            // Control Action Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(0, 0);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Date picker label
            Label lblDate = new Label();
            lblDate.Text = "Select Ledger Date:";
            lblDate.Location = new Point(0, 12);
            lblDate.AutoSize = true;
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            actionPanel.Controls.Add(lblDate);

            // Date Picker
            dtpLedgerDate = new DateTimePicker();
            dtpLedgerDate.Format = DateTimePickerFormat.Short;
            dtpLedgerDate.Size = new Size(160, 28);
            dtpLedgerDate.Location = new Point(lblDate.Left + lblDate.PreferredWidth + 10, 7);
            dtpLedgerDate.Font = Theme.MainFont;
            dtpLedgerDate.ValueChanged += (s, e) => LoadLedger();
            actionPanel.Controls.Add(dtpLedgerDate);

            // Search Panel container (aligned right)
            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(250, 36);
            searchPanel.Location = new Point(660, 7);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.TextChanged += (s, e) => LoadLedger();
            searchPanel.Controls.Add(txtSearch);
            actionPanel.Controls.Add(searchPanel);

            // Search Label next to search box
            Label lblSearch = new Label();
            lblSearch.Text = "🔍 Filter Product:";
            lblSearch.AutoSize = true;
            lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblSearch, Theme.TextDark, Theme.BoldFont);
            
            actionPanel.Resize += (s, e) => {
                lblSearch.Location = new Point(searchPanel.Left - lblSearch.PreferredWidth - 10, 12);
            };
            lblSearch.Location = new Point(searchPanel.Left - lblSearch.PreferredWidth - 10, 12);
            actionPanel.Controls.Add(lblSearch);

            page.Controls.Add(actionPanel);

            // GridView
            gridLedger = new DataGridView();
            gridLedger.Size = new Size(910, 410);
            gridLedger.Location = new Point(0, 60);
            gridLedger.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridLedger);
            page.Controls.Add(gridLedger);

            // Summary Label at bottom
            lblTotalSummary = new Label();
            lblTotalSummary.Text = "";
            lblTotalSummary.Location = new Point(0, 480);
            lblTotalSummary.Size = new Size(910, 25);
            lblTotalSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Theme.StyleLabel(lblTotalSummary, Theme.TextDark, Theme.BoldFont);
            page.Controls.Add(lblTotalSummary);
        }

        private void InitializeItemwiseSalesTab(Panel page)
        {
            // Range Preset Filter
            Label lblPreset = new Label();
            lblPreset.Text = "Range Preset:";
            lblPreset.Location = new Point(0, 10);
            lblPreset.AutoSize = true;
            Theme.StyleLabel(lblPreset, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblPreset);

            comboItemwiseDateFilter = new ComboBox();
            comboItemwiseDateFilter.Size = new Size(120, 28);
            comboItemwiseDateFilter.Location = new Point(0, 32);
            comboItemwiseDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboItemwiseDateFilter);
            comboItemwiseDateFilter.BackColor = Theme.Primary;
            comboItemwiseDateFilter.Items.AddRange(new string[] { "Today", "Custom Range", "This Month", "All Time" });
            page.Controls.Add(comboItemwiseDateFilter);

            // Filters
            Label lblFrom = new Label();
            lblFrom.Text = "From Date:";
            lblFrom.Location = new Point(140, 10);
            lblFrom.AutoSize = true;
            Theme.StyleLabel(lblFrom, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblFrom);

            itemwiseFromDate = new DateTimePicker();
            itemwiseFromDate.Size = new Size(110, 28);
            itemwiseFromDate.Location = new Point(140, 32);
            itemwiseFromDate.Font = Theme.MainFont;
            itemwiseFromDate.Format = DateTimePickerFormat.Short;
            itemwiseFromDate.Value = DateTime.Today;
            page.Controls.Add(itemwiseFromDate);

            Label lblTo = new Label();
            lblTo.Text = "To Date:";
            lblTo.Location = new Point(265, 10);
            lblTo.AutoSize = true;
            Theme.StyleLabel(lblTo, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblTo);

            itemwiseToDate = new DateTimePicker();
            itemwiseToDate.Size = new Size(110, 28);
            itemwiseToDate.Location = new Point(265, 32);
            itemwiseToDate.Font = Theme.MainFont;
            itemwiseToDate.Format = DateTimePickerFormat.Short;
            itemwiseToDate.Value = DateTime.Today;
            page.Controls.Add(itemwiseToDate);

            btnItemwiseSearch = new Button();
            btnItemwiseSearch.Text = "🔍 Generate Log";
            btnItemwiseSearch.Size = new Size(130, 36);
            btnItemwiseSearch.Location = new Point(390, 28);
            Theme.StylePrimaryButton(btnItemwiseSearch);
            btnItemwiseSearch.Click += (s, e) => LoadItemwiseSales();
            page.Controls.Add(btnItemwiseSearch);

            // Search Box for Item Code / Name
            Label lblSearch = new Label();
            lblSearch.Text = "Search Product / Code:";
            lblSearch.Location = new Point(540, 10);
            lblSearch.AutoSize = true;
            Theme.StyleLabel(lblSearch, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblSearch);

            txtItemwiseSearch = new TextBox();
            txtItemwiseSearch.Size = new Size(200, 28);
            txtItemwiseSearch.Location = new Point(540, 32);
            txtItemwiseSearch.Font = Theme.MainFont;
            Theme.StyleTextBox(txtItemwiseSearch);
            txtItemwiseSearch.TextChanged += (s, e) => LoadItemwiseSales();
            page.Controls.Add(txtItemwiseSearch);

            // Preset handler
            comboItemwiseDateFilter.SelectedIndex = 0;
            itemwiseFromDate.Enabled = false;
            itemwiseToDate.Enabled = false;

            comboItemwiseDateFilter.SelectedIndexChanged += (s, e) =>
            {
                if (comboItemwiseDateFilter.SelectedIndex == 0) // "Today"
                {
                    itemwiseFromDate.Value = DateTime.Today;
                    itemwiseToDate.Value = DateTime.Today;
                    itemwiseFromDate.Enabled = false;
                    itemwiseToDate.Enabled = false;
                    LoadItemwiseSales();
                }
                else if (comboItemwiseDateFilter.SelectedIndex == 1) // "Custom Range"
                {
                    itemwiseFromDate.Enabled = true;
                    itemwiseToDate.Enabled = true;
                }
                else if (comboItemwiseDateFilter.SelectedIndex == 2) // "This Month"
                {
                    DateTime now = DateTime.Today;
                    itemwiseFromDate.Value = new DateTime(now.Year, now.Month, 1);
                    itemwiseToDate.Value = now;
                    itemwiseFromDate.Enabled = false;
                    itemwiseToDate.Enabled = false;
                    LoadItemwiseSales();
                }
                else if (comboItemwiseDateFilter.SelectedIndex == 3) // "All Time"
                {
                    itemwiseFromDate.Value = new DateTime(2000, 1, 1);
                    itemwiseToDate.Value = DateTime.Today.AddDays(365);
                    itemwiseFromDate.Enabled = false;
                    itemwiseToDate.Enabled = false;
                    LoadItemwiseSales();
                }
            };

            // GridView
            gridItemwiseSales = new DataGridView();
            gridItemwiseSales.Size = new Size(910, 380);
            gridItemwiseSales.Location = new Point(0, 75);
            gridItemwiseSales.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridItemwiseSales);
            page.Controls.Add(gridItemwiseSales);

            // Summary Label
            lblItemwiseSummary = new Label();
            lblItemwiseSummary.Text = "Unique Items Sold: 0  •  Total Units Sold: 0  •  Total Revenue: Rs. 0.00";
            lblItemwiseSummary.AutoSize = false;
            lblItemwiseSummary.Size = new Size(910, 30);
            lblItemwiseSummary.Location = new Point(0, 460);
            lblItemwiseSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblItemwiseSummary, Theme.TextLight, Theme.BoldFont);
            lblItemwiseSummary.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            page.Controls.Add(lblItemwiseSummary);
        }

        private void LoadLedger()
        {
            try
            {
                if (dtpLedgerDate == null) return;
                DateTime selectedDate = dtpLedgerDate.Value.Date;
                DateTime startDate = selectedDate;
                DateTime endDate = selectedDate.AddDays(1);

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            p.Name AS [Brand Name],
                            
                            -- Fresh Arrival (Purchases on day D)
                            ISNULL(purchasesOnDay.Qty, 0) AS [Fresh Arrival],
                            
                            -- Sale (Net Sales on day D: Sales - Returns)
                            ISNULL(salesOnDay.Qty, 0) - ISNULL(returnsOnDay.Qty, 0) AS [Sale],
                            
                            -- Closing Balance (Stock at end of day D)
                            p.Stock 
                              + ISNULL(salesAfterDay.Qty, 0) 
                              - ISNULL(purchasesAfterDay.Qty, 0) 
                              - ISNULL(returnsAfterDay.Qty, 0) AS [Closing Balance]
                        FROM Products p
                        OUTER APPLY (
                            SELECT SUM(pd.Quantity) AS Qty
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases pur ON pd.PurchaseId = pur.Id
                            WHERE pd.ProductId = p.Id AND pur.PurchaseDate >= @startDate AND pur.PurchaseDate < @endDate
                        ) purchasesOnDay
                        OUTER APPLY (
                            SELECT SUM(sd.Quantity) AS Qty
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId = s.Id
                            WHERE sd.ProductId = p.Id AND s.SaleDate >= @startDate AND s.SaleDate < @endDate
                        ) salesOnDay
                        OUTER APPLY (
                            SELECT SUM(srd.Quantity) AS Qty
                            FROM SalesReturnDetails srd
                            INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                            WHERE srd.ProductId = p.Id AND sr.ReturnDate >= @startDate AND sr.ReturnDate < @endDate
                        ) returnsOnDay
                        OUTER APPLY (
                            SELECT SUM(pd.Quantity) AS Qty
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases pur ON pd.PurchaseId = pur.Id
                            WHERE pd.ProductId = p.Id AND pur.PurchaseDate >= @endDate
                        ) purchasesAfterDay
                        OUTER APPLY (
                            SELECT SUM(sd.Quantity) AS Qty
                            FROM SaleDetails sd
                            INNER JOIN Sales s ON sd.SaleId = s.Id
                            WHERE sd.ProductId = p.Id AND s.SaleDate >= @endDate
                        ) salesAfterDay
                        OUTER APPLY (
                            SELECT SUM(srd.Quantity) AS Qty
                            FROM SalesReturnDetails srd
                            INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                            WHERE srd.ProductId = p.Id AND sr.ReturnDate >= @endDate
                        ) returnsAfterDay
                        WHERE (p.Name LIKE @search OR p.Category LIKE @search)
                        ORDER BY p.Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        cmd.Parameters.AddWithValue("@search", $"%{txtSearch.Text.Trim()}%");

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable rawDt = new DataTable();
                            da.Fill(rawDt);

                            // Build final display table with calculated Opening Balance and Total
                            DataTable displayDt = new DataTable();
                            displayDt.Columns.Add("Brand Name", typeof(string));
                            displayDt.Columns.Add("Opening Balance", typeof(int));
                            displayDt.Columns.Add("Fresh Arrival", typeof(int));
                            displayDt.Columns.Add("Total", typeof(int));
                            displayDt.Columns.Add("Sale", typeof(int));
                            displayDt.Columns.Add("Closing Balance", typeof(int));

                            int totalOpening = 0;
                            int totalFresh = 0;
                            int totalSale = 0;
                            int totalClosing = 0;

                            foreach (DataRow r in rawDt.Rows)
                            {
                                string brandName = r["Brand Name"].ToString();
                                int freshArrival = Convert.ToInt32(r["Fresh Arrival"]);
                                int sale = Convert.ToInt32(r["Sale"]);
                                int closingBalance = Convert.ToInt32(r["Closing Balance"]);

                                int openingBalance = closingBalance + sale - freshArrival;
                                int total = openingBalance + freshArrival;

                                displayDt.Rows.Add(brandName, openingBalance, freshArrival, total, sale, closingBalance);

                                totalOpening += openingBalance;
                                totalFresh += freshArrival;
                                totalSale += sale;
                                totalClosing += closingBalance;
                            }

                            if (gridLedger != null)
                            {
                                gridLedger.DataSource = displayDt;

                                if (gridLedger.Columns["Brand Name"] != null) gridLedger.Columns["Brand Name"].FillWeight = 200;
                                
                                string[] numCols = { "Opening Balance", "Fresh Arrival", "Total", "Sale", "Closing Balance" };
                                foreach (var col in numCols)
                                {
                                    if (gridLedger.Columns[col] != null)
                                    {
                                        gridLedger.Columns[col].DefaultCellStyle.Format = "N0";
                                        gridLedger.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                        gridLedger.Columns[col].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    }
                                }
                            }

                            if (lblTotalSummary != null)
                            {
                                lblTotalSummary.Text = $"Summary for {selectedDate:dd MMM yyyy}:   Opening Stock: {totalOpening:N0}   |   Fresh Arrival: {totalFresh:N0}   |   Total Stock: {(totalOpening + totalFresh):N0}   |   Sales: {totalSale:N0}   |   Closing Stock: {totalClosing:N0}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily ledger book: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadItemwiseSales()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    DateTime fDate = itemwiseFromDate != null ? itemwiseFromDate.Value.Date : DateTime.Today;
                    DateTime tDate = itemwiseToDate != null ? itemwiseToDate.Value.Date : DateTime.Today;
                    string search = txtItemwiseSearch != null ? txtItemwiseSearch.Text.Trim() : "";

                    string query = @"
                        SELECT 
                            p.Code AS [Item Code],
                            p.Name AS [Product Name],
                            SUM(sd.Quantity) AS [Qty Sold],
                            SUM(sd.Total) AS [Total Amount]
                        FROM SaleDetails sd
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        WHERE CAST(s.SaleDate AS DATE) >= @fromDate 
                          AND CAST(s.SaleDate AS DATE) <= @toDate";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND (p.Code LIKE @search OR p.Name LIKE @search)";
                    }

                    query += @"
                        GROUP BY p.Code, p.Name
                        ORDER BY SUM(sd.Quantity) DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromDate", fDate);
                        cmd.Parameters.AddWithValue("@toDate", tDate);
                        if (!string.IsNullOrEmpty(search))
                        {
                            cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (gridItemwiseSales != null)
                            {
                                gridItemwiseSales.DataSource = dt;

                                if (gridItemwiseSales.Columns["Qty Sold"] != null)
                                {
                                    gridItemwiseSales.Columns["Qty Sold"].DefaultCellStyle.Format = "N0";
                                    gridItemwiseSales.Columns["Qty Sold"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    gridItemwiseSales.Columns["Qty Sold"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                    gridItemwiseSales.Columns["Qty Sold"].FillWeight = 80;
                                }
                                if (gridItemwiseSales.Columns["Total Amount"] != null)
                                {
                                    gridItemwiseSales.Columns["Total Amount"].DefaultCellStyle.Format = "N2";
                                    gridItemwiseSales.Columns["Total Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    gridItemwiseSales.Columns["Total Amount"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                                    gridItemwiseSales.Columns["Total Amount"].FillWeight = 100;
                                }
                                if (gridItemwiseSales.Columns["Item Code"] != null)
                                {
                                    gridItemwiseSales.Columns["Item Code"].FillWeight = 110;
                                }
                                if (gridItemwiseSales.Columns["Product Name"] != null)
                                {
                                    gridItemwiseSales.Columns["Product Name"].FillWeight = 220;
                                }
                            }

                            // Compute Summary
                            int uniqueItems = dt.Rows.Count;
                            long totalUnits = 0;
                            decimal totalRev = 0;

                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["Qty Sold"] != DBNull.Value) totalUnits += Convert.ToInt64(row["Qty Sold"]);
                                if (row["Total Amount"] != DBNull.Value) totalRev += Convert.ToDecimal(row["Total Amount"]);
                            }

                            if (lblItemwiseSummary != null)
                            {
                                lblItemwiseSummary.Text = $"Unique Items Sold: {uniqueItems}  •  Total Units Sold: {totalUnits:N0}  •  Total Revenue: Rs. {totalRev:N2}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading itemwise sales report: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
