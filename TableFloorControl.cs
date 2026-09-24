using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class TableFloorControl : UserControl, IFocusableControl
    {
        private Panel topHeaderPanel;
        private FlowLayoutPanel modeTabsPanel;
        private FlowLayoutPanel tableGridPanel;
        private Panel bottomLegendPanel;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.Timer liveClockTimer;

        private Button btnModeDining;
        private Button btnModeTakeaway;
        private Button btnModeDelivery;
        private Button btnModeWaiting;

        private Label lblDaySale;
        private Label lblUnsettled;
        private Button btnTableShift;
        private Button btnShareTable;
        private Button btnRefresh;

        private string currentFilterMode = "DINING";
        private readonly Dictionary<string, TableCardView> activeCardViews = new Dictionary<string, TableCardView>(StringComparer.OrdinalIgnoreCase);

        public event Action<string, string> OnTableSelected; // tableNumber, orderType

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: Paints all descendants from bottom to top using double-buffering (eliminates WinForms redraw flicker)
                return cp;
            }
        }

        private static void SetDoubleBuffered(Control c)
        {
            if (c == null) return;
            try
            {
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, c, new object[] { true });
            }
            catch { }
        }

        public TableFloorControl(string initialMode = "DINING")
        {
            currentFilterMode = string.IsNullOrEmpty(initialMode) ? "DINING" : initialMode;
            InitializeComponent();
            SetDoubleBuffered(this);
            SetDoubleBuffered(tableGridPanel);

            LoadTableCards();

            // 1. Live second-by-second timer for active table timestamps (100% in-memory, ZERO database load, ZERO flicker/blinking)
            liveClockTimer = new System.Windows.Forms.Timer();
            liveClockTimer.Interval = 1000;
            liveClockTimer.Tick += (s, e) => UpdateLiveTimersOnly();
            liveClockTimer.Start();

            // 2. Periodic background database status refresh (every 4 seconds, updates in-place without rebuilding controls)
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 4000;
            refreshTimer.Tick += (s, e) => LoadTableCards();
            refreshTimer.Start();

            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                tableGridPanel?.Focus();
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (liveClockTimer != null)
                {
                    liveClockTimer.Stop();
                    liveClockTimer.Dispose();
                    liveClockTimer = null;
                }
                if (refreshTimer != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }
            }
            base.Dispose(disposing);
        }

        public void SwitchMode(string mode)
        {
            currentFilterMode = string.IsNullOrEmpty(mode) ? "DINING" : mode;
            if (modeTabsPanel != null)
            {
                foreach (Control c in modeTabsPanel.Controls)
                {
                    if (c is Button b) UpdateTabStyle(b, b.Tag?.ToString() == currentFilterMode);
                }
            }
            if (btnTableShift != null) btnTableShift.Visible = (currentFilterMode == "DINING");
            if (btnShareTable != null) btnShareTable.Visible = (currentFilterMode == "DINING");
            
            activeCardViews.Clear();
            LoadTableCards();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.Secondary;
            this.Padding = new Padding(12);

            // ================= 1. TOP HEADER PANEL =================
            topHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.CardBg,
                Padding = new Padding(8, 6, 8, 6)
            };

            TableLayoutPanel topHeaderTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            topHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Mode Tabs (DINING, TAKE AWAY, DELIVERY, WAITING)
            modeTabsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnModeDining = CreateModeTabButton("🍽️ DINING", "DINING", currentFilterMode == "DINING");
            btnModeTakeaway = CreateModeTabButton("🛍️ TAKE AWAY", "TAKEAWAY", currentFilterMode == "TAKEAWAY");
            btnModeDelivery = CreateModeTabButton("🛵 DELIVERY", "DELIVERY", currentFilterMode == "DELIVERY");
            btnModeWaiting = CreateModeTabButton("⏳ WAITING", "WAITING", currentFilterMode == "WAITING");

            modeTabsPanel.Controls.Add(btnModeDining);
            modeTabsPanel.Controls.Add(btnModeTakeaway);
            modeTabsPanel.Controls.Add(btnModeDelivery);
            modeTabsPanel.Controls.Add(btnModeWaiting);
            topHeaderTable.Controls.Add(modeTabsPanel, 0, 0);

            // Live Stats & Action Buttons (Right side FlowLayoutPanel)
            FlowLayoutPanel rightStatsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 0)
            };

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(10, 0, 10, 0),
                BackColor = Theme.CardBorder,
                ForeColor = Theme.TextLight,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadTableCards();
            rightStatsFlow.Controls.Add(btnRefresh);

            btnTableShift = new Button
            {
                Text = "🔁 Shift Table",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(10, 0, 10, 0),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnTableShift.FlatAppearance.BorderSize = 0;
            btnTableShift.Click += BtnTableShift_Click;
            rightStatsFlow.Controls.Add(btnTableShift);

            btnShareTable = new Button
            {
                Text = "🪑 Share Table",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(10, 0, 10, 0),
                BackColor = Color.FromArgb(109, 40, 217), // Violet
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnShareTable.FlatAppearance.BorderSize = 0;
            btnShareTable.Click += (s, e) => BtnShareTable_Click(null);
            rightStatsFlow.Controls.Add(btnShareTable);

            lblUnsettled = new Label
            {
                Text = "Unsettled: ₹0.00",
                ForeColor = Color.FromArgb(244, 114, 182), // Soft Pink
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(12, 8, 4, 0)
            };
            rightStatsFlow.Controls.Add(lblUnsettled);

            lblDaySale = new Label
            {
                Text = "Day Sale: ₹0.00",
                ForeColor = Theme.Success,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(12, 8, 4, 0)
            };
            rightStatsFlow.Controls.Add(lblDaySale);

            topHeaderTable.Controls.Add(rightStatsFlow, 1, 0);
            topHeaderPanel.Controls.Add(topHeaderTable);

            // ================= 2. BOTTOM LEGEND PANEL =================
            bottomLegendPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Theme.CardBg,
                Padding = new Padding(16, 6, 16, 6)
            };

            FlowLayoutPanel legendFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };
            legendFlow.Controls.Add(CreateLegendBadge("Available", Color.FromArgb(51, 65, 85), Color.FromArgb(203, 213, 225)));
            legendFlow.Controls.Add(CreateLegendBadge("Running (Active KOT)", Color.FromArgb(109, 40, 217), Color.White));
            legendFlow.Controls.Add(CreateLegendBadge("Printed (Bill Generated)", Color.FromArgb(14, 116, 144), Color.White));
            bottomLegendPanel.Controls.Add(legendFlow);

            // ================= 3. TABLE GRID PANEL =================
            tableGridPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Secondary,
                Padding = new Padding(10, 10, 10, 10)
            };
            tableGridPanel.SizeChanged += (s, e) => AdjustCardSizes();

            // Add docked controls in correct Z-order
            this.Controls.Add(tableGridPanel);
            this.Controls.Add(bottomLegendPanel);
            this.Controls.Add(topHeaderPanel);

            topHeaderPanel.SendToBack();
            bottomLegendPanel.SendToBack();
            tableGridPanel.BringToFront();
        }

        private void AdjustCardSizes()
        {
            if (tableGridPanel == null || tableGridPanel.Controls.Count == 0) return;
            tableGridPanel.SuspendLayout();

            int availW = tableGridPanel.ClientSize.Width - tableGridPanel.Padding.Horizontal - 25;
            if (availW < 200) availW = 400;
            int cols = Math.Max(2, Math.Min(6, availW / 215));
            int cardW = Math.Max(180, (availW / cols) - 18);
            int cardH = 125;

            foreach (Control c in tableGridPanel.Controls)
            {
                if (c is Panel card && card.Tag is TableCardView view)
                {
                    card.Size = new Size(cardW, cardH);
                    if (view.LblNumber != null)
                    {
                        view.LblNumber.Size = new Size(cardW - 110, view.TableNumber.Contains("-") ? 38 : 55);
                    }
                    if (view.RightBox != null)
                    {
                        view.RightBox.Location = new Point(cardW - 95, 12);
                    }
                    if (view.LblAvail != null)
                    {
                        view.LblAvail.Location = new Point(14, cardH - 35);
                    }
                    card.Invalidate();
                }
            }

            tableGridPanel.ResumeLayout();
        }

        private Button CreateModeTabButton(string text, string mode, bool isActive)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 36,
                Padding = new Padding(12, 0, 12, 0),
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = mode,
                Margin = new Padding(0, 4, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            UpdateTabStyle(btn, isActive);
            btn.Click += (s, e) => SwitchMode(mode);
            return btn;
        }

        private void UpdateTabStyle(Button btn, bool active)
        {
            if (active)
            {
                btn.BackColor = Theme.Accent;
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Theme.InputBg;
                btn.ForeColor = Theme.TextMuted;
            }
        }

        private Control CreateLegendBadge(string text, Color bg, Color fg)
        {
            Panel p = new Panel
            {
                Size = new Size(180, 24),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 20, 0)
            };

            Panel dot = new Panel
            {
                Size = new Size(14, 14),
                Location = new Point(2, 5),
                BackColor = bg
            };
            dot.Paint += (s, e) => {
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, 13, 13), 3))
                using (SolidBrush b = new SolidBrush(bg))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(b, path);
                }
            };
            p.Controls.Add(dot);

            Label lbl = new Label
            {
                Text = text,
                Font = Theme.SmallFont,
                ForeColor = fg,
                Location = new Point(22, 3),
                AutoSize = true
            };
            p.Controls.Add(lbl);

            return p;
        }

        public void LoadTableCards()
        {
            try
            {
                decimal daySales = 0;
                decimal unsettledSales = 0;

                // Load Live Header Sales
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            (SELECT ISNULL(SUM(GrandTotal), 0) 
                             FROM Sales 
                             WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)) AS DaySales,
                            (
                                ISNULL((
                                    SELECT SUM(kd.Amount)
                                    FROM KOTMaster k
                                    INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                                    WHERE k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0
                                ), 0)
                                +
                                ISNULL((
                                    SELECT SUM(DueAmount)
                                    FROM Sales 
                                    WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND DueAmount > 0
                                ), 0)
                            ) AS UnsettledSales", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                daySales = Convert.ToDecimal(r["DaySales"]);
                                unsettledSales = Convert.ToDecimal(r["UnsettledSales"]);
                            }
                        }
                    }
                }

                if (lblDaySale != null) lblDaySale.Text = $"Day Sale: ₹{daySales:N1}";
                if (lblUnsettled != null) lblUnsettled.Text = $"Unsettled: ₹{unsettledSales:N1}";

                // Load Tables
                int availW = tableGridPanel.ClientSize.Width - tableGridPanel.Padding.Horizontal - 25;
                if (availW < 200) availW = 400;
                int cols = Math.Max(2, Math.Min(6, availW / 215));
                int cardW = Math.Max(180, (availW / cols) - 18);
                int cardH = 125;

                List<TableCardData> tableList = new List<TableCardData>();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Fetch live active KOT data mapped by TableNumber strictly for the active tab mode
                    Dictionary<string, LiveKotData> liveKotMap = new Dictionary<string, LiveKotData>(StringComparer.OrdinalIgnoreCase);
                    string kotWhere = "";
                    if (currentFilterMode == "DINING")
                    {
                        kotWhere = " AND (k.OrderType = 'DINING' OR k.OrderType IS NULL OR k.OrderType = '') AND k.TableNumber NOT LIKE 'Waiting%'";
                    }
                    else if (currentFilterMode == "TAKEAWAY")
                    {
                        kotWhere = " AND k.OrderType = 'TAKEAWAY'";
                    }
                    else if (currentFilterMode == "DELIVERY")
                    {
                        kotWhere = " AND k.OrderType = 'DELIVERY'";
                    }
                    else if (currentFilterMode == "WAITING")
                    {
                        kotWhere = " AND k.TableNumber LIKE 'Waiting%'";
                    }

                    string kotQuery = @"
                        SELECT k.TableNumber, k.KOTNumber, k.CreatedAt, k.Steward, kd.Amount
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0" + kotWhere;

                    using (SqlCommand cmd = new SqlCommand(kotQuery, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string tNum = r["TableNumber"].ToString();
                            int kotNo = Convert.ToInt32(r["KOTNumber"]);
                            DateTime createdAt = Convert.ToDateTime(r["CreatedAt"]);
                            string stwd = r["Steward"]?.ToString();
                            decimal amt = Convert.ToDecimal(r["Amount"]);

                            if (!liveKotMap.TryGetValue(tNum, out LiveKotData data))
                            {
                                data = new LiveKotData { TableNumber = tNum, EarliestTime = createdAt, Steward = stwd };
                                liveKotMap[tNum] = data;
                            }

                            data.TotalAmount += amt;
                            if (!data.KotNumbers.Contains(kotNo)) data.KotNumbers.Add(kotNo);
                            if (createdAt < data.EarliestTime) data.EarliestTime = createdAt;
                            if (!string.IsNullOrEmpty(stwd)) data.Steward = stwd;
                        }
                    }

                    // 2. Fetch or generate tables based on currentFilterMode
                    if (currentFilterMode == "DINING")
                    {
                        string query = @"
                            SELECT TableNumber, TableName, Status, CurrentBillAmount, 
                                   OrderStartTime, BilledTime, ActiveKotNumbers, CurrentSteward
                            FROM CafeTables 
                            WHERE IsActive = 1 AND TableNumber NOT LIKE 'Waiting%'";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string tNum = r["TableNumber"].ToString();
                                string tName = r["TableName"]?.ToString() ?? tNum;
                                string status = "Available";
                                decimal amount = 0;
                                object startTimeObj = DBNull.Value;
                                object billedTimeObj = r["BilledTime"];
                                string kots = "";
                                string steward = r["CurrentSteward"]?.ToString() ?? "";

                                if (liveKotMap.TryGetValue(tNum, out LiveKotData live))
                                {
                                    status = "Running";
                                    amount = live.TotalAmount;
                                    startTimeObj = live.EarliestTime;
                                    live.KotNumbers.Sort();
                                    kots = string.Join(",", live.KotNumbers);
                                    if (!string.IsNullOrEmpty(live.Steward)) steward = live.Steward;
                                }

                                tableList.Add(new TableCardData
                                {
                                    TableNumber = tNum,
                                    TableName = tName,
                                    Status = status,
                                    Amount = amount,
                                    StartTime = startTimeObj,
                                    BilledTime = billedTimeObj,
                                    Kots = kots,
                                    Steward = steward
                                });
                            }
                        }
                    }
                    else if (currentFilterMode == "WAITING")
                    {
                        string query = @"
                            SELECT TableNumber, TableName, Status, CurrentBillAmount, 
                                   OrderStartTime, BilledTime, ActiveKotNumbers, CurrentSteward
                            FROM CafeTables 
                            WHERE IsActive = 1 AND TableNumber LIKE 'Waiting%'";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string tNum = r["TableNumber"].ToString();
                                string tName = r["TableName"]?.ToString() ?? tNum;
                                string status = "Available";
                                decimal amount = 0;
                                object startTimeObj = DBNull.Value;
                                object billedTimeObj = r["BilledTime"];
                                string kots = "";
                                string steward = r["CurrentSteward"]?.ToString() ?? "";

                                if (liveKotMap.TryGetValue(tNum, out LiveKotData live))
                                {
                                    status = "Running";
                                    amount = live.TotalAmount;
                                    startTimeObj = live.EarliestTime;
                                    live.KotNumbers.Sort();
                                    kots = string.Join(",", live.KotNumbers);
                                    if (!string.IsNullOrEmpty(live.Steward)) steward = live.Steward;
                                }

                                tableList.Add(new TableCardData
                                {
                                    TableNumber = tNum,
                                    TableName = tName,
                                    Status = status,
                                    Amount = amount,
                                    StartTime = startTimeObj,
                                    BilledTime = billedTimeObj,
                                    Kots = kots,
                                    Steward = steward
                                });
                            }
                        }
                    }
                    else if (currentFilterMode == "TAKEAWAY")
                    {
                        HashSet<int> tokenNums = new HashSet<int>();
                        for (int i = 1; i <= 10; i++) tokenNums.Add(i);
                        foreach (string k in liveKotMap.Keys)
                        {
                            if (int.TryParse(k, out int n)) tokenNums.Add(n);
                        }

                        List<int> sortedTokens = new List<int>(tokenNums);
                        sortedTokens.Sort();

                        foreach (int n in sortedTokens)
                        {
                            string tNum = n.ToString();
                            string tName = "Token " + tNum;
                            string status = "Available";
                            decimal amount = 0;
                            object startTimeObj = DBNull.Value;
                            string kots = "";
                            string steward = "";

                            if (liveKotMap.TryGetValue(tNum, out LiveKotData live))
                            {
                                status = "Running";
                                amount = live.TotalAmount;
                                startTimeObj = live.EarliestTime;
                                live.KotNumbers.Sort();
                                kots = string.Join(",", live.KotNumbers);
                                if (!string.IsNullOrEmpty(live.Steward)) steward = live.Steward;
                            }

                            tableList.Add(new TableCardData
                            {
                                TableNumber = tNum,
                                TableName = tName,
                                Status = status,
                                Amount = amount,
                                StartTime = startTimeObj,
                                BilledTime = DBNull.Value,
                                Kots = kots,
                                Steward = steward
                            });
                        }
                    }
                    else if (currentFilterMode == "DELIVERY")
                    {
                        HashSet<int> deliveryNums = new HashSet<int>();
                        for (int i = 1; i <= 10; i++) deliveryNums.Add(i);
                        foreach (string k in liveKotMap.Keys)
                        {
                            if (int.TryParse(k, out int n)) deliveryNums.Add(n);
                        }

                        List<int> sortedDeliveries = new List<int>(deliveryNums);
                        sortedDeliveries.Sort();

                        foreach (int n in sortedDeliveries)
                        {
                            string tNum = n.ToString();
                            string tName = "Delivery " + tNum;
                            string status = "Available";
                            decimal amount = 0;
                            object startTimeObj = DBNull.Value;
                            string kots = "";
                            string steward = "";

                            if (liveKotMap.TryGetValue(tNum, out LiveKotData live))
                            {
                                status = "Running";
                                amount = live.TotalAmount;
                                startTimeObj = live.EarliestTime;
                                live.KotNumbers.Sort();
                                kots = string.Join(",", live.KotNumbers);
                                if (!string.IsNullOrEmpty(live.Steward)) steward = live.Steward;
                            }

                            tableList.Add(new TableCardData
                            {
                                TableNumber = tNum,
                                TableName = tName,
                                Status = status,
                                Amount = amount,
                                StartTime = startTimeObj,
                                BilledTime = DBNull.Value,
                                Kots = kots,
                                Steward = steward
                            });
                        }
                    }
                }

                // 3. Sort tables naturally (1, 2, 3, 4-A, 4-B, 5, 6... 10)
                tableList.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));

                // 4. CHECK IF STRUCTURE MATCHES TO UPDATE IN-PLACE (ZERO FLICKER / ZERO BLINK)
                bool canUpdateInPlace = (tableGridPanel.Controls.Count == tableList.Count && activeCardViews.Count == tableList.Count);
                if (canUpdateInPlace)
                {
                    for (int i = 0; i < tableList.Count; i++)
                    {
                        if (tableGridPanel.Controls[i].Tag is TableCardView view &&
                            string.Equals(view.TableNumber, tableList[i].TableNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            // Matches
                        }
                        else
                        {
                            canUpdateInPlace = false;
                            break;
                        }
                    }
                }

                if (canUpdateInPlace)
                {
                    // Perfectly matches! Update existing cards in-place without touching Controls collection!
                    for (int i = 0; i < tableList.Count; i++)
                    {
                        var t = tableList[i];
                        if (activeCardViews.TryGetValue(t.TableNumber, out TableCardView view))
                        {
                            UpdateExistingCard(view, t);
                        }
                    }
                }
                else
                {
                    // Structure changed (mode switch or sub-table added/removed): rebuild cards cleanly
                    tableGridPanel.SuspendLayout();
                    activeCardViews.Clear();
                    tableGridPanel.Controls.Clear();

                    foreach (var t in tableList)
                    {
                        Control card = CreateTableCard(t, cardW, cardH);
                        tableGridPanel.Controls.Add(card);
                    }

                    tableGridPanel.ResumeLayout();
                }

                MainForm.Instance?.RefreshLiveOrderCounts();
            }
            catch
            {
                tableGridPanel.ResumeLayout();
                // Avoid popup spamming in timer
            }
        }

        private void UpdateLiveTimersOnly()
        {
            foreach (var kvp in activeCardViews)
            {
                var view = kvp.Value;
                if (view.CurrentStatus == "Running" || view.CurrentStatus == "Printed")
                {
                    var t = view.LastData;
                    if (t == null) continue;

                    string elapsedStr = "";
                    if (view.CurrentStatus == "Running" && t.StartTime != DBNull.Value && t.StartTime != null)
                    {
                        TimeSpan span = DateTime.Now - Convert.ToDateTime(t.StartTime);
                        elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
                    }
                    else if (view.CurrentStatus == "Printed")
                    {
                        DateTime refTime = (t.BilledTime != DBNull.Value && t.BilledTime != null)
                            ? Convert.ToDateTime(t.BilledTime)
                            : (t.StartTime != DBNull.Value && t.StartTime != null ? Convert.ToDateTime(t.StartTime) : DateTime.Now);
                        TimeSpan span = DateTime.Now - refTime;
                        elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
                    }

                    if (view.LblTimer != null && view.LblTimer.Text != elapsedStr)
                    {
                        view.LblTimer.Text = elapsedStr;
                    }
                }
            }
        }

        private void UpdateExistingCard(TableCardView view, TableCardData t)
        {
            view.LastData = t;

            // Check if status changed
            if (view.CurrentStatus != t.Status)
            {
                view.CurrentStatus = t.Status;
                if (t.Status == "Running")
                {
                    view.CardPanel.BackColor = Color.FromArgb(67, 56, 202);
                    view.BorderColor = Color.FromArgb(129, 140, 248);
                    if (view.LblAvail != null) view.LblAvail.Visible = false;
                    if (view.RightBox != null) view.RightBox.Visible = true;
                }
                else if (t.Status == "Printed")
                {
                    view.CardPanel.BackColor = Color.FromArgb(14, 116, 144);
                    view.BorderColor = Color.FromArgb(56, 189, 248);
                    if (view.LblAvail != null) view.LblAvail.Visible = false;
                    if (view.RightBox != null) view.RightBox.Visible = true;
                }
                else
                {
                    view.CardPanel.BackColor = Color.FromArgb(30, 41, 59);
                    view.BorderColor = Color.FromArgb(51, 65, 85);
                    if (view.LblAvail != null) view.LblAvail.Visible = true;
                    if (view.RightBox != null) view.RightBox.Visible = false;
                }
                view.CardPanel.Invalidate();
            }

            // Update Amount, KOT, and Timer
            if (t.Status == "Running" || t.Status == "Printed")
            {
                string amtText = $"₹{t.Amount:0}";
                if (view.LblAmount != null && view.LblAmount.Text != amtText)
                {
                    view.LblAmount.Text = amtText;
                }

                string kotText = !string.IsNullOrEmpty(t.Kots) ? $"KOT: {t.Kots}" : "";
                if (view.LblKot != null)
                {
                    if (view.LblKot.Text != kotText) view.LblKot.Text = kotText;
                    view.LblKot.Visible = !string.IsNullOrEmpty(kotText);
                }

                string elapsedStr = "";
                if (t.Status == "Running" && t.StartTime != DBNull.Value && t.StartTime != null)
                {
                    TimeSpan span = DateTime.Now - Convert.ToDateTime(t.StartTime);
                    elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
                }
                else if (t.Status == "Printed")
                {
                    DateTime refTime = (t.BilledTime != DBNull.Value && t.BilledTime != null)
                        ? Convert.ToDateTime(t.BilledTime)
                        : (t.StartTime != DBNull.Value && t.StartTime != null ? Convert.ToDateTime(t.StartTime) : DateTime.Now);
                    TimeSpan span = DateTime.Now - refTime;
                    elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
                }

                if (view.LblTimer != null && view.LblTimer.Text != elapsedStr)
                {
                    view.LblTimer.Text = elapsedStr;
                }
            }
        }

        private class LiveKotData
        {
            public string TableNumber { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime EarliestTime { get; set; }
            public List<int> KotNumbers { get; } = new List<int>();
            public string Steward { get; set; }
        }

        private class TableCardData
        {
            public string TableNumber { get; set; }
            public string TableName { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
            public object StartTime { get; set; }
            public object BilledTime { get; set; }
            public string Kots { get; set; }
            public string Steward { get; set; }
        }

        private class TableCardView
        {
            public string TableNumber { get; set; }
            public Panel CardPanel { get; set; }
            public Label LblNumber { get; set; }
            public Label LblSub { get; set; }
            public Label LblAvail { get; set; }
            public Panel RightBox { get; set; }
            public Label LblTimer { get; set; }
            public Label LblAmount { get; set; }
            public Label LblKot { get; set; }
            public Color BorderColor { get; set; }
            public string CurrentStatus { get; set; }
            public TableCardData LastData { get; set; }
        }

        private Control CreateTableCard(TableCardData t, int cardW = 210, int cardH = 125)
        {
            string tableNum = t.TableNumber;
            string status = t.Status;
            decimal amount = t.Amount;
            object startTimeObj = t.StartTime;
            object billedTimeObj = t.BilledTime;
            string kots = t.Kots;

            Panel card = new Panel
            {
                Size = new Size(cardW, cardH),
                Margin = new Padding(8),
                Cursor = Cursors.Hand
            };
            SetDoubleBuffered(card);

            TableCardView view = new TableCardView
            {
                TableNumber = tableNum,
                CardPanel = card,
                CurrentStatus = status,
                LastData = t
            };

            Color cardBg = Color.FromArgb(30, 41, 59);      // Default Available
            Color cardBorder = Color.FromArgb(51, 65, 85);
            Color textMain = Color.White;

            string elapsedStr = "";
            if (status == "Running" && startTimeObj != DBNull.Value && startTimeObj != null)
            {
                cardBg = Color.FromArgb(67, 56, 202); // Deep Indigo/Violet
                cardBorder = Color.FromArgb(129, 140, 248);
                TimeSpan span = DateTime.Now - Convert.ToDateTime(startTimeObj);
                elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
            }
            else if (status == "Printed")
            {
                cardBg = Color.FromArgb(14, 116, 144); // Deep Cyan
                cardBorder = Color.FromArgb(56, 189, 248);
                DateTime refTime = (billedTimeObj != DBNull.Value && billedTimeObj != null)
                    ? Convert.ToDateTime(billedTimeObj)
                    : (startTimeObj != DBNull.Value && startTimeObj != null ? Convert.ToDateTime(startTimeObj) : DateTime.Now);
                TimeSpan span = DateTime.Now - refTime;
                elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
            }

            view.BorderColor = cardBorder;
            card.BackColor = cardBg;

            // Paint Card Border and Rounded styling
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color bColor = (card.Tag is TableCardView cv) ? cv.BorderColor : Color.FromArgb(51, 65, 85);
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                using (Pen pen = new Pen(bColor, 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Left Section: Table Number
            Label lblNum = new Label
            {
                Text = tableNum,
                Font = new Font("Segoe UI", tableNum.Length > 3 ? 15F : 24F, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(14, tableNum.Contains("-") ? 10 : 18),
                Size = new Size(cardW - 110, tableNum.Contains("-") ? 38 : 55),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblNum);
            view.LblNumber = lblNum;

            if (currentFilterMode == "TAKEAWAY")
            {
                Label lblSub = new Label
                {
                    Text = $"🛍️ Token {tableNum}",
                    Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(254, 215, 170),
                    Location = new Point(14, 48),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblSub);
                view.LblSub = lblSub;
            }
            else if (currentFilterMode == "DELIVERY")
            {
                Label lblSub = new Label
                {
                    Text = $"🛵 Order {tableNum}",
                    Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(186, 230, 253),
                    Location = new Point(14, 48),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblSub);
                view.LblSub = lblSub;
            }
            else if (tableNum.Contains("-"))
            {
                char custChar = tableNum.Substring(tableNum.IndexOf('-') + 1)[0];
                Label lblShared = new Label
                {
                    Text = $"🪑 Cust {custChar}",
                    Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(254, 240, 138), // Amber/Yellow
                    Location = new Point(14, 48),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblShared);
                view.LblSub = lblShared;
            }

            // Right Section: Timer & Amount
            Panel rightBox = new Panel
            {
                Location = new Point(cardW - 95, 12),
                Size = new Size(88, 100),
                BackColor = Color.FromArgb(30, 0, 0, 0), // Semi-transparent badge
                Visible = (status == "Running" || status == "Printed")
            };
            SetDoubleBuffered(rightBox);

            Label lblTimer = new Label
            {
                Text = elapsedStr,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 231, 255),
                Location = new Point(2, 6),
                Size = new Size(84, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            rightBox.Controls.Add(lblTimer);
            view.LblTimer = lblTimer;

            Label lblAmt = new Label
            {
                Text = $"₹{amount:0}",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(2, 32),
                Size = new Size(84, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            rightBox.Controls.Add(lblAmt);
            view.LblAmount = lblAmt;

            Label lblKot = new Label
            {
                Text = !string.IsNullOrEmpty(kots) ? $"KOT: {kots}" : "",
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(2, 68),
                Size = new Size(84, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Visible = !string.IsNullOrEmpty(kots)
            };
            rightBox.Controls.Add(lblKot);
            view.LblKot = lblKot;

            card.Controls.Add(rightBox);
            view.RightBox = rightBox;

            // Available Badge
            Label lblAvail = new Label
            {
                Text = "Available",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(14, cardH - 35),
                AutoSize = true,
                BackColor = Color.Transparent,
                Visible = (status == "Available")
            };
            card.Controls.Add(lblAvail);
            view.LblAvail = lblAvail;

            card.Tag = view;
            activeCardViews[tableNum] = view;

            // Click Handler to Open POS Billing for this Table
            void HandleCardClick()
            {
                string orderType = currentFilterMode == "TAKEAWAY" ? "TAKEAWAY" : (currentFilterMode == "DELIVERY" ? "DELIVERY" : "DINING");
                OnTableSelected?.Invoke(tableNum, orderType);
            }

            void AttachClickRecursively(Control parent)
            {
                parent.Cursor = Cursors.Hand;
                parent.MouseUp += (s, e) => {
                    if (e.Button == MouseButtons.Right)
                    {
                        ShowTableContextMenu(tableNum, status, card, e.Location);
                    }
                    else if (e.Button == MouseButtons.Left)
                    {
                        HandleCardClick();
                    }
                };
                foreach (Control child in parent.Controls)
                {
                    AttachClickRecursively(child);
                }
            }

            AttachClickRecursively(card);

            return card;
        }

        private void ShowTableContextMenu(string tableNum, string status, Control sourceControl, Point pt)
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(20, 27, 42),
                ForeColor = Color.White,
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Renderer = new SalesBillingControl.DarkMenuRenderer()
            };

            var itemOpen = new ToolStripMenuItem($"🍽️ Open Order ({tableNum})");
            itemOpen.Click += (s, e) => {
                string orderType = currentFilterMode == "TAKEAWAY" ? "TAKEAWAY" : (currentFilterMode == "DELIVERY" ? "DELIVERY" : "DINING");
                OnTableSelected?.Invoke(tableNum, orderType);
            };
            menu.Items.Add(itemOpen);

            if (status == "Running" || status == "Printed")
            {
                menu.Items.Add(new ToolStripSeparator());

                var itemCancel = new ToolStripMenuItem("🚫 Cancel Order / Void KOT")
                {
                    ForeColor = Color.FromArgb(248, 113, 113) // Soft Red
                };
                itemCancel.Click += (s, e) => CancelOrderFromFloor(tableNum);
                menu.Items.Add(itemCancel);

                var itemShift = new ToolStripMenuItem("🔁 Shift Table");
                itemShift.Click += (s, e) => {
                    using (TableShiftDialog dlg = new TableShiftDialog(tableNum))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK) LoadTableCards();
                    }
                };
                menu.Items.Add(itemShift);

                var itemShare = new ToolStripMenuItem("🪑 Share Table");
                itemShare.Click += (s, e) => BtnShareTable_Click(tableNum);
                menu.Items.Add(itemShare);
            }

            menu.Show(sourceControl, pt);
        }

        private void CancelOrderFromFloor(string tableNum)
        {
            int activeCount = 0;
            decimal totalGross = 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(kd.Amount), 0), COUNT(DISTINCT k.Id)
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId AND kd.IsVoided = 0
                        WHERE k.TableNumber = @tNum AND k.Status IN ('Active', 'Served', 'Printed')", conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", tableNum);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                totalGross = Convert.ToDecimal(rdr[0]);
                                activeCount = Convert.ToInt32(rdr[1]);
                            }
                        }
                    }
                }
            }
            catch { }

            if (activeCount == 0)
            {
                MessageBox.Show($"Table {tableNum} does not have any active kitchen orders to cancel.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string title = $"Table {tableNum} ({activeCount} KOT, ₹{totalGross:N0})";

            using (VoidKotDialog dlg = new VoidKotDialog(title, 1))
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
                                    string findKotsSql = @"
                                        SELECT Id FROM KOTMaster 
                                        WHERE TableNumber = @tNum 
                                          AND Status IN ('Active', 'Served', 'Printed')";

                                    using (SqlCommand cmd = new SqlCommand(findKotsSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@tNum", tableNum);
                                        using (SqlDataReader rdr = cmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                affectedKotIds.Add(Convert.ToInt32(rdr["Id"]));
                                            }
                                        }
                                    }

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
                                        cmd.Parameters.AddWithValue("@tNum", tableNum);
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

                        LoadTableCards();
                        MainForm.Instance?.RefreshLiveOrderCounts();

                        MessageBox.Show(
                            $"Order for Table {tableNum} was CANCELLED and VOIDED successfully.\n\nReason: {reason}\nAudit record saved to KOT & Reports Register.\nTable is now Available.",
                            "Order Cancelled",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to cancel order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnTableShift_Click(object sender, EventArgs e)
        {
            using (TableShiftDialog dlg = new TableShiftDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadTableCards();
                }
            }
        }

        private void BtnShareTable_Click(string targetTable)
        {
            using (TableShareDialog dlg = new TableShareDialog(targetTable))
            {
                if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedTableNumber))
                {
                    LoadTableCards();
                    OnTableSelected?.Invoke(dlg.SelectedTableNumber, "DINING");
                }
            }
        }
    }
}
