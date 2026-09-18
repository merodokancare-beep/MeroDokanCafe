using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace MeroDokan
{
    /* =========================================================================
       TVS RP 3220 / POS-80 / 80MM THERMAL RECEIPT & KOT PRINTING ENGINE
       Exact replica of "The Local Cafe" Dine-In, Takeaway, KOT & Void Tickets
       ========================================================================= */
    internal static class ThermalReceiptPrinter
    {
        private const int PaperWidth = 284; // 80mm printable width = 72mm = 284 GDI units
        private const int MarginLeft = 6;
        private const int MarginRight = 6;
        private const int UsableWidth = PaperWidth - MarginLeft - MarginRight; // 272 units
        private const string PaperName = "Thermal80mm";

        #region Public API
        public static void ShowPreview(int saleId)
        {
            try
            {
                PrintDocument doc = BuildCustomerBillDocument(saleId);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.Size = new Size(360, 720);
                try { ((Form)dlg).Text = "Receipt Preview - 80mm Thermal"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Print Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsVirtualOrPdfPrinter(string printerName)
        {
            if (string.IsNullOrEmpty(printerName)) return true;
            string lower = printerName.ToLowerInvariant();
            return lower.Contains("pdf") || lower.Contains("xps") || lower.Contains("onenote") || 
                   lower.Contains("fax") || lower.Contains("document writer") || lower.Contains("virtual");
        }

        public static void Print(int saleId)
        {
            try
            {
                PrintDocument doc = BuildCustomerBillDocument(saleId);
                string printer = doc.PrinterSettings.PrinterName;
                if (IsVirtualOrPdfPrinter(printer))
                {
                    // Open clean 80mm preview instead of popup asking to save PDF file to system drive
                    ShowPreview(saleId);
                    return;
                }
                doc.PrintController = new StandardPrintController(); // Silent printing without "Printing Page 1..." popup
                doc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt: {ex.Message}\nPlease check your printer connection.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void PrintKOT(int kotId)
        {
            try
            {
                PrintDocument doc = BuildKotDocument(kotId);
                string printer = doc.PrinterSettings.PrinterName;
                // If no physical thermal printer is configured or default is PDF,
                // do NOT prompt to save PDF in system drive.
                if (IsVirtualOrPdfPrinter(printer))
                {
                    return;
                }
                doc.PrintController = new StandardPrintController(); // Silent printing without "Printing Page 1..." popup
                doc.Print();
            }
            catch
            {
                // Silently ignore if thermal printer is temporarily disconnected
            }
        }

        public static void ShowKOTPreview(int kotId)
        {
            try
            {
                PrintDocument doc = BuildKotDocument(kotId);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.Size = new Size(360, 600);
                try { ((Form)dlg).Text = "KOT Kitchen Ticket Preview (80mm)"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating KOT preview: {ex.Message}", "KOT Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void PrintVoidKOT(int kotId, string reason)
        {
            try
            {
                PrintDocument doc = BuildVoidKotDocument(kotId, reason);
                string printer = doc.PrinterSettings.PrinterName;
                if (IsVirtualOrPdfPrinter(printer))
                {
                    return;
                }
                doc.PrintController = new StandardPrintController();
                doc.Print();
            }
            catch { }
        }
        #endregion

        #region Printer Setup & Discovery
        private static string FindThermalPrinter(string configuredName = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(configuredName))
                {
                    foreach (string p in PrinterSettings.InstalledPrinters)
                    {
                        if (p.Equals(configuredName, StringComparison.OrdinalIgnoreCase))
                            return p;
                    }
                }

                // Auto-detect TVS RP 3220, POS-80, Thermal printer
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    string lower = p.ToLowerInvariant();
                    if (lower.Contains("3220") || lower.Contains("rp 3220") || lower.Contains("rp3220") ||
                        lower.Contains("pos-80") || lower.Contains("pos80") || lower.Contains("receipt") ||
                        lower.Contains("80mm") || lower.Contains("thermal") || lower.Contains("kitchen") || lower.Contains("tvs"))
                    {
                        return p;
                    }
                }
            }
            catch { }
            return null; // Fallback to Windows default printer
        }
        #endregion

        #region Data Models
        private class CafeBillItem
        {
            public string Name;
            public int Qty;
            public decimal Rate;
            public decimal Amt;
        }

        private class CafeBillData
        {
            public string ShopName = "The Local Cafe";
            public string LogoPath = "";
            public string GSTIN = "11BIDPB3498K1ZD";
            public string Address = "vajra world Mall Balwa khani,\nGangtok Sikkim 737101";
            public string ContactNo = "9971592652";
            public string OrderType = "DINING";
            public string TableNumber = "5";
            public string BillNo = "6";
            public string DateStr = "";
            public string Kots = "";
            public List<CafeBillItem> Items = new List<CafeBillItem>();
            public int TotalQty = 0;
            public decimal SubTotal = 0;
            public decimal Discount = 0;
            public decimal GstPercent = 5.0m;
            public decimal GstAmount = 0;
            public decimal CgstAmount = 0;
            public decimal SgstAmount = 0;
            public decimal RoundOff = 0;
            public decimal TotalInvoiceValue = 0;
            public string FooterGreeting = "Tashi Delek! Thukje Che!";
            public string Branding = "Powered by - MeroDokan";
            public string BillingPrinter = null;
        }

        private class KotData
        {
            public int KotNumber = 0;
            public string TableNumber = "";
            public string OrderType = "DINING";
            public string BillNumber = "";
            public string Steward = "";
            public string DateStr = "";
            public string KotComment = "";
            public List<CafeBillItem> Items = new List<CafeBillItem>();
            public bool IsVoid = false;
            public string VoidReason = "";
            public string KitchenPrinter = null;
        }
        #endregion

        #region Document Builders
        private static PrintDocument BuildCustomerBillDocument(int saleId)
        {
            CafeBillData d = LoadCustomerBillData(saleId);
            int pageHeight = EstimateCustomerBillHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "CafeBill_" + d.BillNo;

            string printer = FindThermalPrinter(d.BillingPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawCustomerBill(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        private static PrintDocument BuildKotDocument(int kotId)
        {
            KotData d = LoadKotData(kotId, false, null);
            int pageHeight = EstimateKotHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "KOT_" + d.KotNumber;

            string printer = FindThermalPrinter(d.KitchenPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawKotSlip(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        private static PrintDocument BuildVoidKotDocument(int kotId, string reason)
        {
            KotData d = LoadKotData(kotId, true, reason);
            int pageHeight = EstimateKotHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "VoidKOT_" + d.KotNumber;

            string printer = FindThermalPrinter(d.KitchenPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawKotSlip(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }
        #endregion

        #region Data Loaders
        private static CafeBillData LoadCustomerBillData(int saleId)
        {
            CafeBillData d = new CafeBillData();

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                // 1. App Profile
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, GSTIN, Address, Phone, ISNULL(ReceiptFooterText, 'Tashi Delek! Thukje Che!') AS ReceiptFooterText, BillingPrinterName, LogoPath FROM AppProfile", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        d.ShopName = r["ShopName"]?.ToString() ?? "The Local Cafe";
                        d.GSTIN = r["GSTIN"]?.ToString() ?? "";
                        d.Address = r["Address"]?.ToString() ?? "";
                        d.ContactNo = r["Phone"]?.ToString() ?? "";
                        d.FooterGreeting = r["ReceiptFooterText"]?.ToString() ?? "Tashi Delek! Thukje Che!";
                        d.BillingPrinter = r["BillingPrinterName"]?.ToString();
                        d.LogoPath = r["LogoPath"]?.ToString();
                    }
                }

                // Fallback resolution for logo
                if (!string.IsNullOrEmpty(d.LogoPath) && !File.Exists(d.LogoPath))
                {
                    string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, d.LogoPath);
                    if (File.Exists(candidate)) d.LogoPath = candidate;
                }
                if (string.IsNullOrEmpty(d.LogoPath) || !File.Exists(d.LogoPath))
                {
                    string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                    string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                    string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                    if (File.Exists(p1)) d.LogoPath = p1;
                    else if (File.Exists(p2)) d.LogoPath = p2;
                    else if (File.Exists(p3)) d.LogoPath = p3;
                }

                // 2. Sales Record
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT s.InvoiceNumber, s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, 
                           ISNULL(s.OrderType, 'DINING') AS OrderType, ISNULL(s.TableNumber, '') AS TableNumber, 
                           ISNULL(s.KotNumbers, '') AS KotNumbers, ISNULL(s.PackingCharges, 0) AS PackingCharges,
                           ISNULL(s.CGSTAmount, 0) AS CGSTAmount, ISNULL(s.SGSTAmount, 0) AS SGSTAmount,
                           ISNULL(s.RoundOff, 0) AS RoundOff, ISNULL(s.TaxableAmount, s.SubTotal) AS TaxableAmount
                    FROM Sales s
                    WHERE s.Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", saleId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            d.BillNo = r["InvoiceNumber"]?.ToString() ?? "";
                            d.DateStr = Convert.ToDateTime(r["SaleDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                            d.OrderType = r["OrderType"].ToString();
                            d.TableNumber = r["TableNumber"].ToString();
                            d.Kots = r["KotNumbers"].ToString();
                            d.TotalInvoiceValue = Convert.ToDecimal(r["GrandTotal"]);
                            d.SubTotal = Convert.ToDecimal(r["TaxableAmount"]);
                            d.Discount = r["Discount"] != DBNull.Value ? Convert.ToDecimal(r["Discount"]) : 0;
                            d.GstAmount = Convert.ToDecimal(r["Tax"]);
                            d.CgstAmount = Convert.ToDecimal(r["CGSTAmount"]);
                            d.SgstAmount = Convert.ToDecimal(r["SGSTAmount"]);
                            d.RoundOff = Convert.ToDecimal(r["RoundOff"]);

                            // If tax split was not stored separately, split 50-50
                            if (d.CgstAmount == 0 && d.SgstAmount == 0 && d.GstAmount > 0)
                            {
                                d.CgstAmount = Math.Round(d.GstAmount / 2.0m, 2);
                                d.SgstAmount = d.GstAmount - d.CgstAmount;
                            }
                        }
                    }
                }

                // 3. Sale Items
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        CASE WHEN sd.ItemType = 'Service' THEN s.Name ELSE p.Name END AS ItemName,
                        sd.Quantity, sd.UnitPrice, sd.Total,
                        ISNULL(sd.TaxableAmount, sd.Total) AS TaxableAmount
                    FROM SaleDetails sd
                    LEFT JOIN Products p ON sd.ProductId = p.Id
                    LEFT JOIN Services s ON sd.ServiceId = s.Id
                    WHERE sd.SaleId = @id
                    ORDER BY sd.Id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", saleId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var it = new CafeBillItem();
                            it.Name = r["ItemName"]?.ToString() ?? "Item";
                            it.Qty = Convert.ToInt32(r["Quantity"]);
                            it.Rate = Convert.ToDecimal(r["UnitPrice"]);
                            it.Amt = Convert.ToDecimal(r["TaxableAmount"]);
                            d.Items.Add(it);
                            d.TotalQty += it.Qty;
                        }
                    }
                }
            }

            return d;
        }

        private static KotData LoadKotData(int kotId, bool isVoid, string voidReason)
        {
            KotData d = new KotData();
            d.IsVoid = isVoid;
            d.VoidReason = voidReason;

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 KitchenPrinterName FROM AppProfile", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        d.KitchenPrinter = r["KitchenPrinterName"]?.ToString();
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT k.KOTNumber, k.TableNumber, k.OrderType, ISNULL(k.Steward, '') AS Steward,
                           k.CreatedAt, ISNULL(k.KotComment, '') AS KotComment, ISNULL(s.InvoiceNumber, '') AS InvoiceNumber
                    FROM KOTMaster k
                    LEFT JOIN Sales s ON k.SaleId = s.Id
                    WHERE k.Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", kotId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            d.KotNumber = Convert.ToInt32(r["KOTNumber"]);
                            d.TableNumber = r["TableNumber"].ToString();
                            d.OrderType = r["OrderType"].ToString();
                            d.Steward = r["Steward"].ToString();
                            d.DateStr = Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss");
                            d.KotComment = r["KotComment"].ToString();
                            d.BillNumber = r["InvoiceNumber"]?.ToString() ?? "";
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ItemName, Quantity, Rate, Amount, ISNULL(Instructions, '') AS Instructions, IsVoided, ISNULL(VoidReason, '') AS VoidReason
                    FROM KOTDetails
                    WHERE KOTId = @id
                    ORDER BY Id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", kotId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            bool itemVoided = Convert.ToBoolean(r["IsVoided"]);
                            if (isVoid && !itemVoided) continue; // If void slip, only show voided items

                            var it = new CafeBillItem();
                            it.Name = r["ItemName"].ToString();
                            it.Qty = Convert.ToInt32(r["Quantity"]);
                            d.Items.Add(it);
                        }
                    }
                }
            }

            return d;
        }
        #endregion

        #region Layout & Drawing Logic (The Local Cafe Format)
        private static Font FontRegular(float size) { return new Font("Consolas", size, FontStyle.Regular); }
        private static Font FontBold(float size) { return new Font("Consolas", size, FontStyle.Bold); }

        private static int EstimateCustomerBillHeight(CafeBillData d)
        {
            int h = 220; // Header and initial meta
            if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
            {
                h += 75;
            }
            if (!string.IsNullOrEmpty(d.Address))
            {
                h += Math.Max(20, (d.Address.Length / 25 + 1) * 18);
            }
            h += d.Items.Count * 28;
            if (d.Discount > 0) h += 25;
            h += 180; // Subtotals, Taxes, Round Off, Total
            h += 90; // Footer greetings & feed
            return Math.Max(h, 520);
        }

        private static int EstimateKotHeight(KotData d)
        {
            int h = 170;
            h += d.Items.Count * 28;
            h += 90;
            return Math.Max(h, 320);
        }

        private static void DrawDashedLine(Graphics g, float y)
        {
            using (Pen p = new Pen(Color.Black, 1))
            {
                p.DashPattern = new float[] { 3, 2 };
                g.DrawLine(p, MarginLeft, y, MarginLeft + UsableWidth, y);
            }
        }

        private static void DrawCustomerBill(Graphics g, CafeBillData d)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            Brush br = Brushes.Black;
            StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };

            using (Font fHead = FontBold(9.5f))
            using (Font fBody = FontRegular(8.5f))
            using (Font fBold = FontBold(8.5f))
            using (Font fFoot = FontRegular(8f))
            {
                float y = 10;

                // 0. Cafe Logo (Centered)
                if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
                {
                    try
                    {
                        using (Image img = Image.FromFile(d.LogoPath))
                        {
                            float logoSize = 65f;
                            float logoX = MarginLeft + (UsableWidth - logoSize) / 2f;
                            g.DrawImage(img, logoX, y, logoSize, logoSize);
                            y += logoSize + 6;
                        }
                    }
                    catch { }
                }

                // 1. Header (Centered)
                if (!string.IsNullOrEmpty(d.ShopName))
                {
                    SizeF shopSz = g.MeasureString(d.ShopName, fHead, (int)UsableWidth, sfCenter);
                    float h = Math.Max(18f, (float)Math.Ceiling(shopSz.Height));
                    g.DrawString(d.ShopName, fHead, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                if (!string.IsNullOrEmpty(d.GSTIN))
                {
                    SizeF gstSz = g.MeasureString("GST No: " + d.GSTIN, fBody, (int)UsableWidth, sfCenter);
                    float h = Math.Max(16f, (float)Math.Ceiling(gstSz.Height));
                    g.DrawString("GST No: " + d.GSTIN, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                if (!string.IsNullOrEmpty(d.Address))
                {
                    string[] addrLines = d.Address.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in addrLines)
                    {
                        SizeF sz = g.MeasureString(line, fBody, (int)UsableWidth, sfCenter);
                        float h = Math.Max(15f, (float)Math.Ceiling(sz.Height));
                        g.DrawString(line, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                        y += h + 2;
                    }
                }

                if (!string.IsNullOrEmpty(d.ContactNo))
                {
                    SizeF phoneSz = g.MeasureString("Contact no: " + d.ContactNo, fBody, (int)UsableWidth, sfCenter);
                    float h = Math.Max(16f, (float)Math.Ceiling(phoneSz.Height));
                    g.DrawString("Contact no: " + d.ContactNo, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 2. Order Metadata
                string typeUpper = d.OrderType.ToUpperInvariant();
                g.DrawString("Type: " + (typeUpper.Contains("TAKE") ? "Take Away" : typeUpper), fBody, br, MarginLeft, y);
                y += 15;

                if (!string.IsNullOrEmpty(d.TableNumber) && !typeUpper.Contains("TAKE"))
                {
                    g.DrawString("Table Number: " + d.TableNumber, fBody, br, MarginLeft, y);
                    y += 15;
                }

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Bill No: " + d.BillNo, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Date: " + d.DateStr, fBody, br, MarginLeft, y);
                y += 15;

                if (!string.IsNullOrEmpty(d.Kots))
                {
                    g.DrawString("Kots: " + d.Kots, fBody, br, MarginLeft, y);
                    y += 15;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 3. Table Column Headers
                g.DrawString("Item", fBody, br, MarginLeft, y);
                g.DrawString("Qty", fBody, br, MarginLeft + 155, y);
                g.DrawString("Amt", fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                // 4. Line Items (Wrapped nicely so long names are fully visible)
                foreach (var it in d.Items)
                {
                    string itemName = it.Name ?? "";
                    SizeF nameSz = g.MeasureString(itemName, fBody, 150, sfLeft);
                    float rowH = Math.Max(16f, (float)Math.Ceiling(nameSz.Height));

                    g.DrawString(itemName, fBody, br, new RectangleF(MarginLeft, y, 150, rowH), sfLeft);
                    g.DrawString(it.Qty.ToString(), fBody, br, MarginLeft + 160, y);
                    g.DrawString(it.Amt.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += rowH + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 5. Total Qty & SubTotal
                g.DrawString("Total Qty:", fBody, br, MarginLeft, y);
                g.DrawString(d.TotalQty.ToString(), fBody, br, MarginLeft + 160, y);
                y += 15;

                g.DrawString("SubTotal:", fBody, br, MarginLeft, y);
                g.DrawString(d.SubTotal.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                if (d.Discount > 0)
                {
                    g.DrawString("Offers / Disc:", fBody, br, MarginLeft, y);
                    g.DrawString("-" + d.Discount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += 18;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 6. GST Breakdown (5% Split into CGST 2.5% & SGST 2.5%)
                g.DrawString("GST@5%:", fBody, br, MarginLeft, y);
                g.DrawString(d.GstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 15;

                g.DrawString("  CGST @2.5%", fBody, br, MarginLeft, y);
                g.DrawString(d.CgstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 15;

                g.DrawString("  SGST @2.5%", fBody, br, MarginLeft, y);
                g.DrawString(d.SgstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                DrawDashedLine(g, y);
                y += 6;

                // 7. Round Off & Total Invoice Value
                g.DrawString("Round Off:", fBody, br, MarginLeft, y);
                g.DrawString(d.RoundOff.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                g.DrawString("Total Invoice Value:", fBold, br, MarginLeft, y);
                g.DrawString(d.TotalInvoiceValue.ToString("0"), fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfRight);
                y += 20;

                DrawDashedLine(g, y);
                y += 10;

                // 8. Custom Greeting & Branding (Centered)
                if (!string.IsNullOrEmpty(d.FooterGreeting))
                {
                    SizeF greetSz = g.MeasureString(d.FooterGreeting, fBody, (int)UsableWidth, sfCenter);
                    float greetH = Math.Max(16f, (float)Math.Ceiling(greetSz.Height));
                    g.DrawString(d.FooterGreeting, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, greetH), sfCenter);
                    y += greetH + 4;
                }

                DrawDashedLine(g, y);
                y += 8;

                if (!string.IsNullOrEmpty(d.Branding))
                {
                    g.DrawString(d.Branding, fFoot, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                    y += 22;
                }
            }
        }

        private static void DrawKotSlip(Graphics g, KotData d)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            Brush br = Brushes.Black;
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

            using (Font fHead = FontBold(9.5f))
            using (Font fBody = FontRegular(8.5f))
            using (Font fBold = FontBold(8.5f))
            {
                float y = 10;

                if (d.IsVoid)
                {
                    g.DrawString("Void KOT Item", fHead, br, MarginLeft, y);
                    y += 18;
                }
                else
                {
                    g.DrawString("KITCHEN ORDER TICKET", fHead, br, MarginLeft, y);
                    y += 18;
                }

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Type:" + d.OrderType, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("T No:" + d.TableNumber, fBold, br, MarginLeft, y);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                if (!string.IsNullOrEmpty(d.BillNumber))
                {
                    g.DrawString("Bill Number:" + d.BillNumber, fBody, br, MarginLeft, y);
                    y += 15;
                }
                g.DrawString("Steward:" + d.Steward, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Date:" + d.DateStr, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Kot Number:" + d.KotNumber, fBold, br, MarginLeft, y);
                y += 18;

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Item", fBody, br, MarginLeft, y);
                g.DrawString("Qty", fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                StringFormat sfItemLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
                int totalQty = 0;
                foreach (var it in d.Items)
                {
                    string prefix = d.IsVoid ? "* " + it.Name : "* " + it.Name;
                    string qtyStr = d.IsVoid ? "- " + it.Qty : it.Qty.ToString();
                    totalQty += it.Qty;

                    SizeF sz = g.MeasureString(prefix, fBold, 180, sfItemLeft);
                    float rowH = Math.Max(18f, (float)Math.Ceiling(sz.Height));

                    g.DrawString(prefix, fBold, br, new RectangleF(MarginLeft, y, 180, rowH), sfItemLeft);
                    g.DrawString(qtyStr, fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += rowH + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                string totalQtyStr = d.IsVoid ? "- " + totalQty : totalQty.ToString();
                g.DrawString("Total Qty:", fBold, br, MarginLeft, y);
                g.DrawString(totalQtyStr, fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                DrawDashedLine(g, y);
                y += 8;

                if (d.IsVoid && !string.IsNullOrEmpty(d.VoidReason))
                {
                    string note = "Void Item Comment: " + d.VoidReason;
                    SizeF nSz = g.MeasureString(note, fBody, (int)UsableWidth, sfItemLeft);
                    float nH = Math.Max(18f, (float)Math.Ceiling(nSz.Height));
                    g.DrawString(note, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, nH), sfItemLeft);
                    y += nH + 4;
                }
                else if (!string.IsNullOrEmpty(d.KotComment))
                {
                    string note = "Notes: " + d.KotComment;
                    SizeF nSz = g.MeasureString(note, fBody, (int)UsableWidth, sfItemLeft);
                    float nH = Math.Max(18f, (float)Math.Ceiling(nSz.Height));
                    g.DrawString(note, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, nH), sfItemLeft);
                    y += nH + 4;
                }
            }
        }
        #endregion
    }
}
