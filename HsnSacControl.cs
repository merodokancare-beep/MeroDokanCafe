using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class HsnSacControl : UserControl, IFocusableControl
    {
        // Entry Controls
        private ComboBox comboType;
        private TextBox txtCode;
        private ComboBox comboGSTRate;
        private TextBox txtDescription;
        private CheckBox chkIsActive;
        private Button btnSave;
        private Button btnClear;
        private Button btnDelete;

        // Grid & Filter Controls
        private TextBox txtSearch;
        private Button btnFilterAll;
        private Button btnFilterHSN;
        private Button btnFilterSAC;
        private DataGridView gridHsnSac;

        // Counters & Badges
        private Label lblTotalCount;
        private Label lblHsnCount;
        private Label lblSacCount;

        private int selectedId = 0;
        private string currentFilterType = "ALL"; // "ALL", "HSN", "SAC"
        private bool isBinding = false;

        public HsnSacControl()
        {
            InitializeComponent();
            LoadHsnSacData();
            this.Load += (s, e) => FocusDefaultControl();
            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                if (txtCode != null && !txtCode.IsDisposed && txtCode.Visible)
                {
                    txtCode.Focus();
                    txtCode.SelectAll();
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1100, 720);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // ==========================================
            // 1. PAGE HEADER & SUBTITLE
            // ==========================================
            Label lblHeader = new Label();
            lblHeader.Text = "📑 HSN & SAC GST Master Hub";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Manage Harmonized System of Nomenclature (HSN for goods) and Services Accounting Code (SAC for services) with GST tax slabs.";
            lblSubtitle.Location = new Point(22, 45);
            lblSubtitle.AutoSize = true;
            Theme.StyleLabel(lblSubtitle, Theme.TextMuted, Theme.MainFont);
            this.Controls.Add(lblSubtitle);

            // ==========================================
            // 2. LEFT PANEL: Entry Form Card
            // ==========================================
            Panel entryCard = Theme.CreateCard(360, 600);
            entryCard.Location = new Point(20, 80);

            Label lblCardTitle = new Label();
            lblCardTitle.Text = "HSN / SAC Code Details";
            lblCardTitle.Location = new Point(16, 16);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.BoldFont);
            entryCard.Controls.Add(lblCardTitle);

            int startY = 50;
            int gapY = 55;

            // Entry Type (HSN Goods / SAC Services)
            Label lblType = new Label();
            lblType.Text = "Classification Type *";
            lblType.Location = new Point(16, startY);
            lblType.AutoSize = true;
            Theme.StyleLabel(lblType, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblType);

            comboType = new ComboBox();
            comboType.Size = new Size(325, 30);
            comboType.Location = new Point(16, startY + 20);
            comboType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboType.Items.AddRange(new string[] { "HSN (Dishes & Food Items)", "SAC (Restaurant & Catering Services)" });
            comboType.SelectedIndex = 0;
            Theme.StyleComboBox(comboType);
            comboType.SelectedIndexChanged += (s, e) => {
                if (selectedId == 0)
                {
                    if (comboType.SelectedIndex == 1 && string.IsNullOrEmpty(txtCode.Text.Trim()))
                        txtCode.Text = "9963";
                    else if (comboType.SelectedIndex == 0 && string.IsNullOrEmpty(txtCode.Text.Trim()))
                        txtCode.Text = "2106";
                }
            };
            entryCard.Controls.Add(comboType);

            // Code & GST Rate Side by Side
            Label lblCode = new Label();
            lblCode.Text = "Tariff / Tax Code *";
            lblCode.Location = new Point(16, startY + gapY);
            lblCode.AutoSize = true;
            Theme.StyleLabel(lblCode, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblCode);

            txtCode = new TextBox();
            txtCode.Size = new Size(155, 30);
            txtCode.Location = new Point(16, startY + gapY + 20);
            Theme.StyleTextBox(txtCode);
            entryCard.Controls.Add(txtCode);

            Label lblGst = new Label();
            lblGst.Text = "Default GST Rate *";
            lblGst.Location = new Point(185, startY + gapY);
            lblGst.AutoSize = true;
            Theme.StyleLabel(lblGst, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblGst);

            comboGSTRate = new ComboBox();
            comboGSTRate.Size = new Size(156, 30);
            comboGSTRate.Location = new Point(185, startY + gapY + 20);
            comboGSTRate.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGSTRate.Items.AddRange(new string[] { "0% (Exempt)", "5% GST", "12% GST", "18% GST (Standard)", "28% GST" });
            comboGSTRate.SelectedIndex = 3; // 18%
            Theme.StyleComboBox(comboGSTRate);
            entryCard.Controls.Add(comboGSTRate);

            // Description Box
            Label lblDesc = new Label();
            lblDesc.Text = "Official Description / Goods or Service Scope *";
            lblDesc.Location = new Point(16, startY + (gapY * 2));
            lblDesc.AutoSize = true;
            Theme.StyleLabel(lblDesc, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblDesc);

            txtDescription = new TextBox();
            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            txtDescription.Size = new Size(325, 75);
            txtDescription.Location = new Point(16, startY + (gapY * 2) + 20);
            Theme.StyleTextBox(txtDescription);
            entryCard.Controls.Add(txtDescription);

            // Active Checkbox
            chkIsActive = new CheckBox();
            chkIsActive.Text = "Active (Available for Product/Service tagging & POS)";
            chkIsActive.Location = new Point(16, startY + (gapY * 3) + 40);
            chkIsActive.Size = new Size(325, 24);
            chkIsActive.Checked = true;
            chkIsActive.ForeColor = Theme.TextLight;
            chkIsActive.Font = Theme.MainFont;
            entryCard.Controls.Add(chkIsActive);

            // Action Buttons
            btnSave = new Button();
            btnSave.Text = "💾 Save Entry";
            btnSave.Size = new Size(158, 38);
            btnSave.Location = new Point(16, startY + (gapY * 4) + 25);
            Theme.StyleSuccessButton(btnSave);
            btnSave.Click += BtnSave_Click;
            entryCard.Controls.Add(btnSave);

            btnClear = new Button();
            btnClear.Text = "🔄 Clear";
            btnClear.Size = new Size(158, 38);
            btnClear.Location = new Point(183, startY + (gapY * 4) + 25);
            Theme.StyleSecondaryButton(btnClear);
            btnClear.Click += (s, e) => ResetForm();
            entryCard.Controls.Add(btnClear);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Code Entry";
            btnDelete.Size = new Size(325, 36);
            btnDelete.Location = new Point(16, startY + (gapY * 5) + 20);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            entryCard.Controls.Add(btnDelete);

            // Help & Knowledge Panel
            Panel infoPanel = new Panel();
            infoPanel.Size = new Size(325, 120);
            infoPanel.Location = new Point(16, startY + (gapY * 6) + 12);
            infoPanel.BackColor = Theme.InputBg;
            infoPanel.Padding = new Padding(10);
            infoPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
                }
            };

            Label lblInfoHead = new Label();
            lblInfoHead.Text = "💡 GST Quick Reference";
            lblInfoHead.Location = new Point(8, 8);
            lblInfoHead.AutoSize = true;
            Theme.StyleLabel(lblInfoHead, Theme.Warning, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            infoPanel.Controls.Add(lblInfoHead);

            Label lblInfoBody = new Label();
            lblInfoBody.Text = "• SAC 996331: Restaurant & mobile food services (5% GST).\n• SAC 996332: Takeaway & delivery food services (5% GST).\n• HSN 2106: Food preparations, pasta, breads & cafe dishes (5% GST).\n• HSN 2202: Non-alcoholic beverages, mocktails & iced teas (12% / 18%).";
            lblInfoBody.Location = new Point(8, 28);
            lblInfoBody.Size = new Size(308, 85);
            Theme.StyleLabel(lblInfoBody, Theme.TextMuted, new Font("Segoe UI", 7.5F));
            infoPanel.Controls.Add(lblInfoBody);

            entryCard.Controls.Add(infoPanel);
            this.Controls.Add(entryCard);

            // ==========================================
            // 3. RIGHT PANEL: Directory & Search Card
            // ==========================================
            Panel gridCard = Theme.CreateCard(680, 600);
            gridCard.Location = new Point(395, 80);
            gridCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Search Bar & Filter Buttons
            Label lblSearchIcon = new Label();
            lblSearchIcon.Text = "🔍";
            lblSearchIcon.Location = new Point(16, 16);
            lblSearchIcon.Size = new Size(24, 24);
            gridCard.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox();
            txtSearch.Size = new Size(220, 30);
            txtSearch.Location = new Point(44, 14);
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => FilterData();
            gridCard.Controls.Add(txtSearch);

            // Filter Tabs
            btnFilterAll = CreateFilterTabButton("All Codes", "ALL", 280);
            btnFilterHSN = CreateFilterTabButton("🍽️ HSN Food/Goods", "HSN", 375);
            btnFilterSAC = CreateFilterTabButton("☕ SAC Cafe Services", "SAC", 510);

            gridCard.Controls.Add(btnFilterAll);
            gridCard.Controls.Add(btnFilterHSN);
            gridCard.Controls.Add(btnFilterSAC);

            // Counters Panel on Top-Right of Card
            FlowLayoutPanel countersPanel = new FlowLayoutPanel();
            countersPanel.FlowDirection = FlowDirection.RightToLeft;
            countersPanel.Location = new Point(590, 12);
            countersPanel.Size = new Size(380, 32);
            countersPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            countersPanel.BackColor = Color.Transparent;

            lblSacCount = CreateStatBadge("SAC: 0", Theme.Accent);
            lblHsnCount = CreateStatBadge("HSN: 0", Theme.Info);
            lblTotalCount = CreateStatBadge("Total: 0", Theme.Success);

            countersPanel.Controls.Add(lblSacCount);
            countersPanel.Controls.Add(lblHsnCount);
            countersPanel.Controls.Add(lblTotalCount);

            gridCard.Controls.Add(countersPanel);

            // Grid View
            gridHsnSac = new DataGridView();
            gridHsnSac.Location = new Point(16, 56);
            gridHsnSac.Size = new Size(648, 526);
            gridHsnSac.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridHsnSac);
            gridHsnSac.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridHsnSac.MultiSelect = false;
            gridHsnSac.ReadOnly = true;
            gridHsnSac.AllowUserToAddRows = false;
            gridHsnSac.RowTemplate.Height = 34;
            gridHsnSac.SelectionChanged += GridHsnSac_SelectionChanged;

            gridCard.Controls.Add(gridHsnSac);
            this.Controls.Add(gridCard);

            UpdateFilterButtonStyles();
        }

        private Button CreateFilterTabButton(string text, string filterType, int xPos)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(95, 30);
            btn.Location = new Point(xPos, 14);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                currentFilterType = filterType;
                UpdateFilterButtonStyles();
                FilterData();
            };
            return btn;
        }

        private void UpdateFilterButtonStyles()
        {
            StyleSingleFilterButton(btnFilterAll, currentFilterType == "ALL");
            StyleSingleFilterButton(btnFilterHSN, currentFilterType == "HSN");
            StyleSingleFilterButton(btnFilterSAC, currentFilterType == "SAC");
        }

        private void StyleSingleFilterButton(Button btn, bool isActive)
        {
            if (btn == null) return;
            if (isActive)
            {
                btn.BackColor = Theme.Accent;
                btn.ForeColor = Theme.TextWhite;
            }
            else
            {
                btn.BackColor = Theme.InputBg;
                btn.ForeColor = Theme.TextMuted;
            }
        }

        private Label CreateStatBadge(string text, Color color)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.AutoSize = true;
            lbl.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            lbl.ForeColor = color;
            lbl.BackColor = Color.FromArgb(28, 38, 54);
            lbl.Padding = new Padding(8, 4, 8, 4);
            lbl.Margin = new Padding(4, 2, 0, 0);
            return lbl;
        }

        public void LoadHsnSacData()
        {
            FilterData();
        }

        private void FilterData()
        {
            try
            {
                DatabaseHelper.InitializeDatabase();

                string search = "%" + (txtSearch?.Text.Trim() ?? "") + "%";
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    string typeCondition = "";
                    if (currentFilterType == "HSN")
                        typeCondition = " AND Type = 'HSN'";
                    else if (currentFilterType == "SAC")
                        typeCondition = " AND Type = 'SAC'";

                    string query = $@"
                        SELECT 
                            Id,
                            Type,
                            Code,
                            Description,
                            GSTRate AS [GST %],
                            CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS [Status],
                            CONVERT(VARCHAR(10), CreatedAt, 120) AS [Added On]
                        FROM HsnSacMaster
                        WHERE (Code LIKE @search OR Description LIKE @search OR Type LIKE @search) {typeCondition}
                        ORDER BY Type DESC, Code ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", search);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        isBinding = true;
                        gridHsnSac.DataSource = dt;
                        isBinding = false;

                        // Formatting columns
                        if (gridHsnSac.Columns["Id"] != null) gridHsnSac.Columns["Id"].Visible = false;
                        if (gridHsnSac.Columns["Type"] != null) { gridHsnSac.Columns["Type"].FillWeight = 60; gridHsnSac.Columns["Type"].HeaderText = "Type"; gridHsnSac.Columns["Type"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                        if (gridHsnSac.Columns["Code"] != null) { gridHsnSac.Columns["Code"].FillWeight = 80; gridHsnSac.Columns["Code"].HeaderText = "Code"; gridHsnSac.Columns["Code"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                        if (gridHsnSac.Columns["Description"] != null) { gridHsnSac.Columns["Description"].FillWeight = 220; gridHsnSac.Columns["Description"].HeaderText = "Scope & Description"; }
                        if (gridHsnSac.Columns["GST %"] != null) { gridHsnSac.Columns["GST %"].FillWeight = 65; gridHsnSac.Columns["GST %"].DefaultCellStyle.Format = "0'%'"; gridHsnSac.Columns["GST %"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                        if (gridHsnSac.Columns["Status"] != null) { gridHsnSac.Columns["Status"].FillWeight = 65; gridHsnSac.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                        if (gridHsnSac.Columns["Added On"] != null) { gridHsnSac.Columns["Added On"].FillWeight = 75; gridHsnSac.Columns["Added On"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }

                        if (gridHsnSac.Rows.Count > 0)
                        {
                            gridHsnSac.ClearSelection();
                        }
                    }

                    // Update Counters
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            COUNT(*) AS TotalCount,
                            ISNULL(SUM(CASE WHEN Type = 'HSN' THEN 1 ELSE 0 END), 0) AS HsnCount,
                            ISNULL(SUM(CASE WHEN Type = 'SAC' THEN 1 ELSE 0 END), 0) AS SacCount
                        FROM HsnSacMaster", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int total = (r["TotalCount"] != DBNull.Value) ? Convert.ToInt32(r["TotalCount"]) : 0;
                                int hsn = (r["HsnCount"] != DBNull.Value) ? Convert.ToInt32(r["HsnCount"]) : 0;
                                int sac = (r["SacCount"] != DBNull.Value) ? Convert.ToInt32(r["SacCount"]) : 0;

                                if (lblTotalCount != null) lblTotalCount.Text = $"Total: {total}";
                                if (lblHsnCount != null) lblHsnCount.Text = $"HSN: {hsn}";
                                if (lblSacCount != null) lblSacCount.Text = $"SAC: {sac}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading HSN/SAC master: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridHsnSac_SelectionChanged(object sender, EventArgs e)
        {
            if (isBinding) return;
            if (gridHsnSac == null || gridHsnSac.SelectedRows.Count == 0) return;

            DataGridViewRow row = gridHsnSac.SelectedRows[0];
            if (row == null || row.Index < 0 || row.Cells["Id"] == null || row.Cells["Id"].Value == null || row.Cells["Id"].Value == DBNull.Value) return;

            try
            {
                selectedId = Convert.ToInt32(row.Cells["Id"].Value);

                string type = row.Cells["Type"]?.Value?.ToString() ?? "HSN";
                if (comboType != null)
                    comboType.SelectedIndex = (type == "SAC") ? 1 : 0;

                if (txtCode != null)
                    txtCode.Text = row.Cells["Code"]?.Value?.ToString() ?? "";

                if (txtDescription != null)
                    txtDescription.Text = row.Cells["Description"]?.Value?.ToString() ?? "";

                decimal gstRate = 18m;
                if (row.Cells["GST %"] != null && row.Cells["GST %"].Value != null && row.Cells["GST %"].Value != DBNull.Value)
                {
                    decimal.TryParse(row.Cells["GST %"].Value.ToString(), out gstRate);
                }

                if (comboGSTRate != null)
                {
                    if (gstRate == 0m) comboGSTRate.SelectedIndex = 0;
                    else if (gstRate == 5m) comboGSTRate.SelectedIndex = 1;
                    else if (gstRate == 12m) comboGSTRate.SelectedIndex = 2;
                    else if (gstRate == 28m) comboGSTRate.SelectedIndex = 4;
                    else comboGSTRate.SelectedIndex = 3; // 18%
                }

                if (chkIsActive != null)
                {
                    string status = row.Cells["Status"]?.Value?.ToString() ?? "Active";
                    chkIsActive.Checked = (status == "Active");
                }

                if (btnSave != null)
                    btnSave.Text = "✏️ Update Entry";

                if (btnDelete != null)
                    btnDelete.Enabled = true;
            }
            catch { }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string code = txtCode.Text.Trim();
            string desc = txtDescription.Text.Trim();
            string type = (comboType.SelectedIndex == 1) ? "SAC" : "HSN";

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Please enter a valid HSN or SAC code.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Please provide a description or scope for this code.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescription.Focus();
                return;
            }

            decimal gstRate = 18m;
            if (comboGSTRate.SelectedIndex == 0) gstRate = 0m;
            else if (comboGSTRate.SelectedIndex == 1) gstRate = 5m;
            else if (comboGSTRate.SelectedIndex == 2) gstRate = 12m;
            else if (comboGSTRate.SelectedIndex == 3) gstRate = 18m;
            else if (comboGSTRate.SelectedIndex == 4) gstRate = 28m;

            bool isActive = chkIsActive.Checked;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Check duplicate code
                    string checkQuery = (selectedId == 0) 
                        ? "SELECT COUNT(*) FROM HsnSacMaster WHERE Code = @code" 
                        : "SELECT COUNT(*) FROM HsnSacMaster WHERE Code = @code AND Id <> @id";

                    using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);
                        if (selectedId != 0) cmd.Parameters.AddWithValue("@id", selectedId);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show($"The code '{code}' already exists in the master database.", "Duplicate Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtCode.Focus();
                            return;
                        }
                    }

                    if (selectedId == 0)
                    {
                        // INSERT
                        string insertSql = @"
                            INSERT INTO HsnSacMaster (Code, Type, Description, GSTRate, IsActive)
                            VALUES (@code, @type, @desc, @gst, @active)";
                        using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.Parameters.AddWithValue("@type", type);
                            cmd.Parameters.AddWithValue("@desc", desc);
                            cmd.Parameters.AddWithValue("@gst", gstRate);
                            cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"New {type} Code '{code}' registered successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // UPDATE
                        string updateSql = @"
                            UPDATE HsnSacMaster
                            SET Code = @code, Type = @type, Description = @desc, GSTRate = @gst, IsActive = @active
                            WHERE Id = @id";
                        using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedId);
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.Parameters.AddWithValue("@type", type);
                            cmd.Parameters.AddWithValue("@desc", desc);
                            cmd.Parameters.AddWithValue("@gst", gstRate);
                            cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"{type} Code '{code}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                ResetForm();
                FilterData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving HSN/SAC entry: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedId == 0)
            {
                MessageBox.Show("Please select an HSN/SAC code to delete from the list.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string code = txtCode.Text.Trim();
            if (MessageBox.Show($"Are you sure you want to permanently delete code '{code}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM HsnSacMaster WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"Code '{code}' deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetForm();
                    FilterData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting entry: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ResetForm()
        {
            selectedId = 0;
            comboType.SelectedIndex = 0;
            txtCode.Text = "";
            txtDescription.Text = "";
            comboGSTRate.SelectedIndex = 3; // 18%
            chkIsActive.Checked = true;
            btnSave.Text = "💾 Save Entry";
            btnDelete.Enabled = false;
            txtCode.Focus();
        }
    }

    // =========================================================================
    // HSN / SAC Quick Lookup Modal Dialog (For Product & Service Master Forms)
    // =========================================================================
    public class HsnSacLookupDialog : Form
    {
        private string targetType; // "HSN" or "SAC"
        private TextBox txtSearch;
        private DataGridView grid;
        private Button btnSelect;
        private Button btnCancel;

        public string SelectedCode { get; private set; } = "";
        public decimal SelectedGSTRate { get; private set; } = 18m;
        public string SelectedDescription { get; private set; } = "";

        public HsnSacLookupDialog(string type = "HSN")
        {
            this.targetType = type;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = (targetType == "SAC") ? "Select SAC Code (Services)" : "Select HSN Code (Goods & Products)";
            this.ClientSize = new Size(680, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.Primary;

            Label lblHeader = new Label();
            lblHeader.Text = (targetType == "SAC") ? "✂️ Service Accounting Codes (SAC)" : "🛍️ Goods & Products Nomenclature (HSN)";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Label lblSearchIcon = new Label();
            lblSearchIcon.Text = "🔍";
            lblSearchIcon.Location = new Point(20, 55);
            lblSearchIcon.Size = new Size(24, 24);
            this.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox();
            txtSearch.Size = new Size(420, 30);
            txtSearch.Location = new Point(48, 52);
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadData();
            this.Controls.Add(txtSearch);

            grid = new DataGridView();
            grid.Location = new Point(20, 95);
            grid.Size = new Size(640, 310);
            Theme.StyleGrid(grid);
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.RowTemplate.Height = 32;
            grid.DoubleClick += (s, e) => DoSelect();
            grid.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    DoSelect();
                }
            };
            this.Controls.Add(grid);

            btnSelect = new Button();
            btnSelect.Text = "✅ Select Code";
            btnSelect.Size = new Size(130, 36);
            btnSelect.Location = new Point(400, 420);
            Theme.StyleSuccessButton(btnSelect);
            btnSelect.Click += (s, e) => DoSelect();
            this.Controls.Add(btnSelect);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(110, 36);
            btnCancel.Location = new Point(545, 420);
            Theme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.Load += (s, e) => txtSearch.Focus();
        }

        private void LoadData()
        {
            try
            {
                string search = "%" + txtSearch.Text.Trim() + "%";
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Code,
                            Description AS [Scope & Description],
                            GSTRate AS [GST %]
                        FROM HsnSacMaster
                        WHERE Type = @type AND IsActive = 1 AND (Code LIKE @search OR Description LIKE @search)
                        ORDER BY Code ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@type", targetType);
                        cmd.Parameters.AddWithValue("@search", search);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        grid.DataSource = dt;
                        if (grid.Columns["Code"] != null) { grid.Columns["Code"].FillWeight = 80; grid.Columns["Code"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                        if (grid.Columns["Scope & Description"] != null) grid.Columns["Scope & Description"].FillWeight = 220;
                        if (grid.Columns["GST %"] != null) { grid.Columns["GST %"].FillWeight = 65; grid.Columns["GST %"].DefaultCellStyle.Format = "0'%'"; grid.Columns["GST %"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error querying codes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoSelect()
        {
            if (grid.SelectedRows.Count > 0)
            {
                DataGridViewRow row = grid.SelectedRows[0];
                SelectedCode = row.Cells["Code"].Value?.ToString() ?? "";
                SelectedDescription = row.Cells["Scope & Description"].Value?.ToString() ?? "";
                SelectedGSTRate = Convert.ToDecimal(row.Cells["GST %"].Value ?? 18m);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
