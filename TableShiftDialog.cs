using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class TableShiftDialog : Form
    {
        private ComboBox cmbSourceTable;
        private ComboBox cmbTargetTable;
        private Label lblSourceInfo;
        private Label lblTargetInfo;
        private Button btnTransfer;
        private Button btnCancel;
        private string _initialSourceTable;

        public string SelectedTargetTable { get; private set; }

        public TableShiftDialog(string sourceTable = null)
        {
            _initialSourceTable = sourceTable;
            InitializeComponent();
            LoadTables();
        }

        private void InitializeComponent()
        {
            this.Text = "Table Shift & Transfer";
            this.Size = new Size(460, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.Primary
            };
            Label lblTitle = new Label
            {
                Text = "🔁 Shift / Transfer Running Table",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(16, 16),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            // Source Table Section
            Label lblSrc = new Label
            {
                Text = "Source Table (Running Order):",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 75),
                AutoSize = true
            };
            this.Controls.Add(lblSrc);

            cmbSourceTable = new ComboBox
            {
                Location = new Point(24, 100),
                Size = new Size(395, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbSourceTable.SelectedIndexChanged += CmbSourceTable_SelectedIndexChanged;
            this.Controls.Add(cmbSourceTable);

            lblSourceInfo = new Label
            {
                Text = "Order Total: ₹0.00 | KOTs: None",
                Font = Theme.SmallFont,
                ForeColor = Theme.Warning,
                Location = new Point(26, 135),
                AutoSize = true
            };
            this.Controls.Add(lblSourceInfo);

            // Target Table Section
            Label lblTgt = new Label
            {
                Text = "Transfer To Target Table:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(24, 165),
                AutoSize = true
            };
            this.Controls.Add(lblTgt);

            cmbTargetTable = new ComboBox
            {
                Location = new Point(24, 190),
                Size = new Size(395, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight
            };
            cmbTargetTable.SelectedIndexChanged += CmbTargetTable_SelectedIndexChanged;
            this.Controls.Add(cmbTargetTable);

            lblTargetInfo = new Label
            {
                Text = "Status: Available",
                Font = Theme.SmallFont,
                ForeColor = Theme.Success,
                Location = new Point(26, 225),
                AutoSize = true
            };
            this.Controls.Add(lblTargetInfo);

            // Action Buttons
            btnTransfer = new Button
            {
                Text = "Confirm Shift",
                Location = new Point(190, 265),
                Size = new Size(130, 40),
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTransfer.FlatAppearance.BorderSize = 0;
            btnTransfer.Click += BtnTransfer_Click;
            this.Controls.Add(btnTransfer);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(330, 265),
                Size = new Size(90, 40),
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

        private void LoadTables()
        {
            try
            {
                cmbSourceTable.Items.Clear();
                cmbTargetTable.Items.Clear();

                List<TableItem> sourceList = new List<TableItem>();
                List<TableItem> targetList = new List<TableItem>();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TableNumber, TableName, Status, CurrentBillAmount, ActiveKotNumbers FROM CafeTables WHERE IsActive = 1", conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string tNum = r["TableNumber"].ToString();
                            string tName = r["TableName"].ToString();
                            string status = r["Status"].ToString();
                            decimal amt = Convert.ToDecimal(r["CurrentBillAmount"]);
                            string kots = r["ActiveKotNumbers"]?.ToString() ?? "";

                            string display = $"{tName} ({status})";
                            if (status == "Running" || status == "Printed")
                            {
                                display += $" - ₹{amt:0.00}";
                                sourceList.Add(new TableItem { TableNumber = tNum, Display = display, Status = status, Amount = amt, Kots = kots });
                            }
                            else
                            {
                                targetList.Add(new TableItem { TableNumber = tNum, Display = display, Status = status, Amount = amt, Kots = kots });
                            }
                        }
                    }
                }

                sourceList.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));
                targetList.Sort((a, b) => TableHelper.CompareTableNumbers(a.TableNumber, b.TableNumber));

                foreach (var it in sourceList) cmbSourceTable.Items.Add(it);
                foreach (var it in targetList) cmbTargetTable.Items.Add(it);

                if (!string.IsNullOrEmpty(_initialSourceTable))
                {
                    for (int i = 0; i < cmbSourceTable.Items.Count; i++)
                    {
                        if (((TableItem)cmbSourceTable.Items[i]).TableNumber == _initialSourceTable)
                        {
                            cmbSourceTable.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else if (cmbSourceTable.Items.Count > 0)
                {
                    cmbSourceTable.SelectedIndex = 0;
                }

                if (cmbTargetTable.Items.Count > 0)
                    cmbTargetTable.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tables: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CmbSourceTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSourceTable.SelectedItem is TableItem item)
            {
                lblSourceInfo.Text = $"Order Total: ₹{item.Amount:0.00} | KOTs: {(string.IsNullOrEmpty(item.Kots) ? "None" : item.Kots)}";
            }
        }

        private void CmbTargetTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTargetTable.SelectedItem is TableItem item)
            {
                lblTargetInfo.Text = $"Status: {item.Status}";
                lblTargetInfo.ForeColor = item.Status == "Available" ? Theme.Success : Theme.Warning;
            }
        }

        private void BtnTransfer_Click(object sender, EventArgs e)
        {
            if (!(cmbSourceTable.SelectedItem is TableItem src) || !(cmbTargetTable.SelectedItem is TableItem tgt))
            {
                MessageBox.Show("Please select both source and target tables.", "Shift Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (src.TableNumber == tgt.TableNumber)
            {
                MessageBox.Show("Source and Target tables cannot be the same.", "Shift Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Get Source Table Data
                            string getSrcSql = "SELECT Status, CurrentBillAmount, OrderStartTime, BilledTime, ActiveKotNumbers, ActiveSaleId, CurrentSteward FROM CafeTables WHERE TableNumber = @src";
                            string status = "Running";
                            decimal amount = 0;
                            object startTime = DBNull.Value;
                            object billedTime = DBNull.Value;
                            string kots = "";
                            object saleId = DBNull.Value;
                            string steward = "";

                            using (SqlCommand cmd = new SqlCommand(getSrcSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@src", src.TableNumber);
                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                    {
                                        status = r["Status"].ToString();
                                        amount = Convert.ToDecimal(r["CurrentBillAmount"]);
                                        startTime = r["OrderStartTime"];
                                        billedTime = r["BilledTime"];
                                        kots = r["ActiveKotNumbers"]?.ToString();
                                        saleId = r["ActiveSaleId"];
                                        steward = r["CurrentSteward"]?.ToString();
                                    }
                                }
                            }

                            // 2. Update Target Table
                            string updTgtSql = @"
                                UPDATE CafeTables 
                                SET Status = @status, CurrentBillAmount = @amt, OrderStartTime = @start, BilledTime = @billed, 
                                    ActiveKotNumbers = @kots, ActiveSaleId = @saleId, CurrentSteward = @steward
                                WHERE TableNumber = @tgt";
                            using (SqlCommand cmd = new SqlCommand(updTgtSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@status", status);
                                cmd.Parameters.AddWithValue("@amt", amount);
                                cmd.Parameters.AddWithValue("@start", startTime ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@billed", billedTime ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@kots", (object)kots ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@saleId", saleId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@steward", (object)steward ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@tgt", tgt.TableNumber);
                                cmd.ExecuteNonQuery();
                            }

                            // 3. Reset Source Table to Available
                            string resetSrcSql = @"
                                UPDATE CafeTables 
                                SET Status = 'Available', CurrentBillAmount = 0.00, OrderStartTime = NULL, BilledTime = NULL, 
                                    ActiveKotNumbers = NULL, ActiveSaleId = NULL, CurrentSteward = NULL
                                WHERE TableNumber = @src";
                            using (SqlCommand cmd = new SqlCommand(resetSrcSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@src", src.TableNumber);
                                cmd.ExecuteNonQuery();
                            }

                            // 4. Update Active KOT records to point to target table number
                            string updKotSql = "UPDATE KOTMaster SET TableNumber = @tgt WHERE TableNumber = @src AND Status IN ('Active', 'Served', 'Printed')";
                            using (SqlCommand cmd = new SqlCommand(updKotSql, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@tgt", tgt.TableNumber);
                                cmd.Parameters.AddWithValue("@src", src.TableNumber);
                                cmd.ExecuteNonQuery();
                            }

                            trans.Commit();
                            SelectedTargetTable = tgt.TableNumber;
                            MessageBox.Show($"Table shifted successfully from {src.TableNumber} to {tgt.TableNumber}!", "Table Shifted", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show($"Failed to shift table: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class TableItem
        {
            public string TableNumber { get; set; }
            public string Display { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
            public string Kots { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}
