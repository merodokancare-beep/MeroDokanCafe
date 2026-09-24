using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class StaffControl : UserControl, IFocusableControl
    {
        private TextBox txtName;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private ComboBox comboRole;
        private NumericUpDown numCommission;
        private CheckBox chkIsActive;
        private TextBox txtSearch;

        private DataGridView gridStaff;
        private Button btnSave;
        private Button btnClear;
        private Button btnDelete;

        private Label lblTotalStaff;
        private Label lblActiveStaff;

        private int selectedStaffId = 0;

        public StaffControl()
        {
            InitializeComponent();
            LoadRolesDropdown();
            LoadStaff();
            this.Load += (s, e) => FocusDefaultControl();
            this.VisibleChanged += (s, e) => { if (this.Visible) FocusDefaultControl(); };
        }

        public void FocusDefaultControl()
        {
            try
            {
                if (txtName != null && !txtName.IsDisposed && txtName.Visible)
                {
                    txtName.Focus();
                    txtName.SelectAll();
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1000, 680);
            this.AutoScroll = true;
            this.AutoScrollMinSize = new Size(950, 520);
            this.BackColor = Theme.Secondary;

            // Header
            Label lblHeader = new Label();
            lblHeader.Text = "🧑‍🍳 Steward & Staff Management";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Register and manage cafe stewards, waiters, head chefs, captains, and kitchen staff";
            lblSubtitle.Location = new Point(22, 45);
            lblSubtitle.AutoSize = true;
            Theme.StyleLabel(lblSubtitle, Theme.TextDark, Theme.MainFont);
            this.Controls.Add(lblSubtitle);

            // Summary Badges Card (Header Right)
            Panel statCard = Theme.CreateCard(340, 44);
            statCard.Location = new Point(620, 12);
            statCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            
            lblTotalStaff = new Label();
            lblTotalStaff.Text = "🧑‍🍳 Total Staff: 0";
            lblTotalStaff.Location = new Point(15, 12);
            lblTotalStaff.AutoSize = true;
            Theme.StyleLabel(lblTotalStaff, Theme.Accent, Theme.BoldFont);
            statCard.Controls.Add(lblTotalStaff);

            lblActiveStaff = new Label();
            lblActiveStaff.Text = "🟢 Active: 0";
            lblActiveStaff.Location = new Point(180, 12);
            lblActiveStaff.AutoSize = true;
            Theme.StyleLabel(lblActiveStaff, Theme.Success, Theme.BoldFont);
            statCard.Controls.Add(lblActiveStaff);

            this.Controls.Add(statCard);

            // LEFT PANEL: Form Entry Card
            Panel entryPanel = Theme.CreateCard(340, 365);
            entryPanel.Location = new Point(20, 75);
            entryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            entryPanel.AutoScroll = true;

            Label lblCardTitle = new Label();
            lblCardTitle.Text = "Staff / Steward Profile";
            lblCardTitle.Location = new Point(15, 12);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.SubHeaderFont);
            entryPanel.Controls.Add(lblCardTitle);

            int startY = 38;
            int gap = 46;

            // Stylist Full Name
            Label lblName = new Label();
            lblName.Text = "Full Name *";
            lblName.Location = new Point(15, startY);
            lblName.AutoSize = true;
            Theme.StyleLabel(lblName, Theme.TextDark, Theme.BoldFont);
            entryPanel.Controls.Add(lblName);

            txtName = new TextBox();
            txtName.Size = new Size(310, 26);
            txtName.Location = new Point(15, startY + 17);
            Theme.StyleTextBox(txtName);
            entryPanel.Controls.Add(txtName);

            // Phone
            Label lblPhone = new Label();
            lblPhone.Text = "Contact Phone Number *";
            lblPhone.Location = new Point(15, startY + gap);
            lblPhone.AutoSize = true;
            Theme.StyleLabel(lblPhone, Theme.TextDark, Theme.BoldFont);
            entryPanel.Controls.Add(lblPhone);

            txtPhone = new TextBox();
            txtPhone.Size = new Size(310, 26);
            txtPhone.Location = new Point(15, startY + gap + 17);
            Theme.StyleTextBox(txtPhone);
            entryPanel.Controls.Add(txtPhone);

            // Email
            Label lblEmail = new Label();
            lblEmail.Text = "Email Address";
            lblEmail.Location = new Point(15, startY + gap * 2);
            lblEmail.AutoSize = true;
            Theme.StyleLabel(lblEmail, Theme.TextDark, Theme.BoldFont);
            entryPanel.Controls.Add(lblEmail);

            txtEmail = new TextBox();
            txtEmail.Size = new Size(310, 26);
            txtEmail.Location = new Point(15, startY + gap * 2 + 17);
            Theme.StyleTextBox(txtEmail);
            entryPanel.Controls.Add(txtEmail);

            // Role / Specialty
            Label lblRole = new Label();
            lblRole.Text = "Primary Role / Specialty *";
            lblRole.Location = new Point(15, startY + gap * 3);
            lblRole.AutoSize = true;
            Theme.StyleLabel(lblRole, Theme.TextDark, Theme.BoldFont);
            entryPanel.Controls.Add(lblRole);

            comboRole = new ComboBox();
            comboRole.Size = new Size(310, 26);
            comboRole.Location = new Point(15, startY + gap * 3 + 17);
            comboRole.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboRole);
            comboRole.SelectedIndexChanged += (s, e) => {
                if (selectedStaffId == 0 && comboRole.SelectedItem is RoleComboItem rItm)
                {
                    numCommission.Value = Math.Max(0, Math.Min(100, rItm.DefaultComm));
                }
            };
            entryPanel.Controls.Add(comboRole);

            // Commission Rate (%)
            Label lblComm = new Label();
            lblComm.Text = "Service Commission Rate (%)";
            lblComm.Location = new Point(15, startY + gap * 4);
            lblComm.AutoSize = true;
            Theme.StyleLabel(lblComm, Theme.TextDark, Theme.BoldFont);
            entryPanel.Controls.Add(lblComm);

            numCommission = new NumericUpDown();
            numCommission.Size = new Size(310, 26);
            numCommission.Location = new Point(15, startY + gap * 4 + 17);
            numCommission.Minimum = 0;
            numCommission.Maximum = 100;
            numCommission.DecimalPlaces = 2;
            numCommission.Value = 10;
            Theme.StyleNumericUpDown(numCommission);
            entryPanel.Controls.Add(numCommission);

            // Active Status
            chkIsActive = new CheckBox();
            chkIsActive.Text = "Currently Active Staff Member";
            chkIsActive.Location = new Point(15, startY + gap * 5 + 6);
            chkIsActive.Size = new Size(310, 24);
            chkIsActive.Checked = true;
            chkIsActive.ForeColor = Theme.TextLight;
            chkIsActive.Font = Theme.MainFont;
            entryPanel.Controls.Add(chkIsActive);

            // Action Buttons
            btnSave = new Button();
            btnSave.Text = "💾 Save Staff";
            btnSave.Size = new Size(150, 36);
            btnSave.Location = new Point(15, startY + gap * 5 + 34);
            Theme.StyleSuccessButton(btnSave);
            btnSave.Click += BtnSave_Click;
            entryPanel.Controls.Add(btnSave);

            btnClear = new Button();
            btnClear.Text = "🔄 Clear / New";
            btnClear.Size = new Size(150, 36);
            btnClear.Location = new Point(175, startY + gap * 5 + 34);
            Theme.StylePrimaryButton(btnClear);
            btnClear.Click += (s, e) => ResetForm();
            entryPanel.Controls.Add(btnClear);

            this.Controls.Add(entryPanel);

            // RIGHT PANEL: Search and Staff Grid
            Panel rightPanel = new Panel();
            rightPanel.Location = new Point(380, 75);
            rightPanel.Size = new Size(600, 575);
            rightPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Search Bar Card
            Panel searchCard = Theme.CreateCard(590, 50);
            searchCard.Location = new Point(0, 0);
            searchCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label lblSearch = new Label();
            lblSearch.Text = "🔍 Search Staff / Steward / Role:";
            lblSearch.Location = new Point(12, 16);
            lblSearch.AutoSize = true;
            Theme.StyleLabel(lblSearch, Theme.TextDark, Theme.BoldFont);
            searchCard.Controls.Add(lblSearch);

            txtSearch = new TextBox();
            txtSearch.Size = new Size(330, 26);
            txtSearch.Location = new Point(235, 13);
            Theme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadStaff();
            searchCard.Controls.Add(txtSearch);

            rightPanel.Controls.Add(searchCard);

            // DataGridView
            gridStaff = new DataGridView();
            gridStaff.Location = new Point(0, 60);
            gridStaff.Size = new Size(590, 460);
            gridStaff.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridStaff);
            gridStaff.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridStaff.MultiSelect = false;
            gridStaff.CellClick += GridStaff_CellClick;
            rightPanel.Controls.Add(gridStaff);

            // Bottom Actions
            Panel gridActions = new Panel();
            gridActions.Location = new Point(0, 530);
            gridActions.Size = new Size(590, 45);
            gridActions.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected Staff";
            btnDelete.Size = new Size(220, 38);
            btnDelete.Location = new Point(0, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            gridActions.Controls.Add(btnDelete);

            Button btnRefresh = new Button();
            btnRefresh.Text = "🔄 Refresh List";
            btnRefresh.Size = new Size(150, 38);
            btnRefresh.Location = new Point(230, 0);
            Theme.StylePrimaryButton(btnRefresh);
            btnRefresh.Click += (s, e) => LoadStaff();
            gridActions.Controls.Add(btnRefresh);

            rightPanel.Controls.Add(gridActions);

            this.Controls.Add(rightPanel);
        }

        private void LoadStaff()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            Id,
                            Name AS [Staff / Steward Name],
                            Phone AS [Contact Phone],
                            Email AS [Email Address],
                            Role AS [Designation / Role],
                            CommissionRate AS [Incentive %],
                            CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS [Status]
                        FROM Staff 
                        WHERE (Name LIKE @search OR Phone LIKE @search OR Role LIKE @search)
                        ORDER BY IsActive DESC, Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch?.Text.Trim() ?? ""}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridStaff.DataSource = dt;

                            int total = dt.Rows.Count;
                            int active = 0;
                            foreach (DataRow r in dt.Rows)
                            {
                                if (r["Status"].ToString() == "Active") active++;
                            }
                            lblTotalStaff.Text = $"🧑‍🍳 Total Staff: {total}";
                            lblActiveStaff.Text = $"🟢 Active: {active}";
                        }
                    }
                }

                if (gridStaff.Columns["Id"] != null)
                    gridStaff.Columns["Id"].Visible = false;

                if (gridStaff.Columns["Incentive %"] != null)
                    gridStaff.Columns["Incentive %"].DefaultCellStyle.Format = "N2";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading staff list: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class RoleComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal DefaultComm { get; set; }
            public override string ToString() => Name;
        }

        private void LoadRolesDropdown()
        {
            try
            {
                if (comboRole == null) return;
                string currentSelected = comboRole.Text;
                comboRole.Items.Clear();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, RoleName, DefaultCommissionRate FROM StylistRoles WHERE IsActive = 1 ORDER BY RoleName ASC", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                comboRole.Items.Add(new RoleComboItem {
                                    Id = Convert.ToInt32(rdr["Id"]),
                                    Name = rdr["RoleName"].ToString(),
                                    DefaultComm = Convert.ToDecimal(rdr["DefaultCommissionRate"])
                                });
                            }
                        }
                    }
                }

                if (comboRole.Items.Count == 0)
                {
                    comboRole.Items.Add(new RoleComboItem { Id = 1, Name = "Steward", DefaultComm = 0 });
                    comboRole.Items.Add(new RoleComboItem { Id = 2, Name = "Senior Steward / Captain", DefaultComm = 0 });
                    comboRole.Items.Add(new RoleComboItem { Id = 3, Name = "Head Chef", DefaultComm = 0 });
                    comboRole.Items.Add(new RoleComboItem { Id = 4, Name = "Barista", DefaultComm = 0 });
                }

                int idx = -1;
                for (int i = 0; i < comboRole.Items.Count; i++)
                {
                    if (comboRole.Items[i].ToString().Equals(currentSelected, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx >= 0)
                {
                    comboRole.SelectedIndex = idx;
                }
                else if (comboRole.Items.Count > 0)
                {
                    comboRole.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void GridStaff_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridStaff.Rows[e.RowIndex].Cells["Id"].Value != null)
            {
                DataGridViewRow row = gridStaff.Rows[e.RowIndex];
                selectedStaffId = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text = row.Cells["Staff / Steward Name"].Value?.ToString() ?? "";
                txtPhone.Text = row.Cells["Contact Phone"].Value?.ToString() ?? "";
                txtEmail.Text = row.Cells["Email Address"].Value?.ToString() ?? "";

                string roleName = row.Cells["Designation / Role"].Value?.ToString() ?? "";
                int rIdx = -1;
                for (int i = 0; i < comboRole.Items.Count; i++)
                {
                    if (comboRole.Items[i].ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase))
                    {
                        rIdx = i;
                        break;
                    }
                }
                if (rIdx >= 0)
                {
                    comboRole.SelectedIndex = rIdx;
                }
                else if (!string.IsNullOrEmpty(roleName))
                {
                    comboRole.Items.Add(new RoleComboItem { Id = 0, Name = roleName, DefaultComm = 0 });
                    comboRole.SelectedIndex = comboRole.Items.Count - 1;
                }

                numCommission.Value = Convert.ToDecimal(row.Cells["Incentive %"].Value ?? 0);
                chkIsActive.Checked = (row.Cells["Status"].Value?.ToString() ?? "") == "Active";

                btnSave.Text = "✏️ Update Staff";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string role = comboRole.SelectedItem?.ToString() ?? "Steward";
            decimal commission = numCommission.Value;
            bool isActive = chkIsActive.Checked;

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter the Staff / Steward Full Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    if (selectedStaffId == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO Staff (Name, Phone, Email, Role, CommissionRate, IsActive)
                            VALUES (@name, @phone, @email, @role, @comm, @act)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@phone", phone);
                            cmd.Parameters.AddWithValue("@email", email);
                            cmd.Parameters.AddWithValue("@role", role);
                            cmd.Parameters.AddWithValue("@comm", commission);
                            cmd.Parameters.AddWithValue("@act", isActive ? 1 : 0);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Staff member added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE Staff 
                            SET Name = @name, Phone = @phone, Email = @email, Role = @role, 
                                CommissionRate = @comm, IsActive = @act
                            WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@phone", phone);
                            cmd.Parameters.AddWithValue("@email", email);
                            cmd.Parameters.AddWithValue("@role", role);
                            cmd.Parameters.AddWithValue("@comm", commission);
                            cmd.Parameters.AddWithValue("@act", isActive ? 1 : 0);
                            cmd.Parameters.AddWithValue("@id", selectedStaffId);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Staff profile updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                ResetForm();
                LoadStaff();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving staff profile: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridStaff.SelectedRows.Count == 0 || selectedStaffId == 0)
            {
                MessageBox.Show("Please select a staff member from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Are you sure you want to delete this staff record?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Staff WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedStaffId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Staff record deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
                LoadStaff();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting staff: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetForm()
        {
            selectedStaffId = 0;
            txtName.Clear();
            txtPhone.Clear();
            txtEmail.Clear();
            if (comboRole.Items.Count > 0) comboRole.SelectedIndex = 0;
            numCommission.Value = 0;
            chkIsActive.Checked = true;
            btnSave.Text = "💾 Save Staff";
            txtName.Focus();
        }
    }
}
