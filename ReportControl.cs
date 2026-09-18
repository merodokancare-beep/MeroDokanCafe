using System;
using System.Data;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class ReportControl : UserControl
    {
        private FlowLayoutPanel tabHeaderPanel;
        private Panel tabContentPanel;
        private Panel panelDailySales;
        private Panel panelProfitLoss;
        private Panel panelPurchaseInward;

        private Button btnTabSales;
        private Button btnTabPL;
        
        // Sales Report Tab controls
        private DateTimePicker salesFromDate;
        private DateTimePicker salesToDate;
        private DataGridView gridSalesReport;
        private Button btnSalesSearch;
        private Label lblSalesSummary;
        private Button btnReprintBill;
        private TextBox txtSalesSearch;
        private ComboBox comboSalesDateFilter;
        private ComboBox comboSalesTypeFilter;
        private Label lblSalesTotalVal;
        private Label lblSalesCountVal;
        private Label lblSalesCashVal;
        private Label lblSalesOnlineVal;
        private Label lblSalesDueVal;

        // Print components for reprint duplicate copy
        private PrintDocument reprintDoc;
        private PrintPreviewDialog reprintPreviewDlg;
        private int printSaleId = 0;

        // Profit/Loss Tab controls
        private DateTimePicker plFromDate;
        private DateTimePicker plToDate;
        private Button btnCalculatePL;
        private Panel cardRevenue;
        private Panel cardCOGS;
        private Panel cardNetProfit;
        private Label lblRevenueVal;
        private Label lblCOGSVal;
        private Label lblNetVal;
        private Label lblPLSummary;
        private ComboBox comboPLDateFilter;

        // Purchase History Tab controls
        private DateTimePicker purchaseFromDate;
        private DateTimePicker purchaseToDate;
        private ComboBox comboFilterSupplier;
        private ComboBox comboPurchaseReportType;
        private DataGridView gridPurchaseReport;
        private Button btnPurchaseSearch;
        private Label lblPurchaseSummary;
        private ComboBox comboPurchaseDateFilter;

        // Collection Summary Tab controls
        private Panel panelCollectionSummary;
        private Button btnTabCollectionSummary;
        private DateTimePicker collFromDate;
        private DateTimePicker collToDate;
        private ComboBox comboCollDateFilter;
        private DataGridView gridCollectionSummary;
        private Button btnCollSearch;
        private Label lblCollInvoiceCountVal;
        private Label lblCollCashVal;
        private Label lblCollOnlineVal;
        private Label lblCollTotalVal;
        private Label lblCollSummary;

        // Staff Commission Tab controls
        private Panel panelStaffCommissions;
        private Button btnTabStaffCommissions;
        private DateTimePicker commFromDate;
        private DateTimePicker commToDate;
        private ComboBox comboCommStaff;
        private ComboBox comboCommDateFilter;
        private ComboBox comboCommItemType;
        private DataGridView gridStaffCommissions;
        private Button btnCommSearch;
        private Panel cardCommRevenue;
        private Panel cardCommPayable;
        private Panel cardCommCount;
        private Label lblCommRevenueVal;
        private Label lblCommPayableVal;
        private Label lblCommCountVal;
        private Label lblCommCountHeader;
        private Label lblCommRevenueHeader;

        // Stylist Job Summary Tab controls
        private Panel panelStylistJobs;
        private Button btnTabStylistJobs;
        private DateTimePicker jobsFromDate;
        private DateTimePicker jobsToDate;
        private ComboBox comboJobsStaff;
        private ComboBox comboJobsDateFilter;
        private DataGridView gridStylistJobs;
        private Button btnJobsSearch;
        private Panel cardJobsTotalCount;
        private Panel cardJobsTotalAmount;
        private Panel cardJobsActiveStylists;
        private Label lblJobsTotalCountVal;
        private Label lblJobsTotalAmountVal;
        private Label lblJobsActiveStylistsVal;

        // Stock Register / Inventory Tab controls
        private Panel panelStockRegister;
        private Button btnTabStockRegister;
        private TextBox txtStockSearch;
        private ComboBox comboStockCategory;
        private ComboBox comboStockStatus;
        private DataGridView gridStockRegister;
        private Button btnStockSearch;
        private Panel cardStockTotalUnits;
        private Panel cardStockCostValue;
        private Panel cardStockRetailValue;
        private Panel cardStockLowAlerts;
        private Label lblStockTotalUnitsVal;
        private Label lblStockCostValueVal;
        private Label lblStockRetailValueVal;
        private Label lblStockLowAlertsVal;

        // Raw Material & Daily Kitchen Usage Report controls
        private Panel panelRawMaterialUsage;
        private DateTimePicker dtpRawFromDate;
        private DateTimePicker dtpRawToDate;
        private ComboBox comboRawCategory;
        private ComboBox comboRawDateFilter;
        private TextBox txtRawSearch;
        private Button btnRawSearch;
        private DataGridView gridRawMaterialReport;
        private Panel cardRawUsageCost;
        private Panel cardRawInwardCost;
        private Panel cardRawStockAsset;
        private Panel cardRawLowStock;
        private Label lblRawUsageCostVal;
        private Label lblRawInwardCostVal;
        private Label lblRawStockAssetVal;
        private Label lblRawLowStockVal;

        public ReportControl()
        {
            InitializeComponent();
            LoadDailySales();
            LoadCollectionSummary();
            CalculateProfitLoss();
            LoadSuppliersFilterDropdown();
            LoadPurchaseHistory();
            LoadStockCategories();
            LoadStockRegisterReport();
            LoadRawMaterialUsageCategories();
            LoadRawMaterialUsageReport();
            LoadStaffFilterDropdown();
            LoadStaffCommissions();
            LoadStylistJobsReport();

            this.Load += (s, e) => {
                if (txtSalesSearch != null)
                {
                    txtSalesSearch.Focus();
                }
            };
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Reports & Business Intelligence Center";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Top Header Panel for Buttons (Responsive wrapping FlowLayoutPanel without ugly scrollbars)
            tabHeaderPanel = new FlowLayoutPanel();
            tabHeaderPanel.Location = new Point(20, 56);
            tabHeaderPanel.Width = 910;
            tabHeaderPanel.AutoSize = true;
            tabHeaderPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tabHeaderPanel.BackColor = Color.Transparent;
            tabHeaderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tabHeaderPanel.FlowDirection = FlowDirection.LeftToRight;
            tabHeaderPanel.WrapContents = true;
            tabHeaderPanel.AutoScroll = false;
            tabHeaderPanel.Padding = new Padding(0, 0, 0, 4);
            this.Controls.Add(tabHeaderPanel);

            // Tab Buttons with compact padding & auto-sizing
            btnTabSales = new Button();
            btnTabSales.Text = "📊 Daily Sales";
            btnTabSales.Height = 34;
            btnTabSales.AutoSize = true;
            btnTabSales.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTabSales.Padding = new Padding(12, 0, 12, 0);
            btnTabSales.Margin = new Padding(0, 0, 6, 6);
            btnTabSales.UseMnemonic = false;
            btnTabSales.Click += (s, e) => ShowTab(panelDailySales, btnTabSales);
            tabHeaderPanel.Controls.Add(btnTabSales);

            btnTabCollectionSummary = new Button();
            btnTabCollectionSummary.Text = "💰 Collections";
            btnTabCollectionSummary.Height = 34;
            btnTabCollectionSummary.AutoSize = true;
            btnTabCollectionSummary.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTabCollectionSummary.Padding = new Padding(12, 0, 12, 0);
            btnTabCollectionSummary.Margin = new Padding(0, 0, 6, 6);
            btnTabCollectionSummary.UseMnemonic = false;
            btnTabCollectionSummary.Click += (s, e) => ShowTab(panelCollectionSummary, btnTabCollectionSummary);
            tabHeaderPanel.Controls.Add(btnTabCollectionSummary);

            btnTabStylistJobs = new Button();
            btnTabStylistJobs.Text = "🍽️ Table / KOT";
            btnTabStylistJobs.Height = 34;
            btnTabStylistJobs.AutoSize = true;
            btnTabStylistJobs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTabStylistJobs.Padding = new Padding(12, 0, 12, 0);
            btnTabStylistJobs.Margin = new Padding(0, 0, 6, 6);
            btnTabStylistJobs.UseMnemonic = false;
            btnTabStylistJobs.Click += (s, e) => ShowTab(panelStylistJobs, btnTabStylistJobs);
            tabHeaderPanel.Controls.Add(btnTabStylistJobs);

            btnTabStaffCommissions = new Button();
            btnTabStaffCommissions.Text = "🧑‍🍳 Staff Sales";
            btnTabStaffCommissions.Height = 34;
            btnTabStaffCommissions.AutoSize = true;
            btnTabStaffCommissions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTabStaffCommissions.Padding = new Padding(12, 0, 12, 0);
            btnTabStaffCommissions.Margin = new Padding(0, 0, 6, 6);
            btnTabStaffCommissions.UseMnemonic = false;
            btnTabStaffCommissions.Click += (s, e) => ShowTab(panelStaffCommissions, btnTabStaffCommissions);
            tabHeaderPanel.Controls.Add(btnTabStaffCommissions);

            bool isAdmin = Session.Role == "Admin";

            if (isAdmin)
            {
                btnTabPL = new Button();
                btnTabPL.Text = "📈 Profit & Loss";
                btnTabPL.Height = 34;
                btnTabPL.AutoSize = true;
                btnTabPL.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                btnTabPL.Padding = new Padding(12, 0, 12, 0);
                btnTabPL.Margin = new Padding(0, 0, 6, 6);
                btnTabPL.UseMnemonic = false;
                btnTabPL.Click += (s, e) => ShowTab(panelProfitLoss, btnTabPL);
                tabHeaderPanel.Controls.Add(btnTabPL);
            }

            btnTabStockRegister = new Button();
            btnTabStockRegister.Text = "📦 Dishes Menu";
            btnTabStockRegister.Height = 34;
            btnTabStockRegister.AutoSize = true;
            btnTabStockRegister.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTabStockRegister.Padding = new Padding(12, 0, 12, 0);
            btnTabStockRegister.Margin = new Padding(0, 0, 6, 6);
            btnTabStockRegister.UseMnemonic = false;
            btnTabStockRegister.Click += (s, e) => ShowTab(panelStockRegister, btnTabStockRegister);
            tabHeaderPanel.Controls.Add(btnTabStockRegister);

            // Main Tab Content Panel
            tabContentPanel = new Panel();
            tabContentPanel.Location = new Point(20, 105);
            tabContentPanel.Size = new Size(910, 525);
            tabContentPanel.BackColor = Theme.Secondary;
            tabContentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(tabContentPanel);

            void UpdateTabContentBounds()
            {
                int top = tabHeaderPanel.Bottom + 6;
                tabContentPanel.Location = new Point(20, top);
                tabContentPanel.Size = new Size(Math.Max(300, this.ClientSize.Width - 40), Math.Max(200, this.ClientSize.Height - top - 15));
            }

            tabHeaderPanel.SizeChanged += (s, e) => UpdateTabContentBounds();
            this.Resize += (s, e) => {
                tabHeaderPanel.Width = Math.Max(300, this.ClientSize.Width - 40);
                UpdateTabContentBounds();
            };

            // Sub Panels for actual content
            panelDailySales = new Panel();
            panelDailySales.Dock = DockStyle.Fill;
            panelDailySales.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelDailySales);

            panelCollectionSummary = new Panel();
            panelCollectionSummary.Dock = DockStyle.Fill;
            panelCollectionSummary.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelCollectionSummary);

            panelStylistJobs = new Panel();
            panelStylistJobs.Dock = DockStyle.Fill;
            panelStylistJobs.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelStylistJobs);

            panelStaffCommissions = new Panel();
            panelStaffCommissions.Dock = DockStyle.Fill;
            panelStaffCommissions.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelStaffCommissions);

            panelProfitLoss = new Panel();
            panelProfitLoss.Dock = DockStyle.Fill;
            panelProfitLoss.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelProfitLoss);

            panelPurchaseInward = new Panel();
            panelPurchaseInward.Dock = DockStyle.Fill;
            panelPurchaseInward.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelPurchaseInward);

            panelStockRegister = new Panel();
            panelStockRegister.Dock = DockStyle.Fill;
            panelStockRegister.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelStockRegister);

            panelRawMaterialUsage = new Panel();
            panelRawMaterialUsage.Dock = DockStyle.Fill;
            panelRawMaterialUsage.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelRawMaterialUsage);

            // Initialize content inside the panels
            InitializeSalesTab(panelDailySales);
            InitializeCollectionSummaryTab(panelCollectionSummary);
            InitializeStylistJobsTab(panelStylistJobs);
            InitializeStaffCommissionTab(panelStaffCommissions);
            InitializePLTab(panelProfitLoss);
            InitializePurchaseTab(panelPurchaseInward);
            InitializeStockRegisterTab(panelStockRegister);
            InitializeRawMaterialUsageTab(panelRawMaterialUsage);

            // Default view: Show Daily Sales tab
            ShowTab(panelDailySales, btnTabSales);

            // Setup Reprint Elements
            reprintDoc = new PrintDocument();
            reprintDoc.PrintPage += ReprintDoc_PrintPage;
            reprintPreviewDlg = new PrintPreviewDialog();
            reprintPreviewDlg.Document = reprintDoc;
            reprintPreviewDlg.Size = new Size(600, 700);
        }

        private void ShowTab(Panel selectedPanel, Button activeBtn)
        {
            if (panelDailySales != null) panelDailySales.Visible = false;
            if (panelCollectionSummary != null) panelCollectionSummary.Visible = false;
            if (panelStylistJobs != null) panelStylistJobs.Visible = false;
            if (panelStaffCommissions != null) panelStaffCommissions.Visible = false;
            if (panelProfitLoss != null) panelProfitLoss.Visible = false;
            if (panelPurchaseInward != null) panelPurchaseInward.Visible = false;
            if (panelStockRegister != null) panelStockRegister.Visible = false;
            if (panelRawMaterialUsage != null) panelRawMaterialUsage.Visible = false;

            if (selectedPanel != null) selectedPanel.Visible = true;

            if (btnTabSales != null) StyleTabButton(btnTabSales, btnTabSales == activeBtn);
            if (btnTabCollectionSummary != null) StyleTabButton(btnTabCollectionSummary, btnTabCollectionSummary == activeBtn);
            if (btnTabStylistJobs != null) StyleTabButton(btnTabStylistJobs, btnTabStylistJobs == activeBtn);
            if (btnTabStaffCommissions != null) StyleTabButton(btnTabStaffCommissions, btnTabStaffCommissions == activeBtn);
            if (btnTabPL != null) StyleTabButton(btnTabPL, btnTabPL == activeBtn);
            if (btnTabStockRegister != null) StyleTabButton(btnTabStockRegister, btnTabStockRegister == activeBtn);

            if (selectedPanel == panelDailySales && txtSalesSearch != null)
            {
                txtSalesSearch.Focus();
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
                btn.BackColor = Theme.Accent; // Indigo Accent
                btn.ForeColor = Theme.TextWhite;
                btn.FlatAppearance.BorderSize = 0; // Seamless borderless look
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }
            else
            {
                btn.BackColor = Color.FromArgb(17, 24, 39); // Match card bg depth
                btn.ForeColor = Theme.TextMuted; // Slate 400 (Muted)
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.CardBorder;
                btn.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            }
        }

        private void ApplyDateRangePreset(string preset, DateTimePicker dtpFrom, DateTimePicker dtpTo, Action onApply)
        {
            DateTime now = DateTime.Today;
            if (preset == "Today")
            {
                dtpFrom.Value = now;
                dtpTo.Value = now;
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "Yesterday")
            {
                dtpFrom.Value = now.AddDays(-1);
                dtpTo.Value = now.AddDays(-1);
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "This Week")
            {
                int diff = (7 + (now.DayOfWeek - DayOfWeek.Sunday)) % 7;
                dtpFrom.Value = now.AddDays(-1 * diff);
                dtpTo.Value = now;
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "This Month")
            {
                dtpFrom.Value = new DateTime(now.Year, now.Month, 1);
                dtpTo.Value = now;
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "Last Month")
            {
                DateTime prev = now.AddMonths(-1);
                dtpFrom.Value = new DateTime(prev.Year, prev.Month, 1);
                dtpTo.Value = new DateTime(now.Year, now.Month, 1).AddDays(-1);
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "This Year")
            {
                dtpFrom.Value = new DateTime(now.Year, 1, 1);
                dtpTo.Value = now;
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else if (preset == "All Time")
            {
                dtpFrom.Value = new DateTime(2020, 1, 1);
                dtpTo.Value = now;
                dtpFrom.Enabled = false;
                dtpTo.Enabled = false;
            }
            else // "Custom Range"
            {
                dtpFrom.Enabled = true;
                dtpTo.Enabled = true;
            }

            onApply?.Invoke();
        }

        private void InitializeSalesTab(Panel page)
        {
            // Filters Bar Panell
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboSalesDateFilter = new ComboBox();
            comboSalesDateFilter.Size = new Size(110, 28);
            comboSalesDateFilter.Margin = new Padding(2, 2, 3, 2);
            comboSalesDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSalesDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboSalesDateFilter);
            filterBar.Controls.Add(comboSalesDateFilter);

            salesFromDate = new DateTimePicker();
            salesFromDate.Format = DateTimePickerFormat.Short;
            salesFromDate.Size = new Size(95, 28);
            salesFromDate.Margin = new Padding(2, 2, 3, 2);
            salesFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(salesFromDate);

            salesToDate = new DateTimePicker();
            salesToDate.Format = DateTimePickerFormat.Short;
            salesToDate.Size = new Size(95, 28);
            salesToDate.Margin = new Padding(2, 2, 3, 2);
            salesToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(salesToDate);

            comboSalesTypeFilter = new ComboBox();
            comboSalesTypeFilter.Size = new Size(110, 28);
            comboSalesTypeFilter.Margin = new Padding(2, 2, 3, 2);
            comboSalesTypeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSalesTypeFilter.Items.AddRange(new string[] { "All Sales", "Service Sale", "Product Sale" });
            comboSalesTypeFilter.SelectedIndex = 0;
            Theme.StyleComboBox(comboSalesTypeFilter);
            comboSalesTypeFilter.SelectedIndexChanged += (s, e) => LoadDailySales();
            filterBar.Controls.Add(comboSalesTypeFilter);

            btnSalesSearch = new Button();
            btnSalesSearch.Text = "🔍 Search";
            btnSalesSearch.Size = new Size(80, 28);
            btnSalesSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnSalesSearch);
            btnSalesSearch.Click += (s, e) => LoadDailySales();
            filterBar.Controls.Add(btnSalesSearch);

            btnReprintBill = new Button();
            btnReprintBill.Text = "🖨️ Print Reprint";
            btnReprintBill.Size = new Size(88, 28);
            btnReprintBill.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSecondaryButton(btnReprintBill);
            btnReprintBill.Click += BtnReprintBill_Click;
            filterBar.Controls.Add(btnReprintBill);

            Button btnExportSales = new Button();
            btnExportSales.Text = "📊 Export Excel";
            btnExportSales.Size = new Size(118, 28);
            btnExportSales.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportSales);
            btnExportSales.Click += (s, e) => ExportGridToExcel(gridSalesReport, "Daily_Sales_Register", "Daily Sales Register Report");
            filterBar.Controls.Add(btnExportSales);

            Panel pnlSearch = new Panel();
            pnlSearch.Size = new Size(135, 28);
            pnlSearch.Margin = new Padding(2, 2, 2, 2);
            pnlSearch.BackColor = Theme.Primary;
            pnlSearch.BorderStyle = BorderStyle.FixedSingle;
            pnlSearch.Padding = new Padding(4, 4, 4, 2);

            txtSalesSearch = new TextBox();
            txtSalesSearch.BorderStyle = BorderStyle.None;
            txtSalesSearch.BackColor = Theme.Primary;
            txtSalesSearch.ForeColor = Theme.TextWhite;
            txtSalesSearch.Font = new Font("Segoe UI", 9.5F);
            /* ================= WINDOWS 7 COMPATIBILITY CHANGE (.NET 8 API replaced) =================
            txtSalesSearch.PlaceholderText = "Search...";
            ================================================================================ */
            Win7Compat.SetPlaceholder(txtSalesSearch, "Search...");
            txtSalesSearch.Dock = DockStyle.Fill;
            txtSalesSearch.TextChanged += (s, e) => LoadDailySales();
            pnlSearch.Controls.Add(txtSalesSearch);
            filterBar.Controls.Add(pnlSearch);

            page.Controls.Add(filterBar);

            // 5 Top KPI Metric Cards for Sales Register
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 5;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            Panel cardRev = Theme.CreateCard(170, 65);
            cardRev.Dock = DockStyle.Fill;
            cardRev.Margin = new Padding(0, 0, 6, 0);
            lblSalesTotalVal = CreatePLCardContent(cardRev, "TOTAL SALES REVENUE", "Rs. 0.00", Theme.Accent);
            layoutCards.Controls.Add(cardRev, 0, 0);

            Panel cardInvoices = Theme.CreateCard(140, 65);
            cardInvoices.Dock = DockStyle.Fill;
            cardInvoices.Margin = new Padding(6, 0, 6, 0);
            lblSalesCountVal = CreatePLCardContent(cardInvoices, "INVOICES ISSUED", "0 Invoices", Theme.TextWhite);
            layoutCards.Controls.Add(cardInvoices, 1, 0);

            Panel cardCash = Theme.CreateCard(170, 65);
            cardCash.Dock = DockStyle.Fill;
            cardCash.Margin = new Padding(6, 0, 6, 0);
            lblSalesCashVal = CreatePLCardContent(cardCash, "💵 CASH COLLECTED", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardCash, 2, 0);

            Panel cardOnline = Theme.CreateCard(180, 65);
            cardOnline.Dock = DockStyle.Fill;
            cardOnline.Margin = new Padding(6, 0, 6, 0);
            lblSalesOnlineVal = CreatePLCardContent(cardOnline, "📱 ONLINE (QR/CARD)", "Rs. 0.00", Color.FromArgb(56, 189, 248));
            layoutCards.Controls.Add(cardOnline, 3, 0);

            Panel cardDue = Theme.CreateCard(140, 65);
            cardDue.Dock = DockStyle.Fill;
            cardDue.Margin = new Padding(6, 0, 0, 0);
            lblSalesDueVal = CreatePLCardContent(cardDue, "OUTSTANDING DUE", "Rs. 0.00", Theme.Danger);
            layoutCards.Controls.Add(cardDue, 4, 0);

            // GridView
            gridSalesReport = new DataGridView();
            gridSalesReport.Location = new Point(20, 150);
            gridSalesReport.Size = new Size(870, 285);
            gridSalesReport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridSalesReport);
            page.Controls.Add(gridSalesReport);

            // Summary Footer Card Bar
            Panel summaryBar = Theme.CreateCard(870, 48);
            summaryBar.Location = new Point(20, 445);
            summaryBar.Size = new Size(870, 48);
            summaryBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            summaryBar.BackColor = Color.FromArgb(17, 24, 39);
            summaryBar.Padding = new Padding(15, 0, 15, 0);

            lblSalesSummary = new Label();
            lblSalesSummary.Text = "Invoices: 0  •  Discount: Rs. 0.00  •  VAT: Rs. 0.00  •  Total: Rs. 0.00  •  Paid: Rs. 0.00  •  Due: Rs. 0.00";
            lblSalesSummary.Dock = DockStyle.Fill;
            lblSalesSummary.Padding = new Padding(0);
            lblSalesSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblSalesSummary, Theme.TextLight, new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold));
            summaryBar.Controls.Add(lblSalesSummary);
            page.Controls.Add(summaryBar);

            comboSalesDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboSalesDateFilter.SelectedItem.ToString(), salesFromDate, salesToDate, () => LoadDailySales());
            };
            comboSalesDateFilter.SelectedIndex = 3; // Default "This Month"

            // Context Menu & Cell Double-Click to View Details or Copy Invoice Number
            ContextMenuStrip cmsSales = new ContextMenuStrip();
            ToolStripMenuItem menuCopyInvoice = new ToolStripMenuItem("📋 Copy Selected Invoice Number");
            menuCopyInvoice.Click += (s, e) =>
            {
                if (gridSalesReport.SelectedRows.Count > 0)
                {
                    DataGridViewRow row = gridSalesReport.SelectedRows[0];
                    if (row.Cells["Invoice No"] != null && row.Cells["Invoice No"].Value != null)
                    {
                        string invoiceNo = row.Cells["Invoice No"].Value.ToString();
                        Clipboard.SetText(invoiceNo);
                        MessageBox.Show($"Invoice Number '{invoiceNo}' copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
            cmsSales.Items.Add(menuCopyInvoice);

            ToolStripMenuItem menuViewDetails = new ToolStripMenuItem("🔍 View Invoice Items & Details");
            menuViewDetails.Click += (s, e) =>
            {
                if (gridSalesReport.SelectedRows.Count > 0)
                {
                    DataGridViewRow row = gridSalesReport.SelectedRows[0];
                    if (row.Cells["Invoice No"] != null && row.Cells["Invoice No"].Value != null)
                    {
                        string invoiceNo = row.Cells["Invoice No"].Value.ToString();
                        using (InvoiceDetailsForm dlg = new InvoiceDetailsForm(invoiceNo))
                        {
                            dlg.ShowDialog();
                        }
                    }
                }
            };
            cmsSales.Items.Add(menuViewDetails);

            gridSalesReport.ContextMenuStrip = cmsSales;

            gridSalesReport.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    gridSalesReport.ClearSelection();
                    gridSalesReport.Rows[e.RowIndex].Selected = true;
                }
            };

            gridSalesReport.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    if (gridSalesReport.Rows[e.RowIndex].Cells["Invoice No"] != null && 
                        gridSalesReport.Rows[e.RowIndex].Cells["Invoice No"].Value != null)
                    {
                        string invoiceNo = gridSalesReport.Rows[e.RowIndex].Cells["Invoice No"].Value.ToString();
                        
                        // Copy invoice number silently in the background
                        Clipboard.SetText(invoiceNo);

                        // Show invoice breakup details dialog
                        using (InvoiceDetailsForm dlg = new InvoiceDetailsForm(invoiceNo))
                        {
                            dlg.ShowDialog();
                        }
                    }
                }
            };

        }

        private void InitializePLTab(Panel page)
        {
            // Filters Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboPLDateFilter = new ComboBox();
            comboPLDateFilter.Size = new Size(115, 28);
            comboPLDateFilter.Margin = new Padding(2, 2, 4, 2);
            comboPLDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPLDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboPLDateFilter);
            filterBar.Controls.Add(comboPLDateFilter);

            plFromDate = new DateTimePicker();
            plFromDate.Format = DateTimePickerFormat.Short;
            plFromDate.Size = new Size(95, 28);
            plFromDate.Margin = new Padding(2, 2, 4, 2);
            plFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(plFromDate);

            plToDate = new DateTimePicker();
            plToDate.Format = DateTimePickerFormat.Short;
            plToDate.Size = new Size(95, 28);
            plToDate.Margin = new Padding(2, 2, 4, 2);
            plToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(plToDate);

            btnCalculatePL = new Button();
            btnCalculatePL.Text = "📊 Compute Analytics";
            btnCalculatePL.Size = new Size(165, 28);
            btnCalculatePL.Margin = new Padding(4, 2, 4, 2);
            Theme.StylePrimaryButton(btnCalculatePL);
            btnCalculatePL.Click += (s, e) => CalculateProfitLoss();
            filterBar.Controls.Add(btnCalculatePL);

            Button btnExportPL = new Button();
            btnExportPL.Text = "📥 Export Excel";
            btnExportPL.Size = new Size(125, 28);
            btnExportPL.Margin = new Padding(4, 2, 4, 2);
            Theme.StyleSuccessButton(btnExportPL);
            btnExportPL.Click += (s, e) => ExportPLExcel();
            filterBar.Controls.Add(btnExportPL);

            page.Controls.Add(filterBar);

            // Responsive Layout Table for Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 85);
            layoutCards.ColumnCount = 3;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // 1. Total Revenue Card
            cardRevenue = Theme.CreateCard(270, 120);
            cardRevenue.Dock = DockStyle.Fill;
            cardRevenue.Margin = new Padding(0, 0, 15, 0);
            cardRevenue.BackColor = Color.FromArgb(17, 24, 39);
            lblRevenueVal = CreatePLCardContent(cardRevenue, "TOTAL REVENUE (RETAIL SALES)", "Rs. 0.00", Theme.TextLight);
            layoutCards.Controls.Add(cardRevenue, 0, 0);

            // 2. Total COGS Card
            cardCOGS = Theme.CreateCard(270, 120);
            cardCOGS.Dock = DockStyle.Fill;
            cardCOGS.Margin = new Padding(15, 0, 15, 0);
            cardCOGS.BackColor = Color.FromArgb(17, 24, 39);
            lblCOGSVal = CreatePLCardContent(cardCOGS, "COST OF GOODS SOLD (COGS)", "Rs. 0.00", Theme.TextDark);
            layoutCards.Controls.Add(cardCOGS, 1, 0);

            // 3. Net Performance Card
            cardNetProfit = Theme.CreateCard(270, 120);
            cardNetProfit.Dock = DockStyle.Fill;
            cardNetProfit.Margin = new Padding(15, 0, 0, 0);
            cardNetProfit.BackColor = Color.FromArgb(17, 24, 39);
            lblNetVal = CreatePLCardContent(cardNetProfit, "NET MARGIN (PROFIT / LOSS)", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardNetProfit, 2, 0);

            // Detailed Analytics Explanation
            lblPLSummary = new Label();
            lblPLSummary.Text = "P&L Summary will appear above. Sales and Costs are filtered based on the date range defined.";
            lblPLSummary.Location = new Point(20, 235);
            lblPLSummary.Size = new Size(870, 250);
            lblPLSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleLabel(lblPLSummary, Theme.TextDark, Theme.MainFont);
            page.Controls.Add(lblPLSummary);

            comboPLDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboPLDateFilter.SelectedItem.ToString(), plFromDate, plToDate, () => CalculateProfitLoss());
            };
            comboPLDateFilter.SelectedIndex = 3; // "This Month"
        }

        private Label CreatePLCardContent(Panel card, string header, string initVal, Color valColor)
        {
            Label dummy;
            return CreatePLCardContent(card, header, initVal, valColor, out dummy);
        }

        private Label CreatePLCardContent(Panel card, string header, string initVal, Color valColor, out Label lblHeader)
        {
            lblHeader = new Label();
            lblHeader.Text = header;
            lblHeader.Location = new Point(12, 12);
            lblHeader.AutoSize = true;
            lblHeader.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            Theme.StyleLabel(lblHeader, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(lblHeader);

            Label lblVal = new Label();
            lblVal.Text = initVal;
            lblVal.Location = new Point(12, 38);
            lblVal.Size = new Size(card.Width - 24, 60);
            lblVal.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Theme.StyleLabel(lblVal, valColor, new Font("Segoe UI", 18F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void LoadDailySales()
        {
            try
            {
                if (gridSalesReport == null) return;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    try
                    {
                        using (SqlCommand fixCmd = new SqlCommand("UPDATE Sales SET AmountPaid = GrandTotal, CashAmount = CASE WHEN PaymentMethod = 'Cash' THEN GrandTotal ELSE CashAmount END WHERE AmountPaid > GrandTotal", conn))
                        {
                            fixCmd.ExecuteNonQuery();
                        }
                    }
                    catch { }

                    string query;
                    if (comboSalesTypeFilter != null && comboSalesTypeFilter.SelectedIndex == 1) // Service Sale
                    {
                        query = @"
                            WITH FilteredDetails AS (
                                SELECT 
                                    sd.SaleId,
                                    SUM(sd.Total) AS ItemSubTotal,
                                    SUM(ISNULL(ROUND(sd.Total * (ISNULL(s.Discount, 0.0) / NULLIF(s.SubTotal, 0.0)), 2), 0.00)) AS ItemDiscount,
                                    SUM(ISNULL(sd.CGSTAmount, 0.0) + ISNULL(sd.SGSTAmount, 0.0) + ISNULL(sd.IGSTAmount, 0.0)) AS ItemTax
                                FROM SaleDetails sd
                                INNER JOIN Sales s ON sd.SaleId = s.Id
                                WHERE sd.ItemType = 'Service'
                                GROUP BY sd.SaleId
                            )
                            SELECT 
                                s.InvoiceNumber as [Invoice No], 
                                s.SaleDate as [Sale Date], 
                                ISNULL(c.Name, 'Walk-in Client') as [Customer],
                                fd.ItemSubTotal as [SubTotal], 
                                fd.ItemDiscount as [Discount], 
                                fd.ItemTax as [Tax], 
                                CASE 
                                    WHEN s.SubTotal > 0 THEN ROUND(s.GrandTotal * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Grand Total], 
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) > s.GrandTotal 
                                            THEN s.GrandTotal 
                                            ELSE (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Amount Paid], 
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN s.PaymentMethod = 'Cash' THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                            WHEN s.PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(s.CashAmount, 0) > s.GrandTotal THEN s.GrandTotal ELSE ISNULL(s.CashAmount, 0) END)
                                            ELSE 0.00
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Cash Paid],
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN ISNULL(s.OnlineAmount, 0) > 0 THEN (CASE WHEN s.OnlineAmount > s.GrandTotal THEN s.GrandTotal ELSE s.OnlineAmount END)
                                            WHEN s.PaymentMethod IN ('Card', 'QR Pay', 'UPI', 'Wallet', 'Online', 'QR Pay / UPI', 'UPI / QR Pay', 'Card / POS') 
                                                 OR s.PaymentMethod LIKE '%UPI%' OR s.PaymentMethod LIKE '%QR%' OR s.PaymentMethod LIKE '%Card%' OR s.PaymentMethod LIKE '%Online%'
                                            THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                            ELSE 0.00
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Online Paid],
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) < 0 
                                            THEN 0.00 
                                            ELSE (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Due Amount], 
                                CASE
                                    WHEN s.PaymentMethod = 'Split' THEN ('Split (Cash: ' + CAST(CAST(ISNULL(s.CashAmount, 0) AS INT) AS VARCHAR) + ' | Online: ' + CAST(CAST(ISNULL(s.OnlineAmount, 0) AS INT) AS VARCHAR) + ')')
                                    ELSE s.PaymentMethod
                                END as [Pay Mode]
                            FROM FilteredDetails fd
                            INNER JOIN Sales s ON fd.SaleId = s.Id
                            LEFT JOIN Customers c ON s.CustomerId = c.Id
                            WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                              AND (ISNULL(c.Name, '') LIKE @search OR s.InvoiceNumber LIKE @search)
                            ORDER BY s.SaleDate DESC";
                    }
                    else if (comboSalesTypeFilter != null && comboSalesTypeFilter.SelectedIndex == 2) // Product Sale
                    {
                        query = @"
                            WITH FilteredDetails AS (
                                SELECT 
                                    sd.SaleId,
                                    SUM(sd.Total) AS ItemSubTotal,
                                    SUM(ISNULL(ROUND(sd.Total * (ISNULL(s.Discount, 0.0) / NULLIF(s.SubTotal, 0.0)), 2), 0.00)) AS ItemDiscount,
                                    SUM(ISNULL(sd.CGSTAmount, 0.0) + ISNULL(sd.SGSTAmount, 0.0) + ISNULL(sd.IGSTAmount, 0.0)) AS ItemTax
                                FROM SaleDetails sd
                                INNER JOIN Sales s ON sd.SaleId = s.Id
                                WHERE (sd.ItemType = 'Product' OR sd.ItemType IS NULL OR sd.ItemType = '')
                                GROUP BY sd.SaleId
                            )
                            SELECT 
                                s.InvoiceNumber as [Invoice No], 
                                s.SaleDate as [Sale Date], 
                                ISNULL(c.Name, 'Walk-in Client') as [Customer],
                                fd.ItemSubTotal as [SubTotal], 
                                fd.ItemDiscount as [Discount], 
                                fd.ItemTax as [Tax], 
                                CASE 
                                    WHEN s.SubTotal > 0 THEN ROUND(s.GrandTotal * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Grand Total], 
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) > s.GrandTotal 
                                            THEN s.GrandTotal 
                                            ELSE (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Amount Paid], 
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN s.PaymentMethod = 'Cash' THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                            WHEN s.PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(s.CashAmount, 0) > s.GrandTotal THEN s.GrandTotal ELSE ISNULL(s.CashAmount, 0) END)
                                            ELSE 0.00
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Cash Paid],
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN ISNULL(s.OnlineAmount, 0) > 0 THEN (CASE WHEN s.OnlineAmount > s.GrandTotal THEN s.GrandTotal ELSE s.OnlineAmount END)
                                            WHEN s.PaymentMethod IN ('Card', 'QR Pay', 'UPI', 'Wallet', 'Online', 'QR Pay / UPI', 'UPI / QR Pay', 'Card / POS') 
                                                 OR s.PaymentMethod LIKE '%UPI%' OR s.PaymentMethod LIKE '%QR%' OR s.PaymentMethod LIKE '%Card%' OR s.PaymentMethod LIKE '%Online%'
                                            THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                            ELSE 0.00
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Online Paid],
                                CASE 
                                    WHEN s.SubTotal > 0 
                                    THEN ROUND((CASE 
                                            WHEN (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) < 0 
                                            THEN 0.00 
                                            ELSE (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                        END) * (fd.ItemSubTotal / s.SubTotal), 2)
                                    ELSE 0.00
                                END as [Due Amount], 
                                CASE
                                    WHEN s.PaymentMethod = 'Split' THEN ('Split (Cash: ' + CAST(CAST(ISNULL(s.CashAmount, 0) AS INT) AS VARCHAR) + ' | Online: ' + CAST(CAST(ISNULL(s.OnlineAmount, 0) AS INT) AS VARCHAR) + ')')
                                    ELSE s.PaymentMethod
                                END as [Pay Mode]
                            FROM FilteredDetails fd
                            INNER JOIN Sales s ON fd.SaleId = s.Id
                            LEFT JOIN Customers c ON s.CustomerId = c.Id
                            WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                              AND (ISNULL(c.Name, '') LIKE @search OR s.InvoiceNumber LIKE @search)
                            ORDER BY s.SaleDate DESC";
                    }
                    else
                    {
                        query = @"
                            SELECT s.InvoiceNumber as [Invoice No], s.SaleDate as [Sale Date], ISNULL(c.Name, 'Walk-in Client') as [Customer],
                                    s.SubTotal as [SubTotal], s.Discount as [Discount], s.Tax as [Tax], 
                                    s.GrandTotal as [Grand Total], 
                                    CASE 
                                        WHEN (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) > s.GrandTotal 
                                        THEN s.GrandTotal 
                                        ELSE (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                    END as [Amount Paid], 
                                    CASE 
                                        WHEN s.PaymentMethod = 'Cash' THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                        WHEN s.PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(s.CashAmount, 0) > s.GrandTotal THEN s.GrandTotal ELSE ISNULL(s.CashAmount, 0) END)
                                        ELSE 0.00
                                    END as [Cash Paid],
                                    CASE 
                                        WHEN ISNULL(s.OnlineAmount, 0) > 0 THEN (CASE WHEN s.OnlineAmount > s.GrandTotal THEN s.GrandTotal ELSE s.OnlineAmount END)
                                        WHEN s.PaymentMethod IN ('Card', 'QR Pay', 'UPI', 'Wallet', 'Online', 'QR Pay / UPI', 'UPI / QR Pay', 'Card / POS') 
                                             OR s.PaymentMethod LIKE '%UPI%' OR s.PaymentMethod LIKE '%QR%' OR s.PaymentMethod LIKE '%Card%' OR s.PaymentMethod LIKE '%Online%'
                                        THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                        ELSE 0.00
                                    END as [Online Paid],
                                    CASE 
                                        WHEN (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) < 0 
                                        THEN 0.00 
                                        ELSE (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                                    END as [Due Amount], 
                                    CASE
                                        WHEN s.PaymentMethod = 'Split' THEN ('Split (Cash: ' + CAST(CAST(ISNULL(s.CashAmount, 0) AS INT) AS VARCHAR) + ' | Online: ' + CAST(CAST(ISNULL(s.OnlineAmount, 0) AS INT) AS VARCHAR) + ')')
                                        ELSE s.PaymentMethod
                                    END as [Pay Mode]
                            FROM Sales s
                            LEFT JOIN Customers c ON s.CustomerId = c.Id
                            WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                              AND (ISNULL(c.Name, '') LIKE @search OR s.InvoiceNumber LIKE @search)
                            ORDER BY s.SaleDate DESC";
                    }

                    string searchVal = (txtSalesSearch != null && !string.IsNullOrWhiteSpace(txtSalesSearch.Text)) 
                        ? "%" + txtSalesSearch.Text.Trim() + "%" 
                        : "%";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", salesFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", salesToDate.Value.Date);
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridSalesReport.DataSource = dt;

                            // Apply custom column formats and weights
                            if (gridSalesReport.Columns["SubTotal"] != null) gridSalesReport.Columns["SubTotal"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Discount"] != null) gridSalesReport.Columns["Discount"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Tax"] != null) gridSalesReport.Columns["Tax"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Grand Total"] != null) gridSalesReport.Columns["Grand Total"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Amount Paid"] != null) gridSalesReport.Columns["Amount Paid"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Cash Paid"] != null) gridSalesReport.Columns["Cash Paid"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Online Paid"] != null) gridSalesReport.Columns["Online Paid"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Due Amount"] != null) gridSalesReport.Columns["Due Amount"].DefaultCellStyle.Format = "N2";

                            if (gridSalesReport.Columns["Invoice No"] != null) gridSalesReport.Columns["Invoice No"].FillWeight = 115;
                            if (gridSalesReport.Columns["Sale Date"] != null) gridSalesReport.Columns["Sale Date"].FillWeight = 100;
                            if (gridSalesReport.Columns["Customer"] != null) gridSalesReport.Columns["Customer"].FillWeight = 110;
                            if (gridSalesReport.Columns["SubTotal"] != null) gridSalesReport.Columns["SubTotal"].FillWeight = 70;
                            if (gridSalesReport.Columns["Discount"] != null) gridSalesReport.Columns["Discount"].FillWeight = 65;
                            if (gridSalesReport.Columns["Tax"] != null) gridSalesReport.Columns["Tax"].FillWeight = 60;
                            if (gridSalesReport.Columns["Grand Total"] != null) gridSalesReport.Columns["Grand Total"].FillWeight = 75;
                            if (gridSalesReport.Columns["Amount Paid"] != null) gridSalesReport.Columns["Amount Paid"].FillWeight = 70;
                            if (gridSalesReport.Columns["Cash Paid"] != null) gridSalesReport.Columns["Cash Paid"].FillWeight = 68;
                            if (gridSalesReport.Columns["Online Paid"] != null) gridSalesReport.Columns["Online Paid"].FillWeight = 68;
                            if (gridSalesReport.Columns["Due Amount"] != null) gridSalesReport.Columns["Due Amount"].FillWeight = 65;
                            if (gridSalesReport.Columns["Pay Mode"] != null) gridSalesReport.Columns["Pay Mode"].FillWeight = 110;

                            decimal totalGrand = 0;
                            decimal totalPaid = 0;
                            decimal totalCashPaid = 0;
                            decimal totalOnlinePaid = 0;
                            decimal totalDue = 0;
                            decimal totalDiscount = 0;
                            decimal totalTax = 0;

                            foreach (DataRow r in dt.Rows)
                            {
                                totalGrand += Convert.ToDecimal(r["Grand Total"]);
                                totalPaid += Convert.ToDecimal(r["Amount Paid"]);
                                totalCashPaid += Convert.ToDecimal(r["Cash Paid"]);
                                totalOnlinePaid += Convert.ToDecimal(r["Online Paid"]);
                                totalDue += Convert.ToDecimal(r["Due Amount"]);
                                totalDiscount += Convert.ToDecimal(r["Discount"]);
                                totalTax += Convert.ToDecimal(r["Tax"]);
                            }

                            if (lblSalesTotalVal != null) lblSalesTotalVal.Text = $"Rs. {totalGrand:N2}";
                            if (lblSalesCountVal != null) lblSalesCountVal.Text = $"{dt.Rows.Count} Invoices";
                            if (lblSalesCashVal != null) lblSalesCashVal.Text = $"Rs. {totalCashPaid:N2}";
                            if (lblSalesOnlineVal != null) lblSalesOnlineVal.Text = $"Rs. {totalOnlinePaid:N2}";
                            if (lblSalesDueVal != null) lblSalesDueVal.Text = $"Rs. {totalDue:N2}";

                            if (lblSalesSummary != null)
                            {
                                lblSalesSummary.Text = $"Total Invoices: {dt.Rows.Count}  •  Gross Sales: Rs. {totalGrand:N2}  •  💵 Cash: Rs. {totalCashPaid:N2}  •  📱 Online/QR: Rs. {totalOnlinePaid:N2}  •  Discount: Rs. {totalDiscount:N2}  •  Tax: Rs. {totalTax:N2}  •  Due: Rs. {totalDue:N2}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sales logs: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateProfitLoss()
        {
            try
            {
                if (lblRevenueVal == null) return;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    DateTime fromDate = plFromDate.Value.Date;
                    DateTime toDate = plToDate.Value.Date;

                    // 1. Get Revenue (Sum of Sales GrandTotal minus Sales Returns TotalRefund)
                    decimal salesRevenue = 0;
                    decimal returnedRefund = 0;
                    
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales WHERE CAST(SaleDate as DATE) BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        salesRevenue = (decimal)cmd.ExecuteScalar();
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalRefund), 0) FROM SalesReturns WHERE CAST(ReturnDate as DATE) BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        returnedRefund = (decimal)cmd.ExecuteScalar();
                    }

                    decimal revenue = salesRevenue - returnedRefund;
                    lblRevenueVal.Text = $"Rs. {revenue:N2}";

                    // 2. Cost of Goods Sold (COGS for retail products)
                    decimal grossCogs = 0;
                    decimal resellableReturnCost = 0;

                    string grossCogsQuery = @"
                        SELECT ISNULL(SUM(sd.Quantity * sd.PurchaseCostAtSale), 0)
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to AND sd.ItemType = 'Product'";

                    using (SqlCommand cmd = new SqlCommand(grossCogsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        grossCogs = (decimal)cmd.ExecuteScalar();
                    }

                    string returnCostQuery = @"
                        SELECT ISNULL(SUM(srd.Quantity * sd.PurchaseCostAtSale), 0)
                        FROM SalesReturnDetails srd
                        INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                        INNER JOIN SaleDetails sd ON sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId
                        WHERE srd.ItemCondition = 'Resellable' 
                          AND CAST(sr.ReturnDate as DATE) BETWEEN @from AND @to";

                    using (SqlCommand cmd = new SqlCommand(returnCostQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        resellableReturnCost = (decimal)cmd.ExecuteScalar();
                    }

                    decimal cogs = Math.Max(0, grossCogs - resellableReturnCost);
                    lblCOGSVal.Text = $"Rs. {cogs:N2}";

                    // 3. Staff Commission Expenses
                    decimal totalStaffCommission = 0;
                    string commQuery = @"
                        SELECT ISNULL(SUM(sd.Total * (ISNULL(st.CommissionRate, 10.0) / 100.0)), 0)
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        INNER JOIN Staff st ON sd.StaffId = st.Id
                        WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                          AND sd.ItemType = 'Service'";
                    
                    using (SqlCommand cmd = new SqlCommand(commQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        totalStaffCommission = (decimal)cmd.ExecuteScalar();
                    }

                    // 4. Net Operating Margin Performance
                    decimal netPerformance = revenue - cogs - totalStaffCommission;
                    lblNetVal.Text = $"Rs. {netPerformance:N2}";

                    if (netPerformance >= 0)
                    {
                        lblNetVal.ForeColor = Theme.Success;
                        cardNetProfit.BackColor = Color.FromArgb(15, 35, 20); // Subtle green highlight
                    }
                    else
                    {
                        lblNetVal.ForeColor = Theme.Danger;
                        cardNetProfit.BackColor = Color.FromArgb(45, 15, 15); // Subtle red highlight
                    }

                    // 5. Details breakdown text
                    decimal marginPercent = revenue > 0 ? (netPerformance / revenue) * 100 : 0;
                    lblPLSummary.Text = $@"--- Cafe & Restaurant Profit & Loss Statement (Analytica Summary) ---

Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}

1. REVENUE BREAKDOWN:
   • Gross Customer Sales & Orders: Rs. {salesRevenue:N2}
   • Returns & Refunds: Rs. {returnedRefund:N2}
   • NET REVENUE: Rs. {revenue:N2}

2. OPERATING COSTS:
   • Cost of Goods Sold (Kitchen Ingredients / Stock COGS): Rs. {cogs:N2}
   • Staff Incentives & Commissions: Rs. {totalStaffCommission:N2}
   • TOTAL DIRECT OPERATING COSTS: Rs. {cogs + totalStaffCommission:N2}

3. NET OPERATING PERFORMANCE:
   • Operating Net Margin: Rs. {netPerformance:N2}
   • Net Profit Margin Ratio: {marginPercent:F2}%

[Note] Food dishes and beverages track inventory COGS and sales revenue for cafe operations.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating Profit & Loss: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializePurchaseTab(Panel page)
        {
            // Filters Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboPurchaseDateFilter = new ComboBox();
            comboPurchaseDateFilter.Size = new Size(110, 28);
            comboPurchaseDateFilter.Margin = new Padding(2, 2, 3, 2);
            comboPurchaseDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPurchaseDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboPurchaseDateFilter);
            filterBar.Controls.Add(comboPurchaseDateFilter);

            purchaseFromDate = new DateTimePicker();
            purchaseFromDate.Format = DateTimePickerFormat.Short;
            purchaseFromDate.Size = new Size(95, 28);
            purchaseFromDate.Margin = new Padding(2, 2, 3, 2);
            purchaseFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(purchaseFromDate);

            purchaseToDate = new DateTimePicker();
            purchaseToDate.Format = DateTimePickerFormat.Short;
            purchaseToDate.Size = new Size(95, 28);
            purchaseToDate.Margin = new Padding(2, 2, 3, 2);
            purchaseToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(purchaseToDate);

            comboFilterSupplier = new ComboBox();
            comboFilterSupplier.Size = new Size(125, 28);
            comboFilterSupplier.Margin = new Padding(2, 2, 3, 2);
            comboFilterSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboFilterSupplier);
            filterBar.Controls.Add(comboFilterSupplier);

            comboPurchaseReportType = new ComboBox();
            comboPurchaseReportType.Size = new Size(135, 28);
            comboPurchaseReportType.Margin = new Padding(2, 2, 3, 2);
            comboPurchaseReportType.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboPurchaseReportType);
            comboPurchaseReportType.Items.AddRange(new string[] { "Invoice Summary", "Product-wise History", "Category-wise History" });
            comboPurchaseReportType.SelectedIndex = 0;
            filterBar.Controls.Add(comboPurchaseReportType);

            btnPurchaseSearch = new Button();
            btnPurchaseSearch.Text = "🔍 Search";
            btnPurchaseSearch.Size = new Size(80, 28);
            btnPurchaseSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnPurchaseSearch);
            btnPurchaseSearch.Click += (s, e) => LoadPurchaseHistory();
            filterBar.Controls.Add(btnPurchaseSearch);

            Button btnExportPurchase = new Button();
            btnExportPurchase.Text = "📊 Export Excel";
            btnExportPurchase.Size = new Size(118, 28);
            btnExportPurchase.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportPurchase);
            btnExportPurchase.Click += (s, e) => ExportGridToExcel(gridPurchaseReport, "Purchase_Register", "Purchase & Stock Inward Report");
            filterBar.Controls.Add(btnExportPurchase);

            page.Controls.Add(filterBar);

            // GridView
            gridPurchaseReport = new DataGridView();
            gridPurchaseReport.Location = new Point(20, 75);
            gridPurchaseReport.Size = new Size(870, 360);
            gridPurchaseReport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridPurchaseReport);
            gridPurchaseReport.CellDoubleClick += GridPurchaseReport_CellDoubleClick;
            page.Controls.Add(gridPurchaseReport);

            // Summary Footer Card Bar
            Panel summaryBar = Theme.CreateCard(870, 48);
            summaryBar.Location = new Point(20, 445);
            summaryBar.Size = new Size(870, 48);
            summaryBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            summaryBar.BackColor = Color.FromArgb(17, 24, 39);
            summaryBar.Padding = new Padding(15, 0, 15, 0);

            lblPurchaseSummary = new Label();
            lblPurchaseSummary.Text = "Inward Invoices: 0  •  Total Valuation: Rs. 0.00";
            lblPurchaseSummary.Dock = DockStyle.Fill;
            lblPurchaseSummary.Padding = new Padding(0);
            lblPurchaseSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblPurchaseSummary, Theme.TextLight, new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold));
            summaryBar.Controls.Add(lblPurchaseSummary);
            page.Controls.Add(summaryBar);

            comboPurchaseDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboPurchaseDateFilter.SelectedItem.ToString(), purchaseFromDate, purchaseToDate, () => LoadPurchaseHistory());
            };
            comboPurchaseDateFilter.SelectedIndex = 3; // "This Month"
        }

        private void LoadSuppliersFilterDropdown()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Suppliers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            DataRow newRow = dt.NewRow();
                            newRow["Id"] = -1;
                            newRow["Name"] = "-- All Suppliers --";
                            dt.Rows.InsertAt(newRow, 0);

                            comboFilterSupplier.DataSource = dt;
                            comboFilterSupplier.DisplayMember = "Name";
                            comboFilterSupplier.ValueMember = "Id";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers filter list: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPurchaseHistory()
        {
            try
            {
                if (gridPurchaseReport == null || lblPurchaseSummary == null || purchaseFromDate == null || purchaseToDate == null) return;

                int supplierId = -1;
                if (comboFilterSupplier != null && comboFilterSupplier.SelectedValue != null)
                {
                    if (comboFilterSupplier.SelectedValue is int)
                    {
                        supplierId = (int)comboFilterSupplier.SelectedValue;
                    }
                    else if (comboFilterSupplier.SelectedValue is DataRowView drv)
                    {
                        supplierId = (int)drv["Id"];
                    }
                }

                string reportType = comboPurchaseReportType?.SelectedItem?.ToString() ?? "Invoice Summary";

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    string query = "";
                    string sumQuery = "";

                    if (reportType == "Invoice Summary")
                    {
                        query = @"
                            SELECT p.PurchaseNumber as [Purchase No], 
                                   p.PurchaseDate as [Purchase Date], 
                                   s.Name as [Supplier], 
                                   p.TotalAmount as [Total Cost], 
                                   u.FullName as [Created By]
                            FROM Purchases p
                            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
                            LEFT JOIN Users u ON p.CreatedBy = u.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " ORDER BY p.PurchaseDate DESC";

                        sumQuery = @"
                            SELECT COUNT(*), ISNULL(SUM(TotalAmount), 0)
                            FROM Purchases
                            WHERE CAST(PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND SupplierId = @supplierId";
                        }
                    }
                    else if (reportType == "Product-wise History")
                    {
                        query = @"
                            SELECT prod.Code as [Product Code],
                                   prod.Name as [Product Name],
                                   prod.Category as [Category],
                                   SUM(pd.Quantity) as [Qty Purchased],
                                   CAST(AVG(pd.PurchasePrice) AS DECIMAL(18,2)) as [Avg Price],
                                   SUM(pd.Quantity * pd.PurchasePrice) as [Total Investment]
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " GROUP BY prod.Code, prod.Name, prod.Category ORDER BY [Total Investment] DESC";

                        sumQuery = @"
                            SELECT COUNT(DISTINCT pd.ProductId), ISNULL(SUM(pd.Quantity * pd.PurchasePrice), 0)
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND p.SupplierId = @supplierId";
                        }
                    }
                    else if (reportType == "Category-wise History")
                    {
                        query = @"
                            SELECT prod.Category as [Category],
                                   SUM(pd.Quantity) as [Total Qty Purchased],
                                   SUM(pd.Quantity * pd.PurchasePrice) as [Total Investment]
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " GROUP BY prod.Category ORDER BY [Total Investment] DESC";

                        sumQuery = @"
                            SELECT COUNT(DISTINCT prod.Category), ISNULL(SUM(pd.Quantity * pd.PurchasePrice), 0)
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND p.SupplierId = @supplierId";
                        }
                    }

                    // Populate Grid
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", purchaseFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", purchaseToDate.Value.Date);
                        if (supplierId != -1)
                        {
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridPurchaseReport.DataSource = dt;

                            // Apply custom column weights and formats depending on report type
                            if (reportType == "Invoice Summary")
                            {
                                if (gridPurchaseReport.Columns["Total Cost"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Cost"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Cost"].FillWeight = 100;
                                }
                                if (gridPurchaseReport.Columns["Purchase No"] != null) gridPurchaseReport.Columns["Purchase No"].FillWeight = 100;
                                if (gridPurchaseReport.Columns["Purchase Date"] != null) gridPurchaseReport.Columns["Purchase Date"].FillWeight = 120;
                                if (gridPurchaseReport.Columns["Supplier"] != null) gridPurchaseReport.Columns["Supplier"].FillWeight = 150;
                                if (gridPurchaseReport.Columns["Created By"] != null) gridPurchaseReport.Columns["Created By"].FillWeight = 100;
                            }
                            else if (reportType == "Product-wise History")
                            {
                                if (gridPurchaseReport.Columns["Avg Price"] != null)
                                {
                                    gridPurchaseReport.Columns["Avg Price"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Avg Price"].FillWeight = 80;
                                }
                                if (gridPurchaseReport.Columns["Total Investment"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Investment"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Investment"].FillWeight = 100;
                                }
                                if (gridPurchaseReport.Columns["Product Code"] != null) gridPurchaseReport.Columns["Product Code"].FillWeight = 80;
                                if (gridPurchaseReport.Columns["Product Name"] != null) gridPurchaseReport.Columns["Product Name"].FillWeight = 180;
                                if (gridPurchaseReport.Columns["Category"] != null) gridPurchaseReport.Columns["Category"].FillWeight = 90;
                                if (gridPurchaseReport.Columns["Qty Purchased"] != null) gridPurchaseReport.Columns["Qty Purchased"].FillWeight = 70;
                            }
                            else if (reportType == "Category-wise History")
                            {
                                if (gridPurchaseReport.Columns["Total Investment"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Investment"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Investment"].FillWeight = 120;
                                }
                                if (gridPurchaseReport.Columns["Category"] != null) gridPurchaseReport.Columns["Category"].FillWeight = 180;
                                if (gridPurchaseReport.Columns["Total Qty Purchased"] != null) gridPurchaseReport.Columns["Total Qty Purchased"].FillWeight = 100;
                            }
                        }
                    }

                    // Compute Summary Numbers
                    using (SqlCommand cmd = new SqlCommand(sumQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", purchaseFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", purchaseToDate.Value.Date);
                        if (supplierId != -1)
                        {
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                        }

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int count = r.GetInt32(0);
                                decimal sumVal = r.GetDecimal(1);

                                if (reportType == "Invoice Summary")
                                {
                                    lblPurchaseSummary.Text = $"Inward Invoices: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                                else if (reportType == "Product-wise History")
                                {
                                    lblPurchaseSummary.Text = $"Products Restocked: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                                else if (reportType == "Category-wise History")
                                {
                                    lblPurchaseSummary.Text = $"Categories Restocked: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading purchase logs: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReprintBill_Click(object sender, EventArgs e)
        {
            if (gridSalesReport.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an invoice from the Daily Sales Register to reprint.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string invoiceNo = gridSalesReport.SelectedRows[0].Cells["Invoice No"].Value.ToString();

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id FROM Sales WHERE InvoiceNumber = @invNum", conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNo);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            printSaleId = Convert.ToInt32(result);

                            /* ================= WINDOWS 7 / THERMAL PRINTER CHANGE =================
                               Original A4 reprint (kept for reverting):
                               reprintPreviewDlg.ShowDialog();
                               Replaced with a 4-inch dynamic-length receipt for TVS-E thermal printer.
                               ======================================================================= */
                            ThermalReceiptPrinter.ShowPreview(printSaleId);
                        }
                        else
                        {
                            MessageBox.Show("Could not find the selected sale transaction in the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving transaction details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReprintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int startX = 50;
            int startY = 50;

            Font fTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
            Font fSubTitle = new Font("Segoe UI", 9F, FontStyle.Italic);
            Font fRegular = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font fBold = new Font("Segoe UI", 10F, FontStyle.Bold);
            Font fDuplicate = new Font("Segoe UI", 12F, FontStyle.Bold);

            // Fetch checkout details from database dynamically for print
            string invNum = "", custName = "", custPhone = "", custAddr = "", dateStr = "", paymentMode = "";
            decimal sub = 0, disc = 0, tx = 0, grand = 0;
            decimal paidAmt = 0, dueAmt = 0;
            decimal totalRefund = 0, cashRefund = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.InvoiceNumber, s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, s.PaymentMethod,
                               c.Name, c.Phone, c.Address,
                               CASE 
                                   WHEN (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) > s.GrandTotal 
                                   THEN s.GrandTotal 
                                   ELSE (s.AmountPaid + ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                               END as AmountPaid,
                               CASE 
                                   WHEN (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) < 0 
                                   THEN 0.00 
                                   ELSE (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) 
                               END as DueAmount,
                               ISNULL((SELECT SUM(TotalRefund) FROM SalesReturns WHERE SaleId = s.Id), 0) as TotalRefund,
                               ISNULL((SELECT SUM(CashRefund) FROM SalesReturns WHERE SaleId = s.Id), 0) as CashRefund
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE s.Id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", printSaleId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                invNum = r.GetString(0);
                                dateStr = r.GetDateTime(1).ToString("yyyy-MM-dd HH:mm");
                                sub = r.GetDecimal(2);
                                disc = r.GetDecimal(3);
                                tx = r.GetDecimal(4);
                                grand = r.GetDecimal(5);
                                paymentMode = r.GetString(6);
                                custName = r.GetString(7);
                                custPhone = r.IsDBNull(8) ? "" : r.GetString(8);
                                custAddr = r.IsDBNull(9) ? "" : r.GetString(9);
                                paidAmt = r.GetDecimal(10);
                                dueAmt = r.GetDecimal(11);
                                totalRefund = r.GetDecimal(12);
                                cashRefund = r.GetDecimal(13);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (Brush bDark = new SolidBrush(Color.Black))
                {
                    g.DrawString($"Database Error Rendering Print: {ex.Message}", fRegular, bDark, startX, startY);
                }
                return;
            }

            using (Brush bDark = new SolidBrush(Color.Black))
            using (Brush bDuplicate = new SolidBrush(Theme.Danger))
            using (Pen pLine = new Pen(Color.Gray, 1))
            {
                // Fetch profile settings dynamically for branding
                string shopName = "Mero Dokan Shop", shopPhone = "+977-1-4200000", shopEmail = "contact@merodokan.com", shopAddress = "Kathmandu, Nepal", logoPath = "", shopGSTIN = "";
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, Phone, Email, Address, LogoPath, GSTIN FROM AppProfile", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    shopName = rdr["ShopName"].ToString();
                                    shopPhone = rdr["Phone"].ToString();
                                    shopEmail = rdr["Email"].ToString();
                                    shopAddress = rdr["Address"].ToString();
                                    logoPath = rdr["LogoPath"]?.ToString();
                                    shopGSTIN = rdr["GSTIN"]?.ToString();
                                }
                            }
                        }
                    }
                }
                catch { }

                // Header Section
                if (!string.IsNullOrEmpty(logoPath) && !File.Exists(logoPath))
                {
                    string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logoPath);
                    if (File.Exists(candidate)) logoPath = candidate;
                }
                if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath))
                {
                    string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                    string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                    if (File.Exists(p1)) logoPath = p1;
                    else if (File.Exists(p2)) logoPath = p2;
                }

                int textShiftX = 0;
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                {
                    try
                    {
                        using (Image logo = Image.FromFile(logoPath))
                        {
                            g.DrawImage(logo, startX, startY - 10, 60, 60);
                            textShiftX = 75;
                        }
                    }
                    catch { }
                }

                g.DrawString(shopName, fTitle, bDark, startX + textShiftX, startY);
                g.DrawString($"{shopAddress} | Phone: {shopPhone} | Email: {shopEmail}", fSubTitle, bDark, startX + textShiftX, startY + 30);
                
                int headerOffset = 0;
                if (!string.IsNullOrEmpty(shopGSTIN))
                {
                    g.DrawString($"GSTIN: {shopGSTIN}", fSubTitle, bDark, startX + textShiftX, startY + 48);
                    headerOffset = 20;
                }

                // Draw a prominent "DUPLICATE COPY" banner
                g.DrawString("*** DUPLICATE COPY ***", fDuplicate, bDuplicate, 400, startY - 12);

                // Draw scannable invoice number QR Code
                BarcodeHelper.DrawQRCode(g, invNum, 660, startY - 12, 60);

                g.DrawLine(pLine, startX, startY + 50 + headerOffset, 750, startY + 50 + headerOffset);

                // Customer Info Block
                g.DrawString($"Invoice No:  {invNum}", fBold, bDark, startX, startY + 65 + headerOffset);
                g.DrawString($"Invoice Date: {dateStr}", fRegular, bDark, 480, startY + 65 + headerOffset);
                
                g.DrawString($"Bill To:     {custName}", fRegular, bDark, startX, startY + 90 + headerOffset);
                g.DrawString($"Address:     {custAddr}", fRegular, bDark, startX, startY + 110 + headerOffset);
                g.DrawString($"Phone No:    {custPhone}", fRegular, bDark, startX, startY + 130 + headerOffset);

                g.DrawLine(pLine, startX, startY + 160 + headerOffset, 750, startY + 160 + headerOffset);

                // Table Headers
                int col1 = startX;
                int col2 = startX + 220;
                int col3 = startX + 350;
                int col4 = startX + 480;
                int col5 = startX + 600;

                int rowY = startY + 175 + headerOffset;
                g.DrawString("Product / Description", fBold, bDark, col1, rowY);
                g.DrawString("Qty Sold", fBold, bDark, col3, rowY);
                g.DrawString("Rate", fBold, bDark, col4, rowY);
                g.DrawString("Total Cost", fBold, bDark, col5, rowY);

                g.DrawLine(pLine, startX, rowY + 25, 750, rowY + 25);
                rowY += 35;

                // Render Items
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string detailsQuery = @"
                            SELECT 
                                CASE WHEN sd.ItemType = 'Service' THEN ISNULL(srv.Name, 'Kitchen Dish') ELSE ISNULL(p.Name, 'Menu Item') END as ItemName,
                                sd.Quantity, 
                                sd.UnitPrice, 
                                sd.Total,
                                ISNULL((SELECT SUM(srd.Quantity) 
                                        FROM SalesReturnDetails srd 
                                        INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id 
                                        WHERE sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId), 0) as ReturnedQty,
                                ISNULL(st.Name, '') as StylistName
                            FROM SaleDetails sd
                            LEFT JOIN Products p ON sd.ProductId = p.Id
                            LEFT JOIN Services srv ON sd.ServiceId = srv.Id
                            LEFT JOIN Staff st ON sd.StaffId = st.Id
                            WHERE sd.SaleId = @id";

                        using (SqlCommand cmd = new SqlCommand(detailsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", printSaleId);
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string pName = r.GetString(0);
                                    int qty = r.GetInt32(1);
                                    decimal rate = r.GetDecimal(2);
                                    decimal total = r.GetDecimal(3);
                                    int returnedQty = r.GetInt32(4);
                                    string stylist = r.GetString(5);

                                    string qtyStr = returnedQty > 0 ? $"{qty} (-{returnedQty})" : qty.ToString();
                                    string displayItem = string.IsNullOrEmpty(stylist) ? pName : $"{pName} ({stylist})";

                                    g.DrawString(displayItem, fRegular, bDark, col1, rowY);
                                    g.DrawString(qtyStr, fRegular, bDark, col3, rowY);
                                    g.DrawString($"Rs. {rate:F2}", fRegular, bDark, col4, rowY);
                                    g.DrawString($"Rs. {total:F2}", fRegular, bDark, col5, rowY);

                                    rowY += 25;
                                }
                            }
                        }
                    }
                }
                catch { }

                g.DrawLine(pLine, startX, rowY + 10, 750, rowY + 10);
                rowY += 25;

                // Summary Totals
                int summaryX = col4 - 40;
                g.DrawString("Sub Total:", fRegular, bDark, summaryX, rowY);
                g.DrawString($"Rs. {sub:N2}", fRegular, bDark, col5, rowY);
                rowY += 20;

                g.DrawString("Discount Amount:", fRegular, bDark, summaryX, rowY);
                g.DrawString($"- Rs. {disc:N2}", fRegular, bDark, col5, rowY);
                rowY += 20;

                decimal taxPercent = sub > 0 ? (tx / sub) * 100m : 0m;
                g.DrawString($"SGST & IGST ({taxPercent:0.##}%):", fRegular, bDark, summaryX, rowY);
                g.DrawString($"Rs. {tx:N2}", fRegular, bDark, col5, rowY);
                rowY += 25;

                g.DrawLine(pLine, summaryX, rowY - 5, 750, rowY - 5);

                g.DrawString("GRAND TOTAL:", fBold, bDark, summaryX, rowY);
                g.DrawString($"Rs. {grand:N2}", fBold, bDark, col5, rowY);
                rowY += 20;

                if (totalRefund > 0)
                {
                    g.DrawString("Returned Amount:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"- Rs. {totalRefund:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    g.DrawString("NET GRAND TOTAL:", fBold, bDark, summaryX, rowY);
                    g.DrawString($"Rs. {grand - totalRefund:N2}", fBold, bDark, col5, rowY);
                    rowY += 25;
                }
                else
                {
                    rowY += 5;
                }

                g.DrawString("Amount Paid:", fRegular, bDark, summaryX, rowY);
                g.DrawString($"Rs. {paidAmt:N2}", fRegular, bDark, col5, rowY);
                rowY += 20;

                if (cashRefund > 0)
                {
                    g.DrawString("Cash Refunded:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"- Rs. {cashRefund:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    g.DrawString("Net Paid Amount:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"Rs. {paidAmt - cashRefund:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;
                }

                g.DrawString("Balance Due:", fBold, bDark, summaryX, rowY);
                g.DrawString($"Rs. {dueAmt:N2}", fBold, bDark, col5, rowY);

                g.DrawString($"Payment Mode: {paymentMode}", fBold, bDark, startX, rowY);
                rowY += 25;

                // Fetch and draw repayment history if there is any
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string pQuery = @"
                            SELECT PaymentDate AS DateVal, Amount, PaymentMethod AS Method, Remarks 
                            FROM CustomerPayments 
                            WHERE SaleId = @saleId 
                            UNION ALL
                            SELECT ReturnDate AS DateVal, (TotalRefund - CashRefund) AS Amount, 'Return Offset' AS Method, 'Returned items offset' AS Remarks
                            FROM SalesReturns
                            WHERE SaleId = @saleId AND (TotalRefund - CashRefund) > 0
                            ORDER BY DateVal ASC";
                        using (SqlCommand cmd = new SqlCommand(pQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@saleId", printSaleId);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                bool hasHistory = false;
                                while (rdr.Read())
                                {
                                    if (!hasHistory)
                                    {
                                        rowY += 10;
                                        g.DrawLine(pLine, startX, rowY, 750, rowY);
                                        rowY += 10;
                                        g.DrawString("Payment History Logs:", fBold, bDark, startX, rowY);
                                        rowY += 18;
                                        hasHistory = true;
                                    }
                                    DateTime pDate = rdr.GetDateTime(0);
                                    decimal pAmount = rdr.GetDecimal(1);
                                    string pMethod = rdr.GetString(2);
                                    string pRemarks = rdr.IsDBNull(3) ? "" : rdr.GetString(3);

                                    string logLine = pMethod == "Return Offset"
                                        ? $"• {pDate:yyyy-MM-dd HH:mm} - Return Offset Rs. {pAmount:N2} ({pRemarks})"
                                        : $"• {pDate:yyyy-MM-dd HH:mm} - Paid Rs. {pAmount:N2} via {pMethod} ({pRemarks})";
                                    g.DrawString(logLine, fRegular, bDark, startX + 15, rowY);
                                    rowY += 18;
                                }
                            }
                        }
                    }
                }
                catch { }

                rowY += 10;
                g.DrawLine(pLine, startX, rowY, 750, rowY);
                rowY += 15;

                // Footer Message
                g.DrawString("Thank you for shopping at Mero Dokan! Please visit us again.", fBold, bDark, startX + 130, rowY);
            }
        }

        private void GridPurchaseReport_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string reportType = comboPurchaseReportType?.SelectedItem?.ToString() ?? "Invoice Summary";
                if (reportType != "Invoice Summary")
                    return;

                DataGridViewRow row = gridPurchaseReport.Rows[e.RowIndex];
                if (row.Cells["Purchase No"] != null && row.Cells["Purchase No"].Value != null)
                {
                    string purchaseNo = row.Cells["Purchase No"].Value.ToString();
                    string purchaseDate = row.Cells["Purchase Date"]?.Value?.ToString() ?? "";
                    string supplier = row.Cells["Supplier"]?.Value?.ToString() ?? "";

                    ShowPurchaseBreakup(purchaseNo, purchaseDate, supplier);
                }
            }
        }

        private void ShowPurchaseBreakup(string purchaseNo, string purchaseDate, string supplier)
        {
            using (var dlg = new PurchaseBreakupDialog(purchaseNo, purchaseDate, supplier))
            {
                dlg.ShowDialog();
            }
        }

        private class PurchaseBreakupDialog : Form
        {
            private string purchaseNo;
            private string purchaseDate;
            private string supplier;

            private DataGridView gridItems;
            private Button btnClose;

            public PurchaseBreakupDialog(string purchaseNo, string purchaseDate, string supplier)
            {
                this.purchaseNo = purchaseNo;
                this.purchaseDate = purchaseDate;
                this.supplier = supplier;
                InitializeComponent();
                LoadItems();
            }

            private void InitializeComponent()
            {
                this.Text = $"Purchase Items Breakup - {purchaseNo}";
                this.ClientSize = new Size(800, 480);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;
                this.Font = Theme.MainFont;
                this.ForeColor = Theme.TextLight;

                Label lblHeader = new Label();
                lblHeader.Text = $"Purchase Breakup for {purchaseNo}";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                Label lblInfo = new Label();
                lblInfo.Text = $"Date: {purchaseDate}   •   Supplier: {supplier}";
                lblInfo.Location = new Point(20, 45);
                lblInfo.AutoSize = true;
                Theme.StyleLabel(lblInfo, Theme.TextDark, Theme.BoldFont);
                this.Controls.Add(lblInfo);

                gridItems = new DataGridView();
                gridItems.Location = new Point(20, 80);
                gridItems.Size = new Size(760, 320);
                Theme.StyleGrid(gridItems);
                this.Controls.Add(gridItems);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(120, 40);
                btnClose.Location = new Point(660, 420);
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);

                this.CancelButton = btnClose;
            }

            private void LoadItems()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT p.Code as [Product Code], 
                                   p.Name as [Product Name], 
                                   pd.Quantity as [Qty], 
                                   pd.PurchasePrice as [Purchase Price], 
                                   (pd.Quantity * pd.PurchasePrice) as [Total Cost]
                            FROM PurchaseDetails pd
                            INNER JOIN Products p ON pd.ProductId = p.Id
                            INNER JOIN Purchases pur ON pd.PurchaseId = pur.Id
                            WHERE pur.PurchaseNumber = @purchaseNo";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@purchaseNo", purchaseNo);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridItems.DataSource = dt;

                                if (gridItems.Columns["Purchase Price"] != null) gridItems.Columns["Purchase Price"].DefaultCellStyle.Format = "N2";
                                if (gridItems.Columns["Total Cost"] != null) gridItems.Columns["Total Cost"].DefaultCellStyle.Format = "N2";

                                if (gridItems.Columns["Product Code"] != null) gridItems.Columns["Product Code"].FillWeight = 80;
                                if (gridItems.Columns["Product Name"] != null) gridItems.Columns["Product Name"].FillWeight = 180;
                                if (gridItems.Columns["Qty"] != null) gridItems.Columns["Qty"].FillWeight = 50;
                                if (gridItems.Columns["Purchase Price"] != null) gridItems.Columns["Purchase Price"].FillWeight = 80;
                                if (gridItems.Columns["Total Cost"] != null) gridItems.Columns["Total Cost"].FillWeight = 90;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading purchase details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==========================================
        // STAFF / STYLIST COMMISSIONS TAB METHODS
        // ==========================================
        private void InitializeStaffCommissionTab(Panel page)
        {
            // Filters Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboCommDateFilter = new ComboBox();
            comboCommDateFilter.Size = new Size(105, 28);
            comboCommDateFilter.Margin = new Padding(2, 2, 3, 2);
            comboCommDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCommDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboCommDateFilter);
            filterBar.Controls.Add(comboCommDateFilter);

            commFromDate = new DateTimePicker();
            commFromDate.Format = DateTimePickerFormat.Short;
            commFromDate.Size = new Size(95, 28);
            commFromDate.Margin = new Padding(2, 2, 3, 2);
            commFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(commFromDate);

            commToDate = new DateTimePicker();
            commToDate.Format = DateTimePickerFormat.Short;
            commToDate.Size = new Size(95, 28);
            commToDate.Margin = new Padding(2, 2, 3, 2);
            commToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(commToDate);

            Label lblStaffFilter = new Label();
            lblStaffFilter.Text = "Steward / Staff:";
            lblStaffFilter.Margin = new Padding(4, 6, 2, 2);
            lblStaffFilter.AutoSize = true;
            Theme.StyleLabel(lblStaffFilter, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblStaffFilter);

            comboCommStaff = new ComboBox();
            comboCommStaff.Size = new Size(140, 28);
            comboCommStaff.Margin = new Padding(2, 2, 3, 2);
            comboCommStaff.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboCommStaff);
            comboCommStaff.SelectedIndexChanged += (s, e) => LoadStaffCommissions();
            filterBar.Controls.Add(comboCommStaff);

            Label lblTypeFilter = new Label();
            lblTypeFilter.Text = "Category:";
            lblTypeFilter.Margin = new Padding(4, 6, 2, 2);
            lblTypeFilter.AutoSize = true;
            Theme.StyleLabel(lblTypeFilter, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblTypeFilter);

            comboCommItemType = new ComboBox();
            comboCommItemType.Size = new Size(130, 28);
            comboCommItemType.Margin = new Padding(2, 2, 3, 2);
            comboCommItemType.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboCommItemType);
            comboCommItemType.Items.AddRange(new string[] { "All Dishes & Items", "Food Only" });
            comboCommItemType.SelectedIndex = 0;
            comboCommItemType.SelectedIndexChanged += (s, e) => LoadStaffCommissions();
            filterBar.Controls.Add(comboCommItemType);

            btnCommSearch = new Button();
            btnCommSearch.Text = "🔍 Search";
            btnCommSearch.Size = new Size(75, 28);
            btnCommSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnCommSearch);
            btnCommSearch.Click += (s, e) => LoadStaffCommissions();
            filterBar.Controls.Add(btnCommSearch);

            Button btnExportComm = new Button();
            btnExportComm.Text = "📊 Export Excel";
            btnExportComm.Size = new Size(115, 28);
            btnExportComm.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportComm);
            btnExportComm.Click += (s, e) => {
                ExportGridToExcel(gridStaffCommissions, "Steward_Staff_Sales_Summary", "Steward & Staff Cafe Sales Performance Report");
            };
            filterBar.Controls.Add(btnExportComm);

            page.Controls.Add(filterBar);

            // Responsive Layout Table for Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 3;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // 1. Orders Handled Count
            cardCommCount = Theme.CreateCard(270, 65);
            cardCommCount.Dock = DockStyle.Fill;
            cardCommCount.Margin = new Padding(0, 0, 10, 0);
            lblCommCountVal = CreatePLCardContent(cardCommCount, "ORDERS HANDLED", "0 Order(s)", Theme.Accent, out lblCommCountHeader);
            layoutCards.Controls.Add(cardCommCount, 0, 0);

            // 2. Gross Steward Revenue
            cardCommRevenue = Theme.CreateCard(270, 65);
            cardCommRevenue.Dock = DockStyle.Fill;
            cardCommRevenue.Margin = new Padding(10, 0, 10, 0);
            lblCommRevenueVal = CreatePLCardContent(cardCommRevenue, "TOTAL STEWARD SALES", "Rs. 0.00", Theme.TextWhite, out lblCommRevenueHeader);
            layoutCards.Controls.Add(cardCommRevenue, 1, 0);

            // 3. Active Stewards
            cardCommPayable = Theme.CreateCard(270, 65);
            cardCommPayable.Dock = DockStyle.Fill;
            cardCommPayable.Margin = new Padding(10, 0, 0, 0);
            lblCommPayableVal = CreatePLCardContent(cardCommPayable, "ACTIVE STEWARDS", "0 Active", Theme.Success);
            layoutCards.Controls.Add(cardCommPayable, 2, 0);

            // DataGridView for Staff Commissions
            gridStaffCommissions = new DataGridView();
            gridStaffCommissions.Location = new Point(20, 150);
            gridStaffCommissions.Size = new Size(870, 335);
            gridStaffCommissions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridStaffCommissions);
            page.Controls.Add(gridStaffCommissions);

            // Wire up preset change and default to "This Month"
            comboCommDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboCommDateFilter.SelectedItem.ToString(), commFromDate, commToDate, () => LoadStaffCommissions());
            };
            comboCommDateFilter.SelectedIndex = 3; // "This Month"
        }

        // ==========================================
        // TABLE & KOT SALES REPORT TAB METHODS
        // ==========================================
        private void InitializeStylistJobsTab(Panel page)
        {
            // Filters Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboJobsDateFilter = new ComboBox();
            comboJobsDateFilter.Size = new Size(110, 28);
            comboJobsDateFilter.Margin = new Padding(2, 2, 3, 2);
            comboJobsDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboJobsDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboJobsDateFilter);
            filterBar.Controls.Add(comboJobsDateFilter);

            jobsFromDate = new DateTimePicker();
            jobsFromDate.Format = DateTimePickerFormat.Short;
            jobsFromDate.Size = new Size(95, 28);
            jobsFromDate.Margin = new Padding(2, 2, 3, 2);
            jobsFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(jobsFromDate);

            jobsToDate = new DateTimePicker();
            jobsToDate.Format = DateTimePickerFormat.Short;
            jobsToDate.Size = new Size(95, 28);
            jobsToDate.Margin = new Padding(2, 2, 3, 2);
            jobsToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(jobsToDate);

            Label lblStaffFilter = new Label();
            lblStaffFilter.Text = "Steward:";
            lblStaffFilter.Margin = new Padding(4, 6, 2, 2);
            lblStaffFilter.AutoSize = true;
            Theme.StyleLabel(lblStaffFilter, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblStaffFilter);

            comboJobsStaff = new ComboBox();
            comboJobsStaff.Size = new Size(140, 28);
            comboJobsStaff.Margin = new Padding(2, 2, 3, 2);
            comboJobsStaff.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboJobsStaff);
            filterBar.Controls.Add(comboJobsStaff);

            btnJobsSearch = new Button();
            btnJobsSearch.Text = "🔍 Search";
            btnJobsSearch.Size = new Size(80, 28);
            btnJobsSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnJobsSearch);
            btnJobsSearch.Click += (s, e) => LoadStylistJobsReport();
            filterBar.Controls.Add(btnJobsSearch);

            Button btnExportJobs = new Button();
            btnExportJobs.Text = "📊 Export Excel";
            btnExportJobs.Size = new Size(118, 28);
            btnExportJobs.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportJobs);
            btnExportJobs.Click += (s, e) => ExportGridToExcel(gridStylistJobs, "Table_KOT_Sales_Summary", "Table & KOT Cafe Sales Summary Report");
            filterBar.Controls.Add(btnExportJobs);

            page.Controls.Add(filterBar);

            // Responsive Layout Table for Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 3;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // 1. Total Bills / Orders
            cardJobsTotalCount = Theme.CreateCard(270, 65);
            cardJobsTotalCount.Dock = DockStyle.Fill;
            cardJobsTotalCount.Margin = new Padding(0, 0, 10, 0);
            lblJobsTotalCountVal = CreatePLCardContent(cardJobsTotalCount, "TOTAL ORDERS & BILLS", "0 Orders", Theme.Accent);
            layoutCards.Controls.Add(cardJobsTotalCount, 0, 0);

            // 2. Total Revenue Generated
            cardJobsTotalAmount = Theme.CreateCard(270, 65);
            cardJobsTotalAmount.Dock = DockStyle.Fill;
            cardJobsTotalAmount.Margin = new Padding(10, 0, 10, 0);
            lblJobsTotalAmountVal = CreatePLCardContent(cardJobsTotalAmount, "TOTAL SALES REVENUE", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardJobsTotalAmount, 1, 0);

            // 3. Active Stewards / Tables
            cardJobsActiveStylists = Theme.CreateCard(270, 65);
            cardJobsActiveStylists.Dock = DockStyle.Fill;
            cardJobsActiveStylists.Margin = new Padding(10, 0, 0, 0);
            lblJobsActiveStylistsVal = CreatePLCardContent(cardJobsActiveStylists, "ACTIVE STEWARDS", "0 Active", Theme.TextWhite);
            layoutCards.Controls.Add(cardJobsActiveStylists, 2, 0);

            // DataGridView for Table KOT Sales
            gridStylistJobs = new DataGridView();
            gridStylistJobs.Location = new Point(20, 150);
            gridStylistJobs.Size = new Size(870, 335);
            gridStylistJobs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridStylistJobs);
            page.Controls.Add(gridStylistJobs);

            // Wire up preset change and default to "This Month"
            comboJobsDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboJobsDateFilter.SelectedItem.ToString(), jobsFromDate, jobsToDate, () => LoadStylistJobsReport());
            };
            comboJobsDateFilter.SelectedIndex = 3; // "This Month"
        }

        private void LoadStaffFilterDropdown()
        {
            try
            {
                if (comboCommStaff != null)
                {
                    comboCommStaff.Items.Clear();
                    comboCommStaff.Items.Add(new SalesBillingControl.ComboBoxItem { Id = 0, Display = "All Stewards & Staff" });
                }

                if (comboJobsStaff != null)
                {
                    comboJobsStaff.Items.Clear();
                    comboJobsStaff.Items.Add(new SalesBillingControl.ComboBoxItem { Id = 0, Display = "All Stewards & Staff" });
                }

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name, Role FROM Staff ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int sId = Convert.ToInt32(rdr["Id"]);
                                string sDisplay = $"{rdr["Name"]} ({rdr["Role"]})";

                                comboCommStaff?.Items.Add(new SalesBillingControl.ComboBoxItem {
                                    Id = sId,
                                    Display = sDisplay
                                });
                                comboJobsStaff?.Items.Add(new SalesBillingControl.ComboBoxItem {
                                    Id = sId,
                                    Display = sDisplay
                                });
                            }
                        }
                    }
                }
                if (comboCommStaff != null && comboCommStaff.Items.Count > 0) comboCommStaff.SelectedIndex = 0;
                if (comboJobsStaff != null && comboJobsStaff.Items.Count > 0) comboJobsStaff.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadStylistJobsReport()
        {
            try
            {
                if (gridStylistJobs == null) return;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            CONVERT(VARCHAR(10), s.SaleDate, 120) AS [Date],
                            s.InvoiceNumber AS [Invoice #],
                            ISNULL(s.OrderType, 'Dining') AS [Order Type],
                            ISNULL(s.TableNumber, 'T-1') AS [Table #],
                            ISNULL(s.KotNumbers, '-') AS [KOT Numbers],
                            ISNULL(s.StewardName, ISNULL(st.Name, 'Tashi')) AS [Steward / Captain],
                            ISNULL(c.Name, 'Walk-in Guest') AS [Guest Name],
                            s.SubTotal AS [SubTotal (Rs.)],
                            s.Discount AS [Discount (Rs.)],
                            ISNULL(s.Tax, 0.00) AS [GST (Rs.)],
                            s.GrandTotal AS [Bill Total (Rs.)],
                            ISNULL(s.PaymentMethod, 'Cash') AS [Payment Mode]
                        FROM Sales s
                        LEFT JOIN Staff st ON st.Name = s.StewardName
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE CAST(s.SaleDate AS DATE) BETWEEN @from AND @to";

                    if (comboJobsStaff?.SelectedItem is SalesBillingControl.ComboBoxItem selectedStaff && selectedStaff.Id > 0)
                    {
                        query += " AND (s.StewardName = @staffName OR st.Id = @staffId)";
                    }

                    query += " ORDER BY s.SaleDate DESC, s.Id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", jobsFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", jobsToDate.Value.Date);

                        if (comboJobsStaff?.SelectedItem is SalesBillingControl.ComboBoxItem filterStaff && filterStaff.Id > 0)
                        {
                            string sName = filterStaff.Display.Split(' ')[0];
                            cmd.Parameters.AddWithValue("@staffName", sName);
                            cmd.Parameters.AddWithValue("@staffId", filterStaff.Id);
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridStylistJobs.DataSource = dt;

                            if (gridStylistJobs.Columns["SubTotal (Rs.)"] != null) gridStylistJobs.Columns["SubTotal (Rs.)"].DefaultCellStyle.Format = "N2";
                            if (gridStylistJobs.Columns["Discount (Rs.)"] != null) gridStylistJobs.Columns["Discount (Rs.)"].DefaultCellStyle.Format = "N2";
                            if (gridStylistJobs.Columns["GST (Rs.)"] != null) gridStylistJobs.Columns["GST (Rs.)"].DefaultCellStyle.Format = "N2";
                            if (gridStylistJobs.Columns["Bill Total (Rs.)"] != null) gridStylistJobs.Columns["Bill Total (Rs.)"].DefaultCellStyle.Format = "N2";

                            int totalBills = dt.Rows.Count;
                            decimal totalAmount = 0;
                            var uniqueStewards = new System.Collections.Generic.HashSet<string>();

                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Bill Total (Rs.)"] != DBNull.Value)
                                    totalAmount += Convert.ToDecimal(r["Bill Total (Rs.)"]);
                                if (r["Steward / Captain"] != DBNull.Value)
                                    uniqueStewards.Add(r["Steward / Captain"].ToString());
                            }

                            lblJobsTotalCountVal.Text = $"{totalBills} Order(s)";
                            lblJobsTotalAmountVal.Text = $"Rs. {totalAmount:N2}";
                            lblJobsActiveStylistsVal.Text = $"{uniqueStewards.Count} Steward(s)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table & KOT sales report: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadStaffCommissions()
        {
            try
            {
                if (gridStaffCommissions == null) return;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            CONVERT(VARCHAR(10), s.SaleDate, 120) AS [Date],
                            s.InvoiceNumber AS [Invoice #],
                            ISNULL(s.OrderType, 'Dining') AS [Order Type],
                            ISNULL(s.TableNumber, '-') AS [Table #],
                            ISNULL(s.StewardName, ISNULL(st.Name, 'Tashi')) AS [Steward / Staff],
                            ISNULL(p.Name, 'Cafe Dish') AS [Dish / Item Name],
                            ISNULL(p.Category, 'Kitchen') AS [Category],
                            sd.Quantity AS [Qty],
                            sd.UnitPrice AS [Rate (Rs.)],
                            ISNULL(sd.Total, 0.00) AS [Total (Rs.)]
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        LEFT JOIN Products p ON sd.ProductId = p.Id
                        LEFT JOIN Staff st ON (sd.StaffId = st.Id OR st.Name = s.StewardName)
                        WHERE CAST(s.SaleDate AS DATE) BETWEEN @from AND @to";

                    if (comboCommStaff?.SelectedItem is SalesBillingControl.ComboBoxItem selectedStaff && selectedStaff.Id > 0)
                    {
                        string sName = selectedStaff.Display.Split(' ')[0];
                        query += " AND (s.StewardName = @staffName OR st.Id = @staffId)";
                    }

                    query += " ORDER BY s.SaleDate DESC, s.Id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", commFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", commToDate.Value.Date);

                        if (comboCommStaff?.SelectedItem is SalesBillingControl.ComboBoxItem filterStaff && filterStaff.Id > 0)
                        {
                            string sName = filterStaff.Display.Split(' ')[0];
                            cmd.Parameters.AddWithValue("@staffName", sName);
                            cmd.Parameters.AddWithValue("@staffId", filterStaff.Id);
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridStaffCommissions.DataSource = dt;

                            if (gridStaffCommissions.Columns["Rate (Rs.)"] != null) gridStaffCommissions.Columns["Rate (Rs.)"].DefaultCellStyle.Format = "N2";
                            if (gridStaffCommissions.Columns["Total (Rs.)"] != null) gridStaffCommissions.Columns["Total (Rs.)"].DefaultCellStyle.Format = "N2";

                            int totalOrders = dt.Rows.Count;
                            decimal totalRev = 0;
                            var uniqueStewards = new System.Collections.Generic.HashSet<string>();

                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Total (Rs.)"] != DBNull.Value)
                                    totalRev += Convert.ToDecimal(r["Total (Rs.)"]);
                                if (r["Steward / Staff"] != DBNull.Value)
                                    uniqueStewards.Add(r["Steward / Staff"].ToString());
                            }

                            lblCommCountVal.Text = $"{totalOrders} Item(s)";
                            lblCommRevenueVal.Text = $"Rs. {totalRev:N2}";
                            lblCommPayableVal.Text = $"{uniqueStewards.Count} Steward(s)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading steward & staff sales report: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // EXPORT TO EXCEL / CSV SUBSYSTEM
        // ==========================================
        private void ExportGridToExcel(DataGridView grid, string defaultFileName, string reportTitle)
        {
            if (grid == null || grid.Rows.Count == 0)
            {
                MessageBox.Show("There are no records to export in this report.", "Export to Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*";
                sfd.FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                sfd.Title = $"Export {reportTitle} to Excel";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            // Write Report Title & Header
                            sw.WriteLine($"\"{reportTitle.Replace("\"", "\"\"")}\"");
                            sw.WriteLine($"\"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
                            sw.WriteLine();

                            // Collect visible columns
                            var visibleCols = new System.Collections.Generic.List<DataGridViewColumn>();
                            for (int i = 0; i < grid.Columns.Count; i++)
                            {
                                if (grid.Columns[i].Visible && !string.IsNullOrWhiteSpace(grid.Columns[i].HeaderText))
                                {
                                    visibleCols.Add(grid.Columns[i]);
                                }
                            }

                            // Write Headers
                            for (int i = 0; i < visibleCols.Count; i++)
                            {
                                string header = visibleCols[i].HeaderText.Replace("\"", "\"\"");
                                sw.Write($"\"{header}\"");
                                if (i < visibleCols.Count - 1) sw.Write(",");
                            }
                            sw.WriteLine();

                            // Write Data Rows
                            foreach (DataGridViewRow row in grid.Rows)
                            {
                                if (row.IsNewRow) continue;

                                for (int i = 0; i < visibleCols.Count; i++)
                                {
                                    object val = row.Cells[visibleCols[i].Name]?.Value;
                                    string cellText = "";

                                    if (val != null && val != DBNull.Value)
                                    {
                                        if (val is DateTime dt)
                                        {
                                            cellText = dt.ToString("yyyy-MM-dd HH:mm");
                                        }
                                        else if (val is decimal dec)
                                        {
                                            cellText = dec.ToString("F2");
                                        }
                                        else
                                        {
                                            cellText = val.ToString().Replace("\"", "\"\"");
                                        }
                                    }

                                    sw.Write($"\"{cellText}\"");
                                    if (i < visibleCols.Count - 1) sw.Write(",");
                                }
                                sw.WriteLine();
                            }
                        }

                        DialogResult res = MessageBox.Show($"Report exported successfully!\nFile: {sfd.FileName}\n\nDo you want to open it in Excel now?", "Export Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting report to Excel: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportPLExcel()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*";
                sfd.FileName = $"Profit_Loss_Statement_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                sfd.Title = "Export Profit & Loss Statement to Excel";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        DateTime fromDate = plFromDate.Value.Date;
                        DateTime toDate = plToDate.Value.Date;

                        decimal salesRevenue = 0;
                        decimal returnedRefund = 0;
                        decimal grossCogs = 0;
                        decimal resellableReturnCost = 0;
                        decimal totalStaffCommission = 0;

                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();

                            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales WHERE CAST(SaleDate as DATE) BETWEEN @from AND @to", conn))
                            {
                                cmd.Parameters.AddWithValue("@from", fromDate);
                                cmd.Parameters.AddWithValue("@to", toDate);
                                salesRevenue = (decimal)cmd.ExecuteScalar();
                            }

                            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalRefund), 0) FROM SalesReturns WHERE CAST(ReturnDate as DATE) BETWEEN @from AND @to", conn))
                            {
                                cmd.Parameters.AddWithValue("@from", fromDate);
                                cmd.Parameters.AddWithValue("@to", toDate);
                                returnedRefund = (decimal)cmd.ExecuteScalar();
                            }

                            string grossCogsQuery = @"
                                SELECT ISNULL(SUM(sd.Quantity * sd.PurchaseCostAtSale), 0)
                                FROM SaleDetails sd
                                INNER JOIN Sales s ON sd.SaleId = s.Id
                                WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to AND sd.ItemType = 'Product'";

                            using (SqlCommand cmd = new SqlCommand(grossCogsQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@from", fromDate);
                                cmd.Parameters.AddWithValue("@to", toDate);
                                grossCogs = (decimal)cmd.ExecuteScalar();
                            }

                            string returnCostQuery = @"
                                SELECT ISNULL(SUM(srd.Quantity * sd.PurchaseCostAtSale), 0)
                                FROM SalesReturnDetails srd
                                INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                                INNER JOIN SaleDetails sd ON sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId
                                WHERE srd.ItemCondition = 'Resellable' 
                                  AND CAST(sr.ReturnDate as DATE) BETWEEN @from AND @to";

                            using (SqlCommand cmd = new SqlCommand(returnCostQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@from", fromDate);
                                cmd.Parameters.AddWithValue("@to", toDate);
                                resellableReturnCost = (decimal)cmd.ExecuteScalar();
                            }

                            string commQuery = @"
                                SELECT ISNULL(SUM(sd.Total * (ISNULL(st.CommissionRate, 10.0) / 100.0)), 0)
                                FROM SaleDetails sd
                                INNER JOIN Sales s ON sd.SaleId = s.Id
                                INNER JOIN Staff st ON sd.StaffId = st.Id
                                WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                                  AND sd.ItemType = 'Service'";

                            using (SqlCommand cmd = new SqlCommand(commQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@from", fromDate);
                                cmd.Parameters.AddWithValue("@to", toDate);
                                totalStaffCommission = (decimal)cmd.ExecuteScalar();
                            }
                        }

                        decimal netRevenue = salesRevenue - returnedRefund;
                        decimal cogs = Math.Max(0, grossCogs - resellableReturnCost);
                        decimal directCosts = cogs + totalStaffCommission;
                        decimal netPerformance = netRevenue - directCosts;
                        decimal marginPercent = netRevenue > 0 ? (netPerformance / netRevenue) * 100 : 0;

                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("\"The Local Cafe Management - Profit & Loss Statement\"");
                            sw.WriteLine($"\"Statement Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}\"");
                            sw.WriteLine($"\"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
                            sw.WriteLine();

                            sw.WriteLine("\"Section\",\"Line Item\",\"Amount (Rs.)\",\"Notes\"");
                            sw.WriteLine($"\"1. Revenue\",\"Gross Customer Sales & Services\",\"{salesRevenue:F2}\",\"Total billing before returns\"");
                            sw.WriteLine($"\"1. Revenue\",\"Less: Returns & Refunds\",\"{returnedRefund:F2}\",\"Client sales returns\"");
                            sw.WriteLine($"\"1. Revenue\",\"NET REVENUE\",\"{netRevenue:F2}\",\"Gross realized revenue\"");
                            sw.WriteLine();
                            sw.WriteLine($"\"2. Operating Costs\",\"Cost of Goods Sold (Retail COGS)\",\"{cogs:F2}\",\"Inventory wholesale acquisition cost\"");
                            sw.WriteLine($"\"2. Operating Costs\",\"Stylist Service Commissions\",\"{totalStaffCommission:F2}\",\"Direct specialist payouts\"");
                            sw.WriteLine($"\"2. Operating Costs\",\"TOTAL DIRECT OPERATING COSTS\",\"{directCosts:F2}\",\"COGS + Stylist commissions\"");
                            sw.WriteLine();
                            sw.WriteLine($"\"3. Net Margin\",\"OPERATING NET PROFIT / MARGIN\",\"{netPerformance:F2}\",\"Net realized earnings\"");
                            sw.WriteLine($"\"3. Net Margin\",\"Net Profit Margin Ratio\",\"{marginPercent:F2}%\",\"Net percentage of revenue\"");
                        }

                        DialogResult res = MessageBox.Show($"Profit & Loss report exported successfully!\nFile: {sfd.FileName}\n\nDo you want to open it in Excel now?", "Export Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting Profit & Loss report to Excel: {ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void InitializeCollectionSummaryTab(Panel page)
        {
            // Filters Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 10);
            filterBar.Size = new Size(870, 52);
            filterBar.Height = 52;
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Color.FromArgb(17, 24, 39);
            filterBar.Padding = new Padding(6, 10, 6, 8);
            filterBar.WrapContents = false;
            filterBar.AutoScroll = false;

            Label lblRange = new Label();
            lblRange.Text = "Date Range:";
            lblRange.Margin = new Padding(2, 6, 2, 2);
            lblRange.AutoSize = true;
            Theme.StyleLabel(lblRange, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblRange);

            comboCollDateFilter = new ComboBox();
            comboCollDateFilter.Size = new Size(130, 28);
            comboCollDateFilter.Margin = new Padding(2, 2, 4, 2);
            comboCollDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCollDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Last Month", "This Year", "All Time", "Custom Range" });
            Theme.StyleComboBox(comboCollDateFilter);
            filterBar.Controls.Add(comboCollDateFilter);

            collFromDate = new DateTimePicker();
            collFromDate.Format = DateTimePickerFormat.Short;
            collFromDate.Size = new Size(95, 28);
            collFromDate.Margin = new Padding(2, 2, 4, 2);
            collFromDate.Font = Theme.MainFont;
            filterBar.Controls.Add(collFromDate);

            collToDate = new DateTimePicker();
            collToDate.Format = DateTimePickerFormat.Short;
            collToDate.Size = new Size(95, 28);
            collToDate.Margin = new Padding(2, 2, 4, 2);
            collToDate.Font = Theme.MainFont;
            filterBar.Controls.Add(collToDate);

            btnCollSearch = new Button();
            btnCollSearch.Text = "🔍 Search";
            btnCollSearch.Size = new Size(90, 28);
            btnCollSearch.Margin = new Padding(4, 2, 4, 2);
            Theme.StylePrimaryButton(btnCollSearch);
            btnCollSearch.Click += (s, e) => LoadCollectionSummary();
            filterBar.Controls.Add(btnCollSearch);

            Button btnExportColl = new Button();
            btnExportColl.Text = "📊 Export Excel";
            btnExportColl.Size = new Size(125, 28);
            btnExportColl.Margin = new Padding(4, 2, 4, 2);
            Theme.StyleSuccessButton(btnExportColl);
            btnExportColl.Click += (s, e) => ExportGridToExcel(gridCollectionSummary, "Payment_Collection_Summary", "Daily Payment Collection Summary Report");
            filterBar.Controls.Add(btnExportColl);

            page.Controls.Add(filterBar);

            // 4 Top KPI Metric Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 4;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            Panel cardInv = Theme.CreateCard(180, 65);
            cardInv.Dock = DockStyle.Fill;
            cardInv.Margin = new Padding(0, 0, 8, 0);
            lblCollInvoiceCountVal = CreatePLCardContent(cardInv, "INVOICES ISSUED", "0 Invoices", Theme.TextWhite);
            layoutCards.Controls.Add(cardInv, 0, 0);

            Panel cardCash = Theme.CreateCard(220, 65);
            cardCash.Dock = DockStyle.Fill;
            cardCash.Margin = new Padding(8, 0, 8, 0);
            lblCollCashVal = CreatePLCardContent(cardCash, "💵 TOTAL CASH COLLECTED", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardCash, 1, 0);

            Panel cardOnline = Theme.CreateCard(220, 65);
            cardOnline.Dock = DockStyle.Fill;
            cardOnline.Margin = new Padding(8, 0, 8, 0);
            lblCollOnlineVal = CreatePLCardContent(cardOnline, "📱 ONLINE PAYMENT (QR/CARD)", "Rs. 0.00", Color.FromArgb(56, 189, 248));
            layoutCards.Controls.Add(cardOnline, 2, 0);

            Panel cardTotal = Theme.CreateCard(220, 65);
            cardTotal.Dock = DockStyle.Fill;
            cardTotal.Margin = new Padding(8, 0, 0, 0);
            lblCollTotalVal = CreatePLCardContent(cardTotal, "💎 TOTAL WITH TAX", "Rs. 0.00", Theme.Accent);
            layoutCards.Controls.Add(cardTotal, 3, 0);

            // GridView
            gridCollectionSummary = new DataGridView();
            gridCollectionSummary.Location = new Point(20, 150);
            gridCollectionSummary.Size = new Size(870, 285);
            gridCollectionSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCollectionSummary);
            page.Controls.Add(gridCollectionSummary);

            // Summary Footer Card Bar
            Panel summaryBar = Theme.CreateCard(870, 48);
            summaryBar.Location = new Point(20, 445);
            summaryBar.Size = new Size(870, 48);
            summaryBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            summaryBar.BackColor = Color.FromArgb(17, 24, 39);
            summaryBar.Padding = new Padding(15, 0, 15, 0);

            lblCollSummary = new Label();
            lblCollSummary.Text = "Days: 0  •  Invoices: 0  •  Cash: Rs. 0.00  •  Online: Rs. 0.00  •  Total: Rs. 0.00";
            lblCollSummary.Dock = DockStyle.Fill;
            lblCollSummary.Padding = new Padding(0);
            lblCollSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblCollSummary, Theme.TextLight, new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold));
            summaryBar.Controls.Add(lblCollSummary);
            page.Controls.Add(summaryBar);

            comboCollDateFilter.SelectedIndexChanged += (s, e) => {
                ApplyDateRangePreset(comboCollDateFilter.SelectedItem.ToString(), collFromDate, collToDate, () => LoadCollectionSummary());
            };
            comboCollDateFilter.SelectedIndex = 3; // Default "This Month"
        }

        private void LoadCollectionSummary()
        {
            try
            {
                if (gridCollectionSummary == null) return;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            CONVERT(VARCHAR(10), s.SaleDate, 120) AS [Date],
                            COUNT(s.Id) AS [No of Invoice Generated],
                            ISNULL(SUM(CASE 
                                WHEN s.PaymentMethod = 'Cash' THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                WHEN s.PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(s.CashAmount, 0) > s.GrandTotal THEN s.GrandTotal ELSE ISNULL(s.CashAmount, 0) END)
                                ELSE 0.00
                            END), 0) AS [Total Cash Collected],
                            ISNULL(SUM(CASE 
                                WHEN ISNULL(s.OnlineAmount, 0) > 0 THEN (CASE WHEN s.OnlineAmount > s.GrandTotal THEN s.GrandTotal ELSE s.OnlineAmount END)
                                WHEN s.PaymentMethod IN ('Card', 'QR Pay', 'UPI', 'Wallet', 'Online', 'QR Pay / UPI', 'UPI / QR Pay', 'Card / POS') 
                                     OR s.PaymentMethod LIKE '%UPI%' OR s.PaymentMethod LIKE '%QR%' OR s.PaymentMethod LIKE '%Card%' OR s.PaymentMethod LIKE '%Online%'
                                THEN (CASE WHEN s.AmountPaid > s.GrandTotal THEN s.GrandTotal ELSE s.AmountPaid END)
                                ELSE 0.00
                            END), 0) AS [Online Payment],
                            ISNULL(SUM(ISNULL(s.TaxableAmount, s.SubTotal - ISNULL(s.Discount, 0))), 0) AS [Total Without Tax],
                            ISNULL(SUM(ISNULL(s.Tax, ISNULL(s.CGSTAmount, 0) + ISNULL(s.SGSTAmount, 0) + ISNULL(s.IGSTAmount, 0))), 0) AS [Tax Collected],
                            ISNULL(SUM(s.GrandTotal), 0) AS [Total With Tax]
                        FROM Sales s
                        WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                        GROUP BY CONVERT(VARCHAR(10), s.SaleDate, 120)
                        ORDER BY [Date] DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", collFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", collToDate.Value.Date);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridCollectionSummary.DataSource = dt;

                            if (gridCollectionSummary.Columns["Total Cash Collected"] != null)
                                gridCollectionSummary.Columns["Total Cash Collected"].DefaultCellStyle.Format = "N2";
                            if (gridCollectionSummary.Columns["Online Payment"] != null)
                                gridCollectionSummary.Columns["Online Payment"].DefaultCellStyle.Format = "N2";
                            if (gridCollectionSummary.Columns["Total Without Tax"] != null)
                                gridCollectionSummary.Columns["Total Without Tax"].DefaultCellStyle.Format = "N2";
                            if (gridCollectionSummary.Columns["Tax Collected"] != null)
                                gridCollectionSummary.Columns["Tax Collected"].DefaultCellStyle.Format = "N2";
                            if (gridCollectionSummary.Columns["Total With Tax"] != null)
                                gridCollectionSummary.Columns["Total With Tax"].DefaultCellStyle.Format = "N2";

                            if (gridCollectionSummary.Columns["Date"] != null) gridCollectionSummary.Columns["Date"].FillWeight = 85;
                            if (gridCollectionSummary.Columns["No of Invoice Generated"] != null)
                            {
                                gridCollectionSummary.Columns["No of Invoice Generated"].FillWeight = 95;
                                gridCollectionSummary.Columns["No of Invoice Generated"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            }
                            if (gridCollectionSummary.Columns["Total Cash Collected"] != null)
                            {
                                gridCollectionSummary.Columns["Total Cash Collected"].FillWeight = 110;
                                gridCollectionSummary.Columns["Total Cash Collected"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            }
                            if (gridCollectionSummary.Columns["Online Payment"] != null)
                            {
                                gridCollectionSummary.Columns["Online Payment"].FillWeight = 110;
                                gridCollectionSummary.Columns["Online Payment"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            }
                            if (gridCollectionSummary.Columns["Total Without Tax"] != null)
                            {
                                gridCollectionSummary.Columns["Total Without Tax"].FillWeight = 110;
                                gridCollectionSummary.Columns["Total Without Tax"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            }
                            if (gridCollectionSummary.Columns["Tax Collected"] != null)
                            {
                                gridCollectionSummary.Columns["Tax Collected"].FillWeight = 95;
                                gridCollectionSummary.Columns["Tax Collected"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            }
                            if (gridCollectionSummary.Columns["Total With Tax"] != null)
                            {
                                gridCollectionSummary.Columns["Total With Tax"].FillWeight = 115;
                                gridCollectionSummary.Columns["Total With Tax"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                            }

                            long totalInvoices = 0;
                            decimal totalCash = 0;
                            decimal totalOnline = 0;
                            decimal totalWithoutTax = 0;
                            decimal totalTax = 0;
                            decimal totalGrand = 0;

                            foreach (DataRow r in dt.Rows)
                            {
                                totalInvoices += Convert.ToInt64(r["No of Invoice Generated"]);
                                totalCash += Convert.ToDecimal(r["Total Cash Collected"]);
                                totalOnline += Convert.ToDecimal(r["Online Payment"]);
                                totalWithoutTax += Convert.ToDecimal(r["Total Without Tax"]);
                                totalTax += Convert.ToDecimal(r["Tax Collected"]);
                                totalGrand += Convert.ToDecimal(r["Total With Tax"]);
                            }

                            if (lblCollInvoiceCountVal != null) lblCollInvoiceCountVal.Text = $"{totalInvoices} Invoices";
                            if (lblCollCashVal != null) lblCollCashVal.Text = $"Rs. {totalCash:N2}";
                            if (lblCollOnlineVal != null) lblCollOnlineVal.Text = $"Rs. {totalOnline:N2}";
                            if (lblCollTotalVal != null) lblCollTotalVal.Text = $"Rs. {totalGrand:N2}";

                            if (lblCollSummary != null)
                            {
                                lblCollSummary.Text = $"Days: {dt.Rows.Count}  •  Invoices: {totalInvoices}  •  💵 Cash: Rs. {totalCash:N2}  •  📱 Online: Rs. {totalOnline:N2}  •  💰 Excl. Tax: Rs. {totalWithoutTax:N2}  •  🧾 Tax: Rs. {totalTax:N2}  •  💎 Total With Tax: Rs. {totalGrand:N2}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading collection summary: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeStockRegisterTab(Panel page)
        {
            // 1. DataGridView for Stock Register (Instantiate first for null safety)
            gridStockRegister = new DataGridView();
            gridStockRegister.Location = new Point(20, 150);
            gridStockRegister.Size = new Size(870, 335);
            gridStockRegister.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridStockRegister);
            gridStockRegister.CellFormatting += GridStockRegister_CellFormatting;

            // 2. Filter Bar Panel
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 20);
            filterBar.Size = new Size(870, 36);
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.FlowDirection = FlowDirection.LeftToRight;
            filterBar.WrapContents = false;
            filterBar.BackColor = Color.Transparent;

            // Search
            Panel pnlSearch = new Panel();
            pnlSearch.Size = new Size(180, 28);
            pnlSearch.Margin = new Padding(2, 2, 4, 2);
            pnlSearch.BackColor = Theme.Primary;
            pnlSearch.BorderStyle = BorderStyle.FixedSingle;
            pnlSearch.Padding = new Padding(4, 4, 4, 2);

            txtStockSearch = new TextBox();
            txtStockSearch.Dock = DockStyle.Fill;
            txtStockSearch.BorderStyle = BorderStyle.None;
            txtStockSearch.BackColor = Theme.Primary;
            txtStockSearch.ForeColor = Theme.TextWhite;
            txtStockSearch.Font = Theme.MainFont;
            txtStockSearch.TextChanged += (s, e) => LoadStockRegisterReport();
            pnlSearch.Controls.Add(txtStockSearch);
            filterBar.Controls.Add(pnlSearch);

            // Category Filter
            Label lblCat = new Label();
            lblCat.Text = "Category:";
            lblCat.Margin = new Padding(4, 6, 2, 2);
            lblCat.AutoSize = true;
            Theme.StyleLabel(lblCat, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblCat);

            comboStockCategory = new ComboBox();
            comboStockCategory.Size = new Size(130, 28);
            comboStockCategory.Margin = new Padding(2, 2, 4, 2);
            comboStockCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboStockCategory);
            comboStockCategory.SelectedIndexChanged += (s, e) => LoadStockRegisterReport();
            filterBar.Controls.Add(comboStockCategory);

            // Status Filter
            Label lblStatus = new Label();
            lblStatus.Text = "Stock Status:";
            lblStatus.Margin = new Padding(4, 6, 2, 2);
            lblStatus.AutoSize = true;
            Theme.StyleLabel(lblStatus, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblStatus);

            comboStockStatus = new ComboBox();
            comboStockStatus.Size = new Size(130, 28);
            comboStockStatus.Margin = new Padding(2, 2, 4, 2);
            comboStockStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            comboStockStatus.Items.AddRange(new string[] { "All Items", "In Stock (Healthy)", "Low Stock Alert (<= Min)", "Out of Stock (0)" });
            Theme.StyleComboBox(comboStockStatus);
            if (comboStockStatus.Items.Count > 0) comboStockStatus.SelectedIndex = 0;
            comboStockStatus.SelectedIndexChanged += (s, e) => LoadStockRegisterReport();
            filterBar.Controls.Add(comboStockStatus);

            // Search Button
            btnStockSearch = new Button();
            btnStockSearch.Text = "🔍 Search";
            btnStockSearch.Size = new Size(80, 28);
            btnStockSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnStockSearch);
            btnStockSearch.Click += (s, e) => LoadStockRegisterReport();
            filterBar.Controls.Add(btnStockSearch);

            // Export Button
            Button btnExportStock = new Button();
            btnExportStock.Text = "📊 Export Excel";
            btnExportStock.Size = new Size(118, 28);
            btnExportStock.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportStock);
            btnExportStock.Click += (s, e) => ExportGridToExcel(gridStockRegister, "Stock_Register_Inventory", "Stock Register & Inventory Valuation Report");
            filterBar.Controls.Add(btnExportStock);

            page.Controls.Add(filterBar);

            // 3. Top KPI Metric Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 4;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // 1. Total Stock Units
            cardStockTotalUnits = Theme.CreateCard(200, 65);
            cardStockTotalUnits.Dock = DockStyle.Fill;
            cardStockTotalUnits.Margin = new Padding(0, 0, 8, 0);
            lblStockTotalUnitsVal = CreatePLCardContent(cardStockTotalUnits, "TOTAL STOCK QUANTITY", "0 Units", Theme.Accent);
            layoutCards.Controls.Add(cardStockTotalUnits, 0, 0);

            // 2. Inventory Value (Cost)
            cardStockCostValue = Theme.CreateCard(200, 65);
            cardStockCostValue.Dock = DockStyle.Fill;
            cardStockCostValue.Margin = new Padding(8, 0, 8, 0);
            lblStockCostValueVal = CreatePLCardContent(cardStockCostValue, "INVENTORY VALUE (COST)", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardStockCostValue, 1, 0);

            // 3. Potential Retail Value
            cardStockRetailValue = Theme.CreateCard(200, 65);
            cardStockRetailValue.Dock = DockStyle.Fill;
            cardStockRetailValue.Margin = new Padding(8, 0, 8, 0);
            lblStockRetailValueVal = CreatePLCardContent(cardStockRetailValue, "RETAIL VALUE (SALES)", "Rs. 0.00", Color.FromArgb(56, 189, 248));
            layoutCards.Controls.Add(cardStockRetailValue, 2, 0);

            // 4. Low / Out of Stock
            cardStockLowAlerts = Theme.CreateCard(200, 65);
            cardStockLowAlerts.Dock = DockStyle.Fill;
            cardStockLowAlerts.Margin = new Padding(8, 0, 0, 0);
            lblStockLowAlertsVal = CreatePLCardContent(cardStockLowAlerts, "LOW / OUT OF STOCK", "0 Items", Color.FromArgb(244, 63, 94));
            layoutCards.Controls.Add(cardStockLowAlerts, 3, 0);

            page.Controls.Add(gridStockRegister);
        }

        private void LoadStockCategories()
        {
            try
            {
                if (comboStockCategory == null) return;
                comboStockCategory.Items.Clear();
                comboStockCategory.Items.Add("All Categories");

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND Category != '' ORDER BY Category ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboStockCategory.Items.Add(rdr["Category"].ToString());
                            }
                        }
                    }
                }
                if (comboStockCategory.Items.Count > 0) comboStockCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadStockRegisterReport()
        {
            if (gridStockRegister == null) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Id,
                            Code AS [Item Code],
                            Name AS [Product Name],
                            Category,
                            ISNULL(HSNCode, '') AS [HSN Code],
                            PurchasePrice AS [Cost Price],
                            SalesPrice AS [Selling Price],
                            Stock AS [Current Stock (Qty)],
                            MinStockLevel AS [Min Level],
                            CAST((Stock * PurchasePrice) AS DECIMAL(18,2)) AS [Total Cost Value],
                            CAST((Stock * SalesPrice) AS DECIMAL(18,2)) AS [Total Retail Value],
                            CASE 
                                WHEN Stock <= 0 THEN '🔴 Out of Stock'
                                WHEN Stock <= MinStockLevel THEN '🟡 Low Stock Alert'
                                ELSE '🟢 In Stock'
                            END AS [Stock Status]
                        FROM Products
                        WHERE 1=1";

                    string search = txtStockSearch?.Text.Trim() ?? "";
                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " AND (Code LIKE @search OR Name LIKE @search OR Category LIKE @search OR HSNCode LIKE @search)";
                    }

                    string selectedCategory = comboStockCategory?.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All Categories")
                    {
                        query += " AND Category = @category";
                    }

                    string selectedStatus = comboStockStatus?.SelectedItem?.ToString();
                    if (selectedStatus == "In Stock (Healthy)")
                    {
                        query += " AND Stock > MinStockLevel";
                    }
                    else if (selectedStatus == "Low Stock Alert (<= Min)")
                    {
                        query += " AND Stock > 0 AND Stock <= MinStockLevel";
                    }
                    else if (selectedStatus == "Out of Stock (0)")
                    {
                        query += " AND Stock <= 0";
                    }

                    query += " ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All Categories") cmd.Parameters.AddWithValue("@category", selectedCategory);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridStockRegister.DataSource = dt;

                            if (gridStockRegister.Columns["Id"] != null) gridStockRegister.Columns["Id"].Visible = false;

                            // Formats
                            if (gridStockRegister.Columns["Cost Price"] != null) gridStockRegister.Columns["Cost Price"].DefaultCellStyle.Format = "N2";
                            if (gridStockRegister.Columns["Selling Price"] != null) gridStockRegister.Columns["Selling Price"].DefaultCellStyle.Format = "N2";
                            if (gridStockRegister.Columns["Total Cost Value"] != null) gridStockRegister.Columns["Total Cost Value"].DefaultCellStyle.Format = "N2";
                            if (gridStockRegister.Columns["Total Retail Value"] != null) gridStockRegister.Columns["Total Retail Value"].DefaultCellStyle.Format = "N2";

                            if (gridStockRegister.Columns["Current Stock (Qty)"] != null)
                            {
                                gridStockRegister.Columns["Current Stock (Qty)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                gridStockRegister.Columns["Current Stock (Qty)"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                            }
                            if (gridStockRegister.Columns["Min Level"] != null)
                            {
                                gridStockRegister.Columns["Min Level"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            }
                            if (gridStockRegister.Columns["Stock Status"] != null)
                            {
                                gridStockRegister.Columns["Stock Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                                gridStockRegister.Columns["Stock Status"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
                            }

                            // Calculate totals for KPI cards
                            decimal totalUnits = 0;
                            decimal totalCostVal = 0;
                            decimal totalRetailVal = 0;
                            int lowAlertCount = 0;

                            foreach (DataRow r in dt.Rows)
                            {
                                decimal st = r["Current Stock (Qty)"] != DBNull.Value ? Convert.ToDecimal(r["Current Stock (Qty)"]) : 0;
                                decimal min = r["Min Level"] != DBNull.Value ? Convert.ToDecimal(r["Min Level"]) : 0;
                                decimal cost = r["Total Cost Value"] != DBNull.Value ? Convert.ToDecimal(r["Total Cost Value"]) : 0;
                                decimal ret = r["Total Retail Value"] != DBNull.Value ? Convert.ToDecimal(r["Total Retail Value"]) : 0;

                                totalUnits += st;
                                totalCostVal += cost;
                                totalRetailVal += ret;

                                if (st <= min)
                                {
                                    lowAlertCount++;
                                }
                            }

                            if (lblStockTotalUnitsVal != null) lblStockTotalUnitsVal.Text = $"{totalUnits:N0} Units";
                            if (lblStockCostValueVal != null) lblStockCostValueVal.Text = $"Rs. {totalCostVal:N2}";
                            if (lblStockRetailValueVal != null) lblStockRetailValueVal.Text = $"Rs. {totalRetailVal:N2}";
                            if (lblStockLowAlertsVal != null) lblStockLowAlertsVal.Text = $"{lowAlertCount} Products";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock register: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridStockRegister_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || gridStockRegister.Rows[e.RowIndex].DataBoundItem == null) return;

            try
            {
                DataRowView rowView = gridStockRegister.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (rowView != null)
                {
                    decimal stock = rowView["Current Stock (Qty)"] != DBNull.Value ? Convert.ToDecimal(rowView["Current Stock (Qty)"]) : 0;
                    decimal min = rowView["Min Level"] != DBNull.Value ? Convert.ToDecimal(rowView["Min Level"]) : 0;

                    if (stock <= 0)
                    {
                        gridStockRegister.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(248, 113, 113); // Soft red
                    }
                    else if (stock <= min)
                    {
                        gridStockRegister.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(251, 191, 36); // Amber
                    }
                }
            }
            catch { }
        }

        private void InitializeRawMaterialUsageTab(Panel page)
        {
            // 1. Grid
            gridRawMaterialReport = new DataGridView();
            gridRawMaterialReport.Location = new Point(20, 150);
            gridRawMaterialReport.Size = new Size(870, 340);
            gridRawMaterialReport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridRawMaterialReport);
            gridRawMaterialReport.CellFormatting += GridRawMaterialReport_CellFormatting;

            // 2. Filter Bar
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Location = new Point(20, 15);
            filterBar.Size = new Size(870, 48);
            filterBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            filterBar.BackColor = Theme.Primary;
            filterBar.Padding = new Padding(8, 8, 8, 8);
            filterBar.WrapContents = false;

            // Date Preset
            comboRawDateFilter = new ComboBox();
            comboRawDateFilter.Size = new Size(120, 28);
            comboRawDateFilter.Margin = new Padding(2, 2, 4, 2);
            comboRawDateFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRawDateFilter.Items.AddRange(new string[] { "Today", "Yesterday", "This Week", "This Month", "Custom Range" });
            comboRawDateFilter.SelectedIndex = 0; // Default to Today
            Theme.StyleComboBox(comboRawDateFilter);
            comboRawDateFilter.SelectedIndexChanged += ComboRawDateFilter_SelectedIndexChanged;
            filterBar.Controls.Add(comboRawDateFilter);

            // From Date
            Label lblFrom = new Label();
            lblFrom.Text = "From:";
            lblFrom.Margin = new Padding(4, 6, 2, 2);
            lblFrom.AutoSize = true;
            Theme.StyleLabel(lblFrom, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblFrom);

            dtpRawFromDate = new DateTimePicker();
            dtpRawFromDate.Size = new Size(110, 28);
            dtpRawFromDate.Margin = new Padding(2, 2, 4, 2);
            dtpRawFromDate.Format = DateTimePickerFormat.Short;
            dtpRawFromDate.Value = DateTime.Today;
            dtpRawFromDate.Font = Theme.MainFont;
            dtpRawFromDate.ValueChanged += (s, e) => LoadRawMaterialUsageReport();
            filterBar.Controls.Add(dtpRawFromDate);

            // To Date
            Label lblTo = new Label();
            lblTo.Text = "To:";
            lblTo.Margin = new Padding(4, 6, 2, 2);
            lblTo.AutoSize = true;
            Theme.StyleLabel(lblTo, Theme.TextDark, Theme.BoldFont);
            filterBar.Controls.Add(lblTo);

            dtpRawToDate = new DateTimePicker();
            dtpRawToDate.Size = new Size(110, 28);
            dtpRawToDate.Margin = new Padding(2, 2, 4, 2);
            dtpRawToDate.Format = DateTimePickerFormat.Short;
            dtpRawToDate.Value = DateTime.Today;
            dtpRawToDate.Font = Theme.MainFont;
            dtpRawToDate.ValueChanged += (s, e) => LoadRawMaterialUsageReport();
            filterBar.Controls.Add(dtpRawToDate);

            // Category Filter
            comboRawCategory = new ComboBox();
            comboRawCategory.Size = new Size(140, 28);
            comboRawCategory.Margin = new Padding(2, 2, 4, 2);
            comboRawCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboRawCategory);
            comboRawCategory.SelectedIndexChanged += (s, e) => LoadRawMaterialUsageReport();
            filterBar.Controls.Add(comboRawCategory);

            // Search Box
            txtRawSearch = new TextBox();
            txtRawSearch.Size = new Size(130, 28);
            txtRawSearch.Margin = new Padding(2, 2, 4, 2);
            txtRawSearch.Font = Theme.MainFont;
            Theme.StyleTextBox(txtRawSearch);
            txtRawSearch.TextChanged += (s, e) => LoadRawMaterialUsageReport();
            filterBar.Controls.Add(txtRawSearch);

            // Search Button
            btnRawSearch = new Button();
            btnRawSearch.Text = "🔍 Search";
            btnRawSearch.Size = new Size(80, 28);
            btnRawSearch.Margin = new Padding(2, 2, 3, 2);
            Theme.StylePrimaryButton(btnRawSearch);
            btnRawSearch.Click += (s, e) => LoadRawMaterialUsageReport();
            filterBar.Controls.Add(btnRawSearch);

            // Export Button
            Button btnExportRaw = new Button();
            btnExportRaw.Text = "📊 Export Excel";
            btnExportRaw.Size = new Size(118, 28);
            btnExportRaw.Margin = new Padding(2, 2, 3, 2);
            Theme.StyleSuccessButton(btnExportRaw);
            btnExportRaw.Click += (s, e) => ExportGridToExcel(gridRawMaterialReport, "RawMaterial_Daily_Usage_Report", "Daily Raw Material Usage & Stock Report");
            filterBar.Controls.Add(btnExportRaw);

            page.Controls.Add(filterBar);

            // 3. Top KPI Metric Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 68);
            layoutCards.Size = new Size(870, 75);
            layoutCards.ColumnCount = 4;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // Card 1: Today's Consumption Cost
            cardRawUsageCost = Theme.CreateCard(200, 65);
            cardRawUsageCost.Dock = DockStyle.Fill;
            cardRawUsageCost.Margin = new Padding(0, 0, 8, 0);
            lblRawUsageCostVal = CreatePLCardContent(cardRawUsageCost, "KITCHEN USAGE COST", "Rs. 0.00", Theme.Danger);
            layoutCards.Controls.Add(cardRawUsageCost, 0, 0);

            // Card 2: Today's Inward Purchases
            cardRawInwardCost = Theme.CreateCard(200, 65);
            cardRawInwardCost.Dock = DockStyle.Fill;
            cardRawInwardCost.Margin = new Padding(8, 0, 8, 0);
            lblRawInwardCostVal = CreatePLCardContent(cardRawInwardCost, "INWARD PURCHASES", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardRawInwardCost, 1, 0);

            // Card 3: In-Hand Stock Asset Value
            cardRawStockAsset = Theme.CreateCard(200, 65);
            cardRawStockAsset.Dock = DockStyle.Fill;
            cardRawStockAsset.Margin = new Padding(8, 0, 8, 0);
            lblRawStockAssetVal = CreatePLCardContent(cardRawStockAsset, "TOTAL STOCK VALUATION", "Rs. 0.00", Theme.Accent);
            layoutCards.Controls.Add(cardRawStockAsset, 2, 0);

            // Card 4: Low Stock Alerts
            cardRawLowStock = Theme.CreateCard(200, 65);
            cardRawLowStock.Dock = DockStyle.Fill;
            cardRawLowStock.Margin = new Padding(8, 0, 0, 0);
            lblRawLowStockVal = CreatePLCardContent(cardRawLowStock, "LOW / CRITICAL STOCK", "0 Items", Color.FromArgb(244, 63, 94));
            layoutCards.Controls.Add(cardRawLowStock, 3, 0);

            page.Controls.Add(gridRawMaterialReport);
        }

        private void ComboRawDateFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboRawDateFilter == null || dtpRawFromDate == null || dtpRawToDate == null) return;

            string sel = comboRawDateFilter.SelectedItem?.ToString() ?? "Today";
            DateTime today = DateTime.Today;

            switch (sel)
            {
                case "Today":
                    dtpRawFromDate.Value = today;
                    dtpRawToDate.Value = today;
                    break;
                case "Yesterday":
                    dtpRawFromDate.Value = today.AddDays(-1);
                    dtpRawToDate.Value = today.AddDays(-1);
                    break;
                case "This Week":
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    dtpRawFromDate.Value = today.AddDays(-1 * diff);
                    dtpRawToDate.Value = today;
                    break;
                case "This Month":
                    dtpRawFromDate.Value = new DateTime(today.Year, today.Month, 1);
                    dtpRawToDate.Value = today;
                    break;
            }
            LoadRawMaterialUsageReport();
        }

        private void LoadRawMaterialUsageCategories()
        {
            try
            {
                if (comboRawCategory == null) return;
                comboRawCategory.Items.Clear();
                comboRawCategory.Items.Add("All Categories");

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT Category FROM RawMaterials WHERE Category IS NOT NULL AND Category != '' ORDER BY Category ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboRawCategory.Items.Add(rdr["Category"].ToString());
                            }
                        }
                    }
                }
                if (comboRawCategory.Items.Count > 0) comboRawCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadRawMaterialUsageReport()
        {
            if (gridRawMaterialReport == null) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    DateTime fromDate = dtpRawFromDate != null ? dtpRawFromDate.Value.Date : DateTime.Today;
                    DateTime toDate = dtpRawToDate != null ? dtpRawToDate.Value.Date : DateTime.Today;

                    string query = @"
                        SELECT 
                            rm.Code as [Item Code],
                            rm.Name as [Raw Material Name],
                            rm.Category as [Category],
                            rm.Unit as [Unit],
                            
                            -- Inward in period
                            ISNULL((
                                SELECT SUM(sm.Quantity) 
                                FROM StockMovements sm 
                                WHERE sm.MaterialId = rm.Id 
                                  AND sm.TransactionType = 'IN_PURCHASE'
                                  AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                            ), 0.0) as [Inward (+)],

                            -- Outward usage in period
                            ISNULL((
                                SELECT SUM(sm.Quantity) 
                                FROM StockMovements sm 
                                WHERE sm.MaterialId = rm.Id 
                                  AND sm.TransactionType = 'OUT_USAGE'
                                  AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                            ), 0.0) as [Kitchen Issued (-)],

                            -- Kitchen Returns in period
                            ISNULL((
                                SELECT SUM(sm.Quantity) 
                                FROM StockMovements sm 
                                WHERE sm.MaterialId = rm.Id 
                                  AND sm.TransactionType = 'IN_RETURN'
                                  AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                            ), 0.0) as [Kitchen Returned (+)],

                            -- Net Consumed in period
                            (
                                ISNULL((
                                    SELECT SUM(sm.Quantity) 
                                    FROM StockMovements sm 
                                    WHERE sm.MaterialId = rm.Id 
                                      AND sm.TransactionType = 'OUT_USAGE'
                                      AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                                ), 0.0)
                                -
                                ISNULL((
                                    SELECT SUM(sm.Quantity) 
                                    FROM StockMovements sm 
                                    WHERE sm.MaterialId = rm.Id 
                                      AND sm.TransactionType = 'IN_RETURN'
                                      AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                                ), 0.0)
                            ) as [Net Consumed (-)],

                            -- Wastage in period
                            ISNULL((
                                SELECT SUM(sm.Quantity) 
                                FROM StockMovements sm 
                                WHERE sm.MaterialId = rm.Id 
                                  AND sm.TransactionType = 'OUT_WASTAGE'
                                  AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                            ), 0.0) as [Wastage (-)],

                            rm.CurrentStock as [Closing Stock],
                            rm.MinStockLevel as [Min Level],
                            rm.UnitPrice as [Unit Cost],
                            
                            -- Net Usage Cost in Period
                            (
                                (
                                    ISNULL((
                                        SELECT SUM(sm.Quantity) 
                                        FROM StockMovements sm 
                                        WHERE sm.MaterialId = rm.Id 
                                          AND sm.TransactionType IN ('OUT_USAGE', 'OUT_WASTAGE')
                                          AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                                    ), 0.0)
                                    -
                                    ISNULL((
                                        SELECT SUM(sm.Quantity) 
                                        FROM StockMovements sm 
                                        WHERE sm.MaterialId = rm.Id 
                                          AND sm.TransactionType = 'IN_RETURN'
                                          AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                                    ), 0.0)
                                ) * rm.UnitPrice
                            ) as [Usage Cost (Rs.)],

                            -- Inward Purchase Cost in Period
                            (
                                ISNULL((
                                    SELECT SUM(sm.TotalCost) 
                                    FROM StockMovements sm 
                                    WHERE sm.MaterialId = rm.Id 
                                      AND sm.TransactionType = 'IN_PURCHASE'
                                      AND CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to
                                ), 0.0)
                            ) as [Inward Cost (Rs.)],

                            (rm.CurrentStock * rm.UnitPrice) as [Stock Value (Rs.)],
                            CASE WHEN rm.CurrentStock <= rm.MinStockLevel THEN 'CRITICAL LOW' ELSE 'OPTIMAL' END as [Status]
                        FROM RawMaterials rm
                        WHERE rm.IsActive = 1";

                    if (comboRawCategory != null && comboRawCategory.SelectedIndex > 0)
                    {
                        query += " AND rm.Category = @cat";
                    }

                    if (txtRawSearch != null && !string.IsNullOrWhiteSpace(txtRawSearch.Text))
                    {
                        query += " AND (rm.Code LIKE @search OR rm.Name LIKE @search)";
                    }

                    query += " ORDER BY [Net Consumed (-)] DESC, rm.CurrentStock ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);

                        if (comboRawCategory != null && comboRawCategory.SelectedIndex > 0)
                        {
                            cmd.Parameters.AddWithValue("@cat", comboRawCategory.SelectedItem.ToString());
                        }

                        if (txtRawSearch != null && !string.IsNullOrWhiteSpace(txtRawSearch.Text))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{txtRawSearch.Text.Trim()}%");
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridRawMaterialReport.DataSource = dt;

                            decimal totalUsageCost = 0;
                            decimal totalInwardCost = 0;
                            decimal totalStockVal = 0;
                            int lowStockCount = 0;

                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Usage Cost (Rs.)"] != DBNull.Value) totalUsageCost += Convert.ToDecimal(r["Usage Cost (Rs.)"]);
                                if (r["Inward Cost (Rs.)"] != DBNull.Value) totalInwardCost += Convert.ToDecimal(r["Inward Cost (Rs.)"]);
                                if (r["Stock Value (Rs.)"] != DBNull.Value) totalStockVal += Convert.ToDecimal(r["Stock Value (Rs.)"]);
                                
                                decimal cur = r["Closing Stock"] != DBNull.Value ? Convert.ToDecimal(r["Closing Stock"]) : 0;
                                decimal min = r["Min Level"] != DBNull.Value ? Convert.ToDecimal(r["Min Level"]) : 0;
                                if (cur <= min) lowStockCount++;
                            }

                            if (lblRawUsageCostVal != null) lblRawUsageCostVal.Text = $"Rs. {totalUsageCost:N2}";
                            if (lblRawInwardCostVal != null) lblRawInwardCostVal.Text = $"Rs. {totalInwardCost:N2}";
                            if (lblRawStockAssetVal != null) lblRawStockAssetVal.Text = $"Rs. {totalStockVal:N2}";
                            if (lblRawLowStockVal != null) lblRawLowStockVal.Text = $"{lowStockCount} Items";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading raw material usage report: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridRawMaterialReport_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || gridRawMaterialReport.Rows[e.RowIndex].DataBoundItem == null) return;

            try
            {
                DataRowView rowView = gridRawMaterialReport.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (rowView != null)
                {
                    decimal stock = rowView["Closing Stock"] != DBNull.Value ? Convert.ToDecimal(rowView["Closing Stock"]) : 0;
                    decimal min = rowView["Min Level"] != DBNull.Value ? Convert.ToDecimal(rowView["Min Level"]) : 0;
                    decimal used = rowView["Kitchen Used (-)"] != DBNull.Value ? Convert.ToDecimal(rowView["Kitchen Used (-)"]) : 0;

                    if (used > 0)
                    {
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Kitchen Used (-)"].Style.ForeColor = Theme.Danger;
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Usage Cost (Rs.)"].Style.ForeColor = Theme.Danger;
                    }

                    if (stock <= 0)
                    {
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Closing Stock"].Style.ForeColor = Color.FromArgb(248, 113, 113);
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Status"].Style.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                    else if (stock <= min)
                    {
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Closing Stock"].Style.ForeColor = Color.FromArgb(251, 191, 36);
                        gridRawMaterialReport.Rows[e.RowIndex].Cells["Status"].Style.ForeColor = Color.FromArgb(251, 191, 36);
                    }
                }
            }
            catch { }
        }
    }
}


