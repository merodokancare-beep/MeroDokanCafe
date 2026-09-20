using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class KotManagerControl : UserControl
    {
        private Panel topPanel;
        private FlowLayoutPanel modeTabsPanel;
        private Panel contentHostPanel;
        private FlowLayoutPanel kdsCardsPanel;
        private Panel historyPanel;
        private DataGridView gridHistory;
        private Button btnTabLive;
        private Button btnTabHistory;
        private Button btnTabVoided;
        private System.Windows.Forms.Timer refreshTimer;
        private Button btnRefresh;
        private Button btnClearHistory;
        private Label lblActiveCount;

        private int currentTabIndex = 0; // 0 = Live, 1 = History

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: Double-buffers all child controls
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

        public KotManagerControl()
        {
            InitializeComponent();
            SetDoubleBuffered(this);
            SetDoubleBuffered(contentHostPanel);
            SetDoubleBuffered(kdsCardsPanel);
            LoadActiveKdsCards();

            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += (s, e) => {
                if (currentTabIndex == 0) LoadActiveKdsCards();
            };
            refreshTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (refreshTimer != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.Secondary;
            this.Padding = new Padding(12);

            // ================= 1. TOP HEADER & ACTION BAR =================
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.CardBg,
                Padding = new Padding(10, 8, 10, 8)
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

            // Left side: Modern Tab Pills (Live Display vs KOT History)
            modeTabsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnTabLive = CreateTabButton("🔥 Live Kitchen Display", true);
            btnTabLive.Click += (s, e) => SwitchTab(0);
            modeTabsPanel.Controls.Add(btnTabLive);

            btnTabHistory = CreateTabButton("📜 All KOT Logs", false);
            btnTabHistory.Click += (s, e) => SwitchTab(1);
            modeTabsPanel.Controls.Add(btnTabHistory);

            btnTabVoided = CreateTabButton("🚫 Cancelled / Voided Orders", false);
            btnTabVoided.Click += (s, e) => SwitchTab(2);
            modeTabsPanel.Controls.Add(btnTabVoided);

            topHeaderTable.Controls.Add(modeTabsPanel, 0, 0);

            // Right side: Active KOT summary & Refresh
            FlowLayoutPanel rightActionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, 0, 0)
            };

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(12, 0, 12, 0),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => {
                if (currentTabIndex == 0) LoadActiveKdsCards();
                else LoadKotHistory();
            };
            rightActionsFlow.Controls.Add(btnRefresh);

            btnClearHistory = new Button
            {
                Text = "🗑️ Clear History Logs",
                AutoSize = true,
                Height = 36,
                Padding = new Padding(12, 0, 12, 0),
                BackColor = Theme.Danger,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0),
                Visible = false
            };
            btnClearHistory.FlatAppearance.BorderSize = 0;
            btnClearHistory.Click += (s, e) => ClearKotHistory();
            rightActionsFlow.Controls.Add(btnClearHistory);

            lblActiveCount = new Label
            {
                Text = "0 Active KOTs",
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(12, 8, 6, 0)
            };
            rightActionsFlow.Controls.Add(lblActiveCount);

            topHeaderTable.Controls.Add(rightActionsFlow, 1, 0);
            topPanel.Controls.Add(topHeaderTable);
            this.Controls.Add(topPanel);

            // ================= 2. CONTENT HOST PANEL =================
            contentHostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Secondary,
                Padding = new Padding(0, 10, 0, 0)
            };

            // 2a. Live KDS Cards Grid Panel
            kdsCardsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Theme.Secondary,
                Padding = new Padding(6)
            };
            kdsCardsPanel.SizeChanged += (s, e) => AdjustKdsCardSizes();
            contentHostPanel.Controls.Add(kdsCardsPanel);

            // 2b. History Logs Grid Panel
            historyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Secondary,
                Visible = false
            };

            gridHistory = new DataGridView();
            Theme.StyleGrid(gridHistory);
            gridHistory.Dock = DockStyle.Fill;
            gridHistory.CellFormatting += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < gridHistory.Rows.Count)
                {
                    var row = gridHistory.Rows[e.RowIndex];
                    string status = row.Cells["Status"]?.Value?.ToString() ?? "";
                    if (status.Equals("Voided", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(248, 113, 113); // Soft Red
                        row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(254, 202, 202);
                    }
                    else if (status.Equals("Served", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
                    }
                }
            };
            gridHistory.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < gridHistory.Rows.Count)
                {
                    if (gridHistory.Rows[e.RowIndex].Cells["Id"] != null)
                    {
                        int kotId = Convert.ToInt32(gridHistory.Rows[e.RowIndex].Cells["Id"].Value);
                        ShowKotDetailsModal(kotId);
                    }
                }
            };

            historyPanel.Controls.Add(gridHistory);
            contentHostPanel.Controls.Add(historyPanel);

            this.Controls.Add(contentHostPanel);

            topPanel.SendToBack();
            contentHostPanel.BringToFront();
        }

        private Button CreateTabButton(string text, bool isActive)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 36,
                Padding = new Padding(14, 0, 14, 0),
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            UpdateTabStyle(btn, isActive);
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

        private void SwitchTab(int tabIndex)
        {
            currentTabIndex = tabIndex;
            UpdateTabStyle(btnTabLive, currentTabIndex == 0);
            UpdateTabStyle(btnTabHistory, currentTabIndex == 1);
            UpdateTabStyle(btnTabVoided, currentTabIndex == 2);
            if (btnClearHistory != null) btnClearHistory.Visible = (currentTabIndex == 1);

            if (currentTabIndex == 0)
            {
                historyPanel.Visible = false;
                kdsCardsPanel.Visible = true;
                LoadActiveKdsCards();
            }
            else
            {
                kdsCardsPanel.Visible = false;
                historyPanel.Visible = true;
                LoadKotHistory();
            }
        }

        public void LoadActiveKdsCards()
        {
            try
            {
                kdsCardsPanel.SuspendLayout();
                kdsCardsPanel.Controls.Clear();

                int totalCount = 0;
                int cookingCount = 0;
                int servedCount = 0;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, KOTNumber, TableNumber, OrderType, Steward, CreatedAt, 
                               ISNULL(KotComment, '') AS KotComment, Status
                        FROM KOTMaster
                        WHERE Status IN ('Active', 'Served', 'Printed') AND IsVoided = 0 AND SaleId IS NULL
                        ORDER BY 
                            CASE Status WHEN 'Active' THEN 1 WHEN 'Printed' THEN 2 ELSE 3 END,
                            CreatedAt ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            totalCount++;
                            int kotId = Convert.ToInt32(r["Id"]);
                            int kotNum = Convert.ToInt32(r["KOTNumber"]);
                            string tNum = r["TableNumber"].ToString();
                            string orderType = r["OrderType"].ToString();
                            string steward = r["Steward"]?.ToString() ?? "";
                            DateTime createdAt = Convert.ToDateTime(r["CreatedAt"]);
                            string comment = r["KotComment"].ToString();
                            string status = r["Status"].ToString();

                            if (status == "Active") cookingCount++;
                            else if (status == "Served") servedCount++;

                            Control card = CreateKdsCard(kotId, kotNum, tNum, orderType, steward, createdAt, comment, status);
                            kdsCardsPanel.Controls.Add(card);
                        }
                    }
                }

                if (totalCount == 0)
                {
                    lblActiveCount.Text = "0 Active KOTs";
                    btnTabLive.Text = "🔥 Live Kitchen Display";

                    Panel emptyCard = new Panel
                    {
                        Size = new Size(Math.Max(300, kdsCardsPanel.ClientSize.Width - 40), 220),
                        BackColor = Theme.CardBg,
                        Margin = new Padding(12)
                    };
                    Label lblEmpty = new Label
                    {
                        Text = "✨ All Caught Up!\nNo pending kitchen orders right now.",
                        Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                        ForeColor = Theme.TextMuted,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    emptyCard.Controls.Add(lblEmpty);
                    kdsCardsPanel.Controls.Add(emptyCard);
                }
                else
                {
                    lblActiveCount.Text = $"{totalCount} Live ({cookingCount} Cooking, {servedCount} Served)";
                    btnTabLive.Text = $"🔥 Live Kitchen ({totalCount})";
                }

                kdsCardsPanel.ResumeLayout();
                AdjustKdsCardSizes();
            }
            catch
            {
                kdsCardsPanel.ResumeLayout();
            }
        }

        private void AdjustKdsCardSizes()
        {
            if (kdsCardsPanel == null || kdsCardsPanel.Controls.Count == 0) return;
            kdsCardsPanel.SuspendLayout();

            int availW = kdsCardsPanel.ClientSize.Width - kdsCardsPanel.Padding.Horizontal - 25;
            if (availW < 260) availW = 280;
            int cols = Math.Max(1, Math.Min(5, availW / 280));
            int cardW = Math.Max(260, (availW / cols) - 16);
            int cardH = 340;

            foreach (Control c in kdsCardsPanel.Controls)
            {
                if (c is Panel card && card.Tag?.ToString() == "KDSCARD")
                {
                    card.Size = new Size(cardW, cardH);
                }
            }

            kdsCardsPanel.ResumeLayout();
        }

        private Control CreateKdsCard(int kotId, int kotNum, string tableNum, string orderType, string steward, DateTime createdAt, string comment, string status)
        {
            TimeSpan elapsed = DateTime.Now - createdAt;
            bool isDelayed = (elapsed.TotalMinutes > 15);
            bool isServed = (status == "Served");
            bool isPrinted = (status == "Printed");

            int cardW = 280;
            int cardH = 340;

            Panel card = new Panel
            {
                Size = new Size(cardW, cardH),
                Margin = new Padding(10),
                BackColor = Color.FromArgb(17, 24, 39), // Obsidian Card Slate
                Tag = "KDSCARD"
            };

            // Theme colors based on status & duration
            Color headerBg;
            Color cardBorder;

            if (isServed)
            {
                headerBg = Color.FromArgb(4, 120, 87); // Forest Emerald for Served
                cardBorder = Color.FromArgb(16, 185, 129);
            }
            else if (isPrinted)
            {
                headerBg = Color.FromArgb(14, 116, 144); // Deep Cyan
                cardBorder = Color.FromArgb(6, 182, 212);
            }
            else if (isDelayed)
            {
                headerBg = Color.FromArgb(185, 28, 28); // Crimson if delayed > 15 min
                cardBorder = Color.FromArgb(239, 68, 68);
            }
            else
            {
                headerBg = Color.FromArgb(67, 56, 202); // Deep Indigo
                cardBorder = Color.FromArgb(99, 102, 241);
            }

            // ================= 1. HEADER PANEL =================
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = headerBg,
                Padding = new Padding(10, 8, 10, 8)
            };

            // Top Row in Header: KOT Number (Left) + Table Number Pill (Right)
            Label lblKotNum = new Label
            {
                Text = $"KOT #{kotNum}",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(8, 8),
                AutoSize = true
            };
            header.Controls.Add(lblKotNum);

            string tableLabel = tableNum.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase) ? tableNum : $"TABLE {tableNum}";
            if (orderType == "TAKEAWAY") tableLabel = $"🛍️ TOKEN {tableNum}";
            else if (orderType == "DELIVERY") tableLabel = $"🛵 {tableNum}";

            Label lblTableBadge = new Label
            {
                Text = tableLabel,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 240, 138), // Soft yellow/amber
                BackColor = Color.FromArgb(50, 0, 0, 0), // Semi-transparent badge
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true,
                Location = new Point(cardW - 120, 8),
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(lblTableBadge);

            // Bottom Row in Header: Status Tag | Steward | Timer
            string timeStr = elapsed.TotalMinutes < 1 ? "Just now" : $"⏱️ {(int)elapsed.TotalMinutes}m ago";
            string stwdStr = string.IsNullOrEmpty(steward) ? "Direct" : steward;
            string statusTag = isServed ? "✓ SERVED" : (isPrinted ? "🧾 BILLED" : "🔥 COOKING");

            Label lblSub = new Label
            {
                Text = $"{statusTag} • {orderType} • {timeStr}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(224, 231, 255),
                Location = new Point(8, 38),
                AutoSize = true
            };
            header.Controls.Add(lblSub);

            // ================= 2. ITEMS LIST (SCROLLABLE) =================
            FlowLayoutPanel itemsList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 23, 42), // Dark Slate
                Padding = new Padding(10, 8, 10, 8)
            };

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ItemName, Quantity, ISNULL(Instructions, '') AS Instructions FROM KOTDetails WHERE KOTId = @id AND IsVoided = 0", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", kotId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string name = r["ItemName"].ToString();
                                int qty = Convert.ToInt32(r["Quantity"]);
                                string instructions = r["Instructions"].ToString();

                                Panel itemRow = new Panel
                                {
                                    Width = cardW - 40,
                                    AutoSize = true,
                                    Margin = new Padding(0, 3, 0, 4),
                                    BackColor = Color.Transparent
                                };

                                Label lblItemName = new Label
                                {
                                    Text = $"• {name}",
                                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                                    ForeColor = isServed ? Color.FromArgb(203, 213, 225) : Theme.TextWhite,
                                    AutoSize = true,
                                    Location = new Point(0, 0),
                                    MaximumSize = new Size(cardW - 85, 0)
                                };
                                itemRow.Controls.Add(lblItemName);

                                Label lblQty = new Label
                                {
                                    Text = $"x {qty}",
                                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                                    ForeColor = isServed ? Color.FromArgb(52, 211, 153) : Theme.Accent,
                                    AutoSize = true,
                                    Location = new Point(cardW - 80, 0),
                                    TextAlign = ContentAlignment.TopRight
                                };
                                itemRow.Controls.Add(lblQty);

                                if (!string.IsNullOrEmpty(instructions))
                                {
                                    Label lblInst = new Label
                                    {
                                        Text = $"  ↳ {instructions}",
                                        Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                                        ForeColor = Color.FromArgb(251, 191, 36), // Amber
                                        AutoSize = true,
                                        Location = new Point(6, 20),
                                        MaximumSize = new Size(cardW - 55, 0)
                                    };
                                    itemRow.Controls.Add(lblInst);
                                }

                                itemsList.Controls.Add(itemRow);
                            }
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(comment))
            {
                Panel noteBox = new Panel
                {
                    Width = cardW - 40,
                    AutoSize = true,
                    BackColor = Color.FromArgb(40, 245, 158, 11),
                    Padding = new Padding(6),
                    Margin = new Padding(0, 6, 0, 4)
                };
                Label lblNote = new Label
                {
                    Text = $"⚠️ Note: {comment}",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(252, 211, 77),
                    Dock = DockStyle.Fill,
                    AutoSize = true
                };
                noteBox.Controls.Add(lblNote);
                itemsList.Controls.Add(noteBox);
            }

            // ================= 3. BOTTOM ACTION BUTTONS =================
            Panel bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(17, 24, 39),
                Padding = new Padding(8, 6, 8, 6)
            };

            Button btnReprint = new Button
            {
                Text = "🖨️ Reprint",
                Height = 34,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReprint.FlatAppearance.BorderSize = 1;
            btnReprint.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnReprint.Click += (s, e) => ThermalReceiptPrinter.PrintKOT(kotId);

            Button btnServed = new Button
            {
                Text = isServed ? "✓ Served (Pending Pay)" : "✓ Mark Served",
                Height = 34,
                BackColor = isServed ? Color.FromArgb(6, 95, 70) : Theme.Success, // Muted emerald if already served, bright emerald if active
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnServed.FlatAppearance.BorderSize = 0;
            btnServed.Click += (s, e) => ToggleKotServedStatus(kotId, isServed ? "Active" : "Served");

            bottom.Controls.Add(btnReprint);
            bottom.Controls.Add(btnServed);

            void LayoutBottomButtons()
            {
                int pw = bottom.ClientSize.Width - 16;
                int bHalf = (pw - 8) / 2;
                btnReprint.Location = new Point(8, 7);
                btnReprint.Size = new Size(bHalf, 34);

                btnServed.Location = new Point(8 + bHalf + 8, 7);
                btnServed.Size = new Size(bHalf, 34);
            }

            bottom.SizeChanged += (s, e) => LayoutBottomButtons();
            LayoutBottomButtons();

            // Correct WinForms Z-Order Docking
            card.Controls.Add(itemsList);
            card.Controls.Add(bottom);
            card.Controls.Add(header);
            itemsList.BringToFront();

            // Paint Card Border
            card.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                using (Pen pen = new Pen(cardBorder, 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            return card;
        }

        private void ToggleKotServedStatus(int kotId, string newStatus)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE KOTMaster SET Status = @status WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        cmd.Parameters.AddWithValue("@id", kotId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadActiveKdsCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating KOT: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadKotHistory()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    string whereClause = "";
                    if (currentTabIndex == 2)
                    {
                        whereClause = "WHERE (k.Status = 'Voided' OR k.IsVoided = 1 OR EXISTS (SELECT 1 FROM KOTDetails kd WHERE kd.KOTId = k.Id AND kd.IsVoided = 1))";
                    }

                    string query = $@"
                        SELECT k.Id, 
                               k.KOTNumber AS [KOT #], 
                               k.TableNumber AS [Table / Token], 
                               k.OrderType AS [Order Type], 
                               ISNULL(k.Steward, '-') AS [Steward], 
                               k.Status AS [Status],
                               (SELECT COUNT(*) FROM KOTDetails WHERE KOTId = k.Id) AS [Total Items],
                               (SELECT ISNULL(SUM(Amount), 0) FROM KOTDetails WHERE KOTId = k.Id AND IsVoided = 1) AS [Void / Loss (Rs.)],
                               ISNULL(k.VoidReason, (SELECT TOP 1 VoidReason FROM KOTDetails WHERE KOTId = k.Id AND IsVoided = 1)) AS [Cancellation Reason],
                               k.VoidedAt AS [Cancelled At],
                               k.CreatedAt AS [Created Time],
                               ISNULL(s.InvoiceNumber, '-') AS [Settled Invoice],
                               ISNULL(k.KotComment, '') AS [Notes]
                        FROM KOTMaster k
                        LEFT JOIN Sales s ON k.SaleId = s.Id
                        {whereClause}
                        ORDER BY k.Id DESC";

                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridHistory.DataSource = dt;

                        if (gridHistory.Columns["Id"] != null)
                            gridHistory.Columns["Id"].Visible = false;
                    }
                }
            }
            catch { }
        }

        private void ShowKotDetailsModal(int kotId)
        {
            try
            {
                Form dlg = new Form
                {
                    Text = $"KOT #{kotId} Details & Audit Trail",
                    Size = new Size(680, 480),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Theme.CardBg,
                    ForeColor = Theme.TextLight,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                DataGridView grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true
                };
                Theme.StyleGrid(grid);

                grid.CellFormatting += (s, e) => {
                    if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count)
                    {
                        var row = grid.Rows[e.RowIndex];
                        string voided = row.Cells["Voided?"]?.Value?.ToString() ?? "";
                        if (voided.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                        {
                            row.DefaultCellStyle.ForeColor = Color.FromArgb(248, 113, 113);
                        }
                    }
                };

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT ItemName AS [Item Name], 
                               Quantity AS [Qty], 
                               Rate AS [Rate (Rs.)], 
                               Amount AS [Total (Rs.)], 
                               CASE WHEN IsVoided = 1 THEN 'Yes' ELSE 'No' END AS [Voided?],
                               ISNULL(VoidReason, '-') AS [Void Reason],
                               VoidedAt AS [Voided At],
                               ISNULL(Instructions, '') AS [Instructions]
                        FROM KOTDetails
                        WHERE KOTId = @id
                        ORDER BY Id ASC";

                    using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@id", kotId);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        grid.DataSource = dt;
                    }
                }

                dlg.Controls.Add(grid);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to view KOT details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearKotHistory()
        {
            var res = MessageBox.Show(
                "Are you sure you want to permanently delete all billed, voided, and completed KOT history logs?\n\nActive live kitchen tickets will not be affected.",
                "Confirm Clear KOT History",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (res != DialogResult.Yes) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string delDetails = @"
                                DELETE FROM KOTDetails 
                                WHERE KOTId IN (SELECT Id FROM KOTMaster WHERE Status IN ('Billed', 'Voided') OR SaleId IS NOT NULL)";
                            using (SqlCommand cmd = new SqlCommand(delDetails, conn, trans))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            string delMaster = @"
                                DELETE FROM KOTMaster 
                                WHERE Status IN ('Billed', 'Voided') OR SaleId IS NOT NULL";
                            using (SqlCommand cmd = new SqlCommand(delMaster, conn, trans))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            // If no KOTs remain at all, reseed counter back to 0
                            using (SqlCommand cmd = new SqlCommand(@"
                                IF NOT EXISTS (SELECT 1 FROM KOTMaster)
                                BEGIN
                                    DBCC CHECKIDENT ('KOTMaster', RESEED, 0);
                                    DBCC CHECKIDENT ('KOTDetails', RESEED, 0);
                                END
                            ", conn, trans))
                            {
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

                MessageBox.Show("All settled KOT history logs have been cleared successfully.", "History Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadKotHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear KOT history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
