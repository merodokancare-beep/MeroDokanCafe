using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class CustomerControl : UserControl, IFocusableControl
    {
        private TextBox txtSearch;
        private DataGridView gridCustomers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;

        public CustomerControl()
        {
            InitializeComponent();
            LoadCustomers();
            this.Load += (s, e) => FocusDefaultControl();
            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                if (txtSearch != null && !txtSearch.IsDisposed && txtSearch.Visible)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Customer CRM Directory";
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
            gridCustomers = new DataGridView();
            gridCustomers.Size = new Size(910, 505);
            gridCustomers.Location = new Point(20, 125);
            gridCustomers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCustomers);
            gridCustomers.CellDoubleClick += GridCustomers_CellDoubleClick;
            gridCustomers.CellContentClick += GridCustomers_CellContentClick;
            this.Controls.Add(gridCustomers);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Customer";
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

            this.Controls.Add(actionPanel);
        }

        private void LoadCustomers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Name, Phone, GSTIN as [GSTIN], StateName as [State / POS], Email, Address,
                               CASE 
                                   WHEN (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = Customers.Id), 0) -
                                         ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = Customers.Id), 0)) < 0 
                                   THEN 0.00 
                                   ELSE (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = Customers.Id), 0) -
                                         ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = Customers.Id), 0)) 
                               END AS [Due Balance],
                               CreatedAt as [Registered Date] 
                        FROM Customers 
                        WHERE Name LIKE @search OR Phone LIKE @search OR Address LIKE @search OR GSTIN LIKE @search
                        ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridCustomers.DataSource = dt;
                        }
                    }
                }

                // Hide Id column beautifully
                if (gridCustomers.Columns["Id"] != null)
                {
                    gridCustomers.Columns["Id"].Visible = false;
                }

                // Format Due Balance column
                if (gridCustomers.Columns["Due Balance"] != null)
                {
                    gridCustomers.Columns["Due Balance"].DefaultCellStyle.Format = "N2";
                }

                // Add "View Details" button column if not already present
                if (gridCustomers.Columns["ViewDetails"] == null)
                {
                    DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
                    btnCol.Name = "ViewDetails";
                    btnCol.HeaderText = "Sold Items";
                    btnCol.Text = "🔍 View Items";
                    btnCol.UseColumnTextForButtonValue = true;
                    btnCol.FlatStyle = FlatStyle.Flat;
                    btnCol.DefaultCellStyle.BackColor = Theme.Success;
                    btnCol.DefaultCellStyle.ForeColor = Color.White;
                    btnCol.DefaultCellStyle.SelectionBackColor = Theme.Success;
                    btnCol.DefaultCellStyle.SelectionForeColor = Color.White;
                    btnCol.FillWeight = 80;
                    gridCustomers.Columns.Add(btnCol);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (CustomerDialog dlg = new CustomerDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();
            string phone = selectedRow.Cells["Phone"].Value?.ToString() ?? "";
            string gstin = selectedRow.Cells["GSTIN"].Value?.ToString() ?? "";
            string state = selectedRow.Cells["State / POS"].Value?.ToString() ?? "";
            string email = selectedRow.Cells["Email"].Value?.ToString() ?? "";
            string address = selectedRow.Cells["Address"].Value?.ToString() ?? "";

            using (CustomerDialog dlg = new CustomerDialog(customerId, name, phone, gstin, state, email, address))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string customerName = selectedRow.Cells["Name"].Value.ToString();

            if (customerName == "Walk-in Customer")
            {
                MessageBox.Show("Cannot delete the system-seeded default 'Walk-in Customer'.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal dueBalance = 0;
            if (selectedRow.Cells["Due Balance"].Value != null && selectedRow.Cells["Due Balance"].Value != DBNull.Value)
            {
                dueBalance = Convert.ToDecimal(selectedRow.Cells["Due Balance"].Value);
            }

            if (dueBalance != 0)
            {
                MessageBox.Show($"Cannot delete customer '{customerName}' because they have an active outstanding balance of Rs. {dueBalance:N2}.\nPlease clear all dues and settle accounts before deleting.", "Account Balance Settle Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete customer '{customerName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Customers WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", customerId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void GridCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = gridCustomers.Rows[e.RowIndex];
                int customerId = Convert.ToInt32(row.Cells["Id"].Value);
                string customerName = row.Cells["Name"].Value.ToString();
                decimal dueBalance = 0;
                if (row.Cells["Due Balance"].Value != null && row.Cells["Due Balance"].Value != DBNull.Value)
                {
                    dueBalance = Convert.ToDecimal(row.Cells["Due Balance"].Value);
                }

                ShowCustomerItems(customerId, customerName, dueBalance);
            }
        }

        private void GridCustomers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridCustomers.Columns[e.ColumnIndex].Name == "ViewDetails")
            {
                DataGridViewRow row = gridCustomers.Rows[e.RowIndex];
                int customerId = Convert.ToInt32(row.Cells["Id"].Value);
                string customerName = row.Cells["Name"].Value.ToString();
                decimal dueBalance = 0;
                if (row.Cells["Due Balance"].Value != null && row.Cells["Due Balance"].Value != DBNull.Value)
                {
                    dueBalance = Convert.ToDecimal(row.Cells["Due Balance"].Value);
                }

                ShowCustomerItems(customerId, customerName, dueBalance);
            }
        }

        private void ShowCustomerItems(int customerId, string customerName, decimal dueBalance)
        {
            using (CustomerPurchasedItemsDialog dlg = new CustomerPurchasedItemsDialog(customerId, customerName, dueBalance))
            {
                dlg.ShowDialog();
            }
            LoadCustomers();
        }

        // Nested Dialog to view sold products & payment breakup for a customer
        private class CustomerPurchasedItemsDialog : Form
        {
            private int customerId;
            private string customerName;
            private decimal dueBalance;

            private Label lblHeader;
            private DataGridView gridInvoices;
            private DataGridView gridItems;
            private Button btnClose;
            private Button btnRecordPayment;

            public CustomerPurchasedItemsDialog(int customerId, string customerName, decimal dueBalance)
            {
                this.customerId = customerId;
                this.customerName = customerName;
                this.dueBalance = dueBalance;
                InitializeComponent();
                LoadItems();
            }

            private void InitializeComponent()
            {
                this.Text = $"Sold Items breakup - {customerName}";
                this.ClientSize = new Size(950, 520);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;
                this.Font = Theme.MainFont;
                this.ForeColor = Theme.TextLight;

                lblHeader = new Label();
                lblHeader.Text = dueBalance > 0 
                    ? $"Outstanding Unpaid Invoices for {customerName} (Net Dues: Rs. {dueBalance:N2})"
                    : $"Sales History for {customerName}";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Invoices Grid
                gridInvoices = new DataGridView();
                gridInvoices.Location = new Point(20, 50);
                gridInvoices.Size = new Size(910, 170);
                Theme.StyleGrid(gridInvoices);
                gridInvoices.SelectionChanged += GridInvoices_SelectionChanged;
                gridInvoices.CellDoubleClick += (s, e) =>
                {
                    if (e.RowIndex >= 0 && gridInvoices.Rows[e.RowIndex].Cells["Invoice No"]?.Value != null)
                    {
                        string invNo = gridInvoices.Rows[e.RowIndex].Cells["Invoice No"].Value.ToString();
                        using (InvoiceDetailsForm dlg = new InvoiceDetailsForm(invNo))
                        {
                            dlg.ShowDialog();
                        }
                    }
                };
                this.Controls.Add(gridInvoices);

                Label lblDetailHeader = new Label();
                lblDetailHeader.Text = "Items in Selected Invoice (Double-click invoice row for full bill details)";
                lblDetailHeader.Location = new Point(20, 235);
                lblDetailHeader.AutoSize = true;
                Theme.StyleLabel(lblDetailHeader, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblDetailHeader);

                // Items Grid
                gridItems = new DataGridView();
                gridItems.Location = new Point(20, 260);
                gridItems.Size = new Size(910, 190);
                Theme.StyleGrid(gridItems);
                this.Controls.Add(gridItems);

                btnRecordPayment = new Button();
                btnRecordPayment.Text = "💳 Record Payment";
                btnRecordPayment.Size = new Size(180, 40);
                btnRecordPayment.Location = new Point(610, 465);
                Theme.StyleSuccessButton(btnRecordPayment);
                btnRecordPayment.Click += BtnRecordPayment_Click;
                btnRecordPayment.Enabled = (dueBalance > 0 && customerName != "Walk-in Customer");
                this.Controls.Add(btnRecordPayment);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(130, 40);
                btnClose.Location = new Point(800, 465);
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);

                this.CancelButton = btnClose;
            }

            private void BtnRecordPayment_Click(object sender, EventArgs e)
            {
                int? selectedSaleId = null;
                string selectedInvoiceNo = "";
                decimal paymentAmount = dueBalance;

                if (gridInvoices.CurrentRow != null)
                {
                    object idVal = gridInvoices.CurrentRow.Cells["Id"].Value;
                    object numVal = gridInvoices.CurrentRow.Cells["Invoice No"].Value;
                    object dueVal = gridInvoices.CurrentRow.Cells["Invoice Due"].Value;

                    if (idVal != null && idVal != DBNull.Value)
                    {
                        selectedSaleId = Convert.ToInt32(idVal);
                        selectedInvoiceNo = numVal?.ToString() ?? "";
                        paymentAmount = Convert.ToDecimal(dueVal);
                    }
                }

                using (RecordPaymentDialog dlg = new RecordPaymentDialog(customerId, customerName, paymentAmount, selectedSaleId, selectedInvoiceNo))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        UpdateDueBalance();
                        LoadItems();
                    }
                }
            }

            private decimal GetLatestDueBalance()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT CASE 
                                WHEN (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = @custId), 0) -
                                      ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = @custId), 0)) < 0 
                                THEN 0.00 
                                ELSE (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = @custId), 0) -
                                      ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = @custId), 0)) 
                            END";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            object val = cmd.ExecuteScalar();
                            return val != DBNull.Value ? Convert.ToDecimal(val) : 0;
                        }
                    }
                }
                catch
                {
                    return 0;
                }
            }

            private void UpdateDueBalance()
            {
                this.dueBalance = GetLatestDueBalance();
                lblHeader.Text = dueBalance > 0 
                    ? $"Outstanding Unpaid Invoices for {customerName} (Net Dues: Rs. {dueBalance:N2})"
                    : $"Sales History for {customerName}";
                btnRecordPayment.Enabled = (dueBalance > 0 && customerName != "Walk-in Customer");
            }

            private void GridInvoices_SelectionChanged(object sender, EventArgs e)
            {
                if (gridInvoices.CurrentRow != null)
                {
                    object val = gridInvoices.CurrentRow.Cells["Id"].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        int saleId = Convert.ToInt32(val);
                        LoadInvoiceItems(saleId);
                        return;
                    }
                }
                gridItems.DataSource = null;
            }

            private void LoadItems()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query;

                        if (dueBalance > 0)
                        {
                            query = @"
                                SELECT s.Id, s.InvoiceNumber as [Invoice No], 
                                       s.SaleDate as [Date],
                                       s.GrandTotal as [Grand Total],
                                       (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) as [Invoice Due]
                                FROM Sales s
                                WHERE s.CustomerId = @custId
                                  AND (s.DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0)) > 0
                                ORDER BY s.SaleDate DESC";
                        }
                        else
                        {
                            query = @"
                                SELECT s.Id, s.InvoiceNumber as [Invoice No], 
                                       s.SaleDate as [Date],
                                       s.GrandTotal as [Grand Total],
                                       0.00 as [Invoice Due]
                                FROM Sales s
                                WHERE s.CustomerId = @custId
                                ORDER BY s.SaleDate DESC";
                        }

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridInvoices.DataSource = dt;

                                // Hide ID
                                if (gridInvoices.Columns["Id"] != null) gridInvoices.Columns["Id"].Visible = false;

                                // Column formats
                                if (gridInvoices.Columns["Date"] != null) gridInvoices.Columns["Date"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                                if (gridInvoices.Columns["Grand Total"] != null) gridInvoices.Columns["Grand Total"].DefaultCellStyle.Format = "N2";
                                if (gridInvoices.Columns["Invoice Due"] != null) gridInvoices.Columns["Invoice Due"].DefaultCellStyle.Format = "N2";

                                // Fill weights
                                if (gridInvoices.Columns["Invoice No"] != null) gridInvoices.Columns["Invoice No"].FillWeight = 100;
                                if (gridInvoices.Columns["Date"] != null) gridInvoices.Columns["Date"].FillWeight = 120;
                                if (gridInvoices.Columns["Grand Total"] != null) gridInvoices.Columns["Grand Total"].FillWeight = 100;
                                if (gridInvoices.Columns["Invoice Due"] != null) gridInvoices.Columns["Invoice Due"].FillWeight = 100;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading sold invoices details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void LoadInvoiceItems(int saleId)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT 
                                CASE WHEN sd.ItemType = 'Service' THEN ISNULL(s.Code, 'CF') ELSE ISNULL(p.Code, 'CF') END as [Item Code],
                                CASE WHEN sd.ItemType = 'Service' THEN ISNULL(s.Name, 'Kitchen Dish') ELSE ISNULL(p.Name, 'Menu Item') END as [Item / Dish Name],
                                ISNULL(st.Name, '-') as [Steward / Staff],
                                sd.Quantity as [Qty], sd.UnitPrice as [Unit Price], sd.Total as [Total Amount]
                            FROM SaleDetails sd
                            LEFT JOIN Products p ON sd.ProductId = p.Id
                            LEFT JOIN Services s ON sd.ServiceId = s.Id
                            LEFT JOIN Staff st ON sd.StaffId = st.Id
                            WHERE sd.SaleId = @saleId";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridItems.DataSource = dt;

                                if (gridItems.Columns["Unit Price"] != null) gridItems.Columns["Unit Price"].DefaultCellStyle.Format = "N2";
                                if (gridItems.Columns["Total Amount"] != null) gridItems.Columns["Total Amount"].DefaultCellStyle.Format = "N2";

                                if (gridItems.Columns["Item Code"] != null) gridItems.Columns["Item Code"].FillWeight = 80;
                                if (gridItems.Columns["Item / Service Name"] != null) gridItems.Columns["Item / Service Name"].FillWeight = 180;
                                if (gridItems.Columns["Stylist"] != null) gridItems.Columns["Stylist"].FillWeight = 100;
                                if (gridItems.Columns["Qty"] != null) gridItems.Columns["Qty"].FillWeight = 50;
                                if (gridItems.Columns["Unit Price"] != null) gridItems.Columns["Unit Price"].FillWeight = 70;
                                if (gridItems.Columns["Total Amount"] != null) gridItems.Columns["Total Amount"].FillWeight = 80;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading invoice items: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Customer Dialog for modal add/edit
        private class CustomerDialog : Form
        {
            private int? customerId = null;
            private TextBox txtName;
            private TextBox txtPhone;
            private TextBox txtGSTIN;
            private ComboBox comboState;
            private TextBox txtEmail;
            private TextBox txtAddress;
            private Button btnSave;
            private Button btnCancel;

            public CustomerDialog()
            {
                InitializeComponent("Add Customer");
            }

            public CustomerDialog(int id, string name, string phone, string gstin, string stateName, string email, string address)
            {
                this.customerId = id;
                InitializeComponent("Edit Customer");
                txtName.Text = name;
                txtPhone.Text = phone;
                txtGSTIN.Text = gstin;
                if (!string.IsNullOrEmpty(stateName))
                {
                    for (int i = 0; i < comboState.Items.Count; i++)
                    {
                        if (comboState.Items[i] is GSTState gs && gs.Name.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                        {
                            comboState.SelectedIndex = i;
                            break;
                        }
                    }
                }
                txtEmail.Text = email;
                txtAddress.Text = address;
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(420, 560); // Sets inner client area precisely
                this.AutoScaleMode = AutoScaleMode.Dpi; // Robust DPI scaling auto-resizing!
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

                // Fields placement
                int startY = 60;
                int gapY = 60;

                // Name
                Label lblName = new Label();
                lblName.Text = "Full Name *";
                lblName.Location = new Point(20, startY);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(375, 30);
                txtName.Location = new Point(20, startY + 20);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Phone
                Label lblPhone = new Label();
                lblPhone.Text = "Phone Number";
                lblPhone.Location = new Point(20, startY + gapY);
                lblPhone.AutoSize = true;
                Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPhone);

                txtPhone = new TextBox();
                txtPhone.Size = new Size(375, 30);
                txtPhone.Location = new Point(20, startY + gapY + 20);
                Theme.StyleTextBox(txtPhone);
                this.Controls.Add(txtPhone);

                // GSTIN & State side-by-side
                Label lblGst = new Label();
                lblGst.Text = "Customer GSTIN (B2B)";
                lblGst.Location = new Point(20, startY + (gapY * 2));
                lblGst.AutoSize = true;
                Theme.StyleLabel(lblGst, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblGst);

                txtGSTIN = new TextBox();
                txtGSTIN.Size = new Size(180, 30);
                txtGSTIN.Location = new Point(20, startY + (gapY * 2) + 20);
                Theme.StyleTextBox(txtGSTIN);
                this.Controls.Add(txtGSTIN);

                Label lblState = new Label();
                lblState.Text = "State / Place of Supply";
                lblState.Location = new Point(210, startY + (gapY * 2));
                lblState.AutoSize = true;
                Theme.StyleLabel(lblState, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblState);

                comboState = new ComboBox();
                comboState.Size = new Size(185, 30);
                comboState.Location = new Point(210, startY + (gapY * 2) + 20);
                comboState.DropDownStyle = ComboBoxStyle.DropDownList;
                Theme.StyleComboBox(comboState);
                foreach (var st in IndianGSTHelper.GetIndianStates())
                {
                    comboState.Items.Add(st);
                }
                if (comboState.Items.Count > 6) comboState.SelectedIndex = 6; // Delhi default
                this.Controls.Add(comboState);

                // Email
                Label lblEmail = new Label();
                lblEmail.Text = "Email Address";
                lblEmail.Location = new Point(20, startY + (gapY * 3));
                lblEmail.AutoSize = true;
                Theme.StyleLabel(lblEmail, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblEmail);

                txtEmail = new TextBox();
                txtEmail.Size = new Size(375, 30);
                txtEmail.Location = new Point(20, startY + (gapY * 3) + 20);
                Theme.StyleTextBox(txtEmail);
                this.Controls.Add(txtEmail);

                // Address
                Label lblAddress = new Label();
                lblAddress.Text = "Address";
                lblAddress.Location = new Point(20, startY + (gapY * 4));
                lblAddress.AutoSize = true;
                Theme.StyleLabel(lblAddress, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAddress);

                txtAddress = new TextBox();
                txtAddress.Size = new Size(375, 30);
                txtAddress.Location = new Point(20, startY + (gapY * 4) + 20);
                Theme.StyleTextBox(txtAddress);
                this.Controls.Add(txtAddress);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Details";
                btnSave.Size = new Size(180, 42);
                btnSave.Location = new Point(20, startY + (gapY * 5) + 15);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(180, 42);
                btnCancel.Location = new Point(215, startY + (gapY * 5) + 15);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtName.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string gstin = txtGSTIN.Text.Trim();
                GSTState selState = comboState.SelectedItem as GSTState ?? new GSTState { Code = "07", Name = "Delhi" };
                string email = txtEmail.Text.Trim();
                string address = txtAddress.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(phone))
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            string checkSql = customerId == null
                                ? "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Phone <> '' AND Phone IS NOT NULL"
                                : "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Id <> @id AND Phone <> '' AND Phone IS NOT NULL";
                            using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@phone", phone);
                                if (customerId != null)
                                {
                                    cmd.Parameters.AddWithValue("@id", customerId.Value);
                                }
                                int count = (int)cmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    MessageBox.Show("A customer with this phone number is already registered.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error validating phone number: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        if (customerId == null)
                        {
                            // INSERT
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Customers (Name, Phone, GSTIN, StateName, StateCode, Email, Address) 
                                VALUES (@name, @phone, @gstin, @stName, @stCode, @email, @address)", conn))
                            {
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@gstin", string.IsNullOrEmpty(gstin) ? DBNull.Value : (object)gstin);
                                cmd.Parameters.AddWithValue("@stName", selState.Name);
                                cmd.Parameters.AddWithValue("@stCode", selState.Code);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // UPDATE
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Customers 
                                SET Name = @name, Phone = @phone, GSTIN = @gstin, StateName = @stName, StateCode = @stCode, Email = @email, Address = @address 
                                WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", customerId.Value);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@gstin", string.IsNullOrEmpty(gstin) ? DBNull.Value : (object)gstin);
                                cmd.Parameters.AddWithValue("@stName", selState.Name);
                                cmd.Parameters.AddWithValue("@stCode", selState.Code);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving customer details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Record Payment Dialog for modal collections
        private class RecordPaymentDialog : Form
        {
            private class UnpaidSaleInfo
            {
                public int Id { get; set; }
                public decimal Remaining { get; set; }
            }

            private int customerId;
            private string customerName;
            private decimal currentDue;
            private int? targetSaleId = null;
            private string invoiceNumber = string.Empty;

            private Label lblCustomerName;
            private Label lblOutstandingDue;
            private TextBox txtAmountToPay;
            private ComboBox comboPayMethod;
            private TextBox txtRemarks;
            private Button btnSave;
            private Button btnCancel;

            public RecordPaymentDialog(int customerId, string customerName, decimal currentDue, int? targetSaleId = null, string invoiceNumber = "")
            {
                this.customerId = customerId;
                this.customerName = customerName;
                this.currentDue = currentDue;
                this.targetSaleId = targetSaleId;
                this.invoiceNumber = invoiceNumber;
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                this.Text = "Record Customer Payment";
                this.ClientSize = new Size(400, 480);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "Record Due Payment";
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int startY = 80;
                int gapY = 70;

                // Customer Info
                lblCustomerName = new Label();
                lblCustomerName.Text = targetSaleId != null && !string.IsNullOrEmpty(invoiceNumber)
                    ? $"Customer: {customerName} | Invoice: {invoiceNumber}"
                    : $"Customer: {customerName}";
                lblCustomerName.Location = new Point(20, startY);
                lblCustomerName.AutoSize = true;
                Theme.StyleLabel(lblCustomerName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCustomerName);

                // Outstanding Due
                lblOutstandingDue = new Label();
                lblOutstandingDue.Text = targetSaleId != null 
                    ? $"Invoice Due: Rs. {currentDue:N2}"
                    : $"Outstanding Due: Rs. {currentDue:N2}";
                lblOutstandingDue.Location = new Point(20, startY + 25);
                lblOutstandingDue.AutoSize = true;
                Theme.StyleLabel(lblOutstandingDue, Theme.Success, Theme.BoldFont);
                this.Controls.Add(lblOutstandingDue);

                // Payment Amount
                Label lblAmount = new Label();
                lblAmount.Text = "Payment Amount (Rs.) *";
                lblAmount.Location = new Point(20, startY + gapY);
                lblAmount.AutoSize = true;
                Theme.StyleLabel(lblAmount, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAmount);

                txtAmountToPay = new TextBox();
                txtAmountToPay.Size = new Size(340, 30);
                txtAmountToPay.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtAmountToPay);
                txtAmountToPay.Text = currentDue > 0 ? currentDue.ToString("0.00") : "0.00";
                this.Controls.Add(txtAmountToPay);

                // Payment Method
                Label lblPayMethod = new Label();
                lblPayMethod.Text = "Payment Method *";
                lblPayMethod.Location = new Point(20, startY + (gapY * 2));
                lblPayMethod.AutoSize = true;
                Theme.StyleLabel(lblPayMethod, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPayMethod);

                comboPayMethod = new ComboBox();
                comboPayMethod.Size = new Size(340, 30);
                comboPayMethod.Location = new Point(20, startY + (gapY * 2) + 25);
                comboPayMethod.DropDownStyle = ComboBoxStyle.DropDownList;
                comboPayMethod.Items.AddRange(new string[] { "Cash", "Card", "QR Pay" });
                comboPayMethod.SelectedIndex = 0;
                comboPayMethod.BackColor = Theme.Primary;
                comboPayMethod.ForeColor = Theme.TextLight;
                comboPayMethod.Font = Theme.MainFont;
                this.Controls.Add(comboPayMethod);

                // Remarks
                Label lblRemarks = new Label();
                lblRemarks.Text = "Remarks";
                lblRemarks.Location = new Point(20, startY + (gapY * 3));
                lblRemarks.AutoSize = true;
                Theme.StyleLabel(lblRemarks, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblRemarks);

                txtRemarks = new TextBox();
                txtRemarks.Size = new Size(340, 30);
                txtRemarks.Location = new Point(20, startY + (gapY * 3) + 25);
                Theme.StyleTextBox(txtRemarks);
                txtRemarks.Text = "Due settlement payment";
                this.Controls.Add(txtRemarks);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Record Payment";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 390);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 390);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtAmountToPay.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                if (!decimal.TryParse(txtAmountToPay.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid payment amount greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string payMethod = comboPayMethod.SelectedItem?.ToString() ?? "Cash";
                string remarks = txtRemarks.Text.Trim();

                if (amount > currentDue)
                {
                    string dueType = targetSaleId != null ? "invoice due" : "outstanding due";
                    DialogResult confirm = MessageBox.Show($"The entered amount (Rs. {amount:N2}) is greater than the {dueType} (Rs. {currentDue:N2}).\nDo you want to proceed?", "Confirm Overpayment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes)
                    {
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        if (targetSaleId != null)
                        {
                            // 1. Record payment linked specifically to targetSaleId inside SQL Transaction
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    string insertPaySql = @"
                                        INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                        VALUES (@custId, GETDATE(), @amount, @payMethod, @remarks, @userId, @saleId)";
                                    using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@custId", customerId);
                                        cmd.Parameters.AddWithValue("@amount", amount);
                                        cmd.Parameters.AddWithValue("@payMethod", payMethod);
                                        cmd.Parameters.AddWithValue("@remarks", remarks);
                                        cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                        cmd.Parameters.AddWithValue("@saleId", targetSaleId.Value);
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
                        else
                        {
                            // 1. Fetch customer's unpaid invoices
                            var unpaidSales = new System.Collections.Generic.List<UnpaidSaleInfo>();
                            string getUnpaidSalesSql = @"
                                SELECT s.Id, s.DueAmount,
                                       ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = s.Id), 0) as PaidSoFar
                                FROM Sales s
                                WHERE s.CustomerId = @custId AND s.DueAmount > 0
                                ORDER BY s.SaleDate ASC, s.Id ASC";

                            using (SqlCommand cmd = new SqlCommand(getUnpaidSalesSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@custId", customerId);
                                using (SqlDataReader rdr = cmd.ExecuteReader())
                                {
                                    while (rdr.Read())
                                    {
                                        int saleId = rdr.GetInt32(0);
                                        decimal due = rdr.GetDecimal(1);
                                        decimal paid = rdr.GetDecimal(2);
                                        decimal remaining = due - paid;
                                        if (remaining > 0)
                                        {
                                            unpaidSales.Add(new UnpaidSaleInfo { Id = saleId, Remaining = remaining });
                                        }
                                    }
                                }
                            }

                            // 2. Allocate payment amount inside SQL Transaction
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    decimal remainingPay = amount;
                                    int saleIdx = 0;

                                    while (remainingPay > 0 && saleIdx < unpaidSales.Count)
                                    {
                                        var activeSale = unpaidSales[saleIdx];
                                        decimal alloc = Math.Min(remainingPay, activeSale.Remaining);

                                        string insertPaySql = @"
                                            INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                            VALUES (@custId, GETDATE(), @amount, @payMethod, @remarks, @userId, @saleId)";
                                        using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@custId", customerId);
                                            cmd.Parameters.AddWithValue("@amount", alloc);
                                            cmd.Parameters.AddWithValue("@payMethod", payMethod);
                                            cmd.Parameters.AddWithValue("@remarks", remarks);
                                            cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                            cmd.Parameters.AddWithValue("@saleId", activeSale.Id);
                                            cmd.ExecuteNonQuery();
                                        }

                                        remainingPay -= alloc;
                                        saleIdx++;
                                    }

                                    // 3. Record overpayment remainder as unlinked (SaleId = null)
                                    if (remainingPay > 0)
                                    {
                                        string insertPaySql = @"
                                            INSERT INTO CustomerPayments (CustomerId, PaymentDate, Amount, PaymentMethod, Remarks, CreatedBy, SaleId)
                                            VALUES (@custId, GETDATE(), @amount, @payMethod, @remarks, @userId, NULL)";
                                        using (SqlCommand cmd = new SqlCommand(insertPaySql, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@custId", customerId);
                                            cmd.Parameters.AddWithValue("@amount", remainingPay);
                                            cmd.Parameters.AddWithValue("@payMethod", payMethod);
                                            cmd.Parameters.AddWithValue("@remarks", remarks);
                                            cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                            cmd.ExecuteNonQuery();
                                        }
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
                    }
                    MessageBox.Show("Payment recorded and allocated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error recording payment: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
