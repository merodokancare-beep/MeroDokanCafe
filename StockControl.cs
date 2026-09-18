using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class StockControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridStock;
        private Button btnAdjust;
        private CheckBox chkLowStockOnly;

        public StockControl()
        {
            InitializeComponent();
            
            // Restrict Manual Stock Adjustment to Admin users only
            if (!string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                btnAdjust.Visible = false;
            }

            LoadStock();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Inventory Stock Register";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Search Panel
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
            gridStock = new DataGridView();
            gridStock.Size = new Size(910, 505);
            gridStock.Location = new Point(20, 125);
            gridStock.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridStock);
            gridStock.DataBindingComplete += GridStock_DataBindingComplete;
            this.Controls.Add(gridStock);

            // Action Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnAdjust = new Button();
            btnAdjust.Text = "⚙️ Manual Stock Adjustment";
            btnAdjust.Size = new Size(240, 40);
            btnAdjust.Location = new Point(0, 0);
            Theme.StylePrimaryButton(btnAdjust);
            btnAdjust.Click += BtnAdjust_Click;
            actionPanel.Controls.Add(btnAdjust);

            // Checkbox for Low Stock Filter
            chkLowStockOnly = new CheckBox();
            chkLowStockOnly.Text = "Show Low Stock Alerts Only";
            chkLowStockOnly.Location = new Point(260, 8);
            chkLowStockOnly.Size = new Size(250, 24);
            chkLowStockOnly.Font = Theme.BoldFont;
            chkLowStockOnly.ForeColor = Theme.Warning;
            chkLowStockOnly.CheckedChanged += ChkLowStockOnly_CheckedChanged;
            actionPanel.Controls.Add(chkLowStockOnly);

            this.Controls.Add(actionPanel);
        }

        private void LoadStock()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Code, Name, Category, Stock as [Current Stock], MinStockLevel as [Min Level],
                               CASE WHEN Stock <= MinStockLevel THEN 'CRITICAL' ELSE 'OPTIMAL' END as [Status]
                        FROM Products 
                        WHERE (Code LIKE @search OR Name LIKE @search)";

                    if (chkLowStockOnly.Checked)
                    {
                        query += " AND Stock <= MinStockLevel";
                    }

                    query += " ORDER BY Stock ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridStock.DataSource = dt;
                        }
                    }
                }

                if (gridStock.Columns["Id"] != null) gridStock.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stock data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadStock();
        }

        private void ChkLowStockOnly_CheckedChanged(object sender, EventArgs e)
        {
            LoadStock();
        }

        private void GridStock_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in gridStock.Rows)
            {
                string status = row.Cells["Status"].Value?.ToString() ?? "";
                if (status == "CRITICAL")
                {
                    // Draw cells or status with warning color
                    row.Cells["Status"].Style.ForeColor = Theme.Danger;
                    row.Cells["Status"].Style.SelectionForeColor = Theme.Danger;
                }
                else
                {
                    row.Cells["Status"].Style.ForeColor = Theme.Success;
                    row.Cells["Status"].Style.SelectionForeColor = Theme.Success;
                }
            }
        }

        private void BtnAdjust_Click(object sender, EventArgs e)
        {
            if (gridStock.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product row to adjust stock.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridStock.SelectedRows[0];
            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string code = selectedRow.Cells["Code"].Value.ToString();
            string name = selectedRow.Cells["Name"].Value.ToString();
            int currentStock = Convert.ToInt32(selectedRow.Cells["Current Stock"].Value);

            using (AdjustStockDialog dlg = new AdjustStockDialog(id, code, name, currentStock))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadStock();
                }
            }
        }

        // Nested Adjustment Dialog
        private class AdjustStockDialog : Form
        {
            private int productId;
            private int currentStock;
            private Label lblCurrentStock;
            private TextBox txtNewStock;
            private TextBox txtRemarks;
            private Button btnSave;
            private Button btnCancel;

            public AdjustStockDialog(int id, string code, string name, int curStock)
            {
                this.productId = id;
                this.currentStock = curStock;
                InitializeComponent(code, name);
                this.Load += (s, e) => txtNewStock.Focus();
            }

            private void InitializeComponent(string code, string name)
            {
                this.Text = "Adjust Inventory Stock Level";
                this.ClientSize = new Size(420, 360);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "Stock Adjustment";
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Info Label
                Label lblInfo = new Label();
                lblInfo.Text = $"{name} ({code})";
                lblInfo.Location = new Point(20, 60);
                lblInfo.Size = new Size(360, 20);
                Theme.StyleLabel(lblInfo, Theme.TextDark, Theme.BoldFont);
                this.Controls.Add(lblInfo);

                // Current stock label
                lblCurrentStock = new Label();
                lblCurrentStock.Text = $"Current Stock Level: {currentStock} unit(s)";
                lblCurrentStock.Location = new Point(20, 95);
                lblCurrentStock.AutoSize = true;
                Theme.StyleLabel(lblCurrentStock, Theme.TextLight, Theme.MainFont);
                this.Controls.Add(lblCurrentStock);

                // New Stock
                Label lblNew = new Label();
                lblNew.Text = "New Absolute Stock Count *";
                lblNew.Location = new Point(20, 135);
                lblNew.AutoSize = true;
                Theme.StyleLabel(lblNew, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblNew);

                txtNewStock = new TextBox();
                txtNewStock.Size = new Size(360, 30);
                txtNewStock.Location = new Point(20, 160);
                Theme.StyleTextBox(txtNewStock);
                txtNewStock.Text = currentStock.ToString();
                this.Controls.Add(txtNewStock);

                // Remarks
                Label lblRem = new Label();
                lblRem.Text = "Adjustment Reason / Remarks";
                lblRem.Location = new Point(20, 200);
                lblRem.AutoSize = true;
                Theme.StyleLabel(lblRem, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblRem);

                txtRemarks = new TextBox();
                txtRemarks.Size = new Size(360, 30);
                txtRemarks.Location = new Point(20, 225);
                Theme.StyleTextBox(txtRemarks);
                this.Controls.Add(txtRemarks);

                // Action Buttons
                btnSave = new Button();
                btnSave.Text = "Confirm Adjustment";
                btnSave.Size = new Size(170, 40);
                btnSave.Location = new Point(20, 280);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(170, 40);
                btnCancel.Location = new Point(210, 280);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                if (!int.TryParse(txtNewStock.Text.Trim(), out int newStock) || newStock < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative integer for new stock.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Products 
                            SET Stock = @stock 
                            WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", productId);
                            cmd.Parameters.AddWithValue("@stock", newStock);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adjusting stock: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
