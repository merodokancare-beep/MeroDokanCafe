using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroDokan
{
    public class QRPaymentDialog : Form
    {
        private PictureBox picQR;
        private Label lblAmount;
        private Label lblPayeeInfo;
        private Label lblInvoiceRef;
        private Button btnConfirm;
        private Button btnCopy;
        private Button btnCancel;
        private string currentUPIString = "";

        public QRPaymentDialog(string upiId, string payeeName, decimal amount, string invoiceNumber)
        {
            InitializeComponent(upiId, payeeName, amount, invoiceNumber);
        }

        private void InitializeComponent(string upiId, string payeeName, decimal amount, string invoiceNumber)
        {
            this.Text = "UPI / QR Digital Payment";
            this.Size = new Size(460, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Theme.Secondary;
            this.ForeColor = Theme.TextLight;

            // Main Container Card
            Panel card = Theme.CreateCard(420, 520);
            card.Location = new Point(12, 12);
            card.BackColor = Theme.CardBg;
            this.Controls.Add(card);

            // 1. Header Title & Subtitle
            Label lblTitle = new Label();
            lblTitle.Text = "📱 Instant QR / UPI Payment";
            lblTitle.Location = new Point(15, 12);
            lblTitle.Size = new Size(390, 26);
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Accent;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblTitle);

            lblInvoiceRef = new Label();
            lblInvoiceRef.Text = string.IsNullOrEmpty(invoiceNumber) ? "Direct Counter Checkout" : $"Invoice Ref: {invoiceNumber}";
            lblInvoiceRef.Location = new Point(15, 38);
            lblInvoiceRef.Size = new Size(390, 18);
            lblInvoiceRef.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblInvoiceRef.ForeColor = Theme.TextMuted;
            lblInvoiceRef.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblInvoiceRef);

            // 2. Amount Box (Prominent)
            Panel amountBox = new Panel();
            amountBox.Size = new Size(380, 48);
            amountBox.Location = new Point(20, 60);
            amountBox.BackColor = Color.FromArgb(24, 40, 60);
            amountBox.BorderStyle = BorderStyle.FixedSingle;
            card.Controls.Add(amountBox);

            lblAmount = new Label();
            lblAmount.Text = $"Payable: Rs. {amount:N2}";
            lblAmount.Dock = DockStyle.Fill;
            lblAmount.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblAmount.ForeColor = Theme.Success;
            lblAmount.TextAlign = ContentAlignment.MiddleCenter;
            amountBox.Controls.Add(lblAmount);

            // 3. QR Code Picture Box (White background card for camera contrast)
            Panel qrWrapper = new Panel();
            qrWrapper.Size = new Size(220, 220);
            qrWrapper.Location = new Point(100, 116);
            qrWrapper.BackColor = Color.White;
            qrWrapper.Padding = new Padding(10);
            card.Controls.Add(qrWrapper);

            picQR = new PictureBox();
            picQR.Dock = DockStyle.Fill;
            picQR.SizeMode = PictureBoxSizeMode.Zoom;
            picQR.BackColor = Color.White;
            qrWrapper.Controls.Add(picQR);

            // Generate UPI String and QR Bitmap
            string finalUPIId = !string.IsNullOrWhiteSpace(upiId) ? upiId.Trim() : "merchant@upi";
            string finalPayee = !string.IsNullOrWhiteSpace(payeeName) ? payeeName.Trim() : "The Local Cafe";
            currentUPIString = BarcodeHelper.GenerateUPIString(finalUPIId, finalPayee, amount, invoiceNumber);

            Bitmap qrBmp = BarcodeHelper.GenerateQRCodeBitmap(currentUPIString, 12);
            if (qrBmp != null)
            {
                picQR.Image = qrBmp;
            }

            // 4. Payee Information & Instructions
            lblPayeeInfo = new Label();
            lblPayeeInfo.Text = $"Payee: {finalPayee}  |  UPI ID: {finalUPIId}";
            lblPayeeInfo.Location = new Point(15, 342);
            lblPayeeInfo.Size = new Size(390, 20);
            lblPayeeInfo.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            lblPayeeInfo.ForeColor = Theme.TextLight;
            lblPayeeInfo.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblPayeeInfo);

            Label lblApps = new Label();
            lblApps.Text = "⚡ Scan with Google Pay, PhonePe, Paytm, BHIM or any UPI / QR App";
            lblApps.Location = new Point(15, 364);
            lblApps.Size = new Size(390, 32);
            lblApps.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            lblApps.ForeColor = Theme.TextMuted;
            lblApps.TextAlign = ContentAlignment.MiddleCenter;
            card.Controls.Add(lblApps);

            // 5. Action Buttons
            btnConfirm = new Button();
            btnConfirm.Text = "✅  Payment Received (Complete)";
            btnConfirm.Size = new Size(380, 42);
            btnConfirm.Location = new Point(20, 404);
            Theme.StylePrimaryButton(btnConfirm);
            btnConfirm.BackColor = Theme.Success;
            btnConfirm.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnConfirm.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            card.Controls.Add(btnConfirm);

            // Bottom Buttons (Copy Link & Cancel)
            btnCopy = new Button();
            btnCopy.Text = "📋 Copy UPI Link";
            btnCopy.Size = new Size(185, 34);
            btnCopy.Location = new Point(20, 454);
            Theme.StyleSecondaryButton(btnCopy);
            btnCopy.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            btnCopy.Click += (s, e) => {
                try
                {
                    Clipboard.SetText(currentUPIString);
                    MessageBox.Show("UPI Payment Link copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            };
            card.Controls.Add(btnCopy);

            btnCancel = new Button();
            btnCancel.Text = "✖ Cancel / Switch Mode";
            btnCancel.Size = new Size(185, 34);
            btnCancel.Location = new Point(215, 454);
            Theme.StyleSecondaryButton(btnCancel);
            btnCancel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            card.Controls.Add(btnCancel);
        }
    }
}
