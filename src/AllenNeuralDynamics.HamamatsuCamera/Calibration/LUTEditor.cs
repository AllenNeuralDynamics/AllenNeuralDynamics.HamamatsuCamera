using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    /// <summary>
    /// Displays and gives user control to configure the lookup table.
    /// </summary>
    public partial class LUTEditor : UserControl
    {
        #region Variables

        private readonly LookupTable _lookupTable;
        private readonly PointPairList _plotPoints;
        private Dictionary<ushort, ushort> _pointsOfInterest;
        private KeyValuePair SelectedPair;

        public event EventHandler LUTChanged;
        private const int MetaDataOffset = 3;           // Number of columns of MetaData in the output .csv file
        private const string ListSeparator = ",";       // List separator for writing to different columns of a .csv file

        internal LookupTable LookupTable => _lookupTable;
        public Dictionary<ushort, ushort> PointsOfInterest => _pointsOfInterest;
        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the component, <see cref="LookupTable"/>, <see cref="PointsOfInterest"/>, and plot.
        /// </summary>
        public LUTEditor()
        {
            try
            {
                InitializeComponent();
                _lookupTable = new LookupTable();
                _plotPoints = new PointPairList();
                InitializePlot();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Initializes the LUT plot, a <see cref="ZedGraphControl"/>.
        /// </summary>
        private void InitializePlot()
        {
            // ZedGraph
            LUT_ZedGraph.MasterPane.Title.Text = "Lookup Table";

            // X-Axis
            LUT_ZedGraph.GraphPane.Title.Text = "Lookup Table";
            LUT_ZedGraph.GraphPane.XAxis.Scale.BaseTic = 0;
            LUT_ZedGraph.GraphPane.XAxis.Scale.MajorStep = 4096;
            LUT_ZedGraph.GraphPane.XAxis.Scale.MinorStep = 1024;
            LUT_ZedGraph.GraphPane.XAxis.Scale.Min = 0;
            LUT_ZedGraph.GraphPane.XAxis.Scale.Max = 65536;
            LUT_ZedGraph.GraphPane.XAxis.Title.Text = "Input Pixel Value";

            // Y-Axis
            LUT_ZedGraph.GraphPane.YAxis.Scale.BaseTic = 0;
            LUT_ZedGraph.GraphPane.YAxis.Scale.MajorStep = 4096;
            LUT_ZedGraph.GraphPane.YAxis.Scale.MinorStep = 1024;
            LUT_ZedGraph.GraphPane.YAxis.Scale.Min = 0;
            LUT_ZedGraph.GraphPane.YAxis.Scale.Max = 65536;
            LUT_ZedGraph.GraphPane.YAxis.Title.Text = "Output Pixel Value";

            LUT_ZedGraph.GraphPane.CurveList.Clear();
            for (int i = 0; i <= ushort.MaxValue; i++)
                _plotPoints.Add(i, i);
            LUT_ZedGraph.GraphPane.AddCurve("LUT", _plotPoints, Color.Black, SymbolType.None);
        }

        /// <summary>
        /// Load the LUT from points of interest stored in a <see cref="Dictionary{TKey, TValue}"/>
        /// that maps input pixel values <see cref="ushort"/> to output pixel values <see cref="ushort"/>.
        /// Stores the points of interest, creates <see cref="KeyValuePair"/> for each point of interest
        /// (as well as an empty <see cref="KeyValuePair"/> for adding new points of interest). Updates the
        /// <see cref="TableLayoutPanel"/> with these <see cref="KeyValuePair"/>.
        /// </summary>
        /// <param name="pointsOfInterest"></param>
        internal void LoadLUT(Dictionary<ushort, ushort> pointsOfInterest)
        {
            try
            {
                if (pointsOfInterest == null)
                    _pointsOfInterest = new Dictionary<ushort, ushort>()
                    {
                        [0] = 0,
                        [ushort.MaxValue] = ushort.MaxValue
                    };
                else
                    _pointsOfInterest = pointsOfInterest;

                var pairs = new List<KeyValuePair>();
                foreach (var pair in _pointsOfInterest)
                {
                    var newPair = new KeyValuePair(pair);
                    newPair.KeyChanged += KeyChanged;
                    newPair.ValueChanged += ValueChanged;
                    newPair.RowSelected += RowSelected;
                    pairs.Add(newPair);
                }
                var orderedPairs = pairs.OrderBy(pair => pair.Key).ToArray();
                var lastPair = new KeyValuePair();
                lastPair.KeyChanged += KeyChanged;
                lastPair.ValueChanged += ValueChanged;
                lastPair.RowSelected += RowSelected;
                POI_Table.Controls.Clear();
                POI_Table.Controls.Add(new KeyValuePair("Input", "Output"));
                POI_Table.Controls.AddRange(orderedPairs);
                POI_Table.Controls.Add(lastPair);
                UpdateLUT();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }


        #endregion

        #region Event Handling

        /// <summary>
        /// Removes a point of interest from the stored member and UI. Updates the LUT.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Remove_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if(SelectedPair != null && SelectedPair.Key != 0 && SelectedPair.Key != ushort.MaxValue)
                {
                    _pointsOfInterest.Remove(SelectedPair.Key);
                    POI_Table.Controls.Remove(SelectedPair);
                    SelectedPair = null;
                    UpdateLUT();
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }


        /// <summary>
        /// Updates the selected pair and the backcolor of the user controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowSelected(object sender, EventArgs e)
        {
            try
            {
                if (sender is KeyValuePair selectedPair)
                {
                    var pairs = POI_Table.Controls.OfType<KeyValuePair>();
                    SelectedPair = selectedPair;
                    foreach (var pair in pairs)
                    {
                        pair.BackColor = pair.Equals(SelectedPair) ? Color.Aqua : Color.WhiteSmoke;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Value changed event handler. Update the point of interest and LUT.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if(sender is KeyValuePair pair)
                {
                    _pointsOfInterest[pair.Key] = pair.Value;
                    if (pair.UpdateValue())
                    {
                        var newPair = new KeyValuePair();
                        newPair.KeyChanged += KeyChanged;
                        newPair.ValueChanged += ValueChanged;
                        newPair.RowSelected += RowSelected;
                        POI_Table.Controls.Add(newPair);
                    }

                    UpdateLUT();
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Key Changed event handler. Check it is unique,
        /// if so add it to the Points of Interest table and reorder it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyChanged(object sender, EventArgs e)
        {
            try
            {
                if(sender is KeyValuePair pair)
                {
                    var isUnique = !_pointsOfInterest.ContainsKey(pair.Key);
                    if (isUnique)
                    {
                        var orderedPairs = POI_Table.Controls.OfType<KeyValuePair>().OrderBy(row => row.Key).ToArray();
                        POI_Table.Controls.Clear();
                        POI_Table.Controls.AddRange(orderedPairs);
                    }

                    pair.UpdateKey(isUnique);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Linearly interpolate between points of interest to populate the Mono16 lookup table.
        /// Then use the Mono16 lookup table to populate the Mono8 lookup table.
        /// </summary>
        private void UpdateLUT()
        {
            try
            {
                var pairs = _pointsOfInterest.OrderBy(pair => pair.Key);
                for (int i = 0; i < pairs.Count() - 1; i++)
                {
                    var former = pairs.ElementAt(i);
                    var latter = pairs.ElementAt(i + 1);
                    var dx = latter.Key - former.Key;
                    var dy = latter.Value - former.Value;
                    var m = (double)dy / (double)dx;
                    var b = former.Value - m * former.Key;

                    for (var j = (int)former.Key; j <= latter.Key; j++)
                    {
                        var value = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, Math.Round(m * j + b)));
                        _plotPoints[j] = new PointPair(j, value);
                        _lookupTable.Mono16[j] = value;
                    }
                }
                var scale = ushort.MaxValue / byte.MaxValue;
                for (var i = 0; i <= byte.MaxValue; i++)
                    _lookupTable.Mono8[i] = (byte)(_lookupTable.Mono16[i * scale] >> 8);

                LUTChanged?.Invoke(this, EventArgs.Empty);
                LUT_ZedGraph.Invalidate();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }

        }

        #endregion

        /// <summary>
        /// Save button click event handler. Generate a .csv file containing columns for
        /// Point of Interest Flag (is this KeyValuePair a point of interest)
        /// Input Pixel value
        /// Output Pixel value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Button_Click(object sender, EventArgs e)
        {
            try
            {
                using var saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "LookupTable.csv";
                saveFileDialog.Filter = "CSV files|*.csv|All files|*.*";
                var result = saveFileDialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {

                    // Create .csv writer
                    using var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.ASCII);
                    var columns = new List<string>(MetaDataOffset)
                            {
                                "POI Flag",
                                "Input Pixel",
                                "Output Pixel"
                            };

                    var header = string.Join(ListSeparator, columns);
                    writer.WriteLine(header);

                    for (var i = 0; i <= ushort.MaxValue; i++)
                    {
                        columns.Clear();
                        columns.Add(_pointsOfInterest.ContainsKey((ushort)i) ? "1" : "0");
                        columns.Add(i.ToString());
                        columns.Add(_lookupTable.Mono16[i].ToString());
                        var row = string.Join(ListSeparator, columns);
                        writer.WriteLine(row);
                    }

                    writer.Close();
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Load button click handler. Populate the Points of interest based on the
        /// user selected .csv file. Then update the LUT.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "CSV files|*.csv|All files|*.*";
                    var result = openFileDialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        using var reader = new StreamReader(openFileDialog.FileName);
                        string line;
                        var numCols = 0;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var values = line.Split(',');
                            if (numCols == 0)
                                numCols = values.Length;
                            if (numCols == 2)
                            {
                                var keySuccess = ushort.TryParse(values[0], out ushort key);
                                var valueSuccess = ushort.TryParse(values[1], out ushort value);
                                if (keySuccess && valueSuccess)
                                    _pointsOfInterest[key] = value;
                            }
                            else if (numCols == 3)
                            {
                                var pointOfInterestSuccess = ushort.TryParse(values[0], out ushort pointOfInterestFlag);
                                var isPointOfInterest = pointOfInterestFlag == 1;
                                var keySuccess = ushort.TryParse(values[1], out ushort key);
                                var valueSuccess = ushort.TryParse(values[2], out ushort value);
                                if (pointOfInterestSuccess && isPointOfInterest && keySuccess && valueSuccess)
                                    _pointsOfInterest[key] = value;
                            }
                        }
                    }
                }
                LoadLUT(_pointsOfInterest);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }
    }
}
