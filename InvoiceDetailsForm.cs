using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class InvoiceDetailsForm : Form
    {
        public InvoiceDetailsForm(string invoiceNumber)
        {
            InitializeComponent(invoiceNumber);
        }

        private void InitializeComponent(string invoiceNumber)
        {
            this.Text = $"Invoice Details - {invoiceNumber}";
            this.Size = new Size(1000, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.Secondary;
            this.ForeColor = Theme.TextLight;
            this.Font = Theme.MainFont;

            Label lblTitle = new Label();
            lblTitle.Text = $"Invoice Details: {invoiceNumber}";
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            Theme.StyleLabel(lblTitle, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblTitle);

            DataGridView gridItems = new DataGridView();
            gridItems.Location = new Point(20, 55);
            gridItems.Size = new Size(this.ClientSize.Width - 40, 240);
            gridItems.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridItems);
            this.Controls.Add(gridItems);

            Label lblTotal = new Label();
            lblTotal.Text = "SubTotal: Rs. 0.00  •  Discount: Rs. 0.00  •  Tax: Rs. 0.00  •  Grand Total: Rs. 0.00  •  Paid (Checkout): Rs. 0.00  •  Paid (Later): Rs. 0.00  •  Total Paid: Rs. 0.00  •  Due: Rs. 0.00";
            lblTotal.Location = new Point(20, 315);
            lblTotal.Size = new Size(this.ClientSize.Width - 40, 30);
            lblTotal.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblTotal, Theme.Success, Theme.BoldFont);
            this.Controls.Add(lblTotal);

            // Load items
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            ISNULL(sd.ItemType, 'Product') AS [Type],
                            CASE WHEN sd.ItemType = 'Service' THEN ISNULL(s.Code, 'CF') ELSE ISNULL(p.Code, 'CF') END AS [Code],
                            CASE WHEN sd.ItemType = 'Service' THEN ISNULL(s.Name, 'Kitchen Dish') ELSE ISNULL(p.Name, 'Menu Item') END AS [Description],
                            ISNULL(sd.HSNSAC, '-') AS [HSN/SAC],
                            ISNULL(st.Name, '-') AS [Steward / Staff],
                            sd.Quantity AS [Qty], 
                            ISNULL((SELECT SUM(srd.Quantity) 
                                    FROM SalesReturnDetails srd 
                                    INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id 
                                    WHERE sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId), 0) AS [Returned],
                            sd.UnitPrice AS [Rate], 
                            ISNULL(sd.TaxableAmount, sd.Total) AS [Taxable],
                            ISNULL(sd.GSTRate, 0) AS [GST %],
                            (ISNULL(sd.CGSTAmount, 0) + ISNULL(sd.SGSTAmount, 0) + ISNULL(sd.IGSTAmount, 0)) AS [Tax Amt],
                            sd.Total AS [Total]
                        FROM SaleDetails sd
                        INNER JOIN Sales sl ON sd.SaleId = sl.Id
                        LEFT JOIN Products p ON sd.ProductId = p.Id
                        LEFT JOIN Services s ON sd.ServiceId = s.Id
                        LEFT JOIN Staff st ON sd.StaffId = st.Id
                        WHERE sl.InvoiceNumber = @invNum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridItems.DataSource = dt;

                            if (gridItems.Columns["Rate"] != null) gridItems.Columns["Rate"].DefaultCellStyle.Format = "N2";
                            if (gridItems.Columns["Taxable"] != null) gridItems.Columns["Taxable"].DefaultCellStyle.Format = "N2";
                            if (gridItems.Columns["Tax Amt"] != null) gridItems.Columns["Tax Amt"].DefaultCellStyle.Format = "N2";
                            if (gridItems.Columns["Total"] != null) gridItems.Columns["Total"].DefaultCellStyle.Format = "N2";
                        }
                    }

                    // Get grand total and details
                    string totalQuery = @"
                        SELECT GrandTotal, Discount, Tax, SubTotal, 
                               CASE WHEN AmountPaid > GrandTotal THEN GrandTotal ELSE AmountPaid END AS AmountPaid, 
                               PaymentMethod,
                               CASE WHEN ISNULL(CashAmount, 0) > GrandTotal THEN GrandTotal ELSE ISNULL(CashAmount, 0) END AS CashAmount,
                               CASE WHEN ISNULL(OnlineAmount, 0) > GrandTotal THEN GrandTotal ELSE ISNULL(OnlineAmount, 0) END AS OnlineAmount,
                               ISNULL(IsGSTBill, 1) AS IsGSTBill,
                               ISNULL(TaxableAmount, 0) AS TaxableAmount,
                               ISNULL(CGSTAmount, 0) AS CGSTAmount,
                               ISNULL(SGSTAmount, 0) AS SGSTAmount,
                               ISNULL(IGSTAmount, 0) AS IGSTAmount,
                               ISNULL(CustomerGSTIN, '') AS CustomerGSTIN,
                               ISNULL(PlaceOfSupply, '') AS PlaceOfSupply,
                               ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = Sales.Id), 0) AS LaterPaid,
                               CASE 
                                   WHEN (DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = Sales.Id), 0)) < 0 
                                   THEN 0.00 
                                   ELSE (DueAmount - ISNULL((SELECT SUM(Amount) FROM CustomerPayments WHERE SaleId = Sales.Id), 0)) 
                               END AS CurrentDue,
                               ISNULL((SELECT SUM(TotalRefund) FROM SalesReturns WHERE SaleId = Sales.Id), 0) AS TotalRefund,
                               ISNULL((SELECT SUM(CashRefund) FROM SalesReturns WHERE SaleId = Sales.Id), 0) AS CashRefund
                        FROM Sales 
                        WHERE InvoiceNumber = @invNum";

                    using (SqlCommand cmd = new SqlCommand(totalQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                decimal grand = Convert.ToDecimal(rdr["GrandTotal"]);
                                decimal discount = Convert.ToDecimal(rdr["Discount"]);
                                decimal tax = Convert.ToDecimal(rdr["Tax"]);
                                decimal sub = Convert.ToDecimal(rdr["SubTotal"]);
                                string payMethod = rdr["PaymentMethod"]?.ToString() ?? "Cash";
                                decimal cashAmt = Convert.ToDecimal(rdr["CashAmount"]);
                                decimal onlineAmt = Convert.ToDecimal(rdr["OnlineAmount"]);
                                decimal taxable = Convert.ToDecimal(rdr["TaxableAmount"]);
                                decimal cgst = Convert.ToDecimal(rdr["CGSTAmount"]);
                                decimal sgst = Convert.ToDecimal(rdr["SGSTAmount"]);
                                decimal igst = Convert.ToDecimal(rdr["IGSTAmount"]);
                                bool isGst = Convert.ToBoolean(rdr["IsGSTBill"]);
                                decimal initialPaid = Convert.ToDecimal(rdr["AmountPaid"]);
                                decimal laterPaid = Convert.ToDecimal(rdr["LaterPaid"]);
                                decimal currentDue = Convert.ToDecimal(rdr["CurrentDue"]);
                                decimal totalRefund = Convert.ToDecimal(rdr["TotalRefund"]);
                                decimal cashRefund = Convert.ToDecimal(rdr["CashRefund"]);
                                decimal totalPaid = initialPaid + laterPaid;

                                string billTag = isGst ? "🧾 [GST Tax Invoice]" : "📄 [Non-GST Retail Bill]";
                                string taxBreakdown = isGst ? $"(CGST: Rs. {cgst:N2} | SGST: Rs. {sgst:N2} | IGST: Rs. {igst:N2})" : "(No Tax)";
                                string payBreakdown = (payMethod == "Split" || (cashAmt > 0 && onlineAmt > 0)) ? $" [Split: Cash Rs. {cashAmt:N2} + Online Rs. {onlineAmt:N2}]" : $" [{payMethod}]";

                                if (totalRefund > 0)
                                {
                                    lblTotal.Text = $"{billTag} SubTotal: Rs. {sub:N2} • Disc: Rs. {discount:N2} • Taxable: Rs. {taxable:N2} • Tax: Rs. {tax:N2} {taxBreakdown} • Grand: Rs. {grand:N2} • Returned: Rs. {totalRefund:N2} • Net: Rs. {grand - totalRefund:N2} • Paid: Rs. {totalPaid - cashRefund:N2}{payBreakdown} • Due: Rs. {currentDue:N2}";
                                }
                                else
                                {
                                    lblTotal.Text = $"{billTag} SubTotal: Rs. {sub:N2} • Disc: Rs. {discount:N2} • Taxable: Rs. {taxable:N2} • Tax: Rs. {tax:N2} {taxBreakdown} • Grand: Rs. {grand:N2} • Total Paid: Rs. {totalPaid:N2}{payBreakdown} • Due: Rs. {currentDue:N2}";
                                }
                            }
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
}
