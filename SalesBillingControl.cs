using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class SalesBillingControl : UserControl, IFocusableControl
    {
        public class ComboBoxItem
        {
            public int Id { get; set; }
            public string Display { get; set; }
            public string Text { get; set; }
            public object Value { get; set; }
            public string Tag { get; set; }

            public ComboBoxItem() { }

            public ComboBoxItem(string text, object value, string tag = "")
            {
                Text = text;
                Display = text;
                Value = value;
                if (value is int id) Id = id;
                Tag = tag;
            }

            public override string ToString()
            {
                return Display ?? Text ?? "";
            }
        }

        public static string GetFinancialYearCode(DateTime dt)
        {
            int startYear = (dt.Month >= 4) ? dt.Year : dt.Year - 1;
            int endYear = startYear + 1;
            return $"{(startYear % 100):D2}-{(endYear % 100):D2}";
        }

        public static string GenerateNextInvoiceNumber(SqlConnection conn, SqlTransaction trans = null, DateTime? forDate = null)
        {
            DateTime dt = forDate ?? DateTime.Now;
            string ddmm = dt.ToString("ddMM");
            string fy = GetFinancialYearCode(dt);
            string prefix = $"LC/{ddmm}/";
            string suffix = $"/{fy}";
            string pattern = $"{prefix}%{suffix}";

            int maxSerial = 0;
            string sql = "SELECT InvoiceNumber FROM Sales WHERE InvoiceNumber LIKE @pat";
            try
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@pat", pattern);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string inv = r[0]?.ToString() ?? "";
                            string[] parts = inv.Split('/');
                            // Expected parts: ["LC", "ddMM", "001", "26-27"]
                            if (parts.Length == 4 && int.TryParse(parts[2], out int serial))
                            {
                                if (serial > maxSerial) maxSerial = serial;
                            }
                        }
                    }
                }
            }
            catch { }

            int nextSerial = maxSerial + 1;
            string candidate = $"{prefix}{nextSerial:D3}{suffix}";

            // Safety collision resolution
            try
            {
                while (true)
                {
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM Sales WHERE InvoiceNumber = @candidate", conn, trans))
                    {
                        checkCmd.Parameters.AddWithValue("@candidate", candidate);
                        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (exists == 0) break;
                    }
                    nextSerial++;
                    candidate = $"{prefix}{nextSerial:D3}{suffix}";
                }
            }
            catch { }

            return candidate;
        }

        public static string GetNextInvoiceNumberPreview(SqlConnection externalConn = null)
        {
            try
            {
                if (externalConn != null && externalConn.State == System.Data.ConnectionState.Open)
                {
                    return GenerateNextInvoiceNumber(externalConn, null);
                }
                using (var conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    return GenerateNextInvoiceNumber(conn, null);
                }
            }
            catch
            {
                DateTime dt = DateTime.Now;
                return $"LC/{dt:ddMM}/001/{GetFinancialYearCode(dt)}";
            }
        }

        // Models
        public class CartItem
        {
            public int ProductId { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Category { get; set; }
            public decimal UnitPrice { get; set; } // Inclusive price
            public decimal TaxableRate { get; set; } // Rate excluding 5% GST
            public int Quantity { get; set; }
            public decimal GstRate { get; set; } = 5.0m;
            public string SpecialInstructions { get; set; }
            public int KotId { get; set; } = 0;
            public int KotNumber { get; set; } = 0;
            public bool IsCommittedToKot { get; set; } = false;

            public decimal LineTotal => UnitPrice * Quantity;
            public decimal LineTaxable => Math.Round((UnitPrice * Quantity) / (1 + (GstRate / 100.0m)), 2);
            public decimal LineTax => LineTotal - LineTaxable;
        }

        // Controls
        private Panel topBarPanel;
        private Panel leftCatalogPanel;
        private Panel rightOrderPanel;

        // Top Bar Controls
        private FlowLayoutPanel subBillsFlow;
        private Button btnActiveMode;
        private ComboBox cmbSteward;
        private Button btnSteward;
        private ContextMenuStrip stewardMenu;
        private Button btnBackToFloor;
        private Button btnShareTableTop;
        private Button btnShiftTable;
        private Button btnCancelOrder;
        private Button btnModeDining;
        private Button btnModeTakeaway;
        private Button btnModeDelivery;

        // Catalog Controls
        private TextBox txtSearchItem;
        private int quickQty = 1;
        private Label lblQtyVal;
        private FlowLayoutPanel categoryTabsPanel;
        private Panel catViewport;
        private System.Windows.Forms.Timer catScrollTimer;
        private int catScrollSpeed = 0;
        private FlowLayoutPanel productGridPanel;
        private System.Windows.Forms.Timer searchDebounceTimer;
        private Panel packingChargePanel;
        private Button btnPack0;
        private Button btnPack10;
        private Button btnPack20;
        private Button btnPackDefault;
        private Button btnPackCustom;
        private decimal currentPackingCharge = 0.0m;

        // Right Order & KOT Panel Controls
        private Panel kotHeaderPanel;
        private Label lblKotOrderInfo;
        private Button btnKotSteward;
        private Button btnQuickCounter;
        private FlowLayoutPanel kotItemsContainer;
        private Label lblTotalQty;
        private Label lblSubTotalTitle;
        private Label lblSubTotal;
        private Label lblDiscountTitle;
        private ComboBox comboDiscountType;
        private TextBox txtDiscountVal;
        private Label lblDiscount;
        private Label lblTaxableTitle;
        private Label lblTaxableVal;
        private Label lblTaxTitle;
        private Label lblTax;
        private Label lblNetTitle;
        private Label lblGrandTotal;
        private Button btnPayCash;
        private Button btnPayUpi;
        private Button btnPayCard;
        private Button btnPaySplit;
        private Button btnPayPrint;
        private Button btnKotComment;
        private Button btnPrintKot;
        private Button btnSettle;
        private string selectedPaymentMethod = "Cash";
        private bool isUpdatingDiscount = false;
        private string currentKotComment = "";
        private decimal currentDiscountAmount = 0.0m;
        private string currentDiscountReason = "";

        // Menu In-Memory Fast Cache
        public class CachedProduct
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public decimal SalesPrice { get; set; }
            public decimal GstRate { get; set; }
            public bool IsVeg { get; set; }
        }

        private static List<CachedProduct> _cachedProducts = null;
        private static List<string> _cachedCategories = null;
        private static List<string> _cachedStewards = null;

        public static void InvalidateMenuCache()
        {
            _cachedProducts = null;
            _cachedCategories = null;
            _cachedStewards = null;
        }

        // State
        public string ActiveTableNumber { get; private set; } = "1";
        public string ActiveOrderType { get; private set; } = "DINING";
        private List<CartItem> cartItems = new List<CartItem>();
        private string selectedCategory = "All";

        public event Action OnNavigateToFloor;

        public SalesBillingControl(string initialTable = "1", string initialOrderType = "DINING")
        {
            ActiveTableNumber = string.IsNullOrEmpty(initialTable) ? "1" : initialTable;
            ActiveOrderType = string.IsNullOrEmpty(initialOrderType) ? "DINING" : initialOrderType;

            InitializeComponent();
            Theme.SetDoubleBuffered(productGridPanel);
            Theme.SetDoubleBuffered(categoryTabsPanel);
            Theme.SetDoubleBuffered(kotItemsContainer);

            LoadStewards();
            LoadCategories();
            LoadProducts();
            LoadTableOrder(ActiveTableNumber, ActiveOrderType);
            this.Load += (s, e) => FocusDefaultControl();
            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                if (txtSearchItem != null && !txtSearchItem.IsDisposed && txtSearchItem.Visible)
                {
                    txtSearchItem.Focus();
                    txtSearchItem.SelectAll();
                }
            }
            catch { }
        }

        public void LoadTableOrder(string tableNum, string orderType = "DINING")
        {
            ActiveTableNumber = string.IsNullOrEmpty(tableNum) ? "1" : tableNum;
            ActiveOrderType = string.IsNullOrEmpty(orderType) ? "DINING" : orderType;

            if (lblKotOrderInfo != null)
            {
                if (ActiveOrderType == "TAKEAWAY")
                    lblKotOrderInfo.Text = $"🛍️ Takeaway Token {ActiveTableNumber}";
                else if (ActiveOrderType == "DELIVERY")
                    lblKotOrderInfo.Text = $"🛵 Delivery #{ActiveTableNumber}";
                else if (ActiveTableNumber.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase))
                    lblKotOrderInfo.Text = $"⏳ Waiting #{ActiveTableNumber}";
                else
                    lblKotOrderInfo.Text = $"🍽️ Table {ActiveTableNumber} • Dining";
            }

            if (ActiveOrderType != "DINING")
            {
                cmbSteward.SelectedItem = "Direct Counter";
            }

            packingChargePanel.Visible = true;
            decimal defaultPacking = GetDefaultTakeawayPackingCharge();
            if (ActiveOrderType == "TAKEAWAY")
            {
                if (currentPackingCharge == 0)
                {
                    currentPackingCharge = defaultPacking;
                }
            }
            else
            {
                currentPackingCharge = 0.0m;
            }
            UpdatePackChargeButtonHighlights();

            // Load Existing Active KOT Items for this table
            cartItems.Clear();
            currentKotComment = "";
            currentDiscountAmount = 0.0m;
            currentDiscountReason = "";
            isUpdatingDiscount = true;
            if (txtDiscountVal != null) txtDiscountVal.Text = "0";
            if (comboDiscountType != null) comboDiscountType.SelectedIndex = 0;
            isUpdatingDiscount = false;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT k.Id AS KotId, k.KOTNumber, k.Steward, k.KotComment,
                               kd.ProductId, kd.ItemName, kd.Quantity, kd.Rate, kd.Amount, kd.Instructions, kd.IsVoided
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.TableNumber = @tNum 
                          AND (k.OrderType = @type OR (@type = 'DINING' AND (k.OrderType IS NULL OR k.OrderType = '')))
                          AND k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0
                        ORDER BY k.KOTNumber, kd.Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                        cmd.Parameters.AddWithValue("@type", ActiveOrderType);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int kotId = Convert.ToInt32(r["KotId"]);
                                int kotNum = Convert.ToInt32(r["KOTNumber"]);
                                string steward = r["Steward"]?.ToString();
                                if (!string.IsNullOrEmpty(steward))
                                {
                                    cmbSteward.SelectedItem = steward;
                                }

                                CartItem it = new CartItem
                                {
                                    KotId = kotId,
                                    KotNumber = kotNum,
                                    ProductId = r["ProductId"] != DBNull.Value ? Convert.ToInt32(r["ProductId"]) : 0,
                                    ItemName = r["ItemName"].ToString(),
                                    UnitPrice = Convert.ToDecimal(r["Rate"]),
                                    Quantity = Convert.ToInt32(r["Quantity"]),
                                    SpecialInstructions = r["Instructions"]?.ToString(),
                                    IsCommittedToKot = true
                                };
                                cartItems.Add(it);
                            }
                        }
                    }
                }
            }
            catch { }

            if (ActiveOrderType == "DINING" && cartItems.Count == 0)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT CurrentSteward FROM CafeTables WHERE TableNumber = @tNum", conn))
                        {
                            cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                            object res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value && !string.IsNullOrWhiteSpace(res.ToString()))
                            {
                                cmbSteward.SelectedItem = res.ToString();
                            }
                        }
                    }
                }
                catch { }
            }
            UpdateStewardButtonText();

            UpdateSubBillsHeader();
            RefreshOrderCartView();
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)(() => {
                    try { FocusDefaultControl(); } catch { }
                }));
            }
        }

        private void UpdateSubBillsHeader()
        {
            if (subBillsFlow == null) return;
            subBillsFlow.SuspendLayout();
            subBillsFlow.Controls.Clear();

            if (ActiveOrderType != "DINING")
            {
                btnActiveMode.Visible = true;
                btnActiveMode.Text = ActiveOrderType == "TAKEAWAY" ? $"🛍️ TAKE AWAY (Token {ActiveTableNumber})" : $"🛵 DELIVERY (Order {ActiveTableNumber})";
                btnActiveMode.BackColor = ActiveOrderType == "TAKEAWAY" ? Theme.Accent : Color.FromArgb(249, 115, 22);
                btnActiveMode.ForeColor = Color.White;
                btnActiveMode.FlatAppearance.BorderSize = 0;
                if (btnShareTableTop != null) btnShareTableTop.Visible = false;
                if (btnShiftTable != null) btnShiftTable.Visible = false;
                subBillsFlow.Controls.Add(btnActiveMode);
                subBillsFlow.ResumeLayout();
                return;
            }

            if (btnShareTableTop != null) btnShareTableTop.Visible = true;
            if (btnShiftTable != null) btnShiftTable.Visible = true;

            string baseNum = TableHelper.GetBaseTableNumber(ActiveTableNumber);
            List<SubTableTabInfo> subTables = new List<SubTableTabInfo>();

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Calculate live totals for this base table and any sub-tables
                    string q = @"
                        SELECT t.TableNumber, t.TableName, t.Status,
                               ISNULL(SUM(kd.Amount), 0) AS LiveTotal
                        FROM CafeTables t
                        LEFT JOIN KOTMaster k ON t.TableNumber = k.TableNumber AND k.Status IN ('Active', 'Served', 'Printed')
                        LEFT JOIN KOTDetails kd ON k.Id = kd.KOTId AND kd.IsVoided = 0
                        WHERE t.IsActive = 1 AND (t.TableNumber = @baseNum OR t.TableNumber LIKE @pattern)
                        GROUP BY t.TableNumber, t.TableName, t.Status";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@baseNum", baseNum);
                        cmd.Parameters.AddWithValue("@pattern", baseNum + "-%");
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string tNum = r["TableNumber"].ToString();
                                string tName = r["TableName"]?.ToString() ?? tNum;
                                string status = r["Status"]?.ToString() ?? "Available";
                                decimal total = Convert.ToDecimal(r["LiveTotal"]);
                                subTables.Add(new SubTableTabInfo { TableNumber = tNum, TableName = tName, Status = status, Amount = total });
                            }
                        }
                    }
                }
            }
            catch { }

            subTables.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));

            if (subTables.Count > 1 || ActiveTableNumber.Contains("-"))
            {
                btnActiveMode.Visible = false;

                foreach (var sub in subTables)
                {
                    bool isCurrent = sub.TableNumber.Equals(ActiveTableNumber, StringComparison.OrdinalIgnoreCase);
                    string custSuffix = TableHelper.GetCustomerSuffix(sub.TableNumber);
                    string btnLabel = string.IsNullOrEmpty(custSuffix)
                        ? $"🍽️ Table {sub.TableNumber} • ₹{sub.Amount:0}"
                        : $"🪑 Cust {custSuffix} ({sub.TableNumber}) • ₹{sub.Amount:0}";

                    Button btnSub = new Button
                    {
                        Text = btnLabel,
                        AutoSize = true,
                        Height = 36,
                        MinimumSize = new Size(80, 36),
                        Padding = new Padding(8, 0, 8, 0),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0, 2, 6, 0),
                        BackColor = isCurrent ? Color.FromArgb(109, 40, 217) : Color.FromArgb(30, 41, 59),
                        ForeColor = isCurrent ? Color.White : Color.FromArgb(203, 213, 225)
                    };
                    btnSub.FlatAppearance.BorderSize = isCurrent ? 0 : 1;
                    btnSub.FlatAppearance.BorderColor = isCurrent ? Color.FromArgb(109, 40, 217) : Color.FromArgb(51, 65, 85);

                    string targetNum = sub.TableNumber;
                    btnSub.Click += (s, e) => {
                        LoadTableOrder(targetNum, "DINING");
                    };
                    subBillsFlow.Controls.Add(btnSub);
                }
            }
            else
            {
                btnActiveMode.Visible = true;
                decimal curAmt = (subTables.Count > 0) ? subTables[0].Amount : 0;
                btnActiveMode.Text = curAmt > 0 ? $"🍽️ Table {ActiveTableNumber} • ₹{curAmt:0}" : $"🍽️ Table {ActiveTableNumber}";
                btnActiveMode.BackColor = Theme.Accent;
                btnActiveMode.ForeColor = Color.White;
                btnActiveMode.FlatAppearance.BorderSize = 0;
                subBillsFlow.Controls.Add(btnActiveMode);
            }

            subBillsFlow.ResumeLayout();
        }

        private class SubTableTabInfo
        {
            public string TableNumber { get; set; }
            public string TableName { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.Secondary;
            this.Padding = new Padding(0);

            // ================= 1. TOP BAR PANEL =================
            topBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(8, 8, 8, 8)
            };
            topBarPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, topBarPanel.Height - 1, topBarPanel.Width, topBarPanel.Height - 1);
            };

            // Left Section (Back Button + Sub-Bills Flow + Share Table + Shift Table)
            FlowLayoutPanel topLeftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            btnBackToFloor = new Button
            {
                Text = "⬅ Tables",
                AutoSize = true,
                Height = 36,
                MinimumSize = new Size(80, 36),
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextWhite,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnBackToFloor.FlatAppearance.BorderSize = 1;
            btnBackToFloor.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnBackToFloor.Click += (s, e) => OnNavigateToFloor?.Invoke();
            topLeftFlow.Controls.Add(btnBackToFloor);

            subBillsFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            topLeftFlow.Controls.Add(subBillsFlow);

            btnActiveMode = new Button
            {
                Text = $"🍽️ Table {ActiveTableNumber}",
                Font = Theme.BoldFont,
                ForeColor = Color.White,
                BackColor = Theme.Accent,
                Height = 36,
                MinimumSize = new Size(80, 36),
                Padding = new Padding(10, 0, 10, 0),
                AutoSize = true,
                Margin = new Padding(0, 2, 6, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnActiveMode.FlatAppearance.BorderSize = 0;
            btnActiveMode.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnActiveMode.Click += (s, e) => OnNavigateToFloor?.Invoke();

            btnShareTableTop = new Button
            {
                Text = "🪑 Share",
                AutoSize = true,
                Height = 36,
                MinimumSize = new Size(80, 36),
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.FromArgb(109, 40, 217), // Violet
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnShareTableTop.FlatAppearance.BorderSize = 0;
            btnShareTableTop.Click += (s, e) => {
                using (TableShareDialog dlg = new TableShareDialog(ActiveTableNumber))
                {
                    if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedTableNumber))
                    {
                        LoadTableOrder(dlg.SelectedTableNumber, ActiveOrderType);
                    }
                }
            };
            topLeftFlow.Controls.Add(btnShareTableTop);

            btnShiftTable = new Button
            {
                Text = "🔁 Shift",
                AutoSize = true,
                Height = 36,
                MinimumSize = new Size(80, 36),
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextLight,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnShiftTable.FlatAppearance.BorderSize = 1;
            btnShiftTable.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnShiftTable.Click += BtnShiftTable_Click;
            topLeftFlow.Controls.Add(btnShiftTable);

            topBarPanel.Controls.Add(topLeftFlow);

            // Right Section (Steward, Mode Switchers)
            FlowLayoutPanel topRightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0),
                Margin = new Padding(0)
            };

            // Steward Selector (Styled button matching adjacent 36px controls exactly)
            cmbSteward = new ComboBox();
            cmbSteward.Visible = false;
            cmbSteward.SelectedIndexChanged += (s, e) => UpdateStewardButtonText();
            this.Controls.Add(cmbSteward);

            stewardMenu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(20, 27, 42),
                ForeColor = Color.White,
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Renderer = new DarkMenuRenderer()
            };

            btnSteward = new Button
            {
                Text = "👤 Steward ▾",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(8, 0, 8, 0),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 0, 0)
            };
            btnSteward.FlatAppearance.BorderSize = 1;
            btnSteward.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnSteward.Click += (s, e) => ShowStewardMenu(btnSteward);
            topRightFlow.Controls.Add(btnSteward);

            // Mode Switch Buttons
            btnModeDelivery = CreateTopSwitchButton("DELIVERY", "🛵 Delivery");
            btnModeTakeaway = CreateTopSwitchButton("TAKEAWAY", "🛍️ Takeaway");
            btnModeDining = CreateTopSwitchButton("DINING", "🍽️ Dining");

            topRightFlow.Controls.Add(btnModeDelivery);
            topRightFlow.Controls.Add(btnModeTakeaway);
            topRightFlow.Controls.Add(btnModeDining);

            topBarPanel.Controls.Add(topRightFlow);

            topBarPanel.SizeChanged += (s, e) => {
                if (topLeftFlow != null && topRightFlow != null)
                {
                    int maxLeft = Math.Max(200, topBarPanel.ClientSize.Width - topRightFlow.Width - 10);
                    topLeftFlow.MaximumSize = new Size(maxLeft, topBarPanel.ClientSize.Height);
                }
            };

            // ================= 2. RIGHT ORDER & KOT CART PANEL =================
            rightOrderPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(8)
            };
            rightOrderPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, 0, 0, rightOrderPanel.Height);
            };

            InitializeRightOrderPanel();

            // ================= 3. LEFT CATALOG & MENU PANEL =================
            leftCatalogPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Secondary,
                Padding = new Padding(8, 6, 8, 6)
            };

            InitializeLeftCatalogPanel();

            // Add docked controls in correct Z-order
            this.Controls.Add(leftCatalogPanel);
            this.Controls.Add(rightOrderPanel);
            this.Controls.Add(topBarPanel);

            topBarPanel.SendToBack();
            rightOrderPanel.SendToBack();
            leftCatalogPanel.BringToFront();
        }

        private Button CreateTopSwitchButton(string mode, string text)
        {
            bool isActive = (ActiveOrderType == mode);
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 36,
                Padding = new Padding(7, 0, 7, 0),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Theme.Accent : Color.FromArgb(30, 41, 59),
                ForeColor = isActive ? Color.White : Theme.TextMuted,
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 0, 0),
                Tag = mode
            };
            btn.FlatAppearance.BorderSize = isActive ? 0 : 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.Click += (s, e) => {
                LoadTableOrder(ActiveTableNumber, mode);
                UpdateTopSwitchButtons();
            };
            return btn;
        }

        private void UpdateTopSwitchButtons()
        {
            if (btnModeDining != null) UpdateSwitchStyle(btnModeDining, ActiveOrderType == "DINING");
            if (btnModeTakeaway != null) UpdateSwitchStyle(btnModeTakeaway, ActiveOrderType == "TAKEAWAY");
            if (btnModeDelivery != null) UpdateSwitchStyle(btnModeDelivery, ActiveOrderType == "DELIVERY");
        }

        private void UpdateSwitchStyle(Button btn, bool active)
        {
            btn.BackColor = active ? Theme.Accent : Color.FromArgb(30, 41, 59);
            btn.ForeColor = active ? Color.White : Theme.TextMuted;
            btn.FlatAppearance.BorderSize = active ? 0 : 1;
        }

        private void InitializeLeftCatalogPanel()
        {
            // Catalog Top Toolbar Stack (Search + Packaging + Categories)
            Panel headerStack = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 8)
            };

            // 1. Search Bar Panel
            Panel searchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 0, 6)
            };
            searchBar.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, searchBar.Width - 1, searchBar.Height - 1), 8))
                using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.2f))
                    e.Graphics.DrawPath(p, path);
            };

            Label lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Left,
                Width = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent
            };
            searchBar.Controls.Add(lblSearchIcon);

            // Right Quick Qty Stepper
            FlowLayoutPanel qtyPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 2, 0, 0)
            };

            Label lblQtyTitle = new Label
            {
                Text = "Qty:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Margin = new Padding(0, 8, 6, 0),
                BackColor = Color.Transparent
            };
            qtyPanel.Controls.Add(lblQtyTitle);

            Button btnMinus = new Button
            {
                Text = "−",
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 2, 0)
            };
            btnMinus.FlatAppearance.BorderSize = 1;
            btnMinus.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnMinus.Click += (s, e) => {
                if (quickQty > 1)
                {
                    quickQty--;
                    lblQtyVal.Text = quickQty.ToString();
                }
            };
            qtyPanel.Controls.Add(btnMinus);

            lblQtyVal = new Label
            {
                Text = "1",
                Size = new Size(34, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                BackColor = Color.FromArgb(24, 33, 47),
                Margin = new Padding(0, 1, 2, 0)
            };
            lblQtyVal.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1))
                    e.Graphics.DrawRectangle(p, 0, 0, lblQtyVal.Width - 1, lblQtyVal.Height - 1);
            };
            qtyPanel.Controls.Add(lblQtyVal);

            Button btnPlus = new Button
            {
                Text = "+",
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 0)
            };
            btnPlus.FlatAppearance.BorderSize = 1;
            btnPlus.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnPlus.Click += (s, e) => {
                if (quickQty < 99)
                {
                    quickQty++;
                    lblQtyVal.Text = quickQty.ToString();
                }
            };
            qtyPanel.Controls.Add(btnPlus);

            searchBar.Controls.Add(qtyPanel);

            Panel txtWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(6, 7, 8, 5)
            };

            txtSearchItem = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Regular),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            Win7Compat.SetPlaceholder(txtSearchItem, "Type dish name or barcode to filter...");
            searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 40 };
            searchDebounceTimer.Tick += (s, e) => {
                searchDebounceTimer.Stop();
                LoadProducts(txtSearchItem.Text.Trim(), selectedCategory);
            };
            txtSearchItem.TextChanged += (s, e) => {
                searchDebounceTimer.Stop();
                searchDebounceTimer.Start();
            };
            txtWrapper.Controls.Add(txtSearchItem);
            searchBar.Controls.Add(txtWrapper);

            // Ensure proper docking order so textbox fills all remaining space and is visible
            lblSearchIcon.SendToBack();
            qtyPanel.SendToBack();
            txtWrapper.BringToFront();

            // 2. Packaging Charges Bar (Visible in Takeaway & Dining modes)
            packingChargePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(20, 30, 48),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 4, 0, 4),
                Visible = true
            };

            Label lblPack = new Label
            {
                Text = "📦 Packaging:",
                Font = Theme.BoldFont,
                ForeColor = Theme.Warning,
                AutoSize = true,
                Margin = new Padding(0, 5, 8, 0)
            };
            packingChargePanel.Controls.Add(lblPack);

            decimal defPack = GetDefaultTakeawayPackingCharge();
            btnPack0 = CreatePackChargeBtn("No Pack (₹0)", 0);
            btnPack10 = CreatePackChargeBtn("₹10", 10);
            btnPack20 = CreatePackChargeBtn("₹20", 20);
            btnPackDefault = CreatePackChargeBtn($"₹{defPack:0} (Standard)", defPack);

            btnPackCustom = new Button
            {
                Text = "✏️ Custom...",
                AutoSize = true,
                Height = 28,
                Padding = new Padding(8, 0, 8, 0),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btnPackCustom.FlatAppearance.BorderSize = 0;
            btnPackCustom.Click += (s, e) => OnBtnPackCustomClick();

            packingChargePanel.Controls.Add(btnPack0);
            packingChargePanel.Controls.Add(btnPack10);
            packingChargePanel.Controls.Add(btnPack20);
            packingChargePanel.Controls.Add(btnPackDefault);
            packingChargePanel.Controls.Add(btnPackCustom);

            // 3. Category Horizontal Tabs Bar with Smooth Chevrons (NO WHITE NATIVE SCROLLBAR)
            Panel categoryContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

            Button btnCatLeft = new Button
            {
                Text = "◀",
                Dock = DockStyle.Left,
                Width = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 33, 47),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnCatLeft.FlatAppearance.BorderSize = 1;
            btnCatLeft.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);

            Button btnCatRight = new Button
            {
                Text = "▶",
                Dock = DockStyle.Right,
                Width = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 33, 47),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnCatRight.FlatAppearance.BorderSize = 1;
            btnCatRight.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);

            catViewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 0, 4, 0)
            };

            categoryTabsPanel = new FlowLayoutPanel
            {
                Location = new Point(4, 4),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            catViewport.Controls.Add(categoryTabsPanel);

            // Auto-scroll timer setup for smooth continuous hover scrolling
            if (catScrollTimer != null) { catScrollTimer.Stop(); catScrollTimer.Dispose(); }
            catScrollTimer = new System.Windows.Forms.Timer { Interval = 16 };
            catScrollTimer.Tick += (s, e) => {
                if (catScrollSpeed == 0 || categoryTabsPanel == null || catViewport == null) return;
                if (categoryTabsPanel.Width <= catViewport.Width) return;

                int maxScroll = Math.Max(0, categoryTabsPanel.Width - catViewport.Width + 24);
                int newLeft = categoryTabsPanel.Left + catScrollSpeed;
                if (newLeft > 4) newLeft = 4;
                if (newLeft < -maxScroll) newLeft = -maxScroll;
                categoryTabsPanel.Left = newLeft;
            };

            btnCatLeft.MouseEnter += (s, e) => {
                catScrollSpeed = 10;
                catScrollTimer.Start();
            };
            btnCatLeft.MouseLeave += (s, e) => {
                catScrollSpeed = 0;
                catScrollTimer.Stop();
            };
            btnCatLeft.Click += (s, e) => {
                categoryTabsPanel.Left = Math.Min(4, categoryTabsPanel.Left + 200);
            };

            btnCatRight.MouseEnter += (s, e) => {
                catScrollSpeed = -10;
                catScrollTimer.Start();
            };
            btnCatRight.MouseLeave += (s, e) => {
                catScrollSpeed = 0;
                catScrollTimer.Stop();
            };
            btnCatRight.Click += (s, e) => {
                int maxScroll = Math.Max(0, categoryTabsPanel.Width - catViewport.Width + 24);
                categoryTabsPanel.Left = Math.Max(-maxScroll, categoryTabsPanel.Left - 200);
            };

            catViewport.MouseMove += (s, e) => HandleCategoryViewportHover(Cursor.Position);
            catViewport.MouseLeave += (s, e) => {
                if (!catViewport.ClientRectangle.Contains(catViewport.PointToClient(Cursor.Position)))
                {
                    catScrollSpeed = 0;
                    catScrollTimer.Stop();
                }
            };
            catViewport.MouseWheel += (s, e) => {
                if (categoryTabsPanel == null || catViewport == null) return;
                int maxScroll = Math.Max(0, categoryTabsPanel.Width - catViewport.Width + 24);
                if (e.Delta < 0)
                    categoryTabsPanel.Left = Math.Max(-maxScroll, categoryTabsPanel.Left - 120);
                else
                    categoryTabsPanel.Left = Math.Min(4, categoryTabsPanel.Left + 120);
            };

            categoryContainer.Controls.Add(catViewport);
            categoryContainer.Controls.Add(btnCatLeft);
            categoryContainer.Controls.Add(btnCatRight);

            // Add in reverse docking order for proper top-to-bottom layout
            headerStack.Controls.Add(categoryContainer);
            headerStack.Controls.Add(packingChargePanel);
            headerStack.Controls.Add(searchBar);

            searchBar.SendToBack();
            packingChargePanel.SendToBack();
            categoryContainer.SendToBack();

            leftCatalogPanel.Controls.Add(headerStack);

            // 4. Product Cards Grid
            productGridPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };
            Theme.SetDoubleBuffered(productGridPanel);
            productGridPanel.SizeChanged += (s, e) => AdjustProductGridTiles();
            leftCatalogPanel.Controls.Add(productGridPanel);

            headerStack.SendToBack();
            productGridPanel.BringToFront();
        }

        private void AdjustProductGridTiles()
        {
            if (productGridPanel == null || productGridPanel.Controls.Count == 0) return;
            productGridPanel.SuspendLayout();

            int scrollbarW = SystemInformation.VerticalScrollBarWidth + 8;
            int availW = productGridPanel.ClientSize.Width - productGridPanel.Padding.Horizontal - scrollbarW;
            if (availW < 180) availW = 360;
            int cols = Math.Max(2, Math.Min(6, availW / 180));
            int tileW = Math.Max(125, (availW / cols) - 10);
            int tileH = 105;

            foreach (Control c in productGridPanel.Controls)
            {
                if (c is ProductTile tile)
                {
                    if (tile.Width != tileW || tile.Height != tileH)
                    {
                        tile.Size = new Size(tileW, tileH);
                    }
                }
            }

            productGridPanel.ResumeLayout(true);
        }

        private Button CreatePackChargeBtn(string text, decimal val)
        {
            Button btn = new Button
            {
                Text = text,
                Tag = val,
                AutoSize = true,
                Height = 28,
                Padding = new Padding(8, 0, 8, 0),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = (currentPackingCharge == val) ? Theme.Accent : Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                currentPackingCharge = val;
                UpdatePackChargeButtonHighlights();
                RefreshOrderCartView();
            };
            return btn;
        }

        private void UpdatePackChargeButtonHighlights()
        {
            if (packingChargePanel == null) return;
            bool matchedPreset = false;
            foreach (Control c in packingChargePanel.Controls)
            {
                if (c is Button b && b.Tag is decimal dVal)
                {
                    if (dVal == currentPackingCharge)
                    {
                        b.BackColor = Theme.Accent;
                        matchedPreset = true;
                    }
                    else
                    {
                        b.BackColor = Color.FromArgb(30, 41, 59);
                    }
                }
            }

            if (btnPackCustom != null)
            {
                if (!matchedPreset && currentPackingCharge > 0)
                {
                    btnPackCustom.BackColor = Theme.Accent;
                    btnPackCustom.Text = $"✏️ Custom (₹{currentPackingCharge:0})";
                }
                else
                {
                    btnPackCustom.BackColor = Color.FromArgb(30, 41, 59);
                    btnPackCustom.Text = "✏️ Custom...";
                }
            }
        }

        private void OnBtnPackCustomClick()
        {
            string prompt = "Enter Packaging Charge (₹):\n\n(Zero tax will be deducted from packaging charge)";
            string def = currentPackingCharge > 0 ? currentPackingCharge.ToString("0") : "20";
            string valStr = Microsoft.VisualBasic.Interaction.InputBox(prompt, "Custom Packaging Charge", def);
            if (!string.IsNullOrWhiteSpace(valStr))
            {
                if (decimal.TryParse(valStr.Trim(), out decimal entered) && entered >= 0)
                {
                    currentPackingCharge = Math.Round(entered, 2);
                    UpdatePackChargeButtonHighlights();
                    RefreshOrderCartView();
                }
                else
                {
                    MessageBox.Show("Please enter a valid amount.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private decimal GetDefaultTakeawayPackingCharge()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 DefaultPackingCharge FROM AppProfile", conn))
                    {
                        object obj = cmd.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value && decimal.TryParse(obj.ToString(), out decimal val))
                        {
                            return val;
                        }
                    }
                }
            }
            catch { }
            return 40.00m;
        }

        private void ShowItemParcelMenu(Button anchor, CartItem item)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new DarkMenuRenderer();

            // Quick splits if quantity >= 2
            if (item.Quantity >= 2)
            {
                for (int p = 1; p < item.Quantity; p++)
                {
                    int eat = item.Quantity - p;
                    int pack = p;
                    string label = $"🛍️ {eat} Eat Here, {pack} Parcel";
                    menu.Items.Add(label, null, (s, e) => {
                        item.SpecialInstructions = $"{eat} Eat Here, {pack} Parcel";
                        PromptForSuggestedPackingCharge();
                        RefreshOrderCartView();
                    });
                }
            }

            menu.Items.Add("🥡 All Parcel (Pack & Carry)", null, (s, e) => {
                item.SpecialInstructions = "All Parcel (Pack & Carry)";
                PromptForSuggestedPackingCharge();
                RefreshOrderCartView();
            });

            menu.Items.Add("🍽️ Eat Here (Dine-In)", null, (s, e) => {
                item.SpecialInstructions = "Eat Here (Dine-In)";
                RefreshOrderCartView();
            });

            menu.Items.Add(new ToolStripSeparator());

            if (item.Quantity >= 2 && !item.IsCommittedToKot)
            {
                menu.Items.Add("✂️ Split Item (1 Eat Here + 1 Parcel)", null, (s, e) => {
                    item.Quantity -= 1;
                    item.SpecialInstructions = "1 Eat Here";
                    cartItems.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        Category = item.Category,
                        UnitPrice = item.UnitPrice,
                        GstRate = item.GstRate,
                        Quantity = 1,
                        SpecialInstructions = "1 Parcel (Carry)",
                        IsCommittedToKot = false
                    });
                    PromptForSuggestedPackingCharge();
                    RefreshOrderCartView();
                });
                menu.Items.Add(new ToolStripSeparator());
            }

            menu.Items.Add("✏️ Custom Kitchen Instruction...", null, (s, e) => {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Enter special cooking / parcel instruction for {item.ItemName}:",
                    "Kitchen / Parcel Note",
                    item.SpecialInstructions ?? "");
                if (!string.IsNullOrWhiteSpace(input))
                {
                    item.SpecialInstructions = input.Trim();
                    if (input.IndexOf("pack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        input.IndexOf("parcel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        input.IndexOf("carry", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        PromptForSuggestedPackingCharge();
                    }
                    RefreshOrderCartView();
                }
            });

            if (!string.IsNullOrEmpty(item.SpecialInstructions))
            {
                menu.Items.Add("❌ Clear Instruction", null, (s, e) => {
                    item.SpecialInstructions = null;
                    RefreshOrderCartView();
                });
            }

            menu.Show(anchor, new Point(0, anchor.Height));
        }

        private void PromptForSuggestedPackingCharge()
        {
            if (currentPackingCharge == 0)
            {
                DialogResult res = MessageBox.Show(
                    "You added a parcel / carry instruction to this order.\n\nWould you like to add packaging charges (e.g. ₹10 / ₹20 / Standard)?",
                    "Add Packaging Charges?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    decimal defPack = GetDefaultTakeawayPackingCharge();
                    if (defPack <= 0) defPack = 20.0m;
                    currentPackingCharge = defPack;
                    UpdatePackChargeButtonHighlights();
                }
            }
        }

        private void InitializeRightOrderPanel()
        {
            // Bottom Checkout Actions Panel
            Panel checkoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 310,
                BackColor = Color.FromArgb(11, 19, 41), // Deep navy matching screenshot
                Padding = new Padding(10, 6, 10, 8)
            };

            // Top Compact KOT Toolbar (Send to Kitchen and Note)
            btnPrintKot = new Button
            {
                Text = "🍳 Send to Kitchen",
                Location = new Point(10, 6),
                Size = new Size(200, 28),
                BackColor = Color.FromArgb(109, 40, 217), // Violet
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPrintKot.FlatAppearance.BorderSize = 0;
            btnPrintKot.Click += BtnPrintKot_Click;
            checkoutPanel.Controls.Add(btnPrintKot);

            btnKotComment = new Button
            {
                Text = "📝 Note",
                Location = new Point(216, 6),
                Size = new Size(154, 28),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnKotComment.FlatAppearance.BorderSize = 1;
            btnKotComment.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnKotComment.Click += BtnKotComment_Click;
            checkoutPanel.Controls.Add(btnKotComment);

            btnCancelOrder = new Button
            {
                Text = "🚫 Cancel",
                Location = new Point(282, 6),
                Size = new Size(88, 28),
                BackColor = Color.FromArgb(153, 27, 27), // Crimson Red
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelOrder.FlatAppearance.BorderSize = 0;
            btnCancelOrder.Click += BtnCancelOrder_Click;
            checkoutPanel.Controls.Add(btnCancelOrder);

            lblTotalQty = new Label { Visible = false };
            checkoutPanel.Controls.Add(lblTotalQty);

            // Row 1: Sub Total
            lblSubTotalTitle = new Label
            {
                Text = "Sub Total",
                Location = new Point(10, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            lblSubTotal = new Label
            {
                Text = "Rs. 0.00",
                Location = new Point(240, 38),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White
            };
            checkoutPanel.Controls.Add(lblSubTotalTitle);
            checkoutPanel.Controls.Add(lblSubTotal);

            // Row 2: Discount with [% v] [ 0 ] and - Rs. 0.00
            lblDiscountTitle = new Label
            {
                Text = "Discount",
                Location = new Point(10, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            comboDiscountType = new ComboBox
            {
                Location = new Point(84, 66),
                Size = new Size(46, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            comboDiscountType.Items.AddRange(new object[] { "%", "Rs." });
            comboDiscountType.SelectedIndex = 0;
            comboDiscountType.SelectedIndexChanged += (s, e) => {
                if (!isUpdatingDiscount)
                {
                    RecalculateDiscountFromInputs();
                    UpdateSummaryDisplay();
                }
            };

            txtDiscountVal = new TextBox
            {
                Text = "0",
                Location = new Point(135, 66),
                Size = new Size(56, 24),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            txtDiscountVal.TextChanged += (s, e) => {
                if (!isUpdatingDiscount)
                {
                    RecalculateDiscountFromInputs();
                    UpdateSummaryDisplay();
                }
            };

            lblDiscount = new Label
            {
                Text = "- Rs. 0.00",
                Location = new Point(240, 68),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129), // Emerald Green
                Cursor = Cursors.Hand
            };
            lblDiscount.Click += (s, e) => OpenDiscountDialog();

            checkoutPanel.Controls.Add(lblDiscountTitle);
            checkoutPanel.Controls.Add(comboDiscountType);
            checkoutPanel.Controls.Add(txtDiscountVal);
            checkoutPanel.Controls.Add(lblDiscount);

            // Row 3: Taxable Value
            lblTaxableTitle = new Label
            {
                Text = "Taxable Value",
                Location = new Point(10, 96),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            lblTaxableVal = new Label
            {
                Text = "Rs. 0.00",
                Location = new Point(240, 94),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White
            };
            checkoutPanel.Controls.Add(lblTaxableTitle);
            checkoutPanel.Controls.Add(lblTaxableVal);

            // Row 4: IGST Tax
            lblTaxTitle = new Label
            {
                Text = "IGST Tax",
                Location = new Point(10, 122),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            lblTax = new Label
            {
                Text = "Rs. 0.00",
                Location = new Point(240, 120),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(249, 115, 22) // Orange
            };
            checkoutPanel.Controls.Add(lblTaxTitle);
            checkoutPanel.Controls.Add(lblTax);

            // Row 5: Divider
            Panel netLine = new Panel
            {
                Location = new Point(10, 148),
                Size = new Size(360, 1),
                BackColor = Color.FromArgb(30, 41, 59)
            };
            checkoutPanel.Controls.Add(netLine);

            // Row 6: Total Payable
            lblNetTitle = new Label
            {
                Text = "Total Payable",
                Location = new Point(10, 156),
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White
            };
            lblGrandTotal = new Label
            {
                Text = "Rs. 0.00",
                Location = new Point(180, 152),
                Size = new Size(190, 32),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(249, 115, 22) // Orange
            };
            checkoutPanel.Controls.Add(lblNetTitle);
            checkoutPanel.Controls.Add(lblGrandTotal);

            // Row 7: 4 Payment Mode Buttons
            btnPayCash = CreatePaymentModeButton("💵\nCash", Color.FromArgb(16, 185, 129), "Cash");
            btnPayUpi = CreatePaymentModeButton("📱\nUPI", Color.FromArgb(139, 92, 246), "UPI");
            btnPayCard = CreatePaymentModeButton("💳\nCard", Color.FromArgb(14, 165, 233), "Card");
            btnPaySplit = CreatePaymentModeButton("🔀\nSplit", Color.FromArgb(249, 115, 22), "Split");

            checkoutPanel.Controls.Add(btnPayCash);
            checkoutPanel.Controls.Add(btnPayUpi);
            checkoutPanel.Controls.Add(btnPayCard);
            checkoutPanel.Controls.Add(btnPaySplit);
            UpdatePaymentModeButtons();

            // Row 8: PAY PRINT Button
            btnPayPrint = new Button
            {
                Text = "🖨️  PAY  PRINT ( Rs. 0.00 )",
                Location = new Point(10, 248),
                Size = new Size(360, 46),
                BackColor = Color.FromArgb(249, 115, 22), // Orange
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPayPrint.FlatAppearance.BorderSize = 0;
            btnPayPrint.Click += BtnSettle_Click;
            btnSettle = btnPayPrint; // Link reference for compatibility
            checkoutPanel.Controls.Add(btnPayPrint);

            // Responsive resize handling
            checkoutPanel.SizeChanged += (s, e) => {
                int pw = checkoutPanel.ClientSize.Width - 20;
                if (pw < 100) return;
                int spacing = 6;
                int cancelBtnW = Math.Max(75, pw * 25 / 100);
                int noteBtnW = Math.Max(65, pw * 23 / 100);
                int kotBtnW = pw - cancelBtnW - noteBtnW - (spacing * 2);

                btnPrintKot.Location = new Point(10, 6);
                btnPrintKot.Size = new Size(kotBtnW, 28);
                btnKotComment.Location = new Point(10 + kotBtnW + spacing, 6);
                btnKotComment.Size = new Size(noteBtnW, 28);
                btnCancelOrder.Location = new Point(10 + kotBtnW + spacing + noteBtnW + spacing, 6);
                btnCancelOrder.Size = new Size(cancelBtnW, 28);

                lblSubTotal.Location = new Point(10 + pw - 130, 38);
                lblDiscount.Location = new Point(10 + pw - 130, 68);
                lblTaxableVal.Location = new Point(10 + pw - 130, 94);
                lblTax.Location = new Point(10 + pw - 130, 120);

                netLine.Width = pw;
                lblGrandTotal.Location = new Point(10 + pw - 190, 152);

                int btnSpacing = 6;
                int btnW = (pw - (3 * btnSpacing)) / 4;
                btnPayCash.Location = new Point(10 + 0 * (btnW + btnSpacing), 190);
                btnPayCash.Size = new Size(btnW, 48);

                btnPayUpi.Location = new Point(10 + 1 * (btnW + btnSpacing), 190);
                btnPayUpi.Size = new Size(btnW, 48);

                btnPayCard.Location = new Point(10 + 2 * (btnW + btnSpacing), 190);
                btnPayCard.Size = new Size(btnW, 48);

                btnPaySplit.Location = new Point(10 + 3 * (btnW + btnSpacing), 190);
                btnPaySplit.Size = new Size(pw - 3 * (btnW + btnSpacing), 48);

                btnPayPrint.Location = new Point(10, 248);
                btnPayPrint.Size = new Size(pw, 46);
            };

            // Top KOT Header Panel (Order Info + Steward + Quick Counter button)
            kotHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(20, 27, 45),
                Padding = new Padding(8, 6, 8, 6)
            };
            kotHeaderPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, kotHeaderPanel.Height - 1, kotHeaderPanel.Width, kotHeaderPanel.Height - 1);
            };

            lblKotOrderInfo = new Label
            {
                Text = "🍽️ Table 1 • Dining",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(8, 12),
                AutoSize = true
            };
            kotHeaderPanel.Controls.Add(lblKotOrderInfo);

            btnQuickCounter = new Button
            {
                Text = "🏪 Counter",
                Size = new Size(82, 32),
                Location = new Point(kotHeaderPanel.Width - 90, 7),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnQuickCounter.FlatAppearance.BorderSize = 1;
            btnQuickCounter.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnQuickCounter.Click += (s, e) => {
                cmbSteward.SelectedItem = "Direct Counter";
                UpdateStewardButtonText();
            };
            kotHeaderPanel.Controls.Add(btnQuickCounter);

            btnKotSteward = new Button
            {
                Text = "👤 Steward ▾",
                Size = new Size(130, 32),
                Location = new Point(kotHeaderPanel.Width - 226, 7),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnKotSteward.FlatAppearance.BorderSize = 1;
            btnKotSteward.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnKotSteward.Click += (s, e) => ShowStewardMenu(btnKotSteward);
            kotHeaderPanel.Controls.Add(btnKotSteward);

            kotHeaderPanel.SizeChanged += (s, e) => {
                int w = kotHeaderPanel.ClientSize.Width;
                btnQuickCounter.Location = new Point(w - 90, 7);
                btnKotSteward.Location = new Point(w - 226, 7);
            };

            rightOrderPanel.Controls.Add(kotHeaderPanel);
            rightOrderPanel.Controls.Add(checkoutPanel);

            // Items List Container
            kotItemsContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(4)
            };
            rightOrderPanel.Controls.Add(kotItemsContainer);

            kotHeaderPanel.SendToBack();
            checkoutPanel.SendToBack();
            kotItemsContainer.BringToFront();
        }

        private Button CreatePaymentModeButton(string text, Color bg, string method)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Emoji", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                selectedPaymentMethod = method;
                UpdatePaymentModeButtons();
            };
            return btn;
        }

        private void UpdatePaymentModeButtons()
        {
            Button[] buttons = new[] { btnPayCash, btnPayUpi, btnPayCard, btnPaySplit };
            string[] modes = new[] { "Cash", "UPI", "Card", "Split" };
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                bool isSelected = string.Equals(selectedPaymentMethod, modes[i], StringComparison.OrdinalIgnoreCase);
                buttons[i].FlatAppearance.BorderSize = isSelected ? 2 : 0;
                buttons[i].FlatAppearance.BorderColor = Color.White;
            }
        }

        private void RecalculateDiscountFromInputs()
        {
            decimal foodGross = cartItems.Sum(x => x.LineTotal);

            string text = txtDiscountVal?.Text?.Trim() ?? "0";
            if (string.IsNullOrEmpty(text)) text = "0";

            if (decimal.TryParse(text, out decimal val) && val >= 0)
            {
                if (comboDiscountType?.SelectedItem?.ToString() == "%")
                {
                    currentDiscountAmount = Math.Round((foodGross * val) / 100m, 2);
                }
                else
                {
                    currentDiscountAmount = Math.Min(foodGross, val);
                }
            }
            else
            {
                currentDiscountAmount = 0m;
            }
        }

        private void UpdateSummaryDisplay()
        {
            decimal foodGross = cartItems.Sum(x => x.LineTotal);
            decimal discountVal = Math.Min(foodGross, currentDiscountAmount);
            decimal discountedFood = Math.Max(0, foodGross - discountVal);

            // Food tax is calculated on food gross after discount
            decimal taxableFood = Math.Round(discountedFood / 1.05m, 2);
            decimal totalGst = discountedFood - taxableFood;

            // Zero tax on packaging charges:
            // Packaging fee is added flat without any tax deduction.
            decimal totalTaxable = taxableFood + currentPackingCharge;
            decimal grandTotal = Math.Round(discountedFood + currentPackingCharge, 0);

            if (lblSubTotal != null) lblSubTotal.Text = $"Rs. {(foodGross + currentPackingCharge):0.00}";
            if (lblDiscount != null) lblDiscount.Text = $"- Rs. {discountVal:0.00}";
            if (lblTaxableVal != null) lblTaxableVal.Text = $"Rs. {totalTaxable:0.00}";
            if (lblTax != null) lblTax.Text = $"Rs. {totalGst:0.00}";
            if (lblGrandTotal != null) lblGrandTotal.Text = $"Rs. {grandTotal:0.00}";
            if (btnPayPrint != null) btnPayPrint.Text = $"🖨️  PAY  PRINT ( Rs. {grandTotal:0.00} )";
        }

        private void OpenDiscountDialog()
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Please add items to the order before applying a discount.", "Order Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal foodGross = cartItems.Sum(x => x.LineTotal);

            using (DiscountDialog dlg = new DiscountDialog(foodGross, currentDiscountAmount, currentDiscountReason))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (dlg.IsDiscountRemoved)
                    {
                        currentDiscountAmount = 0m;
                        currentDiscountReason = "";
                        isUpdatingDiscount = true;
                        if (txtDiscountVal != null) txtDiscountVal.Text = "0";
                        if (comboDiscountType != null) comboDiscountType.SelectedIndex = 0;
                        isUpdatingDiscount = false;
                    }
                    else
                    {
                        currentDiscountAmount = dlg.DiscountAmount;
                        currentDiscountReason = dlg.DiscountReason;
                        isUpdatingDiscount = true;
                        if (comboDiscountType != null) comboDiscountType.SelectedIndex = 1; // "Rs."
                        if (txtDiscountVal != null) txtDiscountVal.Text = currentDiscountAmount.ToString("0.##");
                        isUpdatingDiscount = false;
                    }
                    UpdateSummaryDisplay();
                }
            }
        }

        private void ShowStewardMenu(Control anchor)
        {
            if (stewardMenu == null) return;
            stewardMenu.Items.Clear();
            foreach (var it in cmbSteward.Items)
            {
                string stwdName = it.ToString();
                var mi = new ToolStripMenuItem(stwdName);
                mi.ForeColor = Color.White;
                mi.BackColor = Color.FromArgb(20, 27, 42);
                bool isSel = (cmbSteward.SelectedItem?.ToString() == stwdName);
                if (isSel)
                {
                    mi.Text = $"✓  {stwdName}";
                    mi.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    mi.ForeColor = Theme.Accent;
                }
                else
                {
                    mi.Text = $"    {stwdName}";
                    mi.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                }
                mi.Click += (ms, me) => {
                    cmbSteward.SelectedItem = stwdName;
                    UpdateStewardButtonText();
                };
                stewardMenu.Items.Add(mi);
            }
            stewardMenu.Show(anchor, new Point(0, anchor.Height));
        }

        private void UpdateStewardButtonText()
        {
            string selected = cmbSteward.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) selected = "Direct Counter";

            bool isCounter = (selected == "Direct Counter");

            if (btnSteward != null)
            {
                btnSteward.Text = isCounter ? "🏪 Counter ▾" : $"👤 {selected} ▾";
                btnSteward.BackColor = isCounter ? Color.FromArgb(24, 32, 50) : Color.FromArgb(30, 41, 59);
            }

            if (btnKotSteward != null)
            {
                btnKotSteward.Text = isCounter ? "🏪 Counter ▾" : $"👤 {selected} ▾";
                btnKotSteward.ForeColor = isCounter ? Theme.Accent : Color.White;
            }

            if (btnQuickCounter != null)
            {
                btnQuickCounter.BackColor = isCounter ? Theme.Accent : Color.FromArgb(30, 41, 59);
                btnQuickCounter.ForeColor = isCounter ? Color.Black : Color.White;
            }
        }

        private void LoadStewards()
        {
            try
            {
                cmbSteward.Items.Clear();
                cmbSteward.Items.Add("Direct Counter");

                if (_cachedStewards == null)
                {
                    _cachedStewards = new List<string>();
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Staff WHERE IsActive = 1 AND Name <> 'Direct Counter' ORDER BY Name", conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                _cachedStewards.Add(r["Name"].ToString());
                            }
                        }
                    }
                }

                foreach (string s in _cachedStewards)
                {
                    cmbSteward.Items.Add(s);
                }

                if (cmbSteward.Items.Count > 0 && cmbSteward.SelectedIndex < 0)
                    cmbSteward.SelectedIndex = 0;

                UpdateStewardButtonText();
            }
            catch { }
        }

        private void LoadCategories()
        {
            try
            {
                categoryTabsPanel.SuspendLayout();
                categoryTabsPanel.Controls.Clear();

                Button btnAll = CreateCategoryTabBtn("All", "All", true);
                var catButtons = new List<Control> { btnAll };

                if (_cachedCategories == null)
                {
                    _cachedCategories = new List<string>();
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Categories ORDER BY Name", conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                _cachedCategories.Add(r["Name"].ToString());
                            }
                        }
                    }
                }

                foreach (string cat in _cachedCategories)
                {
                    catButtons.Add(CreateCategoryTabBtn(cat, cat, false));
                }

                categoryTabsPanel.Controls.AddRange(catButtons.ToArray());
                categoryTabsPanel.ResumeLayout(true);
            }
            catch { }
        }

        private void HandleCategoryViewportHover(Point screenPt)
        {
            if (catViewport == null || categoryTabsPanel == null || categoryTabsPanel.Width <= catViewport.Width)
            {
                catScrollSpeed = 0;
                if (catScrollTimer != null && catScrollTimer.Enabled) catScrollTimer.Stop();
                return;
            }

            Point clientPt = catViewport.PointToClient(screenPt);
            int edgeThreshold = 140; // Zone from left and right edges where hover triggers auto-scroll

            if (clientPt.X >= catViewport.Width - edgeThreshold && clientPt.X <= catViewport.Width && clientPt.Y >= 0 && clientPt.Y <= catViewport.Height)
            {
                float factor = (float)(clientPt.X - (catViewport.Width - edgeThreshold)) / edgeThreshold;
                catScrollSpeed = -(int)Math.Max(4, factor * 14);
                if (catScrollTimer != null && !catScrollTimer.Enabled) catScrollTimer.Start();
            }
            else if (clientPt.X <= edgeThreshold && clientPt.X >= 0 && clientPt.Y >= 0 && clientPt.Y <= catViewport.Height)
            {
                float factor = (float)(edgeThreshold - clientPt.X) / edgeThreshold;
                catScrollSpeed = (int)Math.Max(4, factor * 14);
                if (catScrollTimer != null && !catScrollTimer.Enabled) catScrollTimer.Start();
            }
            else
            {
                catScrollSpeed = 0;
                if (catScrollTimer != null && catScrollTimer.Enabled) catScrollTimer.Stop();
            }
        }

        private Button CreateCategoryTabBtn(string text, string catName, bool isActive)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 34,
                Padding = new Padding(14, 0, 14, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = isActive ? Theme.Accent : Color.FromArgb(24, 33, 47),
                ForeColor = isActive ? Color.White : Color.FromArgb(148, 163, 184),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0),
                Tag = catName
            };
            btn.FlatAppearance.BorderSize = isActive ? 0 : 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.MouseMove += (s, e) => HandleCategoryViewportHover(Cursor.Position);
            btn.MouseEnter += (s, e) => {
                if (btn.Tag?.ToString() != selectedCategory)
                {
                    btn.BackColor = Color.FromArgb(30, 41, 59);
                    btn.ForeColor = Color.White;
                }
                HandleCategoryViewportHover(Cursor.Position);
            };
            btn.MouseLeave += (s, e) => {
                if (btn.Tag?.ToString() != selectedCategory)
                {
                    btn.BackColor = Color.FromArgb(24, 33, 47);
                    btn.ForeColor = Color.FromArgb(148, 163, 184);
                }
            };
            btn.Click += (s, e) => {
                selectedCategory = catName;
                foreach (Control c in categoryTabsPanel.Controls)
                {
                    if (c is Button b)
                    {
                        bool isSel = (b.Tag?.ToString() == catName);
                        b.BackColor = isSel ? Theme.Accent : Color.FromArgb(24, 33, 47);
                        b.ForeColor = isSel ? Color.White : Color.FromArgb(148, 163, 184);
                        b.FlatAppearance.BorderSize = isSel ? 0 : 1;
                    }
                }
                LoadProducts(txtSearchItem.Text.Trim(), selectedCategory);
            };
            return btn;
        }

        private void LoadProducts(string query = "", string category = "All")
        {
            try
            {
                if (_cachedProducts == null)
                {
                    _cachedProducts = new List<CachedProduct>();
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string sql = "SELECT Id, Code, Name, Category, SalesPrice, ISNULL(GSTRate, 5.00) AS GSTRate FROM Products ORDER BY Name";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string name = r["Name"].ToString();
                                _cachedProducts.Add(new CachedProduct
                                {
                                    Id = Convert.ToInt32(r["Id"]),
                                    Code = r["Code"]?.ToString() ?? "",
                                    Name = name,
                                    Category = r["Category"]?.ToString() ?? "",
                                    SalesPrice = Convert.ToDecimal(r["SalesPrice"]),
                                    GstRate = Convert.ToDecimal(r["GSTRate"]),
                                    IsVeg = !name.ToLower().Contains("chicken") && !name.ToLower().Contains("non veg") && !name.ToLower().Contains("egg")
                                });
                            }
                        }
                    }
                }

                IEnumerable<CachedProduct> filtered = _cachedProducts;
                if (!string.IsNullOrEmpty(category) && category != "All")
                {
                    filtered = filtered.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(query))
                {
                    filtered = filtered.Where(p => (p.Name != null && p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                                                || (p.Code != null && p.Code.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                var list = filtered.ToList();

                productGridPanel.SuspendLayout();
                productGridPanel.Controls.Clear();

                int scrollbarW = SystemInformation.VerticalScrollBarWidth + 8;
                int availW = productGridPanel.ClientSize.Width - productGridPanel.Padding.Horizontal - scrollbarW;
                if (availW < 180) availW = 360;
                int cols = Math.Max(2, Math.Min(6, availW / 180));
                int tileW = Math.Max(125, (availW / cols) - 10);
                int tileH = 105;

                var tiles = new Control[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    tiles[i] = new ProductTile(p, tileW, tileH, () => {
                        int qty = quickQty;
                        AddToCart(p.Id, p.Code, p.Name, p.Category, p.SalesPrice, p.GstRate, qty);
                        quickQty = 1;
                        if (lblQtyVal != null) lblQtyVal.Text = "1";
                    });
                }

                productGridPanel.Controls.AddRange(tiles);
                productGridPanel.ResumeLayout(true);
            }
            catch
            {
                productGridPanel.ResumeLayout(true);
            }
        }

        public class ProductTile : Control
        {
            public CachedProduct Product { get; private set; }
            private readonly Action onAddAction;
            private bool isHovered = false;

            private static readonly Font CatFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            private static readonly Font NameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            private static readonly Font PriceFont = new Font("Segoe UI", 11F, FontStyle.Bold);
            private static readonly Font AddFont = new Font("Segoe UI", 8F, FontStyle.Bold);

            private static readonly Color BgColor = Color.FromArgb(24, 33, 47);
            private static readonly Color HoverBgColor = Color.FromArgb(30, 41, 59);
            private static readonly Color BorderColor = Color.FromArgb(51, 65, 85);
            private static readonly Color VegColor = Color.FromArgb(16, 185, 129);
            private static readonly Color NonVegColor = Color.FromArgb(239, 68, 68);

            public ProductTile(CachedProduct product, int width, int height, Action onAdd)
            {
                Product = product;
                onAddAction = onAdd;

                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Size = new Size(width, height);
                Margin = new Padding(6);
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                isHovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                isHovered = false;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
                {
                    onAddAction?.Invoke();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

                // Background & Border
                using (GraphicsPath path = Theme.GetRoundedPath(rect, 8))
                using (SolidBrush bgBrush = new SolidBrush(isHovered ? HoverBgColor : BgColor))
                using (Pen borderPen = new Pen(isHovered ? Theme.Accent : BorderColor, isHovered ? 2 : 1))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }

                // Veg / Non-Veg icon at (10, 10)
                Rectangle iconBox = new Rectangle(10, 10, 14, 14);
                Color iconColor = Product.IsVeg ? VegColor : NonVegColor;
                using (Pen p = new Pen(iconColor, 1.5f))
                    g.DrawRectangle(p, iconBox);
                using (SolidBrush b = new SolidBrush(iconColor))
                    g.FillEllipse(b, iconBox.X + 3, iconBox.Y + 3, 8, 8);

                // Category tag (top right)
                if (!string.IsNullOrEmpty(Product.Category))
                {
                    Rectangle catRect = new Rectangle(30, 8, Width - 40, 16);
                    TextRenderer.DrawText(g, Product.Category.ToUpper(), CatFont, catRect, Color.FromArgb(148, 163, 184),
                        TextFormatFlags.Right | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
                }

                // Dish Name
                Rectangle nameRect = new Rectangle(10, 28, Width - 20, 42);
                TextRenderer.DrawText(g, Product.Name, NameFont, nameRect, Color.FromArgb(248, 250, 252),
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);

                // Price (bottom left)
                string priceText = $"₹{Product.SalesPrice:0}";
                Rectangle priceRect = new Rectangle(10, Height - 30, Width / 2, 24);
                TextRenderer.DrawText(g, priceText, PriceFont, priceRect, Theme.Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // "+ ADD" badge (bottom right)
                int addW = 54;
                int addH = 22;
                Rectangle addRect = new Rectangle(Width - addW - 10, Height - addH - 7, addW, addH);
                using (GraphicsPath addPath = Theme.GetRoundedPath(addRect, 4))
                using (SolidBrush addBg = new SolidBrush(Color.FromArgb(25, 16, 185, 129)))
                using (Pen addPen = new Pen(Color.FromArgb(16, 185, 129), 1))
                {
                    g.FillPath(addBg, addPath);
                    g.DrawPath(addPen, addPath);
                }
                TextRenderer.DrawText(g, "+ ADD", AddFont, addRect, Color.FromArgb(16, 185, 129),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void AddToCart(int id, string code, string name, string cat, decimal price, decimal gst, int qty)
        {
            // Find in uncommitted new items
            var existing = cartItems.FirstOrDefault(x => x.ProductId == id && !x.IsCommittedToKot);
            if (existing != null)
            {
                existing.Quantity += qty;
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    ProductId = id,
                    ItemCode = code,
                    ItemName = name,
                    Category = cat,
                    UnitPrice = price,
                    GstRate = gst,
                    Quantity = qty,
                    IsCommittedToKot = false
                });
            }

            RefreshOrderCartView();
        }

        private void RefreshOrderCartView()
        {
            kotItemsContainer.SuspendLayout();
            kotItemsContainer.Controls.Clear();

            int totalQty = 0;
            decimal totalGross = 0;

            int panelW = Math.Max(280, kotItemsContainer.ClientSize.Width - 12);
            if (panelW < 200) panelW = 350;

            // Group by KOT
            var kotGroups = cartItems.GroupBy(x => x.KotNumber).OrderBy(g => g.Key);

            foreach (var group in kotGroups)
            {
                int kotNum = group.Key;
                bool isNewKot = (kotNum == 0);

                // Header Banner for this KOT
                Panel kotHeader = new Panel
                {
                    Width = panelW,
                    Height = 28,
                    BackColor = isNewKot ? Color.FromArgb(30, 41, 59) : Color.FromArgb(67, 56, 202),
                    Margin = new Padding(0, 4, 0, 4)
                };

                Label lblKotTitle = new Label
                {
                    Text = isNewKot ? "⭐ New Items (Unsent to Kitchen)" : $"🍳 KOT #{kotNum} (In Kitchen)",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(8, 5),
                    AutoSize = true
                };
                kotHeader.Controls.Add(lblKotTitle);
                kotItemsContainer.Controls.Add(kotHeader);

                foreach (var item in group)
                {
                    totalQty += item.Quantity;
                    totalGross += item.LineTotal;

                    bool hasInstruction = !string.IsNullOrEmpty(item.SpecialInstructions);
                    int rowHeight = hasInstruction ? 62 : 44;

                    Panel itemRow = new Panel
                    {
                        Width = panelW,
                        Height = rowHeight,
                        BackColor = Color.FromArgb(24, 33, 47),
                        Margin = new Padding(0, 0, 0, 2)
                    };

                    itemRow.Paint += (s, e) => {
                        using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1))
                            e.Graphics.DrawLine(p, 0, itemRow.Height - 1, itemRow.Width, itemRow.Height - 1);
                    };

                    // Delete / Void Icon Button
                    Button btnDel = new Button
                    {
                        Text = "🗑️",
                        Size = new Size(24, 24),
                        Location = new Point(4, 9),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = Theme.Danger,
                        Cursor = Cursors.Hand
                    };
                    btnDel.FlatAppearance.BorderSize = 0;
                    btnDel.Click += (s, e) => RemoveOrVoidItem(item);
                    itemRow.Controls.Add(btnDel);

                    // Parcel / Special Instruction Icon Button
                    Button btnParcel = new Button
                    {
                        Text = hasInstruction ? "🛍️" : "🏷️",
                        Size = new Size(26, 24),
                        Location = new Point(30, 9),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = hasInstruction ? Color.FromArgb(60, 50, 20) : Color.Transparent,
                        ForeColor = hasInstruction ? Color.FromArgb(251, 191, 36) : Color.FromArgb(148, 163, 184),
                        Font = new Font("Segoe UI Emoji", 8.5F),
                        Cursor = Cursors.Hand
                    };
                    btnParcel.FlatAppearance.BorderSize = hasInstruction ? 1 : 0;
                    btnParcel.FlatAppearance.BorderColor = Color.FromArgb(251, 191, 36);
                    btnParcel.Click += (s, e) => ShowItemParcelMenu(btnParcel, item);
                    itemRow.Controls.Add(btnParcel);

                    // Name
                    Label lblName = new Label
                    {
                        Text = item.ItemName,
                        Location = new Point(58, 11),
                        Size = new Size(panelW - 195, 20),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoEllipsis = true,
                        Cursor = Cursors.Hand
                    };
                    lblName.Click += (s, e) => ShowItemParcelMenu(btnParcel, item);
                    itemRow.Controls.Add(lblName);

                    // Qty Controls
                    Label lblQtyVal = new Label
                    {
                        Text = item.Quantity.ToString(),
                        Location = new Point(panelW - 130, 11),
                        Size = new Size(35, 22),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = Theme.BoldFont,
                        ForeColor = Theme.Accent
                    };
                    itemRow.Controls.Add(lblQtyVal);

                    // Line Taxable Amount
                    Label lblAmt = new Label
                    {
                        Text = $"₹{item.LineTaxable:0.00}",
                        Location = new Point(panelW - 90, 11),
                        Size = new Size(85, 22),
                        TextAlign = ContentAlignment.MiddleRight,
                        Font = Theme.BoldFont,
                        ForeColor = Color.White
                    };
                    itemRow.Controls.Add(lblAmt);

                    if (hasInstruction)
                    {
                        Label lblInstruction = new Label
                        {
                            Text = $"🛍️ {item.SpecialInstructions}",
                            Location = new Point(58, 36),
                            Size = new Size(panelW - 150, 18),
                            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                            ForeColor = Color.FromArgb(251, 191, 36),
                            AutoEllipsis = true,
                            Cursor = Cursors.Hand
                        };
                        lblInstruction.Click += (s, e) => ShowItemParcelMenu(btnParcel, item);
                        itemRow.Controls.Add(lblInstruction);
                    }

                    kotItemsContainer.Controls.Add(itemRow);
                }
            }

            // Add Packaging Charges if applicable (for both Takeaway and Dining leftover/parcel)
            if (currentPackingCharge > 0)
            {
                totalGross += currentPackingCharge;
                totalQty += 1;

                Panel packRow = new Panel
                {
                    Width = panelW,
                    Height = 36,
                    BackColor = Color.FromArgb(25, 35, 55),
                    Margin = new Padding(0, 4, 0, 2)
                };

                Label lblPackName = new Label
                {
                    Text = "📦 Packaging Charges",
                    Location = new Point(10, 9),
                    AutoSize = true,
                    Font = Theme.BoldFont,
                    ForeColor = Theme.Warning
                };
                packRow.Controls.Add(lblPackName);

                // Zero tax on packaging: display full packaging fee directly without tax deduction
                Label lblPackAmt = new Label
                {
                    Text = $"₹{currentPackingCharge:0.00}",
                    Location = new Point(panelW - 90, 9),
                    Size = new Size(85, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = Theme.BoldFont,
                    ForeColor = Theme.Warning
                };
                packRow.Controls.Add(lblPackAmt);

                kotItemsContainer.Controls.Add(packRow);
            }

            kotItemsContainer.ResumeLayout();

            // Calculate & update summary display
            RecalculateDiscountFromInputs();
            UpdateSummaryDisplay();
        }

        private void RemoveOrVoidItem(CartItem item)
        {
            if (!item.IsCommittedToKot)
            {
                cartItems.Remove(item);
                RefreshOrderCartView();
            }
            else
            {
                // Item is already printed in kitchen! Prompt Void Dialog
                using (VoidKotDialog dlg = new VoidKotDialog(item.ItemName, item.Quantity))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string reason = !string.IsNullOrWhiteSpace(dlg.Comment) ? dlg.Comment : (dlg.SelectedReason ?? "Item Voided");

                            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                            {
                                conn.Open();
                                string sql = @"
                                    UPDATE KOTDetails 
                                    SET IsVoided = 1, VoidReason = @reason, VoidedAt = GETDATE()
                                    WHERE KOTId = @kotId AND ItemName = @name;

                                    -- If all items in this KOT ticket are now voided, mark KOTMaster as Voided too
                                    IF NOT EXISTS (SELECT 1 FROM KOTDetails WHERE KOTId = @kotId AND IsVoided = 0)
                                    BEGIN
                                        UPDATE KOTMaster 
                                        SET Status = 'Voided', IsVoided = 1, VoidReason = @reason, VoidedAt = GETDATE()
                                        WHERE Id = @kotId;
                                    END";

                                using (SqlCommand cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@reason", reason);
                                    cmd.Parameters.AddWithValue("@kotId", item.KotId);
                                    cmd.Parameters.AddWithValue("@name", item.ItemName);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (dlg.ShouldPrintSlip)
                            {
                                ThermalReceiptPrinter.PrintVoidKOT(item.KotId, reason);
                            }

                            cartItems.Remove(item);
                            RefreshOrderCartView();
                            UpdateTableSummaryInDb();
                            MainForm.Instance?.RefreshLiveOrderCounts();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to void item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void BtnCancelOrder_Click(object sender, EventArgs e)
        {
            CancelCurrentOrderOrVoidKot();
        }

        public void CancelCurrentOrderOrVoidKot()
        {
            if (cartItems.Count == 0)
            {
                // Also check if there's any active KOT on this table in DB
                bool hasActiveKotInDb = false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT COUNT(1) FROM KOTMaster 
                            WHERE TableNumber = @tNum 
                              AND Status IN ('Active', 'Served', 'Printed')
                              AND (OrderType = @type OR (@type = 'DINING' AND (OrderType IS NULL OR OrderType = '')))", conn))
                        {
                            cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                            cmd.Parameters.AddWithValue("@type", ActiveOrderType);
                            hasActiveKotInDb = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                        }
                    }
                }
                catch { }

                if (!hasActiveKotInDb)
                {
                    MessageBox.Show("There is no active order or KOT on this table to cancel.", "No Active Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            bool hasCommittedKots = cartItems.Any(x => x.IsCommittedToKot);
            if (!hasCommittedKots)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT COUNT(1) FROM KOTMaster 
                            WHERE TableNumber = @tNum 
                              AND Status IN ('Active', 'Served', 'Printed')", conn))
                        {
                            cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                            hasCommittedKots = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                        }
                    }
                }
                catch { }
            }

            if (!hasCommittedKots)
            {
                // Unsaved cart only: Simple confirmation
                var confirm = MessageBox.Show(
                    $"Cancel unsaved items and clear {ActiveOrderType} order for Table/Token {ActiveTableNumber}?",
                    "Clear Unsaved Order",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    cartItems.Clear();
                    RefreshOrderCartView();
                    UpdateTableSummaryInDb();
                    OnNavigateToFloor?.Invoke();
                }
                return;
            }

            // Committed KOT exists! Prompt Void Dialog with Reason
            int totalQty = cartItems.Where(x => x.IsCommittedToKot).Sum(x => x.Quantity);
            decimal totalAmt = cartItems.Where(x => x.IsCommittedToKot).Sum(x => x.LineTotal);
            string summaryTitle = $"Table {ActiveTableNumber} ({cartItems.Count} items, ₹{totalAmt:N0})";

            using (VoidKotDialog dlg = new VoidKotDialog(summaryTitle, totalQty > 0 ? totalQty : 1))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string reason = !string.IsNullOrWhiteSpace(dlg.Comment) ? dlg.Comment : (dlg.SelectedReason ?? "Order Cancelled by Customer");
                    List<int> affectedKotIds = new List<int>();

                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // 1. Find all active KOT IDs for this table
                                    string findKotsSql = @"
                                        SELECT Id FROM KOTMaster 
                                        WHERE TableNumber = @tNum 
                                          AND Status IN ('Active', 'Served', 'Printed')
                                          AND (OrderType = @type OR (@type = 'DINING' AND (OrderType IS NULL OR OrderType = '')))";

                                    using (SqlCommand cmd = new SqlCommand(findKotsSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.Parameters.AddWithValue("@type", ActiveOrderType);
                                        using (SqlDataReader rdr = cmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                affectedKotIds.Add(Convert.ToInt32(rdr["Id"]));
                                            }
                                        }
                                    }

                                    // 2. Update KOTDetails to Voided with Reason
                                    if (affectedKotIds.Count > 0)
                                    {
                                        string kotIdList = string.Join(",", affectedKotIds);
                                        string updateDetailsSql = $@"
                                            UPDATE KOTDetails
                                            SET IsVoided = 1,
                                                VoidReason = @reason,
                                                VoidedAt = GETDATE()
                                            WHERE KOTId IN ({kotIdList}) AND IsVoided = 0";

                                        using (SqlCommand cmd = new SqlCommand(updateDetailsSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@reason", reason);
                                            cmd.ExecuteNonQuery();
                                        }

                                        // 3. Update KOTMaster to Voided
                                        string updateMasterSql = $@"
                                            UPDATE KOTMaster
                                            SET Status = 'Voided',
                                                IsVoided = 1,
                                                VoidReason = @reason,
                                                VoidedAt = GETDATE(),
                                                KotComment = ISNULL(KotComment + ' | ', '') + 'CANCELLED: ' + @reason
                                            WHERE Id IN ({kotIdList})";

                                        using (SqlCommand cmd = new SqlCommand(updateMasterSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@reason", reason);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    // 4. Reset CafeTables row
                                    string resetTableSql = @"
                                        UPDATE CafeTables
                                        SET Status = 'Available',
                                            CurrentBillAmount = 0.00,
                                            ActiveKotNumbers = NULL,
                                            CurrentSteward = NULL,
                                            OrderStartTime = NULL
                                        WHERE TableNumber = @tNum";

                                    using (SqlCommand cmd = new SqlCommand(resetTableSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.ExecuteNonQuery();
                                    }

                                    trans.Commit();
                                }
                                catch
                                {
                                    trans.Rollback();
                                    throw;
                                }
                            }
                        }

                        // 5. Print Void KOT slip to Kitchen Printer if requested
                        if (dlg.ShouldPrintSlip && affectedKotIds.Count > 0)
                        {
                            foreach (int kId in affectedKotIds)
                            {
                                try
                                {
                                    ThermalReceiptPrinter.PrintVoidKOT(kId, reason);
                                }
                                catch { }
                            }
                        }

                        // 6. Clear local cart & refresh views
                        cartItems.Clear();
                        currentKotComment = "";
                        currentDiscountAmount = 0;
                        RefreshOrderCartView();

                        MainForm.Instance?.RefreshLiveOrderCounts();

                        MessageBox.Show(
                            $"Order for Table {ActiveTableNumber} has been successfully CANCELLED and VOIDED.\n\nReason: {reason}\nAudit record saved to KOT & Reports Register.\nTable is now Available.",
                            "Order Cancelled & Voided",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        OnNavigateToFloor?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to cancel order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnKotComment_Click(object sender, EventArgs e)
        {
            string comment = Microsoft.VisualBasic.Interaction.InputBox("Enter Special Cooking / KOT Instruction for Kitchen:", "KOT Comment", currentKotComment);
            if (!string.IsNullOrEmpty(comment))
            {
                currentKotComment = comment;
                btnKotComment.Text = $"📝 Note: {comment}";
            }
        }

        private void BtnPrintKot_Click(object sender, EventArgs e)
        {
            var uncommitted = cartItems.Where(x => !x.IsCommittedToKot).ToList();
            if (uncommitted.Count == 0)
            {
                MessageBox.Show("All items have already been sent to the kitchen.", "No New Items", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int newKotId = 0;
                int nextKotNumber = 1;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Get Next KOT Number
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(KOTNumber), 0) + 1 FROM KOTMaster", conn))
                    {
                        nextKotNumber = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Insert KOT Master
                    string steward = cmbSteward.SelectedItem?.ToString() ?? "Direct Counter";
                    string insKotSql = @"
                        INSERT INTO KOTMaster (KOTNumber, TableNumber, OrderType, Steward, Status, KotComment, CreatedAt)
                        VALUES (@num, @tNum, @type, @stwd, 'Active', @note, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(insKotSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@num", nextKotNumber);
                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                        cmd.Parameters.AddWithValue("@type", ActiveOrderType);
                        cmd.Parameters.AddWithValue("@stwd", steward);
                        cmd.Parameters.AddWithValue("@note", (object)currentKotComment ?? DBNull.Value);
                        newKotId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Insert KOT Details
                    foreach (var it in uncommitted)
                    {
                        string insDetSql = @"
                            INSERT INTO KOTDetails (KOTId, ProductId, ItemName, Quantity, Rate, Amount, Instructions)
                            VALUES (@kotId, @pid, @name, @qty, @rate, @amt, @ins)";

                        using (SqlCommand cmd = new SqlCommand(insDetSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@kotId", newKotId);
                            cmd.Parameters.AddWithValue("@pid", it.ProductId > 0 ? (object)it.ProductId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@name", it.ItemName);
                            cmd.Parameters.AddWithValue("@qty", it.Quantity);
                            cmd.Parameters.AddWithValue("@rate", it.UnitPrice);
                            cmd.Parameters.AddWithValue("@amt", it.LineTotal);
                            cmd.Parameters.AddWithValue("@ins", (object)it.SpecialInstructions ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        it.KotId = newKotId;
                        it.KotNumber = nextKotNumber;
                        it.IsCommittedToKot = true;
                    }
                }

                // Update Table in DB to Running status
                UpdateTableSummaryInDb();

                // Print KOT Slip to Kitchen Printer
                ThermalReceiptPrinter.PrintKOT(newKotId);

                MessageBox.Show($"KOT #{nextKotNumber} printed and sent to kitchen successfully!", "KOT Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshOrderCartView();
                MainForm.Instance?.RefreshLiveOrderCounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending KOT: {ex.Message}", "KOT Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTableSummaryInDb()
        {
            try
            {
                // Only update physical CafeTables if it's a DINING order or Waiting table
                if (ActiveOrderType != "DINING" && !ActiveTableNumber.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                decimal foodGross = cartItems.Sum(x => x.LineTotal);
                decimal netRunning = Math.Max(0, foodGross - currentDiscountAmount) + currentPackingCharge;

                string kots = string.Join(",", cartItems.Where(x => x.KotNumber > 0).Select(x => x.KotNumber.ToString()).Distinct());
                string steward = cmbSteward.SelectedItem?.ToString() ?? "Direct Counter";

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = @"
                        UPDATE CafeTables 
                        SET Status = CASE WHEN @gross > 0 THEN 'Running' ELSE 'Available' END,
                            CurrentBillAmount = @gross,
                            OrderStartTime = ISNULL(OrderStartTime, GETDATE()),
                            ActiveKotNumbers = @kots,
                            CurrentSteward = @stwd
                        WHERE TableNumber = @tNum";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@gross", netRunning);
                        cmd.Parameters.AddWithValue("@kots", (object)kots ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@stwd", (object)steward ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private void BtnSettle_Click(object sender, EventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Cannot settle an empty order.", "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal foodGross = cartItems.Sum(x => x.LineTotal);
            decimal discountVal = Math.Min(foodGross, currentDiscountAmount);
            decimal discountedFood = Math.Max(0, foodGross - discountVal);

            // Food tax is calculated on food gross after discount
            decimal taxableFood = Math.Round(discountedFood / 1.05m, 2);
            decimal totalGst = discountedFood - taxableFood;
            decimal cgst = Math.Round(totalGst / 2.0m, 2);
            decimal sgst = totalGst - cgst;

            // Zero tax on packaging charges:
            decimal totalTaxable = taxableFood + currentPackingCharge;
            decimal grandTotal = Math.Round(discountedFood + currentPackingCharge, 0);
            decimal roundOff = grandTotal - (discountedFood + currentPackingCharge);

            string defaultMethod = selectedPaymentMethod;
            if (defaultMethod == "UPI") defaultMethod = "UPI / QR Pay";

            using (SettlePaymentDialog dlg = new SettlePaymentDialog(grandTotal, "", "", defaultMethod))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int saleId = 0;
                        string settledInvNumber = "";
                        string kots = string.Join(",", cartItems.Where(x => x.KotNumber > 0).Select(x => x.KotNumber.ToString()).Distinct());
                        string steward = cmbSteward.SelectedItem?.ToString() ?? "Direct Counter";

                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // 1. Next Invoice Number (LC/DDmm/001/26-27)
                                    string invNumber = GenerateNextInvoiceNumber(conn, trans);
                                    settledInvNumber = invNumber;

                                    decimal paidAmt = Math.Min(grandTotal, Math.Max(0, dlg.AmountPaid));
                                    decimal dueAmt = Math.Max(0, grandTotal - paidAmt);
                                    decimal cashAmt = (dlg.PaymentMethod == "Cash") ? paidAmt : (dlg.PaymentMethod == "Split" ? dlg.CashAmount : 0m);
                                    decimal onlineAmt = (dlg.PaymentMethod == "Card" || dlg.PaymentMethod == "UPI / QR Pay" || dlg.PaymentMethod == "Online") ? paidAmt : (dlg.PaymentMethod == "Split" ? dlg.OnlineAmount : 0m);

                                    // 2. Insert Sales Record (Packaging charges have 0% GST)
                                    string insSaleSql = @"
                                        INSERT INTO Sales (
                                            InvoiceNumber, SaleDate, SubTotal, Discount, Tax, GrandTotal, AmountPaid, DueAmount, PaymentMethod,
                                            OrderType, TableNumber, KotNumbers, PackingCharges, StewardName, RoundOff, TaxableAmount, CGSTAmount, SGSTAmount, IsGSTBill,
                                            CashAmount, OnlineAmount
                                        ) VALUES (
                                            @inv, GETDATE(), @sub, @disc, @tax, @grand, @paid, @due, @payMethod,
                                            @orderType, @tNum, @kots, @packing, @stwd, @roundOff, @taxable, @cgst, @sgst, 1,
                                            @cashAmt, @onlineAmt
                                        );
                                        SELECT SCOPE_IDENTITY();";

                                    using (SqlCommand cmd = new SqlCommand(insSaleSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@inv", invNumber);
                                        cmd.Parameters.AddWithValue("@sub", totalTaxable);
                                        cmd.Parameters.AddWithValue("@disc", discountVal);
                                        cmd.Parameters.AddWithValue("@tax", totalGst);
                                        cmd.Parameters.AddWithValue("@grand", grandTotal);
                                        cmd.Parameters.AddWithValue("@paid", paidAmt);
                                        cmd.Parameters.AddWithValue("@due", dueAmt);
                                        cmd.Parameters.AddWithValue("@payMethod", dlg.PaymentMethod);
                                        cmd.Parameters.AddWithValue("@cashAmt", cashAmt);
                                        cmd.Parameters.AddWithValue("@onlineAmt", onlineAmt);
                                        cmd.Parameters.AddWithValue("@orderType", ActiveOrderType);
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.Parameters.AddWithValue("@kots", (object)kots ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@packing", currentPackingCharge);
                                        cmd.Parameters.AddWithValue("@stwd", steward);
                                        cmd.Parameters.AddWithValue("@roundOff", roundOff);
                                        cmd.Parameters.AddWithValue("@taxable", totalTaxable);
                                        cmd.Parameters.AddWithValue("@cgst", cgst);
                                        cmd.Parameters.AddWithValue("@sgst", sgst);
                                        saleId = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    // 3. Insert SaleDetails
                                    foreach (var it in cartItems)
                                    {
                                        string insDetSql = @"
                                            INSERT INTO SaleDetails (SaleId, ItemType, ProductId, Quantity, UnitPrice, Total, TaxableAmount, CGSTAmount, SGSTAmount, GSTRate, Instructions)
                                            VALUES (@saleId, 'Product', @pid, @qty, @price, @total, @taxable, @cgst, @sgst, 5.00, @ins)";

                                        using (SqlCommand cmd = new SqlCommand(insDetSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@saleId", saleId);
                                            cmd.Parameters.AddWithValue("@pid", it.ProductId > 0 ? (object)it.ProductId : DBNull.Value);
                                            cmd.Parameters.AddWithValue("@qty", it.Quantity);
                                            cmd.Parameters.AddWithValue("@price", it.UnitPrice);
                                            cmd.Parameters.AddWithValue("@total", it.LineTotal);
                                            cmd.Parameters.AddWithValue("@taxable", it.LineTaxable);
                                            cmd.Parameters.AddWithValue("@cgst", Math.Round(it.LineTax / 2.0m, 2));
                                            cmd.Parameters.AddWithValue("@sgst", it.LineTax - Math.Round(it.LineTax / 2.0m, 2));
                                            cmd.Parameters.AddWithValue("@ins", (object)it.SpecialInstructions ?? DBNull.Value);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    // 4. Add Packing Charges as detail line if present (ZERO TAX)
                                    if (currentPackingCharge > 0)
                                    {
                                        string insPackDet = @"
                                            INSERT INTO SaleDetails (SaleId, ItemType, ProductId, Quantity, UnitPrice, Total, TaxableAmount, CGSTAmount, SGSTAmount, GSTRate, Instructions)
                                            VALUES (@saleId, 'Packaging', NULL, 1, @price, @price, @taxable, 0.00, 0.00, 0.00, 'Packaging Charges')";
                                        using (SqlCommand cmd = new SqlCommand(insPackDet, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@saleId", saleId);
                                            cmd.Parameters.AddWithValue("@price", currentPackingCharge);
                                            cmd.Parameters.AddWithValue("@taxable", currentPackingCharge);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    // 5. Mark KOTs as Billed
                                    string billKotSql = @"
                                        UPDATE KOTMaster 
                                        SET Status = 'Billed', SaleId = @saleId 
                                        WHERE TableNumber = @tNum 
                                          AND (OrderType = @orderType OR (@orderType = 'DINING' AND (OrderType IS NULL OR OrderType = '')))
                                          AND Status IN ('Active', 'Served', 'Printed')";
                                    using (SqlCommand cmd = new SqlCommand(billKotSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@saleId", saleId);
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.Parameters.AddWithValue("@orderType", ActiveOrderType);
                                        cmd.ExecuteNonQuery();
                                    }

                                    // 6. Reset Cafe Table to Available & Consolidate Shared Sub-Tables (for Dining and Waiting tables)
                                    if (ActiveOrderType == "DINING" || ActiveTableNumber.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string resetTableSql = @"
                                            UPDATE CafeTables 
                                            SET Status = 'Available', CurrentBillAmount = 0.00, OrderStartTime = NULL, BilledTime = NULL,
                                                ActiveKotNumbers = NULL, ActiveSaleId = NULL, CurrentSteward = NULL
                                            WHERE TableNumber = @tNum;

                                            -- If this was a shared sub-table (e.g., 2-A or 2-B), check if all sibling sub-tables are now settled
                                            IF CHARINDEX('-', @tNum) > 0
                                            BEGIN
                                                DECLARE @baseNum NVARCHAR(50) = SUBSTRING(@tNum, 1, CHARINDEX('-', @tNum) - 1);
                                                DECLARE @pattern NVARCHAR(55) = @baseNum + '-%';

                                                -- If no sibling sub-table is still running
                                                IF NOT EXISTS (SELECT 1 FROM CafeTables WHERE TableNumber LIKE @pattern AND Status IN ('Running', 'Printed') AND TableNumber <> @tNum)
                                                BEGIN
                                                    -- If the base table row doesn't exist, restore it from this row
                                                    IF NOT EXISTS (SELECT 1 FROM CafeTables WHERE TableNumber = @baseNum)
                                                    BEGIN
                                                        UPDATE CafeTables 
                                                        SET TableNumber = @baseNum, TableName = 'Table ' + @baseNum, Status = 'Available', CurrentBillAmount = 0.00,
                                                            OrderStartTime = NULL, BilledTime = NULL, ActiveKotNumbers = NULL, ActiveSaleId = NULL, CurrentSteward = NULL
                                                        WHERE TableNumber = @tNum;
                                                    END
                                                    ELSE
                                                    BEGIN
                                                        DELETE FROM CafeTables WHERE TableNumber = @tNum;
                                                    END

                                                    -- Clean up any other available sub-table rows for this base table
                                                    DELETE FROM CafeTables WHERE TableNumber LIKE @pattern AND TableNumber <> @baseNum AND Status = 'Available';
                                                END
                                            END";
                                        using (SqlCommand cmd = new SqlCommand(resetTableSql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    trans.Commit();
                                }
                                catch
                                {
                                    trans.Rollback();
                                    throw;
                                }
                            }
                        }

                        // Print Customer Bill
                        if (dlg.ShouldPrintReceipt)
                        {
                            ThermalReceiptPrinter.Print(saleId);
                        }

                        MessageBox.Show($"Invoice {settledInvNumber} settled successfully!", "Settlement Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        cartItems.Clear();
                        currentPackingCharge = 0;
                        currentDiscountAmount = 0m;
                        currentDiscountReason = "";
                        isUpdatingDiscount = true;
                        if (txtDiscountVal != null) txtDiscountVal.Text = "0";
                        if (comboDiscountType != null) comboDiscountType.SelectedIndex = 0;
                        isUpdatingDiscount = false;
                        RefreshOrderCartView();
                        MainForm.Instance?.RefreshLiveOrderCounts();

                        // Navigate back to floor plan
                        OnNavigateToFloor?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error settling bill: {ex.Message}", "Settlement Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnShiftTable_Click(object sender, EventArgs e)
        {
            using (TableShiftDialog dlg = new TableShiftDialog(ActiveTableNumber))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadTableOrder(dlg.SelectedTargetTable, ActiveOrderType);
                }
            }
        }

        public class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkMenuColors()) { }
            private class DarkMenuColors : ProfessionalColorTable
            {
                public override Color MenuItemSelected => Color.FromArgb(45, 55, 75);
                public override Color MenuItemSelectedGradientBegin => Color.FromArgb(45, 55, 75);
                public override Color MenuItemSelectedGradientEnd => Color.FromArgb(45, 55, 75);
                public override Color MenuItemBorder => Color.Transparent;
                public override Color MenuBorder => Color.FromArgb(51, 65, 85);
                public override Color ToolStripDropDownBackground => Color.FromArgb(20, 27, 42);
                public override Color ImageMarginGradientBegin => Color.FromArgb(20, 27, 42);
                public override Color ImageMarginGradientMiddle => Color.FromArgb(20, 27, 42);
                public override Color ImageMarginGradientEnd => Color.FromArgb(20, 27, 42);
            }
        }
    }
}
