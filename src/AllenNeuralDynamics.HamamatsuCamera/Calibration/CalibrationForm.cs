using Bonsai.Design;
using AllenNeuralDynamics.HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    public partial class CalibrationForm : Form
    {
        #region Constants 

        //private const double SubArr_OFF = 1.0;
        //private const double SubArr_ON = 2.0;
        private const int NUM_MISC_PROPS = 1;   // Crop Mode, LUT MIN, aand LUT MAX
        private const float ROW_SIZE = 30.0f;
        //private const int LUT_ABS_MIN = 0;
        //private const int LUT_ABS_MAX = 100;
        private const int WAIT_TIME = 200;
        private const int TIMEOUT = 5000;

        /// <summary>
        /// Common string lookups.
        ///// </summary>
        //private static class StaticData.Strings
        //{
        //    // Camera Setting StaticData.Strings
        //    public const string SETTINGS = "SETTINGS";
        //    public const string SUBARR = "SUBARRAY";
        //    public const string SUBARR_HPOS = "SUBARRAY HPOS";
        //    public const string SUBARR_VPOS = "SUBARRAY VPOS";
        //    public const string SUBARR_HSIZE = "SUBARRAY HSIZE";
        //    public const string SUBARR_VSIZE = "SUBARRAY VSIZE";
        //    public const string SUBARR_MODE = "SUBARRAY MODE";
        //    public const string NUM_PIXELS_HORZ = "IMAGE DETECTOR PIXEL NUM HORZ";
        //    public const string NUM_PIXELS_VERT = "IMAGE DETECTOR PIXEL NUM VERT";
        //    public const string BINNING = "BINNING";

        //    // Misc. Setting StaticData.Strings
        //    public const string CROP_MODE = "Crop Mode";
        //    public const string AUTO = "Auto";
        //    public const string MANUAL = "Manual";
        //    public const string LUT = "LUT";
        //    public const string LUT_MIN = "LUT Min";
        //    public const string LUT_MAX = "LUT Max";

        //    // Other StaticData.Strings
        //    public const string ERROR = "Error:";
        //}


        //public static Dictionary<int, string> GroupNames = new Dictionary<int, string>()
        //{
        //    { 0         , "Miscellaneous"           },
        //    { 1         , "Sensor Mode and Speed"   },
        //    { 2         , "Trigger"                 },
        //    { 4         , "Feature"                 },
        //    { 8         , "Output Trigger"          },
        //    { 128       , "Sensor Cooler"           },
        //    { 1024      , "Binning and ROI"         },
        //    { 2048      , "Sensor Mode and Speed"   },
        //    { 4096      , "ALU"                     },
        //    { 8192      , "System Information 1"    },
        //    { 65536     , "Synchronous Timing"      },
        //    { 131072    , "System Information 2"    },
        //    { 262144    , "System Information 3"    },
        //    { 4194304   , "System Information 4"    },
        //    { 8388608   , "Master Pulse"            },
        //    { 33554432  , "Data Reduction"          }
        //};

        //public static Dictionary<string, int> StaticData.MiscSettingGroups = new Dictionary<string, int>()
        //{
        //    { StaticData.Strings.CROP_MODE  , 1024   },
        //    { StaticData.Strings.LUT_MIN    , 1024   },
        //    { StaticData.Strings.LUT_MAX    , 1024   },
        //};

        #endregion

        #region Private Members

        private C13440 C13440;
        private IDisposable Subscription;
        private IEnumerable<DCAM_PROP_MANAGER> CameraProps;
        private IEnumerable<Control> SettingsHierarchy;
        private CropSettings AutoCrop = new CropSettings();
        private CropSettings ManualCrop = new CropSettings();
        private string UpdatingProp;
        private Dictionary<int, int> LookupTable;

        #endregion

        #region Initialization
        /// <summary>
        /// Constructor for form. Initializes components, pulls camera data,
        /// loads form, closes splash screen, and begins acquisition.
        /// </summary>
        /// <param name="c13440">Instance of the <see cref="C13440"/> node.</param>
        /// <param name="provider">Service provider</param>
        public CalibrationForm(C13440 c13440, IServiceProvider provider)
        {
            try
            {
                InitializeComponent();

                // Pull required data then load form
                PullCameraData(c13440);
                LoadForm();
                Image_Visualizer.Regions = C13440.ImageProcessingProperties.Regions;
                LUTControl.LoadLUT(C13440.ImageProcessingProperties.PointsOfInterest);

                // Once ready, close the splash
                SplashScreen.CloseSplash();

                // If data was not loaded successfully, close the form
                if (CameraProps == null)
                {
                    MessageBox.Show(Resources.MsgBox_Error_LoadCameraSettings, StaticData.Strings.ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
                C13440.FrameFactory.Acquiring = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CalibrationForm\nMessage: {ex.Message}");
                this.Close();
            }
        }

        /// <summary>
        /// Begin the subscription to the camera. Once the camera is ready, it will 
        /// send a null frame.
        /// </summary>
        /// <param name="c13440"></param>
        private void PullCameraData(C13440 c13440)
        {
            try
            {
                C13440 = c13440;
                Image_Visualizer.RegionsChanged += Image_Visualizer_RegionChanged;

                Subscription = C13440.Generate()
                    .Do(frame =>
                    {
                        if (!frame.isValid() && CameraProps == null && C13440.CameraProps != null)
                            CameraProps = C13440.CameraProps;
                    })
                    .Where(frame => frame.isValid())
                    .Do(frame => Image_Visualizer.UpdateFrame(frame))
                    .Subscribe();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: PullCameraData\nMessage: {ex.Message}");
            }
        }

        /// <summary>
        /// Wait for the camera properties to be loaded. Then, create the settings table.
        /// </summary>
        private void LoadForm()
        {
            try
            {
                // Wait for data loaded
                var totalWaitTime = 0;
                while (CameraProps == null)
                {
                    Thread.Sleep(WAIT_TIME);
                    totalWaitTime += WAIT_TIME;
                    if (totalWaitTime >= TIMEOUT)
                        return;
                }

                // If data loaded successfully, then create a table to display the data
                if (CameraProps.Any())
                    Settings_Panel.Controls.Add(CreateSettingsTable());
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: LoadForm\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Settings Table Creation

        /// <summary>
        /// Instantiate and configure the settings table. Then,
        /// add groups to the table. Finally get the control heirarchy.
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
                Console.WriteLine($"Error: CreateGroupSetting\nMessage: {ex.Message}");
            }
            return new TableLayoutPanel();
        }

        private void SetDimensions(ref TableLayoutPanel table, int numCols, int numRows, SizeType sizeType)
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
                Console.WriteLine($"Error: SetDimensions\nMessage: {ex.Message}");
            }
        }

        private TableLayoutPanel CreateGroup(int group)
        {
            try
            {
                // Get number of rows
                var groupSettings = CameraProps.Where(prop => prop.m_attr.iGroup == group);
                var numRows = 1 + groupSettings.Count();

                var miscSettings = StaticData.MiscSettingGroups.Where(pair => pair.Value == group);
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
                Console.WriteLine($"Error: CreateGroup\nMessage: {ex.Message}");
            }
            return new TableLayoutPanel();
        }

        private NameValuePair CreateGroupSetting(DCAM_PROP_MANAGER groupSetting)
        {
            try
            {
                NameValuePair nameValuePair = new NameValuePair(groupSetting);
                nameValuePair.Padding = new Padding(40, 0, 0, 0);
                nameValuePair.Dock = DockStyle.Fill;
                nameValuePair.ReleaseBuffer += Setting_ReleaseBuffer;
                nameValuePair.StartAcquisition += Setting_StartAcquisition;
                nameValuePair.RefreshProps += Setting_RefreshProps;

                UpdateRelatedSoftwareSettings(ref nameValuePair);


                return nameValuePair;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateGroupSetting\nMessage: {ex.Message}");
            }
            return new NameValuePair();
        }

        private void UpdateRelatedSoftwareSettings(ref NameValuePair nameValuePair)
        {
            switch (nameValuePair.SettingName)
            {
                case StaticData.Strings.SUBARR_HPOS:
                case StaticData.Strings.SUBARR_HSIZE:
                case StaticData.Strings.SUBARR_VPOS:
                case StaticData.Strings.SUBARR_VSIZE:
                case StaticData.Strings.SUBARR_MODE:
                    if (C13440.CropMode == CropMode.Auto) nameValuePair.Disable();
                    break;
                case StaticData.Strings.NUM_PIXELS_HORZ:
                    Image_Visualizer.NumPixelsHorz = (int)nameValuePair.SettingValue;
                    break;
                case StaticData.Strings.NUM_PIXELS_VERT:
                    Image_Visualizer.NumPixelsVert = (int)nameValuePair.SettingValue;
                    break;
                case StaticData.Strings.BINNING:
                    Image_Visualizer.Binning = (int)nameValuePair.SettingValue;
                    break;
            }
        }

        private TableLayoutPanel CreateMiscSetting(string key)
        {
            try
            {
                var miscSettingTable = new TableLayoutPanel();
                SetDimensions(ref miscSettingTable, 2, 1, SizeType.Percent);
                miscSettingTable.Padding = new Padding(40, 0, 0, 0);
                miscSettingTable.Dock = DockStyle.Fill;

                // Create Name Label
                var nameLabel = new Label();
                nameLabel.Text = key;
                nameLabel.Dock = DockStyle.Fill;
                nameLabel.Margin = new Padding(3);
                nameLabel.Padding = new Padding(0);
                nameLabel.TextAlign = ContentAlignment.MiddleLeft;
                nameLabel.Click += UnClickLabel;


                miscSettingTable.Controls.Add(nameLabel, 0, 0);
                // Create Value Control
                if (key == StaticData.Strings.CROP_MODE)
                {
                    var cropValue = new ComboBox();
                    cropValue.Tag = StaticData.Strings.CROP_MODE;
                    cropValue.DropDownStyle = ComboBoxStyle.DropDownList;
                    cropValue.Dock = DockStyle.Fill;
                    cropValue.Font = new Font(Settings_Panel.Font, FontStyle.Regular);
                    cropValue.Margin = new Padding(3);
                    cropValue.Items.Add(StaticData.Strings.AUTO);
                    cropValue.Items.Add(StaticData.Strings.MANUAL);
                    cropValue.SelectedIndex = C13440.CropMode == CropMode.Auto ? 0 : 1;
                    Image_Visualizer.CropMode = C13440.CropMode;
                    cropValue.SelectionChangeCommitted += Setting_ReleaseBuffer;
                    miscSettingTable.Controls.Add(cropValue, 1, 0);
                }
                return miscSettingTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateMiscSetting\nMessage: {ex.Message}");
            }
            return new TableLayoutPanel();
        }

        private Label CreateGroupLabel(int group)
        {
            try
            {
                var groupLabel = new Label();
                groupLabel.Name = StaticData.GroupNames[group];
                groupLabel.Text = StaticData.GroupNames[group];
                groupLabel.Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
                groupLabel.Dock = DockStyle.Fill;
                groupLabel.Margin = new Padding(0);
                groupLabel.TextAlign = ContentAlignment.MiddleLeft;
                groupLabel.Click += UnClickLabel;
                groupLabel.BackColor = Color.SandyBrown;
                groupLabel.Tag = group;

                return groupLabel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateGroupLabel\nMessage: {ex.Message}");
            }
            return new Label();
        }


        /// <summary>
        /// Recursively, finds the hierarchy of controls belonging to a root <see cref="Control"/>.
        /// Creates a <see cref="Queue{Control}"/> starting with the root <see cref="Control"/>.
        /// While the queue is not empty, remove the next control of the queue, yield returning it.
        /// Then add of that control's children to the queue.
        /// </summary>
        /// <param name="root"><see cref="Control"/> that is the Active Control of the <see cref="Settings_PropGrid"/>.</param>
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

        private void Setting_RefreshProps(object sender, EventArgs e)
        {
            //if (sender is NameValuePair)
            //{
            //    var changedValueControl = (NameValuePair)sender;
            //    var nameValuePairs = Settings_TableLayoutPanel.Controls.OfType<NameValuePair>();
            //    if (nameValuePairs.Any())
            //    {
            //        nameValuePairs = nameValuePairs.Where(pair => pair.SettingName != changedValueControl.SettingName);
            //        foreach (var nameValuePair in nameValuePairs)
            //            nameValuePair.RefreshValue();
            //    }

            //}
        }

        private void Setting_StartAcquisition(object sender, EventArgs e)
        {
            try
            {
                if (sender is NameValuePair)
                    C13440.FrameFactory.AcquisitionStarted += FrameFactory_AcquisitionStarted;
                

                C13440.FrameFactory.Acquiring = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: NameValuePair_StartAcquisition\nMessage: {ex.Message}");
            }
        }

        private void FrameFactory_AcquisitionStarted(object sender, EventArgs e)
        {
            try
            {
                var nameValuePairs = SettingsHierarchy.OfType<NameValuePair>().Where(pair => pair.SettingName != UpdatingProp);
                foreach (var nameValuePair in nameValuePairs)
                {
                    Action safeRefreshValue = delegate { nameValuePair.RefreshValue(); };
                    nameValuePair.Invoke(safeRefreshValue);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: FrameFactory_AcquisitionStarted\nMessage{ex.Message}");
            }
            finally
            {
                C13440.FrameFactory.AcquisitionStarted -= FrameFactory_AcquisitionStarted;
            }
        }

        private void Setting_ReleaseBuffer(object sender, EventArgs e)
        {
            try
            {
                if (sender is NameValuePair)
                    UpdatingProp = ((NameValuePair)sender).SettingName;
                else
                    UpdatingProp = StaticData.Strings.CROP_MODE;

                C13440.FrameFactory.BufferReleased += FrameFactory_BufferReleased;
                C13440.FrameFactory.Acquiring = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: FrameFactory_AcquisitionStarted\nMessage: {ex.Message}");
            }

        }

        private void FrameFactory_BufferReleased(object sender, EventArgs e)
        {
            try
            {

                if (UpdatingProp == StaticData.Strings.CROP_MODE)
                {
                    Action safeUpdateROISettings = delegate { UpdateROISettings(IsAutoCrop()); };
                    this.Invoke(safeUpdateROISettings);
                    Action safeStartAcquisition = delegate { Setting_StartAcquisition(null, EventArgs.Empty); };
                    this.Invoke(safeStartAcquisition);
                }
                else if(UpdatingProp == StaticData.Strings.AUTO)
                {
                    Action safeUpdateROISettings = delegate { UpdateROISettings(true); };
                    this.Invoke(safeUpdateROISettings);
                    Action safeStartAcquisition = delegate { Setting_StartAcquisition(null, EventArgs.Empty); };
                    this.Invoke(safeStartAcquisition);
                }
                else if(UpdatingProp == StaticData.Strings.SETTINGS)
                {
                    Action safeLoadSettings = delegate { LoadSettings(); };
                    this.Invoke(safeLoadSettings);
                    Action safeStartAcquisition = delegate {
                        C13440.FrameFactory.AcquisitionStarted += FrameFactory_AcquisitionStarted;
                        C13440.FrameFactory.Acquiring = true;
                    };
                    this.Invoke(safeStartAcquisition);
                }
                else if(UpdatingProp == StaticData.Strings.BINNING)
                {
                    var nameValuePair = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName == UpdatingProp).First();
                    Action safeUpdateValue = delegate { nameValuePair.UpdateValue(); };
                    nameValuePair.Invoke(safeUpdateValue);
                    Action safeUpdateBinning = delegate { UpdateBinning(); };
                    this.Invoke(safeUpdateBinning);
                }
                else if (UpdatingProp == StaticData.Strings.SUBARR_HPOS)
                {
                    var nameValuePair = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName == UpdatingProp).First();
                    Action safeUpdateValue = delegate { nameValuePair.UpdateValue(); };
                    nameValuePair.Invoke(safeUpdateValue);
                    var mode = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_MODE)).First().SettingValue;
                    var binning = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.BINNING)).First().SettingValue;
                    if (mode == StaticData.SubArr_ON)
                        C13440.FrameFactory.Left = (int)(nameValuePair.SettingValue / binning);
                }
                else if (UpdatingProp == StaticData.Strings.SUBARR_VPOS)
                {
                    var nameValuePair = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName == UpdatingProp).First();
                    Action safeUpdateValue = delegate { nameValuePair.UpdateValue(); };
                    nameValuePair.Invoke(safeUpdateValue);
                    var mode = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_MODE)).First().SettingValue;
                    var binning = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.BINNING)).First().SettingValue;
                    if (mode == StaticData.SubArr_ON)
                        C13440.FrameFactory.Top = (int)(nameValuePair.SettingValue / binning);
                }
                else
                {

                    var nameValuePair = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName == UpdatingProp).First();
                    Action safeUpdateValue = delegate { nameValuePair.UpdateValue(); };
                    nameValuePair.Invoke(safeUpdateValue);
                    if (nameValuePair.SettingName.Equals(StaticData.Strings.SUBARR_MODE))
                    {
                        if(nameValuePair.SettingValue == StaticData.SubArr_ON)
                        {
                            var binning = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.BINNING)).First().SettingValue;
                            C13440.FrameFactory.Left = (int)(SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_HPOS)).First().SettingValue / binning);
                            C13440.FrameFactory.Top = (int)(SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_VPOS)).First().SettingValue / binning);
                        }
                        else
                        {
                            C13440.FrameFactory.Left = 0;
                            C13440.FrameFactory.Top = 0;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: FrameFactory_BufferReleased\nMessage: {ex.Message}");
            }
            finally
            {
                C13440.FrameFactory.BufferReleased -= FrameFactory_BufferReleased;
            }
        }



        #endregion

        #region Helper Functions

        private void UpdateBinning()
        {
            try
            {
                var binCtrl = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Equals(StaticData.Strings.BINNING)).First();
                var subarrayControls = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Contains(StaticData.Strings.SUBARR));
                C13440.FrameFactory.Left = (int)(subarrayControls.Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_HPOS)).First().SettingValue / binCtrl.SettingValue);
                C13440.FrameFactory.Top = (int)(subarrayControls.Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_VPOS)).First().SettingValue / binCtrl.SettingValue);
                Image_Visualizer.UpdateBinning((int)binCtrl.SettingValue);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: UpdateBinning()\nMessage: {ex.Message}");
            }
        }

        private void UpdateROISettings(bool auto)
        {
            try
            {
                Image_Visualizer.CropMode = auto ? CropMode.Auto : CropMode.Manual;
                C13440.CropMode = Image_Visualizer.CropMode;
                var subarrayControls = SettingsHierarchy.OfType<NameValuePair>().Where(prop => prop.SettingName.Contains(StaticData.Strings.SUBARR));

                // Disable Subarray Mode
                var modeControl = subarrayControls.Where(prop => prop.SettingName.Equals(StaticData.Strings.SUBARR_MODE)).First();
                modeControl.SettingValue = StaticData.SubArr_OFF;
                modeControl.Setting.setvalue(modeControl.SettingValue);

                // Set New Crop Values
                var newCrop = auto ? AutoCrop.GetCrop() : ManualCrop.GetCrop();
                var cropControls = subarrayControls.Where(prop => !prop.SettingName.Equals(StaticData.Strings.SUBARR_MODE));
                for (int i = 0; i < cropControls.Count(); i++)
                {
                    var cropControl = cropControls.ElementAt(i);
                    var newValue = newCrop.ElementAt(i);
                    cropControl.Setting.setgetvalue(ref newValue);
                    cropControl.SettingValue = newValue;
                    if (cropControl.SettingName.Equals(StaticData.Strings.SUBARR_HPOS))
                        C13440.FrameFactory.Left = (int)cropControl.SettingValue;
                    else if (cropControl.SettingName.Equals(StaticData.Strings.SUBARR_VPOS))
                        C13440.FrameFactory.Top = (int)cropControl.SettingValue;
                }

                modeControl.SettingValue = auto ? StaticData.SubArr_ON : ManualCrop.Mode;
                modeControl.Setting.setvalue(modeControl.SettingValue);

                // Disable user access to subarray settings
                foreach (var subarrayControl in subarrayControls)
                {
                    subarrayControl.RefreshValue();
                    if (auto)
                        subarrayControl.Disable();
                    else
                        subarrayControl.Enable();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UpdateROISettings\nMessage: {ex.Message}");
            }
        }

        private bool IsAutoCrop()
        {
            try
            {
                var cropValues = SettingsHierarchy.Where(ctrl => ctrl.Tag is string && (string)ctrl.Tag == StaticData.Strings.CROP_MODE);
                if (!cropValues.Any())
                    return false;

                var cropValue = (ComboBox)cropValues.First();
                return (string)cropValue.SelectedItem == StaticData.Strings.AUTO;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: IsAutoCrop\nMessage: {ex.Message}");
            }
            return false;
        }

        private void UnClickLabel(object sender, EventArgs e)
        {
            try
            {
                this.Focus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UnClickLabel\nMessage: {ex.Message}");
            }
        }

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
                        using (var reader = XmlReader.Create(openFileDialog.FileName))
                        {
                            Image_Visualizer.Regions = new List<Rectangle>();
                            while (reader.Read())
                            {
                                if (reader.Name == "Setting" && reader.HasAttributes && reader.AttributeCount == 2)
                                {
                                    CameraProps.Where(prop => prop.m_idProp.getidprop() == int.Parse(reader[0])).First().setvalue(double.Parse(reader[1]));

                                    reader.MoveToElement();
                                }
                                if(reader.Name == "Region" && reader.HasAttributes && reader.AttributeCount == 4)
                                {
                                    Image_Visualizer.Regions.Add(new Rectangle(int.Parse(reader[0]), int.Parse(reader[1]), int.Parse(reader[2]), int.Parse(reader[3])));
                                    reader.MoveToElement();
                                }
                                if(reader.Name == "CropMode" && reader.HasAttributes && reader.AttributeCount == 1)
                                {
                                    bool auto = reader[0].Equals("Auto");
                                    SettingsHierarchy.Where(ctrl => ctrl.Tag is string && (string)ctrl.Tag == StaticData.Strings.CROP_MODE).Cast<ComboBox>().First().SelectedIndex = auto ? 0 : 1;
                                    UpdateROISettings(auto);
                                }
                            }
                        }
                    }
                }
                // TODO: Update the Left and Top values of the frame factory
                double mode = 0.0;
                CameraProps.Where(prop => prop.getname() == StaticData.Strings.SUBARR_MODE).First().getvalue(ref mode);
                if (mode == StaticData.SubArr_ON)
                {
                    var hpos = 0.0;
                    var vpos = 0.0;
                    CameraProps.Where(prop => prop.getname().Equals(StaticData.Strings.SUBARR_HPOS)).First().getvalue(ref hpos);
                    CameraProps.Where(prop => prop.getname().Equals(StaticData.Strings.SUBARR_VPOS)).First().getvalue(ref vpos);
                    C13440.FrameFactory.Left = (int)hpos;
                    C13440.FrameFactory.Top = (int)vpos;
                }
                else
                {
                    C13440.FrameFactory.Left = 0;
                    C13440.FrameFactory.Top = 0;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: LoadSettings()\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Open/Close

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                StoreProps();
                if (Subscription != null)
                {
                    Subscription.Dispose();
                    Subscription = null;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: OnFormClosing\nMessage: {ex.Message}");
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

        private void StoreProps()
        {
            try
            {
                if (C13440.StoredSettings == null)
                    C13440.StoredSettings = new Dictionary<int, double>();
                foreach(var prop in CameraProps)
                {
                    var key = prop.m_idProp.getidprop();
                    var value = 0.0;
                    prop.getvalue(ref value);
                    C13440.StoredSettings[key] = value;
                }
                C13440.ImageProcessingProperties.Regions = Image_Visualizer.Regions;
                C13440.ImageProcessingProperties.LookupTable = LUTControl.LookupTable;
                C13440.ImageProcessingProperties.PointsOfInterest = LUTControl.PointsOfInterest;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: StoreProps()\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Event Handling

        private void Save_Button_Click(object sender, EventArgs e)
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
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        using (var writer = XmlWriter.Create(saveFileDialog.FileName, new XmlWriterSettings { Indent = true }))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("Settings");

                            foreach(var prop in C13440.CameraProps)
                            {
                                var value = 0.0;
                                prop.getvalue(ref value);
                                writer.WriteStartElement("Setting");
                                writer.WriteAttributeString("ID", prop.m_idProp.getidprop().ToString());
                                writer.WriteAttributeString("Value", value.ToString());
                                writer.WriteString(prop.getname());
                                writer.WriteEndElement();
                            }
                            foreach(var region in Image_Visualizer.Regions)
                            {
                                writer.WriteStartElement("Region");
                                writer.WriteAttributeString("X", region.X.ToString());
                                writer.WriteAttributeString("Y", region.Y.ToString());
                                writer.WriteAttributeString("Width", region.Width.ToString());
                                writer.WriteAttributeString("Height", region.Height.ToString());
                                writer.WriteEndElement();
                            }

                            writer.WriteStartElement("CropMode");
                            writer.WriteAttributeString("Value", C13440.CropMode.ToString());
                            writer.WriteEndElement();

                            writer.WriteEndDocument();
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Save_Button_Click\nMessage: {ex.Message}");
            }
        }

        private void Load_Button_Click(object sender, EventArgs e)
        {
            try
            {
                UpdatingProp = StaticData.Strings.SETTINGS;
                C13440.FrameFactory.BufferReleased += FrameFactory_BufferReleased;
                C13440.FrameFactory.Acquiring = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Load_Button_Click\nMessage: {ex.Message}");
            }
        }

        private void Image_Visualizer_RegionChanged(object sender, EventArgs e)
        {
            try
            {
                AutoCrop.Crop = Image_Visualizer.NextCrop;
                var auto = IsAutoCrop();
                if (auto)
                {
                    UpdatingProp = StaticData.Strings.AUTO;
                    C13440.FrameFactory.BufferReleased += FrameFactory_BufferReleased;
                    C13440.FrameFactory.Acquiring = false;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: Image_Visualizer_RegionChanged\nMessage: {ex.Message}");
            }
        }

        #endregion

        private class CropSettings
        {
            public double HPos { get; set; }
            public double HSize { get; set; }
            public double VPos { get; set; }
            public double VSize { get; set; }
            public double Mode { get; set; }
            public Rectangle Crop
            {
                set
                {
                    HPos = value.X;
                    HSize = value.Width;
                    VPos = value.Y;
                    VSize = value.Height;
                }
            }

            public IEnumerable<double> GetCrop()
            {
                yield return HPos;
                yield return HSize;
                yield return VPos;
                yield return VSize;
            }
        }

        private void LUTControl_LUTChanged(object sender, EventArgs e)
        {
            LookupTable = LUTControl.LookupTable;
            Image_Visualizer.LookupTable = LookupTable;
        }
    }
}
