using HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace HamamatsuCamera.Visualizers
{
    public partial class Component1 : UserControl
    {

        #region Constants

        

        #endregion
        private const float GraphAspect = 400.0F / 240.0F;      // Aspect ratio of the plots
        private const int Capacity = 300;                       // Capacity of the plots

        private List<RegionPlot> RegionPlots;
        private PictureBox ImageView;
        private bool IsInitialized;
        private int NumSignals;
        private bool Deinterleaved = false;
        public Component1()
        {
            InitializeComponent();
            IsInitialized = false;
        }
        internal void UpdateFrame(VisualizerData data)
        {
            if (!IsInitialized)
                InitializeTable(data);

            UpdateCurves(data.CurrentSignal, data.RegionData);

            if(data.Image != null)
            {
                // Update Image
                if (ImageView.Image != null)
                    ImageView.Image.Dispose();
                ImageView.Image = data.Image;

                // Update Axes
                UpdateAxes(data.xMin, data.xMax, data.yMins, data.yMaxes);
                FPSVal_Label.Text = data.FPS.ToString("0.##") + " Hz";
            }
        }
        private void UpdateAxes(double xMin, double xMax, List<double> yMins, List<double> yMaxes)
        {
            for (int i = 0; i < RegionPlots.Count; i++)
            {
                RegionPlots[i].InterleavedPlot.YAxis.Scale.Min = yMins[i];
                RegionPlots[i].InterleavedPlot.YAxis.Scale.Max = yMaxes[i];
                RegionPlots[i].InterleavedPlot.XAxis.Scale.Min = xMin;
                RegionPlots[i].InterleavedPlot.XAxis.Scale.Max = xMax;
                RegionPlots[i].InterleavedPlot.XAxis.Scale.MajorStep = Math.Round(4.0 * (xMax - xMin) / 5.0, MidpointRounding.ToEven) / 4.0;
                RegionPlots[i].InterleavedPlot.XAxis.Scale.BaseTic = 0;
                var ticProps = FindTicProps(yMins[i], yMaxes[i]);
                RegionPlots[i].InterleavedPlot.YAxis.Scale.BaseTic = ticProps.Item1;
                RegionPlots[i].InterleavedPlot.YAxis.Scale.MajorStep = ticProps.Item2;
                var numDigits = ((decimal)ticProps.Item2).ToString().Length;
                if (numDigits > 7)
                {

                    var sigFigs = ticProps.Item3;
                    RegionPlots[i].InterleavedPlot.YAxis.Scale.Format = "E" + sigFigs;
                }

                else
                {
                    var sigfigs = ((decimal)ticProps.Item2).ToString().Length - 2;
                    RegionPlots[i].InterleavedPlot.YAxis.Scale.Format = "N" + sigfigs;
                }

                for (int j = 0; j < NumSignals; j++)
                {
                    RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.Min = yMins[i];
                    RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.Max = yMaxes[i];
                    RegionPlots[i].DeinterleavedPlots[j].XAxis.Scale.Min = xMin;
                    RegionPlots[i].DeinterleavedPlots[j].XAxis.Scale.Max = xMax;
                    RegionPlots[i].DeinterleavedPlots[j].XAxis.Scale.MajorStep = Math.Round(4.0 * (xMax - xMin) / 5.0, MidpointRounding.ToEven) / 4.0;
                    RegionPlots[i].DeinterleavedPlots[j].XAxis.Scale.BaseTic = 0;
                    ticProps = FindTicProps(yMins[i], yMaxes[i]);
                    RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.BaseTic = ticProps.Item1;
                    RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.MajorStep = ticProps.Item2;
                    numDigits = ((decimal)ticProps.Item2).ToString().Length;
                    if (numDigits > 7)
                    {

                        var sigFigs = ticProps.Item3;
                        RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.Format = "E" + sigFigs;
                    }

                    else
                    {
                        var sigfigs = ((decimal)ticProps.Item2).ToString().Length - 2;
                        RegionPlots[i].DeinterleavedPlots[j].YAxis.Scale.Format = "N" + sigfigs;
                    }
                }
                RegionPlots[i].Invalidate();
            }
        }
        private void UpdateCurves(int currentSignal, List<PointPair> regionData)
        {
            for (int i = 0; i < RegionPlots.Count; i++)
            {
                RegionPlots[i].InterleavedPlot.CurveList[0].AddPoint(regionData[i]);
                RegionPlots[i].DeinterleavedPlots[currentSignal].CurveList[0].AddPoint(regionData[i]);
                while (RegionPlots[i].InterleavedPlot.CurveList[0].Points.Count > Capacity)
                    RegionPlots[i].InterleavedPlot.CurveList[0].RemovePoint(0);
                while (RegionPlots[i].DeinterleavedPlots[currentSignal].CurveList[0].Points.Count > Capacity)
                    RegionPlots[i].DeinterleavedPlots[currentSignal].CurveList[0].RemovePoint(0);
            }
        }

        #region Initialization

        private void InitializeTable(VisualizerData data)
        {
            NumSignals = data.DeinterleaveCount;
            // Ensure Plots Table
            FormatTable(data.RegionData.Count);
            AddImageVisualizer();
            AddPlots(data.RegionData.Count);
            ROIVal_Label.Text = data.RegionData.Count.ToString();

            IsInitialized = true;
        }
        private void FormatTable(int numRegions)
        {
            Tuple<int, int> matrix = FindMatrix(numRegions);      // Find target matrix

            // Ensure Row Count and Styles
            Plots_Table.RowCount = matrix.Item1;
            Plots_Table.RowStyles.Clear();
            for (int i = 0; i < Plots_Table.RowCount; i++)
            {
                Plots_Table.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f / (float)Plots_Table.RowCount));
            }

            // Ensure Column Count and Styles
            Plots_Table.ColumnCount = matrix.Item2;
            Plots_Table.ColumnStyles.Clear();
            for (int i = 0; i < Plots_Table.ColumnCount; i++)
            {
                Plots_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / (float)Plots_Table.RowCount));
            }
        }
        private void AddImageVisualizer()
        {
            ImageView = new PictureBox();
            ImageView.Dock = DockStyle.Fill;
            ImageView.SizeMode = PictureBoxSizeMode.Zoom;
            Plots_Table.Controls.Add(ImageView);
        }
        private void AddPlots(int numRegions)
        {
            RegionPlots = new List<RegionPlot>();
            for (int i = 0; i < numRegions; i++)
            {
                RegionPlots.Add(new RegionPlot(i, NumSignals));
                Plots_Table.Controls.Add(RegionPlots[i]);
            }
        }

        #endregion
        /// <summary>
        /// Maximizes the display area of the region plots based on <see cref="Regions"/>,
        /// the <see cref="GraphAspect"/> of the plots, and the current size
        /// of the <see cref="TableLayoutPanel_Plots"/>.
        /// 
        /// Proof available upon request.
        /// </summary>
        /// <returns>New number of Rows and Columns of the <see cref="TableLayoutPanel_Plots"/></returns>
        private Tuple<int, int> FindMatrix(int numRegions)
        {
            float GraphListAspect = (float)Plots_Table.Width / (float)Plots_Table.Height;
            int totVisableROIs = numRegions + 1;

            int bestMatrix = 1;
            int Rows;
            int Cols;
            float minAspectDiff = float.MaxValue;
            int lastMatrix = (int)Math.Ceiling(Math.Sqrt((float)totVisableROIs));

            if (GraphListAspect >= 1.0)
            {
                for (int l = 1; l <= lastMatrix; l++)
                {
                    float actualGraphListAspect = (float)Math.Ceiling((float)totVisableROIs / (float)l) / (float)l * GraphAspect;
                    float aspectDiff = Math.Abs(GraphListAspect - actualGraphListAspect);

                    if (aspectDiff < minAspectDiff)
                    {
                        bestMatrix = l;
                        minAspectDiff = aspectDiff;
                    }
                }

                Cols = (int)Math.Ceiling((float)totVisableROIs / (float)bestMatrix);
                Rows = bestMatrix;
            }
            else
            {
                for (int l = 1; l <= lastMatrix; l++)
                {
                    float actualGraphListAspect = (float)l / (float)Math.Ceiling((float)totVisableROIs / (float)l) * GraphAspect;
                    float aspectDiff = Math.Abs(GraphListAspect - actualGraphListAspect);

                    if (aspectDiff < minAspectDiff)
                    {
                        bestMatrix = l;
                        minAspectDiff = aspectDiff;
                    }
                }
                Rows = (int)Math.Ceiling((float)totVisableROIs / (float)bestMatrix);
                Cols = bestMatrix;
            }

            return Tuple.Create(Rows, Cols);
        }/// <summary>
         /// Helper function for keeping tics of plots spaced well.
         /// </summary>
         /// <param name="min">Minimum Y-value of plot</param>
         /// <param name="max">Maximum Y-value of plot</param>
         /// <returns>Base Tic, Tic step size, and number of significant figures to display on y-axis labels.</returns>
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

        private class RegionPlot : ZedGraphControl
        {
            public int ROI;
            public int NumSignals;
            public GraphPane InterleavedPlot;
            public List<GraphPane> DeinterleavedPlots;
            public RegionPlot(int index, int numSignals)
            {
                ROI = index;
                NumSignals = numSignals;

                // Configure Control and MasterPane
                Configure();

                // Create GraphPanes
                CreatePlots();
            }

            private void Configure()
            {
                Dock = DockStyle.Fill;

                MasterPane.Tag = "Master";
                MasterPane.Title.IsVisible = true;
                MasterPane.Title.Text = "ROI " + ROI;
                MasterPane.Title.FontSpec.Size = 18;
                MasterPane.TitleGap = 0.0f;
                MasterPane.InnerPaneGap = 0;
                MasterPane.Margin.All = 2.5f;
                MasterPane.Border.IsVisible = true;
                MasterPane.IsFontsScaled = true;
                MasterPane.PaneList.Clear();
            }

            private void CreatePlots()
            {
                InterleavedPlot = new GraphPane();
                InterleavedPlot.Tag = 0;
                InterleavedPlot.Title.IsVisible = false;
                InterleavedPlot.YAxisList.Clear();
                InterleavedPlot.AddYAxis("Fluorescence (A.U.)");
                InterleavedPlot.Border.IsVisible = true;

                InterleavedPlot.Margin.All = 0;
                InterleavedPlot.YAxis.Title.Gap = 0;
                InterleavedPlot.XAxis.IsVisible = true;
                InterleavedPlot.XAxis.Scale.IsVisible = true;
                InterleavedPlot.XAxis.Scale.MajorUnit = DateUnit.Second;

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

                var interleavedPoints = new RollingPointPairList(Capacity);
                var interleavedSeries = InterleavedPlot.AddCurve("Data", interleavedPoints, Color.Black, SymbolType.None);
                interleavedSeries.Label.IsVisible = false;
                interleavedSeries.Line.IsAntiAlias = false;
                interleavedSeries.Line.IsOptimizedDraw = true;

                MasterPane.Add(InterleavedPlot);

                DeinterleavedPlots = new List<GraphPane>();
                for (int j = 1; j <= NumSignals; j++)
                {
                    GraphPane graph = new GraphPane();
                    graph.Tag = j;
                    graph.Title.IsVisible = false;
                    graph.YAxisList.Clear();
                    graph.AddYAxis(j.ToString());
                    Color color = GetColor(j);
                    graph.Border.IsVisible = j == 0 ? true : false;

                    graph.Margin.All = 0;
                    graph.YAxis.Color = color;
                    graph.YAxis.MinorTic.Color = color;
                    graph.YAxis.MinorTic.Color = color;
                    graph.YAxis.Scale.FontSpec.FontColor = color;

                    graph.YAxis.Title.FontSpec.FontColor = color;

                    graph.YAxis.Title.Gap = 0;
                    graph.XAxis.IsVisible = true;
                    graph.XAxis.Scale.IsVisible = j % NumSignals == 0;
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
                    graph.XAxis.Title.IsVisible = j % NumSignals == 0;
                    graph.Chart.IsRectAuto = true;
                    graph.IsFontsScaled = true;
                    
                    var points = new RollingPointPairList(Capacity);
                    var series = graph.AddCurve("Data", points, color, SymbolType.None);
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
                else if (NumSignals == 1)
                    return Color.Red;

                double normalizedIndex = (double)(index - 1) / (double)(NumSignals - 1);
                int red = normalizedIndex <= 0.25 ? 255 : normalizedIndex <= 0.5 ? Remap(normalizedIndex, 0.25, 0.5, 255, 0) : 0;
                int green = normalizedIndex <= 0.25 ? Remap(normalizedIndex, 0.0, 0.25, 0, 255) : normalizedIndex <= 0.75 ? 255 : Remap(normalizedIndex, 0.75, 1.0, 255, 0);
                int blue = normalizedIndex <= 0.5 ? 0 : normalizedIndex <= 0.75 ? Remap(normalizedIndex, 0.5, 0.75, 0, 255) : 255;
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

            internal void UpdateLayout(bool deinterleaved)
            {

                MasterPane.PaneList.Clear();
                // If should be interleaved
                if (!deinterleaved)
                {
                    MasterPane.Add(InterleavedPlot);
                    using (Graphics g = CreateGraphics())
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
            /// Updates the sizes of the deinterleaved graphpanes such that 
            /// they are the same size, share the same X-Axis,
            /// and have the same font sizes. 
            /// </summary>
            /// <param name="roiNum">The region index.</param>
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
        }

        private void Deinterleave_Button_Click(object sender, EventArgs e)
        {// Update the text of the button
            if (Deinterleaved)
                Deinterleave_Button.Text = "Deinterleave";
            else
                Deinterleave_Button.Text = "Interleave";

            // Toggle flag
            Deinterleaved = !Deinterleaved;

            // For each region plot

            for (int i = 0; i < RegionPlots.Count; i++)
            {
                // Clear all plots from ZedGraphControl
                RegionPlots[i].SuspendLayout();

                RegionPlots[i].UpdateLayout(Deinterleaved);
                
                RegionPlots[i].Invalidate();
                RegionPlots[i].ResumeLayout();
            }
        }
    }
}
