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
        
        // Vibrant Accents
        public static Color Accent { get; set; } = Color.FromArgb(255, 107, 0);            // #ff6b00 Vibrant Electric Orange
        public static Color AccentHover { get; set; } = Color.FromArgb(234, 88, 12);       // #ea580c Deep Orange
        public static Color AccentLight { get; set; } = Color.FromArgb(45, 30, 20);        // Dark amber/orange tint
        
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

        public static void ApplyThemePreset(string name)
        {
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
                    if (lbl.Font != null)
                    {
                        if (lbl.Font.Size >= 13F)
                            lbl.Font = HeaderFont;
                        else if (lbl.Font.Size >= 10.5F)
                            lbl.Font = SubHeaderFont;
                        else if (lbl.Font.Bold)
                            lbl.Font = BoldFont;
                        else
                            lbl.Font = MainFont;
                    }
                }
                else if (container is TextBox txt)
                {
                    txt.Font = MainFont;
                    txt.BackColor = InputBg;
                    txt.ForeColor = TextLight;
                }
                else if (container is NumericUpDown num)
                {
                    num.Font = MainFont;
                    num.BackColor = InputBg;
                    num.ForeColor = TextLight;
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
                    cb.BackColor = InputBg;
                    cb.ForeColor = TextLight;
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
