using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class SupplierControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridSuppliers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;

        public SupplierControl()
        {
            InitializeComponent();
            LoadSuppliers();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Supplier Directory";
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
            gridSuppliers = new DataGridView();
            gridSuppliers.Size = new Size(910, 505);
            gridSuppliers.Location = new Point(20, 125);
            gridSuppliers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridSuppliers);
            this.Controls.Add(gridSuppliers);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Supplier";
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

        private void LoadSuppliers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Name, ContactPerson as [Contact Person], Phone, Email, Address, CreatedAt as [Registered Date] 
                        FROM Suppliers 
                        WHERE Name LIKE @search OR ContactPerson LIKE @search OR Phone LIKE @search
                        ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridSuppliers.DataSource = dt;
                        }
                    }
                }

                // Hide Id column beautifully
                if (gridSuppliers.Columns["Id"] != null)
                {
                    gridSuppliers.Columns["Id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadSuppliers();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (SupplierDialog dlg = new SupplierDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadSuppliers();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridSuppliers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a supplier to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridSuppliers.SelectedRows[0];
            int supplierId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();
            string contactPerson = selectedRow.Cells["Contact Person"].Value?.ToString() ?? "";
            string phone = selectedRow.Cells["Phone"].Value?.ToString() ?? "";
            string email = selectedRow.Cells["Email"].Value?.ToString() ?? "";
            string address = selectedRow.Cells["Address"].Value?.ToString() ?? "";

            using (SupplierDialog dlg = new SupplierDialog(supplierId, name, contactPerson, phone, email, address))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadSuppliers();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridSuppliers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a supplier to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridSuppliers.SelectedRows[0];
            int supplierId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string supplierName = selectedRow.Cells["Name"].Value.ToString();

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete supplier '{supplierName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Suppliers WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", supplierId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadSuppliers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting supplier: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Supplier Dialog for modal add/edit
        private class SupplierDialog : Form
        {
            private int? supplierId = null;
            private TextBox txtName;
            private TextBox txtContactPerson;
            private TextBox txtPhone;
            private TextBox txtEmail;
            private TextBox txtAddress;
            private Button btnSave;
            private Button btnCancel;

            public SupplierDialog()
            {
                InitializeComponent("Add Supplier");
            }

            public SupplierDialog(int id, string name, string contactPerson, string phone, string email, string address)
            {
                this.supplierId = id;
                InitializeComponent("Edit Supplier");
                txtName.Text = name;
                txtContactPerson.Text = contactPerson;
                txtPhone.Text = phone;
                txtEmail.Text = email;
                txtAddress.Text = address;
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(400, 540);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Form header
                Label lblHeader = new Label();
                lblHeader.Text = title;
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Fields placement
                int startY = 80;
                int gapY = 70;

                // Name
                Label lblName = new Label();
                lblName.Text = "Company / Supplier Name *";
                lblName.Location = new Point(20, startY);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(340, 30);
                txtName.Location = new Point(20, startY + 25);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Contact Person
                Label lblContact = new Label();
                lblContact.Text = "Contact Person";
                lblContact.Location = new Point(20, startY + gapY);
                lblContact.AutoSize = true;
                Theme.StyleLabel(lblContact, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblContact);

                txtContactPerson = new TextBox();
                txtContactPerson.Size = new Size(340, 30);
                txtContactPerson.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtContactPerson);
                this.Controls.Add(txtContactPerson);

                // Phone
                Label lblPhone = new Label();
                lblPhone.Text = "Phone Number";
                lblPhone.Location = new Point(20, startY + (gapY * 2));
                lblPhone.AutoSize = true;
                Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPhone);

                txtPhone = new TextBox();
                txtPhone.Size = new Size(340, 30);
                txtPhone.Location = new Point(20, startY + (gapY * 2) + 25);
                Theme.StyleTextBox(txtPhone);
                this.Controls.Add(txtPhone);

                // Email
                Label lblEmail = new Label();
                lblEmail.Text = "Email Address";
                lblEmail.Location = new Point(20, startY + (gapY * 3));
                lblEmail.AutoSize = true;
                Theme.StyleLabel(lblEmail, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblEmail);

                txtEmail = new TextBox();
                txtEmail.Size = new Size(340, 30);
                txtEmail.Location = new Point(20, startY + (gapY * 3) + 25);
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
                txtAddress.Size = new Size(340, 30);
                txtAddress.Location = new Point(20, startY + (gapY * 4) + 25);
                Theme.StyleTextBox(txtAddress);
                this.Controls.Add(txtAddress);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Details";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 440);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 440);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtName.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string contactPerson = txtContactPerson.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string email = txtEmail.Text.Trim();
                string address = txtAddress.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Supplier Company Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        if (supplierId == null)
                        {
                            // INSERT
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address) 
                                VALUES (@name, @contact, @phone, @email, @address)", conn))
                            {
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@contact", contactPerson);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // UPDATE
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Suppliers 
                                SET Name = @name, ContactPerson = @contact, Phone = @phone, Email = @email, Address = @address 
                                WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", supplierId.Value);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@contact", contactPerson);
                                cmd.Parameters.AddWithValue("@phone", phone);
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
                    MessageBox.Show($"Error saving supplier details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
