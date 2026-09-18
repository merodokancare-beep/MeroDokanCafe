using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class ProductControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridProducts;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;

        public ProductControl()
        {
            InitializeComponent();
            LoadProducts();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Product Master Directory";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Search Panel container
            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(300, 36);
            searchPanel.Location = new Point(630, 15);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);
            this.Controls.Add(searchPanel);

            // GridView
            gridProducts = new DataGridView();
            gridProducts.Size = new Size(910, 505);
            gridProducts.Location = new Point(20, 125);
            gridProducts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridProducts);
            this.Controls.Add(gridProducts);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Product";
            btnAdd.Size = new Size(160, 40);
            btnAdd.Location = new Point(0, 0);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            actionPanel.Controls.Add(btnAdd);

            btnEdit = new Button();
            btnEdit.Text = "📝 Edit Selected";
            btnEdit.Size = new Size(160, 40);
            btnEdit.Location = new Point(180, 0);
            Theme.StylePrimaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;
            actionPanel.Controls.Add(btnEdit);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected";
            btnDelete.Size = new Size(160, 40);
            btnDelete.Location = new Point(360, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            Button btnManageCategories = new Button();
            btnManageCategories.Text = "📂 Manage Categories";
            btnManageCategories.Size = new Size(180, 40);
            btnManageCategories.Location = new Point(540, 0);
            Theme.StylePrimaryButton(btnManageCategories);
            btnManageCategories.Click += BtnManageCategories_Click;
            actionPanel.Controls.Add(btnManageCategories);

            this.Controls.Add(actionPanel);
        }

        private void BtnManageCategories_Click(object sender, EventArgs e)
        {
            using (CategoryMasterDialog dlg = new CategoryMasterDialog())
            {
                dlg.ShowDialog();
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Code, HSNCode as [HSN Code], Name, Category, GSTRate as [GST %], PurchasePrice as [Cost Price], 
                               SalesPrice as [Sales Price], Stock as [Qty In Stock], MinStockLevel as [Min Level], Description 
                        FROM Products 
                        WHERE Code LIKE @search OR Name LIKE @search OR Category LIKE @search OR HSNCode LIKE @search
                        ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridProducts.DataSource = dt;
                        }
                    }
                }

                // Hide columns beautifully
                if (gridProducts.Columns["Id"] != null) gridProducts.Columns["Id"].Visible = false;
                if (gridProducts.Columns["Description"] != null) gridProducts.Columns["Description"].Visible = false;

                // Configure column HeaderTexts and formatting
                if (gridProducts.Columns["Code"] != null) gridProducts.Columns["Code"].HeaderText = "Code";
                if (gridProducts.Columns["HSN Code"] != null) gridProducts.Columns["HSN Code"].HeaderText = "HSN Code";
                if (gridProducts.Columns["Name"] != null) gridProducts.Columns["Name"].HeaderText = "Product Name";
                if (gridProducts.Columns["Category"] != null) gridProducts.Columns["Category"].HeaderText = "Category";
                if (gridProducts.Columns["GST %"] != null)
                {
                    gridProducts.Columns["GST %"].HeaderText = "GST %";
                    gridProducts.Columns["GST %"].DefaultCellStyle.Format = "0'%'";
                }
                if (gridProducts.Columns["Cost Price"] != null)
                {
                    gridProducts.Columns["Cost Price"].HeaderText = "Cost Price";
                    gridProducts.Columns["Cost Price"].DefaultCellStyle.Format = "N2";
                }
                if (gridProducts.Columns["Sales Price"] != null)
                {
                    gridProducts.Columns["Sales Price"].HeaderText = "Sales Price";
                    gridProducts.Columns["Sales Price"].DefaultCellStyle.Format = "N2";
                }
                if (gridProducts.Columns["Qty In Stock"] != null)
                {
                    gridProducts.Columns["Qty In Stock"].HeaderText = "Stock Qty";
                    gridProducts.Columns["Qty In Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    gridProducts.Columns["Qty In Stock"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                }
                if (gridProducts.Columns["Min Level"] != null)
                {
                    gridProducts.Columns["Min Level"].HeaderText = "Min Level";
                    gridProducts.Columns["Min Level"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Configure Fill Weights for elegant proportional sizing
                if (gridProducts.Columns["Code"] != null) gridProducts.Columns["Code"].FillWeight = 50;
                if (gridProducts.Columns["HSN Code"] != null) gridProducts.Columns["HSN Code"].FillWeight = 50;
                if (gridProducts.Columns["Name"] != null) gridProducts.Columns["Name"].FillWeight = 160;
                if (gridProducts.Columns["Category"] != null) gridProducts.Columns["Category"].FillWeight = 75;
                if (gridProducts.Columns["GST %"] != null) gridProducts.Columns["GST %"].FillWeight = 45;
                if (gridProducts.Columns["Cost Price"] != null) gridProducts.Columns["Cost Price"].FillWeight = 65;
                if (gridProducts.Columns["Sales Price"] != null) gridProducts.Columns["Sales Price"].FillWeight = 65;
                if (gridProducts.Columns["Qty In Stock"] != null) gridProducts.Columns["Qty In Stock"].FillWeight = 65;
                if (gridProducts.Columns["Min Level"] != null) gridProducts.Columns["Min Level"].FillWeight = 55;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProducts();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (ProductDialog dlg = new ProductDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridProducts.SelectedRows[0];
            if (selectedRow.Cells["Id"] == null || selectedRow.Cells["Id"].Value == null || selectedRow.Cells["Id"].Value == DBNull.Value) return;

            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string code = selectedRow.Cells["Code"]?.Value?.ToString() ?? "";
            string hsn = selectedRow.Cells["HSN Code"]?.Value?.ToString() ?? "2106";
            string name = selectedRow.Cells["Name"]?.Value?.ToString() ?? "";
            string category = selectedRow.Cells["Category"]?.Value?.ToString() ?? "";

            decimal gstRate = 5m;
            if (selectedRow.Cells["GST %"]?.Value != null && selectedRow.Cells["GST %"].Value != DBNull.Value)
                decimal.TryParse(selectedRow.Cells["GST %"].Value.ToString(), out gstRate);

            decimal cost = 0m;
            if (selectedRow.Cells["Cost Price"]?.Value != null && selectedRow.Cells["Cost Price"].Value != DBNull.Value)
                decimal.TryParse(selectedRow.Cells["Cost Price"].Value.ToString(), out cost);

            decimal sales = 0m;
            if (selectedRow.Cells["Sales Price"]?.Value != null && selectedRow.Cells["Sales Price"].Value != DBNull.Value)
                decimal.TryParse(selectedRow.Cells["Sales Price"].Value.ToString(), out sales);

            int stock = 0;
            if (selectedRow.Cells["Qty In Stock"]?.Value != null && selectedRow.Cells["Qty In Stock"].Value != DBNull.Value)
                int.TryParse(selectedRow.Cells["Qty In Stock"].Value.ToString(), out stock);

            int minLevel = 5;
            if (selectedRow.Cells["Min Level"]?.Value != null && selectedRow.Cells["Min Level"].Value != DBNull.Value)
                int.TryParse(selectedRow.Cells["Min Level"].Value.ToString(), out minLevel);

            string desc = selectedRow.Cells["Description"]?.Value?.ToString() ?? "";

            using (ProductDialog dlg = new ProductDialog(id, code, hsn, name, category, gstRate, cost, sales, stock, minLevel, desc))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridProducts.SelectedRows[0];
            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete product '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Products WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Product Dialog for modal add/edit
        private class ProductDialog : Form
        {
            private int? productId = null;
            private TextBox txtCode;
            private TextBox txtHSN;
            private TextBox txtName;
            private ComboBox comboCategory;
            private ComboBox comboGSTRate;
            private TextBox txtMinLevel;
            private TextBox txtCostPrice;
            private TextBox txtSalesPrice;
            private TextBox txtDescription;
            private Button btnSave;
            private Button btnCancel;
            private bool isInitializing = false;
            private System.Collections.Generic.Dictionary<string, (string Hsn, decimal Gst)> categoryMapping = new System.Collections.Generic.Dictionary<string, (string Hsn, decimal Gst)>();
            private System.Collections.Generic.Dictionary<string, string> codeToCategoryMapping = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public ProductDialog()
            {
                InitializeComponent("Add Product");
                this.Load += (s, e) => txtCode.Focus();
            }

            public ProductDialog(int id, string code, string hsn, string name, string category, decimal gstRate, decimal cost, decimal sales, int stock, int minLevel, string desc)
            {
                this.productId = id;
                isInitializing = true;
                InitializeComponent("Edit Product");

                txtCode.Text = code;
                txtHSN.Text = string.IsNullOrEmpty(hsn) ? "2106" : hsn;
                txtName.Text = name;
                
                if (!string.IsNullOrEmpty(category) && !comboCategory.Items.Contains(category))
                {
                    comboCategory.Items.Add(category);
                }
                comboCategory.Text = category;

                if (gstRate == 0m) comboGSTRate.SelectedIndex = 0;
                else if (gstRate == 5m) comboGSTRate.SelectedIndex = 1;
                else if (gstRate == 12m) comboGSTRate.SelectedIndex = 2;
                else if (gstRate == 28m) comboGSTRate.SelectedIndex = 4;
                else comboGSTRate.SelectedIndex = 3; // 18%

                txtCostPrice.Text = cost.ToString("0.00");
                txtSalesPrice.Text = sales.ToString("0.00");
                txtMinLevel.Text = minLevel.ToString();
                txtDescription.Text = desc;
                
                // If editing, make code read-only to preserve reference integrity
                txtCode.ReadOnly = true;
                txtCode.BackColor = Theme.AlternateRow;
                isInitializing = false;
                this.Load += (s, e) => txtName.Focus();
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(480, 680); // Sets inner client area precisely
                this.AutoScaleMode = AutoScaleMode.Dpi; // Robust DPI scaling handling!
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Form header
                Label lblHeader = new Label();
                lblHeader.Text = title;
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int startY = 60;
                int gapY = 58;

                // 1. Category
                Label lblCategory = new Label();
                lblCategory.Text = "Category";
                lblCategory.Location = new Point(20, startY);
                lblCategory.AutoSize = true;
                Theme.StyleLabel(lblCategory, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCategory);

                comboCategory = new ComboBox();
                comboCategory.Size = new Size(365, 30);
                comboCategory.Location = new Point(20, startY + 18);
                comboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
                Theme.StyleComboBox(comboCategory);
                this.Controls.Add(comboCategory);

                Button btnAddCategory = new Button();
                btnAddCategory.Text = "➕";
                btnAddCategory.Size = new Size(48, 30);
                btnAddCategory.Location = new Point(392, startY + 18);
                Theme.StyleSuccessButton(btnAddCategory);
                btnAddCategory.Click += (s, e) => {
                    using (CategoryMasterDialog catDlg = new CategoryMasterDialog())
                    {
                        catDlg.ShowDialog();
                        string prevSelection = comboCategory.SelectedItem?.ToString();
                        LoadCategories();
                        if (!string.IsNullOrEmpty(prevSelection) && comboCategory.Items.Contains(prevSelection))
                            comboCategory.SelectedItem = prevSelection;
                    }
                };
                this.Controls.Add(btnAddCategory);

                // 2. Code (SKU) & HSN Code side by side
                Label lblCode = new Label();
                lblCode.Text = "Product Code / Barcode (SKU) *";
                lblCode.Location = new Point(20, startY + gapY);
                lblCode.AutoSize = true;
                Theme.StyleLabel(lblCode, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCode);

                txtCode = new TextBox();
                txtCode.Size = new Size(200, 30);
                txtCode.Location = new Point(20, startY + gapY + 18);
                Theme.StyleTextBox(txtCode);
                this.Controls.Add(txtCode);

                Label lblHSN = new Label();
                lblHSN.Text = "HSN Code *";
                lblHSN.Location = new Point(240, startY + gapY);
                lblHSN.AutoSize = true;
                Theme.StyleLabel(lblHSN, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblHSN);

                txtHSN = new TextBox();
                txtHSN.Size = new Size(150, 30);
                txtHSN.Location = new Point(240, startY + gapY + 18);
                Theme.StyleTextBox(txtHSN);
                txtHSN.Text = "2106";
                txtHSN.TextChanged += (s, e) => {
                    if (isInitializing) return;
                    string hsn = txtHSN.Text.Trim();
                    if (!string.IsNullOrEmpty(hsn) && codeToCategoryMapping != null && codeToCategoryMapping.ContainsKey(hsn))
                    {
                        string matchedCat = codeToCategoryMapping[hsn];
                        if (comboCategory.SelectedItem?.ToString() != matchedCat)
                        {
                            comboCategory.SelectedItem = matchedCat;
                        }
                    }
                };
                this.Controls.Add(txtHSN);

                Button btnLookupHSN = new Button();
                btnLookupHSN.Text = "🔍";
                btnLookupHSN.Size = new Size(46, 30);
                btnLookupHSN.Location = new Point(394, startY + gapY + 18);
                Theme.StyleSecondaryButton(btnLookupHSN);
                btnLookupHSN.Click += (s, e) => {
                    using (HsnSacLookupDialog dlg = new HsnSacLookupDialog("HSN"))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            txtHSN.Text = dlg.SelectedCode;
                            if (comboGSTRate != null)
                            {
                                if (dlg.SelectedGSTRate == 0m) comboGSTRate.SelectedIndex = 0;
                                else if (dlg.SelectedGSTRate == 5m) comboGSTRate.SelectedIndex = 1;
                                else if (dlg.SelectedGSTRate == 12m) comboGSTRate.SelectedIndex = 2;
                                else if (dlg.SelectedGSTRate == 28m) comboGSTRate.SelectedIndex = 4;
                                else comboGSTRate.SelectedIndex = 3; // 18%
                            }

                            if (!string.IsNullOrEmpty(dlg.SelectedCode) && codeToCategoryMapping != null && codeToCategoryMapping.ContainsKey(dlg.SelectedCode))
                            {
                                string matchedCat = codeToCategoryMapping[dlg.SelectedCode];
                                comboCategory.SelectedItem = matchedCat;
                            }
                        }
                    }
                };
                this.Controls.Add(btnLookupHSN);

                // 3. Product Name
                Label lblName = new Label();
                lblName.Text = "Product Name *";
                lblName.Location = new Point(20, startY + (gapY * 2));
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(420, 30);
                txtName.Location = new Point(20, startY + (gapY * 2) + 18);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // 4. GST Slab & Alert Qty Threshold side by side
                Label lblGst = new Label();
                lblGst.Text = "GST Rate Slab *";
                lblGst.Location = new Point(20, startY + (gapY * 3));
                lblGst.AutoSize = true;
                Theme.StyleLabel(lblGst, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblGst);

                comboGSTRate = new ComboBox();
                comboGSTRate.Size = new Size(200, 30);
                comboGSTRate.Location = new Point(20, startY + (gapY * 3) + 18);
                comboGSTRate.DropDownStyle = ComboBoxStyle.DropDownList;
                comboGSTRate.Items.AddRange(new string[] { "0% (Exempt)", "5% GST", "12% GST", "18% GST (Standard)", "28% GST (Luxury)" });
                comboGSTRate.SelectedIndex = 3; // 18%
                Theme.StyleComboBox(comboGSTRate);
                this.Controls.Add(comboGSTRate);

                Label lblMin = new Label();
                lblMin.Text = "Alert Qty Threshold *";
                lblMin.Location = new Point(240, startY + (gapY * 3));
                lblMin.AutoSize = true;
                Theme.StyleLabel(lblMin, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblMin);

                txtMinLevel = new TextBox();
                txtMinLevel.Size = new Size(200, 30);
                txtMinLevel.Location = new Point(240, startY + (gapY * 3) + 18);
                Theme.StyleTextBox(txtMinLevel);
                txtMinLevel.Text = "5";
                this.Controls.Add(txtMinLevel);

                // 5. Cost Price & Sales Price side by side
                Label lblCostPrice = new Label();
                lblCostPrice.Text = "Cost Price *";
                lblCostPrice.Location = new Point(20, startY + (gapY * 4));
                lblCostPrice.AutoSize = true;
                Theme.StyleLabel(lblCostPrice, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCostPrice);

                txtCostPrice = new TextBox();
                txtCostPrice.Size = new Size(200, 30);
                txtCostPrice.Location = new Point(20, startY + (gapY * 4) + 18);
                Theme.StyleTextBox(txtCostPrice);
                txtCostPrice.Text = "0.00";
                this.Controls.Add(txtCostPrice);

                Label lblSalesPrice = new Label();
                lblSalesPrice.Text = "Sales Price *";
                lblSalesPrice.Location = new Point(240, startY + (gapY * 4));
                lblSalesPrice.AutoSize = true;
                Theme.StyleLabel(lblSalesPrice, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblSalesPrice);

                txtSalesPrice = new TextBox();
                txtSalesPrice.Size = new Size(200, 30);
                txtSalesPrice.Location = new Point(240, startY + (gapY * 4) + 18);
                Theme.StyleTextBox(txtSalesPrice);
                txtSalesPrice.Text = "0.00";
                this.Controls.Add(txtSalesPrice);

                // 6. Description
                Label lblDesc = new Label();
                lblDesc.Text = "Description / Specifications";
                lblDesc.Location = new Point(20, startY + (gapY * 5));
                lblDesc.AutoSize = true;
                Theme.StyleLabel(lblDesc, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblDesc);

                txtDescription = new TextBox();
                txtDescription.Size = new Size(420, 45);
                txtDescription.Location = new Point(20, startY + (gapY * 5) + 18);
                txtDescription.Multiline = true;
                Theme.StyleTextBox(txtDescription);
                this.Controls.Add(txtDescription);

                // 7. Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Product";
                btnSave.Size = new Size(200, 45);
                btnSave.Location = new Point(20, startY + (gapY * 6) + 15);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(200, 45);
                btnCancel.Location = new Point(240, startY + (gapY * 6) + 15);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;

                // Wire category change handler safely after controls are initialized
                comboCategory.SelectedIndexChanged += (s, e) => {
                    if (isInitializing) return;
                    if (txtHSN == null || comboGSTRate == null) return;
                    string selCat = comboCategory.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selCat) && categoryMapping.ContainsKey(selCat))
                    {
                        var mapping = categoryMapping[selCat];
                        if (!string.IsNullOrEmpty(mapping.Hsn))
                        {
                            txtHSN.Text = mapping.Hsn;
                        }
                        if (mapping.Gst == 0m) comboGSTRate.SelectedIndex = 0;
                        else if (mapping.Gst == 5m) comboGSTRate.SelectedIndex = 1;
                        else if (mapping.Gst == 12m) comboGSTRate.SelectedIndex = 2;
                        else if (mapping.Gst == 28m) comboGSTRate.SelectedIndex = 4;
                        else comboGSTRate.SelectedIndex = 3; // 18%
                    }
                };

                LoadCategories();
            }

            private void LoadCategories()
            {
                comboCategory.Items.Clear();
                categoryMapping.Clear();
                codeToCategoryMapping.Clear();
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT Name, ISNULL(HsnSacCode, '2106') AS HsnSacCode, ISNULL(GSTRate, 5.00) AS GSTRate, ISNULL(Type, 'Product') AS Type 
                            FROM Categories 
                            ORDER BY CASE WHEN ISNULL(Type, 'Product') = 'Product' THEN 0 ELSE 1 END, Name ASC", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    string name = rdr["Name"].ToString();
                                    string hsn = rdr["HsnSacCode"].ToString();
                                    decimal gst = Convert.ToDecimal(rdr["GSTRate"]);
                                    comboCategory.Items.Add(name);
                                    categoryMapping[name] = (hsn, gst);
                                    if (!string.IsNullOrEmpty(hsn) && !codeToCategoryMapping.ContainsKey(hsn))
                                    {
                                        codeToCategoryMapping[hsn] = name;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories for dropdown: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (comboCategory.Items.Count == 0)
                {
                    comboCategory.Items.Add("Coffee & Hot Brews");
                    categoryMapping["Coffee & Hot Brews"] = ("0901", 5m);
                    codeToCategoryMapping["0901"] = "Coffee & Hot Brews";
                }
                comboCategory.SelectedIndex = 0;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string code = txtCode.Text.Trim();
                string hsn = txtHSN.Text.Trim();
                if (string.IsNullOrEmpty(hsn)) hsn = "2106";
                string name = txtName.Text.Trim();
                string category = comboCategory.SelectedItem?.ToString() ?? "Others";
                string desc = txtDescription.Text.Trim();

                decimal gstRate = 18.00m;
                if (comboGSTRate.SelectedIndex == 0) gstRate = 0.00m;
                else if (comboGSTRate.SelectedIndex == 1) gstRate = 5.00m;
                else if (comboGSTRate.SelectedIndex == 2) gstRate = 12.00m;
                else if (comboGSTRate.SelectedIndex == 4) gstRate = 28.00m;
                else gstRate = 18.00m;

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Product Code and Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtMinLevel.Text.Trim(), out int minLevel))
                {
                    MessageBox.Show("Please enter a valid numeric value for threshold quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCostPrice.Text.Trim(), out decimal costPrice) || costPrice < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative decimal value for Cost Price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtSalesPrice.Text.Trim(), out decimal salesPrice) || salesPrice < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative decimal value for Sales Price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique code validation on Add
                        if (productId == null)
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Code = @code", conn))
                            {
                                checkCmd.Parameters.AddWithValue("@code", code);
                                if ((int)checkCmd.ExecuteScalar() > 0)
                                {
                                    MessageBox.Show($"Product Code already exists! A product with code '{code}' is already registered in the catalog.", "Duplicate Product Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // INSERT
                            int insertedId = 0;
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Products (Code, HSNCode, Name, Category, GSTRate, PurchasePrice, SalesPrice, Stock, MinStockLevel, Description) 
                                OUTPUT INSERTED.Id
                                VALUES (@code, @hsn, @name, @category, @gstRate, @purchasePrice, @salesPrice, 0, @minLevel, @desc)", conn))
                            {
                                cmd.Parameters.AddWithValue("@code", code);
                                cmd.Parameters.AddWithValue("@hsn", hsn);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.Parameters.AddWithValue("@gstRate", gstRate);
                                cmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                cmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                cmd.Parameters.AddWithValue("@minLevel", minLevel);
                                cmd.Parameters.AddWithValue("@desc", desc);
                                insertedId = (int)cmd.ExecuteScalar();
                            }

                            // Log Price History if prices > 0
                            if (costPrice > 0 || salesPrice > 0)
                            {
                                using (SqlCommand histCmd = new SqlCommand(@"
                                    INSERT INTO ProductPriceHistory (ProductId, OldPurchasePrice, NewPurchasePrice, OldSalesPrice, NewSalesPrice, ChangeDate, ChangedBy, Source)
                                    VALUES (@prodId, 0.00, @purchasePrice, 0.00, @salesPrice, GETDATE(), @userId, @source)", conn))
                                {
                                    histCmd.Parameters.AddWithValue("@prodId", insertedId);
                                    histCmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                    histCmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                    if (Session.UserId > 0)
                                        histCmd.Parameters.AddWithValue("@userId", Session.UserId);
                                    else
                                        histCmd.Parameters.AddWithValue("@userId", DBNull.Value);
                                    histCmd.Parameters.AddWithValue("@source", "Product Master Creation");
                                    histCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                        {
                            // UPDATE (code and stock are managed elsewhere/transactionally)
                            string updateSql = @"
                                DECLARE @oldPurchase DECIMAL(18,2);
                                DECLARE @oldSales DECIMAL(18,2);

                                SELECT @oldPurchase = PurchasePrice, @oldSales = SalesPrice 
                                FROM Products 
                                WHERE Id = @id;

                                UPDATE Products 
                                SET HSNCode = @hsn, Name = @name, Category = @category, GSTRate = @gstRate, PurchasePrice = @purchasePrice, SalesPrice = @salesPrice, MinStockLevel = @minLevel, Description = @desc 
                                WHERE Id = @id;

                                IF (@oldPurchase <> @purchasePrice OR @oldSales <> @salesPrice)
                                BEGIN
                                    INSERT INTO ProductPriceHistory (ProductId, OldPurchasePrice, NewPurchasePrice, OldSalesPrice, NewSalesPrice, ChangeDate, ChangedBy, Source)
                                    VALUES (@id, @oldPurchase, @purchasePrice, @oldSales, @salesPrice, GETDATE(), @userId, @source);
                                END";
                            using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", productId.Value);
                                cmd.Parameters.AddWithValue("@hsn", hsn);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.Parameters.AddWithValue("@gstRate", gstRate);
                                cmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                cmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                cmd.Parameters.AddWithValue("@minLevel", minLevel);
                                cmd.Parameters.AddWithValue("@desc", desc);
                                if (Session.UserId > 0)
                                    cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                else
                                    cmd.Parameters.AddWithValue("@userId", DBNull.Value);
                                cmd.Parameters.AddWithValue("@source", "Product Master Update");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving product details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                if (keyData == Keys.Enter && this.ActiveControl == txtCode)
                {
                    txtName.Focus();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        // Nested Category Master Dialog
        private class CategoryMasterDialog : Form
        {
            private DataGridView gridCategories;
            private TextBox txtName;
            private Button btnAdd;
            private Button btnDelete;
            private Button btnClose;

            public CategoryMasterDialog()
            {
                InitializeComponent();
                LoadCategories();
                this.Load += (s, e) => txtName.Focus();
            }

            private void InitializeComponent()
            {
                this.Text = "Manage Categories";
                this.ClientSize = new Size(440, 500); // Expanded width and sets inner client area precisely!
                this.AutoScaleMode = AutoScaleMode.Dpi; // Robust DPI scaling auto-resizing!
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Header
                Label lblHeader = new Label();
                lblHeader.Text = "Category Master";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Add Panel
                Label lblName = new Label();
                lblName.Text = "Category Name *";
                lblName.Location = new Point(20, 60);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(250, 32); 
                txtName.Location = new Point(20, 85);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                btnAdd = new Button();
                btnAdd.Text = "+ Add";
                btnAdd.Size = new Size(130, 38); 
                btnAdd.Location = new Point(290, 82); 
                Theme.StyleSuccessButton(btnAdd);
                btnAdd.Click += BtnAdd_Click;
                this.Controls.Add(btnAdd);

                // Grid View
                gridCategories = new DataGridView();
                gridCategories.Size = new Size(365, 270);
                gridCategories.Location = new Point(20, 130);
                Theme.StyleGrid(gridCategories);
                this.Controls.Add(gridCategories);

                // Delete & Close buttons
                btnDelete = new Button();
                btnDelete.Text = "🗑️ Delete Selected";
                btnDelete.Size = new Size(185, 42); 
                btnDelete.Location = new Point(20, 430); 
                Theme.StyleDangerButton(btnDelete);
                btnDelete.Click += BtnDelete_Click;
                this.Controls.Add(btnDelete);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(185, 42); 
                btnClose.Location = new Point(235, 430); 
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);
            }

            private void LoadCategories()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = "SELECT Id, Name FROM Categories ORDER BY Name ASC";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridCategories.DataSource = dt;
                            }
                        }
                    }

                    if (gridCategories.Columns["Id"] != null)
                        gridCategories.Columns["Id"].Visible = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnAdd_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Category name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique Check
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE Name = @name", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", name);
                            if ((int)checkCmd.ExecuteScalar() > 0)
                            {
                                MessageBox.Show("This category already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand("INSERT INTO Categories (Name) VALUES (@name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    txtName.Clear();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding category: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnDelete_Click(object sender, EventArgs e)
            {
                if (gridCategories.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a category to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DataGridViewRow row = gridCategories.SelectedRows[0];
                int id = Convert.ToInt32(row.Cells["Id"].Value);
                string name = row.Cells["Name"].Value.ToString();

                DialogResult confirm = MessageBox.Show($"Are you sure you want to delete category '{name}'?\nThis will not delete products belonging to this category.", 
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
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadCategories();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting category: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
