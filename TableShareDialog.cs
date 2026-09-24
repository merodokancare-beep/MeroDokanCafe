using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class TableShareDialog : Form
    {
        private ComboBox cmbTable;
        private Label lblTableStatus;
        private Label lblSubTablePreview;

        private RadioButton radMoveKot;
        private RadioButton radSplitItems;
        private RadioButton radNewOrder;

        private Panel kotListPanel;
        private FlowLayoutPanel kotListFlow;
        private List<KotCheckboxItem> kotCheckboxes = new List<KotCheckboxItem>();

        private Panel splitItemsPanel;
        private FlowLayoutPanel itemsListFlow;
        private List<SplitItemRow> itemRows = new List<SplitItemRow>();

        private Button btnConfirm;
        private Button btnCancel;

        private string _preselectedTable;

        public string SelectedTableNumber { get; private set; }

        public TableShareDialog(string initialTable = null)
        {
            _preselectedTable = initialTable;
            InitializeComponent();
            LoadTables();

            this.Shown += (s, e) => {
                cmbTable?.Focus();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "Share Table & Split Billing";
            this.Size = new Size(520, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;

            // 1. Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Primary,
                Padding = new Padding(16, 10, 16, 10)
            };
            Label lblTitle = new Label
            {
                Text = "🪑 Share Table / Split Billing",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(14, 10),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Create separate orders and independent bills for customers sharing the same table",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(16, 34),
                AutoSize = true
            };
            header.Controls.Add(lblSubtitle);
            this.Controls.Add(header);

            // 2. Select Base Table
            Label lblSelect = new Label
            {
                Text = "Select Table to Share:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 75),
                AutoSize = true
            };
            this.Controls.Add(lblSelect);

            cmbTable = new ComboBox
            {
                Location = new Point(24, 98),
                Size = new Size(456, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbTable.SelectedIndexChanged += CmbTable_SelectedIndexChanged;
            this.Controls.Add(cmbTable);

            lblTableStatus = new Label
            {
                Text = "Status: Available",
                Font = Theme.SmallFont,
                ForeColor = Theme.Success,
                Location = new Point(26, 134),
                AutoSize = true
            };
            this.Controls.Add(lblTableStatus);

            // 3. New Sub-Table Preview Box
            Panel previewBox = new Panel
            {
                Location = new Point(24, 158),
                Size = new Size(456, 44),
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10, 8, 10, 8)
            };
            lblSubTablePreview = new Label
            {
                Text = "New Shared Bill Target: Table 4-B (Customer B)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 240, 138), // Amber / Yellow
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            previewBox.Controls.Add(lblSubTablePreview);
            this.Controls.Add(previewBox);

            // 4. Options
            Label lblOptionTitle = new Label
            {
                Text = "Sharing / Billing Mode:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 212),
                AutoSize = true
            };
            this.Controls.Add(lblOptionTitle);

            radMoveKot = new RadioButton
            {
                Text = "Move entire KOT(s) to 2nd customer (Separate Bill)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(26, 236),
                Size = new Size(450, 24),
                Checked = true
            };
            radMoveKot.CheckedChanged += (s, e) => ToggleMode();
            this.Controls.Add(radMoveKot);

            radSplitItems = new RadioButton
            {
                Text = "Split specific menu items to 2nd customer",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(26, 262),
                Size = new Size(450, 24)
            };
            radSplitItems.CheckedChanged += (s, e) => ToggleMode();
            this.Controls.Add(radSplitItems);

            radNewOrder = new RadioButton
            {
                Text = "Start fresh empty order for 2nd customer (Cust B)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(26, 288),
                Size = new Size(450, 24)
            };
            radNewOrder.CheckedChanged += (s, e) => ToggleMode();
            this.Controls.Add(radNewOrder);

            // 5A. Move KOT Container
            kotListPanel = new Panel
            {
                Location = new Point(24, 318),
                Size = new Size(456, 170),
                BackColor = Color.FromArgb(15, 23, 42),
                Visible = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            kotListFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(6)
            };
            kotListPanel.Controls.Add(kotListFlow);
            this.Controls.Add(kotListPanel);

            // 5B. Split Items Container
            splitItemsPanel = new Panel
            {
                Location = new Point(24, 318),
                Size = new Size(456, 170),
                BackColor = Color.FromArgb(15, 23, 42),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            itemsListFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(6)
            };
            splitItemsPanel.Controls.Add(itemsListFlow);
            this.Controls.Add(splitItemsPanel);

            // 6. Action Buttons
            btnConfirm = new Button
            {
                Text = "🪑 Open Shared Bill",
                Location = new Point(220, 505),
                Size = new Size(165, 40),
                BackColor = Color.FromArgb(109, 40, 217),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(395, 505),
                Size = new Size(85, 40),
                BackColor = Color.FromArgb(50, 60, 80),
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void ToggleMode()
        {
            kotListPanel.Visible = radMoveKot.Checked;
            splitItemsPanel.Visible = radSplitItems.Checked;

            if (radMoveKot.Checked)
            {
                this.Size = new Size(520, 600);
                btnConfirm.Location = new Point(220, 505);
                btnCancel.Location = new Point(395, 505);
                LoadActiveKotsForMoving();
            }
            else if (radSplitItems.Checked)
            {
                this.Size = new Size(520, 600);
                btnConfirm.Location = new Point(220, 505);
                btnCancel.Location = new Point(395, 505);
                LoadActiveItemsForSplit();
            }
            else // Fresh order
            {
                this.Size = new Size(520, 420);
                btnConfirm.Location = new Point(220, 325);
                btnCancel.Location = new Point(395, 325);
            }
        }

        private void LoadTables()
        {
            try
            {
                cmbTable.Items.Clear();
                int selectedIdx = 0;

                List<TableItem> list = new List<TableItem>();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Load live KOT summaries
                    Dictionary<string, LiveSummary> kotMap = new Dictionary<string, LiveSummary>(StringComparer.OrdinalIgnoreCase);
                    string kotSql = @"
                        SELECT k.TableNumber, k.KOTNumber, kd.Amount
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0";
                    using (SqlCommand cmd = new SqlCommand(kotSql, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string tNum = r["TableNumber"].ToString();
                            int kNo = Convert.ToInt32(r["KOTNumber"]);
                            decimal amt = Convert.ToDecimal(r["Amount"]);

                            if (!kotMap.TryGetValue(tNum, out LiveSummary s))
                            {
                                s = new LiveSummary();
                                kotMap[tNum] = s;
                            }
                            s.TotalAmount += amt;
                            if (!s.KotNumbers.Contains(kNo)) s.KotNumbers.Add(kNo);
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT TableNumber, TableName, Status, CurrentBillAmount, ActiveKotNumbers FROM CafeTables WHERE IsActive = 1", conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string tNum = r["TableNumber"].ToString();
                            if (tNum.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase)) continue;

                            string tName = r["TableName"]?.ToString() ?? tNum;
                            string status = r["Status"]?.ToString() ?? "Available";
                            decimal amt = r["CurrentBillAmount"] != DBNull.Value ? Convert.ToDecimal(r["CurrentBillAmount"]) : 0;
                            string kots = r["ActiveKotNumbers"]?.ToString() ?? "";

                            if (kotMap.TryGetValue(tNum, out LiveSummary s))
                            {
                                status = "Running";
                                amt = s.TotalAmount;
                                s.KotNumbers.Sort();
                                kots = string.Join(",", s.KotNumbers);
                            }

                            string display = status == "Running" || status == "Printed"
                                ? $"🍽️ {tName} — Active Order: ₹{amt:0} (KOT: {kots})"
                                : $"🟢 {tName} — Available";

                            list.Add(new TableItem { TableNumber = tNum, TableName = tName, Status = status, Amount = amt, Kots = kots, Display = display });
                        }
                    }
                }

                // Sort tables naturally
                list.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));

                for (int i = 0; i < list.Count; i++)
                {
                    cmbTable.Items.Add(list[i]);
                    if (!string.IsNullOrEmpty(_preselectedTable) && list[i].TableNumber.Equals(_preselectedTable, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                    }
                }

                if (cmbTable.Items.Count > 0)
                {
                    cmbTable.SelectedIndex = selectedIdx;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class LiveSummary
        {
            public decimal TotalAmount { get; set; }
            public List<int> KotNumbers { get; } = new List<int>();
        }

        private void CmbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTable.SelectedItem is TableItem item)
            {
                lblTableStatus.Text = item.Status == "Running" 
                    ? $"Status: Occupied / Running (Order: ₹{item.Amount:N2})" 
                    : $"Status: {item.Status}";
                lblTableStatus.ForeColor = item.Status == "Running" ? Theme.Warning : Theme.Success;

                string baseNum = TableHelper.GetBaseTableNumber(item.TableNumber);
                string nextSubTable = CalculateNextSubTable(baseNum);
                char letter = nextSubTable.Contains("-") ? nextSubTable.Substring(nextSubTable.IndexOf('-') + 1)[0] : 'B';

                lblSubTablePreview.Text = $"New Shared Bill Target: Table {nextSubTable} (Customer {letter})";

                if (radMoveKot.Checked)
                {
                    LoadActiveKotsForMoving();
                }
                else if (radSplitItems.Checked)
                {
                    LoadActiveItemsForSplit();
                }
            }
        }

        private string CalculateNextSubTable(string baseNum)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TableNumber FROM CafeTables WHERE TableNumber LIKE @pattern", conn))
                    {
                        cmd.Parameters.AddWithValue("@pattern", baseNum + "%");
                        List<string> existing = new List<string>();
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read()) existing.Add(r["TableNumber"].ToString());
                        }

                        char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F' };
                        foreach (char c in letters)
                        {
                            string candidate = $"{baseNum}-{c}";
                            if (!existing.Contains(candidate))
                            {
                                return candidate;
                            }
                        }
                    }
                }
            }
            catch { }
            return $"{baseNum}-B";
        }

        private void LoadActiveKotsForMoving()
        {
            kotListFlow.Controls.Clear();
            kotCheckboxes.Clear();

            if (!(cmbTable.SelectedItem is TableItem item)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT k.Id, k.KOTNumber, k.CreatedAt,
                               ISNULL(SUM(kd.Amount), 0) AS KotTotal,
                               COUNT(kd.Id) AS ItemCount
                        FROM KOTMaster k
                        LEFT JOIN KOTDetails kd ON k.Id = kd.KOTId AND kd.IsVoided = 0
                        WHERE k.TableNumber = @tNum AND k.Status IN ('Active', 'Served', 'Printed')
                        GROUP BY k.Id, k.KOTNumber, k.CreatedAt
                        ORDER BY k.KOTNumber";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", item.TableNumber);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            int count = 0;
                            while (r.Read())
                            {
                                int kotId = Convert.ToInt32(r["Id"]);
                                int kotNo = Convert.ToInt32(r["KOTNumber"]);
                                decimal total = Convert.ToDecimal(r["KotTotal"]);
                                int items = Convert.ToInt32(r["ItemCount"]);

                                // If multiple KOTs exist, select the 2nd one by default
                                bool checkByDefault = (count > 0);

                                KotCheckboxItem chk = new KotCheckboxItem(kotId, kotNo, total, items, checkByDefault);
                                kotCheckboxes.Add(chk);
                                kotListFlow.Controls.Add(chk.ControlPanel);
                                count++;
                            }
                        }
                    }
                }

                if (kotCheckboxes.Count == 0)
                {
                    Label lblNone = new Label
                    {
                        Text = "No active KOTs found on this table.\n(Switch to 'Start Fresh Empty Order' to create Customer B bill)",
                        Font = Theme.SmallFont,
                        ForeColor = Theme.TextMuted,
                        AutoSize = true,
                        Margin = new Padding(8)
                    };
                    kotListFlow.Controls.Add(lblNone);
                }
                else if (kotCheckboxes.Count == 1)
                {
                    // If only 1 KOT exists, switch to split items mode automatically
                    radSplitItems.Checked = true;
                }
            }
            catch { }
        }

        private void LoadActiveItemsForSplit()
        {
            itemsListFlow.Controls.Clear();
            itemRows.Clear();

            if (!(cmbTable.SelectedItem is TableItem item)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT kd.Id as DetailId, kd.KOTId, kd.ItemName, kd.Quantity, kd.Rate, kd.ProductId
                        FROM KOTMaster k
                        INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                        WHERE k.TableNumber = @tNum AND k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0
                        ORDER BY kd.Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", item.TableNumber);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int detailId = Convert.ToInt32(r["DetailId"]);
                                int kotId = Convert.ToInt32(r["KOTId"]);
                                string name = r["ItemName"].ToString();
                                int qty = Convert.ToInt32(r["Quantity"]);
                                decimal rate = Convert.ToDecimal(r["Rate"]);
                                int? pId = r["ProductId"] != DBNull.Value ? (int?)Convert.ToInt32(r["ProductId"]) : null;

                                SplitItemRow row = new SplitItemRow(detailId, kotId, name, qty, rate, pId);
                                itemRows.Add(row);
                                itemsListFlow.Controls.Add(row.ControlPanel);
                            }
                        }
                    }
                }

                if (itemRows.Count == 0)
                {
                    Label lblNone = new Label
                    {
                        Text = "No active KOT items on this table to split.",
                        Font = Theme.SmallFont,
                        ForeColor = Theme.TextMuted,
                        AutoSize = true,
                        Margin = new Padding(8)
                    };
                    itemsListFlow.Controls.Add(lblNone);
                }
            }
            catch { }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (!(cmbTable.SelectedItem is TableItem item)) return;

            string baseNum = TableHelper.GetBaseTableNumber(item.TableNumber);
            string newSubTable = CalculateNextSubTable(baseNum);
            char letter = newSubTable.Contains("-") ? newSubTable.Substring(newSubTable.IndexOf('-') + 1)[0] : 'B';
            string sourceTable = item.TableNumber;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. If source table was single "4", rename/promote to "4-A"
                            string effectiveSourceTable = sourceTable;
                            if (!sourceTable.Contains("-"))
                            {
                                effectiveSourceTable = $"{baseNum}-A";

                                // Update CafeTables row for 4 -> 4-A
                                string updTableSql = @"
                                    UPDATE CafeTables 
                                    SET TableNumber = @tableA, TableName = @nameA
                                    WHERE TableNumber = @oldNum";
                                using (SqlCommand cmd = new SqlCommand(updTableSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@tableA", effectiveSourceTable);
                                    cmd.Parameters.AddWithValue("@nameA", $"Table {baseNum} (Cust A)");
                                    cmd.Parameters.AddWithValue("@oldNum", sourceTable);
                                    cmd.ExecuteNonQuery();
                                }

                                // Update KOTMaster records for 4 -> 4-A
                                string updKotSql = "UPDATE KOTMaster SET TableNumber = @tableA WHERE TableNumber = @oldNum AND Status IN ('Active', 'Served', 'Printed')";
                                using (SqlCommand cmd = new SqlCommand(updKotSql, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@tableA", effectiveSourceTable);
                                    cmd.Parameters.AddWithValue("@oldNum", sourceTable);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // 2. Insert new CafeTables row for 4-B
                            string insNewTableSql = @"
                                IF NOT EXISTS (SELECT 1 FROM CafeTables WHERE TableNumber = @newNum)
                                BEGIN
                                    INSERT INTO CafeTables (TableNumber, TableName, Section, Capacity, Status, CurrentBillAmount, OrderStartTime, IsActive)
                                    VALUES (@newNum, @newName, 'Main Dining', 2, 'Available', 0.00, NULL, 1);
                                END
                                ELSE
                                BEGIN
                                    UPDATE CafeTables SET Status = 'Available', CurrentBillAmount = 0.00 WHERE TableNumber = @newNum;
                                END";
                            using (SqlCommand cmd = new SqlCommand(insNewTableSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@newNum", newSubTable);
                                cmd.Parameters.AddWithValue("@newName", $"Table {baseNum} (Cust {letter})");
                                cmd.ExecuteNonQuery();
                            }

                            // 3. Handle Transfer
                            if (radMoveKot.Checked)
                            {
                                // Move selected KOTs directly to newSubTable
                                List<KotCheckboxItem> selectedKots = kotCheckboxes.FindAll(x => x.IsSelected);
                                foreach (var k in selectedKots)
                                {
                                    string moveKotSql = "UPDATE KOTMaster SET TableNumber = @tgt WHERE Id = @kId";
                                    using (SqlCommand cmd = new SqlCommand(moveKotSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@tgt", newSubTable);
                                        cmd.Parameters.AddWithValue("@kId", k.KotId);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (radSplitItems.Checked)
                            {
                                List<SplitItemRow> selectedItems = itemRows.FindAll(x => x.IsSelected && x.SplitQuantity > 0);
                                if (selectedItems.Count > 0)
                                {
                                    // Get Next KOT Number
                                    int nextKotNumber = 1;
                                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(KOTNumber), 0) + 1 FROM KOTMaster", conn, trans))
                                    {
                                        nextKotNumber = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    // Insert KOT for target sub-table
                                    int newKotId = 0;
                                    string insKotSql = @"
                                        INSERT INTO KOTMaster (KOTNumber, TableNumber, OrderType, Steward, Status, KotComment, CreatedAt)
                                        VALUES (@kotNo, @tNum, 'DINING', 'Tashi', 'Active', 'Split from shared table order', GETDATE());
                                        SELECT SCOPE_IDENTITY();";
                                    using (SqlCommand cmd = new SqlCommand(insKotSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@kotNo", nextKotNumber);
                                        cmd.Parameters.AddWithValue("@tNum", newSubTable);
                                        newKotId = Convert.ToInt32(cmd.ExecuteScalar());
                                    }

                                    foreach (var it in selectedItems)
                                    {
                                        decimal lineTotal = it.SplitQuantity * it.Rate;

                                        // Insert into new KOT
                                        string insDet = @"
                                            INSERT INTO KOTDetails (KOTId, ProductId, ItemName, Quantity, Rate, Amount, Instructions)
                                            VALUES (@kotId, @pId, @name, @qty, @rate, @amt, 'Split Item')";
                                        using (SqlCommand cmd = new SqlCommand(insDet, conn, trans))
                                        {
                                            cmd.Parameters.AddWithValue("@kotId", newKotId);
                                            cmd.Parameters.AddWithValue("@pId", (object)it.ProductId ?? DBNull.Value);
                                            cmd.Parameters.AddWithValue("@name", it.ItemName);
                                            cmd.Parameters.AddWithValue("@qty", it.SplitQuantity);
                                            cmd.Parameters.AddWithValue("@rate", it.Rate);
                                            cmd.Parameters.AddWithValue("@amt", lineTotal);
                                            cmd.ExecuteNonQuery();
                                        }

                                        // Deduct from original KOT
                                        if (it.SplitQuantity >= it.MaxQuantity)
                                        {
                                            using (SqlCommand cmd = new SqlCommand("UPDATE KOTDetails SET IsVoided = 1, VoidReason = 'Split to Table ' + @tgt WHERE Id = @detId", conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@tgt", newSubTable);
                                                cmd.Parameters.AddWithValue("@detId", it.DetailId);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                        else
                                        {
                                            int remQty = it.MaxQuantity - it.SplitQuantity;
                                            decimal remAmt = remQty * it.Rate;
                                            using (SqlCommand cmd = new SqlCommand("UPDATE KOTDetails SET Quantity = @remQty, Amount = @remAmt WHERE Id = @detId", conn, trans))
                                            {
                                                cmd.Parameters.AddWithValue("@remQty", remQty);
                                                cmd.Parameters.AddWithValue("@remAmt", remAmt);
                                                cmd.Parameters.AddWithValue("@detId", it.DetailId);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                            }

                            // 4. Synchronize CafeTables status, amounts, and KOTs for BOTH effectiveSourceTable AND newSubTable
                            SyncTableSummary(effectiveSourceTable, conn, trans);
                            SyncTableSummary(newSubTable, conn, trans);

                            trans.Commit();
                            SelectedTableNumber = newSubTable;
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to share table: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncTableSummary(string tableNum, SqlConnection conn, SqlTransaction trans)
        {
            decimal total = 0;
            DateTime? minTime = null;
            List<int> kots = new List<int>();

            string q = @"
                SELECT k.KOTNumber, k.CreatedAt, kd.Amount
                FROM KOTMaster k
                INNER JOIN KOTDetails kd ON k.Id = kd.KOTId
                WHERE k.TableNumber = @tNum AND k.Status IN ('Active', 'Served', 'Printed') AND kd.IsVoided = 0";

            using (SqlCommand cmd = new SqlCommand(q, conn, trans))
            {
                cmd.Parameters.AddWithValue("@tNum", tableNum);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int kNo = Convert.ToInt32(r["KOTNumber"]);
                        DateTime cAt = Convert.ToDateTime(r["CreatedAt"]);
                        decimal amt = Convert.ToDecimal(r["Amount"]);

                        total += amt;
                        if (!kots.Contains(kNo)) kots.Add(kNo);
                        if (!minTime.HasValue || cAt < minTime.Value) minTime = cAt;
                    }
                }
            }

            kots.Sort();
            string kotStr = string.Join(",", kots);
            string status = total > 0 ? "Running" : "Available";

            string upd = @"
                UPDATE CafeTables
                SET Status = @status,
                    CurrentBillAmount = @amt,
                    OrderStartTime = @stTime,
                    ActiveKotNumbers = @kots
                WHERE TableNumber = @tNum";

            using (SqlCommand cmd = new SqlCommand(upd, conn, trans))
            {
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@amt", total);
                cmd.Parameters.AddWithValue("@stTime", minTime.HasValue ? (object)minTime.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@kots", (object)kotStr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tNum", tableNum);
                cmd.ExecuteNonQuery();
            }
        }

        private class TableItem
        {
            public string TableNumber { get; set; }
            public string TableName { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
            public string Kots { get; set; }
            public string Display { get; set; }

            public override string ToString() => Display;
        }

        private class KotCheckboxItem
        {
            public int KotId { get; }
            public int KotNumber { get; }
            public decimal TotalAmount { get; }
            public int ItemCount { get; }
            public CheckBox ChkBox { get; }
            public Panel ControlPanel { get; }

            public bool IsSelected => ChkBox.Checked;

            public KotCheckboxItem(int kotId, int kotNumber, decimal totalAmount, int itemCount, bool checkByDefault)
            {
                KotId = kotId;
                KotNumber = kotNumber;
                TotalAmount = totalAmount;
                ItemCount = itemCount;

                ControlPanel = new Panel
                {
                    Width = 425,
                    Height = 36,
                    Margin = new Padding(0, 3, 0, 3),
                    BackColor = Color.FromArgb(25, 35, 55)
                };

                ChkBox = new CheckBox
                {
                    Text = $" Move KOT #{kotNumber}   (₹{totalAmount:0.00} • {itemCount} items)",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(8, 7),
                    Size = new Size(405, 22),
                    Checked = checkByDefault
                };
                ControlPanel.Controls.Add(ChkBox);
            }
        }

        private class SplitItemRow
        {
            public int DetailId { get; }
            public int KotId { get; }
            public string ItemName { get; }
            public int MaxQuantity { get; }
            public decimal Rate { get; }
            public int? ProductId { get; }

            public CheckBox ChkBox { get; }
            public NumericUpDown NumQty { get; }
            public Panel ControlPanel { get; }

            public bool IsSelected => ChkBox.Checked;
            public int SplitQuantity => (int)NumQty.Value;

            public SplitItemRow(int detailId, int kotId, string itemName, int maxQty, decimal rate, int? productId)
            {
                DetailId = detailId;
                KotId = kotId;
                ItemName = itemName;
                MaxQuantity = maxQty;
                Rate = rate;
                ProductId = productId;

                ControlPanel = new Panel
                {
                    Width = 425,
                    Height = 32,
                    Margin = new Padding(0, 2, 0, 2),
                    BackColor = Color.Transparent
                };

                ChkBox = new CheckBox
                {
                    Text = $"{itemName} (₹{rate:0} ea)",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(4, 5),
                    Size = new Size(270, 22),
                    Checked = true
                };
                ControlPanel.Controls.Add(ChkBox);

                NumQty = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = maxQty,
                    Value = maxQty,
                    Location = new Point(285, 4),
                    Size = new Size(55, 24),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    BackColor = Theme.InputBg,
                    ForeColor = Color.White
                };
                ControlPanel.Controls.Add(NumQty);

                Label lblMax = new Label
                {
                    Text = $"/ {maxQty}",
                    Font = Theme.SmallFont,
                    ForeColor = Theme.TextMuted,
                    Location = new Point(345, 7),
                    AutoSize = true
                };
                ControlPanel.Controls.Add(lblMax);
            }
        }
    }
}
