using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public RegionSelectorForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.Paint += OnPaint;
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            isSelecting = true;
            startPoint = e.Location;
            endPoint = e.Location;
            Invalidate();
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            isSelecting = false;
            SelectedRegion = GetRectangle(startPoint, endPoint);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            if (isSelecting)
            {
                Rectangle rect = GetRectangle(startPoint, endPoint);
                e.Graphics.DrawRectangle(Pens.Red, rect);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Red)), rect);
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

        public async Task<string> SaveCapturedRegionAsync(string fileName)
        {
            string imgFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Img");

            // Create the folder if it doesn't exist
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
