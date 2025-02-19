using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessor
{
    public class RegionSelectorForm : Form
    {
        public Rectangle SelectedRegion { get; private set; }
        private Point startPoint;
        private Point endPoint;
        private bool isSelecting;
        private Rectangle allScreensBounds;

        public RegionSelectorForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;

            // Get the full bounding box that covers all monitors
            allScreensBounds = GetTrueMultiMonitorBounds();

            // Position and size the form to cover all monitors
            this.Location = new Point(allScreensBounds.Left, allScreensBounds.Top);
            this.Size = new Size(allScreensBounds.Width, allScreensBounds.Height);


            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.Paint += OnPaint;
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            isSelecting = true;
            startPoint = ConvertToGlobalScreenCoordinates(e.Location);
            endPoint = startPoint;
            Invalidate();
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = ConvertToGlobalScreenCoordinates(e.Location);
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            isSelecting = false;
            endPoint = ConvertToGlobalScreenCoordinates(e.Location);

            SelectedRegion = GetRectangle(startPoint, endPoint);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            using (SolidBrush overlayBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                e.Graphics.FillRectangle(overlayBrush, this.ClientRectangle); // Gray overlay
            }

            if (isSelecting)
            {
                Rectangle rect = GetRectangle(startPoint, endPoint);
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, Color.Red)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
        }

        private Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
        }

        private static Rectangle GetTrueMultiMonitorBounds()
        {
            string debugInfo = "Detected Screens:\n";
            
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                debugInfo += $"Monitor: {screen.DeviceName}, Bounds: {screen.Bounds}\n";
                minX = Math.Min(minX, screen.Bounds.Left);
                minY = Math.Min(minY, screen.Bounds.Top);
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            Rectangle fullBounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            debugInfo += $"Computed Multi-Monitor Bounds: {fullBounds}\n";
            
            System.Windows.Forms.MessageBox.Show(debugInfo, "Monitor Debug Info", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            
            return fullBounds;
        }



        private Point ConvertToGlobalScreenCoordinates(Point localPoint)
        {
            return new Point(localPoint.X + this.Left, localPoint.Y + this.Top);
        }

        public Bitmap CaptureRegion()
        {
            if (SelectedRegion == Rectangle.Empty)
            {
                throw new InvalidOperationException("No region selected.");
            }

            bool isValid = Screen.AllScreens.Any(screen => screen.Bounds.IntersectsWith(SelectedRegion));
            if (!isValid)
            {
                throw new InvalidOperationException("Selected region is not fully within any screen.");
            }

            Bitmap bitmap = new Bitmap(SelectedRegion.Width, SelectedRegion.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(SelectedRegion.Location, Point.Empty, SelectedRegion.Size);
            }
            return bitmap;
        }


        public async Task<string> SaveCapturedRegionAsync(string fileName)
        {
            string imgFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImgAndData");

            if (!Directory.Exists(imgFolderPath))
            {
                Directory.CreateDirectory(imgFolderPath);
            }

            string filePath = Path.Combine(imgFolderPath, fileName);

            using (var bitmap = CaptureRegion())
            {
                await Task.Run(() => bitmap.Save(filePath, ImageFormat.Png));
            }

            return filePath;
        }
    }
}
