using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class CategoryControl : UserControl
    {
        // Entry Form Controls
        private TextBox txtName;
        private ComboBox comboType;
        private TextBox txtHsnSac;
        private ComboBox comboGSTRate;
        private Button btnLookupCode;
        private Button btnAdd;
        private Button btnClear;
        private Button btnDelete;

        // Grid & Filter Controls
        private TextBox txtSearch;
        private Button btnFilterAll;
        private Button btnFilterServices;
        private Button btnFilterProducts;
        private DataGridView gridCategories;
        private Label lblTotalCount;
        private Label lblServicesCount;
        private Label lblProductsCount;

        private int selectedCategoryId = 0;
        private string currentFilterType = "ALL"; // "ALL", "Service", "Product"
        private bool isBinding = false;

        public CategoryControl()
        {
            InitializeComponent();
            LoadCategories();
            this.Load += (s, e) => txtName.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1050, 700);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // ==========================================
            // 1. PAGE HEADER & SUBTITLE
            // ==========================================
            Label lblHeader = new Label();
            lblHeader.Text = "🏷️ Cafe Menu Categories Master";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Organize cafe dishes, snacks, beverages and cuisines with GST rates and tariff codes.";
            lblSubtitle.Location = new Point(22, 45);
            lblSubtitle.AutoSize = true;
            Theme.StyleLabel(lblSubtitle, Theme.TextMuted, Theme.MainFont);
            this.Controls.Add(lblSubtitle);

            // ==========================================
            // 2. LEFT PANEL: Category Entry Form Card
            // ==========================================
            Panel entryCard = Theme.CreateCard(360, 580);
            entryCard.Location = new Point(20, 80);

            Label lblCardTitle = new Label();
            lblCardTitle.Text = "Category & Tax Mapping";
            lblCardTitle.Location = new Point(16, 16);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.BoldFont);
            entryCard.Controls.Add(lblCardTitle);

            int startY = 48;
            int gapY = 56;

            // 1. Category Name
            Label lblNameTitle = new Label();
            lblNameTitle.Text = "Category Name *";
            lblNameTitle.Location = new Point(16, startY);
            lblNameTitle.AutoSize = true;
            Theme.StyleLabel(lblNameTitle, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblNameTitle);

            txtName = new TextBox();
            txtName.Size = new Size(325, 30);
            txtName.Location = new Point(16, startY + 20);
            Theme.StyleTextBox(txtName);
            entryCard.Controls.Add(txtName);

            // 2. Category Classification (Dish vs Beverage/Packaged)
            Label lblTypeTitle = new Label();
            lblTypeTitle.Text = "Classification Type *";
            lblTypeTitle.Location = new Point(16, startY + gapY);
            lblTypeTitle.AutoSize = true;
            Theme.StyleLabel(lblTypeTitle, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblTypeTitle);

            comboType = new ComboBox();
            comboType.Size = new Size(325, 30);
            comboType.Location = new Point(16, startY + gapY + 20);
            comboType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboType.Items.AddRange(new string[] { "🍽️ Food & Kitchen Dish", "🥤 Beverages & Drinks" });
            comboType.SelectedIndex = 0;
            Theme.StyleComboBox(comboType);
            entryCard.Controls.Add(comboType);

            // 3. Mapped HSN / SAC Code & GST Rate side by side
            Label lblHsnTitle = new Label();
            lblHsnTitle.Text = "Default HSN/SAC *";
            lblHsnTitle.Location = new Point(16, startY + (gapY * 2));
            lblHsnTitle.AutoSize = true;
            Theme.StyleLabel(lblHsnTitle, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblHsnTitle);

            txtHsnSac = new TextBox();
            txtHsnSac.Size = new Size(110, 30);
            txtHsnSac.Location = new Point(16, startY + (gapY * 2) + 20);
            Theme.StyleTextBox(txtHsnSac);
            txtHsnSac.Text = "2106";
            entryCard.Controls.Add(txtHsnSac);

            btnLookupCode = new Button();
            btnLookupCode.Text = "🔍";
            btnLookupCode.Size = new Size(38, 30);
            btnLookupCode.Location = new Point(130, startY + (gapY * 2) + 20);
            Theme.StyleSecondaryButton(btnLookupCode);
            btnLookupCode.Click += (s, e) => {
                string lookupType = (comboType.SelectedIndex == 1) ? "HSN" : "SAC";
                using (HsnSacLookupDialog dlg = new HsnSacLookupDialog(lookupType))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        if (txtHsnSac != null) txtHsnSac.Text = dlg.SelectedCode;
                        if (comboGSTRate != null)
                        {
                            if (dlg.SelectedGSTRate == 0m) comboGSTRate.SelectedIndex = 0;
                            else if (dlg.SelectedGSTRate == 5m) comboGSTRate.SelectedIndex = 1;
                            else if (dlg.SelectedGSTRate == 12m) comboGSTRate.SelectedIndex = 2;
                            else if (dlg.SelectedGSTRate == 28m) comboGSTRate.SelectedIndex = 4;
                            else comboGSTRate.SelectedIndex = 3; // 18%
                        }
                    }
                }
            };
            entryCard.Controls.Add(btnLookupCode);

            Label lblGstTitle = new Label();
            lblGstTitle.Text = "Default GST Rate *";
            lblGstTitle.Location = new Point(180, startY + (gapY * 2));
            lblGstTitle.AutoSize = true;
            Theme.StyleLabel(lblGstTitle, Theme.TextDark, Theme.BoldFont);
            entryCard.Controls.Add(lblGstTitle);

            comboGSTRate = new ComboBox();
            comboGSTRate.Size = new Size(161, 30);
            comboGSTRate.Location = new Point(180, startY + (gapY * 2) + 20);
            comboGSTRate.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGSTRate.Items.AddRange(new string[] { "0% (Exempt)", "5% GST", "12% GST", "18% GST (Standard)", "28% GST" });
            comboGSTRate.SelectedIndex = 3; // 18%
            Theme.StyleComboBox(comboGSTRate);
            entryCard.Controls.Add(comboGSTRate);

            // Wire comboType change handler safely after txtHsnSac is created
            comboType.SelectedIndexChanged += (s, e) => {
                if (isBinding || txtHsnSac == null) return;
                if (selectedCategoryId == 0)
                {
                    if (comboType.SelectedIndex == 0)
                    {
                        if (string.IsNullOrEmpty(txtHsnSac.Text) || txtHsnSac.Text == "2106" || txtHsnSac.Text == "0901" || txtHsnSac.Text == "1905")
                            txtHsnSac.Text = "996331";
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(txtHsnSac.Text) || txtHsnSac.Text == "996331" || txtHsnSac.Text == "996332" || txtHsnSac.Text == "996339")
                            txtHsnSac.Text = "2106";
                    }
                }
            };

            // Action Buttons
            btnAdd = new Button();
            btnAdd.Text = "💾 Save Category";
            btnAdd.Size = new Size(158, 38);
            btnAdd.Location = new Point(16, startY + (gapY * 3) + 25);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            entryCard.Controls.Add(btnAdd);

            btnClear = new Button();
            btnClear.Text = "🔄 Clear";
            btnClear.Size = new Size(158, 38);
            btnClear.Location = new Point(183, startY + (gapY * 3) + 25);
            Theme.StyleSecondaryButton(btnClear);
            btnClear.Click += (s, e) => ResetForm();
            entryCard.Controls.Add(btnClear);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Category";
            btnDelete.Size = new Size(325, 36);
            btnDelete.Location = new Point(16, startY + (gapY * 4) + 20);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            entryCard.Controls.Add(btnDelete);

            // Help Tips Box
            Panel tipsPanel = new Panel();
            tipsPanel.Size = new Size(325, 120);
            tipsPanel.Location = new Point(16, startY + (gapY * 5) + 16);
            tipsPanel.BackColor = Theme.InputBg;
            tipsPanel.Padding = new Padding(10);
            tipsPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, tipsPanel.Width - 1, tipsPanel.Height - 1);
                }
            };

            Label lblTipHeader = new Label();
            lblTipHeader.Text = "💡 Auto HSN / SAC Mapping";
            lblTipHeader.Location = new Point(8, 8);
            lblTipHeader.AutoSize = true;
            Theme.StyleLabel(lblTipHeader, Theme.Warning, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            tipsPanel.Controls.Add(lblTipHeader);

            Label lblTipBody = new Label();
            lblTipBody.Text = "When you select this Category while creating a new Service or Product, the system automatically fills its mapped SAC/HSN Code and GST Rate!";
            lblTipBody.Location = new Point(8, 28);
            lblTipBody.Size = new Size(308, 85);
            Theme.StyleLabel(lblTipBody, Theme.TextMuted, new Font("Segoe UI", 7.5F));
            tipsPanel.Controls.Add(lblTipBody);

            entryCard.Controls.Add(tipsPanel);
            this.Controls.Add(entryCard);

            // ==========================================
            // 3. RIGHT PANEL: Category Grid Directory Card
            // ==========================================
            Panel gridCard = Theme.CreateCard(630, 580);
            gridCard.Location = new Point(395, 80);
            gridCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Search Box inside gridCard
            Label lblSearchIcon = new Label();
            lblSearchIcon.Text = "🔍";
            lblSearchIcon.Location = new Point(16, 16);
            lblSearchIcon.Size = new Size(24, 24);
            gridCard.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox();
            txtSearch.Size = new Size(200, 30);
            txtSearch.Location = new Point(44, 14);
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => FilterCategories();
            gridCard.Controls.Add(txtSearch);

            // Filter Tabs
            btnFilterAll = CreateFilterTabButton("All Categories", "ALL", 255);
            btnFilterServices = CreateFilterTabButton("🍽️ Food & Kitchen", "Service", 365);
            btnFilterProducts = CreateFilterTabButton("🥤 Drinks & Retail", "Product", 495);

            gridCard.Controls.Add(btnFilterAll);
            gridCard.Controls.Add(btnFilterServices);
            gridCard.Controls.Add(btnFilterProducts);

            // Counters Panel
            FlowLayoutPanel countersPanel = new FlowLayoutPanel();
            countersPanel.FlowDirection = FlowDirection.RightToLeft;
            countersPanel.Location = new Point(570, 12);
            countersPanel.Size = new Size(350, 32);
            countersPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            countersPanel.BackColor = Color.Transparent;

            lblProductsCount = CreateStatBadge("Drinks: 0", Theme.Info);
            lblServicesCount = CreateStatBadge("Dishes: 0", Theme.Accent);
            lblTotalCount = CreateStatBadge("Total: 0", Theme.Success);

            countersPanel.Controls.Add(lblProductsCount);
            countersPanel.Controls.Add(lblServicesCount);
            countersPanel.Controls.Add(lblTotalCount);

            gridCard.Controls.Add(countersPanel);

            // Grid
            gridCategories = new DataGridView();
            gridCategories.Location = new Point(16, 56);
            gridCategories.Size = new Size(598, 506);
            gridCategories.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCategories);
            gridCategories.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridCategories.MultiSelect = false;
            gridCategories.ReadOnly = true;
            gridCategories.AllowUserToAddRows = false;
            gridCategories.RowTemplate.Height = 34;
            gridCategories.SelectionChanged += GridCategories_SelectionChanged;
            gridCard.Controls.Add(gridCategories);

            this.Controls.Add(gridCard);
            UpdateFilterButtonStyles();
        }

        private Button CreateFilterTabButton(string text, string filterType, int xPos)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(98, 30);
            btn.Location = new Point(xPos, 14);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => {
                currentFilterType = filterType;
                UpdateFilterButtonStyles();
                FilterCategories();
            };
            return btn;
        }

        private void UpdateFilterButtonStyles()
        {
            StyleSingleFilterButton(btnFilterAll, currentFilterType == "ALL");
            StyleSingleFilterButton(btnFilterServices, currentFilterType == "Service");
            StyleSingleFilterButton(btnFilterProducts, currentFilterType == "Product");
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

        public void LoadCategories()
        {
            FilterCategories();
        }

        private void FilterCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    string typeCondition = "";
                    if (currentFilterType == "Service")
                        typeCondition = " AND ISNULL(c.Type, 'Service') = 'Service'";
                    else if (currentFilterType == "Product")
                        typeCondition = " AND ISNULL(c.Type, 'Service') = 'Product'";

                    string query = $@"
                        SELECT 
                            c.Id, 
                            c.Name AS [Category Name],
                            ISNULL(c.Type, 'Service') AS [Type],
                            ISNULL(c.HsnSacCode, '996331') AS [HSN/SAC Code],
                            ISNULL(c.GSTRate, 18.00) AS [GST %],
                            (SELECT COUNT(*) FROM Services s WHERE s.Category = c.Name) AS [Services],
                            (SELECT COUNT(*) FROM Products p WHERE p.Category = c.Name) AS [Products]
                        FROM Categories c 
                        WHERE (c.Name LIKE @search OR ISNULL(c.HsnSacCode, '') LIKE @search) {typeCondition}
                        ORDER BY c.Type DESC, c.Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch?.Text.Trim() ?? ""}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            isBinding = true;
                            da.Fill(dt);
                            gridCategories.DataSource = dt;

                            if (gridCategories.Columns["Id"] != null) gridCategories.Columns["Id"].Visible = false;
                            if (gridCategories.Columns["Category Name"] != null) gridCategories.Columns["Category Name"].FillWeight = 160;
                            if (gridCategories.Columns["Type"] != null) { gridCategories.Columns["Type"].FillWeight = 80; gridCategories.Columns["Type"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                            if (gridCategories.Columns["HSN/SAC Code"] != null) { gridCategories.Columns["HSN/SAC Code"].FillWeight = 95; gridCategories.Columns["HSN/SAC Code"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                            if (gridCategories.Columns["GST %"] != null) { gridCategories.Columns["GST %"].FillWeight = 70; gridCategories.Columns["GST %"].DefaultCellStyle.Format = "0'%'"; gridCategories.Columns["GST %"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
                            if (gridCategories.Columns["Services"] != null) { gridCategories.Columns["Services"].FillWeight = 75; gridCategories.Columns["Services"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
                            if (gridCategories.Columns["Products"] != null) { gridCategories.Columns["Products"].FillWeight = 75; gridCategories.Columns["Products"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }

                            if (gridCategories.Rows.Count > 0)
                            {
                                gridCategories.ClearSelection();
                            }
                            isBinding = false;
                        }
                    }

                    // Update Counters
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            COUNT(*) AS TotalCount,
                            ISNULL(SUM(CASE WHEN ISNULL(Type, 'Service') = 'Service' THEN 1 ELSE 0 END), 0) AS ServiceCount,
                            ISNULL(SUM(CASE WHEN ISNULL(Type, 'Service') = 'Product' THEN 1 ELSE 0 END), 0) AS ProductCount
                        FROM Categories", conn))
                    {
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int total = (r["TotalCount"] != DBNull.Value) ? Convert.ToInt32(r["TotalCount"]) : 0;
                                int srv = (r["ServiceCount"] != DBNull.Value) ? Convert.ToInt32(r["ServiceCount"]) : 0;
                                int prd = (r["ProductCount"] != DBNull.Value) ? Convert.ToInt32(r["ProductCount"]) : 0;

                                if (lblTotalCount != null) lblTotalCount.Text = $"Total: {total}";
                                if (lblServicesCount != null) lblServicesCount.Text = $"Dishes: {srv}";
                                if (lblProductsCount != null) lblProductsCount.Text = $"Drinks: {prd}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}\n{ex.StackTrace}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridCategories_SelectionChanged(object sender, EventArgs e)
        {
            if (isBinding) return;
            if (gridCategories == null || gridCategories.SelectedRows.Count == 0) return;

            DataGridViewRow row = gridCategories.SelectedRows[0];
            if (row == null || row.Index < 0 || row.Cells["Id"] == null || row.Cells["Id"].Value == null || row.Cells["Id"].Value == DBNull.Value) return;

            try
            {
                selectedCategoryId = Convert.ToInt32(row.Cells["Id"].Value);
                if (txtName != null)
                    txtName.Text = row.Cells["Category Name"]?.Value?.ToString() ?? "";

                string type = row.Cells["Type"]?.Value?.ToString() ?? "Service";
                if (comboType != null)
                    comboType.SelectedIndex = (type == "Product") ? 1 : 0;

                if (txtHsnSac != null)
                    txtHsnSac.Text = row.Cells["HSN/SAC Code"]?.Value?.ToString() ?? ((type == "Product") ? "2202" : "2106");

                decimal gstRate = 5m;
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
                    else comboGSTRate.SelectedIndex = 1; // 5% default for cafe
                }

                if (btnAdd != null)
                    btnAdd.Text = "✏️ Update Category";
                if (btnDelete != null)
                    btnDelete.Enabled = true;
            }
            catch { }
        }

        private void ResetForm()
        {
            selectedCategoryId = 0;
            txtName.Clear();
            comboType.SelectedIndex = 0;
            txtHsnSac.Text = "2106";
            comboGSTRate.SelectedIndex = 1; // 5% GST
            btnAdd.Text = "💾 Save Category";
            btnDelete.Enabled = false;
            txtName.Focus();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a category name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            string type = (comboType.SelectedIndex == 1) ? "Product" : "Service";
            string hsnSac = txtHsnSac.Text.Trim();
            if (string.IsNullOrEmpty(hsnSac))
            {
                hsnSac = (type == "Product") ? "2202" : "2106";
            }

            decimal gstRate = 18m;
            if (comboGSTRate.SelectedIndex == 0) gstRate = 0m;
            else if (comboGSTRate.SelectedIndex == 1) gstRate = 5m;
            else if (comboGSTRate.SelectedIndex == 2) gstRate = 12m;
            else if (comboGSTRate.SelectedIndex == 3) gstRate = 18m;
            else if (comboGSTRate.SelectedIndex == 4) gstRate = 28m;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    if (selectedCategoryId == 0)
                    {
                        // Unique Check
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE Name = @name", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", name);
                            if ((int)checkCmd.ExecuteScalar() > 0)
                            {
                                MessageBox.Show("This category name already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Categories (Name, Type, HsnSacCode, GSTRate) 
                            VALUES (@name, @type, @hsn, @gst)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@type", type);
                            cmd.Parameters.AddWithValue("@hsn", hsnSac);
                            cmd.Parameters.AddWithValue("@gst", gstRate);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"Category '{name}' ({type}) created successfully with mapped code {hsnSac}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Update
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE Name = @name AND Id <> @id", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", name);
                            checkCmd.Parameters.AddWithValue("@id", selectedCategoryId);
                            if ((int)checkCmd.ExecuteScalar() > 0)
                            {
                                MessageBox.Show("Another category with this name already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Categories 
                            SET Name = @name, Type = @type, HsnSacCode = @hsn, GSTRate = @gst 
                            WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@type", type);
                            cmd.Parameters.AddWithValue("@hsn", hsnSac);
                            cmd.Parameters.AddWithValue("@gst", gstRate);
                            cmd.Parameters.AddWithValue("@id", selectedCategoryId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"Category '{name}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                ResetForm();
                FilterCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving category: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedCategoryId == 0)
            {
                MessageBox.Show("Please select a category from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = txtName.Text.Trim();
            if (name.Equals("Others", StringComparison.OrdinalIgnoreCase) || name.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot delete the system default category.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to delete category '{name}'?", 
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Categories WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedCategoryId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    ResetForm();
                    FilterCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting category: {ex.Message}\n(It may be in use by existing services or products)", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
