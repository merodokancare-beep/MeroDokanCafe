using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class SalesReturnControl : UserControl
    {
        private TextBox txtInvoiceSearch;
        private Button btnSearch;
        private Label lblCustomerVal;
        private Label lblDateVal;
        private Label lblOriginalTotalVal;
        private Label lblPaymentModeVal;

        private DataGridView gridReturnItems;
        private Label lblRefundTotal;
        private Button btnProcessRefund;

        private int activeSaleId = 0;
        private DataTable returnTable;
        private decimal refundTotalAmount = 0;
        private decimal discountRatio = 0;

        public SalesReturnControl()
        {
            InitializeComponent();
            InitializeReturnTable();
            this.Load += (s, e) => txtInvoiceSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Sales Return & Customer Refund Terminal";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // LEFT PANEL: Invoice Search & Summary Card
            Panel entryPanel = Theme.CreateCard(360, 520);
            entryPanel.Location = new Point(20, 65);
            entryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            Label lblEntryHeader = new Label();
            lblEntryHeader.Text = "Invoice Retrieval";
            lblEntryHeader.Location = new Point(15, 15);
            Theme.StyleLabel(lblEntryHeader, Theme.TextLight, Theme.SubHeaderFont);
            entryPanel.Controls.Add(lblEntryHeader);

            // Invoice Search Input
            Label lblSearch = new Label();
            lblSearch.Text = "Enter or Scan Invoice Number *";
            lblSearch.Location = new Point(15, 55);
            lblSearch.AutoSize = true;
            Theme.StyleLabel(lblSearch, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblSearch);

            txtInvoiceSearch = new TextBox();
            txtInvoiceSearch.Size = new Size(330, 30);
            txtInvoiceSearch.Location = new Point(15, 78);
            Theme.StyleTextBox(txtInvoiceSearch);
            txtInvoiceSearch.KeyDown += TxtInvoiceSearch_KeyDown;
            entryPanel.Controls.Add(txtInvoiceSearch);

            // Search Action Button
            btnSearch = new Button();
            btnSearch.Text = "🔍 Search Invoice";
            btnSearch.Size = new Size(330, 38);
            btnSearch.Location = new Point(15, 120);
            Theme.StylePrimaryButton(btnSearch);
            btnSearch.Click += BtnSearch_Click;
            entryPanel.Controls.Add(btnSearch);

            // Bill Details Panel inside Left Panel
            Panel detailsPanel = new Panel();
            detailsPanel.Size = new Size(330, 320);
            detailsPanel.Location = new Point(15, 180);
            detailsPanel.BackColor = Color.FromArgb(17, 24, 39);
            detailsPanel.Padding = new Padding(15);
            detailsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;

            Label lblDetailsHeader = new Label();
            lblDetailsHeader.Text = "Bill Details Summary";
            lblDetailsHeader.Location = new Point(15, 15);
            lblDetailsHeader.AutoSize = true;
            Theme.StyleLabel(lblDetailsHeader, Theme.TextDark, Theme.BoldFont);
            detailsPanel.Controls.Add(lblDetailsHeader);

            int startY = 55;
            int gapY = 60;

            // Customer Info
            Label lblCustomer = new Label();
            lblCustomer.Text = "Customer:";
            lblCustomer.Location = new Point(15, startY);
            lblCustomer.AutoSize = true;
            Theme.StyleLabel(lblCustomer, Theme.TextDark, Theme.MainFont);
            detailsPanel.Controls.Add(lblCustomer);

            lblCustomerVal = new Label();
            lblCustomerVal.Text = "--";
            lblCustomerVal.Location = new Point(15, startY + 20);
            lblCustomerVal.Size = new Size(300, 20);
            Theme.StyleLabel(lblCustomerVal, Theme.TextLight, Theme.BoldFont);
            detailsPanel.Controls.Add(lblCustomerVal);

            // Date
            Label lblDate = new Label();
            lblDate.Text = "Date of Checkout:";
            lblDate.Location = new Point(15, startY + gapY);
            lblDate.AutoSize = true;
            Theme.StyleLabel(lblDate, Theme.TextDark, Theme.MainFont);
            detailsPanel.Controls.Add(lblDate);

            lblDateVal = new Label();
            lblDateVal.Text = "--";
            lblDateVal.Location = new Point(15, startY + gapY + 20);
            lblDateVal.Size = new Size(300, 20);
            Theme.StyleLabel(lblDateVal, Theme.TextLight, Theme.BoldFont);
            detailsPanel.Controls.Add(lblDateVal);

            // Total Amount
            Label lblOriginalTotal = new Label();
            lblOriginalTotal.Text = "Original Grand Total:";
            lblOriginalTotal.Location = new Point(15, startY + gapY * 2);
            lblOriginalTotal.AutoSize = true;
            Theme.StyleLabel(lblOriginalTotal, Theme.TextDark, Theme.MainFont);
            detailsPanel.Controls.Add(lblOriginalTotal);

            lblOriginalTotalVal = new Label();
            lblOriginalTotalVal.Text = "--";
            lblOriginalTotalVal.Location = new Point(15, startY + gapY * 2 + 20);
            lblOriginalTotalVal.Size = new Size(300, 20);
            Theme.StyleLabel(lblOriginalTotalVal, Theme.TextLight, Theme.BoldFont);
            detailsPanel.Controls.Add(lblOriginalTotalVal);

            // Payment Mode
            Label lblPaymentMode = new Label();
            lblPaymentMode.Text = "Payment Mode:";
            lblPaymentMode.Location = new Point(15, startY + gapY * 3);
            lblPaymentMode.AutoSize = true;
            Theme.StyleLabel(lblPaymentMode, Theme.TextDark, Theme.MainFont);
            detailsPanel.Controls.Add(lblPaymentMode);

            lblPaymentModeVal = new Label();
            lblPaymentModeVal.Text = "--";
            lblPaymentModeVal.Location = new Point(15, startY + gapY * 3 + 20);
            lblPaymentModeVal.Size = new Size(300, 20);
            Theme.StyleLabel(lblPaymentModeVal, Theme.TextLight, Theme.BoldFont);
            detailsPanel.Controls.Add(lblPaymentModeVal);

            entryPanel.Controls.Add(detailsPanel);
            this.Controls.Add(entryPanel);

            // RIGHT PANEL: Cart Grid & Refund Summary
            gridReturnItems = new DataGridView();
            gridReturnItems.Size = new Size(530, 340);
            gridReturnItems.Location = new Point(400, 65);
            gridReturnItems.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridReturnItems);
            gridReturnItems.CellValidating += GridReturnItems_CellValidating;
            gridReturnItems.CellValueChanged += GridReturnItems_CellValueChanged;
            gridReturnItems.CurrentCellDirtyStateChanged += GridReturnItems_CurrentCellDirtyStateChanged;
            gridReturnItems.EditingControlShowing += GridReturnItems_EditingControlShowing;
            this.Controls.Add(gridReturnItems);

            // Refund Summary Panel
            Panel refundPanel = Theme.CreateCard(530, 150);
            refundPanel.Location = new Point(400, 435);
            refundPanel.BackColor = Color.FromArgb(17, 24, 39);
            refundPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Label lblRefundHeader = new Label();
            lblRefundHeader.Text = "TOTAL ESTIMATED REFUND";
            lblRefundHeader.Location = new Point(15, 15);
            Theme.StyleLabel(lblRefundHeader, Theme.TextDark, Theme.BoldFont);
            refundPanel.Controls.Add(lblRefundHeader);

            lblRefundTotal = new Label();
            lblRefundTotal.Text = "Rs. 0.00";
            lblRefundTotal.Location = new Point(15, 45);
            lblRefundTotal.Size = new Size(240, 50);
            Theme.StyleLabel(lblRefundTotal, Theme.Success, new Font("Segoe UI", 24F, FontStyle.Bold));
            refundPanel.Controls.Add(lblRefundTotal);

            btnProcessRefund = new Button();
            btnProcessRefund.Text = "🔄 PROCESS RETURN";
            btnProcessRefund.Size = new Size(220, 55);
            btnProcessRefund.Location = new Point(295, 45);
            btnProcessRefund.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleSuccessButton(btnProcessRefund);
            btnProcessRefund.Click += BtnProcessRefund_Click;
            refundPanel.Controls.Add(btnProcessRefund);

            this.Controls.Add(refundPanel);
        }

        private void InitializeReturnTable()
        {
            returnTable = new DataTable();
            returnTable.Columns.Add("ProductId", typeof(int));
            returnTable.Columns.Add("Code", typeof(string));
            returnTable.Columns.Add("Name", typeof(string));
            returnTable.Columns.Add("SoldQty", typeof(int));
            returnTable.Columns.Add("ReturnedQty", typeof(int));
            returnTable.Columns.Add("Price", typeof(decimal));
            returnTable.Columns.Add("ReturnQty", typeof(int));
            returnTable.Columns.Add("Condition", typeof(string));

            // Set DataTable Columns writable explicitly to prevent read-only locking
            returnTable.Columns["ReturnQty"].ReadOnly = false;
            returnTable.Columns["Condition"].ReadOnly = false;

            gridReturnItems.DataSource = returnTable;

            // Make only specific columns visible/editable
            if (gridReturnItems.Columns["ProductId"] != null) gridReturnItems.Columns["ProductId"].Visible = false;

            gridReturnItems.ReadOnly = false;
            gridReturnItems.EditMode = DataGridViewEditMode.EditOnEnter;

            foreach (DataGridViewColumn col in gridReturnItems.Columns)
            {
                if (col.Name == "ReturnQty" || col.Name == "Condition")
                {
                    col.ReadOnly = false;
                }
                else
                {
                    col.ReadOnly = true;
                }
            }

            // Replace standard Condition column with a Combobox Column for resellable/damaged condition
            if (gridReturnItems.Columns["Condition"] != null)
            {
                gridReturnItems.Columns.Remove("Condition");

                DataGridViewComboBoxColumn comboCol = new DataGridViewComboBoxColumn();
                comboCol.Name = "Condition";
                comboCol.HeaderText = "Item Condition";
                comboCol.DataPropertyName = "Condition";
                comboCol.Items.AddRange(new string[] { "Resellable", "Damaged" });
                comboCol.FlatStyle = FlatStyle.Flat;
                comboCol.DefaultCellStyle.BackColor = Theme.Primary;
                comboCol.DefaultCellStyle.ForeColor = Theme.TextLight;
                comboCol.ReadOnly = false;
                gridReturnItems.Columns.Add(comboCol);
            }

            // Set Header Texts
            if (gridReturnItems.Columns["Code"] != null) gridReturnItems.Columns["Code"].HeaderText = "Code";
            if (gridReturnItems.Columns["Name"] != null) gridReturnItems.Columns["Name"].HeaderText = "Product Name";
            if (gridReturnItems.Columns["SoldQty"] != null) gridReturnItems.Columns["SoldQty"].HeaderText = "Sold Qty";
            if (gridReturnItems.Columns["ReturnedQty"] != null) gridReturnItems.Columns["ReturnedQty"].HeaderText = "Returned";
            if (gridReturnItems.Columns["Price"] != null)
            {
                gridReturnItems.Columns["Price"].HeaderText = "Refund Price";
                gridReturnItems.Columns["Price"].DefaultCellStyle.Format = "N2";
            }
            if (gridReturnItems.Columns["ReturnQty"] != null) gridReturnItems.Columns["ReturnQty"].HeaderText = "Return Qty";
            if (gridReturnItems.Columns["Condition"] != null) gridReturnItems.Columns["Condition"].HeaderText = "Item Condition";

            // Configure Fill Weights for elegant proportional sizing
            if (gridReturnItems.Columns["Code"] != null) gridReturnItems.Columns["Code"].FillWeight = 40;
            if (gridReturnItems.Columns["Name"] != null) gridReturnItems.Columns["Name"].FillWeight = 160;
            if (gridReturnItems.Columns["SoldQty"] != null) gridReturnItems.Columns["SoldQty"].FillWeight = 60;
            if (gridReturnItems.Columns["ReturnedQty"] != null) gridReturnItems.Columns["ReturnedQty"].FillWeight = 65;
            if (gridReturnItems.Columns["Price"] != null) gridReturnItems.Columns["Price"].FillWeight = 65;
            if (gridReturnItems.Columns["ReturnQty"] != null) gridReturnItems.Columns["ReturnQty"].FillWeight = 70;
            if (gridReturnItems.Columns["Condition"] != null) gridReturnItems.Columns["Condition"].FillWeight = 110;
        }

        private void TxtInvoiceSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnSearch_Click(null, null);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string invoiceNum = txtInvoiceSearch.Text.Trim();
            if (string.IsNullOrEmpty(invoiceNum))
            {
                MessageBox.Show("Please enter an Invoice Number to search.", "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Load Sales Invoice details
                    string salesSql = @"
                        SELECT s.Id, c.Name as CustomerName, s.SaleDate, s.SubTotal, s.Discount, s.GrandTotal, s.PaymentMethod
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE s.InvoiceNumber = @invNum";

                    using (SqlCommand cmd = new SqlCommand(salesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNum);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                activeSaleId = Convert.ToInt32(rdr["Id"]);
                                lblCustomerVal.Text = rdr["CustomerName"].ToString();
                                lblDateVal.Text = Convert.ToDateTime(rdr["SaleDate"]).ToString("yyyy-MM-dd HH:mm");
                                lblOriginalTotalVal.Text = $"Rs. {Convert.ToDecimal(rdr["GrandTotal"]):N2}";
                                lblPaymentModeVal.Text = rdr["PaymentMethod"].ToString();

                                decimal subTotal = Convert.ToDecimal(rdr["SubTotal"]);
                                decimal discount = Convert.ToDecimal(rdr["Discount"]);
                                discountRatio = subTotal > 0 ? Math.Min(discount / subTotal, 1) : 0;
                            }
                            else
                            {
                                MessageBox.Show("Sales Invoice not found. Please verify the invoice number.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                ResetInvoiceSummaries();
                                return;
                            }
                        }
                    }

                    // 2. Load Invoice Items & already-returned quantities
                    string itemsSql = @"
                        SELECT sd.ProductId, p.Code, p.Name, sd.Quantity as SoldQty, 
                               ISNULL((SELECT SUM(srd.Quantity) 
                                       FROM SalesReturnDetails srd 
                                       INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id 
                                       WHERE sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId), 0) as AlreadyReturnedQty,
                               sd.UnitPrice as Price
                        FROM SaleDetails sd
                        INNER JOIN Products p ON sd.ProductId = p.Id
                        WHERE sd.SaleId = @saleId";

                    returnTable.Clear();
                    using (SqlCommand cmd = new SqlCommand(itemsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@saleId", activeSaleId);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                decimal originalPrice = Convert.ToDecimal(rdr["Price"]);
                                decimal refundPrice = Math.Round(originalPrice - (originalPrice * discountRatio), 2);

                                returnTable.Rows.Add(
                                    rdr["ProductId"],
                                    rdr["Code"],
                                    rdr["Name"],
                                    rdr["SoldQty"],
                                    rdr["AlreadyReturnedQty"],
                                    refundPrice,
                                    0, // ReturnQty defaults to 0
                                    "Resellable" // Default condition
                                );
                            }
                        }
                    }

                    CalculateRefundTotals();

                    // Re-enforce writable columns after loading data to prevent WinForms read-only auto-inference locking
                    gridReturnItems.ReadOnly = false;
                    foreach (DataGridViewColumn col in gridReturnItems.Columns)
                    {
                        if (col.Name == "ReturnQty" || col.Name == "Condition")
                        {
                            col.ReadOnly = false;
                        }
                        else
                        {
                            col.ReadOnly = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving invoice details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetInvoiceSummaries()
        {
            activeSaleId = 0;
            discountRatio = 0;
            lblCustomerVal.Text = "--";
            lblDateVal.Text = "--";
            lblOriginalTotalVal.Text = "--";
            lblPaymentModeVal.Text = "--";
            returnTable.Clear();
            CalculateRefundTotals();
        }

        private void GridReturnItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (gridReturnItems.Columns[e.ColumnIndex].Name == "ReturnQty")
            {
                string input = e.FormattedValue.ToString();
                if (string.IsNullOrEmpty(input)) return;

                if (!int.TryParse(input, out int returnQty) || returnQty < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative integer for Return Quantity.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                int soldQty = Convert.ToInt32(gridReturnItems.Rows[e.RowIndex].Cells["SoldQty"].Value);
                int returnedQty = Convert.ToInt32(gridReturnItems.Rows[e.RowIndex].Cells["ReturnedQty"].Value);

                if (returnQty + returnedQty > soldQty)
                {
                    int maxAllowed = soldQty - returnedQty;
                    MessageBox.Show($"Return exceeds original transaction boundaries!\nSold Quantity: {soldQty}\nAlready Returned: {returnedQty}\nMax quantity you can return is: {maxAllowed} unit(s)", "Exceeds Original Sale Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void GridReturnItems_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (gridReturnItems.CurrentCell != null && gridReturnItems.Columns[gridReturnItems.CurrentCell.ColumnIndex].Name == "ReturnQty")
            {
                TextBox txt = e.Control as TextBox;
                if (txt != null)
                {
                    txt.TextChanged -= ReturnQtyTextBox_TextChanged;
                    txt.TextChanged += ReturnQtyTextBox_TextChanged;
                }
            }
        }

        private void ReturnQtyTextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null || gridReturnItems.CurrentCell == null) return;

            int activeRowIndex = gridReturnItems.CurrentCell.RowIndex;
            string typedText = txt.Text.Trim();

            // Parse typed value, default to 0 if empty/invalid
            int currentTypedQty = 0;
            int.TryParse(typedText, out currentTypedQty);

            decimal liveRefundTotalAmount = 0;
            foreach (DataGridViewRow gridRow in gridReturnItems.Rows)
            {
                DataRowView rowView = gridRow.DataBoundItem as DataRowView;
                if (rowView == null) continue;

                decimal price = Convert.ToDecimal(rowView["Price"]);
                if (gridRow.Index == activeRowIndex)
                {
                    liveRefundTotalAmount += currentTypedQty * price;
                }
                else
                {
                    int returnQty = Convert.ToInt32(rowView["ReturnQty"]);
                    liveRefundTotalAmount += returnQty * price;
                }
            }
            lblRefundTotal.Text = $"Rs. {liveRefundTotalAmount:N2}";
        }

        private void GridReturnItems_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridReturnItems.IsCurrentCellDirty)
            {
                // Only commit instantly for ComboBox column changes. 
                // Do NOT commit for TextBox column (ReturnQty) while the user is typing their numeric input.
                if (gridReturnItems.CurrentCell is DataGridViewComboBoxCell)
                {
                    gridReturnItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            }
        }

        private void GridReturnItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridReturnItems.Columns[e.ColumnIndex].Name == "ReturnQty" || gridReturnItems.Columns[e.ColumnIndex].Name == "Condition")
            {
                CalculateRefundTotals();
            }
        }

        private void CalculateRefundTotals()
        {
            refundTotalAmount = 0;
            foreach (DataRow row in returnTable.Rows)
            {
                int returnQty = Convert.ToInt32(row["ReturnQty"]);
                decimal price = Convert.ToDecimal(row["Price"]);
                refundTotalAmount += returnQty * price;
            }
            lblRefundTotal.Text = $"Rs. {refundTotalAmount:N2}";
        }

        private void BtnProcessRefund_Click(object sender, EventArgs e)
        {
            if (activeSaleId == 0)
            {
                MessageBox.Show("Please search and load a valid sales invoice first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if there is anything to return
            bool hasItemsToReturn = false;
            foreach (DataRow row in returnTable.Rows)
            {
                int returnQty = Convert.ToInt32(row["ReturnQty"]);
                if (returnQty > 0)
                {
                    hasItemsToReturn = true;
                    break;
                }
            }

            if (!hasItemsToReturn)
            {
                MessageBox.Show("Please enter a return quantity greater than 0 for at least one item.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to process this return transaction?\nTotal Refund: Rs. {refundTotalAmount:N2}", "Confirm Return Transaction", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string returnNumber = $"RET-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Fetch CustomerId and current DueAmount of the sale
                    int? customerId = null;
                    decimal saleDueAmount = 0;
                    string getSaleDetailsSql = "SELECT CustomerId, DueAmount FROM Sales WHERE Id = @saleId";
                    using (SqlCommand cmd = new SqlCommand(getSaleDetailsSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@saleId", activeSaleId);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0)) customerId = rdr.GetInt32(0);
                                saleDueAmount = rdr.GetDecimal(1);
                            }
                        }
                    }

                    decimal dueOffset = 0;
                    decimal cashRefund = refundTotalAmount;

                    if (customerId.HasValue && saleDueAmount > 0)
                    {
                        // Get overall customer outstanding due balance
                        decimal customerNetDue = 0;
                        string getNetDueSql = @"
                            SELECT (ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = @custId), 0) -
                                    ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE CustomerId = @custId), 0))";
                        using (SqlCommand cmd = new SqlCommand(getNetDueSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId.Value);
                            object res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value)
                            {
                                customerNetDue = Convert.ToDecimal(res);
                            }
                        }

                        decimal maxDueOffset = saleDueAmount;
                        if (customerNetDue < maxDueOffset)
                        {
                            maxDueOffset = customerNetDue;
                        }
                        if (maxDueOffset < 0) maxDueOffset = 0;

                        dueOffset = Math.Min(refundTotalAmount, maxDueOffset);
                        cashRefund = refundTotalAmount - dueOffset;

                        if (dueOffset > 0)
                        {
                            // Update the invoice's outstanding DueAmount
                            string updateSaleDueSql = "UPDATE Sales SET DueAmount = DueAmount - @offset WHERE Id = @saleId";
                            using (SqlCommand cmd = new SqlCommand(updateSaleDueSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@offset", dueOffset);
                                cmd.Parameters.AddWithValue("@saleId", activeSaleId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // 1. Insert Sales Returns Header
                    int returnId = 0;
                    string returnHeaderSql = @"
                        INSERT INTO SalesReturns (ReturnNumber, SaleId, ReturnDate, TotalRefund, CashRefund, CreatedBy)
                        OUTPUT INSERTED.Id
                        VALUES (@retNum, @saleId, GETDATE(), @refund, @cashRefund, @userId)";

                    using (SqlCommand cmd = new SqlCommand(returnHeaderSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@retNum", returnNumber);
                        cmd.Parameters.AddWithValue("@saleId", activeSaleId);
                        cmd.Parameters.AddWithValue("@refund", refundTotalAmount);
                        cmd.Parameters.AddWithValue("@cashRefund", cashRefund);
                        cmd.Parameters.AddWithValue("@userId", Session.UserId);
                        returnId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Loop line items to insert Details and Restock Products
                    foreach (DataRow row in returnTable.Rows)
                    {
                        int prodId = (int)row["ProductId"];
                        int returnQty = (int)row["ReturnQty"];
                        decimal price = (decimal)row["Price"];
                        string condition = row["Condition"].ToString();

                        if (returnQty <= 0) continue;

                        // Insert SalesReturnDetails
                        string returnDetailsSql = @"
                            INSERT INTO SalesReturnDetails (ReturnId, ProductId, Quantity, RefundPrice, Total, ItemCondition)
                            VALUES (@retId, @prodId, @qty, @refPrice, @total, @condition)";

                        using (SqlCommand cmd = new SqlCommand(returnDetailsSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@retId", returnId);
                            cmd.Parameters.AddWithValue("@prodId", prodId);
                            cmd.Parameters.AddWithValue("@qty", returnQty);
                            cmd.Parameters.AddWithValue("@refPrice", price);
                            cmd.Parameters.AddWithValue("@total", returnQty * price);
                            cmd.Parameters.AddWithValue("@condition", condition);
                            cmd.ExecuteNonQuery();
                        }

                        // Restock Products if condition is 'Resellable'
                        if (condition == "Resellable")
                        {
                            string restockSql = @"
                                UPDATE Products
                                SET Stock = Stock + @qty
                                WHERE Id = @prodId";

                            using (SqlCommand cmd = new SqlCommand(restockSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@qty", returnQty);
                                cmd.Parameters.AddWithValue("@prodId", prodId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();

                    string successMsg = $"Sales return processed successfully!\nReturn Receipt: {returnNumber}\nTotal Refund: Rs. {refundTotalAmount:N2}";
                    if (dueOffset > 0)
                    {
                        successMsg += $"\nAmount Offset from Dues: Rs. {dueOffset:N2}\nCash Refund Paid: Rs. {cashRefund:N2}";
                    }
                    else
                    {
                        successMsg += $"\nCash Refund Paid: Rs. {cashRefund:N2}";
                    }
                    MessageBox.Show(successMsg, "Return Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Reset screen
                    txtInvoiceSearch.Clear();
                    ResetInvoiceSummaries();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Failed to process sales return: {ex.Message}", "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
