using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class DashboardControl : UserControl, IFocusableControl
    {
        private Panel cardFoodRevenue;
        private Panel cardBeverageRevenue;
        private Panel cardOrdersCount;
        private Panel cardStaffCount;

        private Label lblFoodVal, lblFoodSub;
        private Label lblBeverageVal, lblBeverageSub;
        private Label lblOrdersVal, lblOrdersSub;
        private Label lblStaffVal, lblStaffSub;

        private DataGridView gridLiveOrders;
        private DataGridView gridTopStewards;
        private Panel chartPanel;

        private decimal totalFoodSales = 0;
        private decimal totalBeverageSales = 0;
        private decimal totalGrossRevenue = 0;
        private int todayKotCount = 0;
        private int activeKotCount = 0;
        private int settledTodayCount = 0;
        private int activeStaffCount = 0;

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                gridLiveOrders?.Focus();
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.Secondary;
            this.DoubleBuffered = true;
            this.Padding = new Padding(16, 12, 16, 12);
            this.AutoScroll = true;

            // ================= 1. HEADER PANEL =================
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.Transparent
            };

            Label lblWelcome = new Label
            {
                Text = $"Welcome Back, {Session.FullName}",
                Location = new Point(0, 0),
                AutoSize = true
            };
            Theme.StyleLabel(lblWelcome, Theme.TextLight, Theme.HeaderFont);
            headerPanel.Controls.Add(lblWelcome);

            Label lblSubtitle = new Label
            {
                Text = $"☕ The Local Cafe • Executive Operations & Live Floor Intelligence • {DateTime.Now:dddd, MMMM dd, yyyy}",
                Location = new Point(2, 30),
                AutoSize = true
            };
            Theme.StyleLabel(lblSubtitle, Theme.TextMuted, Theme.MainFont);
            headerPanel.Controls.Add(lblSubtitle);

            this.Controls.Add(headerPanel);

            // ================= 2. 4 SMART KPI CARDS =================
            TableLayoutPanel kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 105,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 8)
            };
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            // 1. Food & Dishes Revenue Card
            cardFoodRevenue = Theme.CreateSmartKpiCard(235, 100, "🍽️ FOOD & DISHES", "₹0.00", "Kitchen food sales", Theme.Accent, out lblFoodVal, out lblFoodSub);
            cardFoodRevenue.Dock = DockStyle.Fill;
            cardFoodRevenue.Margin = new Padding(0, 0, 6, 0);
            kpiLayout.Controls.Add(cardFoodRevenue, 0, 0);

            // 2. Beverages & Drinks Card
            cardBeverageRevenue = Theme.CreateSmartKpiCard(235, 100, "☕ BEVERAGES & DRINKS", "₹0.00", "Coffee, teas & counter items", Theme.Success, out lblBeverageVal, out lblBeverageSub);
            cardBeverageRevenue.Dock = DockStyle.Fill;
            cardBeverageRevenue.Margin = new Padding(6, 0, 6, 0);
            kpiLayout.Controls.Add(cardBeverageRevenue, 1, 0);

            // 3. Today's KOTs & Orders Card
            cardOrdersCount = Theme.CreateSmartKpiCard(235, 100, "📋 TODAY'S KOTs & ORDERS", "0 Orders", "0 Active Tables", Theme.Warning, out lblOrdersVal, out lblOrdersSub);
            cardOrdersCount.Dock = DockStyle.Fill;
            cardOrdersCount.Margin = new Padding(6, 0, 6, 0);
            kpiLayout.Controls.Add(cardOrdersCount, 2, 0);

            // 4. Active Stewards & Chefs Card
            cardStaffCount = Theme.CreateSmartKpiCard(235, 100, "🧑‍🍳 ACTIVE CREW ON DUTY", "0 Staff", "Stewards & Chefs", Theme.Info, out lblStaffVal, out lblStaffSub);
            cardStaffCount.Dock = DockStyle.Fill;
            cardStaffCount.Margin = new Padding(6, 0, 0, 0);
            kpiLayout.Controls.Add(cardStaffCount, 3, 0);

            this.Controls.Add(kpiLayout);

            // Spacer Panel
            Panel spacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
            this.Controls.Add(spacer);

            // ================= 3. MAIN BODY SPLIT (CHART & GRIDS) =================
            TableLayoutPanel bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f)); // Left Chart
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f)); // Right Grids

            // 3a. LEFT COLUMN: Chart Panel
            Panel leftColPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 8, 0)
            };

            Label lblChartTitle = new Label
            {
                Text = "📊 Revenue Contribution & Cafe Analytics",
                Dock = DockStyle.Top,
                Height = 28,
                AutoSize = false
            };
            Theme.StyleLabel(lblChartTitle, Theme.TextLight, Theme.SubHeaderFont);
            leftColPanel.Controls.Add(lblChartTitle);

            chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBg,
                Padding = new Padding(12)
            };
            chartPanel.Paint += ChartPanel_Paint;
            chartPanel.SizeChanged += (s, e) => chartPanel.Invalidate();
            leftColPanel.Controls.Add(chartPanel);

            lblChartTitle.SendToBack();
            chartPanel.BringToFront();
            bodyLayout.Controls.Add(leftColPanel, 0, 0);

            // 3b. RIGHT COLUMN: 2 Grids in TableLayoutPanel
            TableLayoutPanel rightGridsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(8, 0, 0, 0)
            };
            rightGridsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            rightGridsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Grid 1: Live Orders & Table Status
            Panel grid1Container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 6)
            };

            Label lblLiveOrdersTitle = new Label
            {
                Text = "🍽️ Live Floor Orders & Active Tables",
                Dock = DockStyle.Top,
                Height = 26,
                AutoSize = false
            };
            Theme.StyleLabel(lblLiveOrdersTitle, Theme.TextLight, Theme.SubHeaderFont);
            grid1Container.Controls.Add(lblLiveOrdersTitle);

            gridLiveOrders = new DataGridView();
            Theme.StyleGrid(gridLiveOrders);
            gridLiveOrders.Dock = DockStyle.Fill;
            gridLiveOrders.CellFormatting += GridLiveOrders_CellFormatting;
            grid1Container.Controls.Add(gridLiveOrders);

            lblLiveOrdersTitle.SendToBack();
            gridLiveOrders.BringToFront();
            rightGridsTable.Controls.Add(grid1Container, 0, 0);

            // Grid 2: Top Stewards & Staff Leaderboard
            Panel grid2Container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0)
            };

            Label lblStewardsTitle = new Label
            {
                Text = "🏆 Top Performing Stewards & Staff Leaderboard",
                Dock = DockStyle.Top,
                Height = 26,
                AutoSize = false
            };
            Theme.StyleLabel(lblStewardsTitle, Theme.TextLight, Theme.SubHeaderFont);
            grid2Container.Controls.Add(lblStewardsTitle);

            gridTopStewards = new DataGridView();
            Theme.StyleGrid(gridTopStewards);
            gridTopStewards.Dock = DockStyle.Fill;
            grid2Container.Controls.Add(gridTopStewards);

            lblStewardsTitle.SendToBack();
            gridTopStewards.BringToFront();
            rightGridsTable.Controls.Add(grid2Container, 0, 1);

            bodyLayout.Controls.Add(rightGridsTable, 1, 0);
            this.Controls.Add(bodyLayout);

            // Correct Z-order of docked panels
            headerPanel.SendToBack();
            kpiLayout.SendToBack();
            spacer.SendToBack();
            bodyLayout.BringToFront();
        }

        private void GridLiveOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridLiveOrders.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "Active" || status == "Running" || status == "Served" || status == "Printed")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(167, 139, 250); // Violet
                    e.CellStyle.Font = Theme.BoldFont;
                }
                else if (status == "Settled" || status == "Completed" || status == "Billed")
                {
                    e.CellStyle.ForeColor = Theme.Success;
                    e.CellStyle.Font = Theme.BoldFont;
                }
                else if (status == "Voided")
                {
                    e.CellStyle.ForeColor = Theme.Danger;
                }
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Food & Dishes Sales
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(sd.Total), 0)
                        FROM SaleDetails sd
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        WHERE p.Category NOT LIKE '%Beverage%' AND p.Category NOT LIKE '%Drink%' AND p.Category NOT LIKE '%Coffee%' AND p.Category NOT LIKE '%Tea%'", conn))
                    {
                        totalFoodSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        lblFoodVal.Text = $"₹{totalFoodSales:N0}";
                    }

                    // 2. Beverages & Drinks Sales
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(sd.Total), 0)
                        FROM SaleDetails sd
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        WHERE p.Category LIKE '%Beverage%' OR p.Category LIKE '%Drink%' OR p.Category LIKE '%Coffee%' OR p.Category LIKE '%Tea%'", conn))
                    {
                        totalBeverageSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        lblBeverageVal.Text = $"₹{totalBeverageSales:N0}";
                    }

                    totalGrossRevenue = totalFoodSales + totalBeverageSales;
                    if (totalGrossRevenue > 0)
                    {
                        decimal foodPct = (totalFoodSales / totalGrossRevenue) * 100m;
                        lblFoodSub.Text = $"{foodPct:0.#}% of Gross Sales Volume";
                        lblBeverageSub.Text = $"{(100m - foodPct):0.#}% from Cafe Drinks & Counter";
                    }

                    // 3. Today's KOTs & Orders
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            ISNULL(COUNT(*), 0) AS TotalKots,
                            ISNULL(SUM(CASE WHEN Status IN ('Active', 'Served', 'Printed', 'Running') THEN 1 ELSE 0 END), 0) AS ActiveKots,
                            ISNULL(SUM(CASE WHEN Status IN ('Billed', 'Settled', 'Completed') THEN 1 ELSE 0 END), 0) AS SettledKots,
                            ISNULL(SUM(CASE WHEN Status = 'Voided' OR IsVoided = 1 THEN 1 ELSE 0 END), 0) AS VoidedKots
                        FROM KOTMaster 
                        WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int activeKots = Convert.ToInt32(r["ActiveKots"]);
                                int settledKots = Convert.ToInt32(r["SettledKots"]);
                                int voidedKots = Convert.ToInt32(r["VoidedKots"]);
                                todayKotCount = Convert.ToInt32(r["TotalKots"]);
                                activeKotCount = activeKots;
                                settledTodayCount = settledKots;

                                lblOrdersVal.Text = todayKotCount == 1 ? "1 Order Today" : $"{todayKotCount} Orders Today";
                                string activeText = activeKots == 1 ? "1 Active KOT" : $"{activeKots} Active KOTs";
                                string settledText = settledKots == 1 ? "1 Settled Order" : $"{settledKots} Settled Orders";
                                if (voidedKots > 0)
                                {
                                    lblOrdersSub.Text = $"{activeText} • {settledText} • {voidedKots} Voided";
                                }
                                else
                                {
                                    lblOrdersSub.Text = $"{activeText} • {settledText}";
                                }
                            }
                        }
                    }

                    // 4. Active Staff Count
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Staff WHERE IsActive = 1", conn))
                    {
                        activeStaffCount = (int)cmd.ExecuteScalar();
                        lblStaffVal.Text = $"{activeStaffCount} Crew Members";
                        lblStaffSub.Text = $"{activeStaffCount} Stewards & Chefs on Duty";
                    }

                    // 5. Live Orders / Recent KOTs Grid
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 10 
                            k.KOTNumber AS [KOT #],
                            k.TableNumber AS [Table / Mode],
                            k.OrderType AS [Type],
                            ISNULL(k.Steward, 'Cashier') AS [Steward],
                            k.Status AS [Status],
                            CASE 
                                WHEN CAST(k.CreatedAt AS DATE) = CAST(GETDATE() AS DATE) 
                                THEN CONVERT(VARCHAR(5), k.CreatedAt, 108)
                                ELSE FORMAT(k.CreatedAt, 'dd-MMM HH:mm')
                            END AS [Time]
                        FROM KOTMaster k
                        ORDER BY 
                            CASE WHEN k.Status IN ('Active', 'Served', 'Printed', 'Running') THEN 1 ELSE 2 END,
                            k.Id DESC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridLiveOrders.DataSource = dt;

                            if (gridLiveOrders.Columns["KOT #"] != null) gridLiveOrders.Columns["KOT #"].FillWeight = 50;
                            if (gridLiveOrders.Columns["Table / Mode"] != null) gridLiveOrders.Columns["Table / Mode"].FillWeight = 85;
                            if (gridLiveOrders.Columns["Type"] != null) gridLiveOrders.Columns["Type"].FillWeight = 75;
                            if (gridLiveOrders.Columns["Steward"] != null) gridLiveOrders.Columns["Steward"].FillWeight = 90;
                            if (gridLiveOrders.Columns["Status"] != null) gridLiveOrders.Columns["Status"].FillWeight = 70;
                            if (gridLiveOrders.Columns["Time"] != null) gridLiveOrders.Columns["Time"].FillWeight = 55;
                        }
                    }

                    // 6. Top Stewards Leaderboard
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 6
                            ROW_NUMBER() OVER (ORDER BY ISNULL(SUM(s.GrandTotal), 0) DESC) AS [Rank],
                            ISNULL(NULLIF(s.SalesPerson, ''), 'Head Steward') AS [Steward Name],
                            COUNT(s.Id) AS [Orders Served],
                            ISNULL(SUM(s.GrandTotal), 0) AS [Total Sales (₹)]
                        FROM Sales s
                        GROUP BY s.SalesPerson
                        ORDER BY [Total Sales (₹)] DESC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridTopStewards.DataSource = dt;

                            if (gridTopStewards.Columns["Rank"] != null) gridTopStewards.Columns["Rank"].FillWeight = 35;
                            if (gridTopStewards.Columns["Steward Name"] != null) gridTopStewards.Columns["Steward Name"].FillWeight = 110;
                            if (gridTopStewards.Columns["Orders Served"] != null) gridTopStewards.Columns["Orders Served"].FillWeight = 75;
                            if (gridTopStewards.Columns["Total Sales (₹)"] != null)
                            {
                                gridTopStewards.Columns["Total Sales (₹)"].DefaultCellStyle.Format = "N2";
                                gridTopStewards.Columns["Total Sales (₹)"].FillWeight = 90;
                            }
                        }
                    }
                }

                chartPanel.Invalidate();
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Font fBold = new Font("Segoe UI", 10, FontStyle.Bold);
            Font fTitle = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            Font fRegular = new Font("Segoe UI", 9F, FontStyle.Regular);

            int w = chartPanel.ClientSize.Width;
            int h = chartPanel.ClientSize.Height;

            // Title
            g.DrawString("Revenue Contribution Share (Kitchen vs Beverages)", fTitle, Brushes.White, 16, 14);

            decimal total = totalFoodSales + totalBeverageSales;
            if (total <= 0)
            {
                g.DrawString("No sales data recorded yet.\nStart billing kitchen dishes and beverages to view analytics.", fRegular, Brushes.Gray, 20, 60);
                return;
            }

            float foodPct = (float)(totalFoodSales / total);
            float bevPct = (float)(totalBeverageSales / total);

            int maxBarW = Math.Max(120, w - 40);

            // Bar 1: Kitchen Food & Dishes
            int barY = 55;
            g.DrawString($"Kitchen Dishes: ₹{totalFoodSales:N2} ({foodPct * 100:0.0}%)", fBold, Brushes.LightCyan, 16, barY);
            using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(16, barY + 22, Math.Max(12, (int)(maxBarW * foodPct)), 20), 5))
            using (Brush b = new SolidBrush(Theme.Accent))
            {
                g.FillPath(b, path);
            }

            // Bar 2: Beverages & Drinks
            int bar2Y = barY + 58;
            g.DrawString($"Beverages & Drinks: ₹{totalBeverageSales:N2} ({bevPct * 100:0.0}%)", fBold, Brushes.LightGreen, 16, bar2Y);
            using (GraphicsPath path = Theme.GetRoundedPath(new Rectangle(16, bar2Y + 22, Math.Max(12, (int)(maxBarW * bevPct)), 20), 5))
            using (Brush b = new SolidBrush(Theme.Success))
            {
                g.FillPath(b, path);
            }

            // Summary Stats box with rounded corners
            int boxY = bar2Y + 58;
            int boxH = Math.Max(160, h - boxY - 14);
            Rectangle boxRect = new Rectangle(16, boxY, Math.Max(160, w - 32), boxH);
            using (GraphicsPath boxPath = Theme.GetRoundedPath(boxRect, 8))
            using (Brush b = new SolidBrush(Color.FromArgb(24, 33, 47)))
            using (Pen p = new Pen(Theme.CardBorder, 1))
            {
                g.FillPath(b, boxPath);
                g.DrawPath(p, boxPath);
            }

            g.DrawString("CAFE & RESTAURANT OPERATIONS SUMMARY", fBold, Brushes.White, 28, boxY + 12);
            g.DrawString($"• Gross Realized Turnover: ₹{total:N2}", fRegular, Brushes.LightGray, 28, boxY + 38);
            g.DrawString($"• Food / Kitchen Share: {foodPct * 100:0.0}% of total turnover", fRegular, Brushes.LightGray, 28, boxY + 62);
            g.DrawString($"• Beverages / Counter Share: {bevPct * 100:0.0}% of total turnover", fRegular, Brushes.LightGray, 28, boxY + 86);
            g.DrawString($"• Active Crew & Stewards: {activeStaffCount} Team members on duty", fRegular, Brushes.LightGray, 28, boxY + 110);
            string orderPlural = todayKotCount == 1 ? "1 Order placed" : $"{todayKotCount} Orders placed";
            g.DrawString($"• Today's Orders / KOTs: {orderPlural}", fRegular, Brushes.LightGray, 28, boxY + 134);
        }
    }
}
