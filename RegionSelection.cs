using System;
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
        private readonly Rectangle allScreensBounds;

        // Initializes the form for selecting a region on the screen
        public RegionSelectorForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;

            allScreensBounds = GetTrueMultiMonitorBounds();

            this.StartPosition = FormStartPosition.Manual;
            this.Location = allScreensBounds.Location;
            this.Size = allScreensBounds.Size;

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.Paint += OnPaint;
        }

        // Handles the start of region selection
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            isSelecting = true;
            startPoint = PointToScreen(e.Location);
            endPoint = startPoint;
            Invalidate();
        }

        // Updates the selected region while dragging
        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = PointToScreen(e.Location);
                Invalidate();
            }
        }

        // Finalizes the selection and closes the form
        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            isSelecting = false;
            endPoint = PointToScreen(e.Location);
            SelectedRegion = GetRectangle(startPoint, endPoint);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Draws the selection rectangle on the screen
        private void OnPaint(object? sender, PaintEventArgs e)
        {
            using (SolidBrush overlayBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                e.Graphics.FillRectangle(overlayBrush, this.ClientRectangle);
            }

            if (isSelecting)
            {
                Rectangle rect = GetRectangle(PointToClient(startPoint), PointToClient(endPoint));
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

        // Gets the total bounding rectangle of all screens
        private static Rectangle GetTrueMultiMonitorBounds()
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                minX = Math.Min(minX, screen.Bounds.Left);
                minY = Math.Min(minY, screen.Bounds.Top);
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        // Creates a rectangle from two corner points
        private Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p1.X - p2.X),
                Math.Abs(p1.Y - p2.Y));
        }

        // Captures the selected region of the screen
        public Bitmap CaptureRegion()
        {
            if (SelectedRegion == Rectangle.Empty)
            {
                throw new InvalidOperationException("No region selected.");
            }

            Bitmap bitmap = new Bitmap(SelectedRegion.Width, SelectedRegion.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(SelectedRegion.Location, Point.Empty, SelectedRegion.Size);
            }
            return bitmap;
        }

        // Saves the captured region to a file asynchronously
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
