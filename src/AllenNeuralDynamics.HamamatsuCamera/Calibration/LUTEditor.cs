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

namespace HamamatsuCamera.Calibration
{
    public partial class LUTEditor : UserControl
    {
        #region Variables

        private ZedGraph.PointPairList Points = new ZedGraph.PointPairList();
        public Dictionary<int, int> LookupTable;
        public Dictionary<int, int> PointsOfInterest;

        private RectangleF ChartRect;

        private KeyValuePair SelectedPair;

        public event EventHandler LUTChanged;
        private const int MetaDataOffset = 3;           // Number of columns of MetaData in the output .csv file
        private const string ListSeparator = ",";       // List separator for writing to different columns of a .csv file

        #endregion

        #region Initialization

        public LUTEditor()
        {
            try
            {
                InitializeComponent();
                InitializeTable();
                InitializePlot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: LUTEditor\nMessage: {ex.Message}");
            }
        }

        private void InitializeTable()
        {
            try
            {
                // Initialize User Points dictionary
                PointsOfInterest = new Dictionary<int, int>
                {
                    [0] = 0,
                    [ushort.MaxValue] = ushort.MaxValue
                };
                POI_Table.Controls.Add(new KeyValuePair("Input", "Output"));
                POI_Table.Controls.Add(new KeyValuePair(0, 0));
                POI_Table.Controls.Add(new KeyValuePair(ushort.MaxValue, ushort.MaxValue));
                POI_Table.Controls.Add(new KeyValuePair());
                var pairs = POI_Table.Controls.OfType<KeyValuePair>();
                if (pairs.Any())
                {
                    foreach (var pair in pairs)
                    {
                        pair.KeyChanged += KeyChanged;
                        pair.ValueChanged += ValueChanged;
                        pair.RowSelected += RowSelected;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: InitializeTable\nMessage: {ex.Message}");
            }
        }

        private void InitializePlot()
        {
            // Initialize LUT Dictionary
            LookupTable = new Dictionary<int, int>();

            // ZedGraph
            LUT_ZedGraph.MasterPane.Title.Text = "Lookup Table";

            // X-Axis
            LUT_ZedGraph.GraphPane.Title.Text = "Lookup Table";
            LUT_ZedGraph.GraphPane.XAxis.Scale.BaseTic = 0;
            LUT_ZedGraph.GraphPane.XAxis.Scale.MajorStep = ushort.MaxValue / 10;
            LUT_ZedGraph.GraphPane.XAxis.Scale.MinorStep = ushort.MaxValue / 100;
            LUT_ZedGraph.GraphPane.XAxis.Scale.Min = 0;
            LUT_ZedGraph.GraphPane.XAxis.Scale.Max = ushort.MaxValue;
            LUT_ZedGraph.GraphPane.XAxis.Title.Text = "Input Pixel Value";

            // Y-Axis
            LUT_ZedGraph.GraphPane.YAxis.Scale.BaseTic = 0;
            LUT_ZedGraph.GraphPane.YAxis.Scale.MajorStep = ushort.MaxValue / 10;
            LUT_ZedGraph.GraphPane.YAxis.Scale.MinorStep = ushort.MaxValue / 100;
            LUT_ZedGraph.GraphPane.YAxis.Scale.Min = 0;
            LUT_ZedGraph.GraphPane.YAxis.Scale.Max = ushort.MaxValue;
            LUT_ZedGraph.GraphPane.YAxis.Title.Text = "Output Pixel Value";


            LookupTable = new Dictionary<int, int>();
            for (int i = 0; i <= ushort.MaxValue; i++)
            {
                LookupTable[i] = i;
                Points.Add(i, i);
            }

            LUT_ZedGraph.GraphPane.CurveList.Clear();
            LUT_ZedGraph.GraphPane.AddCurve("LUT", Points, Color.Black, ZedGraph.SymbolType.None);
        }


        #endregion

        #region Event Handling

        private void Remove_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if(SelectedPair != null && SelectedPair.Key != 0 && SelectedPair.Key != ushort.MaxValue)
                {
                    PointsOfInterest.Remove(SelectedPair.Key);
                    POI_Table.Controls.Remove(SelectedPair);
                    SelectedPair = null;
                    UpdateLUT();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: Remove_Button_Click\nMessage: {ex.Message}");
            }
        }

        internal void LoadLUT(Dictionary<int, int> pointsOfInterest)
        {
            try
            {
                if(pointsOfInterest != null)
                {
                    PointsOfInterest = pointsOfInterest;

                    // Update Table
                    foreach(var key in PointsOfInterest.Keys)
                    {
                        if (key != 0 && key != ushort.MaxValue)
                        {

                            var newPair = new KeyValuePair(key, PointsOfInterest[key]);
                            newPair.KeyChanged += KeyChanged;
                            newPair.ValueChanged += ValueChanged;
                            newPair.RowSelected += RowSelected;
                            POI_Table.Controls.Add(newPair);
                        }
                    }

                    var orderedPairs = POI_Table.Controls.OfType<KeyValuePair>().OrderBy(row => row.Key).ToArray();
                    POI_Table.Controls.Clear();
                    POI_Table.Controls.AddRange(orderedPairs);

                    UpdateLUT();

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: LoadLUT\nMessage: {ex.Message}");
            }
        }

        private void RowSelected(object sender, EventArgs e)
        {
            try
            {
                if (sender is KeyValuePair)
                {
                    var pairs = POI_Table.Controls.OfType<KeyValuePair>();
                    SelectedPair = (KeyValuePair)sender;
                    foreach (var pair in pairs)
                    {
                        pair.BackColor = pair.Equals(SelectedPair) ? Color.Aqua : Color.WhiteSmoke;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ValueChanged\nMessage: {ex.Message}");
            }
        }
        private void ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if(sender is KeyValuePair)
                {
                    var pair = (KeyValuePair)sender;
                    PointsOfInterest[pair.Key] = pair.Value;
                    if (pair.UpdateValue())
                    {
                        var newPair = new KeyValuePair();
                        newPair.KeyChanged += KeyChanged;
                        newPair.ValueChanged += ValueChanged;
                        newPair.RowSelected += RowSelected;
                        POI_Table.Controls.Add(newPair);
                    }

                    // TODO: Update LookupTable
                    UpdateLUT();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ValueChanged\nMessage: {ex.Message}");
            }
        }

        private void KeyChanged(object sender, EventArgs e)
        {
            try
            {
                if(sender is KeyValuePair)
                {
                    var pair = (KeyValuePair)sender;
                    var isUnique = !PointsOfInterest.ContainsKey(pair.Key);
                    if (isUnique)
                    {
                        var orderedPairs = POI_Table.Controls.OfType<KeyValuePair>().OrderBy(row => row.Key).ToArray();
                        POI_Table.Controls.Clear();
                        POI_Table.Controls.AddRange(orderedPairs);
                    }
                    else
                    {
                        // TODO: Revert state of pair

                    }

                    pair.UpdateKey(isUnique);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: KeyChanged\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Helper Functions

        private void UpdateLUT()
        {
            try
            {
                // TODO: Linear regression between each Point of interest
                var pairs = POI_Table.Controls.OfType<KeyValuePair>().Where(pair => pair.Key >= 0 && pair.Key <= ushort.MaxValue).OrderBy(pair => pair.Key);
                for(int i = 0; i < pairs.Count() - 1; i++)
                {
                    var former = pairs.ElementAt(i);
                    var latter = pairs.ElementAt(i + 1);
                    var dx = latter.Key - former.Key;
                    var dy = latter.Value - former.Value;
                    var m = (double)dy / (double)dx;
                    var b = former.Value - m * former.Key;

                    for(int j = former.Key; j <= latter.Key; j++)
                    {
                        var value = (int)Math.Round(m * j + b);
                        Points[j] = new ZedGraph.PointPair(j, value);
                        LookupTable[j] = value;
                    }
                }
                if (LUTChanged != null)
                    LUTChanged.Invoke(this, EventArgs.Empty);
                LUT_ZedGraph.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UpdateLUT\nMessage: {ex.Message}");
            }

        }

        #endregion

        private void Save_Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.FileName = "LookupTable.csv";
                    saveFileDialog.Filter = "CSV files|*.csv|All files|*.*";
                    var result = saveFileDialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {

                        // Create .csv writer
                        using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.ASCII))
                        {
                            var columns = new List<string>(MetaDataOffset);
                            columns.Add("POI Flag");
                            columns.Add("Input Pixel");
                            columns.Add("Output Pixel");

                            var header = string.Join(ListSeparator, columns);
                            writer.WriteLine(header);

                            foreach(var key in LookupTable.Keys)
                            {
                                columns.Clear();
                                columns.Add(PointsOfInterest.ContainsKey(key) ? "1" : "0");
                                columns.Add(key.ToString());
                                columns.Add(LookupTable[key].ToString());
                                var row = string.Join(ListSeparator, columns);
                                writer.WriteLine(row);
                            }

                            writer.Close();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: Save_Button_Click\nMessage: {ex.Message}");
            }
        }

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
                        using (var reader =new StreamReader(openFileDialog.FileName))
                        {
                            string line;
                            var numCols = 0;
                            while((line = reader.ReadLine()) != null)
                            {
                                var values = line.Split(',');
                                if (numCols == 0)
                                    numCols = values.Length;
                                if(numCols == 2)
                                {
                                    if(int.TryParse(values[0], out int key) && int.TryParse(values[1], out int value))
                                        PointsOfInterest[key] = value;
                                }
                                else if(numCols == 3)
                                {
                                    if (int.TryParse(values[0], out int POIFlag) && int.TryParse(values[1], out int key) && int.TryParse(values[2], out int value))
                                    {
                                        if(POIFlag == 1)
                                            PointsOfInterest[key] = value;
                                    }
                                }
                            }
                        }
                    }
                }
                LoadLUT(PointsOfInterest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Load_Button_Click\nMessage: {ex.Message}");
            }
        }
    }
}
