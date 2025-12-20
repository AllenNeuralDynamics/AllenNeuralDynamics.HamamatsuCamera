using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    public partial class ImageVisualizer : UserControl
    {
        private const double _maxImageScale = 50.0;
        private const double _minImageScale = 1.0;
        private const double _scaleIncrement = 5.0;

        private ulong _prevFrameCount;
        private double _prevTimestamp;
        private bool _isFirstFrame;
        private bool _isMouseDown;
        private bool _isAddingRegion;
        private bool _isMovingRegion;
        private bool _isScalingRegion;
        private Point _mouseDownStartPosition;
        private double _imageScale = 11.0;

        public event EventHandler RegionsChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<RegionOfInterest> Regions
        {
            get { return ImageRendererControl.Regions; }
            set
            {
                ImageRendererControl.Regions = value;
                if (ImageRendererControl.Regions != null)
                    CountVal_Label.Text = ImageRendererControl.Regions.Count.ToString();
            }
        }
        public Point CurrentCropLocation
        {
            get { return ImageRendererControl.CurrentCropLocation; }
            set { ImageRendererControl.CurrentCropLocation = value; }
        }
        public Rectangle NextCrop
        {
            get { return ImageRendererControl.NextCrop; }
            set { ImageRendererControl.NextCrop = value; }
        }
        public CropMode CropMode
        {
            get { return ImageRendererControl.CropMode; }
            set { ImageRendererControl.CropMode = value; }
        }
        private int SelectedRegion
        {
            get { return ImageRendererControl.SelectedRegion; }
            set { ImageRendererControl.SelectedRegion = value; }
        }
        internal int NumPixelsHorizontal { get; set; }
        internal int NumPixelsVertical { get; set; }
        internal int Binning { get; set; }


        #region Form Access


        /// <summary>
        /// Initializes the user control and declares that the next frame is the first frame.
        /// </summary>
        public ImageVisualizer()
        {
            try
            {
                InitializeComponent();
                _isFirstFrame = true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Resets the stored frame counter and timestamp.
        /// </summary>
        internal void ResetFrameCount()
        {
            _prevFrameCount = 0;
            _prevTimestamp = 0;
        }

        /// <summary>
        /// Triggers an update from a new frame, handling any errors that occurs.
        /// </summary>
        /// <param name="frameContainer">New frame.</param>
        internal void TryUpdateNewFrame(IFrameContainer frameContainer)
        {
            try
            {
                UpdateNewFrame(frameContainer);
            }
            catch (ObjectDisposedException)
            {
                ConsoleLogger.SuppressError();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the ImageRendererControl with the new image and updates the FPS display.
        /// </summary>
        /// <param name="frameContainer">New frame.</param>
        private void UpdateNewFrame(IFrameContainer frameContainer)
        {
            if (frameContainer is Frame frame)
            {
                if (frame.Image == null) return;
                InitFirstFrame();
                ImageRendererControl.ProcessImage(frame.Image, _imageScale);
                UpdateFPS(frame);
            }
        }

        /// <summary>
        /// On the first frame, store the full image width and height in bins.
        /// Also initializes the next crop and current crop location.
        /// </summary>
        private void InitFirstFrame()
        {
            if (!_isFirstFrame) return;

            ImageRendererControl.ImageFullWidth = NumPixelsHorizontal / Binning;
            ImageRendererControl.ImageFullHeight = NumPixelsVertical / Binning;
            NextCrop = GetNextCrop();
            CurrentCropLocation = NextCrop.Location;

            _isFirstFrame = false;
        }

        /// <summary>
        /// Calculates the FPS based and the previous and current frame counter and timestamp.
        /// Updates the FPS display accordingly.
        /// </summary>
        /// <param name="frame">New Frame.</param>
        private void UpdateFPS(Frame frame)
        {
            if(_prevFrameCount == 0)
            {
                _prevFrameCount = frame.FrameCounter;
                _prevTimestamp = frame.Timestamp;
                return;
            }

            var df = frame.FrameCounter - _prevFrameCount;
            var dt = frame.Timestamp - _prevTimestamp;
            _prevFrameCount = frame.FrameCounter;
            _prevTimestamp = frame.Timestamp;
            var fps = df / dt;
            var text = fps.ToString("0.##") + " Hz";
            if (FPSVal_Label.InvokeRequired)
            {
                FPSVal_Label.Invoke(new Action(() =>
                {
                    FPSVal_Label.Text = text;
                }));
            }
            else
                FPSVal_Label.Text = text;
        }

        #endregion

        #region Event Handling
        /// <summary>
        /// ImageRendererControl MouseDown event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageRendererControl_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                Focus();
                HandleMouseDown(e);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// ImageRendererControl MouseUp event handler. Invokes the RegionsChanged event
        /// if regions were changed and resets all booleans used to track how the regions
        /// were being changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageRendererControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isAddingRegion || _isMovingRegion || _isScalingRegion)
                RegionsChanged?.Invoke(this, EventArgs.Empty);

            _isAddingRegion = false;
            _isMovingRegion = false;
            _isScalingRegion = false;
            _isMouseDown = false;
        }

        /// <summary>
        /// ImageRendererControl MouseMove event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageRendererControl_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                HandleMouseMove(e);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// ImageVisualizer KeyDown event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageVisualizer_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                HandleKeyDown(e);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Tries to tab select the next region.
        /// </summary>
        internal void TryTabSelectRegion()
        {
            try
            {
                TabSelectRegion();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// If regions exist, select the next region.
        /// </summary>
        private void TabSelectRegion()
        {
            if (Regions.Count == 0) return;

            SelectedRegion = (SelectedRegion + 1) % Regions.Count;
        }

        /// <summary>
        /// Store the mouse down start position, checks which region (if any) were selected.
        /// Update that the mouse is currently down and determine whether a region is being
        /// added, moved, or scaled.
        /// </summary>
        /// <param name="e"></param>
        private void HandleMouseDown(MouseEventArgs e)
        {
            _mouseDownStartPosition = GetLocationInFrame(e.Location);
            SelectedRegion = GetSelectedRegion();
            _isMouseDown = true;
            _isAddingRegion = SelectedRegion < 0 && e.Button == MouseButtons.Left;
            _isMovingRegion = SelectedRegion >= 0 && e.Button == MouseButtons.Left;
            _isScalingRegion = SelectedRegion >= 0 && e.Button == MouseButtons.Right;
        }

        /// <summary>
        /// Add, move, or scale a region. Alternatively, do nothing.
        /// </summary>
        /// <param name="e"></param>
        private void HandleMouseMove(MouseEventArgs e)
        {
            if (_isAddingRegion)
                AddRegion(e.Location);
            else if (_isMovingRegion)
                MoveRegion(e.Location);
            else if (_isScalingRegion)
                ScaleRegion(e.Location);
        }

        /// <summary>
        /// Do nothing if mouse is down. Alternatively, adjust image scale or delete a region.
        /// </summary>
        /// <param name="e"></param>
        private void HandleKeyDown(KeyEventArgs e)
        {
            // Do nothing if _isMouseDown
            if (_isMouseDown)
                return;

            if (e.KeyCode == Keys.PageUp && _imageScale < _maxImageScale) _imageScale = (byte)Math.Min(_imageScale + _scaleIncrement, _maxImageScale);
            else if (e.KeyCode == Keys.PageDown && _imageScale > _minImageScale) _imageScale = (byte)Math.Max(_imageScale - _scaleIncrement, _minImageScale);
            else if (e.KeyCode == Keys.Delete) DeleteRegion();
        }

        /// <summary>
        /// Removes the currently selected region, update the UI and storage members, and invoke the RegionsChanged event.
        /// </summary>
        private void DeleteRegion()
        {
            if (SelectedRegion < 0 || Regions == null || Regions.Count == 0) return;

            Regions.RemoveAt(SelectedRegion);
            CountVal_Label.Text = Regions.Count.ToString();
            SelectedRegion = Math.Max(SelectedRegion - 1, 0);

            NextCrop = GetNextCrop();
            RegionsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Creates a new region at the specified location.
        /// Update the UI and storage members.
        /// </summary>
        /// <param name="location"></param>
        private void AddRegion(Point location)
        {
            var region = CreateRegion(location);
            Regions.Add(region);
            CountVal_Label.Text = Regions.Count.ToString();
            SelectedRegion = Regions.Count - 1;
            _isAddingRegion = false;
            _isScalingRegion = true;
            NextCrop = GetNextCrop();
        }

        /// <summary>
        /// Replace the currently selected region with a new <see cref="RegionOfInterest"/>
        /// translated based on the specified location.
        /// </summary>
        /// <param name="location">The current mouse location.</param>
        private void MoveRegion(Point location)
        {
            var currentRegion = Regions[SelectedRegion];
            var prevMousePos = _mouseDownStartPosition;
            var currMousePos = GetLocationInFrame(location);
            var tx = currMousePos.X - prevMousePos.X;
            var ty = currMousePos.Y - prevMousePos.Y;
            var xPos = Math.Max(0, Math.Min(currentRegion.X + tx, ImageRendererControl.ImageFullWidth - 1 - currentRegion.Width));
            var yPos = Math.Max(0, Math.Min(currentRegion.Y + ty, ImageRendererControl.ImageFullHeight - 1 - currentRegion.Height));
            var region = new RegionOfInterest(xPos, yPos, currentRegion.Width, currentRegion.Height);
            Regions[SelectedRegion] = region;
            _mouseDownStartPosition = currMousePos;
            NextCrop = GetNextCrop();
        }

        /// <summary>
        /// Replace the currently selected region with a new <see cref="RegionOfInterest"/>
        /// scaled based on the current mouse location.
        /// </summary>
        /// <param name="location"></param>
        private void ScaleRegion(Point location)
        {
            var region = CreateRegion(location);
            Regions[SelectedRegion] = region;
            NextCrop = GetNextCrop();
        }

        /// <summary>
        /// Create a new <see cref="RegionOfInterest"/> based on the current and stored mouse position.
        /// </summary>
        /// <param name="location">Current mouse position.</param>
        /// <returns>New <see cref="RegionOfInterest"/>.</returns>
        private RegionOfInterest CreateRegion(Point location)
        {
            var centerPos = _mouseDownStartPosition;
            var edgePos = GetLocationInFrame(location);
            var halfWidth = Math.Abs(edgePos.X - centerPos.X);
            var halfHeight = Math.Abs(edgePos.Y - centerPos.Y);

            // Ensure region within bounds of image
            var minXDistToImgEdge = Math.Min(ImageRendererControl.ImageFullWidth - centerPos.X, centerPos.X);
            var minYDistToImgEdge = Math.Min(ImageRendererControl.ImageFullHeight - centerPos.Y, centerPos.Y);
            halfWidth = Math.Min(halfWidth, minXDistToImgEdge);
            halfHeight = Math.Min(halfHeight, minYDistToImgEdge);

            // Enable Control modifier key
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var min = Math.Min(halfWidth, halfHeight);
                halfWidth = min;
                halfHeight = min;
            }
            var xPos = centerPos.X - halfWidth;
            var yPos = centerPos.Y - halfHeight;
            var width = 2 * halfWidth;
            var height = 2 * halfHeight;
            var region = new RegionOfInterest(xPos, yPos, width, height);
            return region;
        }

        /// <summary>
        /// Get the selected region index based on the intersection of the mouse down start position
        /// and the regions of interest.
        /// </summary>
        /// <returns>-1 if no region was selected. Otherwise, the region index.</returns>
        private int GetSelectedRegion()
        {
            for (var i = 0; i < Regions.Count; i++)
                if (TestIntersection(Regions[i], _mouseDownStartPosition))
                    return i;
            return -1;
        }

        /// <summary>
        /// Checks if the location is inside of a region.
        /// </summary>
        /// <param name="region">Region of interest.</param>
        /// <param name="locInFrame">Location in frame.</param>
        /// <returns></returns>
        private static bool TestIntersection(RegionOfInterest region, Point locInFrame)
        {
            var regionPoints = GetPoints(region);

            // Check each edge of the rectangle to see if point lies of the edge, all edges must pass
            for (var i = 0; i < regionPoints.Count; i++)
            {
                var point2 = regionPoints[(i + 1) % regionPoints.Count];
                var point1 = regionPoints[i % regionPoints.Count];

                var D = (point2.X - point1.X) * (locInFrame.Y - point1.Y) - (locInFrame.X - point1.X) * (point2.Y - point1.Y);

                if (D < 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Converts <see cref="RegionOfInterest"/> to a <see cref="List{Point}"/>.
        /// </summary>
        /// <param name="region">Region of Interest</param>
        /// <returns>List of points.</returns>
        private static List<Point> GetPoints(RegionOfInterest region)
        {
            var points = new List<Point>
            {
                new Point(region.X, region.Y),
                new Point(region.X + region.Width, region.Y),
                new Point(region.X + region.Width, region.Y + region.Height),
                new Point(region.X, region.Y + region.Height)
            };
            return points;
        }

        /// <summary>
        /// Converts a location from UI display coordinates to frame coordinates.
        /// </summary>
        /// <param name="location">Location in UI coordinates.</param>
        /// <returns>Location in frame coordinates.</returns>
        private Point GetLocationInFrame(Point location)
        {
            var x = location.X;
            var y = location.Y;
            var frame_width = ImageRendererControl.ImageFullWidth;
            var frame_height = ImageRendererControl.ImageFullHeight;
            var image_width = ImageRendererControl.Width;
            var image_height = ImageRendererControl.Height;

            return new Point(
                Math.Max(0, Math.Min((int)(x * frame_width / (float)image_width), frame_width - 1)),
                Math.Max(0, Math.Min((int)(y * frame_height / (float)image_height), frame_height - 1)));
        }


        /// <summary>
        /// Gets the next crop that would be used if AutoCrop was enabled. Used to show the next crop while
        /// the current crop is stale (i.e. while moving a region of interest we want to wait to update the
        /// camera crop until we are done moving).
        /// </summary>
        /// <returns></returns>
        private Rectangle GetNextCrop()
        {
            try
            {
                if (Regions.Count > 0)
                {
                    var hPos = (int)Math.Ceiling(Regions.Select(rect => rect.X).Min() / 4.0) * 4;
                    var hSize = (int)Math.Floor((Regions.Select(rect => rect.X + rect.Width).Max() - hPos) / 4.0) * 4;
                    var vPos = (int)Math.Ceiling(Regions.Select(rect => rect.Y).Min() / 4.0) * 4;
                    var vSize = (int)Math.Floor((Regions.Select(rect => rect.Y + rect.Height).Max() - vPos) / 4.0) * 4;
                    return new Rectangle(hPos, vPos, hSize, vSize);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }

            return new Rectangle(0, 0, ImageRendererControl.ImageFullWidth, ImageRendererControl.ImageFullHeight);
        }

        /// <summary>
        /// Updates the binning and all stored members affected by it.
        /// </summary>
        /// <param name="newBinning">New binning</param>
        internal void UpdateBinning(int newBinning)
        {
            try
            {
                var s = (double)Binning / newBinning;
                for (int i = 0; i < Regions.Count; i++)
                    Regions[i] = new RegionOfInterest((int)(Regions[i].X * s), (int)(Regions[i].Y * s), (int)(Regions[i].Width * s), (int)(Regions[i].Height * s));

                ImageRendererControl.ImageFullWidth = NumPixelsHorizontal / newBinning;
                ImageRendererControl.ImageFullHeight = NumPixelsVertical / newBinning;
                Binning = newBinning;
                NextCrop = GetNextCrop();
                ImageRendererControl.CurrentCropLocation = NextCrop.Location;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the <see cref="Calibration.ImageRendererControl"/> with new
        /// image full width/height.
        /// </summary>
        /// <param name="manualCrop">New manual crop.</param>
        internal void UpdateSubarray(CropSettings manualCrop)
        {
            if(CropMode == CropMode.Auto || !manualCrop.Mode)
            {
                ImageRendererControl.ImageFullWidth = NumPixelsHorizontal / Binning;
                ImageRendererControl.ImageFullHeight = NumPixelsVertical / Binning;
            }
            else
            {
                ImageRendererControl.ImageFullWidth = (int)(manualCrop.HSize / Binning);
                ImageRendererControl.ImageFullHeight = (int)(manualCrop.VSize / Binning);
            }
        }

        #endregion

    }
}
