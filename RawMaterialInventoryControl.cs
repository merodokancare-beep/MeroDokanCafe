using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace MeroDokan
{
    public class RawMaterialInventoryControl : UserControl
    {
        private FlowLayoutPanel tabHeaderPanel;
        private Panel tabContentPanel;
        private Button btnTabRegister;
        private Button btnTabUsage;
        private Button btnTabInward;
        private Button btnTabKitchenClose;
        private Button btnTabLedger;
        
        // Tab 1: Stock Register
        private Panel tabRegister;
        private TextBox txtSearchRegister;
        private ComboBox comboCategoryRegister;
        private CheckBox chkLowStockOnly;
        private DataGridView gridRegister;
        private Button btnNewMaterial;
        private Button btnEditMaterial;
        private Button btnDeleteMaterial;
        private Button btnAdjustStock;
        private Button btnQuickLogUsage;
        private Button btnQuickInward;
        private Label lblTotalStockValue;
        private Label lblTotalItemCount;

        // Tab 2: Daily Kitchen Usage (Outward)
        private Panel tabUsage;
        private ComboBox comboUsageMaterial;
        private TextBox txtUsageQty;
        private Label lblUsageUnit;
        private ComboBox comboUsageDept;
        private DateTimePicker dtpUsageDate;
        private TextBox txtUsageRemarks;
        private Button btnSaveUsage;
        private DataGridView gridTodayUsage;
        private Label lblTodayUsageCost;
        private Button btnDeleteUsageEntry;

        // Tab 3: Stock Inward (Purchases)
        private Panel tabInward;
        private ComboBox comboInwardMaterial;
        private ComboBox comboInwardSupplier;
        private TextBox txtInwardRefNo;
        private TextBox txtInwardQty;
        private Label lblInwardUnit;
        private TextBox txtInwardUnitCost;
        private DateTimePicker dtpInwardDate;
        private TextBox txtInwardRemarks;
        private Button btnSaveInward;
        private DataGridView gridRecentInward;
        private Label lblTodayInwardCost;
        private Button btnDeleteInwardEntry;

        // Tab 4: Close Kitchen (Return Unused Stock)
        private Panel tabKitchenClose;
        private ComboBox comboCloseMaterial;
        private Label lblCloseIssuedBadge;
        private TextBox txtCloseReturnQty;
        private Label lblCloseUnit;
        private ComboBox comboCloseReason;
        private DateTimePicker dtpCloseDate;
        private TextBox txtCloseRemarks;
        private Button btnSaveKitchenClose;
        private DataGridView gridTodayIssued;
        private DataGridView gridTodayReturns;
        private Label lblTodayReturnCost;
        private Label lblTodayNetUsageCost;
        private Button btnDeleteReturnEntry;

        // Tab 5: Movement Ledger
        private Panel tabLedger;
        private DateTimePicker dtpLedgerFrom;
        private DateTimePicker dtpLedgerTo;
        private ComboBox comboLedgerType;
        private TextBox txtLedgerSearch;
        private Button btnFilterLedger;
        private DataGridView gridLedger;

        public RawMaterialInventoryControl()
        {
            InitializeComponent();
            LoadRegisterData();
            LoadUsageDropdowns();
            LoadTodayUsageData();
            LoadInwardDropdowns();
            LoadRecentInwardData();
            LoadCloseDropdowns();
            LoadTodayIssuedList();
            LoadTodayReturnsData();
            LoadLedgerData();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;
            this.BackColor = Theme.Secondary;
            this.Padding = new Padding(0);

            // 1. Sleek Page Header
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.AutoSize = true;
            headerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            headerPanel.BackColor = Theme.Primary;
            headerPanel.Padding = new Padding(20, 10, 20, 4);
            this.Controls.Add(headerPanel);

            Label lblTitle = new Label();
            lblTitle.Text = "☕  Cafe Raw Material Stock & Daily Usage Hub";
            lblTitle.Dock = DockStyle.Top;
            lblTitle.AutoSize = true;
            lblTitle.UseMnemonic = false;
            Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.HeaderFont);
            headerPanel.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Track raw ingredients, record daily kitchen/barista consumption, receive inward stock, and audit inventory.";
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.AutoSize = true;
            lblSubtitle.UseMnemonic = false;
            lblSubtitle.ForeColor = Theme.TextMuted;
            lblSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblSubtitle.Padding = new Padding(0, 2, 0, 6);
            headerPanel.Controls.Add(lblSubtitle);

            // 2. Tab Navigation Bar (Dark Styled Buttons)
            tabHeaderPanel = new FlowLayoutPanel();
            tabHeaderPanel.Dock = DockStyle.Top;
            tabHeaderPanel.AutoSize = true;
            tabHeaderPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tabHeaderPanel.BackColor = Color.FromArgb(15, 23, 42); // Sleek deep slate
            tabHeaderPanel.Padding = new Padding(16, 6, 16, 6);
            tabHeaderPanel.WrapContents = true;
            tabHeaderPanel.AutoScroll = false;
            this.Controls.Add(tabHeaderPanel);

            btnTabRegister = new Button { Text = "📋 Raw Material Stock Register", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, Padding = new Padding(14, 0, 14, 0), Margin = new Padding(0, 0, 8, 4), UseMnemonic = false };
            btnTabRegister.Click += (s, e) => ShowTab(tabRegister, btnTabRegister);
            tabHeaderPanel.Controls.Add(btnTabRegister);

            btnTabUsage = new Button { Text = "📝 Daily Kitchen Usage Log (- OUT)", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, Padding = new Padding(14, 0, 14, 0), Margin = new Padding(0, 0, 8, 4), UseMnemonic = false };
            btnTabUsage.Click += (s, e) => ShowTab(tabUsage, btnTabUsage);
            tabHeaderPanel.Controls.Add(btnTabUsage);

            btnTabInward = new Button { Text = "🚚 Stock Inward Delivery (+ IN)", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, Padding = new Padding(14, 0, 14, 0), Margin = new Padding(0, 0, 8, 4), UseMnemonic = false };
            btnTabInward.Click += (s, e) => ShowTab(tabInward, btnTabInward);
            tabHeaderPanel.Controls.Add(btnTabInward);

            btnTabKitchenClose = new Button { Text = "🌙 Close Kitchen / Return Stock", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, Padding = new Padding(14, 0, 14, 0), Margin = new Padding(0, 0, 8, 4), UseMnemonic = false };
            btnTabKitchenClose.Click += (s, e) => ShowTab(tabKitchenClose, btnTabKitchenClose);
            tabHeaderPanel.Controls.Add(btnTabKitchenClose);

            btnTabLedger = new Button { Text = "📜 Movement Audit Ledger", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Height = 36, Padding = new Padding(14, 0, 14, 0), Margin = new Padding(0, 0, 8, 4), UseMnemonic = false };
            btnTabLedger.Click += (s, e) => ShowTab(tabLedger, btnTabLedger);
            tabHeaderPanel.Controls.Add(btnTabLedger);

            // 3. Main Tab Content Container
            tabContentPanel = new Panel();
            tabContentPanel.Dock = DockStyle.Fill;
            tabContentPanel.BackColor = Theme.Secondary;
            this.Controls.Add(tabContentPanel);

            // Tab Panels
            tabRegister = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Secondary };
            BuildRegisterTab();
            tabContentPanel.Controls.Add(tabRegister);

            tabUsage = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Secondary };
            BuildUsageTab();
            tabContentPanel.Controls.Add(tabUsage);

            tabInward = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Secondary };
            BuildInwardTab();
            tabContentPanel.Controls.Add(tabInward);

            tabKitchenClose = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Secondary };
            BuildKitchenCloseTab();
            tabContentPanel.Controls.Add(tabKitchenClose);

            tabLedger = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Secondary };
            BuildLedgerTab();
            tabContentPanel.Controls.Add(tabLedger);

            // Z-Order: Ensure header & tab nav stay at top, content fills rest
            headerPanel.SendToBack();
            tabHeaderPanel.SendToBack();
            tabContentPanel.BringToFront();

            // Default Active Tab
            ShowTab(tabRegister, btnTabRegister);
        }

        private void ShowTab(Panel selectedPanel, Button activeBtn)
        {
            tabRegister.Visible = false;
            tabUsage.Visible = false;
            tabInward.Visible = false;
            tabKitchenClose.Visible = false;
            tabLedger.Visible = false;

            selectedPanel.Visible = true;

            StyleTabButton(btnTabRegister, btnTabRegister == activeBtn);
            StyleTabButton(btnTabUsage, btnTabUsage == activeBtn);
            StyleTabButton(btnTabInward, btnTabInward == activeBtn);
            StyleTabButton(btnTabKitchenClose, btnTabKitchenClose == activeBtn);
            StyleTabButton(btnTabLedger, btnTabLedger == activeBtn);

            if (selectedPanel == tabRegister) LoadRegisterData();
            else if (selectedPanel == tabUsage) { LoadUsageDropdowns(); LoadTodayUsageData(); }
            else if (selectedPanel == tabInward) { LoadInwardDropdowns(); LoadRecentInwardData(); }
            else if (selectedPanel == tabKitchenClose) { LoadCloseDropdowns(); LoadTodayIssuedList(); LoadTodayReturnsData(); }
            else if (selectedPanel == tabLedger) LoadLedgerData();
        }

        private void StyleTabButton(Button btn, bool isActive)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = Theme.BoldFont;
            btn.Cursor = Cursors.Hand;
            btn.Height = 36;

            if (isActive)
            {
                btn.BackColor = Theme.Accent; // Vibrant Orange / Indigo Accent
                btn.ForeColor = Theme.TextWhite;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }
            else
            {
                btn.BackColor = Color.FromArgb(17, 24, 39); // Match card bg depth
                btn.ForeColor = Theme.TextMuted; // Slate 400
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.CardBorder;
                btn.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            }
        }

        #region TAB 1: STOCK REGISTER
        private void BuildRegisterTab()
        {
            // Filter / Action Bar
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Dock = DockStyle.Top;
            filterBar.AutoSize = true;
            filterBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            filterBar.BackColor = Theme.Primary;
            filterBar.Padding = new Padding(12, 8, 12, 8);
            filterBar.WrapContents = true;
            filterBar.Margin = new Padding(0);
            tabRegister.Controls.Add(filterBar);

            txtSearchRegister = new TextBox();
            txtSearchRegister.Size = new Size(180, 28);
            txtSearchRegister.Font = Theme.MainFont;
            txtSearchRegister.Margin = new Padding(4, 4, 6, 4);
            Theme.StyleTextBox(txtSearchRegister);
            txtSearchRegister.TextChanged += (s, e) => LoadRegisterData();
            filterBar.Controls.Add(txtSearchRegister);

            comboCategoryRegister = new ComboBox();
            comboCategoryRegister.Size = new Size(160, 28);
            comboCategoryRegister.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCategoryRegister.BackColor = Theme.Secondary;
            comboCategoryRegister.ForeColor = Theme.TextLight;
            comboCategoryRegister.Font = Theme.MainFont;
            comboCategoryRegister.Margin = new Padding(4, 4, 6, 4);
            comboCategoryRegister.Items.AddRange(new string[] { "All Categories", "Dairy & Milk", "Coffee & Tea", "Meats & Non-Veg", "Pantry & Grains", "Produce", "Packaging & Disposables", "Beverages & Syrups" });
            comboCategoryRegister.SelectedIndex = 0;
            comboCategoryRegister.SelectedIndexChanged += (s, e) => LoadRegisterData();
            filterBar.Controls.Add(comboCategoryRegister);

            chkLowStockOnly = new CheckBox();
            chkLowStockOnly.Text = "⚠️ Low Stock Only";
            chkLowStockOnly.AutoSize = true;
            chkLowStockOnly.ForeColor = Theme.Warning;
            chkLowStockOnly.Font = Theme.BoldFont;
            chkLowStockOnly.Margin = new Padding(6, 8, 10, 4);
            chkLowStockOnly.CheckedChanged += (s, e) => LoadRegisterData();
            filterBar.Controls.Add(chkLowStockOnly);

            btnNewMaterial = new Button();
            btnNewMaterial.Text = "➕ New Raw Material";
            btnNewMaterial.Size = new Size(155, 30);
            btnNewMaterial.Margin = new Padding(4, 3, 4, 3);
            btnNewMaterial.UseMnemonic = false;
            Theme.StyleSuccessButton(btnNewMaterial);
            btnNewMaterial.Click += BtnNewMaterial_Click;
            filterBar.Controls.Add(btnNewMaterial);

            btnQuickLogUsage = new Button();
            btnQuickLogUsage.Text = "📝 Log Kitchen";
            btnQuickLogUsage.Size = new Size(120, 30);
            btnQuickLogUsage.Margin = new Padding(4, 3, 4, 3);
            btnQuickLogUsage.UseMnemonic = false;
            Theme.StylePrimaryButton(btnQuickLogUsage);
            btnQuickLogUsage.Click += (s, e) => { ShowTab(tabUsage, btnTabUsage); };
            filterBar.Controls.Add(btnQuickLogUsage);

            btnQuickInward = new Button();
            btnQuickInward.Text = "🚚 Inward Delivery";
            btnQuickInward.Size = new Size(130, 30);
            btnQuickInward.Margin = new Padding(4, 3, 4, 3);
            btnQuickInward.UseMnemonic = false;
            Theme.StyleSecondaryButton(btnQuickInward);
            btnQuickInward.Click += (s, e) => { ShowTab(tabInward, btnTabInward); };
            filterBar.Controls.Add(btnQuickInward);

            btnAdjustStock = new Button();
            btnAdjustStock.Text = "⚙️ Adjust Stock";
            btnAdjustStock.Size = new Size(120, 30);
            btnAdjustStock.Margin = new Padding(4, 3, 4, 3);
            btnAdjustStock.UseMnemonic = false;
            Theme.StyleSecondaryButton(btnAdjustStock);
            btnAdjustStock.Click += BtnAdjustStock_Click;
            filterBar.Controls.Add(btnAdjustStock);

            btnEditMaterial = new Button();
            btnEditMaterial.Text = "✏️ Edit";
            btnEditMaterial.Size = new Size(75, 30);
            btnEditMaterial.Margin = new Padding(4, 3, 4, 3);
            btnEditMaterial.UseMnemonic = false;
            Theme.StyleSecondaryButton(btnEditMaterial);
            btnEditMaterial.Click += BtnEditMaterial_Click;
            filterBar.Controls.Add(btnEditMaterial);

            btnDeleteMaterial = new Button();
            btnDeleteMaterial.Text = "🗑️ Delete";
            btnDeleteMaterial.Size = new Size(85, 30);
            btnDeleteMaterial.Margin = new Padding(4, 3, 4, 3);
            btnDeleteMaterial.UseMnemonic = false;
            Theme.StyleDangerButton(btnDeleteMaterial);
            btnDeleteMaterial.Click += BtnDeleteMaterial_Click;
            filterBar.Controls.Add(btnDeleteMaterial);

            // Bottom Summary Bar
            Panel summaryBar = new Panel();
            summaryBar.Dock = DockStyle.Bottom;
            summaryBar.Height = 36;
            summaryBar.BackColor = Theme.Primary;
            summaryBar.Padding = new Padding(16, 6, 16, 6);
            tabRegister.Controls.Add(summaryBar);

            lblTotalItemCount = new Label();
            lblTotalItemCount.Text = "📦 Total Active Ingredients: 0";
            lblTotalItemCount.Dock = DockStyle.Left;
            lblTotalItemCount.AutoSize = true;
            lblTotalItemCount.UseMnemonic = false;
            lblTotalItemCount.Font = Theme.BoldFont;
            lblTotalItemCount.ForeColor = Theme.TextMuted;
            summaryBar.Controls.Add(lblTotalItemCount);

            lblTotalStockValue = new Label();
            lblTotalStockValue.Text = "Total Raw Material Inventory Value: Rs. 0.00";
            lblTotalStockValue.Dock = DockStyle.Right;
            lblTotalStockValue.AutoSize = true;
            lblTotalStockValue.UseMnemonic = false;
            lblTotalStockValue.Font = Theme.BoldFont;
            lblTotalStockValue.ForeColor = Theme.Accent;
            summaryBar.Controls.Add(lblTotalStockValue);

            // DataGridView
            gridRegister = new DataGridView();
            gridRegister.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridRegister);
            gridRegister.ColumnHeadersVisible = true;
            gridRegister.ColumnHeadersHeight = 40;
            gridRegister.DataBindingComplete += GridRegister_DataBindingComplete;
            tabRegister.Controls.Add(gridRegister);

            // Docking order: Edge docked controls (Top/Bottom) must be at back, Fill grid must be at front
            filterBar.SendToBack();
            summaryBar.SendToBack();
            gridRegister.BringToFront();
        }

        private void LoadRegisterData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Id,
                            Code as [Code],
                            Name as [Raw Material Item],
                            Category as [Category],
                            CurrentStock as [In-Hand Stock],
                            Unit as [Unit],
                            MinStockLevel as [Min Level],
                            CASE WHEN CurrentStock <= MinStockLevel THEN 'CRITICAL LOW' ELSE 'OPTIMAL' END as [Stock Status],
                            UnitPrice as [Unit Cost (Rs.)],
                            (CurrentStock * UnitPrice) as [Total Value (Rs.)]
                        FROM RawMaterials
                        WHERE IsActive = 1";

                    if (comboCategoryRegister != null && comboCategoryRegister.SelectedIndex > 0)
                    {
                        query += " AND Category = @cat";
                    }

                    if (chkLowStockOnly != null && chkLowStockOnly.Checked)
                    {
                        query += " AND CurrentStock <= MinStockLevel";
                    }

                    if (txtSearchRegister != null && !string.IsNullOrWhiteSpace(txtSearchRegister.Text))
                    {
                        query += " AND (Code LIKE @search OR Name LIKE @search OR Category LIKE @search)";
                    }

                    query += " ORDER BY [Stock Status] ASC, CurrentStock ASC, Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (comboCategoryRegister != null && comboCategoryRegister.SelectedIndex > 0)
                        {
                            cmd.Parameters.AddWithValue("@cat", comboCategoryRegister.SelectedItem.ToString());
                        }

                        if (txtSearchRegister != null && !string.IsNullOrWhiteSpace(txtSearchRegister.Text))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{txtSearchRegister.Text.Trim()}%");
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridRegister.DataSource = dt;

                            if (gridRegister.Columns["Id"] != null) gridRegister.Columns["Id"].Visible = false;

                            decimal totalVal = 0;
                            int totalCount = dt.Rows.Count;
                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Total Value (Rs.)"] != DBNull.Value)
                                    totalVal += Convert.ToDecimal(r["Total Value (Rs.)"]);
                            }

                            lblTotalItemCount.Text = $"📦 Total Ingredients: {totalCount}";
                            lblTotalStockValue.Text = $"Total Inventory Valuation: Rs. {totalVal:N2}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRegisterData Error: {ex.Message}");
            }
        }

        private void GridRegister_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridRegister.Columns["Code"] != null)
                {
                    gridRegister.Columns["Code"].FillWeight = 50;
                    gridRegister.Columns["Code"].MinimumWidth = 85;
                }
                if (gridRegister.Columns["Raw Material Item"] != null)
                {
                    gridRegister.Columns["Raw Material Item"].FillWeight = 130;
                    gridRegister.Columns["Raw Material Item"].MinimumWidth = 160;
                }
                if (gridRegister.Columns["Category"] != null)
                {
                    gridRegister.Columns["Category"].FillWeight = 75;
                    gridRegister.Columns["Category"].MinimumWidth = 110;
                }
                if (gridRegister.Columns["In-Hand Stock"] != null)
                {
                    gridRegister.Columns["In-Hand Stock"].FillWeight = 60;
                    gridRegister.Columns["In-Hand Stock"].MinimumWidth = 90;
                    gridRegister.Columns["In-Hand Stock"].DefaultCellStyle.Format = "N3";
                    gridRegister.Columns["In-Hand Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRegister.Columns["Unit"] != null)
                {
                    gridRegister.Columns["Unit"].FillWeight = 40;
                    gridRegister.Columns["Unit"].MinimumWidth = 60;
                    gridRegister.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridRegister.Columns["Min Level"] != null)
                {
                    gridRegister.Columns["Min Level"].FillWeight = 50;
                    gridRegister.Columns["Min Level"].MinimumWidth = 80;
                    gridRegister.Columns["Min Level"].DefaultCellStyle.Format = "N3";
                    gridRegister.Columns["Min Level"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRegister.Columns["Stock Status"] != null)
                {
                    gridRegister.Columns["Stock Status"].FillWeight = 70;
                    gridRegister.Columns["Stock Status"].MinimumWidth = 100;
                    gridRegister.Columns["Stock Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridRegister.Columns["Unit Cost (Rs.)"] != null)
                {
                    gridRegister.Columns["Unit Cost (Rs.)"].FillWeight = 60;
                    gridRegister.Columns["Unit Cost (Rs.)"].MinimumWidth = 95;
                    gridRegister.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridRegister.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRegister.Columns["Total Value (Rs.)"] != null)
                {
                    gridRegister.Columns["Total Value (Rs.)"].FillWeight = 70;
                    gridRegister.Columns["Total Value (Rs.)"].MinimumWidth = 105;
                    gridRegister.Columns["Total Value (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridRegister.Columns["Total Value (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                foreach (DataGridViewRow row in gridRegister.Rows)
                {
                    if (row.Cells["Stock Status"] != null && row.Cells["Stock Status"].Value != null)
                    {
                        string status = row.Cells["Stock Status"].Value.ToString();
                        if (status == "CRITICAL LOW")
                        {
                            row.Cells["Stock Status"].Style.ForeColor = Theme.Danger;
                            row.Cells["Stock Status"].Style.Font = Theme.BoldFont;
                            row.Cells["In-Hand Stock"].Style.ForeColor = Theme.Danger;
                            row.Cells["In-Hand Stock"].Style.Font = Theme.BoldFont;
                        }
                        else
                        {
                            row.Cells["Stock Status"].Style.ForeColor = Theme.Success;
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnNewMaterial_Click(object sender, EventArgs e)
        {
            using (var dlg = new RawMaterialModalDialog(null))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadRegisterData();
                    LoadUsageDropdowns();
                    LoadInwardDropdowns();
                }
            }
        }

        private void BtnEditMaterial_Click(object sender, EventArgs e)
        {
            if (gridRegister.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a raw material to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id = Convert.ToInt32(gridRegister.SelectedRows[0].Cells["Id"].Value);
            using (var dlg = new RawMaterialModalDialog(id))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadRegisterData();
                    LoadUsageDropdowns();
                    LoadInwardDropdowns();
                }
            }
        }

        private void BtnAdjustStock_Click(object sender, EventArgs e)
        {
            if (gridRegister.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a raw material to adjust stock count.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id = Convert.ToInt32(gridRegister.SelectedRows[0].Cells["Id"].Value);
            string name = gridRegister.SelectedRows[0].Cells["Raw Material Item"].Value.ToString();
            string unit = gridRegister.SelectedRows[0].Cells["Unit"].Value.ToString();
            decimal curStock = Convert.ToDecimal(gridRegister.SelectedRows[0].Cells["In-Hand Stock"].Value);

            using (var dlg = new AdjustRawMaterialDialog(id, name, unit, curStock))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadRegisterData();
                    LoadLedgerData();
                }
            }
        }

        private void BtnDeleteMaterial_Click(object sender, EventArgs e)
        {
            if (gridRegister.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a raw material to deactivate.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id = Convert.ToInt32(gridRegister.SelectedRows[0].Cells["Id"].Value);
            string name = gridRegister.SelectedRows[0].Cells["Raw Material Item"].Value.ToString();

            if (MessageBox.Show($"Are you sure you want to deactivate '{name}'?", "Confirm Deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("UPDATE RawMaterials SET IsActive = 0 WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadRegisterData();
                    LoadUsageDropdowns();
                    LoadInwardDropdowns();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deactivating raw material: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region TAB 2: DAILY KITCHEN USAGE (OUTWARD)
        private void BuildUsageTab()
        {
            // 1. Top Input Card
            Panel entryCard = new Panel();
            entryCard.Dock = DockStyle.Top;
            entryCard.AutoSize = true;
            entryCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            entryCard.BackColor = Color.FromArgb(16, 22, 34);
            entryCard.Padding = new Padding(16, 12, 16, 14);
            tabUsage.Controls.Add(entryCard);

            // Header Banner
            Label lblCardTitle = new Label();
            lblCardTitle.Text = "📝  Record Kitchen & Barista Raw Material Consumption (- OUT)";
            lblCardTitle.Dock = DockStyle.Top;
            lblCardTitle.Height = 24;
            lblCardTitle.UseMnemonic = false;
            Theme.StyleLabel(lblCardTitle, Theme.Accent, Theme.BoldFont);
            entryCard.Controls.Add(lblCardTitle);

            // Row 1: 4 Equal Grid Columns for Inputs
            TableLayoutPanel row1Layout = new TableLayoutPanel();
            row1Layout.Dock = DockStyle.Top;
            row1Layout.AutoSize = true;
            row1Layout.ColumnCount = 4;
            row1Layout.RowCount = 2;
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            row1Layout.Padding = new Padding(0, 4, 0, 8);
            entryCard.Controls.Add(row1Layout);

            // Row 1 Labels
            Label lblMat = new Label { Text = "Raw Material / Ingredient *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblMat, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblMat, 0, 0);

            Label lblQty = new Label { Text = "Quantity Used (- OUT) *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblQty, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblQty, 1, 0);

            Label lblDept = new Label { Text = "Kitchen Section / Station *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblDept, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblDept, 2, 0);

            Label lblDate = new Label { Text = "Usage Date *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblDate, 3, 0);

            // Row 1 Controls
            comboUsageMaterial = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28, Margin = new Padding(0, 2, 8, 0) };
            comboUsageMaterial.BackColor = Theme.Secondary;
            comboUsageMaterial.ForeColor = Theme.TextLight;
            comboUsageMaterial.Font = Theme.MainFont;
            comboUsageMaterial.SelectedIndexChanged += ComboUsageMaterial_SelectedIndexChanged;
            row1Layout.Controls.Add(comboUsageMaterial, 0, 1);

            TableLayoutPanel qtyBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Height = 30, Margin = new Padding(0, 2, 8, 0) };
            qtyBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            qtyBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            txtUsageQty = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 0, 4, 0) };
            Theme.StyleTextBox(txtUsageQty);
            lblUsageUnit = new Label { Dock = DockStyle.Fill, Text = "Kg", TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            Theme.StyleLabel(lblUsageUnit, Theme.Accent, Theme.BoldFont);
            qtyBox.Controls.Add(txtUsageQty, 0, 0);
            qtyBox.Controls.Add(lblUsageUnit, 1, 0);
            row1Layout.Controls.Add(qtyBox, 1, 1);

            comboUsageDept = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28, Margin = new Padding(0, 2, 8, 0) };
            comboUsageDept.BackColor = Theme.Secondary;
            comboUsageDept.ForeColor = Theme.TextLight;
            comboUsageDept.Font = Theme.MainFont;
            comboUsageDept.Items.AddRange(new string[] { "Kitchen / Main Cook", "Barista / Coffee Bar", "Bakery & Pastry", "Food Prep Station", "Store / General", "Wastage / Expired (- OUT)" });
            comboUsageDept.SelectedIndex = 0;
            row1Layout.Controls.Add(comboUsageDept, 2, 1);

            dtpUsageDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Font = Theme.MainFont, Height = 28, Margin = new Padding(0, 2, 0, 0) };
            row1Layout.Controls.Add(dtpUsageDate, 3, 1);

            // Row 2: Full Width Notes + Aligned Action Button
            TableLayoutPanel row2Layout = new TableLayoutPanel();
            row2Layout.Dock = DockStyle.Top;
            row2Layout.AutoSize = true;
            row2Layout.ColumnCount = 2;
            row2Layout.RowCount = 2;
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78f));
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            row2Layout.Padding = new Padding(0, 2, 0, 0);
            entryCard.Controls.Add(row2Layout);

            Label lblRemTag = new Label { Text = "Cooking / Preparation Notes / Spoilage Reason", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblRemTag, Theme.TextMuted, Theme.BoldFont);
            row2Layout.Controls.Add(lblRemTag, 0, 0);

            Label lblEmpty = new Label { Text = "", AutoSize = true, Dock = DockStyle.Fill };
            row2Layout.Controls.Add(lblEmpty, 1, 0);

            txtUsageRemarks = new TextBox { Dock = DockStyle.Fill, Height = 28, Margin = new Padding(0, 2, 10, 0) };
            Theme.StyleTextBox(txtUsageRemarks);
            row2Layout.Controls.Add(txtUsageRemarks, 0, 1);

            btnSaveUsage = new Button { Text = "💾  RECORD USAGE", Dock = DockStyle.Fill, Height = 32, Margin = new Padding(0, 0, 0, 0), UseMnemonic = false };
            Theme.StyleSuccessButton(btnSaveUsage);
            btnSaveUsage.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSaveUsage.Click += BtnSaveUsage_Click;
            row2Layout.Controls.Add(btnSaveUsage, 1, 1);

            // Order of controls in entryCard
            lblCardTitle.SendToBack();
            row1Layout.BringToFront();
            row2Layout.BringToFront();

            // 2. Grid Section Container
            Panel gridContainer = new Panel();
            gridContainer.Dock = DockStyle.Fill;
            gridContainer.Padding = new Padding(16, 8, 16, 10);
            tabUsage.Controls.Add(gridContainer);

            Panel gridHeaderStrip = new Panel();
            gridHeaderStrip.Dock = DockStyle.Top;
            gridHeaderStrip.Height = 36;
            gridHeaderStrip.Padding = new Padding(0, 4, 0, 4);
            gridContainer.Controls.Add(gridHeaderStrip);

            Label lblGridTitle = new Label();
            lblGridTitle.Text = "📋  Today's Recorded Kitchen Consumption Logs";
            lblGridTitle.Dock = DockStyle.Left;
            lblGridTitle.AutoSize = true;
            lblGridTitle.UseMnemonic = false;
            Theme.StyleLabel(lblGridTitle, Theme.TextLight, Theme.BoldFont);
            gridHeaderStrip.Controls.Add(lblGridTitle);

            FlowLayoutPanel rightStrip = new FlowLayoutPanel();
            rightStrip.Dock = DockStyle.Right;
            rightStrip.AutoSize = true;
            rightStrip.FlowDirection = FlowDirection.RightToLeft;
            gridHeaderStrip.Controls.Add(rightStrip);

            btnDeleteUsageEntry = new Button();
            btnDeleteUsageEntry.Text = "🗑️ Revert Selected";
            btnDeleteUsageEntry.Size = new Size(135, 28);
            btnDeleteUsageEntry.Margin = new Padding(8, 0, 0, 0);
            btnDeleteUsageEntry.UseMnemonic = false;
            Theme.StyleDangerButton(btnDeleteUsageEntry);
            btnDeleteUsageEntry.Click += BtnDeleteUsageEntry_Click;
            rightStrip.Controls.Add(btnDeleteUsageEntry);

            lblTodayUsageCost = new Label();
            lblTodayUsageCost.Text = "Today's Consumption Cost: Rs. 0.00";
            lblTodayUsageCost.AutoSize = true;
            lblTodayUsageCost.UseMnemonic = false;
            lblTodayUsageCost.Margin = new Padding(0, 4, 12, 0);
            Theme.StyleLabel(lblTodayUsageCost, Theme.Danger, Theme.BoldFont);
            rightStrip.Controls.Add(lblTodayUsageCost);

            gridTodayUsage = new DataGridView();
            gridTodayUsage.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridTodayUsage);
            gridTodayUsage.ColumnHeadersHeight = 38;
            gridTodayUsage.DataBindingComplete += GridTodayUsage_DataBindingComplete;
            gridContainer.Controls.Add(gridTodayUsage);

            // Docking order
            entryCard.SendToBack();
            gridContainer.BringToFront();
            gridHeaderStrip.SendToBack();
            gridTodayUsage.BringToFront();
        }

        private void LoadUsageDropdowns()
        {
            try
            {
                if (comboUsageMaterial == null) return;
                comboUsageMaterial.Items.Clear();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code, Name, Unit, CurrentStock, UnitPrice FROM RawMaterials WHERE IsActive = 1 ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboUsageMaterial.Items.Add(new MaterialComboItem {
                                    Id = Convert.ToInt32(rdr["Id"]),
                                    Code = rdr["Code"].ToString(),
                                    Name = rdr["Name"].ToString(),
                                    Unit = rdr["Unit"].ToString(),
                                    CurrentStock = Convert.ToDecimal(rdr["CurrentStock"]),
                                    UnitPrice = Convert.ToDecimal(rdr["UnitPrice"])
                                });
                            }
                        }
                    }
                }
                if (comboUsageMaterial.Items.Count > 0) comboUsageMaterial.SelectedIndex = 0;
            }
            catch { }
        }

        private void ComboUsageMaterial_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboUsageMaterial.SelectedItem is MaterialComboItem item)
            {
                lblUsageUnit.Text = $"{item.Unit} (In-Stock: {item.CurrentStock:N2} {item.Unit})";
            }
        }

        private void BtnSaveUsage_Click(object sender, EventArgs e)
        {
            if (comboUsageMaterial.SelectedItem == null)
            {
                MessageBox.Show("Please select a raw material.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtUsageQty.Text.Trim(), out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid positive quantity consumed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsageQty.Focus();
                return;
            }

            MaterialComboItem item = (MaterialComboItem)comboUsageMaterial.SelectedItem;

            // Insufficient stock check (prevent negative stock)
            if (qty > item.CurrentStock)
            {
                MessageBox.Show(
                    $"Cannot record kitchen usage of {qty:N2} {item.Unit} because only {item.CurrentStock:N2} {item.Unit} is currently available in stock.\n\nNegative stock is not permitted. Please enter a quantity up to {item.CurrentStock:N2} {item.Unit}, or record an Inward Stock Delivery first if new supplies have arrived.",
                    "Insufficient In-Hand Stock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtUsageQty.Focus();
                txtUsageQty.SelectAll();
                return;
            }

            string dept = comboUsageDept.SelectedItem?.ToString() ?? "Kitchen";
            string remarks = txtUsageRemarks.Text.Trim();
            if (string.IsNullOrEmpty(remarks)) remarks = "Daily Kitchen Consumption";
            DateTime usageDate = dtpUsageDate.Value;

            bool success = DatabaseHelper.LogRawMaterialUsage(item.Id, qty, dept, remarks, Session.Username ?? "Kitchen Staff", usageDate);
            if (success)
            {
                txtUsageQty.Clear();
                txtUsageRemarks.Clear();
                LoadUsageDropdowns();
                LoadTodayUsageData();
                LoadTodayIssuedList();
                LoadCloseDropdowns();
                MessageBox.Show($"Successfully recorded {qty:N2} {item.Unit} used for '{item.Name}'.", "Usage Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to record kitchen usage. Please verify that sufficient stock is available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteUsageEntry_Click(object sender, EventArgs e)
        {
            if (gridTodayUsage.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a usage log to revert.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int moveId = Convert.ToInt32(gridTodayUsage.SelectedRows[0].Cells["Id"].Value);
            string item = gridTodayUsage.SelectedRows[0].Cells["Raw Material"].Value.ToString();
            string qty = gridTodayUsage.SelectedRows[0].Cells["Qty Used (-)"].Value.ToString();

            if (MessageBox.Show($"Are you sure you want to revert {qty} of '{item}' back to stock?", "Confirm Reversal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (DatabaseHelper.DeleteStockMovement(moveId))
                {
                    LoadUsageDropdowns();
                    LoadTodayUsageData();
                    LoadTodayIssuedList();
                    LoadCloseDropdowns();
                }
            }
        }

        private void LoadTodayUsageData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            sm.Id,
                            sm.TransactionDate as [Date & Time],
                            rm.Name as [Raw Material],
                            rm.Category as [Category],
                            sm.Quantity as [Qty Used (-)],
                            rm.Unit as [Unit],
                            sm.UnitCost as [Rate (Rs.)],
                            sm.TotalCost as [Cost (Rs.)],
                            sm.Department as [Section / Station],
                            sm.Remarks as [Cooking Remarks],
                            sm.CreatedBy as [Logged By]
                        FROM StockMovements sm
                        JOIN RawMaterials rm ON sm.MaterialId = rm.Id
                        WHERE sm.TransactionType IN ('OUT_USAGE', 'OUT_WASTAGE')
                          AND CAST(sm.TransactionDate AS DATE) = CAST(GETDATE() AS DATE)
                        ORDER BY sm.TransactionDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridTodayUsage.DataSource = dt;

                            if (gridTodayUsage.Columns["Id"] != null) gridTodayUsage.Columns["Id"].Visible = false;

                            decimal todayCost = 0;
                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Cost (Rs.)"] != DBNull.Value) todayCost += Convert.ToDecimal(r["Cost (Rs.)"]);
                            }
                            lblTodayUsageCost.Text = $"Today's Total Usage Cost: Rs. {todayCost:N2}";
                        }
                    }
                }
            }
            catch { }
        }

        private void GridTodayUsage_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridTodayUsage.Columns["Date & Time"] != null)
                {
                    gridTodayUsage.Columns["Date & Time"].FillWeight = 65;
                    gridTodayUsage.Columns["Date & Time"].MinimumWidth = 85;
                    gridTodayUsage.Columns["Date & Time"].DefaultCellStyle.Format = "hh:mm tt";
                }
                if (gridTodayUsage.Columns["Raw Material"] != null)
                {
                    gridTodayUsage.Columns["Raw Material"].FillWeight = 120;
                    gridTodayUsage.Columns["Raw Material"].MinimumWidth = 150;
                }
                if (gridTodayUsage.Columns["Category"] != null)
                {
                    gridTodayUsage.Columns["Category"].FillWeight = 70;
                    gridTodayUsage.Columns["Category"].MinimumWidth = 100;
                }
                if (gridTodayUsage.Columns["Qty Used (-)"] != null)
                {
                    gridTodayUsage.Columns["Qty Used (-)"].FillWeight = 60;
                    gridTodayUsage.Columns["Qty Used (-)"].MinimumWidth = 90;
                    gridTodayUsage.Columns["Qty Used (-)"].DefaultCellStyle.Format = "N3";
                    gridTodayUsage.Columns["Qty Used (-)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayUsage.Columns["Unit"] != null)
                {
                    gridTodayUsage.Columns["Unit"].FillWeight = 40;
                    gridTodayUsage.Columns["Unit"].MinimumWidth = 60;
                    gridTodayUsage.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridTodayUsage.Columns["Rate (Rs.)"] != null)
                {
                    gridTodayUsage.Columns["Rate (Rs.)"].FillWeight = 55;
                    gridTodayUsage.Columns["Rate (Rs.)"].MinimumWidth = 85;
                    gridTodayUsage.Columns["Rate (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridTodayUsage.Columns["Rate (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayUsage.Columns["Cost (Rs.)"] != null)
                {
                    gridTodayUsage.Columns["Cost (Rs.)"].FillWeight = 65;
                    gridTodayUsage.Columns["Cost (Rs.)"].MinimumWidth = 95;
                    gridTodayUsage.Columns["Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridTodayUsage.Columns["Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayUsage.Columns["Section / Station"] != null)
                {
                    gridTodayUsage.Columns["Section / Station"].FillWeight = 85;
                    gridTodayUsage.Columns["Section / Station"].MinimumWidth = 120;
                }
                if (gridTodayUsage.Columns["Cooking Remarks"] != null)
                {
                    gridTodayUsage.Columns["Cooking Remarks"].FillWeight = 110;
                    gridTodayUsage.Columns["Cooking Remarks"].MinimumWidth = 140;
                }
                if (gridTodayUsage.Columns["Logged By"] != null)
                {
                    gridTodayUsage.Columns["Logged By"].FillWeight = 55;
                    gridTodayUsage.Columns["Logged By"].MinimumWidth = 80;
                }
            }
            catch { }
        }
        #endregion

        #region TAB 3: STOCK INWARD (PURCHASES)
        private void BuildInwardTab()
        {
            // 1. Top Input Card
            Panel entryCard = new Panel();
            entryCard.Dock = DockStyle.Top;
            entryCard.AutoSize = true;
            entryCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            entryCard.BackColor = Color.FromArgb(16, 22, 34);
            entryCard.Padding = new Padding(16, 12, 16, 14);
            tabInward.Controls.Add(entryCard);

            // Banner Title
            Label lblHeader = new Label();
            lblHeader.Text = "🚚  Record New Stock Inward Delivery / Supplier Purchase (+ IN)";
            lblHeader.Dock = DockStyle.Top;
            lblHeader.Height = 24;
            lblHeader.UseMnemonic = false;
            Theme.StyleLabel(lblHeader, Theme.Success, Theme.BoldFont);
            entryCard.Controls.Add(lblHeader);

            // Row 1: 4 Columns
            TableLayoutPanel row1Layout = new TableLayoutPanel();
            row1Layout.Dock = DockStyle.Top;
            row1Layout.AutoSize = true;
            row1Layout.ColumnCount = 4;
            row1Layout.RowCount = 2;
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            row1Layout.Padding = new Padding(0, 4, 0, 8);
            entryCard.Controls.Add(row1Layout);

            // Row 1: Labels
            Label lblMat = new Label { Text = "Raw Material *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblMat, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblMat, 0, 0);

            Label lblSupp = new Label { Text = "Supplier / Vendor", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblSupp, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblSupp, 1, 0);

            Label lblRef = new Label { Text = "Invoice / Ref No", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblRef, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblRef, 2, 0);

            Label lblDate = new Label { Text = "Delivery Date *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblDate, 3, 0);

            // Row 1: Controls
            comboInwardMaterial = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28, Margin = new Padding(0, 2, 8, 0) };
            comboInwardMaterial.BackColor = Theme.Secondary;
            comboInwardMaterial.ForeColor = Theme.TextLight;
            comboInwardMaterial.Font = Theme.MainFont;
            comboInwardMaterial.SelectedIndexChanged += ComboInwardMaterial_SelectedIndexChanged;
            row1Layout.Controls.Add(comboInwardMaterial, 0, 1);

            comboInwardSupplier = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28, Margin = new Padding(0, 2, 8, 0) };
            comboInwardSupplier.BackColor = Theme.Secondary;
            comboInwardSupplier.ForeColor = Theme.TextLight;
            comboInwardSupplier.Font = Theme.MainFont;
            row1Layout.Controls.Add(comboInwardSupplier, 1, 1);

            txtInwardRefNo = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 2, 8, 0) };
            Theme.StyleTextBox(txtInwardRefNo);
            row1Layout.Controls.Add(txtInwardRefNo, 2, 1);

            dtpInwardDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Font = Theme.MainFont, Height = 28, Margin = new Padding(0, 2, 0, 0) };
            row1Layout.Controls.Add(dtpInwardDate, 3, 1);

            // Row 2: 4 Columns (Qty, Unit Rate, Remarks, Action Button)
            TableLayoutPanel row2Layout = new TableLayoutPanel();
            row2Layout.Dock = DockStyle.Top;
            row2Layout.AutoSize = true;
            row2Layout.ColumnCount = 4;
            row2Layout.RowCount = 2;
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            row2Layout.Padding = new Padding(0, 2, 0, 0);
            entryCard.Controls.Add(row2Layout);

            // Row 2: Labels
            Label lblQ = new Label { Text = "Qty Received (+ IN) *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblQ, Theme.TextLight, Theme.BoldFont);
            row2Layout.Controls.Add(lblQ, 0, 0);

            Label lblRate = new Label { Text = "Unit Purchase Cost (Rs.) *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblRate, Theme.TextLight, Theme.BoldFont);
            row2Layout.Controls.Add(lblRate, 1, 0);

            Label lblRemT = new Label { Text = "Delivery Notes / Batch Remarks", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblRemT, Theme.TextMuted, Theme.BoldFont);
            row2Layout.Controls.Add(lblRemT, 2, 0);

            Label lblEmpty2 = new Label { Text = "", AutoSize = true, Dock = DockStyle.Fill };
            row2Layout.Controls.Add(lblEmpty2, 3, 0);

            // Row 2: Controls
            TableLayoutPanel qtyInBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Height = 30, Margin = new Padding(0, 2, 8, 0) };
            qtyInBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            qtyInBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            txtInwardQty = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 0, 4, 0) };
            Theme.StyleTextBox(txtInwardQty);
            lblInwardUnit = new Label { Dock = DockStyle.Fill, Text = "Kg", TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            Theme.StyleLabel(lblInwardUnit, Theme.Success, Theme.BoldFont);
            qtyInBox.Controls.Add(txtInwardQty, 0, 0);
            qtyInBox.Controls.Add(lblInwardUnit, 1, 0);
            row2Layout.Controls.Add(qtyInBox, 0, 1);

            txtInwardUnitCost = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 2, 8, 0) };
            Theme.StyleTextBox(txtInwardUnitCost);
            row2Layout.Controls.Add(txtInwardUnitCost, 1, 1);

            txtInwardRemarks = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 2, 10, 0) };
            Theme.StyleTextBox(txtInwardRemarks);
            row2Layout.Controls.Add(txtInwardRemarks, 2, 1);

            btnSaveInward = new Button { Text = "📥  RECORD INWARD", Dock = DockStyle.Fill, Height = 32, Margin = new Padding(0, 0, 0, 0), UseMnemonic = false };
            Theme.StyleSuccessButton(btnSaveInward);
            btnSaveInward.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSaveInward.Click += BtnSaveInward_Click;
            row2Layout.Controls.Add(btnSaveInward, 3, 1);

            // Order of controls in entryCard
            lblHeader.SendToBack();
            row1Layout.BringToFront();
            row2Layout.BringToFront();

            // 2. Grid Section Container
            Panel gridContainer = new Panel();
            gridContainer.Dock = DockStyle.Fill;
            gridContainer.Padding = new Padding(16, 8, 16, 10);
            tabInward.Controls.Add(gridContainer);

            Panel gridHeaderStrip = new Panel();
            gridHeaderStrip.Dock = DockStyle.Top;
            gridHeaderStrip.Height = 36;
            gridHeaderStrip.Padding = new Padding(0, 4, 0, 4);
            gridContainer.Controls.Add(gridHeaderStrip);

            Label lblGridTitle = new Label();
            lblGridTitle.Text = "🚚  Recent Raw Material Inward Receipts & Delivery History";
            lblGridTitle.Dock = DockStyle.Left;
            lblGridTitle.AutoSize = true;
            lblGridTitle.UseMnemonic = false;
            Theme.StyleLabel(lblGridTitle, Theme.TextLight, Theme.BoldFont);
            gridHeaderStrip.Controls.Add(lblGridTitle);

            FlowLayoutPanel rightStrip = new FlowLayoutPanel();
            rightStrip.Dock = DockStyle.Right;
            rightStrip.AutoSize = true;
            rightStrip.FlowDirection = FlowDirection.RightToLeft;
            gridHeaderStrip.Controls.Add(rightStrip);

            btnDeleteInwardEntry = new Button();
            btnDeleteInwardEntry.Text = "🗑️ Revert Selected";
            btnDeleteInwardEntry.Size = new Size(135, 28);
            btnDeleteInwardEntry.Margin = new Padding(8, 0, 0, 0);
            btnDeleteInwardEntry.UseMnemonic = false;
            Theme.StyleDangerButton(btnDeleteInwardEntry);
            btnDeleteInwardEntry.Click += BtnDeleteInwardEntry_Click;
            rightStrip.Controls.Add(btnDeleteInwardEntry);

            lblTodayInwardCost = new Label();
            lblTodayInwardCost.Text = "Today's Total Inward: Rs. 0.00";
            lblTodayInwardCost.AutoSize = true;
            lblTodayInwardCost.UseMnemonic = false;
            lblTodayInwardCost.Margin = new Padding(0, 4, 12, 0);
            Theme.StyleLabel(lblTodayInwardCost, Theme.Success, Theme.BoldFont);
            rightStrip.Controls.Add(lblTodayInwardCost);

            gridRecentInward = new DataGridView();
            gridRecentInward.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridRecentInward);
            gridRecentInward.ColumnHeadersHeight = 38;
            gridRecentInward.DataBindingComplete += GridRecentInward_DataBindingComplete;
            gridContainer.Controls.Add(gridRecentInward);

            // Docking order
            entryCard.SendToBack();
            gridContainer.BringToFront();
            gridHeaderStrip.SendToBack();
            gridRecentInward.BringToFront();
        }

        private void LoadInwardDropdowns()
        {
            try
            {
                if (comboInwardMaterial == null) return;
                comboInwardMaterial.Items.Clear();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code, Name, Unit, CurrentStock, UnitPrice FROM RawMaterials WHERE IsActive = 1 ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboInwardMaterial.Items.Add(new MaterialComboItem {
                                    Id = Convert.ToInt32(rdr["Id"]),
                                    Code = rdr["Code"].ToString(),
                                    Name = rdr["Name"].ToString(),
                                    Unit = rdr["Unit"].ToString(),
                                    CurrentStock = Convert.ToDecimal(rdr["CurrentStock"]),
                                    UnitPrice = Convert.ToDecimal(rdr["UnitPrice"])
                                });
                            }
                        }
                    }

                    comboInwardSupplier.Items.Clear();
                    comboInwardSupplier.Items.Add(new SupplierComboItem { Id = null, Name = "-- General / Local Market --" });
                    using (SqlCommand cmdSupp = new SqlCommand("SELECT Id, Name FROM Suppliers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdrSupp = cmdSupp.ExecuteReader())
                        {
                            while (rdrSupp.Read())
                            {
                                comboInwardSupplier.Items.Add(new SupplierComboItem {
                                    Id = Convert.ToInt32(rdrSupp["Id"]),
                                    Name = rdrSupp["Name"].ToString()
                                });
                            }
                        }
                    }
                }
                if (comboInwardMaterial.Items.Count > 0) comboInwardMaterial.SelectedIndex = 0;
                if (comboInwardSupplier.Items.Count > 0) comboInwardSupplier.SelectedIndex = 0;
            }
            catch { }
        }

        private void ComboInwardMaterial_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboInwardMaterial.SelectedItem is MaterialComboItem item)
            {
                lblInwardUnit.Text = item.Unit;
                txtInwardUnitCost.Text = item.UnitPrice.ToString("F2");
            }
        }

        private void BtnSaveInward_Click(object sender, EventArgs e)
        {
            if (comboInwardMaterial.SelectedItem == null)
            {
                MessageBox.Show("Please select a raw material.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtInwardQty.Text.Trim(), out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid positive quantity received.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtInwardQty.Focus();
                return;
            }

            if (!decimal.TryParse(txtInwardUnitCost.Text.Trim(), out decimal unitCost) || unitCost < 0)
            {
                MessageBox.Show("Please enter a valid unit cost.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtInwardUnitCost.Focus();
                return;
            }

            MaterialComboItem item = (MaterialComboItem)comboInwardMaterial.SelectedItem;
            SupplierComboItem suppItem = comboInwardSupplier.SelectedItem as SupplierComboItem;
            string refNo = txtInwardRefNo.Text.Trim();
            string remarks = txtInwardRemarks.Text.Trim();
            if (string.IsNullOrEmpty(remarks)) remarks = "Supplier stock inward receipt";
            DateTime inwardDate = dtpInwardDate.Value;

            bool success = DatabaseHelper.InwardRawMaterial(item.Id, qty, unitCost, suppItem?.Id, refNo, remarks, Session.Username ?? "Manager", inwardDate);
            if (success)
            {
                txtInwardQty.Clear();
                txtInwardRefNo.Clear();
                txtInwardRemarks.Clear();
                LoadInwardDropdowns();
                LoadRecentInwardData();
                MessageBox.Show($"Successfully inwarded {qty} {item.Unit} for '{item.Name}' at Rs. {unitCost:N2}/{item.Unit}.", "Inward Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to record inward delivery. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteInwardEntry_Click(object sender, EventArgs e)
        {
            if (gridRecentInward.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an inward entry to revert.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int moveId = Convert.ToInt32(gridRecentInward.SelectedRows[0].Cells["Id"].Value);
            string item = gridRecentInward.SelectedRows[0].Cells["Raw Material"].Value.ToString();
            string qty = gridRecentInward.SelectedRows[0].Cells["Qty Inward (+)"].Value.ToString();

            if (MessageBox.Show($"Are you sure you want to revert inward receipt of {qty} of '{item}'?", "Confirm Reversal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (DatabaseHelper.DeleteStockMovement(moveId))
                {
                    LoadInwardDropdowns();
                    LoadRecentInwardData();
                }
            }
        }

        private void LoadRecentInwardData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            sm.Id,
                            sm.TransactionDate as [Date & Time],
                            rm.Name as [Raw Material],
                            rm.Category as [Category],
                            sm.Quantity as [Qty Inward (+)],
                            rm.Unit as [Unit],
                            sm.UnitCost as [Unit Cost (Rs.)],
                            sm.TotalCost as [Total Cost (Rs.)],
                            ISNULL(s.Name, 'Local / Direct') as [Supplier],
                            ISNULL(sm.ReferenceNo, '-') as [Invoice / Ref No],
                            sm.CreatedBy as [Received By]
                        FROM StockMovements sm
                        JOIN RawMaterials rm ON sm.MaterialId = rm.Id
                        LEFT JOIN Suppliers s ON sm.SupplierId = s.Id
                        WHERE sm.TransactionType = 'IN_PURCHASE'
                        ORDER BY sm.TransactionDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridRecentInward.DataSource = dt;

                            if (gridRecentInward.Columns["Id"] != null) gridRecentInward.Columns["Id"].Visible = false;

                            decimal todayInward = 0;
                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Date & Time"] != DBNull.Value && Convert.ToDateTime(r["Date & Time"]).Date == DateTime.Today)
                                {
                                    if (r["Total Cost (Rs.)"] != DBNull.Value) todayInward += Convert.ToDecimal(r["Total Cost (Rs.)"]);
                                }
                            }
                            lblTodayInwardCost.Text = $"Today's Total Inward: Rs. {todayInward:N2}";
                        }
                    }
                }
            }
            catch { }
        }

        private void GridRecentInward_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridRecentInward.Columns["Date & Time"] != null)
                {
                    gridRecentInward.Columns["Date & Time"].FillWeight = 75;
                    gridRecentInward.Columns["Date & Time"].MinimumWidth = 135;
                    gridRecentInward.Columns["Date & Time"].DefaultCellStyle.Format = "dd MMM, hh:mm tt";
                }
                if (gridRecentInward.Columns["Raw Material"] != null)
                {
                    gridRecentInward.Columns["Raw Material"].FillWeight = 125;
                    gridRecentInward.Columns["Raw Material"].MinimumWidth = 160;
                }
                if (gridRecentInward.Columns["Category"] != null)
                {
                    gridRecentInward.Columns["Category"].FillWeight = 75;
                    gridRecentInward.Columns["Category"].MinimumWidth = 110;
                }
                if (gridRecentInward.Columns["Qty Inward (+)"] != null)
                {
                    gridRecentInward.Columns["Qty Inward (+)"].FillWeight = 60;
                    gridRecentInward.Columns["Qty Inward (+)"].MinimumWidth = 95;
                    gridRecentInward.Columns["Qty Inward (+)"].DefaultCellStyle.Format = "N3";
                    gridRecentInward.Columns["Qty Inward (+)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRecentInward.Columns["Unit"] != null)
                {
                    gridRecentInward.Columns["Unit"].FillWeight = 40;
                    gridRecentInward.Columns["Unit"].MinimumWidth = 60;
                    gridRecentInward.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridRecentInward.Columns["Unit Cost (Rs.)"] != null)
                {
                    gridRecentInward.Columns["Unit Cost (Rs.)"].FillWeight = 60;
                    gridRecentInward.Columns["Unit Cost (Rs.)"].MinimumWidth = 95;
                    gridRecentInward.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridRecentInward.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRecentInward.Columns["Total Cost (Rs.)"] != null)
                {
                    gridRecentInward.Columns["Total Cost (Rs.)"].FillWeight = 65;
                    gridRecentInward.Columns["Total Cost (Rs.)"].MinimumWidth = 105;
                    gridRecentInward.Columns["Total Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridRecentInward.Columns["Total Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridRecentInward.Columns["Supplier"] != null)
                {
                    gridRecentInward.Columns["Supplier"].FillWeight = 85;
                    gridRecentInward.Columns["Supplier"].MinimumWidth = 130;
                }
                if (gridRecentInward.Columns["Invoice / Ref No"] != null)
                {
                    gridRecentInward.Columns["Invoice / Ref No"].FillWeight = 75;
                    gridRecentInward.Columns["Invoice / Ref No"].MinimumWidth = 125;
                }
                if (gridRecentInward.Columns["Received By"] != null)
                {
                    gridRecentInward.Columns["Received By"].FillWeight = 55;
                    gridRecentInward.Columns["Received By"].MinimumWidth = 85;
                }
            }
            catch { }
        }
        #endregion

        #region TAB 4: CLOSE KITCHEN (RETURN UNUSED STOCK)
        private void BuildKitchenCloseTab()
        {
            // 1. Top Input Card
            Panel entryCard = new Panel();
            entryCard.Dock = DockStyle.Top;
            entryCard.AutoSize = true;
            entryCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            entryCard.BackColor = Color.FromArgb(16, 22, 34);
            entryCard.Padding = new Padding(16, 12, 16, 14);
            tabKitchenClose.Controls.Add(entryCard);

            // Header Banner
            Label lblCardTitle = new Label();
            lblCardTitle.Text = "🌙  Close Kitchen — Return Unused Ingredients to Main Stock (+ IN)";
            lblCardTitle.Dock = DockStyle.Top;
            lblCardTitle.Height = 24;
            lblCardTitle.UseMnemonic = false;
            Theme.StyleLabel(lblCardTitle, Theme.Accent, Theme.BoldFont);
            entryCard.Controls.Add(lblCardTitle);

            // Row 1: 4 Equal Grid Columns for Inputs
            TableLayoutPanel row1Layout = new TableLayoutPanel();
            row1Layout.Dock = DockStyle.Top;
            row1Layout.AutoSize = true;
            row1Layout.ColumnCount = 4;
            row1Layout.RowCount = 2;
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            row1Layout.Padding = new Padding(0, 4, 0, 8);
            entryCard.Controls.Add(row1Layout);

            // Row 1 Labels
            Label lblMat = new Label { Text = "Raw Material / Ingredient *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblMat, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblMat, 0, 0);

            Label lblQty = new Label { Text = "Unused Qty Returned (+ IN) *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblQty, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblQty, 1, 0);

            Label lblReason = new Label { Text = "Return Condition / Preset *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblReason, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblReason, 2, 0);

            Label lblDate = new Label { Text = "Closing Date *", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblDate, Theme.TextLight, Theme.BoldFont);
            row1Layout.Controls.Add(lblDate, 3, 0);

            // Row 1 Controls
            Panel matBox = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 2, 8, 0) };
            comboCloseMaterial = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            comboCloseMaterial.BackColor = Theme.Secondary;
            comboCloseMaterial.ForeColor = Theme.TextLight;
            comboCloseMaterial.Font = Theme.MainFont;
            comboCloseMaterial.SelectedIndexChanged += ComboCloseMaterial_SelectedIndexChanged;
            matBox.Controls.Add(comboCloseMaterial);

            lblCloseIssuedBadge = new Label { Dock = DockStyle.Bottom, Text = "Issued today: 0.00 | In-Stock: 0.00", AutoSize = true, Margin = new Padding(0, 3, 0, 0), UseMnemonic = false };
            Theme.StyleLabel(lblCloseIssuedBadge, Theme.Success, new Font("Segoe UI", 8.25F, FontStyle.Regular));
            matBox.Controls.Add(lblCloseIssuedBadge);
            row1Layout.Controls.Add(matBox, 0, 1);

            TableLayoutPanel qtyBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Height = 30, Margin = new Padding(0, 2, 8, 0) };
            qtyBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            qtyBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            txtCloseReturnQty = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 0, 4, 0) };
            Theme.StyleTextBox(txtCloseReturnQty);
            lblCloseUnit = new Label { Dock = DockStyle.Fill, Text = "Kg", TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            Theme.StyleLabel(lblCloseUnit, Theme.Success, Theme.BoldFont);
            qtyBox.Controls.Add(txtCloseReturnQty, 0, 0);
            qtyBox.Controls.Add(lblCloseUnit, 1, 0);
            row1Layout.Controls.Add(qtyBox, 1, 1);

            comboCloseReason = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28, Margin = new Padding(0, 2, 8, 0) };
            comboCloseReason.BackColor = Theme.Secondary;
            comboCloseReason.ForeColor = Theme.TextLight;
            comboCloseReason.Font = Theme.MainFont;
            comboCloseReason.Items.AddRange(new string[] { 
                "Returned Unused to Main Chiller / Store", 
                "End of Day Shift Leftover", 
                "Unopened / Sealed Pack Returned", 
                "Prep Leftover (Stored for Tomorrow)", 
                "General Kitchen Return" 
            });
            comboCloseReason.SelectedIndex = 0;
            row1Layout.Controls.Add(comboCloseReason, 2, 1);

            dtpCloseDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Font = Theme.MainFont, Height = 28, Margin = new Padding(0, 2, 0, 0) };
            row1Layout.Controls.Add(dtpCloseDate, 3, 1);

            // Row 2: Full Width Notes + Action Button
            TableLayoutPanel row2Layout = new TableLayoutPanel();
            row2Layout.Dock = DockStyle.Top;
            row2Layout.AutoSize = true;
            row2Layout.ColumnCount = 2;
            row2Layout.RowCount = 2;
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76f));
            row2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            row2Layout.Padding = new Padding(0, 2, 0, 0);
            entryCard.Controls.Add(row2Layout);

            Label lblRemTag = new Label { Text = "Closing Notes / Storage Location Remarks", AutoSize = true, Dock = DockStyle.Fill, UseMnemonic = false };
            Theme.StyleLabel(lblRemTag, Theme.TextMuted, Theme.BoldFont);
            row2Layout.Controls.Add(lblRemTag, 0, 0);

            txtCloseRemarks = new TextBox { Dock = DockStyle.Fill, Height = 26, Margin = new Padding(0, 2, 10, 0) };
            Theme.StyleTextBox(txtCloseRemarks);
            row2Layout.Controls.Add(txtCloseRemarks, 0, 1);

            btnSaveKitchenClose = new Button { Text = "🔄  RETURN TO STOCK (+ IN)", Dock = DockStyle.Fill, Height = 32, Margin = new Padding(0, 0, 0, 0), UseMnemonic = false };
            Theme.StyleSuccessButton(btnSaveKitchenClose);
            btnSaveKitchenClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSaveKitchenClose.Click += BtnSaveKitchenClose_Click;
            row2Layout.Controls.Add(btnSaveKitchenClose, 1, 1);

            // Docking order inside entryCard
            lblCardTitle.SendToBack();
            row1Layout.BringToFront();
            row2Layout.BringToFront();

            // 2. Dual-Grid Container (Issued Issues on Top + Recorded Returns on Bottom)
            Panel gridContainer = new Panel();
            gridContainer.Dock = DockStyle.Fill;
            gridContainer.Padding = new Padding(16, 4, 16, 10);
            tabKitchenClose.Controls.Add(gridContainer);

            SplitContainer splitClose = new SplitContainer();
            splitClose.Dock = DockStyle.Fill;
            splitClose.Orientation = Orientation.Horizontal;
            splitClose.SplitterDistance = 190;
            splitClose.SplitterWidth = 6;
            splitClose.BackColor = Theme.Primary;
            gridContainer.Controls.Add(splitClose);

            // --- Panel 1 (Top): Active Kitchen Issues (Click-to-Return) ---
            Panel pnlIssuedHeader = new Panel();
            pnlIssuedHeader.Dock = DockStyle.Top;
            pnlIssuedHeader.Height = 32;
            pnlIssuedHeader.Padding = new Padding(0, 4, 0, 4);
            splitClose.Panel1.Controls.Add(pnlIssuedHeader);

            Label lblIssuedTitle = new Label();
            lblIssuedTitle.Text = "📋  Today's Active Kitchen Issues (👇 Click any row below to quickly select and return):";
            lblIssuedTitle.Dock = DockStyle.Left;
            lblIssuedTitle.AutoSize = true;
            lblIssuedTitle.UseMnemonic = false;
            Theme.StyleLabel(lblIssuedTitle, Theme.Accent, Theme.BoldFont);
            pnlIssuedHeader.Controls.Add(lblIssuedTitle);

            gridTodayIssued = new DataGridView();
            gridTodayIssued.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridTodayIssued);
            gridTodayIssued.ColumnHeadersHeight = 34;
            gridTodayIssued.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridTodayIssued.MultiSelect = false;
            gridTodayIssued.CellClick += GridTodayIssued_CellClick;
            gridTodayIssued.DataBindingComplete += GridTodayIssued_DataBindingComplete;
            splitClose.Panel1.Controls.Add(gridTodayIssued);

            pnlIssuedHeader.SendToBack();
            gridTodayIssued.BringToFront();

            // --- Panel 2 (Bottom): Recorded Returns & Reconciled Stock ---
            Panel pnlReturnsHeader = new Panel();
            pnlReturnsHeader.Dock = DockStyle.Top;
            pnlReturnsHeader.Height = 34;
            pnlReturnsHeader.Padding = new Padding(0, 4, 0, 4);
            splitClose.Panel2.Controls.Add(pnlReturnsHeader);

            Label lblGridTitle = new Label();
            lblGridTitle.Text = "🌙  Today's Recorded Kitchen Returns & Reclaimed Stock";
            lblGridTitle.Dock = DockStyle.Left;
            lblGridTitle.AutoSize = true;
            lblGridTitle.UseMnemonic = false;
            Theme.StyleLabel(lblGridTitle, Theme.TextLight, Theme.BoldFont);
            pnlReturnsHeader.Controls.Add(lblGridTitle);

            FlowLayoutPanel rightStrip = new FlowLayoutPanel();
            rightStrip.Dock = DockStyle.Right;
            rightStrip.AutoSize = true;
            rightStrip.FlowDirection = FlowDirection.RightToLeft;
            pnlReturnsHeader.Controls.Add(rightStrip);

            btnDeleteReturnEntry = new Button();
            btnDeleteReturnEntry.Text = "🗑️ Revert Selected";
            btnDeleteReturnEntry.Size = new Size(135, 26);
            btnDeleteReturnEntry.Margin = new Padding(8, 0, 0, 0);
            btnDeleteReturnEntry.UseMnemonic = false;
            Theme.StyleDangerButton(btnDeleteReturnEntry);
            btnDeleteReturnEntry.Click += BtnDeleteReturnEntry_Click;
            rightStrip.Controls.Add(btnDeleteReturnEntry);

            lblTodayNetUsageCost = new Label();
            lblTodayNetUsageCost.Text = "Net Today Consumption: Rs. 0.00";
            lblTodayNetUsageCost.AutoSize = true;
            lblTodayNetUsageCost.UseMnemonic = false;
            lblTodayNetUsageCost.Margin = new Padding(0, 4, 12, 0);
            Theme.StyleLabel(lblTodayNetUsageCost, Theme.Accent, Theme.BoldFont);
            rightStrip.Controls.Add(lblTodayNetUsageCost);

            lblTodayReturnCost = new Label();
            lblTodayReturnCost.Text = "Today's Returns: Rs. 0.00";
            lblTodayReturnCost.AutoSize = true;
            lblTodayReturnCost.UseMnemonic = false;
            lblTodayReturnCost.Margin = new Padding(0, 4, 12, 0);
            Theme.StyleLabel(lblTodayReturnCost, Theme.Success, Theme.BoldFont);
            rightStrip.Controls.Add(lblTodayReturnCost);

            gridTodayReturns = new DataGridView();
            gridTodayReturns.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridTodayReturns);
            gridTodayReturns.ColumnHeadersHeight = 34;
            gridTodayReturns.DataBindingComplete += GridTodayReturns_DataBindingComplete;
            splitClose.Panel2.Controls.Add(gridTodayReturns);

            pnlReturnsHeader.SendToBack();
            gridTodayReturns.BringToFront();

            // Docking order
            entryCard.SendToBack();
            gridContainer.BringToFront();
        }

        private void LoadTodayIssuedList()
        {
            try
            {
                if (gridTodayIssued == null) return;
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            rm.Id as [MaterialId],
                            rm.Name as [Raw Material],
                            rm.Category as [Category],
                            ISNULL(SUM(sm.Quantity), 0) as [Issued Today],
                            ISNULL((
                                SELECT SUM(ret.Quantity) 
                                FROM StockMovements ret 
                                WHERE ret.MaterialId = rm.Id 
                                  AND ret.TransactionType = 'IN_RETURN' 
                                  AND CAST(ret.TransactionDate AS DATE) = CAST(GETDATE() AS DATE)
                            ), 0) as [Returned Qty],
                            (
                                ISNULL(SUM(sm.Quantity), 0) - 
                                ISNULL((
                                    SELECT SUM(ret.Quantity) 
                                    FROM StockMovements ret 
                                    WHERE ret.MaterialId = rm.Id 
                                      AND ret.TransactionType = 'IN_RETURN' 
                                      AND CAST(ret.TransactionDate AS DATE) = CAST(GETDATE() AS DATE)
                                ), 0)
                            ) as [Available to Return],
                            rm.Unit as [Unit],
                            rm.UnitPrice as [Rate (Rs.)],
                            (ISNULL(SUM(sm.Quantity), 0) * rm.UnitPrice) as [Total Issued Cost (Rs.)],
                            MAX(sm.Department) as [Kitchen Section]
                        FROM StockMovements sm
                        JOIN RawMaterials rm ON sm.MaterialId = rm.Id
                        WHERE sm.TransactionType = 'OUT_USAGE'
                          AND CAST(sm.TransactionDate AS DATE) = CAST(GETDATE() AS DATE)
                        GROUP BY rm.Id, rm.Name, rm.Category, rm.Unit, rm.UnitPrice
                        ORDER BY [Available to Return] DESC, rm.Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridTodayIssued.DataSource = dt;
                            if (gridTodayIssued.Columns["MaterialId"] != null) gridTodayIssued.Columns["MaterialId"].Visible = false;
                        }
                    }
                }
            }
            catch { }
        }

        private void GridTodayIssued_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridTodayIssued.Rows[e.RowIndex].Cells["MaterialId"]?.Value != null)
            {
                int matId = Convert.ToInt32(gridTodayIssued.Rows[e.RowIndex].Cells["MaterialId"].Value);
                for (int i = 0; i < comboCloseMaterial.Items.Count; i++)
                {
                    if (comboCloseMaterial.Items[i] is MaterialComboItem m && m.Id == matId)
                    {
                        comboCloseMaterial.SelectedIndex = i;
                        break;
                    }
                }
                txtCloseReturnQty.Focus();
                txtCloseReturnQty.SelectAll();
            }
        }

        private void GridTodayIssued_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridTodayIssued.Columns["Raw Material"] != null) { gridTodayIssued.Columns["Raw Material"].FillWeight = 130; gridTodayIssued.Columns["Raw Material"].MinimumWidth = 160; }
                if (gridTodayIssued.Columns["Category"] != null) { gridTodayIssued.Columns["Category"].FillWeight = 80; gridTodayIssued.Columns["Category"].MinimumWidth = 110; }
                if (gridTodayIssued.Columns["Issued Today"] != null) { gridTodayIssued.Columns["Issued Today"].FillWeight = 65; gridTodayIssued.Columns["Issued Today"].DefaultCellStyle.Format = "N3"; gridTodayIssued.Columns["Issued Today"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                if (gridTodayIssued.Columns["Returned Qty"] != null) { gridTodayIssued.Columns["Returned Qty"].FillWeight = 65; gridTodayIssued.Columns["Returned Qty"].DefaultCellStyle.Format = "N3"; gridTodayIssued.Columns["Returned Qty"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                if (gridTodayIssued.Columns["Available to Return"] != null) { 
                    gridTodayIssued.Columns["Available to Return"].FillWeight = 75; 
                    gridTodayIssued.Columns["Available to Return"].DefaultCellStyle.Format = "N3"; 
                    gridTodayIssued.Columns["Available to Return"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    gridTodayIssued.Columns["Available to Return"].DefaultCellStyle.Font = Theme.BoldFont;
                    gridTodayIssued.Columns["Available to Return"].DefaultCellStyle.ForeColor = Theme.Accent;
                }
                if (gridTodayIssued.Columns["Unit"] != null) { gridTodayIssued.Columns["Unit"].FillWeight = 40; gridTodayIssued.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                if (gridTodayIssued.Columns["Rate (Rs.)"] != null) { gridTodayIssued.Columns["Rate (Rs.)"].FillWeight = 60; gridTodayIssued.Columns["Rate (Rs.)"].DefaultCellStyle.Format = "N2"; gridTodayIssued.Columns["Rate (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                if (gridTodayIssued.Columns["Total Issued Cost (Rs.)"] != null) { gridTodayIssued.Columns["Total Issued Cost (Rs.)"].FillWeight = 70; gridTodayIssued.Columns["Total Issued Cost (Rs.)"].DefaultCellStyle.Format = "N2"; gridTodayIssued.Columns["Total Issued Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                if (gridTodayIssued.Columns["Kitchen Section"] != null) { gridTodayIssued.Columns["Kitchen Section"].FillWeight = 85; gridTodayIssued.Columns["Kitchen Section"].MinimumWidth = 120; }
            }
            catch { }
        }

        private void LoadCloseDropdowns()
        {
            try
            {
                if (comboCloseMaterial == null) return;
                comboCloseMaterial.Items.Clear();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code, Name, Unit, CurrentStock, UnitPrice FROM RawMaterials WHERE IsActive = 1 ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboCloseMaterial.Items.Add(new MaterialComboItem {
                                    Id = Convert.ToInt32(rdr["Id"]),
                                    Code = rdr["Code"].ToString(),
                                    Name = rdr["Name"].ToString(),
                                    Unit = rdr["Unit"].ToString(),
                                    CurrentStock = Convert.ToDecimal(rdr["CurrentStock"]),
                                    UnitPrice = Convert.ToDecimal(rdr["UnitPrice"])
                                });
                            }
                        }
                    }
                }
                if (comboCloseMaterial.Items.Count > 0) comboCloseMaterial.SelectedIndex = 0;
            }
            catch { }
        }

        private decimal GetTodayIssuedQty(int materialId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(Quantity), 0) 
                        FROM StockMovements 
                        WHERE MaterialId = @mId 
                          AND TransactionType = 'OUT_USAGE' 
                          AND CAST(TransactionDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        cmd.Parameters.AddWithValue("@mId", materialId);
                        object res = cmd.ExecuteScalar();
                        return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
                    }
                }
            }
            catch { return 0; }
        }

        private decimal GetTodayReturnedQty(int materialId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(Quantity), 0) 
                        FROM StockMovements 
                        WHERE MaterialId = @mId 
                          AND TransactionType = 'IN_RETURN' 
                          AND CAST(TransactionDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
                    {
                        cmd.Parameters.AddWithValue("@mId", materialId);
                        object res = cmd.ExecuteScalar();
                        return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
                    }
                }
            }
            catch { return 0; }
        }

        private void ComboCloseMaterial_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboCloseMaterial.SelectedItem is MaterialComboItem item)
            {
                lblCloseUnit.Text = item.Unit;
                decimal issuedToday = GetTodayIssuedQty(item.Id);
                decimal returnedToday = GetTodayReturnedQty(item.Id);
                decimal maxReturnable = Math.Max(0, issuedToday - returnedToday);
                lblCloseIssuedBadge.Text = $"Issued Today: {issuedToday:N2} {item.Unit}  |  Already Returned: {returnedToday:N2} {item.Unit}  |  Max Returnable: {maxReturnable:N2} {item.Unit}";
            }
        }

        private void BtnSaveKitchenClose_Click(object sender, EventArgs e)
        {
            if (comboCloseMaterial.SelectedItem == null)
            {
                MessageBox.Show("Please select a raw material to return.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtCloseReturnQty.Text.Trim(), out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid positive return quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCloseReturnQty.Focus();
                return;
            }

            MaterialComboItem item = (MaterialComboItem)comboCloseMaterial.SelectedItem;
            decimal issuedToday = GetTodayIssuedQty(item.Id);
            decimal alreadyReturned = GetTodayReturnedQty(item.Id);
            decimal maxReturnable = Math.Max(0, issuedToday - alreadyReturned);

            if (qty > maxReturnable)
            {
                MessageBox.Show(
                    $"Cannot return {qty:N2} {item.Unit} of '{item.Name}'.\n\nYou cannot return more than what was actually issued to the kitchen today.\n\n• Total Issued Today: {issuedToday:N2} {item.Unit}\n• Already Returned Today: {alreadyReturned:N2} {item.Unit}\n• Maximum Returnable: {maxReturnable:N2} {item.Unit}",
                    "Return Limit Exceeded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                txtCloseReturnQty.Focus();
                txtCloseReturnQty.SelectAll();
                return;
            }

            string reason = comboCloseReason.SelectedItem?.ToString() ?? "Kitchen Evening Return to Stock";
            string notes = txtCloseRemarks.Text.Trim();
            string combinedRemarks = string.IsNullOrEmpty(notes) ? reason : $"{reason} - {notes}";
            DateTime closeDate = dtpCloseDate.Value;

            bool success = DatabaseHelper.ReturnKitchenRawMaterial(item.Id, qty, combinedRemarks, Session.Username ?? "Kitchen Staff", closeDate);
            if (success)
            {
                txtCloseReturnQty.Clear();
                txtCloseRemarks.Clear();
                LoadCloseDropdowns();
                LoadTodayIssuedList();
                LoadTodayReturnsData();
                LoadRegisterData();
                MessageBox.Show($"Successfully returned {qty:N2} {item.Unit} of '{item.Name}' back to main stock.\n\nNew Stock Balance: {(item.CurrentStock + qty):N2} {item.Unit}", "Kitchen Stock Returned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to record kitchen return. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteReturnEntry_Click(object sender, EventArgs e)
        {
            if (gridTodayReturns.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a return log entry to revert.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int moveId = Convert.ToInt32(gridTodayReturns.SelectedRows[0].Cells["Id"].Value);
            string item = gridTodayReturns.SelectedRows[0].Cells["Raw Material"].Value.ToString();
            string qty = gridTodayReturns.SelectedRows[0].Cells["Qty Returned (+)"].Value.ToString();

            if (MessageBox.Show($"Are you sure you want to revert this return of {qty} for '{item}'?", "Confirm Reversal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (DatabaseHelper.DeleteStockMovement(moveId))
                {
                    LoadCloseDropdowns();
                    LoadTodayIssuedList();
                    LoadTodayReturnsData();
                    LoadRegisterData();
                }
            }
        }

        private void LoadTodayReturnsData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            sm.Id,
                            sm.TransactionDate as [Date & Time],
                            rm.Name as [Raw Material],
                            rm.Category as [Category],
                            sm.Quantity as [Qty Returned (+)],
                            rm.Unit as [Unit],
                            sm.UnitCost as [Rate (Rs.)],
                            sm.TotalCost as [Cost (Rs.)],
                            sm.Department as [Section],
                            sm.Remarks as [Return Reason / Notes],
                            sm.CreatedBy as [Logged By]
                        FROM StockMovements sm
                        JOIN RawMaterials rm ON sm.MaterialId = rm.Id
                        WHERE sm.TransactionType = 'IN_RETURN'
                          AND CAST(sm.TransactionDate AS DATE) = CAST(GETDATE() AS DATE)
                        ORDER BY sm.TransactionDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridTodayReturns.DataSource = dt;
                            if (gridTodayReturns.Columns["Id"] != null) gridTodayReturns.Columns["Id"].Visible = false;

                            decimal todayReturnCost = 0;
                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Cost (Rs.)"] != DBNull.Value) todayReturnCost += Convert.ToDecimal(r["Cost (Rs.)"]);
                            }
                            lblTodayReturnCost.Text = $"Today's Total Returns: Rs. {todayReturnCost:N2}";

                            // Calculate today's net consumption cost
                            decimal todayUsageCost = 0;
                            using (SqlCommand cmdUsage = new SqlCommand(@"
                                SELECT ISNULL(SUM(TotalCost), 0) 
                                FROM StockMovements 
                                WHERE TransactionType = 'OUT_USAGE' 
                                  AND CAST(TransactionDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
                            {
                                object resU = cmdUsage.ExecuteScalar();
                                if (resU != null && resU != DBNull.Value) todayUsageCost = Convert.ToDecimal(resU);
                            }
                            decimal netCost = Math.Max(0, todayUsageCost - todayReturnCost);
                            lblTodayNetUsageCost.Text = $"Net Today Consumption: Rs. {netCost:N2}";
                        }
                    }
                }
            }
            catch { }
        }

        private void GridTodayReturns_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridTodayReturns.Columns["Date & Time"] != null)
                {
                    gridTodayReturns.Columns["Date & Time"].FillWeight = 75;
                    gridTodayReturns.Columns["Date & Time"].MinimumWidth = 135;
                    gridTodayReturns.Columns["Date & Time"].DefaultCellStyle.Format = "dd MMM, hh:mm tt";
                }
                if (gridTodayReturns.Columns["Raw Material"] != null)
                {
                    gridTodayReturns.Columns["Raw Material"].FillWeight = 125;
                    gridTodayReturns.Columns["Raw Material"].MinimumWidth = 160;
                }
                if (gridTodayReturns.Columns["Category"] != null)
                {
                    gridTodayReturns.Columns["Category"].FillWeight = 75;
                    gridTodayReturns.Columns["Category"].MinimumWidth = 110;
                }
                if (gridTodayReturns.Columns["Qty Returned (+)"] != null)
                {
                    gridTodayReturns.Columns["Qty Returned (+)"].FillWeight = 65;
                    gridTodayReturns.Columns["Qty Returned (+)"].MinimumWidth = 95;
                    gridTodayReturns.Columns["Qty Returned (+)"].DefaultCellStyle.Format = "N3";
                    gridTodayReturns.Columns["Qty Returned (+)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayReturns.Columns["Unit"] != null)
                {
                    gridTodayReturns.Columns["Unit"].FillWeight = 40;
                    gridTodayReturns.Columns["Unit"].MinimumWidth = 60;
                    gridTodayReturns.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridTodayReturns.Columns["Rate (Rs.)"] != null)
                {
                    gridTodayReturns.Columns["Rate (Rs.)"].FillWeight = 60;
                    gridTodayReturns.Columns["Rate (Rs.)"].MinimumWidth = 95;
                    gridTodayReturns.Columns["Rate (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridTodayReturns.Columns["Rate (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayReturns.Columns["Cost (Rs.)"] != null)
                {
                    gridTodayReturns.Columns["Cost (Rs.)"].FillWeight = 65;
                    gridTodayReturns.Columns["Cost (Rs.)"].MinimumWidth = 105;
                    gridTodayReturns.Columns["Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridTodayReturns.Columns["Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridTodayReturns.Columns["Section"] != null)
                {
                    gridTodayReturns.Columns["Section"].FillWeight = 75;
                    gridTodayReturns.Columns["Section"].MinimumWidth = 120;
                }
                if (gridTodayReturns.Columns["Return Reason / Notes"] != null)
                {
                    gridTodayReturns.Columns["Return Reason / Notes"].FillWeight = 110;
                    gridTodayReturns.Columns["Return Reason / Notes"].MinimumWidth = 150;
                }
                if (gridTodayReturns.Columns["Logged By"] != null)
                {
                    gridTodayReturns.Columns["Logged By"].FillWeight = 55;
                    gridTodayReturns.Columns["Logged By"].MinimumWidth = 85;
                }
            }
            catch { }
        }
        #endregion

        #region TAB 5: MOVEMENT AUDIT LEDGER
        private void BuildLedgerTab()
        {
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Dock = DockStyle.Top;
            filterBar.AutoSize = true;
            filterBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            filterBar.BackColor = Theme.Primary;
            filterBar.Padding = new Padding(12, 8, 12, 8);
            filterBar.WrapContents = true;
            filterBar.Margin = new Padding(0);
            tabLedger.Controls.Add(filterBar);

            Label lblFrom = new Label { Text = "From:", AutoSize = true, Margin = new Padding(4, 7, 2, 4), UseMnemonic = false };
            Theme.StyleLabel(lblFrom, Theme.TextLight, Theme.BoldFont);
            filterBar.Controls.Add(lblFrom);

            dtpLedgerFrom = new DateTimePicker { Size = new Size(115, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7), Font = Theme.MainFont, Margin = new Padding(2, 4, 8, 4) };
            filterBar.Controls.Add(dtpLedgerFrom);

            Label lblTo = new Label { Text = "To:", AutoSize = true, Margin = new Padding(4, 7, 2, 4), UseMnemonic = false };
            Theme.StyleLabel(lblTo, Theme.TextLight, Theme.BoldFont);
            filterBar.Controls.Add(lblTo);

            dtpLedgerTo = new DateTimePicker { Size = new Size(115, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = Theme.MainFont, Margin = new Padding(2, 4, 8, 4) };
            filterBar.Controls.Add(dtpLedgerTo);

            comboLedgerType = new ComboBox();
            comboLedgerType.Size = new Size(150, 28);
            comboLedgerType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLedgerType.BackColor = Theme.Secondary;
            comboLedgerType.ForeColor = Theme.TextLight;
            comboLedgerType.Font = Theme.MainFont;
            comboLedgerType.Margin = new Padding(4, 4, 8, 4);
            comboLedgerType.Items.AddRange(new string[] { "All Movements", "IN_PURCHASE (+)", "OUT_USAGE (-)", "IN_RETURN (+)", "OUT_WASTAGE (-)", "ADJUSTMENT" });
            comboLedgerType.SelectedIndex = 0;
            filterBar.Controls.Add(comboLedgerType);

            txtLedgerSearch = new TextBox();
            txtLedgerSearch.Size = new Size(160, 28);
            txtLedgerSearch.Font = Theme.MainFont;
            txtLedgerSearch.Margin = new Padding(4, 4, 8, 4);
            Theme.StyleTextBox(txtLedgerSearch);
            filterBar.Controls.Add(txtLedgerSearch);

            btnFilterLedger = new Button();
            btnFilterLedger.Text = "🔍 Search";
            btnFilterLedger.Size = new Size(95, 30);
            btnFilterLedger.Margin = new Padding(4, 3, 6, 3);
            btnFilterLedger.UseMnemonic = false;
            Theme.StylePrimaryButton(btnFilterLedger);
            btnFilterLedger.Click += (s, e) => LoadLedgerData();
            filterBar.Controls.Add(btnFilterLedger);

            Button btnExport = new Button();
            btnExport.Text = "📊 Export CSV";
            btnExport.Size = new Size(115, 30);
            btnExport.Margin = new Padding(4, 3, 6, 3);
            btnExport.UseMnemonic = false;
            Theme.StyleSuccessButton(btnExport);
            btnExport.Click += (s, e) => ExportLedger();
            filterBar.Controls.Add(btnExport);

            gridLedger = new DataGridView();
            gridLedger.Dock = DockStyle.Fill;
            Theme.StyleGrid(gridLedger);
            gridLedger.ColumnHeadersHeight = 38;
            gridLedger.DataBindingComplete += GridLedger_DataBindingComplete;
            tabLedger.Controls.Add(gridLedger);

            // Docking Order
            filterBar.SendToBack();
            gridLedger.BringToFront();
        }

        private void LoadLedgerData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            sm.Id,
                            sm.TransactionDate as [Date & Time],
                            rm.Code as [Item Code],
                            rm.Name as [Raw Material],
                            sm.TransactionType as [Movement Type],
                            sm.Quantity as [Quantity],
                            rm.Unit as [Unit],
                            sm.UnitCost as [Unit Cost (Rs.)],
                            sm.TotalCost as [Total Cost (Rs.)],
                            ISNULL(sm.Department, ISNULL(s.Name, 'General')) as [Dept / Source],
                            ISNULL(sm.ReferenceNo, '-') as [Ref / Invoice No],
                            ISNULL(sm.Remarks, '-') as [Remarks],
                            sm.CreatedBy as [Logged By]
                        FROM StockMovements sm
                        JOIN RawMaterials rm ON sm.MaterialId = rm.Id
                        LEFT JOIN Suppliers s ON sm.SupplierId = s.Id
                        WHERE CAST(sm.TransactionDate AS DATE) BETWEEN @from AND @to";

                    if (comboLedgerType.SelectedIndex > 0)
                    {
                        string sel = comboLedgerType.SelectedItem.ToString();
                        string typeKey = sel.Split(' ')[0];
                        query += " AND sm.TransactionType = @type";
                    }

                    if (!string.IsNullOrWhiteSpace(txtLedgerSearch.Text))
                    {
                        query += " AND (rm.Code LIKE @search OR rm.Name LIKE @search OR sm.Remarks LIKE @search)";
                    }

                    query += " ORDER BY sm.TransactionDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", dtpLedgerFrom.Value.Date);
                        cmd.Parameters.AddWithValue("@to", dtpLedgerTo.Value.Date);

                        if (comboLedgerType.SelectedIndex > 0)
                        {
                            string sel = comboLedgerType.SelectedItem.ToString();
                            string typeKey = sel.Split(' ')[0];
                            cmd.Parameters.AddWithValue("@type", typeKey);
                        }

                        cmd.Parameters.AddWithValue("@search", $"%{txtLedgerSearch.Text.Trim()}%");

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridLedger.DataSource = dt;
                            if (gridLedger.Columns["Id"] != null) gridLedger.Columns["Id"].Visible = false;
                        }
                    }
                }
            }
            catch { }
        }

        private void GridLedger_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                if (gridLedger.Columns["Date & Time"] != null)
                {
                    gridLedger.Columns["Date & Time"].FillWeight = 70;
                    gridLedger.Columns["Date & Time"].MinimumWidth = 130;
                    gridLedger.Columns["Date & Time"].DefaultCellStyle.Format = "dd MMM, hh:mm tt";
                }
                if (gridLedger.Columns["Item Code"] != null)
                {
                    gridLedger.Columns["Item Code"].FillWeight = 50;
                    gridLedger.Columns["Item Code"].MinimumWidth = 85;
                }
                if (gridLedger.Columns["Raw Material"] != null)
                {
                    gridLedger.Columns["Raw Material"].FillWeight = 120;
                    gridLedger.Columns["Raw Material"].MinimumWidth = 150;
                }
                if (gridLedger.Columns["Movement Type"] != null)
                {
                    gridLedger.Columns["Movement Type"].FillWeight = 65;
                    gridLedger.Columns["Movement Type"].MinimumWidth = 110;
                    gridLedger.Columns["Movement Type"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridLedger.Columns["Quantity"] != null)
                {
                    gridLedger.Columns["Quantity"].FillWeight = 55;
                    gridLedger.Columns["Quantity"].MinimumWidth = 85;
                    gridLedger.Columns["Quantity"].DefaultCellStyle.Format = "N3";
                    gridLedger.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridLedger.Columns["Unit"] != null)
                {
                    gridLedger.Columns["Unit"].FillWeight = 35;
                    gridLedger.Columns["Unit"].MinimumWidth = 55;
                    gridLedger.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (gridLedger.Columns["Unit Cost (Rs.)"] != null)
                {
                    gridLedger.Columns["Unit Cost (Rs.)"].FillWeight = 55;
                    gridLedger.Columns["Unit Cost (Rs.)"].MinimumWidth = 90;
                    gridLedger.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridLedger.Columns["Unit Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridLedger.Columns["Total Cost (Rs.)"] != null)
                {
                    gridLedger.Columns["Total Cost (Rs.)"].FillWeight = 60;
                    gridLedger.Columns["Total Cost (Rs.)"].MinimumWidth = 95;
                    gridLedger.Columns["Total Cost (Rs.)"].DefaultCellStyle.Format = "N2";
                    gridLedger.Columns["Total Cost (Rs.)"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (gridLedger.Columns["Dept / Source"] != null)
                {
                    gridLedger.Columns["Dept / Source"].FillWeight = 75;
                    gridLedger.Columns["Dept / Source"].MinimumWidth = 115;
                }
                if (gridLedger.Columns["Ref / Invoice No"] != null)
                {
                    gridLedger.Columns["Ref / Invoice No"].FillWeight = 70;
                    gridLedger.Columns["Ref / Invoice No"].MinimumWidth = 110;
                }
                if (gridLedger.Columns["Remarks"] != null)
                {
                    gridLedger.Columns["Remarks"].FillWeight = 95;
                    gridLedger.Columns["Remarks"].MinimumWidth = 130;
                }
                if (gridLedger.Columns["Logged By"] != null)
                {
                    gridLedger.Columns["Logged By"].FillWeight = 50;
                    gridLedger.Columns["Logged By"].MinimumWidth = 75;
                }

                foreach (DataGridViewRow r in gridLedger.Rows)
                {
                    if (r.Cells["Movement Type"] != null && r.Cells["Movement Type"].Value != null)
                    {
                        string type = r.Cells["Movement Type"].Value.ToString();
                        if (type == "IN_PURCHASE" || type == "IN_RETURN")
                        {
                            r.Cells["Movement Type"].Style.ForeColor = Theme.Success;
                            r.Cells["Movement Type"].Style.Font = Theme.BoldFont;
                        }
                        else if (type == "OUT_USAGE")
                        {
                            r.Cells["Movement Type"].Style.ForeColor = Theme.Accent;
                        }
                        else if (type == "OUT_WASTAGE")
                        {
                            r.Cells["Movement Type"].Style.ForeColor = Theme.Danger;
                            r.Cells["Movement Type"].Style.Font = Theme.BoldFont;
                        }
                    }
                }
            }
            catch { }
        }

        private void ExportLedger()
        {
            try
            {
                if (gridLedger.DataSource is DataTable dt && dt.Rows.Count > 0)
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "CSV files (*.csv)|*.csv";
                        sfd.FileName = $"Stock_Movements_Ledger_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                if (dt.Columns[i].ColumnName == "Id") continue;
                                sb.Append("\"" + dt.Columns[i].ColumnName + "\"" + (i == dt.Columns.Count - 1 ? "" : ","));
                            }
                            sb.AppendLine();

                            foreach (DataRow row in dt.Rows)
                            {
                                for (int i = 0; i < dt.Columns.Count; i++)
                                {
                                    if (dt.Columns[i].ColumnName == "Id") continue;
                                    sb.Append("\"" + row[i].ToString().Replace("\"", "\"\"") + "\"" + (i == dt.Columns.Count - 1 ? "" : ","));
                                }
                                sb.AppendLine();
                            }

                            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                            MessageBox.Show("Movement Ledger exported successfully!", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No data available to export.", "Export Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region HELPER DATA CLASSES & MODAL DIALOGS
        private class MaterialComboItem
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public decimal CurrentStock { get; set; }
            public decimal UnitPrice { get; set; }
            public override string ToString() => $"{Name} ({Code}) - In-Stock: {CurrentStock:N2} {Unit}";
        }

        private class SupplierComboItem
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        private class RawMaterialModalDialog : Form
        {
            private int? materialId;
            private TextBox txtCode;
            private TextBox txtName;
            private ComboBox comboCategory;
            private ComboBox comboUnit;
            private TextBox txtUnitPrice;
            private TextBox txtInitialStock;
            private TextBox txtMinStock;
            private Button btnSave;
            private Button btnCancel;

            public RawMaterialModalDialog(int? id)
            {
                this.materialId = id;
                this.Text = id.HasValue ? "Edit Raw Material / Ingredient" : "Add New Raw Material";
                this.ClientSize = new Size(480, 480);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                InitializeModal();
                if (id.HasValue)
                {
                    LoadMaterialDetails(id.Value);
                }
                else
                {
                    GenerateNextCode();
                }
            }

            private void InitializeModal()
            {
                Label lblHeader = new Label();
                lblHeader.Text = materialId.HasValue ? "✏️ Edit Raw Material" : "➕ Add Cafe Raw Material";
                lblHeader.Location = new Point(25, 20);
                lblHeader.AutoSize = true;
                lblHeader.UseMnemonic = false;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int y = 65;

                // Code
                Label lblCode = new Label();
                lblCode.Text = "Material Code *";
                lblCode.Location = new Point(25, y);
                lblCode.AutoSize = true;
                lblCode.UseMnemonic = false;
                Theme.StyleLabel(lblCode, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCode);

                txtCode = new TextBox();
                txtCode.Size = new Size(180, 28);
                txtCode.Location = new Point(25, y + 20);
                Theme.StyleTextBox(txtCode);
                this.Controls.Add(txtCode);

                // Category
                Label lblCat = new Label();
                lblCat.Text = "Category *";
                lblCat.Location = new Point(230, y);
                lblCat.AutoSize = true;
                lblCat.UseMnemonic = false;
                Theme.StyleLabel(lblCat, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCat);

                comboCategory = new ComboBox();
                comboCategory.Size = new Size(220, 28);
                comboCategory.Location = new Point(230, y + 20);
                comboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
                comboCategory.BackColor = Theme.Secondary;
                comboCategory.ForeColor = Theme.TextLight;
                comboCategory.Font = Theme.MainFont;
                comboCategory.Items.AddRange(new string[] { "Dairy & Milk", "Coffee & Tea", "Meats & Non-Veg", "Pantry & Grains", "Produce", "Packaging & Disposables", "Beverages & Syrups" });
                comboCategory.SelectedIndex = 0;
                this.Controls.Add(comboCategory);

                y += 60;

                // Name
                Label lblName = new Label();
                lblName.Text = "Raw Material / Ingredient Name *";
                lblName.Location = new Point(25, y);
                lblName.AutoSize = true;
                lblName.UseMnemonic = false;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(425, 28);
                txtName.Location = new Point(25, y + 20);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                y += 60;

                // Unit & Unit Price
                Label lblUnit = new Label();
                lblUnit.Text = "Measurement Unit *";
                lblUnit.Location = new Point(25, y);
                lblUnit.AutoSize = true;
                lblUnit.UseMnemonic = false;
                Theme.StyleLabel(lblUnit, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblUnit);

                comboUnit = new ComboBox();
                comboUnit.Size = new Size(180, 28);
                comboUnit.Location = new Point(25, y + 20);
                comboUnit.DropDownStyle = ComboBoxStyle.DropDownList;
                comboUnit.BackColor = Theme.Secondary;
                comboUnit.ForeColor = Theme.TextLight;
                comboUnit.Font = Theme.MainFont;
                comboUnit.Items.AddRange(new string[] { "Kg", "Litre", "Pcs", "Gms", "Pack", "Box", "Bottles" });
                comboUnit.SelectedIndex = 0;
                this.Controls.Add(comboUnit);

                Label lblPrice = new Label();
                lblPrice.Text = "Unit Purchase Cost (Rs.) *";
                lblPrice.Location = new Point(230, y);
                lblPrice.AutoSize = true;
                lblPrice.UseMnemonic = false;
                Theme.StyleLabel(lblPrice, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPrice);

                txtUnitPrice = new TextBox();
                txtUnitPrice.Size = new Size(220, 28);
                txtUnitPrice.Location = new Point(230, y + 20);
                txtUnitPrice.Text = "0.00";
                Theme.StyleTextBox(txtUnitPrice);
                this.Controls.Add(txtUnitPrice);

                y += 60;

                // Initial Stock & Min Stock Level
                Label lblInit = new Label();
                lblInit.Text = materialId.HasValue ? "In-Hand Stock (Read Only)" : "Initial Stock Count";
                lblInit.Location = new Point(25, y);
                lblInit.AutoSize = true;
                lblInit.UseMnemonic = false;
                Theme.StyleLabel(lblInit, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblInit);

                txtInitialStock = new TextBox();
                txtInitialStock.Size = new Size(180, 28);
                txtInitialStock.Location = new Point(25, y + 20);
                txtInitialStock.Text = "0";
                if (materialId.HasValue) txtInitialStock.ReadOnly = true;
                Theme.StyleTextBox(txtInitialStock);
                this.Controls.Add(txtInitialStock);

                Label lblMin = new Label();
                lblMin.Text = "Low Stock Alert Level *";
                lblMin.Location = new Point(230, y);
                lblMin.AutoSize = true;
                lblMin.UseMnemonic = false;
                Theme.StyleLabel(lblMin, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblMin);

                txtMinStock = new TextBox();
                txtMinStock.Size = new Size(220, 28);
                txtMinStock.Location = new Point(230, y + 20);
                txtMinStock.Text = "5";
                Theme.StyleTextBox(txtMinStock);
                this.Controls.Add(txtMinStock);

                // Buttons
                btnSave = new Button();
                btnSave.Text = "💾 Save Raw Material";
                btnSave.Size = new Size(230, 38);
                btnSave.Location = new Point(25, 410);
                btnSave.UseMnemonic = false;
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(180, 38);
                btnCancel.Location = new Point(270, 410);
                btnCancel.UseMnemonic = false;
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
                this.Controls.Add(btnCancel);
            }

            private void GenerateNextCode()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM RawMaterials", conn))
                        {
                            int nextId = (int)cmd.ExecuteScalar();
                            txtCode.Text = $"RAW-{nextId:D3}";
                        }
                    }
                }
                catch { txtCode.Text = "RAW-001"; }
            }

            private void LoadMaterialDetails(int id)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT * FROM RawMaterials WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    txtCode.Text = rdr["Code"].ToString();
                                    txtName.Text = rdr["Name"].ToString();
                                    string cat = rdr["Category"].ToString();
                                    if (comboCategory.Items.Contains(cat)) comboCategory.SelectedItem = cat;
                                    string unit = rdr["Unit"].ToString();
                                    if (comboUnit.Items.Contains(unit)) comboUnit.SelectedItem = unit;
                                    txtInitialStock.Text = Convert.ToDecimal(rdr["CurrentStock"]).ToString("F2");
                                    txtMinStock.Text = Convert.ToDecimal(rdr["MinStockLevel"]).ToString("F2");
                                    txtUnitPrice.Text = Convert.ToDecimal(rdr["UnitPrice"]).ToString("F2");
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string code = txtCode.Text.Trim();
                string name = txtName.Text.Trim();
                string cat = comboCategory.SelectedItem?.ToString() ?? "Pantry & Grains";
                string unit = comboUnit.SelectedItem?.ToString() ?? "Kg";

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Please provide a valid code and name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtUnitPrice.Text.Trim(), out decimal unitPrice) || unitPrice < 0)
                {
                    MessageBox.Show("Please enter a valid unit price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtMinStock.Text.Trim(), out decimal minStock) || minStock < 0) minStock = 5;

                decimal initStock = 0;
                if (!materialId.HasValue) decimal.TryParse(txtInitialStock.Text.Trim(), out initStock);

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        if (materialId.HasValue)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE RawMaterials 
                                SET Code = @code, Name = @name, Category = @cat, Unit = @unit, 
                                    MinStockLevel = @minStock, UnitPrice = @price
                                WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@code", code);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@cat", cat);
                                cmd.Parameters.AddWithValue("@unit", unit);
                                cmd.Parameters.AddWithValue("@minStock", minStock);
                                cmd.Parameters.AddWithValue("@price", unitPrice);
                                cmd.Parameters.AddWithValue("@id", materialId.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO RawMaterials (Code, Name, Category, Unit, CurrentStock, MinStockLevel, UnitPrice, IsActive)
                                VALUES (@code, @name, @cat, @unit, @initStock, @minStock, @price, 1)", conn))
                            {
                                cmd.Parameters.AddWithValue("@code", code);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@cat", cat);
                                cmd.Parameters.AddWithValue("@unit", unit);
                                cmd.Parameters.AddWithValue("@initStock", initStock);
                                cmd.Parameters.AddWithValue("@minStock", minStock);
                                cmd.Parameters.AddWithValue("@price", unitPrice);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving raw material: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class AdjustRawMaterialDialog : Form
        {
            private int materialId;
            private decimal currentStock;
            private TextBox txtNewStock;
            private TextBox txtRemarks;
            private Button btnSave;
            private Button btnCancel;

            public AdjustRawMaterialDialog(int id, string name, string unit, decimal curStock)
            {
                this.materialId = id;
                this.currentStock = curStock;

                this.Text = "Adjust Raw Material Stock Count";
                this.ClientSize = new Size(420, 320);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "⚙️ Stock Count Adjustment";
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                lblHeader.UseMnemonic = false;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                Label lblInfo = new Label();
                lblInfo.Text = $"{name} (Current: {curStock:N2} {unit})";
                lblInfo.Location = new Point(20, 60);
                lblInfo.AutoSize = true;
                lblInfo.UseMnemonic = false;
                Theme.StyleLabel(lblInfo, Theme.Accent, Theme.BoldFont);
                this.Controls.Add(lblInfo);

                Label lblNew = new Label();
                lblNew.Text = $"Enter Correct In-Hand Stock Count ({unit}) *";
                lblNew.Location = new Point(20, 100);
                lblNew.AutoSize = true;
                lblNew.UseMnemonic = false;
                Theme.StyleLabel(lblNew, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblNew);

                txtNewStock = new TextBox();
                txtNewStock.Size = new Size(380, 28);
                txtNewStock.Location = new Point(20, 122);
                txtNewStock.Text = curStock.ToString("F2");
                Theme.StyleTextBox(txtNewStock);
                this.Controls.Add(txtNewStock);

                Label lblRem = new Label();
                lblRem.Text = "Adjustment Reason (e.g., Physical count audit, Spoilage, Reconcile)";
                lblRem.Location = new Point(20, 160);
                lblRem.AutoSize = true;
                lblRem.UseMnemonic = false;
                Theme.StyleLabel(lblRem, Theme.TextLight, Theme.MainFont);
                this.Controls.Add(lblRem);

                txtRemarks = new TextBox();
                txtRemarks.Size = new Size(380, 28);
                txtRemarks.Location = new Point(20, 182);
                Theme.StyleTextBox(txtRemarks);
                this.Controls.Add(txtRemarks);

                btnSave = new Button();
                btnSave.Text = "💾 Save Adjustment";
                btnSave.Size = new Size(200, 36);
                btnSave.Location = new Point(20, 245);
                btnSave.UseMnemonic = false;
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += (s, e) => {
                    if (!decimal.TryParse(txtNewStock.Text.Trim(), out decimal newStk) || newStk < 0)
                    {
                        MessageBox.Show("Please enter a valid non-negative stock count.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    string rem = txtRemarks.Text.Trim();
                    bool ok = DatabaseHelper.AdjustRawMaterialStock(materialId, newStk, rem, Session.Username ?? "Manager");
                    if (ok)
                    {
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show("Failed to save stock adjustment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 36);
                btnCancel.Location = new Point(240, 245);
                btnCancel.UseMnemonic = false;
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
                this.Controls.Add(btnCancel);
            }
        }
        #endregion
    }
}
