using System;
using System.Collections.Generic;
using System.Drawing;

namespace MeroDokan
{
    public static class BarcodeHelper
    {
        private static readonly Dictionary<char, string> Code39Map = new Dictionary<char, string>
        {
            {'0', "000110100"}, {'1', "100100001"}, {'2', "001100001"},
            {'3', "101100000"}, {'4', "000110001"}, {'5', "100110000"},
            {'6', "001110000"}, {'7', "000100101"}, {'8', "100100100"},
            {'9', "001100100"}, {'A', "100001001"}, {'B', "001001001"},
            {'C', "101001000"}, {'D', "000011001"}, {'E', "100011000"},
            {'F', "001011000"}, {'G', "000001101"}, {'H', "100001100"},
            {'I', "001001100"}, {'J', "000011100"}, {'K', "100000011"},
            {'L', "001000011"}, {'M', "101000010"}, {'N', "000010011"},
            {'O', "100010010"}, {'P', "001010010"}, {'Q', "000000111"},
            {'R', "100000110"}, {'S', "001000110"}, {'T', "000010110"},
            {'U', "110000001"}, {'V', "011000001"}, {'W', "111000000"},
            {'X', "010010001"}, {'Y', "110010000"}, {'Z', "011010000"},
            {'-', "010000101"}, {'.', "110000100"}, {' ', "011000100"},
            {'$', "010101000"}, {'/', "010100010"}, {'+', "010001010"},
            {'%', "000101010"}, {'*', "010010100"}
        };

        public static void DrawCode39Barcode(Graphics g, string data, float x, float y, float height, float narrowBarWidth)
        {
            if (string.IsNullOrEmpty(data)) return;

            string formatted = "*" + data.ToUpper() + "*";
            float wideBarWidth = narrowBarWidth * 3f; // Standard 3:1 ratio
            float currentX = x;

            using (Brush brush = new SolidBrush(Color.Black))
            {
                foreach (char c in formatted)
                {
                    if (!Code39Map.ContainsKey(c)) continue;

                    string pattern = Code39Map[c];

                    // Draw the 9 elements of the pattern (alternating black bar and white space)
                    for (int i = 0; i < 9; i++)
                    {
                        bool isBar = (i % 2 == 0);
                        bool isWide = (pattern[i] == '1');
                        float width = isWide ? wideBarWidth : narrowBarWidth;

                        if (isBar)
                        {
                            g.FillRectangle(brush, currentX, y, width, height);
                        }

                        currentX += width;
                    }

                    // Inter-character gap
                    currentX += narrowBarWidth;
                }
            }
        }

        public static void DrawQRCode(Graphics g, string data, float x, float y, float size)
        {
            if (string.IsNullOrEmpty(data)) return;
            try
            {
                using (var qrGenerator = new QRCoder.QRCodeGenerator())
                {
                    using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.M))
                    {
                        using (var qrCode = new QRCoder.QRCode(qrCodeData))
                        {
                            using (var bmp = qrCode.GetGraphic(12))
                            {
                                var prevInterpolation = g.InterpolationMode;
                                var prevPixelOffset = g.PixelOffsetMode;
                                var prevSmoothing = g.SmoothingMode;

                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                                g.DrawImage(bmp, x, y, size, size);

                                g.InterpolationMode = prevInterpolation;
                                g.PixelOffsetMode = prevPixelOffset;
                                g.SmoothingMode = prevSmoothing;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                using (Font fallbackFont = new Font("Segoe UI", 7F, FontStyle.Regular))
                {
                    g.DrawString("QR Error", fallbackFont, Brushes.Red, x, y);
                }
            }
        }

        public static Bitmap GenerateQRCodeBitmap(string data, int pixelsPerModule = 10)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            try
            {
                using (var qrGenerator = new QRCoder.QRCodeGenerator())
                {
                    using (var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.M))
                    {
                        using (var qrCode = new QRCoder.QRCode(qrCodeData))
                        {
                            return qrCode.GetGraphic(pixelsPerModule, Color.Black, Color.White, true);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static string GenerateUPIString(string upiId, string payeeName, decimal amount, string note = "")
        {
            if (string.IsNullOrWhiteSpace(upiId)) return "";
            
            string encodedName = !string.IsNullOrWhiteSpace(payeeName) ? Uri.EscapeDataString(payeeName.Trim()) : "Merchant";
            string encodedNote = !string.IsNullOrWhiteSpace(note) ? Uri.EscapeDataString(note.Trim()) : "Bill Payment";
            
            // Standard UPI Deep-Link Format (Supported across GPay, PhonePe, Paytm, BHIM, Cred, Banking Apps)
            if (amount > 0)
            {
                return $"upi://pay?pa={upiId.Trim()}&pn={encodedName}&am={amount:F2}&cu=INR&tn={encodedNote}";
            }
            else
            {
                return $"upi://pay?pa={upiId.Trim()}&pn={encodedName}&cu=INR&tn={encodedNote}";
            }
        }
    }
}
