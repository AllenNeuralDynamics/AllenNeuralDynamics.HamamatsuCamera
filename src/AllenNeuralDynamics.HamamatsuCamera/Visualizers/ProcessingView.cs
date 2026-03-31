using AllenNeuralDynamics.HamamatsuCamera.Models;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    /// <summary>
    /// The visualizer used for the <see cref="Processing"/> node. Updates the <see cref="VisualizerRendererControl"/>
    /// with the latest <see cref="IplImage"/> and updates all of the <see cref="RegionPlot"/> with the buffer of metadata
    /// at 30Hz.
    /// </summary>
    public partial class ProcessingView : UserControl
    {
        private const double _maxImageScale = 50.0;
        private const double _minImageScale = 1.0;
        private const double _scaleIncrement = 5.0;
        private const float GraphAspect = 400.0F / 240.0F;      // Aspect ratio of the plots
        private List<RegionPlot> RegionPlots;
        private bool _isInitialized;
        private bool _isDeinterleaved;
        private int _imageIndex;
        private double _imageScale = 11.0;
        private Form _parentForm;

        public ProcessingView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the <see cref="Visualizers.VisualizerRendererControl"/> with
        /// the latest <see cref="IplImage"/>
        /// </summary>
        /// <param name="image"></param>
        internal void TryUpdateImage(IplImage image)
        {
            try
            {
                VisualizerRendererControl.ProcessImage(image, _imageScale);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the <see cref="Visualizers.VisualizerRendererControl"/> from an
        /// array of <see cref="IplImage"/>, changing the index every call.
        /// This way it loops through each interleaved channel while still only
        /// updating at 30Hz.
        /// </summary>
        /// <param name="images"></param>
        internal void TryUpdateImageBundle(IplImage[] images)
        {
            try
            {
                if (_imageIndex < images.Length)
                    VisualizerRendererControl.ProcessImage(images[_imageIndex], _imageScale);
                _imageIndex = (_imageIndex + 1) % images.Length;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the <see cref="RegionPlot"/> for each region based on batched
        /// <see cref="VisualizerData"/>.
        /// </summary>
        /// <param name="batch">Batch of <see cref="VisualizerData"/></param>
        internal void TryUpdateRegionDataBatch(IList<VisualizerData> batch)
        {
            try
            {
                UpdateRegionDataBatch(batch);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the <see cref="RegionPlot"/> for each region based on batched <see cref="FrameBundle"/>
        /// metadata. This takes the form of a batch of <see cref="FrameData"/> arrays.
        /// </summary>
        /// <param name="batch">Batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays.</param>
        internal void TryUpdateRegionDataBundleBatch(IList<FrameData[]> batch)
        {
            try
            {
                UpdateRegionDataBundleBatch(batch);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Specify the Row/Col count and styles based on the number of regions of interest.
        /// </summary>
        /// <param name="numRegions">Number of regions of interest.</param>
        private void FormatTable(int numRegions)
        {
            Tuple<int, int> matrix = FindMatrix(numRegions);      // Find target matrix

            // Ensure Row Count and Styles
            Plots_Table.RowCount = matrix.Item1;
            Plots_Table.RowStyles.Clear();
            for (int i = 0; i < Plots_Table.RowCount; i++)
            {
                Plots_Table.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f / Plots_Table.RowCount));
            }

            // Ensure Column Count and Styles
            Plots_Table.ColumnCount = matrix.Item2;
            Plots_Table.ColumnStyles.Clear();
            for (int i = 0; i < Plots_Table.ColumnCount; i++)
            {
                Plots_Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / Plots_Table.RowCount));
            }
        }

        /// <summary>
        /// Updates the <see cref="RegionPlot"/> for each region based on batched
        /// <see cref="VisualizerData"/>. On the first batch, initialize the plots table.
        /// Update the curves and FPS display.
        /// </summary>
        /// <param name="batch">Batch of <see cref="VisualizerData"/></param>
        private void UpdateRegionDataBatch(IList<VisualizerData> batch)
        {
            if (!_isInitialized)
                InitializePlotsTable(batch);

            UpdateCurves(batch);
            UpdateFPS(batch);
        }

        /// <summary>
        /// Updates the <see cref="RegionPlot"/> for each region based on batched <see cref="FrameBundle"/>
        /// metadata. This takes the form of a batch of <see cref="FrameData"/> arrays.
        /// On the first batch, initialize the plots table.
        /// Update the curves and FPS display.
        /// </summary>
        /// <param name="batch">Batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays</param>
        private void UpdateRegionDataBundleBatch(IList<FrameData[]> batch)
        {
            if (!_isInitialized)
                InitializePlotsTable(batch);

            UpdateCurves(batch);
            UpdateFPS(batch);
        }

        /// <summary>
        /// Initialize Plots table based on the first batch of <see cref="VisualizerData"/>.
        /// Count the number of regions and signals, calculate the capacity,
        /// and update the UI.
        /// </summary>
        /// <param name="batch">Batch of <see cref="VisualizerData"/></param>
        private void InitializePlotsTable(IList<VisualizerData> batch)
        {
            var numRegions = batch[0].RegionData.Length;
            var numSignals = batch[0].DeinterleaveCount;
            var capacity = Math.Max(batch.Count * 30 * 5, 300); // Targeting a 5 sec window by default
            CapVal_Label.Text = capacity.ToString();
            ROIVal_Label.Text = numRegions.ToString();
            FormatTable(numRegions);
            AddPlots(numRegions, numSignals, capacity);
            _isInitialized = true;
        }

        /// <summary>
        /// Initialize Plots table based on the first batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays.
        /// Count the number of regions and signals, calculate the capacity,
        /// and update the UI.
        /// </summary>
        /// <param name="batch">Batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays</param>
        private void InitializePlotsTable(IList<FrameData[]> batch)
        {
            var numRegions = batch[0][0].RegionData.Length;
            var numSignals = batch[0][0].DeinterleaveCount;
            var capacity = Math.Max(batch.Count * batch[0].Length * 30 * 5, 300); // Targeting a 5 sec window by default
            CapVal_Label.Text = capacity.ToString();
            ROIVal_Label.Text = numRegions.ToString();
            FormatTable(numRegions);
            AddPlots(numRegions, numSignals, capacity);
            _isInitialized = true;
        }

        /// <summary>
        /// Update the FPS based on a batch of <see cref="VisualizerData"/>.
        /// </summary>
        /// <param name="batch">Batch of <see cref="VisualizerData"/></param>
        private void UpdateFPS(IList<VisualizerData> batch)
        {
            var df = batch[batch.Count - 1].FrameCounter - batch[0].FrameCounter;
            var dt = batch[batch.Count - 1].Timestamp - batch[0].Timestamp;
            if (dt > 0)
            {
                var fps = df / dt;
                FPSVal_Label.Text = fps.ToString("0.##") + " Hz";
            }
        }

        /// <summary>
        /// Update the FPS based on a batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays.
        /// </summary>
        /// <param name="batch">Batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays</param>
        private void UpdateFPS(IList<FrameData[]> batch)
        {
            var lastbatch = batch[batch.Count - 1];
            var df = lastbatch[lastbatch.Length - 1].FrameCounter - batch[0][0].FrameCounter;
            var dt = lastbatch[lastbatch.Length - 1].Timestamp - batch[0][0].Timestamp;
            if (dt > 0)
            {
                var fps = df / dt;
                FPSVal_Label.Text = fps.ToString("0.##") + " Hz";
            }
        }

        /// <summary>
        /// Find the number of rows and columns that would maximize the display area of
        /// the plots based on the number of regions of interest, the area
        /// of the display surface, and the plot aspect ratio.
        /// </summary>
        /// <param name="numRegions"></param>
        /// <returns>Item1 = Number of Rows, Item2 = Number of Columns</returns>
        private Tuple<int, int> FindMatrix(int numRegions)
        {
            if (numRegions == 1)
                return Tuple.Create(1, 1);
            float GraphListAspect = Plots_Table.Width / (float)Plots_Table.Height;
            int totVisableROIs = numRegions;

            int bestMatrix = 1;
            int Rows;
            int Cols;
            float minAspectDiff = float.MaxValue;
            int lastMatrix = (int)Math.Ceiling(Math.Sqrt((float)totVisableROIs));

            if (GraphListAspect >= 1.0)
            {
                for (int l = 1; l <= lastMatrix; l++)
                {
                    float actualGraphListAspect = (float)Math.Ceiling(totVisableROIs / (float)l) / l * GraphAspect;
                    float aspectDiff = Math.Abs(GraphListAspect - actualGraphListAspect);

                    if (aspectDiff < minAspectDiff)
                    {
                        bestMatrix = l;
                        minAspectDiff = aspectDiff;
                    }
                }

                Cols = (int)Math.Ceiling(totVisableROIs / (float)bestMatrix);
                Rows = bestMatrix;
            }
            else
            {
                for (int l = 1; l <= lastMatrix; l++)
                {
                    float actualGraphListAspect = l / (float)Math.Ceiling(totVisableROIs / (float)l) * GraphAspect;
                    float aspectDiff = Math.Abs(GraphListAspect - actualGraphListAspect);

                    if (aspectDiff < minAspectDiff)
                    {
                        bestMatrix = l;
                        minAspectDiff = aspectDiff;
                    }
                }
                Rows = (int)Math.Ceiling(totVisableROIs / (float)bestMatrix);
                Cols = bestMatrix;
            }

            return Tuple.Create(Rows, Cols);
        }

        /// <summary>
        /// Add <see cref="RegionPlot"/> for each region of interest.
        /// </summary>
        /// <param name="numRegions">Number of regions of interest</param>
        /// <param name="numSignals">Number of signals per region of interest</param>
        /// <param name="capacity">Capacity of the interleaved plot.</param>
        private void AddPlots(int numRegions, int numSignals, int capacity)
        {
            RegionPlots = new List<RegionPlot>();
            for (int i = 0; i < numRegions; i++)
            {
                RegionPlots.Add(new RegionPlot(i, numSignals, capacity));
                Plots_Table.Controls.Add(RegionPlots[i]);
            }
        }

        /// <summary>
        /// Update the curves for each <see cref="RegionPlot"/> based
        /// on a new batch of <see cref="VisualizerData"/>.
        /// </summary>
        /// <param name="batch">Batch of <see cref="VisualizerData"/></param>
        private void UpdateCurves(IList<VisualizerData> batch)
        {
            foreach (var regionPlot in RegionPlots)
            {
                regionPlot.AddFrames(batch);
                regionPlot.Invalidate();
            }
        }

        /// <summary>
        /// Update the curves for each <see cref="RegionPlot"/> based on
        /// a new batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays.
        /// </summary>
        /// <param name="batch">Batch of <see cref="FrameBundle"/> metadata of the form <see cref="FrameData"/> arrays</param>
        private void UpdateCurves(IList<FrameData[]> batch)
        {
            foreach (var regionPlot in RegionPlots)
            {
                regionPlot.AddFrames(batch);
                regionPlot.Invalidate();
            }
        }

        /// <summary>
        /// Deinterleave button click handler. Updates the button's state
        /// and updates the layout of each <see cref="RegionPlot"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Deinterleave_Button_Click(object sender, EventArgs e)
        {
            // Update the text of the button
            if (_isDeinterleaved)
                Deinterleave_Button.Text = "Deinterleave";
            else
                Deinterleave_Button.Text = "Interleave";

            // Toggle flag
            _isDeinterleaved = !_isDeinterleaved;

            // For each region plot

            for (int i = 0; i < RegionPlots.Count; i++)
            {
                // Clear all plots from ZedGraphControl
                RegionPlots[i].SuspendLayout();

                RegionPlots[i].UpdateLayout(_isDeinterleaved);

                RegionPlots[i].Invalidate();
                RegionPlots[i].ResumeLayout();
            }
        }

        private void ParentForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.PageUp && _imageScale < _maxImageScale) _imageScale = (byte)Math.Min(_imageScale + _scaleIncrement, _maxImageScale);
            else if (e.KeyCode == Keys.PageDown && _imageScale > _minImageScale) _imageScale = (byte)Math.Max(_imageScale - _scaleIncrement, _minImageScale);
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
                _parentForm.KeyDown -= ParentForm_KeyDown;
                _parentForm = null;
            }

            // Try to find the form after the parent changed
            _parentForm = this.FindForm();

            if (_parentForm != null)
            {
                _parentForm.KeyDown += ParentForm_KeyDown;
            }
        }
    }
}
