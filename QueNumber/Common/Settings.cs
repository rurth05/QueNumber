using System.Drawing;
using FontStyle = System.Windows.FontStyle;

namespace QueNumber.Common
{
    class Settings
    {
        public static string BackImg = "BackPrint.png";
        public static Font HeaderSettings = new Font("Arial", 10f, System.Drawing.FontStyle.Regular);
        public static Point HeaderLocation = new Point(180, 70);
        public static Font QueNumberSettings = new Font("Arial", 24f, System.Drawing.FontStyle.Bold);
        public static Point QueNumLocation = new Point(180, 140);
        public static Font FooterSettings = new Font("Arial", 9f, System.Drawing.FontStyle.Regular);
        public static Point FooterLocation = new Point(180, 260);
        public static string PrinterName = "Bullzip PDF Printer";
    }
}
