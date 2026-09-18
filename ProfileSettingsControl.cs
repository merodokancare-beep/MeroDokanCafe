using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class ProfileSettingsControl : UserControl
    {
        private TextBox txtOwnerName;
        private TextBox txtShopName;
        private TextBox txtGSTIN;
        private TextBox txtPhone;
        private TextBox txtEmail;
        private TextBox txtAddress;
        private TextBox txtBackupFolder;
        private Button btnBrowseBackupPath;
        private string loadedGoogleDriveAddress = "";
        private ComboBox comboState;
        private ComboBox comboDefaultBillType;
        private ComboBox comboTaxMode;
        private ComboBox comboDefaultGSTRate;
        private ComboBox comboThemePreset;
        private ComboBox comboFontSize;
        
        private TextBox txtUPIId;
        private TextBox txtUPIName;
        private CheckBox chkAutoShowQR;
        private CheckBox chkPrintQROnReceipt;

        private PictureBox picProfilePic;
        private PictureBox picLogo;
        private string loadedProfilePicPath = "";
        private string loadedLogoPath = "";
        
        private Button btnUploadProfilePic;
        private Button btnUploadLogo;
        private Button btnSave;

        // Callback event when settings are successfully saved (notifies MainForm to reload colors)
        public event Action OnSettingsSaved;

        public ProfileSettingsControl()
        {
            InitializeComponent();
            LoadProfileData();
            this.Load += (s, e) => txtOwnerName.Focus();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(950, 520);
            this.AutoScroll = true;
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "☕ Cafe Profile & Application Settings Panel";
            lblHeader.Location = new Point(20, 12);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Responsive Layout Table for split columns
            TableLayoutPanel splitLayout = new TableLayoutPanel();
            splitLayout.Location = new Point(20, 48);
            splitLayout.Size = new Size(910, 430);
            splitLayout.ColumnCount = 2;
            splitLayout.RowCount = 1;
            splitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            splitLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            splitLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            splitLayout.BackColor = Color.Transparent;
            this.Controls.Add(splitLayout);

            // ==========================================
            // LEFT COLUMN: Cafe Details
            // ==========================================
            Panel leftPanel = Theme.CreateCard(440, 420);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Margin = new Padding(0, 0, 10, 0);
            splitLayout.Controls.Add(leftPanel, 0, 0);

            Label lblDetailsHeader = new Label();
            lblDetailsHeader.Text = "Store Owner & Identity Information";
            lblDetailsHeader.Location = new Point(15, 12);
            lblDetailsHeader.AutoSize = true;
            Theme.StyleLabel(lblDetailsHeader, Theme.TextLight, Theme.SubHeaderFont);
            leftPanel.Controls.Add(lblDetailsHeader);

            // 1. Owner Name
            Label lblOwnerName = new Label();
            lblOwnerName.Text = "Shop Owner / Manager Full Name *";
            lblOwnerName.Location = new Point(15, 36);
            lblOwnerName.AutoSize = true;
            Theme.StyleLabel(lblOwnerName, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblOwnerName);

            txtOwnerName = new TextBox();
            txtOwnerName.Size = new Size(410, 26);
            txtOwnerName.Location = new Point(15, 54);
            Theme.StyleTextBox(txtOwnerName);
            leftPanel.Controls.Add(txtOwnerName);

            // 2. Shop Name
            Label lblShopName = new Label();
            lblShopName.Text = "Registered Cafe / Restaurant Name *";
            lblShopName.Location = new Point(15, 84);
            lblShopName.AutoSize = true;
            Theme.StyleLabel(lblShopName, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblShopName);

            txtShopName = new TextBox();
            txtShopName.Size = new Size(410, 26);
            txtShopName.Location = new Point(15, 102);
            Theme.StyleTextBox(txtShopName);
            leftPanel.Controls.Add(txtShopName);

            // 3. GSTIN (Left) & State / Place of Supply (Right)
            Label lblGSTIN = new Label();
            lblGSTIN.Text = "Cafe GSTIN *";
            lblGSTIN.Location = new Point(15, 132);
            lblGSTIN.AutoSize = true;
            Theme.StyleLabel(lblGSTIN, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblGSTIN);

            txtGSTIN = new TextBox();
            txtGSTIN.Size = new Size(195, 26);
            txtGSTIN.Location = new Point(15, 150);
            Theme.StyleTextBox(txtGSTIN);
            leftPanel.Controls.Add(txtGSTIN);

            Label lblState = new Label();
            lblState.Text = "Place of Supply / State *";
            lblState.Location = new Point(220, 132);
            lblState.AutoSize = true;
            Theme.StyleLabel(lblState, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblState);

            comboState = new ComboBox();
            comboState.Size = new Size(205, 26);
            comboState.Location = new Point(220, 150);
            comboState.DropDownStyle = ComboBoxStyle.DropDownList;
            comboState.BackColor = Theme.Primary;
            comboState.ForeColor = Theme.TextLight;
            comboState.Font = Theme.MainFont;
            foreach (var st in IndianGSTHelper.GetIndianStates())
            {
                comboState.Items.Add(st);
            }
            if (comboState.Items.Count > 6) comboState.SelectedIndex = 6; // Default to Delhi (07)
            leftPanel.Controls.Add(comboState);

            // 4. Contact Phone (Left) & Email Address (Right)
            Label lblPhone = new Label();
            lblPhone.Text = "Official Phone *";
            lblPhone.Location = new Point(15, 180);
            lblPhone.AutoSize = true;
            Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblPhone);

            txtPhone = new TextBox();
            txtPhone.Size = new Size(195, 26);
            txtPhone.Location = new Point(15, 198);
            Theme.StyleTextBox(txtPhone);
            leftPanel.Controls.Add(txtPhone);

            Label lblEmail = new Label();
            lblEmail.Text = "Official Email *";
            lblEmail.Location = new Point(220, 180);
            lblEmail.AutoSize = true;
            Theme.StyleLabel(lblEmail, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblEmail);

            txtEmail = new TextBox();
            txtEmail.Size = new Size(205, 26);
            txtEmail.Location = new Point(220, 198);
            Theme.StyleTextBox(txtEmail);
            leftPanel.Controls.Add(txtEmail);

            // 5. Address
            Label lblAddress = new Label();
            lblAddress.Text = "Cafe Location Address *";
            lblAddress.Location = new Point(15, 228);
            lblAddress.AutoSize = true;
            Theme.StyleLabel(lblAddress, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblAddress);

            txtAddress = new TextBox();
            txtAddress.Size = new Size(410, 26);
            txtAddress.Location = new Point(15, 246);
            Theme.StyleTextBox(txtAddress);
            leftPanel.Controls.Add(txtAddress);

            // 6. Local Backup Folder
            Label lblBackupFolder = new Label();
            lblBackupFolder.Text = "Daily Database Backup Path *";
            lblBackupFolder.Location = new Point(15, 276);
            lblBackupFolder.AutoSize = true;
            Theme.StyleLabel(lblBackupFolder, Theme.TextLight, Theme.BoldFont);
            leftPanel.Controls.Add(lblBackupFolder);

            txtBackupFolder = new TextBox();
            txtBackupFolder.Size = new Size(280, 26);
            txtBackupFolder.Location = new Point(15, 294);
            Theme.StyleTextBox(txtBackupFolder);
            leftPanel.Controls.Add(txtBackupFolder);

            btnBrowseBackupPath = new Button();
            btnBrowseBackupPath.Text = "📂 Browse...";
            btnBrowseBackupPath.Size = new Size(120, 28);
            btnBrowseBackupPath.Location = new Point(305, 293);
            Theme.StyleSecondaryButton(btnBrowseBackupPath);
            btnBrowseBackupPath.Click += BtnBrowseBackupPath_Click;
            leftPanel.Controls.Add(btnBrowseBackupPath);

            // 7. Info Footer Note
            Label lblInfo = new Label();
            lblInfo.Text = "💡 Cafe branding, GSTIN, and address appear automatically on invoices and thermal receipts.";
            lblInfo.Location = new Point(15, 332);
            lblInfo.Size = new Size(410, 32);
            lblInfo.ForeColor = Theme.TextMuted;
            lblInfo.Font = Theme.SmallFont;
            leftPanel.Controls.Add(lblInfo);

            // ==========================================
            // RIGHT COLUMN: Theme & Image Branding
            // ==========================================
            Panel rightPanel = Theme.CreateCard(440, 420);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Margin = new Padding(10, 0, 0, 0);
            splitLayout.Controls.Add(rightPanel, 1, 0);

            Label lblBrandingHeader = new Label();
            lblBrandingHeader.Text = "Application Theme & Image Branding Options";
            lblBrandingHeader.Location = new Point(15, 12);
            lblBrandingHeader.AutoSize = true;
            Theme.StyleLabel(lblBrandingHeader, Theme.TextLight, Theme.SubHeaderFont);
            rightPanel.Controls.Add(lblBrandingHeader);

            // 1. Theme Preset & Font Size (Side by Side)
            Label lblTheme = new Label();
            lblTheme.Text = "Choose Preset Theme *";
            lblTheme.Location = new Point(15, 36);
            lblTheme.AutoSize = true;
            Theme.StyleLabel(lblTheme, Theme.TextLight, Theme.BoldFont);
            rightPanel.Controls.Add(lblTheme);

            comboThemePreset = new ComboBox();
            comboThemePreset.Size = new Size(195, 26);
            comboThemePreset.Location = new Point(15, 54);
            comboThemePreset.DropDownStyle = ComboBoxStyle.DropDownList;
            comboThemePreset.BackColor = Theme.Primary;
            comboThemePreset.ForeColor = Theme.TextLight;
            comboThemePreset.Font = Theme.MainFont;
            comboThemePreset.Items.AddRange(new string[] { 
                "Dark Slate", "Emerald Mint", "Deep Olive", "Cyberpunk Purple", "Midnight Blue", 
                "Sunset Crimson", "Ocean Breeze", "Forest Moss", "Rose Gold", 
                "Pure Alabaster", "Snowy Mint", "Nordic Light", "Soft Peach", "Custom Color Picker..." 
            });
            comboThemePreset.SelectedIndex = 0;
            comboThemePreset.SelectedIndexChanged += ComboThemePreset_SelectedIndexChanged;
            rightPanel.Controls.Add(comboThemePreset);

            Label lblFontSize = new Label();
            lblFontSize.Text = "Font Size *";
            lblFontSize.Location = new Point(220, 36);
            lblFontSize.AutoSize = true;
            Theme.StyleLabel(lblFontSize, Theme.TextLight, Theme.BoldFont);
            rightPanel.Controls.Add(lblFontSize);

            comboFontSize = new ComboBox();
            comboFontSize.Size = new Size(205, 26);
            comboFontSize.Location = new Point(220, 54);
            comboFontSize.DropDownStyle = ComboBoxStyle.DropDownList;
            comboFontSize.BackColor = Theme.Primary;
            comboFontSize.ForeColor = Theme.TextLight;
            comboFontSize.Font = Theme.MainFont;
            comboFontSize.Items.AddRange(new string[] { "Small", "Medium", "Large" });
            comboFontSize.SelectedIndex = 1;
            rightPanel.Controls.Add(comboFontSize);

            // 2. GST & TAX BILLING PREFERENCES
            Label lblGstSection = new Label();
            lblGstSection.Text = "🇮🇳 Indian GST & Tax Billing Preferences";
            lblGstSection.Location = new Point(15, 86);
            lblGstSection.AutoSize = true;
            Theme.StyleLabel(lblGstSection, Theme.Accent, Theme.BoldFont);
            rightPanel.Controls.Add(lblGstSection);

            // Default Bill Mode (Left) & Tax Pricing Mode (Right)
            Label lblDefaultBill = new Label();
            lblDefaultBill.Text = "Default Bill Mode *";
            lblDefaultBill.Location = new Point(15, 106);
            lblDefaultBill.AutoSize = true;
            Theme.StyleLabel(lblDefaultBill, Theme.TextLight, Theme.SmallFont);
            rightPanel.Controls.Add(lblDefaultBill);

            comboDefaultBillType = new ComboBox();
            comboDefaultBillType.Size = new Size(195, 26);
            comboDefaultBillType.Location = new Point(15, 124);
            comboDefaultBillType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDefaultBillType.BackColor = Theme.Primary;
            comboDefaultBillType.ForeColor = Theme.TextLight;
            comboDefaultBillType.Font = Theme.MainFont;
            comboDefaultBillType.Items.AddRange(new string[] { "GST Tax Invoice", "Non-GST Retail Bill" });
            comboDefaultBillType.SelectedIndex = 0;
            rightPanel.Controls.Add(comboDefaultBillType);

            Label lblTaxMode = new Label();
            lblTaxMode.Text = "Tax Pricing Mode *";
            lblTaxMode.Location = new Point(220, 106);
            lblTaxMode.AutoSize = true;
            Theme.StyleLabel(lblTaxMode, Theme.TextLight, Theme.SmallFont);
            rightPanel.Controls.Add(lblTaxMode);

            comboTaxMode = new ComboBox();
            comboTaxMode.Size = new Size(205, 26);
            comboTaxMode.Location = new Point(220, 124);
            comboTaxMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTaxMode.BackColor = Theme.Primary;
            comboTaxMode.ForeColor = Theme.TextLight;
            comboTaxMode.Font = Theme.MainFont;
            comboTaxMode.Items.AddRange(new string[] { "Tax Inclusive (MRP includes GST)", "Tax Exclusive (GST on top)" });
            comboTaxMode.SelectedIndex = 0;
            rightPanel.Controls.Add(comboTaxMode);

            // Default Food/Beverage GST Rate
            Label lblDefaultGst = new Label();
            lblDefaultGst.Text = "Default Food/Beverage GST Rate *";
            lblDefaultGst.Location = new Point(15, 154);
            lblDefaultGst.AutoSize = true;
            Theme.StyleLabel(lblDefaultGst, Theme.TextLight, Theme.SmallFont);
            rightPanel.Controls.Add(lblDefaultGst);

            comboDefaultGSTRate = new ComboBox();
            comboDefaultGSTRate.Size = new Size(410, 26);
            comboDefaultGSTRate.Location = new Point(15, 172);
            comboDefaultGSTRate.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDefaultGSTRate.BackColor = Theme.Primary;
            comboDefaultGSTRate.ForeColor = Theme.TextLight;
            comboDefaultGSTRate.Font = Theme.MainFont;
            comboDefaultGSTRate.Items.AddRange(new string[] { "5% GST (Standard Cafe & Restaurant)", "12% GST (Beverages)", "18% GST", "28% GST", "0% (Exempt)" });
            comboDefaultGSTRate.SelectedIndex = 0;
            rightPanel.Controls.Add(comboDefaultGSTRate);

            // 3. DIGITAL UPI / QR PAYMENT SETTINGS
            Label lblUpiSection = new Label();
            lblUpiSection.Text = "📱 Digital UPI / QR Code Payment Settings";
            lblUpiSection.Location = new Point(15, 204);
            lblUpiSection.AutoSize = true;
            Theme.StyleLabel(lblUpiSection, Theme.UPIColor, Theme.BoldFont);
            rightPanel.Controls.Add(lblUpiSection);

            // UPI ID (Left) & UPI Payee Name (Right)
            Label lblUpiId = new Label();
            lblUpiId.Text = "Merchant UPI ID / VPA";
            lblUpiId.Location = new Point(15, 224);
            lblUpiId.AutoSize = true;
            Theme.StyleLabel(lblUpiId, Theme.TextLight, Theme.SmallFont);
            rightPanel.Controls.Add(lblUpiId);

            txtUPIId = new TextBox();
            txtUPIId.Size = new Size(195, 26);
            txtUPIId.Location = new Point(15, 242);
            Theme.StyleTextBox(txtUPIId);
            rightPanel.Controls.Add(txtUPIId);

            Label lblUpiName = new Label();
            lblUpiName.Text = "Merchant Payee Name";
            lblUpiName.Location = new Point(220, 224);
            lblUpiName.AutoSize = true;
            Theme.StyleLabel(lblUpiName, Theme.TextLight, Theme.SmallFont);
            rightPanel.Controls.Add(lblUpiName);

            txtUPIName = new TextBox();
            txtUPIName.Size = new Size(205, 26);
            txtUPIName.Location = new Point(220, 242);
            Theme.StyleTextBox(txtUPIName);
            rightPanel.Controls.Add(txtUPIName);

            // Checkboxes
            chkAutoShowQR = new CheckBox();
            chkAutoShowQR.Text = "Auto-show QR on QR Pay";
            chkAutoShowQR.Location = new Point(15, 274);
            chkAutoShowQR.AutoSize = true;
            chkAutoShowQR.ForeColor = Theme.TextLight;
            chkAutoShowQR.Font = Theme.SmallFont;
            chkAutoShowQR.Checked = true;
            rightPanel.Controls.Add(chkAutoShowQR);

            chkPrintQROnReceipt = new CheckBox();
            chkPrintQROnReceipt.Text = "Print QR on Thermal Receipt";
            chkPrintQROnReceipt.Location = new Point(220, 274);
            chkPrintQROnReceipt.AutoSize = true;
            chkPrintQROnReceipt.ForeColor = Theme.TextLight;
            chkPrintQROnReceipt.Font = Theme.SmallFont;
            chkPrintQROnReceipt.Checked = true;
            rightPanel.Controls.Add(chkPrintQROnReceipt);

            // 4. Compact Image Branding Strip (Avatar & Logo)
            picProfilePic = new PictureBox();
            picProfilePic.Size = new Size(32, 32);
            picProfilePic.Location = new Point(15, 302);
            picProfilePic.BorderStyle = BorderStyle.FixedSingle;
            picProfilePic.SizeMode = PictureBoxSizeMode.Zoom;
            picProfilePic.BackColor = Color.FromArgb(17, 24, 39);
            rightPanel.Controls.Add(picProfilePic);

            btnUploadProfilePic = new Button();
            btnUploadProfilePic.Text = "📷 Avatar";
            btnUploadProfilePic.Size = new Size(100, 30);
            btnUploadProfilePic.Location = new Point(54, 303);
            Theme.StyleSecondaryButton(btnUploadProfilePic);
            btnUploadProfilePic.Click += (s, e) => UploadImage(ref loadedProfilePicPath, picProfilePic);
            rightPanel.Controls.Add(btnUploadProfilePic);

            picLogo = new PictureBox();
            picLogo.Size = new Size(32, 32);
            picLogo.Location = new Point(220, 302);
            picLogo.BorderStyle = BorderStyle.FixedSingle;
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.BackColor = Color.FromArgb(17, 24, 39);
            rightPanel.Controls.Add(picLogo);

            btnUploadLogo = new Button();
            btnUploadLogo.Text = "🖼️ Shop Logo";
            btnUploadLogo.Size = new Size(110, 30);
            btnUploadLogo.Location = new Point(259, 303);
            Theme.StyleSecondaryButton(btnUploadLogo);
            btnUploadLogo.Click += (s, e) => UploadImage(ref loadedLogoPath, picLogo);
            rightPanel.Controls.Add(btnUploadLogo);

            // 5. SAVE SETTINGS BUTTON (Prominent & Unclipped)
            btnSave = new Button();
            btnSave.Text = "💾 SAVE PROFILE CONFIGURATIONS";
            btnSave.Size = new Size(410, 42);
            btnSave.Location = new Point(15, 346);
            Theme.StyleSuccessButton(btnSave);
            btnSave.Click += BtnSave_Click;
            rightPanel.Controls.Add(btnSave);
        }

        private void UploadImage(ref string imagePathField, PictureBox picBox)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Select Image File";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Dispose previous image if any
                        if (picBox.Image != null)
                        {
                            picBox.Image.Dispose();
                            picBox.Image = null;
                        }

                        // Load and display preview safely without keeping the file open / locked
                        byte[] bytes = File.ReadAllBytes(ofd.FileName);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            picBox.Image = Image.FromStream(ms);
                        }

                        imagePathField = ofd.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load image: {ex.Message}", "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadProfileData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 * FROM AppProfile", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                txtOwnerName.Text = rdr["OwnerName"]?.ToString() ?? "Cafe Manager";
                                txtShopName.Text = rdr["ShopName"]?.ToString() ?? "The Local Cafe";
                                txtGSTIN.Text = rdr["GSTIN"] != DBNull.Value ? rdr["GSTIN"].ToString() : "11BIDPB3498K1ZD";
                                txtPhone.Text = rdr["Phone"]?.ToString() ?? "9971592652";
                                txtEmail.Text = rdr["Email"]?.ToString() ?? "contact@thelocalcafe.com";
                                txtAddress.Text = rdr["Address"]?.ToString() ?? "vajra world Mall Balwa khani, Gangtok Sikkim 737101";
                                txtBackupFolder.Text = rdr["BackupFolderPath"] != DBNull.Value ? rdr["BackupFolderPath"].ToString() : @"D:\MeroDokanCafe\DailyDatabaseBackup";
                                loadedGoogleDriveAddress = rdr["GoogleDriveAddress"] != DBNull.Value ? rdr["GoogleDriveAddress"].ToString() : "";

                                // GST State Selection
                                string stateCode = (rdr["StateCode"] != DBNull.Value) ? rdr["StateCode"].ToString() : "07";
                                for (int i = 0; i < comboState.Items.Count; i++)
                                {
                                    if (comboState.Items[i] is GSTState gs && gs.Code == stateCode)
                                    {
                                        comboState.SelectedIndex = i;
                                        break;
                                    }
                                }

                                // Default Bill Type
                                string defBill = (rdr["DefaultBillType"] != DBNull.Value) ? rdr["DefaultBillType"].ToString() : "GST";
                                comboDefaultBillType.SelectedIndex = defBill.Contains("Non") ? 1 : 0;

                                // Tax Mode (Inclusive vs Exclusive)
                                bool isInc = (rdr["IsTaxInclusive"] != DBNull.Value) ? Convert.ToBoolean(rdr["IsTaxInclusive"]) : true;
                                comboTaxMode.SelectedIndex = isInc ? 0 : 1;

                                // Default GST Rate (0: 5%, 1: 12%, 2: 18%, 3: 28%, 4: 0%)
                                decimal defGst = (rdr["DefaultGSTRate"] != DBNull.Value) ? Convert.ToDecimal(rdr["DefaultGSTRate"]) : 5.00m;
                                if (defGst == 5m) comboDefaultGSTRate.SelectedIndex = 0;
                                else if (defGst == 12m) comboDefaultGSTRate.SelectedIndex = 1;
                                else if (defGst == 18m) comboDefaultGSTRate.SelectedIndex = 2;
                                else if (defGst == 28m) comboDefaultGSTRate.SelectedIndex = 3;
                                else if (defGst == 0m) comboDefaultGSTRate.SelectedIndex = 4;
                                else comboDefaultGSTRate.SelectedIndex = 0; // Default 5% for Cafe

                                // Digital UPI & QR Code Settings
                                txtUPIId.Text = (rdr["UPIId"] != DBNull.Value) ? rdr["UPIId"].ToString() : "";
                                txtUPIName.Text = (rdr["UPIName"] != DBNull.Value) ? rdr["UPIName"].ToString() : "";
                                chkAutoShowQR.Checked = (rdr["AutoShowQROnUPI"] != DBNull.Value) ? Convert.ToBoolean(rdr["AutoShowQROnUPI"]) : true;
                                chkPrintQROnReceipt.Checked = (rdr["PrintQROnReceipt"] != DBNull.Value) ? Convert.ToBoolean(rdr["PrintQROnReceipt"]) : true;

                                string preset = rdr["ThemePreset"]?.ToString() ?? "Dark Slate";
                                if (preset.StartsWith("CUSTOM|"))
                                {
                                    customThemeString = preset;
                                    isHandlingThemeChange = true;
                                    if (!comboThemePreset.Items.Contains("Custom Theme"))
                                    {
                                        comboThemePreset.Items.Add("Custom Theme");
                                    }
                                    comboThemePreset.SelectedItem = "Custom Theme";
                                    isHandlingThemeChange = false;
                                }
                                else
                                {
                                    int idx = comboThemePreset.Items.IndexOf(preset);
                                    comboThemePreset.SelectedIndex = idx >= 0 ? idx : 0;
                                }

                                string fontSize = rdr["FontSizePreset"]?.ToString() ?? "Medium";
                                int fontIdx = comboFontSize.Items.IndexOf(fontSize);
                                comboFontSize.SelectedIndex = fontIdx >= 0 ? fontIdx : 1;

                                // Load Profile Pic
                                string profPic = rdr["ProfilePicPath"]?.ToString();
                                if (!string.IsNullOrEmpty(profPic) && File.Exists(profPic))
                                {
                                    loadedProfilePicPath = profPic;
                                    try
                                    {
                                        byte[] bytes = File.ReadAllBytes(profPic);
                                        using (MemoryStream ms = new MemoryStream(bytes))
                                        {
                                            picProfilePic.Image = Image.FromStream(ms);
                                        }
                                    }
                                    catch { }
                                }

                                // Load Logo
                                string logo = rdr["LogoPath"]?.ToString();
                                if (!string.IsNullOrEmpty(logo) && !File.Exists(logo))
                                {
                                    string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logo);
                                    if (File.Exists(candidate)) logo = candidate;
                                }
                                if (string.IsNullOrEmpty(logo) || !File.Exists(logo))
                                {
                                    string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                                    string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                                    if (File.Exists(p1)) logo = p1;
                                    else if (File.Exists(p2)) logo = p2;
                                }

                                if (!string.IsNullOrEmpty(logo) && File.Exists(logo))
                                {
                                    loadedLogoPath = logo;
                                    try
                                    {
                                        byte[] bytes = File.ReadAllBytes(logo);
                                        using (MemoryStream ms = new MemoryStream(bytes))
                                        {
                                            picLogo.Image = Image.FromStream(ms);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profile details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string owner = txtOwnerName.Text.Trim();
            string shop = txtShopName.Text.Trim();
            string gstin = txtGSTIN.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string addr = txtAddress.Text.Trim();
            string backupFolder = txtBackupFolder.Text.Trim();
            string gDriveAddr = !string.IsNullOrEmpty(loadedGoogleDriveAddress) ? loadedGoogleDriveAddress : "https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec";
            string upiId = txtUPIId.Text.Trim();
            string upiName = txtUPIName.Text.Trim();
            bool autoShowQR = chkAutoShowQR.Checked;
            bool printQROnReceipt = chkPrintQROnReceipt.Checked;

            string themePreset = comboThemePreset.SelectedItem?.ToString() ?? "Dark Slate";
            if (themePreset == "Custom Theme" && !string.IsNullOrEmpty(customThemeString))
            {
                themePreset = customThemeString;
            }
            string fontSizePreset = comboFontSize.SelectedItem?.ToString() ?? "Medium";

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(shop) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(addr) || string.IsNullOrEmpty(backupFolder))
            {
                MessageBox.Show("Please fill in all required fields marked with an asterisk (*).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Set up local Assets folder directory for copying images safely
                string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                if (!Directory.Exists(assetsDir))
                {
                    Directory.CreateDirectory(assetsDir);
                }

                // Copy Profile Pic if updated and path is external
                string finalProfilePicPath = loadedProfilePicPath;
                if (!string.IsNullOrEmpty(loadedProfilePicPath) && !loadedProfilePicPath.StartsWith(assetsDir))
                {
                    string ext = Path.GetExtension(loadedProfilePicPath);
                    string dest = Path.Combine(assetsDir, "profile" + ext);

                    // Dispose any active handles to prevent file lock error
                    if (picProfilePic.Image != null)
                    {
                        picProfilePic.Image.Dispose();
                        picProfilePic.Image = null;
                    }

                    File.Copy(loadedProfilePicPath, dest, true);
                    finalProfilePicPath = dest;

                    // Reload
                    byte[] bytes = File.ReadAllBytes(dest);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        picProfilePic.Image = Image.FromStream(ms);
                    }
                    loadedProfilePicPath = dest;
                }

                // Copy Logo if updated and path is external
                string finalLogoPath = loadedLogoPath;
                if (!string.IsNullOrEmpty(loadedLogoPath) && !loadedLogoPath.StartsWith(assetsDir))
                {
                    string ext = Path.GetExtension(loadedLogoPath);
                    string dest = Path.Combine(assetsDir, "logo" + ext);

                    // Dispose any active handles to prevent file lock error
                    if (picLogo.Image != null)
                    {
                        picLogo.Image.Dispose();
                        picLogo.Image = null;
                    }

                    File.Copy(loadedLogoPath, dest, true);
                    finalLogoPath = dest;

                    // Reload
                    byte[] bytes = File.ReadAllBytes(dest);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        picLogo.Image = Image.FromStream(ms);
                    }
                    loadedLogoPath = dest;
                }

                // Extract GST settings
                GSTState selState = comboState.SelectedItem as GSTState ?? new GSTState { Code = "07", Name = "Delhi" };
                bool isTaxInclusive = comboTaxMode.SelectedIndex == 0;
                string defaultBillType = comboDefaultBillType.SelectedIndex == 1 ? "Non-GST" : "GST";
                decimal defaultGSTRate = 5.00m;
                if (comboDefaultGSTRate.SelectedIndex == 0) defaultGSTRate = 5.00m;
                else if (comboDefaultGSTRate.SelectedIndex == 1) defaultGSTRate = 12.00m;
                else if (comboDefaultGSTRate.SelectedIndex == 2) defaultGSTRate = 18.00m;
                else if (comboDefaultGSTRate.SelectedIndex == 3) defaultGSTRate = 28.00m;
                else if (comboDefaultGSTRate.SelectedIndex == 4) defaultGSTRate = 0.00m;

                // Update settings in database
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string updateSql = @"
                        UPDATE AppProfile 
                        SET OwnerName = @owner, ShopName = @shop, GSTIN = @gstin, Phone = @phone, Email = @email, Address = @addr, 
                            LogoPath = @logo, ProfilePicPath = @profPic, ThemePreset = @preset, FontSizePreset = @fontSize,
                            BackupFolderPath = @backupFolder, GoogleDriveAddress = @gDriveAddr,
                            StateName = @stName, StateCode = @stCode, IsTaxInclusive = @isTaxInc,
                            DefaultBillType = @defBill, DefaultGSTRate = @defGst,
                            UPIId = @upiId, UPIName = @upiName, AutoShowQROnUPI = @autoQR, PrintQROnReceipt = @printQR";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@owner", owner);
                        cmd.Parameters.AddWithValue("@shop", shop);
                        cmd.Parameters.AddWithValue("@gstin", string.IsNullOrEmpty(gstin) ? DBNull.Value : (object)gstin);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@addr", addr);
                        cmd.Parameters.AddWithValue("@logo", string.IsNullOrEmpty(finalLogoPath) ? DBNull.Value : (object)finalLogoPath);
                        cmd.Parameters.AddWithValue("@profPic", string.IsNullOrEmpty(finalProfilePicPath) ? DBNull.Value : (object)finalProfilePicPath);
                        cmd.Parameters.AddWithValue("@preset", themePreset);
                        cmd.Parameters.AddWithValue("@fontSize", fontSizePreset);
                        cmd.Parameters.AddWithValue("@backupFolder", backupFolder);
                        cmd.Parameters.AddWithValue("@gDriveAddr", string.IsNullOrEmpty(gDriveAddr) ? DBNull.Value : (object)gDriveAddr);
                        cmd.Parameters.AddWithValue("@stName", selState.Name);
                        cmd.Parameters.AddWithValue("@stCode", selState.Code);
                        cmd.Parameters.AddWithValue("@isTaxInc", isTaxInclusive);
                        cmd.Parameters.AddWithValue("@defBill", defaultBillType);
                        cmd.Parameters.AddWithValue("@defGst", defaultGSTRate);
                        cmd.Parameters.AddWithValue("@upiId", string.IsNullOrEmpty(upiId) ? DBNull.Value : (object)upiId);
                        cmd.Parameters.AddWithValue("@upiName", string.IsNullOrEmpty(upiName) ? DBNull.Value : (object)upiName);
                        cmd.Parameters.AddWithValue("@autoQR", autoShowQR);
                        cmd.Parameters.AddWithValue("@printQR", printQROnReceipt);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Apply dynamic theme & font immediately at runtime!
                Theme.ApplyThemePreset(themePreset);
                Theme.ApplyFontSizePreset(fontSizePreset);

                MessageBox.Show("Profile and application branding configurations saved successfully!", "Settings Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Notify shell MainForm to update UI colors immediately!
                OnSettingsSaved?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowseBackupPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Local Database Backup Directory";
                if (Directory.Exists(txtBackupFolder.Text))
                {
                    fbd.SelectedPath = txtBackupFolder.Text;
                }
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private string customThemeString = "";
        private bool isHandlingThemeChange = false;

        private void ComboThemePreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isHandlingThemeChange) return;

            string selected = comboThemePreset.SelectedItem?.ToString();
            if (selected == "Custom Color Picker...")
            {
                using (CustomThemeDialog dlg = new CustomThemeDialog(customThemeString))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        customThemeString = dlg.CustomThemeString;
                        
                        isHandlingThemeChange = true;
                        if (!comboThemePreset.Items.Contains("Custom Theme"))
                        {
                            comboThemePreset.Items.Add("Custom Theme");
                        }
                        comboThemePreset.SelectedItem = "Custom Theme";
                        isHandlingThemeChange = false;
                    }
                    else
                    {
                        isHandlingThemeChange = true;
                        LoadProfileThemeSelection();
                        isHandlingThemeChange = false;
                    }
                }
            }
        }

        private void LoadProfileThemeSelection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ThemePreset FROM AppProfile", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            string preset = result.ToString();
                            if (preset.StartsWith("CUSTOM|"))
                            {
                                customThemeString = preset;
                                if (!comboThemePreset.Items.Contains("Custom Theme"))
                                {
                                    comboThemePreset.Items.Add("Custom Theme");
                                }
                                comboThemePreset.SelectedItem = "Custom Theme";
                            }
                            else
                            {
                                int idx = comboThemePreset.Items.IndexOf(preset);
                                comboThemePreset.SelectedIndex = idx >= 0 ? idx : 0;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Nested Custom Theme Builder Dialog
        private class CustomThemeDialog : Form
        {
            public string CustomThemeString { get; private set; } = "";

            private CheckBox chkIsLight;
            private Panel pnlPrimary;
            private Panel pnlSecondary;
            private Panel pnlAccent;

            private Panel previewCard;
            private Panel previewHeader;
            private Label previewHeaderLabel;
            private Label previewLabel;
            private Label previewMuted;
            private Button previewButton;

            public CustomThemeDialog(string currentCustomTheme)
            {
                InitializeComponent();
                LoadCurrentCustomTheme(currentCustomTheme);
            }

            private void InitializeComponent()
            {
                this.Text = "Premium Theme Designer";
                this.ClientSize = new Size(600, 480); // Expanded width to support Large Fonts side-by-side!
                this.AutoScaleMode = AutoScaleMode.Dpi;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = "Custom Theme Designer";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                chkIsLight = new CheckBox();
                chkIsLight.Text = "Is this a Light/White Theme? (Uses dark text)";
                chkIsLight.Location = new Point(25, 60);
                chkIsLight.Size = new Size(550, 25); // Expanded width
                chkIsLight.ForeColor = Theme.TextLight;
                chkIsLight.Font = Theme.BoldFont;
                chkIsLight.CheckedChanged += (s, e) => UpdatePreview();
                this.Controls.Add(chkIsLight);

                int startY = 100;
                int gapY = 75; // Increased vertical gap to prevent any label collisions!

                // Primary Color Selection
                Label lblPrimary = new Label();
                lblPrimary.Text = "Primary Color (Sidebar/Header)";
                lblPrimary.Location = new Point(25, startY);
                lblPrimary.AutoSize = true;
                Theme.StyleLabel(lblPrimary, Theme.TextDark, Theme.MainFont);
                this.Controls.Add(lblPrimary);

                pnlPrimary = new Panel();
                pnlPrimary.Size = new Size(30, 30);
                pnlPrimary.Location = new Point(25, startY + 24);
                pnlPrimary.BorderStyle = BorderStyle.FixedSingle;
                pnlPrimary.BackColor = Theme.Primary;
                this.Controls.Add(pnlPrimary);

                Button btnPrimary = new Button();
                btnPrimary.Text = "Select...";
                btnPrimary.Size = new Size(110, 38); // Taller button (38px) to prevent vertical text clipping under large fonts!
                btnPrimary.Location = new Point(65, startY + 20); // Perfectly centered vertically with panel
                Theme.StyleSecondaryButton(btnPrimary);
                btnPrimary.Click += (s, e) => ChooseColor(pnlPrimary);
                this.Controls.Add(btnPrimary);

                // Secondary Color Selection
                Label lblSecondary = new Label();
                lblSecondary.Text = "Secondary Color (Cards/Body)";
                lblSecondary.Location = new Point(25, startY + gapY);
                lblSecondary.AutoSize = true;
                Theme.StyleLabel(lblSecondary, Theme.TextDark, Theme.MainFont);
                this.Controls.Add(lblSecondary);

                pnlSecondary = new Panel();
                pnlSecondary.Size = new Size(30, 30);
                pnlSecondary.Location = new Point(25, startY + gapY + 24);
                pnlSecondary.BorderStyle = BorderStyle.FixedSingle;
                pnlSecondary.BackColor = Theme.Secondary;
                this.Controls.Add(pnlSecondary);

                Button btnSecondary = new Button();
                btnSecondary.Text = "Select...";
                btnSecondary.Size = new Size(110, 38); // Taller button (38px)
                btnSecondary.Location = new Point(65, startY + gapY + 20);
                Theme.StyleSecondaryButton(btnSecondary);
                btnSecondary.Click += (s, e) => ChooseColor(pnlSecondary);
                this.Controls.Add(btnSecondary);

                // Accent Color Selection
                Label lblAccent = new Label();
                lblAccent.Text = "Accent Color (Buttons/Focus)";
                lblAccent.Location = new Point(25, startY + gapY * 2);
                lblAccent.AutoSize = true;
                Theme.StyleLabel(lblAccent, Theme.TextDark, Theme.MainFont);
                this.Controls.Add(lblAccent);

                pnlAccent = new Panel();
                pnlAccent.Size = new Size(30, 30);
                pnlAccent.Location = new Point(25, startY + gapY * 2 + 24);
                pnlAccent.BorderStyle = BorderStyle.FixedSingle;
                pnlAccent.BackColor = Theme.Accent;
                this.Controls.Add(pnlAccent);

                Button btnAccent = new Button();
                btnAccent.Text = "Select...";
                btnAccent.Size = new Size(110, 38); // Taller button (38px)
                btnAccent.Location = new Point(65, startY + gapY * 2 + 20);
                Theme.StyleSecondaryButton(btnAccent);
                btnAccent.Click += (s, e) => ChooseColor(pnlAccent);
                this.Controls.Add(btnAccent);

                // ==========================================
                // LIVE PREVIEW GROUP (Shifted Right)
                // ==========================================
                previewCard = new Panel();
                previewCard.Size = new Size(250, 220); // Expanded width slightly
                previewCard.Location = new Point(325, 105); // Shifted right to X = 325
                previewCard.BorderStyle = BorderStyle.FixedSingle;
                previewCard.BackColor = Theme.Secondary;
                this.Controls.Add(previewCard);

                previewHeader = new Panel();
                previewHeader.Size = new Size(250, 45);
                previewHeader.Location = new Point(0, 0);
                previewHeader.BackColor = Theme.Primary;
                previewCard.Controls.Add(previewHeader);

                previewHeaderLabel = new Label();
                previewHeaderLabel.Text = "Live Preview Panel";
                previewHeaderLabel.Location = new Point(10, 12);
                previewHeaderLabel.AutoSize = true;
                Theme.StyleLabel(previewHeaderLabel, Theme.TextLight, Theme.SubHeaderFont);
                previewHeader.Controls.Add(previewHeaderLabel);

                previewLabel = new Label();
                previewLabel.Text = "Sample Primary text";
                previewLabel.Location = new Point(15, 60);
                previewLabel.AutoSize = true;
                Theme.StyleLabel(previewLabel, Theme.TextLight, Theme.BoldFont);
                previewCard.Controls.Add(previewLabel);

                previewMuted = new Label();
                previewMuted.Text = "Sample secondary muted label";
                previewMuted.Location = new Point(15, 85);
                previewMuted.AutoSize = true;
                Theme.StyleLabel(previewMuted, Theme.TextDark, Theme.MainFont);
                previewCard.Controls.Add(previewMuted);

                previewButton = new Button();
                previewButton.Text = "Primary Action Button";
                previewButton.Size = new Size(220, 38); // Expanded width
                previewButton.Location = new Point(15, 125);
                Theme.StyleSuccessButton(previewButton);
                previewCard.Controls.Add(previewButton);

                // Bottom Action Buttons
                Button btnSave = new Button();
                btnSave.Text = "Apply Theme Design";
                btnSave.Size = new Size(260, 45); // Taller and wider
                btnSave.Location = new Point(25, 390);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(260, 45); // Taller and wider
                btnCancel.Location = new Point(315, 390);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                UpdatePreview();
            }

            private void ChooseColor(Panel previewPanel)
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = previewPanel.BackColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        previewPanel.BackColor = cd.Color;
                        UpdatePreview();
                    }
                }
            }

            private void UpdatePreview()
            {
                Color primary = pnlPrimary.BackColor;
                Color secondary = pnlSecondary.BackColor;
                Color accent = pnlAccent.BackColor;
                bool isLight = chkIsLight.Checked;

                previewCard.BackColor = secondary;
                previewHeader.BackColor = primary;

                Color textLight = isLight ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
                Color textDark = isLight ? Color.FromArgb(71, 85, 105) : Color.FromArgb(148, 163, 184);

                previewHeaderLabel.ForeColor = textLight;
                previewLabel.ForeColor = textLight;
                previewMuted.ForeColor = textDark;

                previewButton.BackColor = accent;
                previewButton.ForeColor = Color.FromArgb(248, 250, 252);
            }

            private void LoadCurrentCustomTheme(string themeString)
            {
                if (string.IsNullOrEmpty(themeString) || !themeString.StartsWith("CUSTOM|"))
                {
                    return;
                }

                try
                {
                    string[] parts = themeString.Split('|');
                    pnlPrimary.BackColor = ColorTranslator.FromHtml(parts[1]);
                    pnlSecondary.BackColor = ColorTranslator.FromHtml(parts[2]);
                    pnlAccent.BackColor = ColorTranslator.FromHtml(parts[3]);
                    chkIsLight.Checked = parts[4] == "1";
                    UpdatePreview();
                }
                catch { }
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string primHex = ColorTranslator.ToHtml(pnlPrimary.BackColor);
                string secHex = ColorTranslator.ToHtml(pnlSecondary.BackColor);
                string accHex = ColorTranslator.ToHtml(pnlAccent.BackColor);
                string isLight = chkIsLight.Checked ? "1" : "0";

                this.CustomThemeString = $"CUSTOM|{primHex}|{secHex}|{accHex}|{isLight}";
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
