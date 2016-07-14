using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using QueNumber.Common;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using System.Drawing.Text;
using PrintDialog = System.Windows.Controls.PrintDialog;
using Timer = System.Timers.Timer;

namespace QueNumber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double gap = 100.0; // pixel gap between each TextBlock
        const int timer_interval = 16; // number of ms between timer ticks. 16 is near 1/60th second, for smoother updates on LCD displays
        const double move_amount = 2.5; // number of pixels to move each timer tick. 1 - 1.5 is ideal, any higher will introduce judders

        private LinkedList<TextBlock> textBlocks = new LinkedList<TextBlock>();
        private Timer timer = new Timer();

        private System.Drawing.Image dsg;
        public MainWindow()
        {
            InitializeComponent();

            AddTextBlock("SELAMAT DATANG, SILAKAN AMBIL NOMOR ANTRIAN ANDA !");
            
            canvas1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(Object state)
            {
                var node = textBlocks.First;

                while (node != null)
                {
                    double left = 0;

                    if (node.Previous != null)
                    {
                        Canvas.SetLeft(node.Value, Canvas.GetLeft(node.Previous.Value) + node.Previous.Value.ActualWidth + gap);
                    }
                    else
                    {
                        Canvas.SetLeft(node.Value, canvas1.Width + gap);
                    }

                    node = node.Next;
                }

                return null;

            }), null);

            timer.Interval = timer_interval;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            canvas1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(Object state)
            {
                var node = textBlocks.First;
                var lastNode = textBlocks.Last;

                while (node != null)
                {
                    double newLeft = Canvas.GetLeft(node.Value) - move_amount;

                    if (newLeft < (0 - (node.Value.ActualWidth + gap)))
                    {
                        textBlocks.Remove(node);

                        var lastNodeLeftPos = Canvas.GetLeft(lastNode.Value);

                        textBlocks.AddLast(node);

                        if ((lastNodeLeftPos + lastNode.Value.ActualWidth + gap) > canvas1.Width) // Last element is offscreen
                        {
                            newLeft = lastNodeLeftPos + lastNode.Value.ActualWidth + gap;
                        }
                        else
                        {
                            newLeft = canvas1.Width + gap;
                        }
                    }

                    Canvas.SetLeft(node.Value, newLeft);

                    node = node == lastNode ? null : node.Next;
                }

                return null;

            }), null);
        }

        void AddTextBlock(string Text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = Text;
            tb.FontSize = 64;
            tb.FontFamily = new FontFamily("LED Counter 7");
            tb.FontWeight = FontWeights.Normal;
            tb.Foreground = Brushes.Red;

            canvas1.Children.Add(tb);

            Canvas.SetTop(tb, 20);
            Canvas.SetLeft(tb, -999);

            textBlocks.AddLast(tb);
        }

        private void img1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            img1.Source = new BitmapImage(new Uri("/QueNumber;component/Images/ButtonPress.png", UriKind.Relative));
            img1.Effect = null;
        }

        private void img1_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            img1.Source = new BitmapImage(new Uri("/QueNumber;component/Images/Button.png", UriKind.Relative));
            img1.Effect = new DropShadowEffect();

            int num = Convert.ToInt32(tb1.Text);
            num++;
            tb1.Text = (num).ToString().PadLeft(3, '0');

            dsg = PrintQue();
            dsg.Save("tes.bmp");

            //if (dsg != null)
            //{
            //    new PrintPreviewDialog();
            //    var dialog = new System.Windows.Forms.PrintDialog();
            //    var document = new System.Drawing.Printing.PrintDocument();
            //    var controller = new StandardPrintController();
            //    dialog.PrinterSettings.Duplex = Duplex.Vertical;
            //    dialog.PrinterSettings.PrinterName = Settings.PrinterName;
            //    document.PrintPage += new PrintPageEventHandler(doc_PrintPage);
            //    document.DefaultPageSettings.Landscape = false;
            //    document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            //    document.PrinterSettings.Copies = 1;
            //    document.PrintController = controller;
            //    document.PrinterSettings = dialog.PrinterSettings;
            //    document.Print();
            //}
        }

        private void img2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            img2.Source = new BitmapImage(new Uri("/QueNumber;component/Images/ButtonPress.png", UriKind.Relative));
            img2.Effect = null;
        }

        private void img2_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            img2.Source = new BitmapImage(new Uri("/QueNumber;component/Images/Button.png", UriKind.Relative));
            img2.Effect = new DropShadowEffect();

            int num = Convert.ToInt32(tb2.Text);
            num++;
            tb2.Text = (num).ToString().PadLeft(3, '0');
        }

        public System.Drawing.Image PrintQue()
        {
            System.Drawing.Image img;

            if (File.Exists(Settings.BackImg))
                img = System.Drawing.Image.FromFile(Settings.BackImg);
            else
                img = System.Drawing.Image.FromFile(Settings.BackImg);

            System.Drawing.Bitmap bmp1 = new Bitmap(img);
            bmp1.SetResolution(200f, 200f);

            System.Drawing.Graphics graphics = Graphics.FromImage(bmp1);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextContrast = 0;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Brush brush = new SolidBrush(Color.Black);
            graphics.DrawString("D9 SOFTWARE INC.", Settings.HeaderSettings, brush, Settings.HeaderLocation);
            graphics.DrawString(tb1.Text, Settings.QueNumberSettings, brush, Settings.QueNumLocation);
            graphics.DrawString("Budayakan Antri", Settings.FooterSettings, brush, Settings.FooterLocation);
            return bmp1;
        }

        private void doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.TextContrast = 0;
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                System.Drawing.Image original = dsg;
                var image = new System.Drawing.Bitmap(original);
                image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                e.Graphics.DrawImage(image, 0, 0, e.PageSettings.PaperSize.Width, e.PageSettings.PaperSize.Height);
                e.HasMorePages = false;
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(exception.Message);
            }
        }
    }
}
