using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class PurchaseControl : UserControl
    {
        private ComboBox comboSupplier;
        private TextBox txtBarcode;
        private ComboBox comboCategory;
        private ComboBox comboProduct;
        private TextBox txtQty;
        private TextBox txtPrice;
        private TextBox txtSalesPrice;
        private DataGridView gridCart;
        private Label lblTotal;
        private Button btnAddItem;
        private Button btnRemoveItem;
        private Button btnSave;

        private DataTable cartTable;
        private decimal grandTotal = 0;

        public PurchaseControl()
        {
            InitializeComponent();
            LoadDropdownData();
            InitializeCart();
            this.Load += (s, e) => txtBarcode.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Purchase / Goods Inward Receipt";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // LEFT PANEL: Product Restock Fields
            Panel entryPanel = Theme.CreateCard(360, 565);
            entryPanel.Location = new Point(20, 60);
            entryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            
            Label lblEntryHeader = new Label();
            lblEntryHeader.Text = "Add Items to Invoice";
            lblEntryHeader.Location = new Point(15, 12);
            Theme.StyleLabel(lblEntryHeader, Theme.TextLight, Theme.SubHeaderFont);
            entryPanel.Controls.Add(lblEntryHeader);

            // Supplier Combobox
            Label lblSupp = new Label();
            lblSupp.Text = "Select Supplier / Distributor *";
            lblSupp.Location = new Point(15, 45);
            lblSupp.AutoSize = true;
            Theme.StyleLabel(lblSupp, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblSupp);

            comboSupplier = new ComboBox();
            comboSupplier.Size = new Size(330, 30);
            comboSupplier.Location = new Point(15, 65);
            comboSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSupplier.BackColor = Theme.Primary;
            comboSupplier.ForeColor = Theme.TextLight;
            comboSupplier.Font = Theme.MainFont;
            entryPanel.Controls.Add(comboSupplier);

            // Barcode Scan Box
            Label lblBarcode = new Label();
            lblBarcode.Text = "Product Barcode / SKU (Scan) *";
            lblBarcode.Location = new Point(15, 105);
            lblBarcode.AutoSize = true;
            Theme.StyleLabel(lblBarcode, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblBarcode);

            txtBarcode = new TextBox();
            txtBarcode.Size = new Size(330, 30);
            txtBarcode.Location = new Point(15, 125);
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            Theme.StyleTextBox(txtBarcode);
            entryPanel.Controls.Add(txtBarcode);

            // Category Combobox
            Label lblCat = new Label();
            lblCat.Text = "Select Category";
            lblCat.Location = new Point(15, 165);
            lblCat.AutoSize = true;
            Theme.StyleLabel(lblCat, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblCat);

            comboCategory = new ComboBox();
            comboCategory.Size = new Size(330, 30);
            comboCategory.Location = new Point(15, 185);
            comboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCategory.BackColor = Theme.Primary;
            comboCategory.ForeColor = Theme.TextLight;
            comboCategory.Font = Theme.MainFont;
            comboCategory.SelectedIndexChanged += ComboCategory_SelectedIndexChanged;
            entryPanel.Controls.Add(comboCategory);

            // Product Combobox
            Label lblProd = new Label();
            lblProd.Text = "Select Product *";
            lblProd.Location = new Point(15, 225);
            lblProd.AutoSize = true;
            Theme.StyleLabel(lblProd, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblProd);

            comboProduct = new ComboBox();
            comboProduct.Size = new Size(330, 30);
            comboProduct.Location = new Point(15, 245);
            comboProduct.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProduct.BackColor = Theme.Primary;
            comboProduct.ForeColor = Theme.TextLight;
            comboProduct.Font = Theme.MainFont;
            comboProduct.SelectedIndexChanged += ComboProduct_SelectedIndexChanged;
            entryPanel.Controls.Add(comboProduct);

            // Purchase Price
            Label lblPrice = new Label();
            lblPrice.Text = "Purchase Unit Cost (Rs.) *";
            lblPrice.Location = new Point(15, 285);
            lblPrice.AutoSize = true;
            Theme.StyleLabel(lblPrice, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblPrice);

            txtPrice = new TextBox();
            txtPrice.Size = new Size(330, 30);
            txtPrice.Location = new Point(15, 305);
            Theme.StyleTextBox(txtPrice);
            txtPrice.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtSalesPrice.Focus();
                    txtSalesPrice.SelectAll();
                }
            };
            entryPanel.Controls.Add(txtPrice);

            // Sales Price
            Label lblSalesPrice = new Label();
            lblSalesPrice.Text = "Sales Unit Price (Rs.) *";
            lblSalesPrice.Location = new Point(15, 345);
            lblSalesPrice.AutoSize = true;
            Theme.StyleLabel(lblSalesPrice, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblSalesPrice);

            txtSalesPrice = new TextBox();
            txtSalesPrice.Size = new Size(330, 30);
            txtSalesPrice.Location = new Point(15, 365);
            Theme.StyleTextBox(txtSalesPrice);
            txtSalesPrice.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtQty.Focus();
                    txtQty.SelectAll();
                }
            };
            entryPanel.Controls.Add(txtSalesPrice);

            // Quantity
            Label lblQty = new Label();
            lblQty.Text = "Purchase Quantity *";
            lblQty.Location = new Point(15, 405);
            lblQty.AutoSize = true;
            Theme.StyleLabel(lblQty, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblQty);

            txtQty = new TextBox();
            txtQty.Size = new Size(330, 30);
            txtQty.Location = new Point(15, 425);
            Theme.StyleTextBox(txtQty);
            txtQty.Text = "1";
            txtQty.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnAddItem_Click(btnAddItem, EventArgs.Empty);
                }
            };
            entryPanel.Controls.Add(txtQty);

            // Add Item Button
            btnAddItem = new Button();
            btnAddItem.Text = "📥 Add Item to Cart";
            btnAddItem.Size = new Size(330, 45);
            btnAddItem.Location = new Point(15, 480);
            Theme.StyleSuccessButton(btnAddItem);
            btnAddItem.Click += BtnAddItem_Click;
            entryPanel.Controls.Add(btnAddItem);

            this.Controls.Add(entryPanel);

            // RIGHT PANEL: Cart Grid & Total Checkout
            gridCart = new DataGridView();
            gridCart.Size = new Size(530, 380);
            gridCart.Location = new Point(400, 65);
            gridCart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCart);
            this.Controls.Add(gridCart);

            // Remove selected row button
            btnRemoveItem = new Button();
            btnRemoveItem.Text = "❌ Remove Selected Item";
            btnRemoveItem.Size = new Size(200, 38); // Increased from 36 to 38 for high-DPI consistency
            btnRemoveItem.Location = new Point(400, 458); // Adjusted Y slightly by 2px to align perfectly
            btnRemoveItem.UseCompatibleTextRendering = true; // Prevents clipping on high-DPI scaling systems
            btnRemoveItem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleDangerButton(btnRemoveItem);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            this.Controls.Add(btnRemoveItem);

            // Summary Info card
            Panel summaryPanel = Theme.CreateCard(530, 110);
            summaryPanel.Location = new Point(400, 510);
            summaryPanel.BackColor = Color.FromArgb(17, 24, 39);
            summaryPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Label lblTotalText = new Label();
            lblTotalText.Text = "TOTAL PURCHASE BILL COST";
            lblTotalText.Location = new Point(15, 15);
            Theme.StyleLabel(lblTotalText, Theme.TextDark, Theme.BoldFont);
            summaryPanel.Controls.Add(lblTotalText);

            lblTotal = new Label();
            lblTotal.Text = "Rs. 0.00";
            lblTotal.Location = new Point(15, 40);
            lblTotal.AutoSize = true;
            Theme.StyleLabel(lblTotal, Theme.Success, new Font("Segoe UI", 20F, FontStyle.Bold));
            summaryPanel.Controls.Add(lblTotal);

            btnSave = new Button();
            btnSave.Text = "💾 SAVE PURCHASE RECORD";
            btnSave.Size = new Size(220, 50);
            btnSave.Location = new Point(290, 40);
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StylePrimaryButton(btnSave);
            btnSave.Click += BtnSave_Click;
            summaryPanel.Controls.Add(btnSave);

            this.Controls.Add(summaryPanel);
        }

        private void InitializeCart()
        {
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductId", typeof(int));
            cartTable.Columns.Add("Code", typeof(string));
            cartTable.Columns.Add("Name", typeof(string));
            cartTable.Columns.Add("Qty", typeof(int));
            cartTable.Columns.Add("CostPrice", typeof(decimal));
            cartTable.Columns.Add("SalesPrice", typeof(decimal));
            cartTable.Columns.Add("TotalCost", typeof(decimal));

            gridCart.DataSource = cartTable;
            
            if (gridCart.Columns["ProductId"] != null) gridCart.Columns["ProductId"].Visible = false;

            // Set Header Texts
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].HeaderText = "Code";
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].HeaderText = "Product Name";
            if (gridCart.Columns["Qty"] != null) gridCart.Columns["Qty"].HeaderText = "Qty";
            if (gridCart.Columns["CostPrice"] != null)
            {
                gridCart.Columns["CostPrice"].HeaderText = "Cost (Rs.)";
                gridCart.Columns["CostPrice"].DefaultCellStyle.Format = "N2";
            }
            if (gridCart.Columns["SalesPrice"] != null)
            {
                gridCart.Columns["SalesPrice"].HeaderText = "Sales Price";
                gridCart.Columns["SalesPrice"].DefaultCellStyle.Format = "N2";
            }
            if (gridCart.Columns["TotalCost"] != null)
            {
                gridCart.Columns["TotalCost"].HeaderText = "Total Cost";
                gridCart.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
            }

            // Set custom fill weights for beautiful proportional layout
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].FillWeight = 50;
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].FillWeight = 150;
            if (gridCart.Columns["Qty"] != null) gridCart.Columns["Qty"].FillWeight = 60;
            if (gridCart.Columns["CostPrice"] != null) gridCart.Columns["CostPrice"].FillWeight = 70;
            if (gridCart.Columns["SalesPrice"] != null) gridCart.Columns["SalesPrice"].FillWeight = 70;
            if (gridCart.Columns["TotalCost"] != null) gridCart.Columns["TotalCost"].FillWeight = 70;
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevents beep sound
                string scannedCode = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(scannedCode)) return;

                if (comboSupplier.SelectedValue == null)
                {
                    if (comboSupplier.Items.Count > 0)
                    {
                        comboSupplier.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("Please select or register a Supplier/Distributor first.", "Distributor Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string sql = "SELECT Id, Code, Name, Category, PurchasePrice, SalesPrice FROM Products WHERE Code = @code OR Name = @code";
                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", scannedCode);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    int prodId = Convert.ToInt32(rdr["Id"]);
                                    string code = rdr["Code"]?.ToString() ?? scannedCode;
                                    string category = rdr["Category"]?.ToString();
                                    string name = rdr["Name"].ToString();
                                    decimal cost = Convert.ToDecimal(rdr["PurchasePrice"]);
                                    decimal sales = Convert.ToDecimal(rdr["SalesPrice"]);

                                    // 1. Select the Category
                                    comboCategory.SelectedIndexChanged -= ComboCategory_SelectedIndexChanged;
                                    if (!string.IsNullOrEmpty(category) && comboCategory.Items.Contains(category))
                                    {
                                        comboCategory.SelectedItem = category;
                                    }
                                    else
                                    {
                                        comboCategory.SelectedIndex = 0; // "-- All Categories --"
                                    }
                                    comboCategory.SelectedIndexChanged += ComboCategory_SelectedIndexChanged;

                                    // 2. Load products
                                    LoadProductsByCategory(comboCategory.SelectedItem?.ToString());

                                    // 3. Select the Product
                                    comboProduct.SelectedIndexChanged -= ComboProduct_SelectedIndexChanged;
                                    comboProduct.SelectedValue = prodId;
                                    comboProduct.SelectedIndexChanged += ComboProduct_SelectedIndexChanged;

                                    // 4. Fill prices
                                    txtPrice.Text = cost.ToString("0.00");
                                    txtSalesPrice.Text = sales.ToString("0.00");

                                    // 5. Default quantity to 1 and directly focus Purchase Quantity for user input
                                    txtQty.Text = "1";
                                    txtQty.Focus();
                                    txtQty.SelectAll();
                                }
                                else
                                {
                                    rdr.Close();
                                    DialogResult ask = MessageBox.Show($"Product code '{scannedCode}' not found in Product Master.\n\nDo you want to add it as a new product?", "New Product Detected", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                    if (ask == DialogResult.Yes)
                                    {
                                        using (QuickAddProductDialog quickDlg = new QuickAddProductDialog(scannedCode))
                                        {
                                            if (quickDlg.ShowDialog() == DialogResult.OK)
                                            {
                                                // Reload drop down categories & products
                                                LoadDropdownData();

                                                int newProdId = quickDlg.NewProductId;
                                                string newCategory = quickDlg.NewCategory;

                                                // Select Category
                                                comboCategory.SelectedIndexChanged -= ComboCategory_SelectedIndexChanged;
                                                comboCategory.SelectedItem = newCategory;
                                                comboCategory.SelectedIndexChanged += ComboCategory_SelectedIndexChanged;

                                                // Load Products
                                                LoadProductsByCategory(newCategory);

                                                // Select Product
                                                comboProduct.SelectedIndexChanged -= ComboProduct_SelectedIndexChanged;
                                                comboProduct.SelectedValue = newProdId;
                                                comboProduct.SelectedIndexChanged += ComboProduct_SelectedIndexChanged;

                                                // Fill prices
                                                txtPrice.Text = quickDlg.NewCostPrice.ToString("0.00");
                                                txtSalesPrice.Text = quickDlg.NewSalesPrice.ToString("0.00");

                                                // Directly focus Purchase Quantity
                                                txtQty.Text = "1";
                                                txtQty.Focus();
                                                txtQty.SelectAll();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        txtBarcode.SelectAll();
                                        txtBarcode.Focus();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error searching barcode database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadDropdownData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Suppliers dropdown
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Suppliers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            comboSupplier.DataSource = dt;
                            comboSupplier.DisplayMember = "Name";
                            comboSupplier.ValueMember = "Id";
                        }
                    }

                    // Categories dropdown
                    using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Categories ORDER BY Name ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            comboCategory.SelectedIndexChanged -= ComboCategory_SelectedIndexChanged;
                            comboCategory.Items.Clear();
                            comboCategory.Items.Add("-- All Categories --");
                            while (rdr.Read())
                            {
                                comboCategory.Items.Add(rdr["Name"].ToString());
                            }
                            comboCategory.SelectedIndex = 0;
                            comboCategory.SelectedIndexChanged += ComboCategory_SelectedIndexChanged;
                        }
                    }
                }

                LoadProductsByCategory("-- All Categories --");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading supplier/category/product dropdown lists: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboCategory.SelectedItem == null) return;
            string selectedCat = comboCategory.SelectedItem.ToString();
            LoadProductsByCategory(selectedCat);
        }

        private void LoadProductsByCategory(string categoryName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    
                    string query = "SELECT Id, Code + ' - ' + Name as DisplayName FROM Products";
                    if (!string.IsNullOrEmpty(categoryName) && categoryName != "-- All Categories --")
                    {
                        query += " WHERE Category = @category";
                    }
                    query += " ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(categoryName) && categoryName != "-- All Categories --")
                        {
                            cmd.Parameters.AddWithValue("@category", categoryName);
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            
                            comboProduct.SelectedIndexChanged -= ComboProduct_SelectedIndexChanged;
                            comboProduct.DataSource = dt;
                            comboProduct.DisplayMember = "DisplayName";
                            comboProduct.ValueMember = "Id";
                            comboProduct.SelectedIndexChanged += ComboProduct_SelectedIndexChanged;
                            
                            if (dt.Rows.Count == 0)
                            {
                                txtPrice.Clear();
                                txtSalesPrice.Clear();
                            }
                            else
                            {
                                ComboProduct_SelectedIndexChanged(comboProduct, EventArgs.Empty);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboProduct.SelectedValue == null) return;

            int prodId = 0;
            if (comboProduct.SelectedValue is int)
            {
                prodId = (int)comboProduct.SelectedValue;
            }
            else if (comboProduct.SelectedValue is DataRowView drv)
            {
                prodId = (int)drv["Id"];
            }
            else
            {
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT PurchasePrice, SalesPrice FROM Products WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", prodId);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                txtPrice.Text = Convert.ToDecimal(rdr["PurchasePrice"]).ToString("0.00");
                                txtSalesPrice.Text = Convert.ToDecimal(rdr["SalesPrice"]).ToString("0.00");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (comboProduct.SelectedValue == null)
            {
                MessageBox.Show("Please select a product first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int prodId = 0;
            if (comboProduct.SelectedValue is int)
            {
                prodId = (int)comboProduct.SelectedValue;
            }
            else if (comboProduct.SelectedValue is DataRowView drv)
            {
                prodId = (int)drv["Id"];
            }
            else
            {
                MessageBox.Show("Please select a valid product first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid non-negative cost price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtSalesPrice.Text.Trim(), out decimal salesPrice) || salesPrice < 0)
            {
                MessageBox.Show("Please enter a valid non-negative sales unit price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Retrieve Product details from Combobox text
            string fullText = comboProduct.Text;
            int hyphenIndex = fullText.IndexOf(" - ");
            string code = hyphenIndex >= 0 ? fullText.Substring(0, hyphenIndex).Trim() : fullText;
            string name = hyphenIndex >= 0 ? fullText.Substring(hyphenIndex + 3).Trim() : fullText;

            // Check if item already exists in the cart. If yes, update it.
            bool found = false;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == prodId)
                {
                    row["Qty"] = (int)row["Qty"] + qty;
                    row["CostPrice"] = price;
                    row["SalesPrice"] = salesPrice;
                    row["TotalCost"] = (int)row["Qty"] * price;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                cartTable.Rows.Add(prodId, code, name, qty, price, salesPrice, qty * price);
            }

            CalculateGrandTotal();

            // Reset quantity and refocus barcode
            txtQty.Text = "1";
            txtBarcode.Clear();
            txtBarcode.Focus();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (gridCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a row in the cart to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            gridCart.Rows.Remove(gridCart.SelectedRows[0]);
            CalculateGrandTotal();
        }

        private void CalculateGrandTotal()
        {
            grandTotal = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                grandTotal += Convert.ToDecimal(row["TotalCost"]);
            }
            lblTotal.Text = $"Rs. {grandTotal:N2}";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (comboSupplier.SelectedValue == null)
            {
                MessageBox.Show("Please select a Supplier.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Cart is empty. Please add at least one product.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int supplierId = (int)comboSupplier.SelectedValue;
            string purchaseNumber = $"PUR-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insert Purchase Header
                    int purchaseId = 0;
                    string headerSql = @"
                        INSERT INTO Purchases (PurchaseNumber, SupplierId, PurchaseDate, TotalAmount, CreatedBy) 
                        OUTPUT INSERTED.Id
                        VALUES (@purNum, @suppId, GETDATE(), @total, @user)";

                    using (SqlCommand cmd = new SqlCommand(headerSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@purNum", purchaseNumber);
                        cmd.Parameters.AddWithValue("@suppId", supplierId);
                        cmd.Parameters.AddWithValue("@total", grandTotal);
                        cmd.Parameters.AddWithValue("@user", Session.UserId);
                        purchaseId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Insert Purchase Details and Update Product Stock
                    foreach (DataRow row in cartTable.Rows)
                    {
                        int prodId = (int)row["ProductId"];
                        int qty = (int)row["Qty"];
                        decimal costPrice = (decimal)row["CostPrice"];

                        // Details Insert
                        string detailsSql = @"
                            INSERT INTO PurchaseDetails (PurchaseId, ProductId, Quantity, PurchasePrice) 
                            VALUES (@purId, @prodId, @qty, @cost)";

                        using (SqlCommand cmd = new SqlCommand(detailsSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@purId", purchaseId);
                            cmd.Parameters.AddWithValue("@prodId", prodId);
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@cost", costPrice);
                            cmd.ExecuteNonQuery();
                        }

                        // Product Stock & Price Update (purchase updates stock, cost, and sales price) + Log Price History
                        string productUpdateSql = @"
                            DECLARE @oldPurchase DECIMAL(18,2);
                            DECLARE @oldSales DECIMAL(18,2);

                            SELECT @oldPurchase = PurchasePrice, @oldSales = SalesPrice 
                            FROM Products 
                            WHERE Id = @id;

                            UPDATE Products 
                            SET Stock = Stock + @qty, PurchasePrice = @cost, SalesPrice = @sales 
                            WHERE Id = @id;

                            IF (@oldPurchase <> @cost OR @oldSales <> @sales)
                            BEGIN
                                INSERT INTO ProductPriceHistory (ProductId, OldPurchasePrice, NewPurchasePrice, OldSalesPrice, NewSalesPrice, ChangeDate, ChangedBy, Source)
                                VALUES (@id, @oldPurchase, @cost, @oldSales, @sales, GETDATE(), @userId, @source);
                            END";

                        using (SqlCommand cmd = new SqlCommand(productUpdateSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@cost", costPrice);
                            cmd.Parameters.AddWithValue("@sales", (decimal)row["SalesPrice"]);
                            cmd.Parameters.AddWithValue("@id", prodId);
                            cmd.Parameters.AddWithValue("@userId", Session.UserId);
                            cmd.Parameters.AddWithValue("@source", $"Purchase Entry: {purchaseNumber}");
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    MessageBox.Show($"Purchase bill recorded successfully!\nReceipt No: {purchaseNumber}", "Receipt Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Reset Screen
                    cartTable.Clear();
                    CalculateGrandTotal();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Failed to record purchase entry: {ex.Message}", "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Quick Add Product Dialog
        private class QuickAddProductDialog : Form
        {
            public int NewProductId { get; private set; }
            public string NewCode { get; private set; }
            public string NewName { get; private set; }
            public string NewCategory { get; private set; }
            public decimal NewCostPrice { get; private set; }
            public decimal NewSalesPrice { get; private set; }

            private TextBox txtCode;
            private TextBox txtName;
            private ComboBox comboCategory;
            private TextBox txtCost;
            private TextBox txtSales;
            private TextBox txtMinLevel;
            private Button btnSave;
            private Button btnCancel;

            public QuickAddProductDialog(string barcode)
            {
                InitializeComponent();
                txtCode.Text = barcode;
            }

            private void InitializeComponent()
            {
                this.Text = "Quick Add Product to Master";
                this.ClientSize = new Size(480, 520);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "Quick Add New Product";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int startY = 65;
                int gapY = 55;

                // Code
                Label lblCode = new Label();
                lblCode.Text = "Product Barcode / SKU";
                lblCode.Location = new Point(20, startY);
                lblCode.AutoSize = true;
                Theme.StyleLabel(lblCode, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCode);

                txtCode = new TextBox();
                txtCode.Size = new Size(420, 30);
                txtCode.Location = new Point(20, startY + 20);
                txtCode.ReadOnly = true;
                txtCode.BackColor = Theme.AlternateRow;
                Theme.StyleTextBox(txtCode);
                this.Controls.Add(txtCode);

                // Category
                Label lblCategory = new Label();
                lblCategory.Text = "Category";
                lblCategory.Location = new Point(20, startY + gapY);
                lblCategory.AutoSize = true;
                Theme.StyleLabel(lblCategory, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCategory);

                comboCategory = new ComboBox();
                comboCategory.Size = new Size(420, 30);
                comboCategory.Location = new Point(20, startY + gapY + 20);
                comboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
                Theme.StyleComboBox(comboCategory);
                this.Controls.Add(comboCategory);
                LoadCategories();

                // Name
                Label lblName = new Label();
                lblName.Text = "Product Name *";
                lblName.Location = new Point(20, startY + (gapY * 2));
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(420, 30);
                txtName.Location = new Point(20, startY + (gapY * 2) + 20);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Cost Price
                Label lblCost = new Label();
                lblCost.Text = "Purchase Unit Cost (Rs.)";
                lblCost.Location = new Point(20, startY + (gapY * 3));
                lblCost.AutoSize = true;
                Theme.StyleLabel(lblCost, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCost);

                txtCost = new TextBox();
                txtCost.Size = new Size(200, 30);
                txtCost.Location = new Point(20, startY + (gapY * 3) + 20);
                txtCost.Text = "0.00";
                Theme.StyleTextBox(txtCost);
                this.Controls.Add(txtCost);

                // Sales Price
                Label lblSales = new Label();
                lblSales.Text = "Sales Unit Price (Rs.)";
                lblSales.Location = new Point(240, startY + (gapY * 3));
                lblSales.AutoSize = true;
                Theme.StyleLabel(lblSales, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblSales);

                txtSales = new TextBox();
                txtSales.Size = new Size(200, 30);
                txtSales.Location = new Point(240, startY + (gapY * 3) + 20);
                txtSales.Text = "0.00";
                Theme.StyleTextBox(txtSales);
                this.Controls.Add(txtSales);

                // Alert Threshold
                Label lblMin = new Label();
                lblMin.Text = "Min Stock Alert Level";
                lblMin.Location = new Point(20, startY + (gapY * 4));
                lblMin.AutoSize = true;
                Theme.StyleLabel(lblMin, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblMin);

                txtMinLevel = new TextBox();
                txtMinLevel.Size = new Size(420, 30);
                txtMinLevel.Location = new Point(20, startY + (gapY * 4) + 20);
                txtMinLevel.Text = "5";
                Theme.StyleTextBox(txtMinLevel);
                this.Controls.Add(txtMinLevel);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save & Select";
                btnSave.Size = new Size(200, 45);
                btnSave.Location = new Point(20, 420);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(200, 45);
                btnCancel.Location = new Point(240, 420);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtName.Focus();
            }

            private void LoadCategories()
            {
                comboCategory.Items.Clear();
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Categories ORDER BY Name ASC", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    comboCategory.Items.Add(rdr["Name"].ToString());
                                }
                            }
                        }
                    }
                }
                catch { }

                if (comboCategory.Items.Count == 0)
                {
                    comboCategory.Items.Add("Others");
                }
                comboCategory.SelectedIndex = 0;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string code = txtCode.Text.Trim();
                string name = txtName.Text.Trim();
                string category = comboCategory.SelectedItem?.ToString() ?? "Others";

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Product Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCost.Text.Trim(), out decimal cost) || cost < 0)
                {
                    MessageBox.Show("Please enter a valid cost price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtSales.Text.Trim(), out decimal sales) || sales < 0)
                {
                    MessageBox.Show("Please enter a valid sales price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtMinLevel.Text.Trim(), out int minLevel) || minLevel < 0)
                {
                    MessageBox.Show("Please enter a valid min stock level.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique Check
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
                        string sql = @"
                            INSERT INTO Products (Code, Name, Category, PurchasePrice, SalesPrice, Stock, MinStockLevel, Description) 
                            OUTPUT INSERTED.Id
                            VALUES (@code, @name, @category, @cost, @sales, 0, @minLevel, @desc)";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@category", category);
                            cmd.Parameters.AddWithValue("@cost", cost);
                            cmd.Parameters.AddWithValue("@sales", sales);
                            cmd.Parameters.AddWithValue("@minLevel", minLevel);
                            cmd.Parameters.AddWithValue("@desc", "");
                            
                            int insertedId = (int)cmd.ExecuteScalar();
                            
                            this.NewProductId = insertedId;
                            this.NewCode = code;
                            this.NewName = name;
                            this.NewCategory = category;
                            this.NewCostPrice = cost;
                            this.NewSalesPrice = sales;
                        }
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving new product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
