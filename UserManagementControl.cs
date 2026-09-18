using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class UserManagementControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridUsers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;

        public UserManagementControl()
        {
            InitializeComponent();
            LoadUsers();
            this.Load += (s, e) => txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 650);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Employee Access & User Management";
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
            gridUsers = new DataGridView();
            gridUsers.Size = new Size(910, 505);
            gridUsers.Location = new Point(20, 125);
            gridUsers.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridUsers);
            this.Controls.Add(gridUsers);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 65);
            actionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnAdd = new Button();
            btnAdd.Text = "+ Add User";
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

        private void LoadUsers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Username, FullName as [Full Name], Role, CreatedAt as [Created Date] 
                        FROM Users 
                        WHERE Username LIKE @search OR FullName LIKE @search
                        ORDER BY Username ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridUsers.DataSource = dt;
                        }
                    }
                }

                if (gridUsers.Columns["Id"] != null)
                {
                    gridUsers.Columns["Id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadUsers();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (UserDialog dlg = new UserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadUsers();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridUsers.SelectedRows[0];
            int userId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string username = selectedRow.Cells["Username"].Value.ToString();
            string fullName = selectedRow.Cells["Full Name"].Value.ToString();
            string role = selectedRow.Cells["Role"].Value.ToString();

            using (UserDialog dlg = new UserDialog(userId, username, fullName, role))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadUsers();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridUsers.SelectedRows[0];
            int userId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string username = selectedRow.Cells["Username"].Value.ToString();

            if (string.Equals(username, Session.Username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot delete your own active session account.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if this is the last Admin
            bool isTargetAdmin = string.Equals(selectedRow.Cells["Role"].Value.ToString(), "Admin", StringComparison.OrdinalIgnoreCase);
            if (isTargetAdmin)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Admin'", conn))
                        {
                            int adminCount = (int)cmd.ExecuteScalar();
                            if (adminCount <= 1)
                            {
                                MessageBox.Show("Cannot delete the last remaining Administrator account.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error validating administrator count: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete user '{username}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Users WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class UserDialog : Form
        {
            private int? userId = null;
            private TextBox txtUsername;
            private TextBox txtFullName;
            private TextBox txtPassword;
            private ComboBox comboRole;
            private Button btnSave;
            private Button btnCancel;

            public UserDialog()
            {
                InitializeComponent("Add User");
            }

            public UserDialog(int id, string username, string fullName, string role)
            {
                this.userId = id;
                InitializeComponent("Edit User");
                txtUsername.Text = username;
                txtFullName.Text = fullName;
                comboRole.SelectedItem = role;
                
                // When editing, password is not required.
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(400, 480);
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = title;
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                int startY = 80;
                int gapY = 70;

                // Username
                Label lblUsername = new Label();
                lblUsername.Text = "Username *";
                lblUsername.Location = new Point(20, startY);
                lblUsername.AutoSize = true;
                Theme.StyleLabel(lblUsername, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblUsername);

                txtUsername = new TextBox();
                txtUsername.Size = new Size(340, 30);
                txtUsername.Location = new Point(20, startY + 25);
                Theme.StyleTextBox(txtUsername);
                this.Controls.Add(txtUsername);

                // Full Name
                Label lblFullName = new Label();
                lblFullName.Text = "Full Name *";
                lblFullName.Location = new Point(20, startY + gapY);
                lblFullName.AutoSize = true;
                Theme.StyleLabel(lblFullName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblFullName);

                txtFullName = new TextBox();
                txtFullName.Size = new Size(340, 30);
                txtFullName.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtFullName);
                this.Controls.Add(txtFullName);

                // Password
                Label lblPassword = new Label();
                lblPassword.Text = userId == null ? "Password *" : "New Password";
                lblPassword.Location = new Point(20, startY + (gapY * 2));
                lblPassword.AutoSize = true;
                Theme.StyleLabel(lblPassword, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPassword);

                txtPassword = new TextBox();
                txtPassword.Size = new Size(340, 30);
                txtPassword.Location = new Point(20, startY + (gapY * 2) + 25);
                txtPassword.PasswordChar = '●';
                Theme.StyleTextBox(txtPassword);
                this.Controls.Add(txtPassword);

                // Role
                Label lblRole = new Label();
                lblRole.Text = "Role *";
                lblRole.Location = new Point(20, startY + (gapY * 3));
                lblRole.AutoSize = true;
                Theme.StyleLabel(lblRole, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblRole);

                comboRole = new ComboBox();
                comboRole.Size = new Size(340, 30);
                comboRole.Location = new Point(20, startY + (gapY * 3) + 25);
                comboRole.DropDownStyle = ComboBoxStyle.DropDownList;
                comboRole.Items.AddRange(new object[] { "Admin", "Employee" });
                comboRole.SelectedIndex = 1; // Default to Employee
                Theme.StyleComboBox(comboRole);
                this.Controls.Add(comboRole);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Details";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 395);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 395);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
                this.Load += (s, e) => txtUsername.Focus();
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string username = txtUsername.Text.Trim();
                string fullName = txtFullName.Text.Trim();
                string password = txtPassword.Text;
                string role = comboRole.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("All fields marked with * are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (userId == null && string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Password is required for new user accounts.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Check uniqueness of username
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @username AND (@id IS NULL OR Id <> @id)", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@username", username);
                            checkCmd.Parameters.AddWithValue("@id", (object)userId ?? DBNull.Value);
                            int count = (int)checkCmd.ExecuteScalar();
                            if (count > 0)
                            {
                                MessageBox.Show("Username already exists. Please choose a different username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        if (userId == null)
                        {
                            // INSERT
                            string hash = DatabaseHelper.HashPassword(password);
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Users (Username, PasswordHash, FullName, Role) 
                                VALUES (@username, @hash, @fullName, @role)", conn))
                            {
                                cmd.Parameters.AddWithValue("@username", username);
                                cmd.Parameters.AddWithValue("@hash", hash);
                                cmd.Parameters.AddWithValue("@fullName", fullName);
                                cmd.Parameters.AddWithValue("@role", role);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // UPDATE
                            string query = @"
                                UPDATE Users 
                                SET Username = @username, FullName = @fullName, Role = @role";
                            
                            if (!string.IsNullOrEmpty(password))
                            {
                                query += ", PasswordHash = @hash";
                            }

                            query += " WHERE Id = @id";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", userId.Value);
                                cmd.Parameters.AddWithValue("@username", username);
                                cmd.Parameters.AddWithValue("@fullName", fullName);
                                cmd.Parameters.AddWithValue("@role", role);
                                if (!string.IsNullOrEmpty(password))
                                {
                                    cmd.Parameters.AddWithValue("@hash", DatabaseHelper.HashPassword(password));
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving user details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}