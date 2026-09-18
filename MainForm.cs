using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        private Panel sidebarPanel;
        private Panel sidebarTopPanel;
        private FlowLayoutPanel sidebarMenuPanel;
        private Panel sidebarBottomPanel;
        private Panel headerPanel;
        private Panel footerPanel;
        private Panel mainContentPanel;

        private FlowLayoutPanel headerCountsFlow;
        private Label lblDiningCount;
        private Label lblTakeawayCount;
        private Label lblDeliveryCount;
        private Label lblWaitingCount;

        private Label lblClockFooter;
        private Label lblInvoiceFooter;
        private System.Windows.Forms.Timer clockTimer;

        // Sidebar Navigation Buttons
        private Button btnDashboard;
        private Button btnTables;
        private Button btnPOS;
        private Button btnKitchen;
        private Button btnMasterMenu;
        private Button btnDailySettlement;
        private Button btnReports;
        private Button btnDatabase;
        private Button btnSettings;
        private Button btnLogout;

        // Master Submenu Accordion Controls
        private Panel masterSubmenuPanel;
        private System.Windows.Forms.Timer masterSubmenuTimer;
        private System.Diagnostics.Stopwatch masterSubmenuStopwatch = new System.Diagnostics.Stopwatch();
        private bool isMasterSubmenuExpanded = false;
        private int animStartHeight = 0;
        private int animTargetHeight = 0;
        private const int MasterSubmenuMaxHeight = 255;
        private const int AnimationDurationMs = 200;

        private Button btnSubProducts;
        private Button btnSubCategories;
        private Button btnSubCustomers;
        private Button btnSubStaff;
        private Button btnSubSuppliers;
        private Button btnSubHsnSac;
        private Button btnSubUsers;
        private Button btnSubProfile;

        // Header & Brand Controls
        private PictureBox picLogoIcon;
        private Label lblLogoIcon;
        private Label lblLogoTitle;
        private Label lblLogoSub;
        private Label lblMenuIcon;
        private Button btnHeaderNewAppt;
        private PictureBox picHeaderAvatar;

        // Sidebar Collapsible State
        private bool isSidebarCollapsed = false;
        private const int SidebarExpandedWidth = 220;
        private const int SidebarCollapsedWidth = 62;
        private ToolTip sidebarToolTip = new ToolTip();

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
            RefreshThemeColors();
            
            this.Load += (s, e) => {
                RefreshLiveOrderCounts();
                if (btnTables != null) btnTables.PerformClick();
                else if (btnDashboard != null) btnDashboard.PerformClick();
            };
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(1300, 780);
            this.MinimumSize = new Size(1100, 640);
            this.WindowState = FormWindowState.Maximized;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Secondary;
            this.Text = "The Local Cafe - POS & Management System";
            this.Icon = Theme.AppIcon;

            // ==========================================
            // 1. LEFT SIDEBAR PANEL (Dark Navy Obsidian)
            // ==========================================
            this.DoubleBuffered = true;

            sidebarPanel = new Panel();
            sidebarPanel.Width = 220;
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.BackColor = Theme.SidebarBg;
            SetDoubleBuffered(sidebarPanel);
            this.Controls.Add(sidebarPanel);

            // 1a. TOP PANEL (Cafe Logo & Slogan)
            sidebarTopPanel = new Panel();
            sidebarTopPanel.Height = 75;
            sidebarTopPanel.Dock = DockStyle.Top;
            sidebarTopPanel.BackColor = Color.Transparent;

            // Cafe Icon / Brand Badge
            picLogoIcon = new PictureBox();
            picLogoIcon.Size = new Size(38, 38);
            picLogoIcon.Location = new Point(12, 14);
            picLogoIcon.SizeMode = PictureBoxSizeMode.Zoom;
            picLogoIcon.BackColor = Color.Transparent;
            picLogoIcon.Visible = false;
            sidebarTopPanel.Controls.Add(picLogoIcon);

            lblLogoIcon = new Label();
            lblLogoIcon.Text = "☕";
            lblLogoIcon.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblLogoIcon.ForeColor = Theme.Accent; // Warm amber accent
            lblLogoIcon.Location = new Point(12, 14);
            lblLogoIcon.Size = new Size(38, 38);
            lblLogoIcon.TextAlign = ContentAlignment.MiddleCenter;
            sidebarTopPanel.Controls.Add(lblLogoIcon);

            lblLogoTitle = new Label();
            lblLogoTitle.Text = "The Local";
            lblLogoTitle.Location = new Point(54, 12);
            lblLogoTitle.Size = new Size(125, 26);
            lblLogoTitle.AutoSize = false;
            lblLogoTitle.AutoEllipsis = true;
            lblLogoTitle.TextAlign = ContentAlignment.MiddleLeft;
            Theme.StyleLabel(lblLogoTitle, Theme.TextWhite, new Font("Segoe UI", 12.5F, FontStyle.Bold));
            sidebarTopPanel.Controls.Add(lblLogoTitle);

            lblLogoSub = new Label();
            lblLogoSub.Text = "CAFE & RESTRO";
            lblLogoSub.Location = new Point(54, 38);
            lblLogoSub.Size = new Size(125, 18);
            lblLogoSub.AutoSize = false;
            lblLogoSub.AutoEllipsis = true;
            lblLogoSub.TextAlign = ContentAlignment.MiddleLeft;
            Theme.StyleLabel(lblLogoSub, Theme.Accent, new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold));
            sidebarTopPanel.Controls.Add(lblLogoSub);

            // Hamburger menu icon on right of top sidebar
            lblMenuIcon = new Label();
            lblMenuIcon.Text = "☰";
            lblMenuIcon.Font = new Font("Segoe UI", 13F, FontStyle.Regular);
            lblMenuIcon.ForeColor = Theme.TextSidebar;
            lblMenuIcon.Location = new Point(180, 20);
            lblMenuIcon.Size = new Size(25, 25);
            lblMenuIcon.Cursor = Cursors.Hand;
            lblMenuIcon.Click += (s, e) => ToggleSidebar();
            lblMenuIcon.MouseEnter += (s, e) => lblMenuIcon.ForeColor = Theme.TextWhite;
            lblMenuIcon.MouseLeave += (s, e) => lblMenuIcon.ForeColor = Theme.TextSidebar;
            sidebarToolTip.SetToolTip(lblMenuIcon, "Collapse Sidebar");
            sidebarTopPanel.Controls.Add(lblMenuIcon);

            // 1b. BOTTOM PANEL (Logout Button)
            sidebarBottomPanel = new Panel();
            sidebarBottomPanel.Height = 54;
            sidebarBottomPanel.Dock = DockStyle.Bottom;
            sidebarBottomPanel.BackColor = Color.Transparent;
            sidebarBottomPanel.Padding = new Padding(12, 6, 12, 10);

            // Logout Button
            btnLogout = new Button();
            btnLogout.Text = "  🚪  Logout";
            btnLogout.Size = new Size(196, 38);
            btnLogout.Location = new Point(12, 8);
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.BackColor = Color.Transparent;
            btnLogout.ForeColor = Color.FromArgb(248, 113, 113); // Soft red
            btnLogout.Font = Theme.BoldFont;
            btnLogout.TextAlign = ContentAlignment.MiddleLeft;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.Click += BtnLogout_Click;
            sidebarBottomPanel.Controls.Add(btnLogout);

            // 1c. MIDDLE MENU PANEL (Scrollable Navigation Buttons)
            sidebarMenuPanel = new FlowLayoutPanel();
            sidebarMenuPanel.Dock = DockStyle.Fill;
            sidebarMenuPanel.FlowDirection = FlowDirection.TopDown;
            sidebarMenuPanel.WrapContents = false;
            sidebarMenuPanel.AutoScroll = true;
            sidebarMenuPanel.BackColor = Color.Transparent;
            sidebarMenuPanel.Padding = new Padding(0, 10, 0, 10);
            SetDoubleBuffered(sidebarMenuPanel);

            int btnHeight = 40;

            // 1. Tables & Floor POS
            btnTables = CreateSidebarNavButton("🍽️  Floor & Tables", btnHeight);
            btnTables.Click += (s, e) => {
                var floorCtrl = new TableFloorControl();
                floorCtrl.OnTableSelected += (tblNum, orderType) => {
                    var posCtrl = new SalesBillingControl();
                    posCtrl.OnNavigateToFloor += () => btnTables.PerformClick();
                    posCtrl.LoadTableOrder(tblNum, orderType);
                    ShowView(posCtrl, btnPOS, $"POS Billing - Table {tblNum}");
                };
                ShowView(floorCtrl, btnTables, "Floor & Table Management");
            };
            sidebarMenuPanel.Controls.Add(btnTables);

            // 2. Quick POS Billing
            btnPOS = CreateSidebarNavButton("🛒  POS Billing", btnHeight);
            btnPOS.Click += (s, e) => {
                var billingCtrl = new SalesBillingControl();
                billingCtrl.OnNavigateToFloor += () => btnTables.PerformClick();
                ShowView(billingCtrl, btnPOS, "POS Billing Terminal");
            };
            sidebarMenuPanel.Controls.Add(btnPOS);

            // 3. Kitchen Orders / KDS
            btnKitchen = CreateSidebarNavButton("👨‍🍳  Kitchen Orders", btnHeight);
            btnKitchen.Click += (s, e) => ShowView(new KotManagerControl(), btnKitchen, "Live Kitchen Display & KOT Log");
            sidebarMenuPanel.Controls.Add(btnKitchen);

            // 4. Dashboard
            btnDashboard = CreateSidebarNavButton("📈  Dashboard", btnHeight);
            btnDashboard.Click += (s, e) => ShowView(new DashboardControl(), btnDashboard, "Dashboard & Analytics");
            sidebarMenuPanel.Controls.Add(btnDashboard);

            // ==========================================
            // 6. MASTER ENTRY & SMOOTH SUBMENU ACCORDION
            // ==========================================
            btnMasterMenu = CreateSidebarNavButton("🏛️  Master Entry         ▸", btnHeight);
            btnMasterMenu.Click += (s, e) => {
                ToggleMasterSubmenu();
                ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Overview), btnMasterMenu, "Master Records & Catalog Hub");
            };
            sidebarMenuPanel.Controls.Add(btnMasterMenu);

            // Master Submenu Container Panel
            masterSubmenuPanel = new Panel();
            masterSubmenuPanel.Width = 205;
            masterSubmenuPanel.Height = 0;
            masterSubmenuPanel.Visible = false;
            masterSubmenuPanel.BackColor = Color.FromArgb(12, 17, 28);
            masterSubmenuPanel.Margin = new Padding(8, 0, 8, 4);
            masterSubmenuPanel.Padding = new Padding(0);
            SetDoubleBuffered(masterSubmenuPanel);
            masterSubmenuPanel.Paint += (s, e) => {
                if (masterSubmenuPanel.Height > 8)
                {
                    using (Pen p = new Pen(Theme.Accent, 2)) // Amber accent vertical guide
                    {
                        e.Graphics.DrawLine(p, 10, 4, 10, masterSubmenuPanel.Height - 4);
                    }
                }
            };

            int subHeight = 29;
            btnSubProducts = CreateSubNavButton("🍲  Dishes & Menu", subHeight, 3);
            btnSubProducts.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Products), btnSubProducts, "Dishes & Menu Items Catalog");
            masterSubmenuPanel.Controls.Add(btnSubProducts);

            btnSubCategories = CreateSubNavButton("🏷️  Food Categories", subHeight, 34);
            btnSubCategories.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Categories), btnSubCategories, "Food Categories Master");
            masterSubmenuPanel.Controls.Add(btnSubCategories);

            btnSubCustomers = CreateSubNavButton("👥  Guest Master", subHeight, 65);
            btnSubCustomers.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Customers), btnSubCustomers, "Guests & Customers Directory");
            masterSubmenuPanel.Controls.Add(btnSubCustomers);

            btnSubStaff = CreateSubNavButton("👨‍🍳  Stewards & Staff", subHeight, 96);
            btnSubStaff.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Staff), btnSubStaff, "Stewards & Kitchen Staff Directory");
            masterSubmenuPanel.Controls.Add(btnSubStaff);

            btnSubSuppliers = CreateSubNavButton("🚚  Supplier Master", subHeight, 127);
            btnSubSuppliers.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Suppliers), btnSubSuppliers, "Suppliers & Raw Material Vendors");
            masterSubmenuPanel.Controls.Add(btnSubSuppliers);

            btnSubHsnSac = CreateSubNavButton("📑  HSN / SAC Master", subHeight, 158);
            btnSubHsnSac.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.HsnSac), btnSubHsnSac, "HSN & SAC Tax Master Hub");
            masterSubmenuPanel.Controls.Add(btnSubHsnSac);

            btnSubUsers = CreateSubNavButton("👤  User Master", subHeight, 189);
            btnSubUsers.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Users), btnSubUsers, "User Accounts & Permissions");
            masterSubmenuPanel.Controls.Add(btnSubUsers);

            btnSubProfile = CreateSubNavButton("⚙️  Cafe Profile", subHeight, 220);
            btnSubProfile.Click += (s, e) => ShowView(new MasterMenuControl(MasterMenuControl.MasterTab.Profile), btnSubProfile, "Cafe Profile & Settings");
            masterSubmenuPanel.Controls.Add(btnSubProfile);

            sidebarMenuPanel.Controls.Add(masterSubmenuPanel);

            // 8. Daily Settlement (Day Close)
            btnDailySettlement = CreateSidebarNavButton("📋  Day Close Settlement", btnHeight);
            btnDailySettlement.Click += (s, e) => ShowView(new DailySettlementControl(), btnDailySettlement, "Daily Cash Register & Day Close");
            sidebarMenuPanel.Controls.Add(btnDailySettlement);

            // 9. Reports
            btnReports = CreateSidebarNavButton("📊  Reports & GST", btnHeight);
            btnReports.Click += (s, e) => ShowView(new ReportControl(), btnReports, "Cafe Analytics, GST & Sales Reports");
            sidebarMenuPanel.Controls.Add(btnReports);

            // 10. Database Management
            btnDatabase = CreateSidebarNavButton("🗄️  Database & Backup", btnHeight);
            btnDatabase.Click += (s, e) => ShowView(new BackupRestoreControl(), btnDatabase, "Database Management & Backup / Restore");
            sidebarMenuPanel.Controls.Add(btnDatabase);

            // 11. Settings
            btnSettings = CreateSidebarNavButton("⚙️  Settings", btnHeight);
            btnSettings.Click += (s, e) => {
                var control = new ProfileSettingsControl();
                control.OnSettingsSaved += RefreshThemeColors;
                ShowView(control, btnSettings, "Cafe Profile & Settings");
            };
            sidebarMenuPanel.Controls.Add(btnSettings);

            // Add sidebar panels in precise docking order
            sidebarPanel.Controls.Add(sidebarMenuPanel);
            sidebarPanel.Controls.Add(sidebarTopPanel);
            sidebarPanel.Controls.Add(sidebarBottomPanel);

            sidebarTopPanel.SendToBack();
            sidebarBottomPanel.SendToBack();
            sidebarMenuPanel.BringToFront();

            // ==========================================
            // 2. TOP HEADER BAR (Modern Obsidian Glass)
            // ==========================================
            headerPanel = new Panel();
            headerPanel.Height = 65;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.BackColor = Theme.Primary;
            headerPanel.Padding = new Padding(20, 10, 20, 10);
            headerPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
                }
            };
            this.Controls.Add(headerPanel);

            // Right-aligned Action Group in Header (Pinned to Right, Auto-adjusting Flow)
            FlowLayoutPanel headerRightPanel = new FlowLayoutPanel();
            headerRightPanel.Dock = DockStyle.Right;
            headerRightPanel.AutoSize = true;
            headerRightPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            headerRightPanel.FlowDirection = FlowDirection.RightToLeft;
            headerRightPanel.WrapContents = false;
            headerRightPanel.BackColor = Color.Transparent;
            headerPanel.Controls.Add(headerRightPanel);

            // Profile Avatar & Session Info at top right
            Panel userChip = new Panel();
            userChip.Size = new Size(200, 42);
            userChip.Margin = new Padding(8, 2, 0, 0);
            userChip.BackColor = Color.Transparent;

            picHeaderAvatar = new PictureBox();
            picHeaderAvatar.Size = new Size(38, 38);
            picHeaderAvatar.Location = new Point(0, 2);
            picHeaderAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            picHeaderAvatar.BackColor = Theme.CardBg;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, 38, 38);
                picHeaderAvatar.Region = new Region(gp);
            }
            userChip.Controls.Add(picHeaderAvatar);

            Label lblUserName = new Label();
            lblUserName.Text = Session.FullName ?? "Admin";
            lblUserName.Location = new Point(44, 4);
            lblUserName.AutoSize = true;
            lblUserName.BackColor = Color.Transparent;
            Theme.StyleLabel(lblUserName, Theme.TextWhite, new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold));
            userChip.Controls.Add(lblUserName);

            Label lblUserRole = new Label();
            lblUserRole.Text = $"{Session.Role ?? "Super Admin"} ▼";
            lblUserRole.Location = new Point(44, 20);
            lblUserRole.AutoSize = true;
            lblUserRole.BackColor = Color.Transparent;
            Theme.StyleLabel(lblUserRole, Theme.TextMuted, new Font("Segoe UI", 7.5F, FontStyle.Regular));
            userChip.Controls.Add(lblUserRole);

            headerRightPanel.Controls.Add(userChip);

            // Notification Bell with Badge
            Panel notifPanel = new Panel();
            notifPanel.Size = new Size(40, 40);
            notifPanel.Margin = new Padding(8, 2, 0, 0);
            notifPanel.BackColor = Theme.CardBg;
            notifPanel.Cursor = Cursors.Hand;
            notifPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, notifPanel.Width - 1, notifPanel.Height - 1);
                }
            };

            Label lblBell = new Label();
            lblBell.Text = "🔔";
            lblBell.Location = new Point(8, 10);
            lblBell.Size = new Size(24, 20);
            lblBell.BackColor = Color.Transparent;
            notifPanel.Controls.Add(lblBell);

            Label lblBadge = new Label();
            lblBadge.Text = "3";
            lblBadge.Size = new Size(16, 16);
            lblBadge.Location = new Point(22, 4);
            lblBadge.BackColor = Color.FromArgb(244, 63, 94); // Rose red
            lblBadge.ForeColor = Color.White;
            lblBadge.Font = new Font("Segoe UI", 6.5F, FontStyle.Bold);
            lblBadge.TextAlign = ContentAlignment.MiddleCenter;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, 16, 16);
                lblBadge.Region = new Region(gp);
            }
            notifPanel.Controls.Add(lblBadge);
            headerRightPanel.Controls.Add(notifPanel);

            // New Order / Table Header Button
            btnHeaderNewAppt = new Button();
            btnHeaderNewAppt.Text = "+ New Order / Table";
            btnHeaderNewAppt.AutoSize = true;
            btnHeaderNewAppt.Height = 40;
            btnHeaderNewAppt.Padding = new Padding(14, 0, 14, 0);
            btnHeaderNewAppt.Margin = new Padding(8, 2, 0, 0);
            Theme.StyleButton(btnHeaderNewAppt, Theme.Success, Theme.TextWhite);
            btnHeaderNewAppt.Click += (s, e) => {
                if (btnTables != null) btnTables.PerformClick();
            };
            headerRightPanel.Controls.Add(btnHeaderNewAppt);

            // Left-aligned Live Order Status Counts (Dining, Take Away, Delivery, Waiting)
            headerCountsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 3, 0, 0)
            };

            var (chipDining, lblD) = CreateHeaderCountChip("🍽️", "Dining", Color.FromArgb(59, 130, 246), () => NavigateToFloorMode("DINING"));
            lblDiningCount = lblD;
            headerCountsFlow.Controls.Add(chipDining);

            var (chipTakeaway, lblT) = CreateHeaderCountChip("🛍️", "Take Away", Color.FromArgb(16, 185, 129), () => NavigateToFloorMode("TAKEAWAY"));
            lblTakeawayCount = lblT;
            headerCountsFlow.Controls.Add(chipTakeaway);

            var (chipDelivery, lblDel) = CreateHeaderCountChip("🛵", "Delivery", Color.FromArgb(249, 115, 22), () => NavigateToFloorMode("DELIVERY"));
            lblDeliveryCount = lblDel;
            headerCountsFlow.Controls.Add(chipDelivery);

            var (chipWaiting, lblW) = CreateHeaderCountChip("⏳", "Waiting", Color.FromArgb(234, 179, 8), () => NavigateToFloorMode("WAITING"));
            lblWaitingCount = lblW;
            headerCountsFlow.Controls.Add(chipWaiting);

            headerPanel.Controls.Add(headerCountsFlow);

            // ==========================================
            // 3. BOTTOM STATUS FOOTER BAR
            // ==========================================
            footerPanel = new Panel();
            footerPanel.Height = 32;
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.BackColor = Theme.Primary;
            footerPanel.Padding = new Padding(20, 6, 20, 6);
            footerPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(p, 0, 0, footerPanel.Width, 0);
                }
            };
            this.Controls.Add(footerPanel);

            lblInvoiceFooter = new Label();
            lblInvoiceFooter.Text = $"🧾 Invoice No : INV-{DateTime.Now:yyMMdd}-0001";
            lblInvoiceFooter.Location = new Point(20, 7);
            lblInvoiceFooter.AutoSize = true;
            Theme.StyleLabel(lblInvoiceFooter, Theme.TextMuted, new Font("Segoe UI", 8F));
            footerPanel.Controls.Add(lblInvoiceFooter);

            Label lblDateFooter = new Label();
            lblDateFooter.Text = $"📅 {DateTime.Now:dd MMM yyyy}";
            lblDateFooter.Location = new Point(250, 7);
            lblDateFooter.AutoSize = true;
            Theme.StyleLabel(lblDateFooter, Theme.TextMuted, new Font("Segoe UI", 8F));
            footerPanel.Controls.Add(lblDateFooter);

            lblClockFooter = new Label();
            lblClockFooter.Text = $"⏰ {DateTime.Now:hh:mm tt}";
            lblClockFooter.Location = new Point(420, 7);
            lblClockFooter.AutoSize = true;
            Theme.StyleLabel(lblClockFooter, Theme.TextMuted, new Font("Segoe UI", 8F));
            footerPanel.Controls.Add(lblClockFooter);

            Label lblPrinterFooter = new Label();
            lblPrinterFooter.Text = "🖨️ Printer : Thermal ESC/POS (Ready)";
            lblPrinterFooter.Location = new Point(600, 7);
            lblPrinterFooter.AutoSize = true;
            Theme.StyleLabel(lblPrinterFooter, Theme.TextMuted, new Font("Segoe UI", 8F));
            footerPanel.Controls.Add(lblPrinterFooter);

            Label lblBranchFooter = new Label();
            lblBranchFooter.Text = "📍 Profile : Main Branch";
            lblBranchFooter.Location = new Point(880, 7);
            lblBranchFooter.AutoSize = true;
            Theme.StyleLabel(lblBranchFooter, Theme.TextMuted, new Font("Segoe UI", 8F));
            footerPanel.Controls.Add(lblBranchFooter);

            // Timer for footer clock & live order counts
            clockTimer = new System.Windows.Forms.Timer();
            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) => {
                lblClockFooter.Text = $"⏰ {DateTime.Now:hh:mm tt}";
                if (DateTime.Now.Second % 2 == 0)
                {
                    RefreshLiveOrderCounts();
                }
                if (DateTime.Now.Second == 0)
                {
                    UpdateInvoiceFooter();
                }
            };
            clockTimer.Start();

            // Initial footer invoice calculation
            UpdateInvoiceFooter();

            // ==========================================
            // 4. MAIN CONTENT CONTAINER PANEL
            // ==========================================
            mainContentPanel = new Panel();
            mainContentPanel.Dock = DockStyle.Fill;
            mainContentPanel.AutoScroll = true;
            mainContentPanel.BackColor = Theme.Secondary;
            mainContentPanel.Padding = new Padding(12);
            this.Controls.Add(mainContentPanel);

            // Docking order
            headerPanel.SendToBack();
            sidebarPanel.SendToBack();
            footerPanel.SendToBack();
            mainContentPanel.BringToFront();
        }

        public void UpdateInvoiceFooter()
        {
            try
            {
                if (lblInvoiceFooter != null && !lblInvoiceFooter.IsDisposed)
                {
                    string nextInv = SalesBillingControl.GetNextInvoiceNumberPreview();
                    lblInvoiceFooter.Text = $"🧾 Next Bill: [{nextInv}]";
                }
            }
            catch { }
        }

        private Button CreateSidebarNavButton(string text, int height)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(196, height);
            btn.Margin = new Padding(12, 2, 12, 2);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Theme.TextSidebar;
            btn.Font = Theme.BoldFont;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private Button CreateSubNavButton(string text, int height, int top)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(182, height);
            btn.Location = new Point(20, top);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Theme.TextSidebar;
            btn.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private static void SetDoubleBuffered(Control c)
        {
            if (SystemInformation.TerminalServerSession) return;
            System.Reflection.PropertyInfo p = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            p?.SetValue(c, true, null);
        }

        private void ToggleMasterSubmenu()
        {
            isMasterSubmenuExpanded = !isMasterSubmenuExpanded;
            btnMasterMenu.Text = isMasterSubmenuExpanded ? "🏛️  Master Entry         ▾" : "🏛️  Master Entry         ▸";

            animStartHeight = masterSubmenuPanel.Height;
            animTargetHeight = isMasterSubmenuExpanded ? MasterSubmenuMaxHeight : 0;

            if (isMasterSubmenuExpanded && !masterSubmenuPanel.Visible)
            {
                masterSubmenuPanel.Visible = true;
            }

            if (masterSubmenuTimer == null)
            {
                masterSubmenuTimer = new System.Windows.Forms.Timer();
                masterSubmenuTimer.Interval = 10;
                masterSubmenuTimer.Tick += MasterSubmenuTimer_Tick;
            }

            masterSubmenuStopwatch.Restart();
            masterSubmenuTimer.Start();
        }

        private void MasterSubmenuTimer_Tick(object sender, EventArgs e)
        {
            float elapsed = (float)masterSubmenuStopwatch.ElapsedMilliseconds;
            float progress = Math.Min(1.0f, elapsed / (float)AnimationDurationMs);

            // Smooth cubic ease-out: f(t) = 1 - (1 - t)^3
            float ease = 1f - (float)Math.Pow(1f - progress, 3);
            int currentHeight = (int)(animStartHeight + (animTargetHeight - animStartHeight) * ease);

            masterSubmenuPanel.Height = Math.Max(0, Math.Min(MasterSubmenuMaxHeight, currentHeight));

            if (progress >= 1.0f)
            {
                masterSubmenuPanel.Height = animTargetHeight;
                if (!isMasterSubmenuExpanded)
                {
                    masterSubmenuPanel.Visible = false;
                }
                masterSubmenuStopwatch.Stop();
                masterSubmenuTimer.Stop();
            }
        }

        private void ShowView(UserControl view, Button activeBtn, string headerTitle)
        {
            Button[] navButtons = { 
                btnTables, btnPOS, btnKitchen, btnDashboard, btnMasterMenu, 
                btnSubProducts, btnSubCategories, btnSubCustomers, 
                btnSubStaff, btnSubSuppliers, btnSubHsnSac, btnSubUsers, btnSubProfile,
                btnDailySettlement, btnReports, btnDatabase, btnSettings 
            };

            foreach (var b in navButtons)
            {
                if (b != null)
                {
                    b.BackColor = Color.Transparent;
                    b.ForeColor = Theme.TextSidebar;
                    b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
                }
            }

            if (activeBtn != null)
            {
                activeBtn.BackColor = Theme.Accent; // Rich amber pill
                activeBtn.ForeColor = Theme.TextWhite;
                activeBtn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }

            // Swap out current Control inside Main Panel
            mainContentPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            mainContentPanel.Controls.Add(view);
            view.BringToFront();
            RefreshLiveOrderCounts();
        }

        private void ToggleSidebar()
        {
            isSidebarCollapsed = !isSidebarCollapsed;

            if (isSidebarCollapsed)
            {
                // Collapse master submenu if open
                if (isMasterSubmenuExpanded)
                {
                    isMasterSubmenuExpanded = false;
                    masterSubmenuPanel.Height = 0;
                    masterSubmenuPanel.Visible = false;
                }

                sidebarPanel.Width = SidebarCollapsedWidth;
                lblLogoTitle.Visible = false;
                lblLogoSub.Visible = false;
                lblMenuIcon.Location = new Point(18, 48);
                lblLogoIcon.Location = new Point(12, 8);
                picLogoIcon.Location = new Point(12, 8);

                SetButtonCollapsedMode(btnTables, "🍽️", "Floor & Tables");
                SetButtonCollapsedMode(btnPOS, "🛒", "POS Billing");
                SetButtonCollapsedMode(btnKitchen, "👨‍🍳", "Kitchen Orders");
                SetButtonCollapsedMode(btnDashboard, "📈", "Dashboard");
                SetButtonCollapsedMode(btnMasterMenu, "🏛️", "Master Records");
                SetButtonCollapsedMode(btnDailySettlement, "📋", "Day Close Settlement");
                SetButtonCollapsedMode(btnReports, "📊", "Reports & GST");
                SetButtonCollapsedMode(btnDatabase, "🗄️", "Database Management");
                SetButtonCollapsedMode(btnSettings, "⚙️", "Settings");

                btnLogout.Text = "🚪";
                btnLogout.Size = new Size(46, 38);
                btnLogout.Location = new Point(8, 8);
                btnLogout.TextAlign = ContentAlignment.MiddleCenter;
                sidebarToolTip?.SetToolTip(btnLogout, "Logout");
                sidebarToolTip?.SetToolTip(lblMenuIcon, "Expand Sidebar");
            }
            else
            {
                sidebarPanel.Width = SidebarExpandedWidth;
                lblLogoTitle.Visible = true;
                lblLogoSub.Visible = true;
                lblMenuIcon.Location = new Point(180, 20);
                lblLogoIcon.Location = new Point(12, 14);
                picLogoIcon.Location = new Point(12, 14);

                SetButtonExpandedMode(btnTables, "🍽️  Floor & Tables");
                SetButtonExpandedMode(btnPOS, "🛒  POS Billing");
                SetButtonExpandedMode(btnKitchen, "👨‍🍳  Kitchen Orders");
                SetButtonExpandedMode(btnDashboard, "📈  Dashboard");
                SetButtonExpandedMode(btnMasterMenu, "🏛️  Master Entry         ▸");
                SetButtonExpandedMode(btnDailySettlement, "📋  Day Close Settlement");
                SetButtonExpandedMode(btnReports, "📊  Reports & GST");
                SetButtonExpandedMode(btnDatabase, "🗄️  Database & Backup");
                SetButtonExpandedMode(btnSettings, "⚙️  Settings");

                btnLogout.Text = "  🚪  Logout";
                btnLogout.Size = new Size(196, 38);
                btnLogout.Location = new Point(12, 8);
                btnLogout.TextAlign = ContentAlignment.MiddleLeft;
                sidebarToolTip?.SetToolTip(btnLogout, null);
                sidebarToolTip?.SetToolTip(lblMenuIcon, "Collapse Sidebar");
            }
        }

        private void SetButtonCollapsedMode(Button btn, string iconText, string tooltip)
        {
            if (btn == null) return;
            btn.Text = iconText;
            btn.Size = new Size(46, 40);
            btn.Margin = new Padding(8, 2, 8, 2);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            sidebarToolTip?.SetToolTip(btn, tooltip);
        }

        private void SetButtonExpandedMode(Button btn, string fullText)
        {
            if (btn == null) return;
            btn.Text = fullText;
            btn.Size = new Size(196, 40);
            btn.Margin = new Padding(12, 2, 12, 2);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            sidebarToolTip?.SetToolTip(btn, null);
        }



        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult logout = MessageBox.Show("Are you sure you want to log out?", "Confirm Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (logout == DialogResult.Yes)
            {
                Session.Clear();
                this.DialogResult = DialogResult.Retry;
                this.Close();
            }
        }

        public void RefreshThemeColors()
        {
            Theme.UpdateFontRecursively(this);
            this.BackColor = Theme.Secondary;
            sidebarPanel.BackColor = Theme.SidebarBg;
            headerPanel.BackColor = Theme.CardBg;
            footerPanel.BackColor = Theme.CardBg;
            mainContentPanel.BackColor = Theme.Secondary;

            RefreshShopBrand();
        }

        public void RefreshShopBrand()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, LogoPath, ProfilePicPath FROM AppProfile", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                string fullShopName = rdr["ShopName"]?.ToString()?.Trim();
                                string logoPath = rdr["LogoPath"] != DBNull.Value ? rdr["LogoPath"]?.ToString() : null;
                                string profPicPath = rdr["ProfilePicPath"] != DBNull.Value ? rdr["ProfilePicPath"]?.ToString() : null;

                                if (!string.IsNullOrWhiteSpace(fullShopName))
                                {
                                    this.Text = $"{fullShopName} - Restaurant & Cafe POS Billing System";

                                    if (lblLogoTitle != null && lblLogoSub != null)
                                    {
                                        string[] words = fullShopName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (words.Length == 1)
                                        {
                                            lblLogoTitle.Text = words[0];
                                            lblLogoSub.Text = "CAFE & RESTAURANT";
                                        }
                                        else if (words.Length == 2)
                                        {
                                            lblLogoTitle.Text = words[0];
                                            lblLogoSub.Text = words[1].ToUpper();
                                        }
                                        else
                                        {
                                            lblLogoTitle.Text = words[0];
                                            lblLogoSub.Text = string.Join(" ", words.Skip(1)).ToUpper();
                                        }

                                        // Dynamic font sizing based on first word length
                                        if (lblLogoTitle.Text.Length > 10)
                                        {
                                            lblLogoTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                                        }
                                        else if (lblLogoTitle.Text.Length > 7)
                                        {
                                            lblLogoTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
                                        }
                                        else
                                        {
                                            lblLogoTitle.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
                                        }
                                    }
                                }

                                // Load and display the official Shop Logo in the sidebar
                                if (picLogoIcon != null && lblLogoIcon != null)
                                {
                                    string resolvedLogo = logoPath;
                                    if (!string.IsNullOrEmpty(resolvedLogo) && !File.Exists(resolvedLogo))
                                    {
                                        string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resolvedLogo);
                                        if (File.Exists(candidate)) resolvedLogo = candidate;
                                    }
                                    if (string.IsNullOrEmpty(resolvedLogo) || !File.Exists(resolvedLogo))
                                    {
                                        string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                                        string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                                        string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                                        if (File.Exists(p1)) resolvedLogo = p1;
                                        else if (File.Exists(p2)) resolvedLogo = p2;
                                        else if (File.Exists(p3)) resolvedLogo = p3;
                                    }

                                    if (!string.IsNullOrEmpty(resolvedLogo) && File.Exists(resolvedLogo))
                                    {
                                        try
                                        {
                                            byte[] bytes = File.ReadAllBytes(resolvedLogo);
                                            using (var ms = new MemoryStream(bytes))
                                            {
                                                var oldImg = picLogoIcon.Image;
                                                picLogoIcon.Image = Image.FromStream(ms);
                                                oldImg?.Dispose();
                                            }
                                            picLogoIcon.Visible = true;
                                            lblLogoIcon.Visible = false;
                                        }
                                        catch
                                        {
                                            picLogoIcon.Visible = false;
                                            lblLogoIcon.Visible = true;
                                        }
                                    }
                                    else
                                    {
                                        picLogoIcon.Visible = false;
                                        lblLogoIcon.Visible = true;
                                    }
                                }

                                // Load Admin Profile Avatar in Top Header
                                if (picHeaderAvatar != null)
                                {
                                    if (!string.IsNullOrEmpty(profPicPath) && File.Exists(profPicPath))
                                    {
                                        try
                                        {
                                            byte[] bytes = File.ReadAllBytes(profPicPath);
                                            using (var ms = new MemoryStream(bytes))
                                            {
                                                var oldImg = picHeaderAvatar.Image;
                                                picHeaderAvatar.Image = Image.FromStream(ms);
                                                oldImg?.Dispose();
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private (Panel chip, Label lblCount) CreateHeaderCountChip(string icon, string title, Color accentColor, Action onClick)
        {
            FlowLayoutPanel chip = new FlowLayoutPanel
            {
                Height = 40,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(20, 29, 47),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 10, 0),
                Padding = new Padding(12, 8, 10, 8)
            };

            Label lblTitle = new Label
            {
                Text = $"{icon}  {title}",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = true,
                Margin = new Padding(0, 3, 8, 0),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            Label lblCount = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                AutoSize = false,
                Size = new Size(26, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 1, 0, 0),
                Cursor = Cursors.Hand
            };

            chip.Controls.Add(lblTitle);
            chip.Controls.Add(lblCount);

            chip.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(45, 59, 82), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, chip.Width - 1, chip.Height - 1);
                }
            };

            void AttachEvents(Control c)
            {
                c.Click += (s, e) => onClick?.Invoke();
                c.MouseEnter += (s, e) => {
                    chip.BackColor = Color.FromArgb(32, 45, 72);
                    lblTitle.ForeColor = Color.White;
                };
                c.MouseLeave += (s, e) => {
                    chip.BackColor = Color.FromArgb(20, 29, 47);
                    lblTitle.ForeColor = Color.FromArgb(203, 213, 225);
                };
            }

            AttachEvents(chip);
            AttachEvents(lblTitle);
            AttachEvents(lblCount);

            return (chip, lblCount);
        }

        private void UpdateCountBadge(Label lbl, int count, Color activeColor)
        {
            if (lbl == null) return;
            lbl.Text = count.ToString();
            if (count > 0)
            {
                lbl.BackColor = activeColor;
                lbl.ForeColor = Color.White;
            }
            else
            {
                lbl.BackColor = Color.FromArgb(30, 41, 59);
                lbl.ForeColor = Color.FromArgb(148, 163, 184);
            }
        }

        public void NavigateToFloorMode(string mode)
        {
            if (mainContentPanel.Controls.Count > 0 && mainContentPanel.Controls[0] is TableFloorControl floor)
            {
                floor.SwitchMode(mode);
            }
            else
            {
                var floorCtrl = new TableFloorControl(mode);
                floorCtrl.OnTableSelected += (tblNum, orderType) => {
                    var posCtrl = new SalesBillingControl();
                    posCtrl.OnNavigateToFloor += () => btnTables.PerformClick();
                    posCtrl.LoadTableOrder(tblNum, orderType);
                    ShowView(posCtrl, btnPOS, $"POS Billing - Table {tblNum}");
                };
                ShowView(floorCtrl, btnTables, "Floor & Table Management");
            }
        }

        public void RefreshLiveOrderCounts()
        {
            try
            {
                int diningCount = 0;
                int takeawayCount = 0;
                int deliveryCount = 0;
                int waitingCount = 0;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            COUNT(DISTINCT CASE WHEN (k.OrderType = 'DINING' OR k.OrderType IS NULL OR k.OrderType = '') AND k.TableNumber NOT LIKE 'Waiting%' THEN k.TableNumber END) AS DiningCount,
                            COUNT(DISTINCT CASE WHEN k.OrderType = 'TAKEAWAY' THEN k.TableNumber END) AS TakeawayCount,
                            COUNT(DISTINCT CASE WHEN k.OrderType = 'DELIVERY' THEN k.TableNumber END) AS DeliveryCount,
                            COUNT(DISTINCT CASE WHEN k.TableNumber LIKE 'Waiting%' THEN k.TableNumber END) AS WaitingCount
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            diningCount = r["DiningCount"] != DBNull.Value ? Convert.ToInt32(r["DiningCount"]) : 0;
                            takeawayCount = r["TakeawayCount"] != DBNull.Value ? Convert.ToInt32(r["TakeawayCount"]) : 0;
                            deliveryCount = r["DeliveryCount"] != DBNull.Value ? Convert.ToInt32(r["DeliveryCount"]) : 0;
                            waitingCount = r["WaitingCount"] != DBNull.Value ? Convert.ToInt32(r["WaitingCount"]) : 0;
                        }
                    }
                }

                if (this.InvokeRequired)
                {
                    this.BeginInvoke((Action)(() => {
                        UpdateCountBadge(lblDiningCount, diningCount, Color.FromArgb(59, 130, 246));
                        UpdateCountBadge(lblTakeawayCount, takeawayCount, Color.FromArgb(16, 185, 129));
                        UpdateCountBadge(lblDeliveryCount, deliveryCount, Color.FromArgb(249, 115, 22));
                        UpdateCountBadge(lblWaitingCount, waitingCount, Color.FromArgb(234, 179, 8));
                    }));
                }
                else
                {
                    UpdateCountBadge(lblDiningCount, diningCount, Color.FromArgb(59, 130, 246));
                    UpdateCountBadge(lblTakeawayCount, takeawayCount, Color.FromArgb(16, 185, 129));
                    UpdateCountBadge(lblDeliveryCount, deliveryCount, Color.FromArgb(249, 115, 22));
                    UpdateCountBadge(lblWaitingCount, waitingCount, Color.FromArgb(234, 179, 8));
                }
            }
            catch
            {
                // Ignore transient db polling errors
            }
        }
    }
}
