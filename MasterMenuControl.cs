using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class MasterMenuControl : UserControl
    {
        public enum MasterTab
        {
            Overview,
            Products,
            Categories,
            Customers,
            Staff,
            Suppliers,
            HsnSac,
            Users,
            Profile
        }

        private Panel topNavPanel;
        private FlowLayoutPanel tabButtonContainer;
        private Panel contentHostPanel;

        // Tab Navigation Buttons
        private Button btnTabOverview;
        private Button btnTabProducts;
        private Button btnTabCategories;
        private Button btnTabCustomers;
        private Button btnTabStaff;
        private Button btnTabSuppliers;
        private Button btnTabHsnSac;
        private Button btnTabUsers;
        private Button btnTabProfile;

        // Overview Container
        private Panel overviewPanel;
        private FlowLayoutPanel cardsFlowPanel;
        private System.Collections.Generic.List<Panel> masterCards = new System.Collections.Generic.List<Panel>();

        // Stat Labels on Overview
        private Label lblCountProducts;
        private Label lblCountCategories;
        private Label lblCountCustomers;
        private Label lblCountStaff;
        private Label lblCountSuppliers;
        private Label lblCountHsnSac;
        private Label lblCountUsers;

        private MasterTab currentTab = MasterTab.Overview;

        public MasterMenuControl(MasterTab initialTab = MasterTab.Overview)
        {
            InitializeComponent();
            SelectTab(initialTab);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1100, 720);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // ==========================================
            // 1. TOP HEADER & TAB NAVIGATION BAR
            // ==========================================
            topNavPanel = new Panel();
            topNavPanel.Height = 116;
            topNavPanel.Dock = DockStyle.Top;
            topNavPanel.BackColor = Theme.Primary;
            topNavPanel.Padding = new Padding(20, 10, 20, 8);
            topNavPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(30, 41, 59), 1))
                {
                    e.Graphics.DrawLine(p, 0, topNavPanel.Height - 1, topNavPanel.Width, topNavPanel.Height - 1);
                }
            };
            this.Controls.Add(topNavPanel);

            // Title & Subtitle Header
            Label lblHeader = new Label();
            lblHeader.Text = "🏛️ Master Records & Catalog Hub";
            lblHeader.Location = new Point(20, 10);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            topNavPanel.Controls.Add(lblHeader);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Centralized management of foundational master entries, services, products, categories, staff, and customer databases.";
            lblSubtitle.Location = new Point(22, 38);
            lblSubtitle.AutoSize = true;
            Theme.StyleLabel(lblSubtitle, Theme.TextMuted, Theme.MainFont);
            topNavPanel.Controls.Add(lblSubtitle);

            // Tab Buttons Container (Sleek Responsive Horizontal Pills with Wrapping)
            tabButtonContainer = new FlowLayoutPanel();
            tabButtonContainer.Location = new Point(20, 66);
            tabButtonContainer.FlowDirection = FlowDirection.LeftToRight;
            tabButtonContainer.WrapContents = true;
            tabButtonContainer.AutoScroll = false;
            tabButtonContainer.BackColor = Color.Transparent;
            tabButtonContainer.Padding = new Padding(0, 0, 0, 4);

            btnTabOverview = CreateTabButton("🌟 Overview", MasterTab.Overview);
            btnTabProducts = CreateTabButton("🍽️ Menu & Dishes", MasterTab.Products);
            btnTabCategories = CreateTabButton("🏷️ Categories", MasterTab.Categories);
            btnTabCustomers = CreateTabButton("👥 Guests & Clients", MasterTab.Customers);
            btnTabStaff = CreateTabButton("🧑‍🍳 Stewards & Staff", MasterTab.Staff);
            btnTabSuppliers = CreateTabButton("🚚 Suppliers", MasterTab.Suppliers);
            btnTabHsnSac = CreateTabButton("📑 HSN / SAC (GST)", MasterTab.HsnSac);
            btnTabUsers = CreateTabButton("👤 User Accounts", MasterTab.Users);
            btnTabProfile = CreateTabButton("⚙️ Cafe Profile", MasterTab.Profile);

            tabButtonContainer.Controls.Add(btnTabOverview);
            tabButtonContainer.Controls.Add(btnTabProducts);
            tabButtonContainer.Controls.Add(btnTabCategories);
            tabButtonContainer.Controls.Add(btnTabCustomers);
            tabButtonContainer.Controls.Add(btnTabStaff);
            tabButtonContainer.Controls.Add(btnTabSuppliers);
            tabButtonContainer.Controls.Add(btnTabHsnSac);
            tabButtonContainer.Controls.Add(btnTabUsers);
            tabButtonContainer.Controls.Add(btnTabProfile);

            topNavPanel.Controls.Add(tabButtonContainer);

            // ==========================================
            // 2. MAIN CONTENT HOST PANEL
            // ==========================================
            contentHostPanel = new Panel();
            contentHostPanel.Dock = DockStyle.Fill;
            contentHostPanel.BackColor = Theme.Secondary;
            contentHostPanel.AutoScroll = true;
            this.Controls.Add(contentHostPanel);

            // Send topNav to back so content fills correctly
            topNavPanel.SendToBack();
            contentHostPanel.BringToFront();

            // Resize and dynamic layout listeners
            this.Resize += (s, e) => { UpdateTopNavHeight(); AdjustCardsLayout(); };
            contentHostPanel.Resize += (s, e) => AdjustCardsLayout();
            this.ParentChanged += (s, e) => { UpdateTopNavHeight(); AdjustCardsLayout(); };
            this.VisibleChanged += (s, e) => { if (this.Visible) { UpdateTopNavHeight(); AdjustCardsLayout(); } };

            // Build Overview Panel
            BuildOverviewView();
            UpdateTopNavHeight();
        }

        private void UpdateTopNavHeight()
        {
            if (topNavPanel != null && tabButtonContainer != null)
            {
                tabButtonContainer.Width = Math.Max(200, topNavPanel.ClientSize.Width - 40);
                int neededH = tabButtonContainer.PreferredSize.Height;
                topNavPanel.Height = 66 + neededH + 10;
            }
        }

        private Button CreateTabButton(string text, MasterTab tab)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.AutoSize = true;
            btn.Height = 32;
            btn.Margin = new Padding(0, 0, 6, 6);
            btn.Padding = new Padding(10, 0, 10, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(18, 24, 40);
            btn.ForeColor = Theme.TextMuted;
            btn.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            btn.Click += (s, e) => SelectTab(tab);
            return btn;
        }

        public void SelectTab(MasterTab tab)
        {
            currentTab = tab;

            // Reset tab button styles
            Button[] allTabs = { 
                btnTabOverview, btnTabProducts, btnTabCategories, 
                btnTabCustomers, btnTabStaff, btnTabSuppliers, btnTabHsnSac, btnTabUsers, btnTabProfile 
            };

            foreach (var b in allTabs)
            {
                if (b != null)
                {
                    b.BackColor = Color.FromArgb(18, 24, 40);
                    b.ForeColor = Theme.TextMuted;
                    b.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
                }
            }

            // Set active button
            Button activeBtn = btnTabOverview;
            switch (tab)
            {
                case MasterTab.Overview: activeBtn = btnTabOverview; break;
                case MasterTab.Products: activeBtn = btnTabProducts; break;
                case MasterTab.Categories: activeBtn = btnTabCategories; break;
                case MasterTab.Customers: activeBtn = btnTabCustomers; break;
                case MasterTab.Staff: activeBtn = btnTabStaff; break;
                case MasterTab.Suppliers: activeBtn = btnTabSuppliers; break;
                case MasterTab.HsnSac: activeBtn = btnTabHsnSac; break;
                case MasterTab.Users: activeBtn = btnTabUsers; break;
                case MasterTab.Profile: activeBtn = btnTabProfile; break;
            }

            if (activeBtn != null)
            {
                activeBtn.BackColor = Theme.Accent;
                activeBtn.ForeColor = Theme.TextWhite;
                activeBtn.FlatAppearance.BorderColor = Theme.Accent;
            }

            // Host corresponding view
            contentHostPanel.Controls.Clear();

            switch (tab)
            {
                case MasterTab.Overview:
                    RefreshOverviewStats();
                    overviewPanel.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(overviewPanel);
                    break;

                case MasterTab.Products:
                    var prodCtrl = new ProductControl();
                    prodCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(prodCtrl);
                    break;

                case MasterTab.Categories:
                    var catCtrl = new CategoryControl();
                    catCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(catCtrl);
                    break;

                case MasterTab.Customers:
                    var custCtrl = new CustomerControl();
                    custCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(custCtrl);
                    break;

                case MasterTab.Staff:
                    var staffCtrl = new StaffControl();
                    staffCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(staffCtrl);
                    break;

                case MasterTab.Suppliers:
                    var suppCtrl = new SupplierControl();
                    suppCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(suppCtrl);
                    break;

                case MasterTab.HsnSac:
                    var hsnSacCtrl = new HsnSacControl();
                    hsnSacCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(hsnSacCtrl);
                    break;

                case MasterTab.Users:
                    var userCtrl = new UserManagementControl();
                    userCtrl.Dock = DockStyle.Fill;
                    contentHostPanel.Controls.Add(userCtrl);
                    break;

                case MasterTab.Profile:
                    var profileCtrl = new ProfileSettingsControl();
                    profileCtrl.Dock = DockStyle.Fill;
                    profileCtrl.OnSettingsSaved += () => {
                        if (this.FindForm() is MainForm mf)
                        {
                            mf.RefreshThemeColors();
                        }
                    };
                    contentHostPanel.Controls.Add(profileCtrl);
                    break;
            }
        }

        private void BuildOverviewView()
        {
            overviewPanel = new Panel();
            overviewPanel.AutoScroll = true;
            overviewPanel.BackColor = Theme.Secondary;
            overviewPanel.Padding = new Padding(15, 10, 15, 15);

            // Overview Section Header Panel
            Panel overviewHeader = new Panel();
            overviewHeader.Dock = DockStyle.Top;
            overviewHeader.Height = 70;
            overviewHeader.BackColor = Color.Transparent;
            overviewHeader.Padding = new Padding(10, 5, 10, 5);

            Label lblSection = new Label();
            lblSection.Text = "All Master Catalogs & Directories";
            lblSection.Location = new Point(10, 8);
            lblSection.AutoSize = true;
            Theme.StyleLabel(lblSection, Theme.TextLight, Theme.SubHeaderFont);
            overviewHeader.Controls.Add(lblSection);

            Label lblSectionSub = new Label();
            lblSectionSub.Text = "Click on any master card below to manage records, update pricing, or register new entries.";
            lblSectionSub.Location = new Point(12, 34);
            lblSectionSub.AutoSize = true;
            Theme.StyleLabel(lblSectionSub, Theme.TextMuted, Theme.MainFont);
            overviewHeader.Controls.Add(lblSectionSub);

            overviewPanel.Controls.Add(overviewHeader);

            // Flow Layout for Cards
            cardsFlowPanel = new FlowLayoutPanel();
            cardsFlowPanel.Dock = DockStyle.Fill;
            cardsFlowPanel.AutoScroll = true;
            cardsFlowPanel.FlowDirection = FlowDirection.LeftToRight;
            cardsFlowPanel.WrapContents = true;
            cardsFlowPanel.BackColor = Color.Transparent;
            cardsFlowPanel.Padding = new Padding(6, 4, 6, 12);
            cardsFlowPanel.SizeChanged += (s, e) => AdjustCardsLayout();

            masterCards.Clear();

            // 1. Dishes & Menu Master Card
            lblCountProducts = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "🍽️", "Dishes & Menu Catalog", 
                "Manage food items, beverages, snacks, breakfast platters, selling prices, categories, and barcodes.",
                lblCountProducts, "0 Dishes",
                Theme.Accent,
                () => SelectTab(MasterTab.Products)
            ));

            // 2. Categories Master Card
            lblCountCategories = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "🏷️", "Food Categories", 
                "Organize menu items into quick POS categories (Tibetan, Continental, Beverages, Desserts, Breakfast).",
                lblCountCategories, "0 Categories",
                Theme.Warning,
                () => SelectTab(MasterTab.Categories)
            ));

            // 3. Customers Master Card
            lblCountCustomers = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "👥", "Guests & Customers", 
                "Customer directory, loyalty points, contact info, and dining visit history.",
                lblCountCustomers, "0 Guests",
                Theme.Success,
                () => SelectTab(MasterTab.Customers)
            ));

            // 4. Stewards & Staff Master Card
            lblCountStaff = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "🧑‍🍳", "Stewards & Staff", 
                "Waitstaff, table stewards, chefs, kitchen operators, contact info, and active roles.",
                lblCountStaff, "0 Staff",
                Theme.UPIColor,
                () => SelectTab(MasterTab.Staff)
            ));

            // 5. Suppliers Master Card
            lblCountSuppliers = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "🚚", "Suppliers & Vendors", 
                "Wholesale suppliers and distributors for cafe ingredients, raw groceries, packaging boxes, and beverages.",
                lblCountSuppliers, "0 Suppliers",
                Theme.WalletColor,
                () => SelectTab(MasterTab.Suppliers)
            ));

            // 6. HSN & SAC GST Master Card
            lblCountHsnSac = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "📑", "HSN & SAC Master (GST)", 
                "Tariff classification codes for restaurant food services (SAC 996331) and retail goods with 5% GST.",
                lblCountHsnSac, "0 Codes",
                Color.FromArgb(168, 85, 247),
                () => SelectTab(MasterTab.HsnSac)
            ));

            // 7. Users & Roles Master Card
            lblCountUsers = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "👤", "System Users & Roles", 
                "Manage POS cashiers, managers, and admin logins with custom operational access permissions.",
                lblCountUsers, "0 Users",
                Color.FromArgb(244, 114, 182),
                () => SelectTab(MasterTab.Users)
            ));

            // 8. Cafe Profile Master Card
            Label lblProfilePlaceholder = new Label();
            cardsFlowPanel.Controls.Add(CreateMasterFeatureCard(
                "⚙️", "Cafe Profile & Settings", 
                "Cafe trading name, address, GSTIN, receipt header/footer notes ('Tashi Delek! Thukje Che!'), and tax setup.",
                lblProfilePlaceholder, "Active Profile",
                Color.FromArgb(45, 212, 191),
                () => SelectTab(MasterTab.Profile)
            ));

            overviewPanel.Controls.Add(cardsFlowPanel);
            overviewHeader.SendToBack();
            cardsFlowPanel.BringToFront();

            overviewPanel.SizeChanged += (s, e) => AdjustCardsLayout();
            AdjustCardsLayout();
        }

        private void AdjustCardsLayout()
        {
            if (cardsFlowPanel == null || masterCards.Count == 0) return;

            int availableWidth = cardsFlowPanel.ClientSize.Width;
            if (availableWidth <= 200)
            {
                if (overviewPanel != null && overviewPanel.ClientSize.Width > 200)
                    availableWidth = overviewPanel.ClientSize.Width - 30;
                else if (contentHostPanel != null && contentHostPanel.ClientSize.Width > 200)
                    availableWidth = contentHostPanel.ClientSize.Width - 30;
                else if (this.ClientSize.Width > 200)
                    availableWidth = this.ClientSize.Width - 30;
                else
                    return;
            }

            // Reserve safety padding for vertical scrollbar
            availableWidth -= 24;

            int cols;
            if (availableWidth >= 1200) cols = 4;
            else if (availableWidth >= 840) cols = 3;
            else if (availableWidth >= 520) cols = 2;
            else cols = 1;

            int margin = 8;
            int totalMargins = margin * 2 * cols;
            int cardWidth = Math.Max(220, (availableWidth - totalMargins) / cols);

            cardsFlowPanel.SuspendLayout();
            foreach (var card in masterCards)
            {
                card.Width = cardWidth;
                card.Margin = new Padding(margin, margin, margin, margin);

                foreach (Control c in card.Controls)
                {
                    if (c.Name == "lblDesc")
                    {
                        c.AutoSize = false;
                        c.Width = cardWidth - 36;
                    }
                    else if (c.Name == "btnOpen")
                    {
                        c.Location = new Point(cardWidth - c.Width - 16, card.Height - 44);
                    }
                }
                card.Invalidate();
            }
            cardsFlowPanel.ResumeLayout(true);
        }

        private Panel CreateMasterFeatureCard(
            string icon, 
            string title, 
            string description, 
            Label countLabel, 
            string defaultCount,
            Color accentColor,
            Action onOpen)
        {
            Panel card = new Panel();
            card.Size = new Size(330, 210);
            card.Margin = new Padding(8, 8, 12, 14);
            card.BackColor = Theme.CardBg;
            card.Cursor = Cursors.Hand;
            card.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
                // Left accent vertical stripe
                using (SolidBrush b = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 5, card.Height);
                }
            };

            // Icon & Header
            Label lblIcon = new Label();
            lblIcon.Text = icon;
            lblIcon.Font = new Font("Segoe UI", 18F, FontStyle.Regular);
            lblIcon.Location = new Point(16, 14);
            lblIcon.Size = new Size(36, 36);
            lblIcon.BackColor = Color.Transparent;
            card.Controls.Add(lblIcon);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Location = new Point(56, 16);
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.BoldFont);
            card.Controls.Add(lblTitle);

            // Count Badge
            countLabel.Text = defaultCount;
            countLabel.Location = new Point(56, 38);
            countLabel.AutoSize = true;
            countLabel.BackColor = Color.Transparent;
            Theme.StyleLabel(countLabel, accentColor, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(countLabel);

            // Description
            Label lblDesc = new Label();
            lblDesc.Name = "lblDesc";
            lblDesc.Text = description;
            lblDesc.Location = new Point(18, 64);
            lblDesc.AutoSize = false;
            lblDesc.Size = new Size(card.Width - 36, 80);
            lblDesc.AutoEllipsis = true;
            lblDesc.BackColor = Color.Transparent;
            Theme.StyleLabel(lblDesc, Theme.TextMuted, new Font("Segoe UI", 8.5F, FontStyle.Regular));
            card.Controls.Add(lblDesc);

            // Action Open Button
            Button btnOpen = new Button();
            btnOpen.Name = "btnOpen";
            btnOpen.Text = "Open Catalog ➔";
            btnOpen.Size = new Size(135, 32);
            btnOpen.Location = new Point(card.Width - 150, 160);
            btnOpen.FlatStyle = FlatStyle.Flat;
            btnOpen.BackColor = Theme.SidebarHover;
            btnOpen.ForeColor = Theme.TextLight;
            btnOpen.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.FlatAppearance.MouseOverBackColor = accentColor;
            btnOpen.Cursor = Cursors.Hand;
            btnOpen.Click += (s, e) => onOpen?.Invoke();
            card.Controls.Add(btnOpen);

            // Click entire card to open
            card.Click += (s, e) => onOpen?.Invoke();
            lblDesc.Click += (s, e) => onOpen?.Invoke();

            masterCards.Add(card);
            return card;
        }

        private void RefreshOverviewStats()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Products Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountProducts.Text = $"{count} Dishes / Items";
                        }
                    }
                    catch { }

                    // Categories Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Categories", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountCategories.Text = $"{count} Categories";
                        }
                    }
                    catch { }

                    // Customers Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Customers", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountCustomers.Text = $"{count} Registered Guests";
                        }
                    }
                    catch { }

                    // Staff Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Staff", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountStaff.Text = $"{count} Stewards & Staff";
                        }
                    }
                    catch { }

                    // Suppliers Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Suppliers", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountSuppliers.Text = $"{count} Vendors";
                        }
                    }
                    catch { }

                    // HSN & SAC Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM HsnSacMaster", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountHsnSac.Text = $"{count} HSN/SAC Codes";
                        }
                    }
                    catch { }

                    // Users Count
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                        {
                            int count = (int)cmd.ExecuteScalar();
                            lblCountUsers.Text = $"{count} User Accounts";
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
