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
    public class SalesBillingControl : UserControl
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

        public static string GetNextInvoiceNumberPreview(SqlConnection externalConn = null)
        {
            try
            {
                if (externalConn != null && externalConn.State == System.Data.ConnectionState.Open)
                {
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM Sales", externalConn))
                    {
                        int nextId = Convert.ToInt32(cmd.ExecuteScalar());
                        return "INV-" + nextId;
                    }
                }
                using (var conn = new System.Data.SqlClient.SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM Sales", conn))
                    {
                        int nextId = Convert.ToInt32(cmd.ExecuteScalar());
                        return "INV-" + nextId;
                    }
                }
            }
            catch
            {
                return "INV-1";
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
        private Label lblActiveMode;
        private ComboBox cmbSteward;
        private Button btnSteward;
        private ContextMenuStrip stewardMenu;
        private Button btnBackToFloor;
        private Button btnShiftTable;
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
        private Panel packingChargePanel;
        private Button btnPack10;
        private Button btnPack40;
        private Button btnPack70;
        private Button btnPackCustom;
        private decimal currentPackingCharge = 0.0m;

        // Right Order & KOT Panel Controls
        private FlowLayoutPanel kotItemsContainer;
        private Label lblTotalQty;
        private Label lblSubTotal;
        private Label lblDiscount;
        private Label lblTax;
        private Label lblGrandTotal;
        private Button btnKotComment;
        private Button btnDiscountAction;
        private Button btnPrintKot;
        private Button btnSettle;
        private string currentKotComment = "";
        private decimal currentDiscountAmount = 0.0m;
        private string currentDiscountReason = "";

        // State
        public string ActiveTableNumber { get; private set; } = "5";
        public string ActiveOrderType { get; private set; } = "DINING";
        private List<CartItem> cartItems = new List<CartItem>();
        private string selectedCategory = "All";

        public event Action OnNavigateToFloor;

        public SalesBillingControl()
        {
            InitializeComponent();
            LoadStewards();
            LoadCategories();
            LoadProducts();
            LoadTableOrder(ActiveTableNumber, ActiveOrderType);
        }

        public void LoadTableOrder(string tableNum, string orderType = "DINING")
        {
            ActiveTableNumber = string.IsNullOrEmpty(tableNum) ? "1" : tableNum;
            ActiveOrderType = string.IsNullOrEmpty(orderType) ? "DINING" : orderType;

            packingChargePanel.Visible = (ActiveOrderType == "TAKEAWAY");
            if (ActiveOrderType == "TAKEAWAY" && currentPackingCharge == 0)
            {
                currentPackingCharge = 40.0m; // Default ₹40 takeaway packing charge
            }

            // Load Existing Active KOT Items for this table
            cartItems.Clear();
            currentKotComment = "";
            currentDiscountAmount = 0.0m;
            currentDiscountReason = "";

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
                        WHERE k.TableNumber = @tNum AND k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0
                        ORDER BY k.KOTNumber, kd.Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
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

            UpdateSubBillsHeader();
            RefreshOrderCartView();
        }

        private void UpdateSubBillsHeader()
        {
            if (subBillsFlow == null) return;
            subBillsFlow.SuspendLayout();
            subBillsFlow.Controls.Clear();

            if (ActiveOrderType != "DINING")
            {
                lblActiveMode.Visible = true;
                lblActiveMode.Text = ActiveOrderType == "TAKEAWAY" ? $"🛍️ TAKE AWAY (Token {ActiveTableNumber})" : $"🛵 DELIVERY (Order {ActiveTableNumber})";
                subBillsFlow.Controls.Add(lblActiveMode);
                subBillsFlow.ResumeLayout();
                return;
            }

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
                lblActiveMode.Visible = false;

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
                lblActiveMode.Visible = true;
                lblActiveMode.Text = $"🍽️ Table {ActiveTableNumber}";
                subBillsFlow.Controls.Add(lblActiveMode);
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
                Margin = new Padding(0)
            };
            topLeftFlow.Controls.Add(subBillsFlow);

            lblActiveMode = new Label
            {
                Text = "🍽️ Table 5",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                BackColor = Color.FromArgb(245, 158, 11, 20),
                Height = 36,
                Padding = new Padding(10, 8, 10, 0),
                AutoSize = true,
                Margin = new Padding(0, 2, 6, 0)
            };
            lblActiveMode.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, lblActiveMode.Width - 1, lblActiveMode.Height - 1), 6))
                using (Pen p = new Pen(Theme.Accent, 1))
                    e.Graphics.DrawPath(p, path);
            };

            Button btnShareTableTop = new Button
            {
                Text = "🪑 Share",
                AutoSize = true,
                Height = 36,
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
            btnSteward.Click += (s, e) => {
                stewardMenu.Items.Clear();
                foreach (var it in cmbSteward.Items)
                {
                    string stwdName = it.ToString();
                    var mi = new ToolStripMenuItem(stwdName);
                    mi.ForeColor = Color.White;
                    mi.BackColor = Color.FromArgb(20, 27, 42);
                    if (cmbSteward.SelectedItem?.ToString() == stwdName)
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
                stewardMenu.Show(btnSteward, new Point(0, btnSteward.Height));
            };
            topRightFlow.Controls.Add(btnSteward);

            // Mode Switch Buttons
            btnModeDelivery = CreateTopSwitchButton("DELIVERY", "🛵 Delivery");
            btnModeTakeaway = CreateTopSwitchButton("TAKEAWAY", "🛍️ Takeaway");
            btnModeDining = CreateTopSwitchButton("DINING", "🍽️ Dining");

            topRightFlow.Controls.Add(btnModeDelivery);
            topRightFlow.Controls.Add(btnModeTakeaway);
            topRightFlow.Controls.Add(btnModeDining);

            topBarPanel.Controls.Add(topRightFlow);

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
            txtSearchItem.TextChanged += (s, e) => LoadProducts(txtSearchItem.Text.Trim(), selectedCategory);
            txtWrapper.Controls.Add(txtSearchItem);
            searchBar.Controls.Add(txtWrapper);

            // Ensure proper docking order so textbox fills all remaining space and is visible
            lblSearchIcon.SendToBack();
            qtyPanel.SendToBack();
            txtWrapper.BringToFront();

            // 2. Takeaway Packaging Charges Bar (Visible in Takeaway mode)
            packingChargePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(20, 30, 48),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 4, 0, 4),
                Visible = false
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

            btnPack10 = CreatePackChargeBtn("₹10", 10);
            btnPack40 = CreatePackChargeBtn("₹40 (Standard)", 40);
            btnPack70 = CreatePackChargeBtn("₹70 (Full Box)", 70);
            btnPackCustom = CreatePackChargeBtn("No Pack (₹0)", 0);

            packingChargePanel.Controls.Add(btnPack10);
            packingChargePanel.Controls.Add(btnPack40);
            packingChargePanel.Controls.Add(btnPack70);
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
                if (c is Panel tile)
                {
                    tile.Size = new Size(tileW, tileH);
                    foreach (Control child in tile.Controls)
                    {
                        if (child is Label lbl && lbl.Font.Size >= 9.5F && lbl.ForeColor == Color.White)
                        {
                            lbl.Size = new Size(tileW - 20, 38);
                        }
                        else if (child is Label lblP && lblP.Font.Size >= 11F)
                        {
                            lblP.Location = new Point(10, tileH - 30);
                        }
                        else if (child is Label lblAdd && lblAdd.Text == "+ ADD")
                        {
                            lblAdd.Location = new Point(tileW - 60, tileH - 30);
                        }
                    }
                    tile.Invalidate();
                }
            }

            productGridPanel.ResumeLayout();
        }

        private Button CreatePackChargeBtn(string text, decimal val)
        {
            Button btn = new Button
            {
                Text = text,
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
                foreach (Control c in packingChargePanel.Controls)
                {
                    if (c is Button b) b.BackColor = Color.FromArgb(30, 41, 59);
                }
                btn.BackColor = Theme.Accent;
                RefreshOrderCartView();
            };
            return btn;
        }

        private void InitializeRightOrderPanel()
        {
            // Bottom Checkout Actions Panel
            Panel checkoutPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 225,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(10, 8, 10, 8)
            };

            // Totals Row
            lblTotalQty = new Label { Text = "Total : 0 No.", Location = new Point(10, 10), AutoSize = true, Font = Theme.BoldFont, ForeColor = Theme.TextLight };
            lblSubTotal = new Label { Text = "₹0.00", Location = new Point(240, 10), Size = new Size(130, 20), TextAlign = ContentAlignment.MiddleRight, Font = Theme.BoldFont, ForeColor = Theme.TextLight };
            checkoutPanel.Controls.Add(lblTotalQty);
            checkoutPanel.Controls.Add(lblSubTotal);

            lblDiscount = new Label 
            { 
                Text = "Offers / Disc: ₹0.00", 
                Location = new Point(10, 32), 
                AutoSize = true, 
                Font = Theme.SmallFont, 
                ForeColor = Theme.TextMuted,
                Cursor = Cursors.Hand
            };
            lblDiscount.Click += (s, e) => OpenDiscountDialog();
            checkoutPanel.Controls.Add(lblDiscount);

            btnDiscountAction = new Button
            {
                Text = "🏷️ Add Disc",
                Location = new Point(265, 29),
                Size = new Size(105, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDiscountAction.FlatAppearance.BorderSize = 1;
            btnDiscountAction.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnDiscountAction.Click += (s, e) => OpenDiscountDialog();
            checkoutPanel.Controls.Add(btnDiscountAction);

            lblTax = new Label { Text = "Tax (5% GST): ₹0.00", Location = new Point(10, 52), AutoSize = true, Font = Theme.BoldFont, ForeColor = Color.FromArgb(244, 114, 182) };
            checkoutPanel.Controls.Add(lblTax);

            Panel netLine = new Panel { Location = new Point(10, 76), Size = new Size(360, 1), BackColor = Theme.CardBorder };
            checkoutPanel.Controls.Add(netLine);

            Label lblNetTitle = new Label { Text = "Net Amount :", Location = new Point(10, 85), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Theme.TextWhite };
            lblGrandTotal = new Label { Text = "₹0.00", Location = new Point(180, 82), Size = new Size(190, 30), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Theme.Accent };
            checkoutPanel.Controls.Add(lblNetTitle);
            checkoutPanel.Controls.Add(lblGrandTotal);

            // Action Buttons (Print KOT / Send to Kitchen and Settle)
            btnPrintKot = new Button
            {
                Text = "🍳 Send to Kitchen (KOT)",
                Location = new Point(10, 122),
                Size = new Size(175, 44),
                BackColor = Color.FromArgb(109, 40, 217), // Violet
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPrintKot.FlatAppearance.BorderSize = 0;
            btnPrintKot.Click += BtnPrintKot_Click;
            checkoutPanel.Controls.Add(btnPrintKot);

            btnSettle = new Button
            {
                Text = "💳 Settle Bill",
                Location = new Point(195, 122),
                Size = new Size(175, 44),
                BackColor = Theme.Success, // Emerald Green
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSettle.FlatAppearance.BorderSize = 0;
            btnSettle.Click += BtnSettle_Click;
            checkoutPanel.Controls.Add(btnSettle);

            btnKotComment = new Button
            {
                Text = "📝 KOT Comment / Special Note",
                Location = new Point(10, 174),
                Size = new Size(360, 34),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextMuted,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnKotComment.FlatAppearance.BorderSize = 1;
            btnKotComment.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnKotComment.Click += BtnKotComment_Click;
            checkoutPanel.Controls.Add(btnKotComment);

            checkoutPanel.SizeChanged += (s, e) => {
                int pw = checkoutPanel.ClientSize.Width - 20;
                if (pw < 200) pw = 200;
                netLine.Width = pw;
                lblSubTotal.Location = new Point(pw - 130, 10);
                if (btnDiscountAction != null) btnDiscountAction.Location = new Point(pw - 105, 29);
                lblGrandTotal.Location = new Point(pw - 190, 82);
                int btnHalf = (pw - 10) / 2;
                btnPrintKot.Size = new Size(btnHalf, 44);
                btnSettle.Location = new Point(10 + btnHalf + 10, 122);
                btnSettle.Size = new Size(btnHalf, 44);
                btnKotComment.Width = pw;
            };

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

            checkoutPanel.SendToBack();
            kotItemsContainer.BringToFront();
        }

        private void OpenDiscountDialog()
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Please add items to the order before applying a discount.", "Order Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal grossTotal = cartItems.Sum(x => x.LineTotal);
            if (ActiveOrderType == "TAKEAWAY") grossTotal += currentPackingCharge;

            using (DiscountDialog dlg = new DiscountDialog(grossTotal, currentDiscountAmount, currentDiscountReason))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (dlg.IsDiscountRemoved)
                    {
                        currentDiscountAmount = 0m;
                        currentDiscountReason = "";
                    }
                    else
                    {
                        currentDiscountAmount = dlg.DiscountAmount;
                        currentDiscountReason = dlg.DiscountReason;
                    }
                    RefreshOrderCartView();
                }
            }
        }

        private void UpdateStewardButtonText()
        {
            string selected = cmbSteward.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) selected = "Steward";
            if (btnSteward != null)
            {
                btnSteward.Text = $"👤 {selected} ▾";
            }
        }

        private void LoadStewards()
        {
            try
            {
                cmbSteward.Items.Clear();
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Staff WHERE IsActive = 1 ORDER BY Name", conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            cmbSteward.Items.Add(r["Name"].ToString());
                        }
                    }
                }

                if (cmbSteward.Items.Count > 0)
                    cmbSteward.SelectedIndex = 0;

                UpdateStewardButtonText();
            }
            catch { }
        }

        private void LoadCategories()
        {
            try
            {
                categoryTabsPanel.Controls.Clear();

                Button btnAll = CreateCategoryTabBtn("All", "All", true);
                categoryTabsPanel.Controls.Add(btnAll);

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Categories ORDER BY Name", conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string cat = r["Name"].ToString();
                            Button btnCat = CreateCategoryTabBtn(cat, cat, false);
                            categoryTabsPanel.Controls.Add(btnCat);
                        }
                    }
                }
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
                productGridPanel.SuspendLayout();
                productGridPanel.Controls.Clear();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT Id, Code, Name, Category, SalesPrice, ISNULL(GSTRate, 5.00) AS GSTRate FROM Products WHERE 1=1";
                    if (!string.IsNullOrEmpty(query))
                    {
                        sql += " AND (Name LIKE @q OR Code LIKE @q)";
                    }
                    if (!string.IsNullOrEmpty(category) && category != "All")
                    {
                        sql += " AND Category = @cat";
                    }
                    sql += " ORDER BY Name";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(query)) cmd.Parameters.AddWithValue("@q", "%" + query + "%");
                        if (!string.IsNullOrEmpty(category) && category != "All") cmd.Parameters.AddWithValue("@cat", category);

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int id = Convert.ToInt32(r["Id"]);
                                string code = r["Code"].ToString();
                                string name = r["Name"].ToString();
                                string cat = r["Category"]?.ToString() ?? "";
                                decimal price = Convert.ToDecimal(r["SalesPrice"]);
                                decimal gst = Convert.ToDecimal(r["GSTRate"]);

                                Control tile = CreateProductTile(id, code, name, cat, price, gst);
                                productGridPanel.Controls.Add(tile);
                            }
                        }
                    }
                }

                productGridPanel.ResumeLayout();
                AdjustProductGridTiles();
            }
            catch
            {
                productGridPanel.ResumeLayout();
            }
        }

        private Control CreateProductTile(int id, string code, string name, string cat, decimal price, decimal gst)
        {
            Panel tile = new Panel
            {
                Size = new Size(165, 105),
                Margin = new Padding(6),
                BackColor = Color.FromArgb(24, 33, 47),
                Cursor = Cursors.Hand
            };

            bool isHovered = false;
            bool isVeg = !name.ToLower().Contains("chicken") && !name.ToLower().Contains("non veg") && !name.ToLower().Contains("egg");

            tile.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using (GraphicsPath path = Theme.GetRoundedPath(rect, 8))
                using (Pen p = new Pen(isHovered ? Theme.Accent : Color.FromArgb(51, 65, 85), isHovered ? 2 : 1))
                {
                    e.Graphics.DrawPath(p, path);
                }

                // Draw Veg / Non-Veg symbol at (10, 10)
                Rectangle iconBox = new Rectangle(10, 10, 14, 14);
                Color iconCol = isVeg ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
                using (Pen p = new Pen(iconCol, 1.5f))
                    e.Graphics.DrawRectangle(p, iconBox);
                using (SolidBrush b = new SolidBrush(iconCol))
                    e.Graphics.FillEllipse(b, iconBox.X + 3, iconBox.Y + 3, 8, 8);
            };

            // Category tag label at top right
            if (!string.IsNullOrEmpty(cat))
            {
                Label lblCat = new Label
                {
                    Text = cat.ToUpper(),
                    Location = new Point(32, 10),
                    Size = new Size(125, 15),
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.TopRight,
                    AutoEllipsis = true,
                    BackColor = Color.Transparent
                };
                tile.Controls.Add(lblCat);
            }

            // Dish Name
            Label lblName = new Label
            {
                Text = name,
                Location = new Point(10, 30),
                Size = new Size(tile.Width - 20, 38),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            tile.Controls.Add(lblName);

            // Price
            Label lblPrice = new Label
            {
                Text = $"₹{price:0}",
                Location = new Point(10, tile.Height - 30),
                Size = new Size(80, 24),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            tile.Controls.Add(lblPrice);

            // Add Button Badge
            Label lblAdd = new Label
            {
                Text = "+ ADD",
                Location = new Point(tile.Width - 60, tile.Height - 30),
                Size = new Size(50, 22),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(16, 185, 129, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblAdd.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, lblAdd.Width - 1, lblAdd.Height - 1), 4))
                using (Pen p = new Pen(Color.FromArgb(16, 185, 129), 1))
                    e.Graphics.DrawPath(p, path);
            };
            tile.Controls.Add(lblAdd);

            Action addAction = () => {
                int qty = quickQty;
                AddToCart(id, code, name, cat, price, gst, qty);
                quickQty = 1;
                if (lblQtyVal != null) lblQtyVal.Text = "1";
            };

            tile.MouseEnter += (s, e) => { isHovered = true; tile.Invalidate(); };
            tile.MouseLeave += (s, e) => { isHovered = false; tile.Invalidate(); };

            tile.Click += (s, e) => addAction();
            lblName.Click += (s, e) => addAction();
            lblPrice.Click += (s, e) => addAction();
            lblAdd.Click += (s, e) => addAction();

            return tile;
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

                    Panel itemRow = new Panel
                    {
                        Width = panelW,
                        Height = 44,
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
                        Size = new Size(26, 26),
                        Location = new Point(4, 8),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = Theme.Danger,
                        Cursor = Cursors.Hand
                    };
                    btnDel.FlatAppearance.BorderSize = 0;
                    btnDel.Click += (s, e) => RemoveOrVoidItem(item);
                    itemRow.Controls.Add(btnDel);

                    // Name
                    Label lblName = new Label
                    {
                        Text = item.ItemName,
                        Location = new Point(34, 11),
                        Size = new Size(panelW - 170, 22),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoEllipsis = true
                    };
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

                    kotItemsContainer.Controls.Add(itemRow);
                }
            }

            // Add Takeaway Packing Charges if applicable
            if (ActiveOrderType == "TAKEAWAY" && currentPackingCharge > 0)
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

                decimal packTaxable = Math.Round(currentPackingCharge / 1.05m, 2);
                Label lblPackAmt = new Label
                {
                    Text = $"₹{packTaxable:0.00}",
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

            // Calculate SubTotal, Discount, and 5% GST (Reverse Calculation from Inclusive Total)
            decimal discountVal = Math.Min(totalGross, currentDiscountAmount);
            decimal discountedGross = Math.Max(0, totalGross - discountVal);

            decimal taxableSubTotal = Math.Round(discountedGross / 1.05m, 2);
            decimal totalGst = discountedGross - taxableSubTotal;
            decimal netAmount = Math.Round(discountedGross, 0);

            lblTotalQty.Text = $"Total : {totalQty} No.";
            lblSubTotal.Text = $"₹{taxableSubTotal:0.00}";

            if (discountVal > 0)
            {
                string reasonSnippet = string.IsNullOrEmpty(currentDiscountReason) ? "" : $" ({currentDiscountReason})";
                lblDiscount.Text = $"Offers / Disc: -₹{discountVal:0.00}{reasonSnippet}";
                lblDiscount.ForeColor = Color.FromArgb(248, 113, 113);
                if (btnDiscountAction != null)
                {
                    btnDiscountAction.Text = "✏️ Edit Disc";
                    btnDiscountAction.ForeColor = Color.FromArgb(248, 113, 113);
                }
            }
            else
            {
                lblDiscount.Text = "Offers / Disc: ₹0.00";
                lblDiscount.ForeColor = Theme.TextMuted;
                if (btnDiscountAction != null)
                {
                    btnDiscountAction.Text = "🏷️ Add Disc";
                    btnDiscountAction.ForeColor = Theme.Accent;
                }
            }

            lblTax.Text = $"Tax (5% GST): ₹{totalGst:0.00}";
            lblGrandTotal.Text = $"₹{netAmount:0.00}";
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
                            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                            {
                                conn.Open();
                                string sql = @"
                                    UPDATE KOTDetails 
                                    SET IsVoided = 1, VoidReason = @reason, VoidedAt = GETDATE()
                                    WHERE KOTId = @kotId AND ItemName = @name";

                                using (SqlCommand cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@reason", dlg.Comment);
                                    cmd.Parameters.AddWithValue("@kotId", item.KotId);
                                    cmd.Parameters.AddWithValue("@name", item.ItemName);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            if (dlg.ShouldPrintSlip)
                            {
                                ThermalReceiptPrinter.PrintVoidKOT(item.KotId, dlg.Comment);
                            }

                            cartItems.Remove(item);
                            RefreshOrderCartView();
                            UpdateTableSummaryInDb();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to void item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
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
                    string steward = cmbSteward.SelectedItem?.ToString() ?? "Tashi";
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
                decimal totalGross = cartItems.Sum(x => x.LineTotal);
                if (ActiveOrderType == "TAKEAWAY") totalGross += currentPackingCharge;
                decimal netRunning = Math.Max(0, totalGross - currentDiscountAmount);

                string kots = string.Join(",", cartItems.Where(x => x.KotNumber > 0).Select(x => x.KotNumber.ToString()).Distinct());
                string steward = cmbSteward.SelectedItem?.ToString() ?? "Tashi";

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

            decimal totalGross = cartItems.Sum(x => x.LineTotal);
            if (ActiveOrderType == "TAKEAWAY") totalGross += currentPackingCharge;

            decimal discountVal = Math.Min(totalGross, currentDiscountAmount);
            decimal discountedGross = Math.Max(0, totalGross - discountVal);

            decimal taxableSubTotal = Math.Round(discountedGross / 1.05m, 2);
            decimal totalGst = discountedGross - taxableSubTotal;
            decimal cgst = Math.Round(totalGst / 2.0m, 2);
            decimal sgst = totalGst - cgst;
            decimal grandTotal = Math.Round(discountedGross, 0);
            decimal roundOff = grandTotal - discountedGross;

            using (SettlePaymentDialog dlg = new SettlePaymentDialog(grandTotal))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int saleId = 0;
                        string kots = string.Join(",", cartItems.Where(x => x.KotNumber > 0).Select(x => x.KotNumber.ToString()).Distinct());
                        string steward = cmbSteward.SelectedItem?.ToString() ?? "Tashi";

                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // 1. Next Invoice Number
                                    int nextInvNo = 1;
                                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM Sales", conn, trans))
                                    {
                                        nextInvNo = Convert.ToInt32(cmd.ExecuteScalar());
                                    }
                                    string invNumber = "INV-" + nextInvNo;

                                    // 2. Insert Sales Record
                                    string insSaleSql = @"
                                        INSERT INTO Sales (
                                            InvoiceNumber, SaleDate, SubTotal, Discount, Tax, GrandTotal, AmountPaid, DueAmount, PaymentMethod,
                                            OrderType, TableNumber, KotNumbers, PackingCharges, StewardName, RoundOff, TaxableAmount, CGSTAmount, SGSTAmount, IsGSTBill
                                        ) VALUES (
                                            @inv, GETDATE(), @sub, @disc, @tax, @grand, @paid, @due, @payMethod,
                                            @orderType, @tNum, @kots, @packing, @stwd, @roundOff, @taxable, @cgst, @sgst, 1
                                        );
                                        SELECT SCOPE_IDENTITY();";

                                    using (SqlCommand cmd = new SqlCommand(insSaleSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@inv", invNumber);
                                        cmd.Parameters.AddWithValue("@sub", taxableSubTotal);
                                        cmd.Parameters.AddWithValue("@disc", discountVal);
                                        cmd.Parameters.AddWithValue("@tax", totalGst);
                                        cmd.Parameters.AddWithValue("@grand", grandTotal);
                                        cmd.Parameters.AddWithValue("@paid", dlg.AmountPaid);
                                        cmd.Parameters.AddWithValue("@due", Math.Max(0, grandTotal - dlg.AmountPaid));
                                        cmd.Parameters.AddWithValue("@payMethod", dlg.PaymentMethod);
                                        cmd.Parameters.AddWithValue("@orderType", ActiveOrderType);
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.Parameters.AddWithValue("@kots", (object)kots ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@packing", currentPackingCharge);
                                        cmd.Parameters.AddWithValue("@stwd", steward);
                                        cmd.Parameters.AddWithValue("@roundOff", roundOff);
                                        cmd.Parameters.AddWithValue("@taxable", taxableSubTotal);
                                        cmd.Parameters.AddWithValue("@cgst", cgst);
                                        cmd.Parameters.AddWithValue("@sgst", sgst);
                                        saleId = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    // 3. Insert SaleDetails
                                    foreach (var it in cartItems)
                                    {
                                        string insDetSql = @"
                                            INSERT INTO SaleDetails (SaleId, ItemType, ProductId, Quantity, UnitPrice, Total, TaxableAmount, CGSTAmount, SGSTAmount, GSTRate)
                                            VALUES (@saleId, 'Product', @pid, @qty, @price, @total, @taxable, @cgst, @sgst, 5.00)";

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
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    // 4. Add Packing Charges as detail line if present
                                    if (ActiveOrderType == "TAKEAWAY" && currentPackingCharge > 0)
                                    {
                                        decimal packTaxable = Math.Round(currentPackingCharge / 1.05m, 2);
                                        decimal packTax = currentPackingCharge - packTaxable;
                                        string insPackDet = @"
                                            INSERT INTO SaleDetails (SaleId, ItemType, ProductId, Quantity, UnitPrice, Total, TaxableAmount, CGSTAmount, SGSTAmount, GSTRate)
                                            VALUES (@saleId, 'Product', NULL, 1, @price, @price, @taxable, @cgst, @sgst, 5.00)";
                                        using (SqlCommand cmd = new SqlCommand(insPackDet, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@saleId", saleId);
                                            cmd.Parameters.AddWithValue("@price", currentPackingCharge);
                                            cmd.Parameters.AddWithValue("@taxable", packTaxable);
                                            cmd.Parameters.AddWithValue("@cgst", Math.Round(packTax / 2.0m, 2));
                                            cmd.Parameters.AddWithValue("@sgst", packTax - Math.Round(packTax / 2.0m, 2));
                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                    // 5. Mark KOTs as Billed
                                    string billKotSql = "UPDATE KOTMaster SET Status = 'Billed', SaleId = @saleId WHERE TableNumber = @tNum AND Status IN ('Active', 'Served', 'Printed')";
                                    using (SqlCommand cmd = new SqlCommand(billKotSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@saleId", saleId);
                                        cmd.Parameters.AddWithValue("@tNum", ActiveTableNumber);
                                        cmd.ExecuteNonQuery();
                                    }

                                    // 6. Reset Cafe Table to Available & Consolidate Shared Sub-Tables
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

                        MessageBox.Show($"Bill #{saleId} settled successfully!", "Settlement Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        cartItems.Clear();
                        currentPackingCharge = 0;
                        currentDiscountAmount = 0m;
                        currentDiscountReason = "";
                        RefreshOrderCartView();

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

        private class DarkMenuRenderer : ToolStripProfessionalRenderer
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
