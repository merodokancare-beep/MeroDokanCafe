using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MeroDokan
{
    public static class Theme
    {
        // Dark Obsidian & Electric Orange Luxury Theme (Matches user screenshot)
        public static Color SidebarBg { get; set; } = Color.FromArgb(9, 13, 22);            // #090d16 Deepest obsidian sidebar
        public static Color SidebarHover { get; set; } = Color.FromArgb(24, 32, 50);        // #182032
        public static Color Primary { get; set; } = Color.FromArgb(14, 19, 31);             // #0e131f Base
        public static Color Secondary { get; set; } = Color.FromArgb(11, 15, 25);           // #0b0f19 Canvas dark background
        public static Color CardBg { get; set; } = Color.FromArgb(22, 28, 45);              // #161c2d Elevated card surface
        public static Color CardBorder { get; set; } = Color.FromArgb(35, 45, 66);          // #232d42 1px card borders
        public static Color AlternateRow { get; set; } = Color.FromArgb(19, 25, 40);        // #131928 Alternate grid row
        public static Color InputBg { get; set; } = Color.FromArgb(16, 21, 35);             // #101523 Dark input boxes
        public static Color InputBorder { get; set; } = Color.FromArgb(46, 58, 82);         // #2e3a52
        
        // Vibrant Accents (Emerald Mint Default)
        public static Color Accent { get; set; } = Color.FromArgb(16, 185, 129);            // #10b981 Vibrant Emerald Mint Green
        public static Color AccentHover { get; set; } = Color.FromArgb(5, 150, 105);       // #059669 Darker Emerald
        public static Color AccentLight { get; set; } = Color.FromArgb(6, 78, 59);         // Deep Emerald tint
        
        // Text - High contrast white and slate
        public static Color TextLight { get; set; } = Color.FromArgb(255, 255, 255);       // #ffffff Pure White headers & titles
        public static Color TextWhite { get; set; } = Color.FromArgb(255, 255, 255);       // #ffffff
        public static Color TextDark { get; set; } = Color.FromArgb(241, 245, 249);        // #f1f5f9 Crisp Light text
        public static Color TextMuted { get; set; } = Color.FromArgb(148, 163, 184);       // #94a3b8 Slate 400 subtext
        public static Color TextSidebar { get; set; } = Color.FromArgb(148, 163, 184);     // #94a3b8 Slate 400
        
        // Status & Badge Accents
        public static Color Success { get; set; } = Color.FromArgb(16, 185, 129);          // #10b981 Emerald Green (Add Customer / Paid)
        public static Color Warning { get; set; } = Color.FromArgb(245, 158, 11);          // #f59e0b Amber / Gold (Active orders)
        public static Color Danger { get; set; } = Color.FromArgb(239, 68, 68);            // #ef4444 Rose Red (Low stock / Delete)
        public static Color Info { get; set; } = Color.FromArgb(56, 189, 248);             // #38bdf8 Sky Blue (Cash received)
        public static Color UPIColor { get; set; } = Color.FromArgb(139, 92, 246);         // #8b5cf6 Violet / Purple (Orders)
        public static Color WalletColor { get; set; } = Color.FromArgb(236, 72, 153);       // #ec4899 Pink
        
        private static Icon _appIcon = null;
        public static Icon AppIcon
        {
            get
            {
                if (_appIcon != null) return _appIcon;
                try
                {
                    string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        _appIcon = new Icon(iconPath);
                        return _appIcon;
                    }
                    _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
                    return _appIcon;
                }
                catch
                {
                    return SystemIcons.Application;
                }
            }
        }

        public static Color AdjustBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, 
                Math.Min(255, Math.Max(0, (int)red)), 
                Math.Min(255, Math.Max(0, (int)green)), 
                Math.Min(255, Math.Max(0, (int)blue)));
        }

        public static string CurrentThemeName { get; set; } = "Emerald Mint";
        public static bool IsDarkTheme { get; set; } = true;

        public static void ApplyThemePreset(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "Emerald Mint";
            CurrentThemeName = name;

            // Handle Custom Theme String (format: CUSTOM|#prim|#sec|#acc|isLight)
            if (name.StartsWith("CUSTOM|"))
            {
                try
                {
                    string[] parts = name.Split('|');
                    Color prim = ColorTranslator.FromHtml(parts[1]);
                    Color sec = ColorTranslator.FromHtml(parts[2]);
                    Color acc = ColorTranslator.FromHtml(parts[3]);
                    bool isLight = parts.Length > 4 && parts[4] == "1";

                    IsDarkTheme = !isLight;
                    Primary = prim;
                    Secondary = sec;
                    Accent = acc;
                    AccentHover = AdjustBrightness(acc, isLight ? -0.15f : 0.15f);
                    AccentLight = AdjustBrightness(acc, isLight ? 0.65f : -0.65f);

                    if (isLight)
                    {
                        SidebarBg = AdjustBrightness(prim, -0.2f);
                        SidebarHover = AdjustBrightness(prim, -0.1f);
                        CardBg = Color.White;
                        CardBorder = AdjustBrightness(sec, -0.14f);
                        AlternateRow = AdjustBrightness(sec, 0.05f);
                        InputBg = Color.White;
                        InputBorder = AdjustBrightness(sec, -0.22f);
                        TextLight = Color.FromArgb(15, 23, 42);
                        TextWhite = Color.White;
                        TextDark = Color.FromArgb(51, 65, 85);
                        TextMuted = Color.FromArgb(100, 116, 139);
                        TextSidebar = Color.FromArgb(226, 232, 240);
                    }
                    else
                    {
                        SidebarBg = AdjustBrightness(prim, -0.25f);
                        SidebarHover = AdjustBrightness(prim, 0.15f);
                        CardBg = AdjustBrightness(sec, 0.15f);
                        CardBorder = AdjustBrightness(sec, 0.35f);
                        AlternateRow = AdjustBrightness(sec, 0.08f);
                        InputBg = AdjustBrightness(sec, 0.08f);
                        InputBorder = AdjustBrightness(sec, 0.45f);
                        TextLight = Color.White;
                        TextWhite = Color.White;
                        TextDark = Color.FromArgb(241, 245, 249);
                        TextMuted = Color.FromArgb(148, 163, 184);
                        TextSidebar = Color.FromArgb(148, 163, 184);
                    }
                    return;
                }
                catch { }
            }

            switch (name.Trim())
            {
                case "Rose Gold":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(18, 15, 20);
                    SidebarHover = Color.FromArgb(38, 31, 43);
                    Primary = Color.FromArgb(26, 20, 28);
                    Secondary = Color.FromArgb(16, 13, 19);
                    CardBg = Color.FromArgb(33, 26, 38);
                    CardBorder = Color.FromArgb(64, 50, 72);
                    AlternateRow = Color.FromArgb(26, 21, 32);
                    InputBg = Color.FromArgb(23, 18, 27);
                    InputBorder = Color.FromArgb(75, 58, 84);
                    Accent = Color.FromArgb(230, 140, 130);
                    AccentHover = Color.FromArgb(215, 120, 110);
                    AccentLight = Color.FromArgb(55, 32, 38);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(250, 238, 240);
                    TextMuted = Color.FromArgb(180, 155, 165);
                    TextSidebar = Color.FromArgb(180, 155, 165);
                    break;

                case "Emerald Mint":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(6, 21, 16);
                    SidebarHover = Color.FromArgb(13, 43, 32);
                    Primary = Color.FromArgb(10, 30, 23);
                    Secondary = Color.FromArgb(5, 18, 13);
                    CardBg = Color.FromArgb(15, 42, 33);
                    CardBorder = Color.FromArgb(27, 69, 54);
                    AlternateRow = Color.FromArgb(11, 34, 26);
                    InputBg = Color.FromArgb(11, 33, 25);
                    InputBorder = Color.FromArgb(34, 87, 68);
                    Accent = Color.FromArgb(16, 185, 129);
                    AccentHover = Color.FromArgb(5, 150, 105);
                    AccentLight = Color.FromArgb(12, 56, 41);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(230, 251, 243);
                    TextMuted = Color.FromArgb(131, 184, 164);
                    TextSidebar = Color.FromArgb(131, 184, 164);
                    break;

                case "Deep Olive":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(17, 20, 12);
                    SidebarHover = Color.FromArgb(34, 40, 24);
                    Primary = Color.FromArgb(24, 29, 17);
                    Secondary = Color.FromArgb(14, 18, 10);
                    CardBg = Color.FromArgb(32, 39, 23);
                    CardBorder = Color.FromArgb(56, 68, 41);
                    AlternateRow = Color.FromArgb(24, 30, 18);
                    InputBg = Color.FromArgb(23, 30, 17);
                    InputBorder = Color.FromArgb(70, 85, 52);
                    Accent = Color.FromArgb(234, 179, 8);
                    AccentHover = Color.FromArgb(202, 138, 4);
                    AccentLight = Color.FromArgb(51, 43, 16);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(247, 254, 231);
                    TextMuted = Color.FromArgb(163, 177, 138);
                    TextSidebar = Color.FromArgb(163, 177, 138);
                    break;

                case "Cyberpunk Purple":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(15, 9, 27);
                    SidebarHover = Color.FromArgb(35, 22, 62);
                    Primary = Color.FromArgb(24, 14, 42);
                    Secondary = Color.FromArgb(12, 7, 23);
                    CardBg = Color.FromArgb(34, 21, 58);
                    CardBorder = Color.FromArgb(65, 41, 110);
                    AlternateRow = Color.FromArgb(26, 16, 46);
                    InputBg = Color.FromArgb(25, 16, 43);
                    InputBorder = Color.FromArgb(85, 54, 145);
                    Accent = Color.FromArgb(217, 70, 239);
                    AccentHover = Color.FromArgb(192, 38, 211);
                    AccentLight = Color.FromArgb(61, 20, 69);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(253, 244, 255);
                    TextMuted = Color.FromArgb(181, 158, 208);
                    TextSidebar = Color.FromArgb(181, 158, 208);
                    break;

                case "Midnight Blue":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(8, 14, 28);
                    SidebarHover = Color.FromArgb(19, 33, 62);
                    Primary = Color.FromArgb(13, 22, 45);
                    Secondary = Color.FromArgb(6, 10, 22);
                    CardBg = Color.FromArgb(20, 33, 66);
                    CardBorder = Color.FromArgb(38, 60, 118);
                    AlternateRow = Color.FromArgb(15, 26, 52);
                    InputBg = Color.FromArgb(14, 24, 49);
                    InputBorder = Color.FromArgb(48, 77, 148);
                    Accent = Color.FromArgb(56, 189, 248);
                    AccentHover = Color.FromArgb(14, 165, 233);
                    AccentLight = Color.FromArgb(14, 48, 79);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(240, 249, 255);
                    TextMuted = Color.FromArgb(140, 160, 186);
                    TextSidebar = Color.FromArgb(140, 160, 186);
                    break;

                case "Sunset Crimson":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(22, 8, 10);
                    SidebarHover = Color.FromArgb(48, 19, 24);
                    Primary = Color.FromArgb(32, 13, 16);
                    Secondary = Color.FromArgb(17, 5, 7);
                    CardBg = Color.FromArgb(45, 18, 22);
                    CardBorder = Color.FromArgb(82, 33, 41);
                    AlternateRow = Color.FromArgb(34, 14, 17);
                    InputBg = Color.FromArgb(33, 14, 17);
                    InputBorder = Color.FromArgb(105, 42, 53);
                    Accent = Color.FromArgb(244, 63, 94);
                    AccentHover = Color.FromArgb(225, 29, 72);
                    AccentLight = Color.FromArgb(71, 20, 30);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(255, 241, 242);
                    TextMuted = Color.FromArgb(191, 161, 165);
                    TextSidebar = Color.FromArgb(191, 161, 165);
                    break;

                case "Ocean Breeze":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(6, 19, 24);
                    SidebarHover = Color.FromArgb(14, 39, 50);
                    Primary = Color.FromArgb(11, 31, 39);
                    Secondary = Color.FromArgb(5, 15, 20);
                    CardBg = Color.FromArgb(17, 43, 54);
                    CardBorder = Color.FromArgb(30, 75, 94);
                    AlternateRow = Color.FromArgb(11, 31, 39);
                    InputBg = Color.FromArgb(12, 33, 42);
                    InputBorder = Color.FromArgb(40, 98, 122);
                    Accent = Color.FromArgb(6, 182, 212);
                    AccentHover = Color.FromArgb(8, 145, 178);
                    AccentLight = Color.FromArgb(11, 58, 68);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(236, 254, 255);
                    TextMuted = Color.FromArgb(125, 164, 179);
                    TextSidebar = Color.FromArgb(125, 164, 179);
                    break;

                case "Forest Moss":
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(10, 20, 9);
                    SidebarHover = Color.FromArgb(22, 43, 20);
                    Primary = Color.FromArgb(16, 32, 15);
                    Secondary = Color.FromArgb(7, 15, 6);
                    CardBg = Color.FromArgb(24, 46, 22);
                    CardBorder = Color.FromArgb(42, 80, 39);
                    AlternateRow = Color.FromArgb(17, 35, 16);
                    InputBg = Color.FromArgb(17, 34, 16);
                    InputBorder = Color.FromArgb(56, 107, 51);
                    Accent = Color.FromArgb(132, 204, 22);
                    AccentHover = Color.FromArgb(101, 163, 13);
                    AccentLight = Color.FromArgb(37, 61, 20);
                    TextLight = Color.White;
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(247, 254, 231);
                    TextMuted = Color.FromArgb(147, 175, 138);
                    TextSidebar = Color.FromArgb(147, 175, 138);
                    break;

                case "Pure Alabaster":
                    IsDarkTheme = false;
                    SidebarBg = Color.FromArgb(30, 41, 59);
                    SidebarHover = Color.FromArgb(51, 65, 85);
                    Primary = Color.FromArgb(248, 250, 252);
                    Secondary = Color.FromArgb(241, 245, 249);
                    CardBg = Color.White;
                    CardBorder = Color.FromArgb(226, 232, 240);
                    AlternateRow = Color.FromArgb(248, 250, 252);
                    InputBg = Color.White;
                    InputBorder = Color.FromArgb(203, 213, 225);
                    Accent = Color.FromArgb(37, 99, 235);
                    AccentHover = Color.FromArgb(29, 78, 216);
                    AccentLight = Color.FromArgb(219, 234, 254);
                    TextLight = Color.FromArgb(15, 23, 42);
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(51, 65, 85);
                    TextMuted = Color.FromArgb(100, 116, 139);
                    TextSidebar = Color.FromArgb(226, 232, 240);
                    break;

                case "Snowy Mint":
                    IsDarkTheme = false;
                    SidebarBg = Color.FromArgb(19, 42, 34);
                    SidebarHover = Color.FromArgb(30, 63, 52);
                    Primary = Color.FromArgb(244, 250, 247);
                    Secondary = Color.FromArgb(234, 244, 239);
                    CardBg = Color.White;
                    CardBorder = Color.FromArgb(209, 231, 221);
                    AlternateRow = Color.FromArgb(244, 250, 247);
                    InputBg = Color.White;
                    InputBorder = Color.FromArgb(186, 219, 204);
                    Accent = Color.FromArgb(5, 150, 105);
                    AccentHover = Color.FromArgb(4, 120, 87);
                    AccentLight = Color.FromArgb(209, 250, 229);
                    TextLight = Color.FromArgb(6, 78, 59);
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(15, 81, 50);
                    TextMuted = Color.FromArgb(88, 129, 87);
                    TextSidebar = Color.FromArgb(232, 245, 233);
                    break;

                case "Nordic Light":
                    IsDarkTheme = false;
                    SidebarBg = Color.FromArgb(24, 34, 47);
                    SidebarHover = Color.FromArgb(38, 53, 72);
                    Primary = Color.FromArgb(245, 247, 250);
                    Secondary = Color.FromArgb(235, 240, 245);
                    CardBg = Color.White;
                    CardBorder = Color.FromArgb(213, 223, 233);
                    AlternateRow = Color.FromArgb(245, 247, 250);
                    InputBg = Color.White;
                    InputBorder = Color.FromArgb(186, 202, 214);
                    Accent = Color.FromArgb(2, 132, 199);
                    AccentHover = Color.FromArgb(3, 105, 161);
                    AccentLight = Color.FromArgb(224, 242, 254);
                    TextLight = Color.FromArgb(12, 26, 41);
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(30, 41, 59);
                    TextMuted = Color.FromArgb(100, 116, 139);
                    TextSidebar = Color.FromArgb(224, 242, 254);
                    break;

                case "Soft Peach":
                    IsDarkTheme = false;
                    SidebarBg = Color.FromArgb(42, 26, 23);
                    SidebarHover = Color.FromArgb(61, 38, 34);
                    Primary = Color.FromArgb(255, 247, 242);
                    Secondary = Color.FromArgb(253, 240, 233);
                    CardBg = Color.White;
                    CardBorder = Color.FromArgb(245, 221, 209);
                    AlternateRow = Color.FromArgb(255, 247, 242);
                    InputBg = Color.White;
                    InputBorder = Color.FromArgb(230, 194, 178);
                    Accent = Color.FromArgb(224, 86, 56);
                    AccentHover = Color.FromArgb(200, 67, 38);
                    AccentLight = Color.FromArgb(255, 229, 220);
                    TextLight = Color.FromArgb(45, 24, 16);
                    TextWhite = Color.White;
                    TextDark = Color.FromArgb(67, 40, 28);
                    TextMuted = Color.FromArgb(127, 85, 57);
                    TextSidebar = Color.FromArgb(255, 232, 214);
                    break;

                case "Dark Slate":
                default:
                    IsDarkTheme = true;
                    SidebarBg = Color.FromArgb(9, 13, 22);
                    SidebarHover = Color.FromArgb(24, 32, 50);
                    Primary = Color.FromArgb(14, 19, 31);
                    Secondary = Color.FromArgb(11, 15, 25);
                    CardBg = Color.FromArgb(22, 28, 45);
                    CardBorder = Color.FromArgb(35, 45, 66);
                    AlternateRow = Color.FromArgb(19, 25, 40);
                    InputBg = Color.FromArgb(16, 21, 35);
                    InputBorder = Color.FromArgb(46, 58, 82);
                    Accent = Color.FromArgb(255, 107, 0);
                    AccentHover = Color.FromArgb(234, 88, 12);
                    AccentLight = Color.FromArgb(45, 30, 20);
                    TextLight = Color.FromArgb(255, 255, 255);
                    TextWhite = Color.FromArgb(255, 255, 255);
                    TextDark = Color.FromArgb(241, 245, 249);
                    TextMuted = Color.FromArgb(148, 163, 184);
                    TextSidebar = Color.FromArgb(148, 163, 184);
                    break;
            }
        }

        public static string FontSizePreset { get; set; } = "Medium";
        public static Font HeaderFont { get; set; } = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        public static Font SubHeaderFont { get; set; } = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        public static Font MainFont { get; set; } = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static Font BoldFont { get; set; } = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        public static Font SmallFont { get; set; } = new Font("Segoe UI", 8F, FontStyle.Regular);

        public static void ApplyFontSizePreset(string preset)
        {
            FontSizePreset = preset;
            float scale = 1.0f;
            if (preset == "Small") scale = 0.88f;
            else if (preset == "Large") scale = 1.22f;

            HeaderFont = new Font("Segoe UI", 13.5F * scale, FontStyle.Bold);
            SubHeaderFont = new Font("Segoe UI Semibold", 10.5F * scale, FontStyle.Bold);
            MainFont = new Font("Segoe UI", 9.5F * scale, FontStyle.Regular);
            BoldFont = new Font("Segoe UI Semibold", 9.5F * scale, FontStyle.Bold);
            SmallFont = new Font("Segoe UI", 8F * scale, FontStyle.Regular);
        }

        public static void UpdateFontRecursively(Control container)
        {
            if (container == null) return;

            try
            {
                if (container is Label lbl)
                {
                    if (lbl.Tag == null && lbl.Font != null)
                    {
                        if (lbl.Font.Size >= 13F) lbl.Tag = "Header";
                        else if (lbl.Font.Size >= 10.5F) lbl.Tag = "SubHeader";
                        else if (lbl.Font.Size <= 8.5F) lbl.Tag = "Small";
                        else if (lbl.Font.Bold) lbl.Tag = "Bold";
                        else lbl.Tag = "Main";
                    }

                    string role = lbl.Tag?.ToString();
                    if (role == "Header") lbl.Font = HeaderFont;
                    else if (role == "SubHeader") lbl.Font = SubHeaderFont;
                    else if (role == "Small") lbl.Font = SmallFont;
                    else if (role == "Bold") lbl.Font = BoldFont;
                    else lbl.Font = MainFont;
                }
                else if (container is TextBox txt)
                {
                    txt.Font = MainFont;
                }
                else if (container is NumericUpDown num)
                {
                    num.Font = MainFont;
                }
                else if (container is Button btn)
                {
                    if (btn.Name != "btnExit" && btn.Name != "btnCopy" && btn.Name != "btnActivate")
                    {
                        btn.Font = BoldFont;
                    }
                }
                else if (container is ComboBox cb)
                {
                    cb.Font = MainFont;
                }
                else if (container is DataGridView dgv)
                {
                    dgv.Font = MainFont;
                    dgv.ColumnHeadersDefaultCellStyle.Font = BoldFont;
                    dgv.DefaultCellStyle.Font = MainFont;
                    dgv.AlternatingRowsDefaultCellStyle.Font = MainFont;
                }
            }
            catch { }

            foreach (Control child in container.Controls)
            {
                UpdateFontRecursively(child);
            }
        }

        public static void ApplyThemeRecursively(Control container)
        {
            if (container == null) return;

            try
            {
                if (container is TextBox txt)
                {
                    txt.BackColor = InputBg;
                    txt.ForeColor = TextLight;
                }
                else if (container is NumericUpDown num)
                {
                    num.BackColor = InputBg;
                    num.ForeColor = TextLight;
                }
                else if (container is ComboBox cb)
                {
                    cb.BackColor = InputBg;
                    cb.ForeColor = TextLight;
                }
                else if (container is Label lbl)
                {
                    // Preserve status/badge highlights
                    if (lbl.ForeColor != Success && lbl.ForeColor != Danger && lbl.ForeColor != Warning && lbl.ForeColor != Info && lbl.ForeColor != Color.White)
                    {
                        string role = lbl.Tag?.ToString();
                        if (role == "Small" || (lbl.Font != null && lbl.Font.Size <= 8.5F))
                            lbl.ForeColor = TextMuted;
                        else
                            lbl.ForeColor = TextLight;
                    }
                }
                else if (container is DataGridView dgv)
                {
                    dgv.BackgroundColor = CardBg;
                    dgv.GridColor = CardBorder;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = Primary;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
                    dgv.DefaultCellStyle.BackColor = CardBg;
                    dgv.DefaultCellStyle.ForeColor = TextLight;
                    dgv.AlternatingRowsDefaultCellStyle.BackColor = AlternateRow;
                    dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextLight;
                }
            }
            catch { }

            foreach (Control child in container.Controls)
            {
                ApplyThemeRecursively(child);
            }
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void StyleButton(Button btn, Color bg, Color fg, int radius = 6)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = BoldFont;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AdjustBrightness(bg, 0.15f);
            btn.Cursor = Cursors.Hand;
            btn.Padding = new Padding(6, 2, 6, 2);
        }

        public static void StylePrimaryButton(Button btn)
        {
            StyleButton(btn, Accent, TextWhite);
        }

        public static void StyleDangerButton(Button btn)
        {
            StyleButton(btn, Danger, TextWhite);
        }

        public static void StyleSuccessButton(Button btn)
        {
            StyleButton(btn, Success, TextWhite);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            StyleButton(btn, CardBg, TextLight);
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.BackColor = InputBg;
            txt.ForeColor = TextLight;
            txt.Font = MainFont;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleNumericUpDown(NumericUpDown num)
        {
            num.BackColor = InputBg;
            num.ForeColor = TextLight;
            num.Font = MainFont;
            num.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleComboBox(ComboBox combo)
        {
            combo.BackColor = InputBg;
            combo.ForeColor = TextLight;
            combo.Font = MainFont;
            combo.FlatStyle = FlatStyle.Flat;
        }

        public static void StyleLabel(Label lbl, Color color, Font font)
        {
            lbl.ForeColor = color;
            lbl.Font = font;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = CardBg;
            grid.GridColor = CardBorder;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.RowTemplate.Height = 40;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Column Header Style
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(16, 21, 35);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 21, 35);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextLight;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 42;

            // Default Row Style
            grid.DefaultCellStyle.BackColor = CardBg;
            grid.DefaultCellStyle.ForeColor = TextLight;
            grid.DefaultCellStyle.Font = MainFont;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(29, 78, 216); // High-contrast Vibrant Blue
            grid.DefaultCellStyle.SelectionForeColor = TextWhite;

            // Alternating Row Style
            grid.AlternatingRowsDefaultCellStyle.BackColor = AlternateRow;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextLight;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(29, 78, 216); // High-contrast Vibrant Blue
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TextWhite;

            // Explicit Row Selection & Highlighting Events
            grid.CellMouseDown += (s, e) => {
                if (e.RowIndex >= 0)
                {
                    grid.ClearSelection();
                    grid.Rows[e.RowIndex].Selected = true;
                }
            };

            grid.RowPrePaint += (s, e) => {
                if (e.RowIndex >= 0 && (e.State & DataGridViewElementStates.Selected) != 0)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(29, 78, 216)))
                    {
                        e.Graphics.FillRectangle(b, e.RowBounds);
                    }
                    // Left Orange Accent Indicator Bar
                    using (SolidBrush barBrush = new SolidBrush(Accent))
                    {
                        e.Graphics.FillRectangle(barBrush, e.RowBounds.Left, e.RowBounds.Top, 5, e.RowBounds.Height);
                    }
                    e.PaintCells(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Background);
                    e.Handled = true;
                }
            };

            grid.RowPostPaint += (s, e) => {
                if (e.RowIndex >= 0 && (e.State & DataGridViewElementStates.Selected) != 0)
                {
                    using (Pen p = new Pen(Color.FromArgb(255, 107, 0), 1))
                    {
                        Rectangle rect = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, e.RowBounds.Width - 1, e.RowBounds.Height - 1);
                        e.Graphics.DrawRectangle(p, rect);
                    }
                }
            };
        }

        public static Panel CreateCard(int width, int height)
        {
            Panel card = new Panel();
            card.Size = new Size(width, height);
            card.BackColor = CardBg;
            card.Padding = new Padding(12);
            card.Paint += (s, e) => {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(CardBorder, 1))
                {
                    g.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };
            return card;
        }

        public static Panel CreateSmartKpiCard(int width, int height, string title, string initialValue, string subText, Color accentColor, out Label lblVal, out Label lblSub)
        {
            Panel card = new Panel();
            card.Size = new Size(width, height);
            card.BackColor = CardBg;
            card.Padding = new Padding(12);

            // Top Glowing Accent Strip
            Panel strip = new Panel();
            strip.Size = new Size(width, 3);
            strip.Dock = DockStyle.Top;
            strip.BackColor = accentColor;
            card.Controls.Add(strip);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Location = new Point(14, 14);
            lblTitle.AutoSize = true;
            StyleLabel(lblTitle, TextMuted, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(lblTitle);

            lblVal = new Label();
            lblVal.Text = initialValue;
            lblVal.Location = new Point(14, 34);
            lblVal.AutoSize = true;
            StyleLabel(lblVal, TextLight, new Font("Segoe UI", 16F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            lblSub = new Label();
            lblSub.Text = subText;
            lblSub.Location = new Point(14, 70);
            lblSub.AutoSize = true;
            StyleLabel(lblSub, accentColor, new Font("Segoe UI", 7.8F, FontStyle.Regular));
            card.Controls.Add(lblSub);

            card.Paint += (s, e) => {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(CardBorder, 1))
                {
                    g.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            return card;
        }

        public static void DrawPillBadge(Graphics g, string text, Rectangle rect, Color bg, Color fg)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = GetRoundedPath(rect, rect.Height / 2))
            {
                using (SolidBrush b = new SolidBrush(bg))
                {
                    g.FillPath(b, path);
                }
            }

            using (Font f = new Font("Segoe UI Semibold", 8F, FontStyle.Bold))
            using (SolidBrush bText = new SolidBrush(fg))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, f, bText, rect, sf);
            }
        }
    }
}
