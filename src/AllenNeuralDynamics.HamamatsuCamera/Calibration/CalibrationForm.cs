using AllenNeuralDynamics.HamamatsuCamera.API;
using AllenNeuralDynamics.HamamatsuCamera.Exceptions;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using Bonsai.Reactive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Timer = System.Windows.Forms.Timer;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    /// <summary>
    /// Form used to configure the <see cref="_instance"/> node and the attached monochromatic Hamamatsu Camera.
    /// </summary>
    public partial class CalibrationForm : Form
    {
        #region Private Members

        // Camera Setting Strings
        private const string _subarrStr = "SUBARRAY";
        private const string _subarrHPosStr = "SUBARRAY HPOS";
        private const string _subarrVPosStr = "SUBARRAY VPOS";
        private const string _subarrHSizeStr = "SUBARRAY HSIZE";
        private const string _subarrVSizeStr = "SUBARRAY VSIZE";
        private const string _subarrModeStr = "SUBARRAY MODE";
        private const string _numPixelsHorzStr = "IMAGE DETECTOR PIXEL NUM HORZ";
        private const string _numPixelsVertStr = "IMAGE DETECTOR PIXEL NUM VERT";
        private const string _binningStr = "BINNING";
        private const string _temperatureStr = "SENSOR TEMPERATURE";
        // Misc. Setting Strings
        private const string _cropModeStr = "Crop Mode";
        private const string _autoStr = "Auto";
        private const string _manualStr = "Manual";
        private readonly Dictionary<int, string> _groupNames = new Dictionary<int, string>()
        {
            { 0         , "Miscellaneous"           },
            { 1         , "Sensor Mode and Speed"   },
            { 2         , "Trigger"                 },
            { 4         , "Feature"                 },
            { 8         , "Output Trigger"          },
            { 128       , "Sensor Cooler"           },
            { 1024      , "Binning and ROI"         },
            { 2048      , "Sensor Mode and Speed"   },
            { 4096      , "ALU"                     },
            { 8192      , "System Information 1"    },
            { 65536     , "Synchronous Timing"      },
            { 131072    , "System Information 2"    },
            { 262144    , "System Information 3"    },
            { 4194304   , "System Information 4"    },
            { 8388608   , "Master Pulse"            },
            { 33554432  , "Data Reduction"          }
        };
        private readonly Dictionary<string, int> _miscSettingGroups = new Dictionary<string, int>()
        {
            { _cropModeStr  , 1024   }
        };

        private const float ROW_SIZE = 30.0f;
        private readonly C13440 _instance;
        private IDisposable Subscription;
        private IEnumerable<DCAM_PROP_MANAGER> CameraProps;
        private IEnumerable<Control> SettingsHierarchy;
        private readonly CropSettings AutoCrop = new CropSettings();
        private readonly CropSettings ManualCrop = new CropSettings();
        private readonly Timer _temperatureTimer;
        private readonly bool _failedToLoad;
        private Dictionary<int, double> _storedSettings = new Dictionary<int, double>();
        private Dictionary<ushort, ushort> _storedPointsOfInterest = new Dictionary<ushort, ushort>();
        private List<RegionOfInterest> _storedRegions;

        #endregion

        #region Initialization
        /// <summary>
        /// Constructs the form, initializes members,
        /// starts an update timer, and starts the <see cref="C13440"/> subscription.
        /// </summary>
        /// <param name="c13440">Instance of the <see cref="C13440"/> node.</param>
        public CalibrationForm(C13440 c13440)
        {
            try
            {
                InitializeComponent();
                _instance = c13440;
                var success = TryInitAndOpen();
                if (!success)
                {
                    _failedToLoad = true;
                    SplashScreen.CloseSplash();
                    return;
                }
                InitializeMembers();
                _temperatureTimer = new Timer()
                {
                    Interval = 5000
                };
                _temperatureTimer.Tick += TemperatureTimer_Tick;
                Subscription = _instance.Generate().Do(frame => Image_Visualizer.TryUpdateNewFrame(frame)).Subscribe();
                SplashScreen.CloseSplash();
                _temperatureTimer.Start();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
                Close();
            }
        }

        /// <summary>
        /// Stores required references and finishes initializing the UI
        /// </summary>
        private void InitializeMembers()
        {
            TryInitAndOpen();
            CameraProps = _instance.CameraProps;
            // Local storage of settings and points of interest to compare to when the form closes to see if anything changed.
            foreach(var prop in CameraProps)
            {
                var key = prop.m_idProp.getidprop();
                var value = 0.0;
                prop.getvalue(ref value);
                _storedSettings[key] = value;
            }

            _storedPointsOfInterest = new Dictionary<ushort, ushort>(_instance.PointsOfInterest);

            Settings_Panel.Controls.Add(CreateSettingsTable());
            if (_instance.Regions != null)
            {
                _storedRegions = new List<RegionOfInterest>(_instance.Regions);
                Image_Visualizer.Regions = new List<RegionOfInterest>(_instance.Regions);   // Need a clone here to prevent CameraCapture from altering Regions in the UI while we are configuring.
            }
            else
            {
                _storedRegions = new List<RegionOfInterest>();
                Image_Visualizer.Regions = new List<RegionOfInterest>();
            }
            UpdateSubarray();
            Image_Visualizer.RegionsChanged += Image_Visualizer_RegionsChanged;
            LUTControl.LoadLUT(_instance.PointsOfInterest);
            LUTControl.LUTSaved += LUTControl_LUTSaved;
            LUTControl.LUTLoaded += LUTControl_LUTLoaded;
        }

        private bool TryInitAndOpen()
        {

            try
            {
                _instance.Capture.InitAndOpen();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Settings Table Creation

        /// <summary>
        /// Instantiate and configure the settings table. Then,
        /// add groups to the table. Finally get the control hierarchy.
        /// </summary>
        /// <returns>Settings table.</returns>
        private TableLayoutPanel CreateSettingsTable()
        {
            try
            {
                // Instantiate table
                var settingsTable = new TableLayoutPanel();

                // Configure table
                var settingGroups = CameraProps.Select(prop => prop.m_attr.iGroup).Distinct();
                SetDimensions(ref settingsTable, 1, settingGroups.Count(), SizeType.AutoSize);
                settingsTable.BackColor = Color.PeachPuff;
                settingsTable.AutoSize = true;
                settingsTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                settingsTable.Dock = DockStyle.Top;
                settingsTable.Margin = new Padding(0);
                settingsTable.Padding = new Padding(0);

                // Add groups to table
                int row = 0;
                foreach (var group in settingGroups)
                {
                    settingsTable.Controls.Add(CreateGroup(group), 0, row++);
                }

                // Get control hierarchy
                SettingsHierarchy = GetControlHierarchy(settingsTable);

                return settingsTable;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return new TableLayoutPanel();
        }

        /// <summary>
        /// Configures a <see cref="TableLayoutPanel"/> to have the required
        /// columns, rows, and size type.
        /// </summary>
        /// <param name="table"><see cref="TableLayoutPanel"/> to be configured.</param>
        /// <param name="numCols">Number of columns to add to the table.</param>
        /// <param name="numRows">Number of rows to add to the table</param>
        /// <param name="sizeType">Size type of the rows.</param>
        private static void SetDimensions(ref TableLayoutPanel table, int numCols, int numRows, SizeType sizeType)
        {
            try
            {
                table.ColumnCount = numCols;
                table.ColumnStyles.Clear();
                for (int i = 0; i < table.ColumnCount; i++)
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / numCols));


                table.RowCount = numRows;
                table.RowStyles.Clear();
                for (int i = 0; i < table.RowCount; i++)
                {
                    if (sizeType == SizeType.AutoSize)
                        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    else if (sizeType == SizeType.Absolute)
                        table.RowStyles.Add(new RowStyle(SizeType.Absolute, ROW_SIZE));
                    else
                        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f / numRows));
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Create a <see cref="TableLayoutPanel"/> for containing the controls
        /// for a group of camera settings.
        /// </summary>
        /// <param name="group">Group ID</param>
        /// <returns><see cref="TableLayoutPanel"/> for a group of camera settings.</returns>
        private TableLayoutPanel CreateGroup(int group)
        {
            try
            {
                // Get number of rows
                var groupSettings = CameraProps.Where(prop => prop.m_attr.iGroup == group);
                var numRows = 1 + groupSettings.Count();

                var miscSettings = _miscSettingGroups.Where(pair => pair.Value == group);
                if (miscSettings.Any())
                    numRows += miscSettings.Count();

                var groupTable = new TableLayoutPanel();
                SetDimensions(ref groupTable, 1, numRows, SizeType.Absolute);
                groupTable.AutoSize = true;
                groupTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                groupTable.Dock = DockStyle.Fill;
                groupTable.Margin = new Padding(0);
                groupTable.Padding = new Padding(0);

                var row = 0;
                groupTable.Controls.Add(CreateGroupLabel(group), 0, row++);
                if (miscSettings.Any())
                    foreach (var miscSetting in miscSettings)
                        groupTable.Controls.Add(CreateMiscSetting(miscSetting.Key), 0, row++);

                foreach (var groupSetting in groupSettings)
                    groupTable.Controls.Add(CreateGroupSetting(groupSetting), 0, row++);

                return groupTable;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return new TableLayoutPanel();
        }

        /// <summary>
        /// Creates and configures a <see cref="NameValuePair"/> to allow for user control
        /// of the specified camera setting.
        /// </summary>
        /// <param name="groupSetting">Specified camera setting.</param>
        /// <returns><see cref="NameValuePair"/> allowing user control of camera setting.</returns>
        private NameValuePair CreateGroupSetting(DCAM_PROP_MANAGER groupSetting)
        {
            try
            {
                var nameValuePair = new NameValuePair(groupSetting)
                {
                    Padding = new Padding(40, 0, 0, 0),
                    Dock = DockStyle.Fill
                };
                nameValuePair.DataStreamPropChangeRequest += HandleDataStreamPropChangeRequest;
                nameValuePair.EffectiveChangeOccurred += NameValuePair_EffectiveChangeOccured;
                switch (nameValuePair.SettingName)
                {
                    case _subarrHPosStr:
                    case _subarrHSizeStr:
                    case _subarrVPosStr:
                    case _subarrVSizeStr:
                    case _subarrModeStr:
                        if (_instance.CropMode == CropMode.Auto) nameValuePair.Disable(DCAMCAP_STATUS.BUSY);
                        break;
                    case _numPixelsHorzStr:
                        ManualCrop.HSize = nameValuePair.SettingValue;
                        Image_Visualizer.NumPixelsHorizontal = (int)nameValuePair.SettingValue;
                        break;
                    case _numPixelsVertStr:
                        ManualCrop.VSize = nameValuePair.SettingValue;
                        Image_Visualizer.NumPixelsVertical = (int)nameValuePair.SettingValue;
                        break;
                    case _binningStr:
                        Image_Visualizer.Binning = (int)nameValuePair.SettingValue;
                        break;
                }
                return nameValuePair;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return new NameValuePair();
        }

        /// <summary>
        /// Creates <see cref="TableLayoutPanel"/> for providing user control
        /// of non-camera specific settings. Currently, this is only used for
        /// <see cref="CropMode"/> but can be expanded by adding to <see cref="_miscSettingGroups"/>.
        /// </summary>
        /// <param name="key">Misc setting ID.</param>
        /// <returns><see cref="TableLayoutPanel"/> for Misc setting.</returns>
        private TableLayoutPanel CreateMiscSetting(string key)
        {
            try
            {
                var miscSettingTable = new TableLayoutPanel();
                SetDimensions(ref miscSettingTable, 2, 1, SizeType.Percent);
                miscSettingTable.Padding = new Padding(40, 0, 0, 0);
                miscSettingTable.Dock = DockStyle.Fill;

                // Create Name Label
                var nameLabel = new Label
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    Padding = new Padding(0),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                nameLabel.Click += UnClickLabel;


                miscSettingTable.Controls.Add(nameLabel, 0, 0);
                // Create Value Control
                if (key == _cropModeStr)
                {
                    var cropValue = new ComboBox
                    {
                        Tag = _cropModeStr,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Dock = DockStyle.Fill,
                        Font = new Font(Settings_Panel.Font, FontStyle.Regular),
                        Margin = new Padding(3)
                    };
                    cropValue.Items.Add(_autoStr);
                    cropValue.Items.Add(_manualStr);
                    cropValue.SelectedIndex = _instance.CropMode == CropMode.Auto ? 0 : 1;
                    Image_Visualizer.CropMode = _instance.CropMode;
                    cropValue.SelectionChangeCommitted += HandleCropModeChanged;
                    miscSettingTable.Controls.Add(cropValue, 1, 0);
                }
                return miscSettingTable;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return new TableLayoutPanel();
        }

        /// <summary>
        /// Creates a <see cref="Label"/> for a camera setting group.
        /// </summary>
        /// <param name="group">Group ID.</param>
        /// <returns><see cref="Label"/> for a camera setting group.</returns>
        private Label CreateGroupLabel(int group)
        {
            try
            {
                var groupLabel = new Label
                {
                    Name = _groupNames[group],
                    Text = _groupNames[group],
                    Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                groupLabel.Click += UnClickLabel;
                groupLabel.BackColor = Color.SandyBrown;
                groupLabel.Tag = group;

                return groupLabel;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return new Label();
        }


        /// <summary>
        /// Recursively, finds the hierarchy of controls belonging to a root <see cref="Control"/>.
        /// Creates a <see cref="Queue{T}"/> of <see cref="Control"/>, starting with the root <see cref="Control"/>.
        /// While the queue is not empty, remove the next control of the queue, yield returning it.
        /// Then add of that control's children to the queue.
        /// </summary>
        /// <param name="root"><see cref="Control"/> that is the Active Control of the <see cref="PropertyGrid"/>.</param>
        /// <returns></returns>
        private IEnumerable<Control> GetControlHierarchy(Control root)
        {
            // Create a control queue
            var queue = new Queue<Control>();

            // Add the root control to the queue
            queue.Enqueue(root);

            // Recursively find the next child controls while there are controls in the queue.
            do
            {
                // Get the next control in the queue.
                var control = queue.Dequeue();

                // Yield return the next control.
                yield return control;

                // Add each child of the next control to the queue
                foreach (var child in control.Controls.OfType<Control>())
                    queue.Enqueue(child);

            } while (queue.Count > 0);
        }

        #endregion

        #region Settings Table Events

        /// <summary>
        /// Handles <see cref="_temperatureTimer"/> ticks for updating the
        /// current camera temperature in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TemperatureTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var temperatureProp = SettingsHierarchy.OfType<NameValuePair>().First(prop => prop.SettingName == _temperatureStr);
                temperatureProp.RefreshValue();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Handles changes to camera settings that affect the data stream.
        /// These require the camera to be in <see cref="DCAMCAP_STATUS.STABLE"/>,
        /// so the capture is paused and buffer is released, then the setting is changed,
        /// and finally the buffer is reallocated and capture is resumed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleDataStreamPropChangeRequest(object sender, EventArgs e)
        {
            try
            {
                if(sender is NameValuePair nameValuePair)
                {
                    var status = _instance.Capture.GetStatus();
                    if (status == DCAMCAP_STATUS.STABLE)
                    {
                        nameValuePair.UpdateValue();
                        if (nameValuePair.SettingName == _binningStr)
                            Image_Visualizer.UpdateBinning((int)nameValuePair.SettingValue);
                        else if (GetIsSubarray(nameValuePair.Setting.m_idProp))
                            UpdateSubarray();

                    }
                    else
                    {
                        _instance.Capture.PauseAndRelease();
                        nameValuePair.UpdateValue();
                        if (nameValuePair.SettingName == _binningStr)
                            Image_Visualizer.UpdateBinning((int)nameValuePair.SettingValue);
                        else if (GetIsSubarray(nameValuePair.Setting.m_idProp))
                            UpdateSubarray();
                        Image_Visualizer.ResetFrameCount();
                        _instance.Capture.ReallocateAndResume();
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="CropMode"/> property. This requires the
        /// camera to be in <see cref="DCAMCAP_STATUS.STABLE"/>. So the capture is paused
        /// and buffer is released. Then the setting is changed and finally the buffer is
        /// reallocated and capture is resumed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCropModeChanged(object sender, EventArgs e)
        {
            try
            {
                var status = _instance.Capture.GetStatus();
                if (status == DCAMCAP_STATUS.STABLE)
                    UpdateROISettings(IsAutoCrop());
                else
                {
                    _instance.Capture.PauseAndRelease();
                    UpdateROISettings(IsAutoCrop());
                    Image_Visualizer.ResetFrameCount();
                    _instance.Capture.ReallocateAndResume();
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Handles changes to camera settings that effect other settings.
        /// These changes cause other camera settings in the UI to become stale,
        /// so here they are all refreshed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NameValuePair_EffectiveChangeOccured(object sender, EventArgs e)
        {
            try
            {
                var nameValuePairs = SettingsHierarchy.OfType<NameValuePair>().Where(pair => !string.IsNullOrEmpty(pair.SettingName));
                foreach (var nameValuePair in nameValuePairs)
                    nameValuePair.RefreshValue();
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Stores the subarray in the Manual Crop variable and updates the <see cref="ImageVisualizer"/>.
        /// </summary>
        private void UpdateSubarray()
        {
            var subarrayControls = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Contains(_subarrStr));
            foreach (var subarrayControl in subarrayControls)
                subarrayControl.RefreshValue();

            ManualCrop.HPos = subarrayControls.First(ctrl => ctrl.Setting.m_idProp == DCAMIDPROP.SUBARRAYHPOS).SettingValue;
            ManualCrop.HSize = subarrayControls.First(ctrl => ctrl.Setting.m_idProp == DCAMIDPROP.SUBARRAYHSIZE).SettingValue;
            ManualCrop.VPos = subarrayControls.First(ctrl => ctrl.Setting.m_idProp == DCAMIDPROP.SUBARRAYVPOS).SettingValue;
            ManualCrop.VSize = subarrayControls.First(ctrl => ctrl.Setting.m_idProp == DCAMIDPROP.SUBARRAYVSIZE).SettingValue;
            ManualCrop.Mode = subarrayControls.First(ctrl => ctrl.Setting.m_idProp == DCAMIDPROP.SUBARRAYMODE).SettingValue == DCAMPROP.MODE.ON;

            Image_Visualizer.UpdateSubarray(ManualCrop);
        }

        /// <summary>
        /// Updates the region of interest properties based on the <see cref="CropMode"/>.
        /// Turns of the subarray, sets the new crop, updates user access to
        /// subarray properties based on <see cref="CropMode"/>.
        /// </summary>
        /// <param name="auto">Current <see cref="CropMode"/>.</param>
        private void UpdateROISettings(bool auto)
        {
            try
            {
                Image_Visualizer.CropMode = auto ? CropMode.Auto : CropMode.Manual;
                _instance.CropMode = Image_Visualizer.CropMode;

                var status = _instance.Capture.GetStatus();
                var subarrayControls = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Contains(_subarrStr));

                // Disable Subarray Mode
                var modeControl = subarrayControls.First(prop => prop.SettingName.Equals(_subarrModeStr));
                modeControl.SettingValue = DCAMPROP.MODE.OFF;
                modeControl.Setting.setvalue(modeControl.SettingValue);

                // Set New Crop Values
                var newCrop = auto ? AutoCrop.GetCrop() : ManualCrop.GetCrop();

                var cropControls = subarrayControls.Where(prop => !prop.SettingName.Equals(_subarrModeStr));
                for (int i = 0; i < cropControls.Count(); i++)
                {
                    var cropControl = cropControls.ElementAt(i);
                    var newValue = newCrop.ElementAt(i);
                    cropControl.Setting.setgetvalue(ref newValue);
                    cropControl.SettingValue = newValue;
                }

                modeControl.SettingValue = auto ? DCAMPROP.MODE.ON : DCAMPROP.MODE.OFF;
                modeControl.Setting.setvalue(modeControl.SettingValue);

                // Disable user access to subarray settings
                foreach (var subarrayControl in subarrayControls)
                {
                    subarrayControl.RefreshValue();
                    if (auto)
                        subarrayControl.Disable(status);
                    else
                        subarrayControl.Enable(status);
                }

                UpdateSubarray();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Checks <see cref="CropMode"/> specified in the UI.
        /// </summary>
        /// <returns>True for AutoCrop, False for ManualCrop.</returns>
        private bool IsAutoCrop()
        {
            try
            {
                var cropValues = SettingsHierarchy.Where(ctrl => ctrl.Tag is string tag && tag == _cropModeStr);
                if (!cropValues.Any())
                    return false;

                var cropValue = (ComboBox)cropValues.First();
                return (string)cropValue.SelectedItem == _autoStr;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return false;
        }

        /// <summary>
        /// Returns focus to the <see cref="CalibrationForm"/>. Used to commit changes
        /// to camera settings that require a <see cref="TextBox"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnClickLabel(object sender, EventArgs e)
        {
            try
            {
                this.Focus();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Loads all settings from an .xml file.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "XML files|*.xml|All files|*.*";
                    var result = openFileDialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        var settings = new Dictionary<int, double>();
                        bool auto = true;
                        using (var reader = XmlReader.Create(openFileDialog.FileName))
                        {
                            Image_Visualizer.Regions = new List<RegionOfInterest>();
                            while (reader.Read())
                            {
                                if (reader.Name == "Setting" && reader.HasAttributes && reader.AttributeCount == 2)
                                {
                                    settings[int.Parse(reader[0])] = double.Parse(reader[1]);
                                    reader.MoveToElement();
                                }
                                if(reader.Name == "Region" && reader.HasAttributes && reader.AttributeCount == 4)
                                {
                                    Image_Visualizer.Regions.Add(new RegionOfInterest(int.Parse(reader[0]), int.Parse(reader[1]), int.Parse(reader[2]), int.Parse(reader[3])));
                                    reader.MoveToElement();
                                }
                                if(reader.Name == "CropMode" && reader.HasAttributes && reader.AttributeCount == 1)
                                {
                                    auto = reader[0].Equals("Auto");
                                    SettingsHierarchy.Where(ctrl => ctrl.Tag is string tag && tag == _cropModeStr).Cast<ComboBox>().First().SelectedIndex = auto ? 0 : 1;
                                    Image_Visualizer.CropMode = auto ? CropMode.Auto : CropMode.Manual;
                                    _instance.CropMode = Image_Visualizer.CropMode;
                                }
                            }
                        }


                        // Must commit the subarray setting before other camera settings. Otherwise, we cannot achieve higher frame rates.
                        var subarraySettings = settings.Where(pair => GetIsSubarray(pair.Key));
                        var otherSettings = settings.Where(pair => !GetIsSubarray(pair.Key));

                        // Must turn Subarray Mode off before changing subarray
                        CameraProps.First(prop => prop.m_idProp == DCAMIDPROP.SUBARRAYMODE).setvalue(DCAMPROP.MODE.OFF);

                        foreach (var pair in subarraySettings)
                        {
                            if(CameraProps.Any(prop => prop.m_idProp == pair.Key))
                                CameraProps.First(prop => prop.m_idProp == pair.Key).setvalue(pair.Value);
                        }
                        foreach (var pair in otherSettings)
                        {
                            if(CameraProps.Any(prop => prop.m_idProp == pair.Key))
                                CameraProps.First(prop => prop.m_idProp == pair.Key).setvalue(pair.Value);
                        }
                        foreach (var nameValuePair in SettingsHierarchy.OfType<NameValuePair>())
                            nameValuePair.RefreshValue();

                        UpdateSubarray();
                        _instance.SettingsPath = openFileDialog.FileName;
                        _instance.Regions = new List<RegionOfInterest>(Image_Visualizer.Regions);
                        foreach (var prop in CameraProps)
                        {
                            var key = prop.m_idProp.getidprop();
                            var value = 0.0;
                            prop.getvalue(ref value);
                            _storedSettings[key] = value;
                        }
                        _storedRegions = new List<RegionOfInterest>(Image_Visualizer.Regions);
                    }
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Checks if the specified property ID is related to the camera's subarray.
        /// </summary>
        /// <param name="propId">Property ID</param>
        /// <returns>True for subarray related camera settings.</returns>
        private static bool GetIsSubarray(int propId)
        {
            return propId == DCAMIDPROP.SUBARRAYHPOS || propId == DCAMIDPROP.SUBARRAYHSIZE || propId == DCAMIDPROP.SUBARRAYVPOS || propId == DCAMIDPROP.SUBARRAYVSIZE || propId == DCAMIDPROP.SUBARRAYMODE;
        }

        #endregion

        #region Open/Close

        /// <summary>
        /// Closes the form if it failed to load.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_failedToLoad)
            {
                Console.WriteLine("Failed to detect the camera. Please ensure that it is connected and powered on.");
                Close();
            }
        }

        /// <summary>
        /// Stores configured properties to the <see cref="C13440"/> instance and disposes
        /// the temperature timer and subscription.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_failedToLoad) return;
                var cameraSettingsChanged = CameraSettingsChanged();
                var regionsChanged = RegionsChanged();
                var pointsOfInterestChanged = PointsOfInterestChanged();
                if (cameraSettingsChanged || regionsChanged)
                {
                    // Save settings
                    var result = MessageBox.Show("One or more camera settings have been modified.\n\n" +
                        "If you don't want to save, the camera will revert to its previous configuration\n\n" +
                        "Do you want to save your changes?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    switch(result)
                    {
                        case DialogResult.Yes:
                            SaveSettings();
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }

                if (pointsOfInterestChanged)
                {
                    var result = MessageBox.Show("The LUT has been modified.\n\n" +
                        "If you don't want to save, the LUT will revert to its previous configuration\n\n" +
                        "Do you want to save your changes?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    switch (result)
                    {
                        case DialogResult.Yes:
                            _instance.LookupTablePath = LUTControl.SaveLUT();
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }
                _temperatureTimer.Stop();
                _temperatureTimer.Dispose();
                if (Subscription != null)
                {
                    Subscription.Dispose();
                    Subscription = null;
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }

            base.OnFormClosing(e);
        }

        private void SaveSettings()
        {
            try
            {

                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.FileName = "C13440Config.xml";
                    saveFileDialog.Filter = "XML files|*.xml|All files|*.*";
                    var result = saveFileDialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        using var writer = XmlWriter.Create(saveFileDialog.FileName, new XmlWriterSettings { Indent = true });
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Settings");

                        foreach (var prop in _instance.CameraProps)
                        {
                            var value = 0.0;
                            prop.getvalue(ref value);
                            writer.WriteStartElement("Setting");
                            writer.WriteAttributeString("ID", prop.m_idProp.getidprop().ToString());
                            writer.WriteAttributeString("Value", value.ToString());
                            writer.WriteString(prop.getname());
                            writer.WriteEndElement();
                        }
                        foreach (var region in Image_Visualizer.Regions)
                        {
                            writer.WriteStartElement("Region");
                            writer.WriteAttributeString("X", region.X.ToString());
                            writer.WriteAttributeString("Y", region.Y.ToString());
                            writer.WriteAttributeString("Width", region.Width.ToString());
                            writer.WriteAttributeString("Height", region.Height.ToString());
                            writer.WriteEndElement();
                        }

                        writer.WriteStartElement("CropMode");
                        writer.WriteAttributeString("Value", _instance.CropMode.ToString());
                        writer.WriteEndElement();

                        writer.WriteEndDocument();
                        writer.Close();
                    }
                    _instance.SettingsPath = saveFileDialog.FileName;
                    _instance.Regions = new List<RegionOfInterest>(Image_Visualizer.Regions);
                    foreach (var prop in CameraProps)
                    {
                        var key = prop.m_idProp.getidprop();
                        var value = 0.0;
                        prop.getvalue(ref value);
                        _storedSettings[key] = value;
                    }
                    _storedRegions = new List<RegionOfInterest>(Image_Visualizer.Regions);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        private bool CameraSettingsChanged()
        {
            foreach (var prop in CameraProps)
            {
                var attr = new DCAMPROPATTRIBUTE(prop.m_attr.attribute);
                if (!attr.has_attr(DCAMPROPATTRIBUTE.WRITABLE))
                    continue;
                var key = prop.m_idProp.getidprop();

                if (!_storedSettings.TryGetValue(key, out var oldValue))
                    return true;

                double currentValue = 0.0;
                prop.getvalue(ref currentValue);
                if (!AreEqual(oldValue, currentValue))
                    return true;
            }

            return false;
        }

        private static bool AreEqual(double a, double b)
        {
            const double epsilon = 0.000001;
            return Math.Abs(a - b) < epsilon;
        }

        private bool RegionsChanged()
        {
            if (_storedRegions.Count != Image_Visualizer.Regions.Count)
                return true;

            return !_storedRegions.SequenceEqual(Image_Visualizer.Regions);
        }

        private bool PointsOfInterestChanged()
        {
            var current = LUTControl.PointsOfInterest;

            if (_storedPointsOfInterest.Count != current.Count)
                return true;

            foreach (var kv in _storedPointsOfInterest)
            {
                if (!current.TryGetValue(kv.Key, out var currentValue))
                    return true; // key removed

                if (kv.Value != currentValue)
                    return true; // value changed
            }

            return false;
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Save button click event handler. Saves camera settings, regions of interest, and crop mode
        /// to an .xml file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Button_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        /// <summary>
        /// Load button click event handler. This requires the camera to be in the
        /// <see cref="DCAMCAP_STATUS.STABLE"/> state. So capture is paused, buffer is released,
        /// settings are loaded, buffer is reallocated, and capture is resumed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Button_Click(object sender, EventArgs e)
        {
            try
            {
                _instance.Capture.PauseAndRelease();
                LoadSettings();
                _instance.Capture.ReallocateAndResume();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Handles changes to the regions of interest dependent on the <see cref="CropMode"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Visualizer_RegionsChanged(object sender, EventArgs e)
        {
            try
            {
                AutoCrop.SetCrop(Image_Visualizer.NextCrop, Image_Visualizer.Binning);
                var auto = IsAutoCrop();
                if (auto)
                {
                    _instance.Capture.PauseAndRelease();
                    UpdateROISettings(true);
                    _instance.Capture.ReallocateAndResume();
                    Image_Visualizer.CurrentCropLocation = Image_Visualizer.NextCrop.Location;
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Handles changes to the LUT, stores the current LUT and updates the capture with it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LUTControl_LUTChanged(object sender, EventArgs e)
        {
            _instance.LookupTable = LUTControl.LookupTable;
            _instance.Capture.UpdateLookupTable();
        }

        private void LUTControl_LUTLoaded(object sender, LUTPathChangedEventArgs e)
        {
            _instance.LookupTablePath = e.FileName;
            _storedPointsOfInterest = new Dictionary<ushort, ushort>(LUTControl.PointsOfInterest);
        }

        private void LUTControl_LUTSaved(object sender, LUTPathChangedEventArgs e)
        {
            _instance.LookupTablePath = e.FileName;
            _storedPointsOfInterest = new Dictionary<ushort, ushort>(LUTControl.PointsOfInterest);
        }

        /// <summary>
        /// Implements the tab select feature for the image visualizer for
        /// selecting a region of interest in the UI.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab)
            {
                Image_Visualizer.TryTabSelectRegion();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion
    }
}
