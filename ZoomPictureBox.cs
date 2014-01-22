using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PA.Picture
{
    public class ZoomPictureBox : PictureBox
    {
        public bool AllowSelection { get; set;}

        public ZoomPictureBox()
            : base()
        {
            this.zoomArea = RectangleF.Empty;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.BackgroundImageLayout = ImageLayout.None;
        }


        #region static

        private static PointF FindCenter(RectangleF r)
        {
            return new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f);
        }

        #endregion

        #region Zoom

        private RectangleF zoomArea;

        public PointF PointToImage(Point location)
        {
            float pX = this.zoomArea.X + location.X * this.zoomArea.Width / this.ClientRectangle.Width;
            float pY = this.zoomArea.Y + location.Y * this.zoomArea.Height / this.ClientRectangle.Height;

            return new PointF(pX, pY);
        }

        public RectangleF RectangleToImage(RectangleF zone)
        {
            if (zone.Width < 0)
            {
                zone.X += zone.Width;
                zone.Width = -zone.Width;
            }

            if (zone.Height < 0)
            {
                zone.Y += zone.Height;
                zone.Height = -zone.Height;
            }

            float rX = this.zoomArea.Width / this.ClientRectangle.Width;
            float rY = this.zoomArea.Height / this.ClientRectangle.Height;

            float pX = this.zoomArea.X + zone.X * rX;
            float pY = this.zoomArea.Y + zone.Y * rY;
            float rW = zone.Width * rX;
            float rH = zone.Height * rY;

            return new RectangleF(pX, pY, rW, rH);
        }

        public void ShowCenter()
        {
            if (this.Visible && this.Image is Image)
            {
                this.zoomArea.X = (this.Image.Width - this.zoomArea.Width) / 2;
                this.zoomArea.Y = (this.Image.Height - this.zoomArea.Height) / 2;

                this.OnVisiblePortionChanged();
            }
        }

        public void ShowCenter(Point mousePoint)
        {
            if (this.Visible && this.Image is Image)
            {
                PointF p = this.PointToImage(mousePoint);
                PointF c = FindCenter(this.zoomArea);

                this.zoomArea.X = p.X - c.X;
                this.zoomArea.Y = p.Y - c.Y;

                this.OnVisiblePortionChanged();
            }
        }

        public void ShowAll()
        {
            if (this.Visible && this.Image is Image)
            {
                this.zoomArea.Location = PointF.Empty;
                this.zoomArea.Size = this.Image.Size;

                this.OnVisiblePortionChanged();
            }
        }

        public void Pan(Point p)
        {
            if (this.Visible && this.Image is Image)
            {
                PointF pp = this.PointToImage(p);

                this.zoomArea.X = p.X - this.zoomArea.Width / 2;
                this.zoomArea.Y = p.Y - this.zoomArea.Height / 2;

                this.OnVisiblePortionChanged();
            }
        }

        public void Pan(int w, int h)
        {
            if (this.Visible && this.Image is Image)
            {
                PointF p = this.PointToImage(new Point(w, h));
                PointF c = this.PointToImage(Point.Empty);

                this.zoomArea.X -= p.X - c.X;
                this.zoomArea.Y -= p.Y - c.Y;

                this.OnVisiblePortionChanged();
            }
        }

        public void Zoom(float zoom, Point mousePoint)
        {
            if (this.Visible && this.Image is Image)
            {
                PointF p = this.PointToImage(mousePoint);

                this.zoomArea.Width = this.zoomArea.Width / zoom;
                this.zoomArea.Height = this.zoomArea.Height / zoom;

                PointF c = this.PointToImage(mousePoint);

                this.zoomArea.X += (p.X - c.X);
                this.zoomArea.Y += (p.Y - c.Y);

                this.OnVisiblePortionChanged();
            }
        }

        #endregion

        #region Mouse

        private Point mouseLocation;
        private Size previousSize;
        private Cursor previousCursor;

        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Focus();
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            this.mouseLocation = Point.Empty;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (this.ClientRectangle.Contains(e.Location))
            {
                if (e.Delta < 0)
                {
                    this.Zoom(0.9f, e.Location);
                }

                if (e.Delta > 0)
                {
                    this.Zoom(1.1f, e.Location);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.previousCursor = this.Cursor;
            base.OnMouseDown(e);

            switch (e.Button)
            {
                case MouseButtons.Middle:
                    this.Cursor = Cursors.SizeAll;
                    break;

                case MouseButtons.Left:
                case MouseButtons.Right:
                    if (this.AllowSelection)
                    {
                        this.mouseSelection = new Rectangle(e.Location, Size.Empty);
                    }
                    break;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.Cursor = this.previousCursor;
            base.OnMouseUp(e);

            switch (e.Button)
            {
                case MouseButtons.Left:
                case MouseButtons.Right:
                    if (this.AllowSelection)
                    {
                        this.OnDrawSelection(this.mouseSelection);
                    }
                    break;
            }

            if (this.AllowSelection)
            {
                this.OnSelectedAreaChanged(this.mouseSelection);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            switch (e.Button)
            {
                case MouseButtons.Middle:
                    this.Pan(e.Location.X - this.mouseLocation.X, e.Location.Y - this.mouseLocation.Y);
                    break;

                case MouseButtons.Left:
                case MouseButtons.Right:
                    if (this.ClientRectangle.Contains(e.Location) && this.AllowSelection)
                    {

                        this.OnDrawSelection(this.mouseSelection);

                        this.mouseSelection.Width = e.Location.X - this.mouseSelection.X;
                        this.mouseSelection.Height = e.Location.Y - this.mouseSelection.Y;

                        this.OnDrawSelection(this.mouseSelection);

                    }
                    break;
            }

            this.mouseLocation = e.Location;
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            this.UseWaitCursor = true;

            e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

            if (this.Image is Image)
            {
                if (this.zoomArea.Size == Size.Empty)
                {
                    e.Graphics.DrawImage(base.Image, this.ClientRectangle, new RectangleF(0, 0, this.Image.Width, this.Image.Height), GraphicsUnit.Pixel);
                }
                else
                {
                    try
                    {
                        if (this.mouseSelection != Rectangle.Empty)
                        {
                            e.Graphics.FillRectangle(Brushes.Azure, this.mouseSelection);
                        }
                        e.Graphics.DrawImage(base.Image, this.ClientRectangle, this.zoomArea, GraphicsUnit.Pixel);
                    }
                    catch
                    {
                        e.Graphics.DrawImage(this.ErrorImage, new Point(2, 2));
                    }
                }
            }
            else
            {
                if (this.InitialImage is Image)
                {
                    float w = this.ClientSize.Width;
                    float h = this.ClientSize.Height;

                    if (this.ClientSize.Width > this.ClientSize.Height * this.InitialImage.Width / this.InitialImage.Height)
                    {
                        w = h * this.InitialImage.Width / this.InitialImage.Height;
                    }
                    else
                    {
                        h = w * this.InitialImage.Height / this.InitialImage.Width;
                    }

                    float x = (this.ClientSize.Width - w) / 2f;
                    float y = (this.ClientSize.Height - h) / 2f;

                    e.Graphics.DrawImage(this.InitialImage, new RectangleF(x, y, w, h), new RectangleF(0, 0, this.InitialImage.Width, this.InitialImage.Height), GraphicsUnit.Pixel);
                }
            }

            this.UseWaitCursor = false;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);

            if (this.previousSize.Height > 0 && this.previousSize.Width > 0 && this.Size.Height > 0)
            {
                SizeF s = SizeF.Subtract(this.ClientRectangle.Size, this.previousSize);

                this.zoomArea.Width += s.Width * this.zoomArea.Width / this.previousSize.Width;
                this.zoomArea.Height += s.Height * this.zoomArea.Height / this.previousSize.Height;

                this.Invalidate();

                this.previousSize = this.ClientRectangle.Size;
            }

           
        }

        protected override void OnLoadCompleted(System.ComponentModel.AsyncCompletedEventArgs e)
        {
            base.OnLoadCompleted(e);

            this.zoomArea.Location = Point.Empty;
            this.zoomArea.Size = base.Image.Size;
        }
        #endregion

        #region Selection

        private Rectangle mouseSelection;

        public RectangleF SelectedArea
        {
            get { return this.RectangleToImage(this.mouseSelection); }
        }

        protected virtual void OnDrawSelection(Rectangle mouseSelection)
        {
            ControlPaint.DrawReversibleFrame(this.RectangleToScreen(mouseSelection), Color.Gray, FrameStyle.Dashed);
        }

        public event EventHandler SelectedAreaChanged;

        protected virtual void OnSelectedAreaChanged(RectangleF selectedArea)
        {
            if (this.SelectedAreaChanged != null)
            {
                this.SelectedAreaChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Properties

        public Rectangle VisiblePortion
        {
            get
            {
                return Rectangle.Round(this.zoomArea);
            }
        }

        public new PictureBoxSizeMode SizeMode
        {
            get
            {
                return base.SizeMode;
            }
            set
            {
                if (value != PictureBoxSizeMode.Normal)
                {
                    throw new ArgumentOutOfRangeException("PictureBoxSizeMode must be 'Normal'");
                }

                base.SizeMode = PictureBoxSizeMode.Normal;
            }
        }

        public new Image Image
        {
            get
            {
                return base.Image;
            }
            set
            {
                base.Image = value;
                if (value is Image && this.zoomArea.IsEmpty)
                {
                    this.zoomArea = new RectangleF(Point.Empty, value.Size);
                }
            }
        }

        #endregion

        #region Thumbnail

        public Image GetThumbnail(int w, int h)
        {
            return this.GetThumbnail(w, h, Color.Transparent);
        }

        public Image GetThumbnail(int w, int h, Color c)
        {
            Image thumb = this.Image.GetThumbnailImage(w, h, new Image.GetThumbnailImageAbort(this.IsThumbnailAbort), IntPtr.Zero);

            float rX = (float)w / this.Image.Width;
            float rY = (float)h / this.Image.Height;

            RectangleF r = new RectangleF(rX * this.zoomArea.X, rY * this.zoomArea.Y, rX * this.zoomArea.Width, rY * this.zoomArea.Height);

            using (Graphics g = Graphics.FromImage(thumb))
            {
                g.DrawRectangle(new Pen(c), Rectangle.Truncate(r));
            }

            return thumb;
        }

        private bool cancelThumb = false;

        public void ThumbnailAbort()
        {
            this.cancelThumb = true;
        }

        public bool IsThumbnailAbort()
        {
            return this.cancelThumb;
        }

        #endregion

        #region Events

        public event EventHandler VisiblePortionChanged;

        protected virtual void OnVisiblePortionChanged()
        {
            this.Invalidate();

            if (this.VisiblePortionChanged != null)
            {
                this.VisiblePortionChanged(this, new EventArgs());
            }
        }

        #endregion
    }
}
