using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MeroDokan
{
    /* ================= WINDOWS 7 SP1 COMPATIBILITY HELPER =================
       .NET Framework 4.8 (Windows 7) WinForms has no TextBox.PlaceholderText
       property - it was added in .NET Core 3.0 / .NET 5+. This helper provides
       the same visual behaviour on Windows 7 using the native Win32
       EM_SETCUEBANNER message.

       When reverting to .NET 8: delete this whole file and restore the two
       original "PlaceholderText" lines marked with WINDOWS 7 COMPATIBILITY
       CHANGE comments in SalesBillingControl.cs and ReportControl.cs.
       ====================================================================== */
    internal static class Win7Compat
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void SetPlaceholder(TextBox box, string text)
        {
            if (box == null || box.IsDisposed || string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                if (!box.IsHandleCreated)
                {
                    box.HandleCreated += (s, e) => SendMessage(box.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
                    // Forcing handle creation here is safe for these simple search/scan boxes
                    var h = box.Handle;
                }
                else
                {
                    // wParam = 1 -> show cue banner even while the box has focus
                    SendMessage(box.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
                }
            }
            catch { }
        }
    }
}
