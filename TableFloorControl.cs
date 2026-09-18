using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class TableFloorControl : UserControl
    {
        private Panel topHeaderPanel;
        private FlowLayoutPanel modeTabsPanel;
        private FlowLayoutPanel tableGridPanel;
        private Panel bottomLegendPanel;
        private System.Windows.Forms.Timer refreshTimer;

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

        public event Action<string, string> OnTableSelected; // tableNumber, orderType

        public TableFloorControl()
        {
            InitializeComponent();
            LoadTableCards();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000; // Refresh every 5 seconds for live timers and states
            refreshTimer.Tick += (s, e) => LoadTableCards();
            refreshTimer.Start();
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

            btnModeDining = CreateModeTabButton("🍽️ DINING", "DINING", true);
            btnModeTakeaway = CreateModeTabButton("🛍️ TAKE AWAY", "TAKEAWAY", false);
            btnModeDelivery = CreateModeTabButton("🛵 DELIVERY", "DELIVERY", false);
            btnModeWaiting = CreateModeTabButton("⏳ WAITING", "WAITING", false);

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
                if (c is Panel card)
                {
                    card.Size = new Size(cardW, cardH);
                    foreach (Control child in card.Controls)
                    {
                        if (child is Label lbl && lbl.Font.Size >= 15F) // Table number
                        {
                            lbl.Size = new Size(cardW - 110, 55);
                        }
                        else if (child is Panel rBox) // Timer / amount box
                        {
                            rBox.Location = new Point(cardW - 95, 12);
                        }
                        else if (child is Label lblA && lblA.Text == "Available")
                        {
                            lblA.Location = new Point(14, cardH - 35);
                        }
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
            btn.Click += (s, e) => {
                currentFilterMode = mode;
                foreach (Control c in modeTabsPanel.Controls)
                {
                    if (c is Button b) UpdateTabStyle(b, b.Tag?.ToString() == mode);
                }
                LoadTableCards();
            };
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
                            ISNULL(SUM(GrandTotal), 0) AS DaySales,
                            ISNULL(SUM(DueAmount), 0) AS UnsettledSales
                        FROM Sales 
                        WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
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
                tableGridPanel.SuspendLayout();
                tableGridPanel.Controls.Clear();

                int availW = tableGridPanel.ClientSize.Width - tableGridPanel.Padding.Horizontal - 25;
                if (availW < 200) availW = 400;
                int cols = Math.Max(2, Math.Min(6, availW / 215));
                int cardW = Math.Max(180, (availW / cols) - 18);
                int cardH = 125;

                List<TableCardData> tableList = new List<TableCardData>();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Fetch live active KOT data mapped by TableNumber
                    Dictionary<string, LiveKotData> liveKotMap = new Dictionary<string, LiveKotData>(StringComparer.OrdinalIgnoreCase);
                    string kotQuery = @"
                        SELECT k.TableNumber, k.KOTNumber, k.CreatedAt, k.Steward, kd.Amount
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0";

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

                    // 2. Fetch all active tables from CafeTables
                    string query = @"
                        SELECT TableNumber, TableName, Status, CurrentBillAmount, 
                               OrderStartTime, BilledTime, ActiveKotNumbers, CurrentSteward
                        FROM CafeTables 
                        WHERE IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string tNum = r["TableNumber"].ToString();
                            string tName = r["TableName"]?.ToString() ?? tNum;
                            string status = r["Status"]?.ToString() ?? "Available";
                            decimal amount = r["CurrentBillAmount"] != DBNull.Value ? Convert.ToDecimal(r["CurrentBillAmount"]) : 0;
                            object startTimeObj = r["OrderStartTime"];
                            object billedTimeObj = r["BilledTime"];
                            string kots = r["ActiveKotNumbers"]?.ToString() ?? "";
                            string steward = r["CurrentSteward"]?.ToString() ?? "";

                            // If live KOTs exist for this table, reflect live data
                            if (liveKotMap.TryGetValue(tNum, out LiveKotData live))
                            {
                                status = "Running";
                                amount = live.TotalAmount;
                                startTimeObj = live.EarliestTime;
                                live.KotNumbers.Sort();
                                kots = string.Join(",", live.KotNumbers);
                                if (!string.IsNullOrEmpty(live.Steward)) steward = live.Steward;
                            }
                            else if (status == "Running" && amount == 0)
                            {
                                status = "Available";
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

                // 3. Sort tables naturally (1, 2, 3, 4-A, 4-B, 5, 6... 10)
                tableList.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));

                foreach (var t in tableList)
                {
                    // Filter logic
                    bool isWaiting = t.TableNumber.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase);
                    if (currentFilterMode == "WAITING" && !isWaiting) continue;
                    if (currentFilterMode == "DINING" && isWaiting) continue;

                    Control card = CreateTableCard(t.TableNumber, t.TableName, t.Status, t.Amount, t.StartTime, t.BilledTime, t.Kots, t.Steward, cardW, cardH);
                    tableGridPanel.Controls.Add(card);
                }

                tableGridPanel.ResumeLayout();
            }
            catch
            {
                tableGridPanel.ResumeLayout();
                // Avoid popup spamming in timer
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

        private Control CreateTableCard(string tableNum, string tableName, string status, decimal amount, object startTimeObj, object billedTimeObj, string kots, string steward, int cardW = 210, int cardH = 125)
        {
            Panel card = new Panel
            {
                Size = new Size(cardW, cardH),
                Margin = new Padding(8),
                Cursor = Cursors.Hand,
                Tag = tableNum
            };

            // Color Schemes based on screenshot
            // Available: Soft Slate/White background with subtle borders
            // Running: Soft Violet background (#6366f1 / #4f46e5) with timer
            // Printed: Soft Teal/Cyan background (#06b6d4 / #0891b2) with timer
            Color cardBg = Color.FromArgb(30, 41, 59);      // Default Available
            Color cardBorder = Color.FromArgb(51, 65, 85);
            Color textMain = Color.White;

            string elapsedStr = "";
            if (status == "Running" && startTimeObj != DBNull.Value)
            {
                cardBg = Color.FromArgb(67, 56, 202); // Deep Indigo/Violet
                cardBorder = Color.FromArgb(129, 140, 248);
                TimeSpan span = DateTime.Now - Convert.ToDateTime(startTimeObj);
                elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
            }
            else if (status == "Printed" && (billedTimeObj != DBNull.Value || startTimeObj != DBNull.Value))
            {
                cardBg = Color.FromArgb(14, 116, 144); // Deep Cyan
                cardBorder = Color.FromArgb(56, 189, 248);
                DateTime refTime = billedTimeObj != DBNull.Value ? Convert.ToDateTime(billedTimeObj) : Convert.ToDateTime(startTimeObj);
                TimeSpan span = DateTime.Now - refTime;
                elapsedStr = $"⏱️ {(int)span.TotalMinutes}:{span.Seconds:D2}";
            }

            card.BackColor = cardBg;

            // Paint Card Border and Rounded styling
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                using (Pen pen = new Pen(cardBorder, 2))
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

            if (tableNum.Contains("-"))
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
            }

            // Right Section: Timer & Amount
            if (status == "Running" || status == "Printed")
            {
                Panel rightBox = new Panel
                {
                    Location = new Point(cardW - 95, 12),
                    Size = new Size(88, 100),
                    BackColor = Color.FromArgb(30, 0, 0, 0) // Semi-transparent badge
                };

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

                if (!string.IsNullOrEmpty(kots))
                {
                    Label lblKot = new Label
                    {
                        Text = $"KOT: {kots}",
                        Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(203, 213, 225),
                        Location = new Point(2, 68),
                        Size = new Size(84, 18),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent
                    };
                    rightBox.Controls.Add(lblKot);
                }

                card.Controls.Add(rightBox);
            }
            else
            {
                // Available Badge
                Label lblAvail = new Label
                {
                    Text = "Available",
                    Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(14, cardH - 35),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblAvail);
            }

            // Click Handler to Open POS Billing for this Table
            void HandleCardClick()
            {
                string orderType = currentFilterMode == "TAKEAWAY" ? "TAKEAWAY" : (currentFilterMode == "DELIVERY" ? "DELIVERY" : "DINING");
                OnTableSelected?.Invoke(tableNum, orderType);
            }

            void AttachClickRecursively(Control parent)
            {
                parent.Cursor = Cursors.Hand;
                parent.Click += (s, e) => HandleCardClick();
                foreach (Control child in parent.Controls)
                {
                    AttachClickRecursively(child);
                }
            }

            AttachClickRecursively(card);

            return card;
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
