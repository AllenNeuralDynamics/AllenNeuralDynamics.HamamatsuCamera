using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    /// <summary>
    /// Custom <see cref="ZedGraphControl"/> for displaying the region data for a <see cref="RegionOfInterest"/>.
    /// Consists of 1 + <see cref="Processing.DeinterleaveCount"/> <see cref="GraphPane"/>, one for the interleaved data, and
    /// one for each channel. Utilizes a <see cref="CircularBufferPointList"/> for efficiently updating the region data stored
    /// by the curve stored in each <see cref="GraphPane"/>. Additionally, provides functionality for quickly swaping between
    /// displaying the interleaved or deinterleaved data.
    /// </summary>
    internal sealed class RegionPlot : ZedGraphControl
    {
        private readonly int _regionIndex;
        private readonly int _numSignals;
        private readonly CircularBufferPointList _interleavedBuffer;
        private readonly CircularBufferPointList[] _deinterleavedBuffers;
        private Form _parentForm;
        private bool _readyForLayout;

        public GraphPane InterleavedPlot { get; private set; }

        public List<GraphPane> DeinterleavedPlots { get; private set; }
        public int Capacity { get; set; }

        /// <summary>
        /// Stores configuration settings, initializes buffers,
        /// and initializes all of the <see cref="GraphPane"/>
        /// </summary>
        /// <param name="index">Index of the region of interest</param>
        /// <param name="numSignals">Number of interleaved signals</param>
        /// <param name="capacity">Capacity of the interleaved signal</param>
        internal RegionPlot(int index, int numSignals, int capacity)
        {
            SuspendLayout();
            _regionIndex = index;
            _numSignals = numSignals;
            Capacity = capacity;
            _interleavedBuffer = new CircularBufferPointList(capacity);
            _deinterleavedBuffers = new CircularBufferPointList[numSignals];
            var deinterleavedBufferCapacity = 300;
            if (numSignals != 0)
                deinterleavedBufferCapacity = capacity / numSignals;
            for (var i = 0; i < numSignals; i++)
                _deinterleavedBuffers[i] = new CircularBufferPointList(deinterleavedBufferCapacity);
            Configure();
            AddInterleavedPlot();
            AddDeinterleavedPlots();
            ResumeLayout(true);
        }

        /// <summary>
        /// Add a batch of <see cref="VisualizerData"/>, updating the
        /// axes.
        /// </summary>
        /// <param name="batch"></param>
        internal void AddFrames(IList<VisualizerData> batch)
        {
            foreach (var frame in batch)
            {
                double x = frame.Timestamp;
                double y = frame.RegionData[_regionIndex];
                _interleavedBuffer.Add(x, y);
                var signalIndex = (int)(frame.FrameCounter % (ulong)_numSignals);
                _deinterleavedBuffers[signalIndex].Add(x, y);
            }
            var xMin = _interleavedBuffer.MinX;
            var xMax = _interleavedBuffer.MaxX;
            var yMin = _interleavedBuffer.MinY;
            var yMax = _interleavedBuffer.MaxY;
            UpdateAxes(xMin, xMax, yMin, yMax, InterleavedPlot);
            for (var i = 0; i < _numSignals; i++)
            {
                var deintXMin = _deinterleavedBuffers[i].MinX;
                var deintXMax = _deinterleavedBuffers[i].MaxX;
                var deintYMin = _deinterleavedBuffers[i].MinY;
                var deintYMax = _deinterleavedBuffers[i].MaxY;
                UpdateAxes(deintXMin, deintXMax, deintYMin, deintYMax, DeinterleavedPlots[i]);
            }
        }

        /// <summary>
        /// Add a batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays,
        /// updating the axes.
        /// </summary>
        /// <param name="batch"></param>
        internal void AddFrames(IList<FrameData[]> batch)
        {
            foreach (var frameDataBundle in batch)
            {
                foreach (var frameData in frameDataBundle)
                {
                    double x = frameData.Timestamp;
                    double y = frameData.RegionData[_regionIndex];
                    _interleavedBuffer.Add(x, y);
                    var signalIndex = (int)(frameData.FrameCounter % (ulong)_numSignals);
                    _deinterleavedBuffers[signalIndex].Add(x, y);
                }
            }
            var xMin = _interleavedBuffer.MinX;
            var xMax = _interleavedBuffer.MaxX;
            var yMin = _interleavedBuffer.MinY;
            var yMax = _interleavedBuffer.MaxY;
            UpdateAxes(xMin, xMax, yMin, yMax, InterleavedPlot);
            for (var i = 0; i < _numSignals; i++)
                UpdateAxes(xMin, xMax, yMin, yMax, DeinterleavedPlots[i]);
        }

        /// <summary>
        /// Updates the X and Y axes: Min, Max, BaseTic, and Major Step
        /// Additionally updates the Y axis scale format to accommodate different
        /// amounts of digits.
        /// </summary>
        /// <param name="xMin">New X min</param>
        /// <param name="xMax">New X max</param>
        /// <param name="yMin">New Y min</param>
        /// <param name="yMax">New Y Max</param>
        /// <param name="pane"><see cref="GraphPane"/> to update.</param>
        private void UpdateAxes(double xMin, double xMax, double yMin, double yMax, GraphPane pane)
        {
            pane.YAxis.Scale.Min = yMin;
            pane.YAxis.Scale.Max = yMax;
            pane.XAxis.Scale.Min = xMin;
            pane.XAxis.Scale.Max = xMax;
            pane.XAxis.Scale.MajorStep = Math.Round(4.0 * (xMax - xMin) / 5.0, MidpointRounding.ToEven) / 4.0;
            pane.XAxis.Scale.BaseTic = 0;
            var ticProps = FindTicProps(yMin, yMax);
            pane.YAxis.Scale.BaseTic = ticProps.Item1;
            pane.YAxis.Scale.MajorStep = ticProps.Item2;
            var numDigits = ((decimal)ticProps.Item2).ToString().Length;
            if (numDigits > 7)
            {

                var sigFigs = ticProps.Item3;
                pane.YAxis.Scale.Format = "E" + sigFigs;
            }
            else
            {
                var sigfigs = ((decimal)ticProps.Item2).ToString().Length - 2;
                pane.YAxis.Scale.Format = "N" + sigfigs;
            }
        }

        /// <summary>
        /// Updates whether to display the interleaved or deinterleaved plot.
        /// </summary>
        /// <param name="deinterleaved">True for showing the deinterleaved plot.</param>
        internal void UpdateLayout(bool deinterleaved)
        {

            MasterPane.PaneList.Clear();
            // If should be interleaved
            if (!deinterleaved)
            {
                MasterPane.Add(InterleavedPlot);
                using Graphics g = CreateGraphics();
                MasterPane.ReSize(g);
            }
            // Otherwise, should be deinterleaved
            else
            {
                foreach (var deintPlot in DeinterleavedPlots)
                    MasterPane.Add(deintPlot);

                // Set the layout of the graph panes for each region plot to be a single column
                using (Graphics g = CreateGraphics())
                    MasterPane.SetLayout(g, PaneLayout.SingleColumn);

                // Update the format of the attached deinterleaved graph panes
                UpdateDeintPlots();
            }
        }

        /// <summary>
        /// Configure the <see cref="ZedGraphControl"/> top level.
        /// </summary>
        private void Configure()
        {
            Dock = DockStyle.Fill;

            MasterPane.Tag = "Master";
            MasterPane.Title.IsVisible = true;
            MasterPane.Title.Text = "ROI " + _regionIndex;
            MasterPane.Title.FontSpec.Size = 18;
            MasterPane.TitleGap = 0.0f;
            MasterPane.InnerPaneGap = 0;
            MasterPane.Margin.All = 2.5f;
            MasterPane.Border.IsVisible = true;
            MasterPane.IsFontsScaled = true;
            MasterPane.PaneList.Clear();
        }

        /// <summary>
        /// Create and configure the <see cref="GraphPane"/> for the interleaved plot
        /// then add it to the <see cref="ZedGraphControl"/>
        /// </summary>
        private void AddInterleavedPlot()
        {
            InterleavedPlot = new GraphPane
            {
                Tag = 0
            };
            InterleavedPlot.Title.IsVisible = false;
            InterleavedPlot.YAxisList.Clear();
            InterleavedPlot.AddYAxis("Fluorescence (A.U.)");
            InterleavedPlot.Border.IsVisible = true;

            InterleavedPlot.Margin.All = 0;
            InterleavedPlot.YAxis.Title.Gap = 0;
            InterleavedPlot.XAxis.IsVisible = true;
            InterleavedPlot.XAxis.Scale.IsVisible = true;
            InterleavedPlot.XAxis.Scale.MajorUnit = DateUnit.Second;
            InterleavedPlot.XAxis.Scale.BaseTic = 0.0;

            InterleavedPlot.XAxis.MajorTic.IsAllTics = true;
            InterleavedPlot.XAxis.MajorTic.IsInside = true;
            InterleavedPlot.XAxis.MajorTic.IsOutside = false;
            InterleavedPlot.XAxis.MajorTic.IsCrossOutside = false;
            InterleavedPlot.XAxis.MajorTic.Size = 10;
            InterleavedPlot.XAxis.Scale.LabelGap = 0;
            InterleavedPlot.XAxis.AxisGap = 0;
            InterleavedPlot.XAxis.MinorTic.IsAllTics = false;
            InterleavedPlot.XAxis.IsAxisSegmentVisible = true;
            InterleavedPlot.XAxis.Title.Text = "Time (seconds)";

            InterleavedPlot.XAxis.Title.Gap = 0;
            InterleavedPlot.XAxis.Title.IsVisible = true;
            InterleavedPlot.Chart.IsRectAuto = true;
            InterleavedPlot.IsFontsScaled = true;

            var interleavedSeries = InterleavedPlot.AddCurve("Data", _interleavedBuffer, Color.Black, SymbolType.None);
            interleavedSeries.Label.IsVisible = false;
            interleavedSeries.Line.IsAntiAlias = false;
            interleavedSeries.Line.IsOptimizedDraw = true;

            MasterPane.Add(InterleavedPlot);
        }

        /// <summary>
        /// Create and configure the <see cref="GraphPane"/> for each deinterleaved plot.
        /// </summary>
        private void AddDeinterleavedPlots()
        {
            DeinterleavedPlots = new List<GraphPane>();
            for (int j = 1; j <= _numSignals; j++)
            {
                GraphPane graph = new()
                {
                    Tag = j
                };
                graph.Title.IsVisible = false;
                graph.Border.IsVisible = false;
                graph.Margin.All = 0;
                graph.Chart.IsRectAuto = true;
                graph.IsFontsScaled = true;

                graph.YAxisList.Clear();
                graph.AddYAxis(j.ToString());
                Color color = GetColor(j);
                graph.YAxis.Color = color;
                graph.YAxis.MinorTic.Color = color;
                graph.YAxis.MinorTic.Color = color;
                graph.YAxis.Scale.FontSpec.FontColor = color;
                graph.YAxis.Title.FontSpec.FontColor = color;

                graph.YAxis.Title.Gap = 0;
                graph.XAxis.IsVisible = true;
                graph.XAxis.Scale.MajorUnit = DateUnit.Second;
                graph.XAxis.MajorTic.IsAllTics = true;
                graph.XAxis.MajorTic.IsInside = true;
                graph.XAxis.MajorTic.IsOutside = false;
                graph.XAxis.MajorTic.IsCrossOutside = false;
                graph.XAxis.MajorTic.Size = 10;
                graph.XAxis.Scale.LabelGap = 0;
                graph.XAxis.AxisGap = 0;
                graph.XAxis.MinorTic.IsAllTics = false;
                graph.XAxis.IsAxisSegmentVisible = true;
                graph.XAxis.Title.Text = "Time (seconds)";
                graph.XAxis.Title.Gap = 0;
                graph.XAxis.Scale.BaseTic = 0.0;
                graph.XAxis.Scale.IsVisible = j % _numSignals == 0;
                graph.XAxis.Title.IsVisible = j % _numSignals == 0;

                var series = graph.AddCurve("Data", _deinterleavedBuffers[j - 1], color, SymbolType.None);
                series.Label.IsVisible = false;
                series.Line.IsAntiAlias = false;
                series.Line.IsOptimizedDraw = true;
                DeinterleavedPlots.Add(graph);
            }
        }

        /// <summary>
        /// Gets a color dependent on the signal index of the region plot.
        /// </summary>
        /// <param name="index">Index for the signal the region plot</param>
        /// <returns>Color dependent on the signal index</returns>
        private Color GetColor(int index)
        {
            if (index == 0)
                return Color.Black;
            else if (_numSignals == 1)
                return Color.Red;

            double normalizedIndex = (index - 1) / (double)(_numSignals - 1);
            int red, green, blue;
            if (normalizedIndex <= 0.25)
            {
                red = 255;
                green = Remap(normalizedIndex, 0.0, 0.25, 0, 255);
                blue = 0;
            }
            else if (normalizedIndex <= 0.5)
            {
                red = Remap(normalizedIndex, 0.25, 0.5, 255, 0);
                green = 255;
                blue = 0;
            }
            else if (normalizedIndex <= 0.75)
            {
                red = 0;
                green = 255;
                blue = Remap(normalizedIndex, 0.5, 0.75, 0, 255);
            }
            else
            {
                red = 0;
                green = Remap(normalizedIndex, 0.75, 1.0, 255, 0);
                blue = 255;
            }
            return Color.FromArgb(red, green, blue);
        }

        /// <summary>
        /// 1D remapping to a new range.
        /// </summary>
        /// <param name="value">Value to remap</param>
        /// <param name="min">Current minimum value</param>
        /// <param name="max">Current maximum value</param>
        /// <param name="newMin">New minimum value</param>
        /// <param name="newMax">New maximum value</param>
        /// <returns></returns>
        private int Remap(double value, double min, double max, int newMin, int newMax)
        {
            return (int)((value - min) / (max - min) * (double)(newMax - newMin) + (double)newMin);
        }

        /// <summary>
        /// Gets the base tic, step, and significant figures of the Y-Axis scale based on the min and max value.
        /// </summary>
        /// <param name="min">Min value</param>
        /// <param name="max">Max value</param>
        /// <returns></returns>
        private Tuple<double, double, int> FindTicProps(double min, double max)
        {
            double diff = max - min;

            double power = 1.0;
            double val;
            bool lookingForPower = true;
            if (diff > 0)
            {
                while (lookingForPower)
                {
                    val = diff * Math.Pow(10.0, power);
                    if (val >= 1.0)
                    {
                        lookingForPower = false;
                    }
                    else
                    {
                        power += 1.0;
                    }
                }
            }
            double step = (int)(diff * Math.Pow(10.0, power)) / (Math.Pow(10.0, power) * 5.0);
            double baseTic = (int)(min * Math.Pow(10.0, power)) / (Math.Pow(10.0, power) * 5.0);
            int sigfigs = (int)power;

            double avg = (min + max) / 2.0;

            power = 1.0;
            lookingForPower = true;
            if (diff > 0)
            {
                while (lookingForPower)
                {
                    val = avg * Math.Pow(10.0, power);
                    if (val >= 1.0)
                    {
                        lookingForPower = false;
                    }
                    else
                    {
                        power += 1.0;
                    }
                }
            }
            sigfigs -= (int)power - 1;



            return Tuple.Create(baseTic, step, sigfigs);
        }

        /// <summary>
        /// Updates the sizes of the deinterleaved graph panes such that
        /// they are the same size, share the same X-Axis,
        /// and have the same font sizes.
        /// </summary>
        private void UpdateDeintPlots()
        {
            var scaleFactor = MasterPane.CalcScaleFactor();
            var margin = MasterPane.Margin.Left;
            var titleHeight = MasterPane.Title.FontSpec.Size * scaleFactor + 5.0f;
            var height = Height - titleHeight - 2.0f * margin;
            var width = (float)Width - 2.0f * margin;
            var xAxisHeight = height * 0.1f;
            var yAxisWidth = width * 0.1f;

            var chartSize = new SizeF(width - yAxisWidth, (height - xAxisHeight) / (float)DeinterleavedPlots.Count);
            var chartPosX = yAxisWidth + margin;

            for (int j = 0; j < DeinterleavedPlots.Count; j++)
            {
                var deintGraph = DeinterleavedPlots[j];

                var chartPoint = new PointF(chartPosX, chartSize.Height * j + margin + titleHeight);
                deintGraph.Chart.Rect = new RectangleF(chartPoint, chartSize);

                var rectPoint = new PointF(margin, chartSize.Height * j + margin + titleHeight);
                var rectSize = new SizeF(width, chartSize.Height);
                var rect = new RectangleF(rectPoint, rectSize);
                rect.Height += j == DeinterleavedPlots.Count - 1 ? xAxisHeight : 0;
                deintGraph.Rect = rect;

                var paneScaleFactor = deintGraph.CalcScaleFactor();
                deintGraph.YAxis.MinorTic.Size = yAxisWidth * 0.05f / paneScaleFactor;
                deintGraph.YAxis.MajorTic.Size = yAxisWidth * 0.1f / paneScaleFactor;
                deintGraph.YAxis.Scale.LabelGap = 0.1f;
                deintGraph.YAxis.Scale.FontSpec.Size = yAxisWidth * 0.5f / (paneScaleFactor * 6.0f);
                deintGraph.YAxis.Title.Gap = 0.1f;
                deintGraph.YAxis.Title.FontSpec.Size = yAxisWidth * 0.2f / paneScaleFactor;

                deintGraph.XAxis.MinorTic.Size = xAxisHeight * 0.05f / paneScaleFactor;
                deintGraph.XAxis.MajorTic.Size = xAxisHeight * 0.1f / paneScaleFactor;
                deintGraph.XAxis.Scale.LabelGap = 0.1f;
                deintGraph.XAxis.Scale.FontSpec.Size = xAxisHeight * 0.3f / paneScaleFactor;
                deintGraph.XAxis.Title.Gap = 0.1f;
                deintGraph.XAxis.Title.FontSpec.Size = xAxisHeight * 0.4f / paneScaleFactor;
            }
        }

        /// <summary>
        /// Update the deinterleaved plots when layout is called.
        /// Note: <see cref="ParentForm_ResizeBegin(object, EventArgs)"/>
        /// and <see cref="ParentForm_ResizeEnd(object, EventArgs)"/> are used to
        /// suppress the majority of fast <see cref="OnLayout(LayoutEventArgs)"/> calls so
        /// it is fairly efficient to do this here.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            if (_readyForLayout)
                UpdateDeintPlots();
        }

        /// <summary>
        /// Used to find the parent form and add event handlers to
        /// the parent form's <see cref="Form.ResizeBegin"/> and <see cref="Form.ResizeEnd"/>
        /// events. This way we can update layout at only at the end of resizing and not while resizing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            // Unhook old form events if needed
            if (_parentForm != null)
            {
                _parentForm.ResizeBegin -= ParentForm_ResizeBegin;
                _parentForm.ResizeEnd -= ParentForm_ResizeEnd;
                _parentForm = null;
            }

            // Try to find the form after the parent changed
            _parentForm = this.FindForm();

            if (_parentForm != null)
            {
                _parentForm.ResizeBegin += ParentForm_ResizeBegin;
                _parentForm.ResizeEnd += ParentForm_ResizeEnd;
            }
        }


        /// <summary>
        /// Suspends layout when resizing begins to suppress <see cref="OnLayout(LayoutEventArgs)"/> calls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParentForm_ResizeBegin(object sender, EventArgs e)
        {
            this.SuspendLayout();
        }

        /// <summary>
        /// Resumes layout when resizing ends to trigger an <see cref="OnLayout(LayoutEventArgs)"/> call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParentForm_ResizeEnd(object sender, EventArgs e)
        {
            this.ResumeLayout(true);
        }

        /// <summary>
        /// Prevent <see cref="OnLayout(LayoutEventArgs)"/> calls from calling <see cref="UpdateDeintPlots"/> before the form is loaded.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.BeginInvoke((Action)(() =>
            {
                _readyForLayout = true;
            }));

        }

    }

    /// <summary>
    /// Custom implementation of <see cref="IPointList"/> to allow for efficient updating of the points
    /// and maintaining a running min/max for X and Y.
    /// </summary>
    internal sealed class CircularBufferPointList : IPointList, IEnumerable<PointPair>
    {
        private double _minX = double.PositiveInfinity;
        private double _maxX = double.NegativeInfinity;
        private double _minY = double.PositiveInfinity;
        private double _maxY = double.NegativeInfinity;
        private readonly double[] _x;
        private readonly double[] _y;
        private int _head;
        private int _count;
        private readonly int _capacity;

        /// <summary>
        /// Specify the capacity and initialize the
        /// array of x and y values.
        /// </summary>
        /// <param name="capacity">Capacity of buffer.</param>
        public CircularBufferPointList(int capacity)
        {
            _capacity = capacity;
            _x = new double[capacity];
            _y = new double[capacity];

            // Mark unused entries
            for (int i = 0; i < capacity; i++)
                _x[i] = double.NaN;
        }

        public int Count => _count;
        public double MinX => _minX;
        public double MaxX => _maxX;
        public double MinY => _minY;
        public double MaxY => _maxY;

        /// <summary>
        /// Add a point in circular fashion. Keeping track of the min/max x value
        /// and determining when a recalculation is required for the min/max y value.
        /// </summary>
        public void Add(double x, double y)
        {
            bool needRecalc = false;
            int idx = _head;
            double oldY = _y[idx];

            // If buffer is full, check if overwritten Y affects min/max
            if (_count == _capacity && (oldY == _minY || oldY == _maxY))
                needRecalc = true;

            // Overwrite the point
            _x[idx] = x;
            _y[idx] = y;

            // Advance head
            _head = (idx + 1) % _capacity;

            // Increase count if buffer not full yet
            if (_count < _capacity)
                _count++;

            // Incremental update of Y min/max
            if (needRecalc)
                RecalculateMinMax(); // scans buffer only when overwritten extreme
            else
            {
                if (y < _minY) _minY = y;
                if (y > _maxY) _maxY = y;
            }

            if (_count > 0)
            {
                int oldestIndex = (_head - _count + _capacity) % _capacity;
                _minX = _x[oldestIndex];       // oldest timestamp
                _maxX = _x[(_head - 1 + _capacity) % _capacity]; // newest timestamp
            }
        }

        /// <summary>
        /// Recalculates the min/max y value
        /// </summary>
        private void RecalculateMinMax()
        {
            _minY = double.PositiveInfinity;
            _maxY = double.NegativeInfinity;

            int start = (_head - _count + _capacity) % _capacity;
            for (int i = 0; i < _count; i++)
            {
                int idx = (start + i) % _capacity;
                double y = _y[idx];
                if (!double.IsNaN(y))
                {
                    if (y < _minY) _minY = y;
                    if (y > _maxY) _maxY = y;
                }
            }
        }

        /// <summary>
        /// Return the point in logical index order.
        /// </summary>
        public PointPair this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                int start = (_head - _count + _capacity) % _capacity;
                int realIndex = (start + index) % _capacity;

                return new PointPair(_x[realIndex], _y[realIndex]);
            }
        }



        /// <summary>
        /// <see cref="PointPair"/> implementation of <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="PointPair"/>.</returns>
        public IEnumerator<PointPair> GetEnumerator()
        {
            if (_count == 0)
                yield break;

            int start = (_head - _count + _capacity) % _capacity;

            for (int i = 0; i < _count; i++)
            {
                int idx = (start + i) % _capacity;

                double x = _x[idx];
                if (!double.IsNaN(x))
                    yield return new PointPair(x, _y[idx]);
            }
        }

        /// <summary>
        /// Non-generic IEnumerable (required by ZedGraph)
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// ICloneable implementation (required by IPointList)
        /// Deep clone of underlying buffers
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var clone = new CircularBufferPointList(_capacity)
            {
                _head = this._head,
                _count = this._count
            };

            Array.Copy(this._x, clone._x, _capacity);
            Array.Copy(this._y, clone._y, _capacity);

            return clone;
        }
    }

}
