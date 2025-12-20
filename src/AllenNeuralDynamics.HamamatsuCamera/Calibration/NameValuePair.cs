using AllenNeuralDynamics.HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    /// <summary>
    /// User control for displaying and configuring camera settings
    /// </summary>
    public partial class NameValuePair : UserControl
    {

        #region Public Members

        public event EventHandler DataStreamPropChangeRequest;
        public event EventHandler EffectiveChangeOccurred;

        public string SettingName { get; private set; }
        public DCAM_PROP_MANAGER Setting { get; private set; }
        public double SettingValue { get; set; }

        #endregion

        #region Private Members

        private ComboBox  _valueComboBox;
        private Label  _valueLabel;
        private KeyFilterType _keyFilter;
        private bool _canEdit;
        private bool _isEnabled;
        private readonly bool _isModal;
        private readonly bool _isDataStream;
        private readonly bool _isWritable;
        private readonly bool _isAccessBusy;
        private readonly bool _isEffective;
        private readonly string _units;
        private readonly static Dictionary<int, string> _unitDictionary = new Dictionary<int, string>()
        {
            { 0 , ""        },
            { 1 , " Sec"    },
            { 2 , "\u00B0C"},
            { 4 , " m/s"    },
            { 5 , " Hz"     },
            { 7 , " \u03BCm"}
        };

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize component and read attributes of the <see cref="DCAM_PROP_MANAGER"/>.
        /// </summary>
        /// <param name="setting">Camera property</param>
        public NameValuePair(DCAM_PROP_MANAGER setting)
        {
            try
            {
                InitializeComponent();
                Setting = setting;
                var attr = new DCAMPROPATTRIBUTE(Setting.m_attr.attribute);
                _isEnabled = true;
                _isModal = attr.is_type(DCAMPROPATTRIBUTE.TYPE_MODE);
                _isDataStream = attr.has_attr(DCAMPROPATTRIBUTE.DATASTREAM) || Setting.m_idProp.Equals(DCAMIDPROP.EXPOSURETIME) || Setting.m_idProp.Equals(DCAMIDPROP.IMAGE_PIXELTYPE);
                _isWritable = attr.has_attr(DCAMPROPATTRIBUTE.WRITABLE);
                _isAccessBusy = attr.has_attr(DCAMPROPATTRIBUTE.ACCESSBUSY);
                _isEffective = attr.has_attr(DCAMPROPATTRIBUTE.EFFECTIVE);
                _units = _unitDictionary[Setting.m_attr.iUnit];
                _canEdit = GetCanEdit(DCAMCAP_STATUS.BUSY);
                CreateInstance();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Default constructor used when <see cref="CalibrationForm"/> fails to create a camera setting UI object.
        /// </summary>
        public NameValuePair()
        {
            // Do nothing
        }

        /// <summary>
        /// Initialize members, create value control dependent on whether the property is modal or not.
        /// Update the state.
        /// </summary>
        private void CreateInstance()
        {
            try
            {
                // Initialize Name
                SettingName = Setting.getname();
                Name_Label.Text = string.Join(" ", SettingName.Split(' ').Select(str => str[0].ToString().ToUpper() + str.Substring(1).ToLower()));

                // Initialize ValueControl
                var settingValue = 0.0;
                Setting.getvalue(ref settingValue);
                SettingValue = settingValue;
                // If Modal, create  _valueComboBox
                if (_isModal)
                {
                    // Make ComboBox Representation
                    _valueComboBox = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Font = new Font(Name_Label.Font, FontStyle.Regular),
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                    };
                    // Loop through each possible value of property
                    double tempVal;
                    tempVal = Setting.m_attr.valuemin;
                    while (tempVal <= Setting.m_attr.valuemax)
                    {
                        // Find text associated with possible value
                        var text = Setting.getvaluetext(tempVal);

                        // Add text/value pair as ComboboxItem
                         _valueComboBox.Items.Add(new ComboboxItem() { Text = text, Value = tempVal });

                        // Break out when no next possible value
                        if (!Setting.queryvalue_next(ref tempVal))
                            break;
                    }

                     _valueComboBox.SelectedItem =  _valueComboBox.Items.Cast<ComboboxItem>().First(item => (int)item.Value == (int)SettingValue);
                     _valueComboBox.SelectionChangeCommitted += ValueComboBox_SelectionChangeCommitted;

                }

                _keyFilter = Setting.m_attr.iUnit == 0 ? KeyFilterType.Integer : KeyFilterType.Decimal;
                _valueLabel = new Label
                {
                    Text = Setting.getvaluetext(SettingValue) + _units,
                    Font = new Font(Name_Label.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    Padding = new Padding(0)
                };

                UpdateState();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Update which UserControl is displayed based on whether the setting is currently editable.
        /// Also, updates the appropriate event handler.
        /// </summary>
        private void UpdateState()
        {
            if (_canEdit)
            {
                Name_Label.BackColor = Color.Transparent;
                if (_valueComboBox != null)
                {
                    _valueComboBox.BackColor = Color.White;
                    RemoveControl(_valueLabel);
                    AddControl(_valueComboBox);
                }
                else
                {
                    RemoveControl(_valueComboBox);
                    AddControl(_valueLabel);
                }
                _valueLabel.BackColor = Color.Transparent;
                _valueLabel.Click -= UnClickLabel;
                _valueLabel.Click -= ValueLabel_Click;
                _valueLabel.Click += ValueLabel_Click;
            }
            else
            {
                Name_Label.BackColor = Color.LightGray;
                if (_valueComboBox != null)
                    _valueComboBox.BackColor = Color.LightGray;
                _valueLabel.BackColor = Color.LightGray;
                _valueLabel.Click -= UnClickLabel;
                _valueLabel.Click -= ValueLabel_Click;
                _valueLabel.Click += UnClickLabel;
                RemoveControl(_valueLabel);
                RemoveControl(_valueComboBox);
                AddControl(_valueLabel);
            }
        }

        /// <summary>
        /// Add a control to the <see cref="TableLayoutPanel"/> if it is not already added.
        /// </summary>
        /// <param name="control">Control to add</param>
        private void AddControl(Control control)
        {
            if (control == null)
                return;
            if (control.Parent != Top_TableLayoutPanel)
                Top_TableLayoutPanel.Controls.Add(control, 1, 0);
        }

        /// <summary>
        /// Remove a control from the <see cref="TableLayoutPanel"/> if it is already added.
        /// </summary>
        /// <param name="control"></param>
        private void RemoveControl(Control control)
        {
            if (control == null)
                return;
            if (control.Parent == Top_TableLayoutPanel)
                Top_TableLayoutPanel.Controls.Remove(control);
        }

        /// <summary>
        /// Check if the setting is editable based on several factors.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private bool GetCanEdit(DCAMCAP_STATUS status)
        {
            if (_isEnabled && _isDataStream)
                return true;

            if (!_isEnabled || !_isWritable || status == DCAMCAP_STATUS.ERROR || status == DCAMCAP_STATUS.UNSTABLE)
                return false;

            if (status == DCAMCAP_STATUS.STABLE)
                return true;

            return _isAccessBusy;
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Change focus to the Name Label to commit changes from Value TextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnClickLabel(object sender, EventArgs e)
        {
            try
            {
                Name_Label.Focus();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Update the cached setting value. Then either invoke the DataStreamPropChangeRequest
        /// or set the camera setting based on the DataStream attribute. Additionally,
        /// if the camera setting is changed and the Effective attribute is present, then invoke
        /// the EffectiveChangeOccurred event to notify the <see cref="CalibrationForm"/> that a refresh is
        /// required for other settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void  ValueComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                if(sender is ComboBox valueComboBox)
                {
                    var selectedItem = (ComboboxItem)valueComboBox.SelectedItem;
                    SettingValue = selectedItem.Value;
                    if (_isDataStream)
                        DataStreamPropChangeRequest.Invoke(this, EventArgs.Empty);
                    else
                    {
                        var val = SettingValue;
                        Setting.setgetvalue(ref val);
                        SettingValue = val;
                        valueComboBox.SelectedItem = valueComboBox.Items.Cast<ComboboxItem>().First(item => (int)item.Value == (int)SettingValue);
                        if (_isEffective)
                            EffectiveChangeOccurred?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Replace the <see cref="Label"/> with a <see cref="TextBox"/>
        /// to allow the user to configure the setting value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void  ValueLabel_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is Label valueLabel)
                {
                    TextBox tb = null;
                    // If a TextBox is already embedded:
                    if (valueLabel.Controls.Count > 0)
                    {
                        // Reference it
                        tb = ((TextBox)valueLabel.Controls[0]);
                        // If it is already visible, it was clicked from outside, so hide it
                        if (tb.Visible)
                        {
                            valueLabel.Text = tb.Text + _units;
                            tb.Hide();
                            return;
                        }
                    }
                    else
                    {
                        tb = new TextBox
                        {
                            Parent = valueLabel,
                            Size = valueLabel.Size
                        };
                        tb.LostFocus += TextBox_LostFocus;
                        tb.KeyPress += TextBox_KeyPress;
                    }
                    tb.Text = valueLabel.Text.Substring(0, valueLabel.Text.Length - _units.Length);
                    tb.Show();
                    tb.Focus();
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Apply the key filter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                // Get the char of the key pressed
                var keyChar = e.KeyChar;
                bool invalidCharEntered;

                // Filter based on the specified type of key filter, tracking if an invalid char was entered.
                switch (_keyFilter)
                {
                    case KeyFilterType.Integer:
                        invalidCharEntered = IsNonIntegerNumeric(keyChar);
                        break;
                    case KeyFilterType.Decimal:
                        invalidCharEntered = IsNonDecimalNumeric(keyChar);
                        break;
                    default:
                        invalidCharEntered = false;
                        break;
                }

                // If and invalid char was entered:
                if (invalidCharEntered)
                {
                    // Stop the character from being entered into the Control.
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Commit changes to the <see cref="TextBox"/> to update the settings or notify the
        /// <see cref="CalibrationForm"/> that a DataStreamPropChange is requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (sender is TextBox tb && tb.Parent is Label label)
                {
                    double.TryParse(tb.Text, out double res);
                    SettingValue = res;
                    if (_isDataStream)
                    {
                        label.Text = string.Empty;
                        tb.Hide();
                        DataStreamPropChangeRequest.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        var val = SettingValue;
                        Setting.setgetvalue(ref val);
                        SettingValue = val;
                        label.Text = Setting.getvaluetext(SettingValue);
                        tb.Hide();
                        if (_isEffective)
                            EffectiveChangeOccurred?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        #endregion

        #region Form Access

        /// <summary>
        /// Disable this user control.
        /// </summary>
        /// <param name="status"></param>
        internal void Disable(DCAMCAP_STATUS status)
        {
            try
            {
                _isEnabled = false;
                _canEdit = GetCanEdit(status);
                UpdateState();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Enable this user control.
        /// </summary>
        /// <param name="status"></param>
        internal void Enable(DCAMCAP_STATUS status)
        {
            try
            {
                _isEnabled = true;
                _canEdit = GetCanEdit(status);
                UpdateState();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Refresh the setting value.
        /// </summary>
        internal void RefreshValue()
        {
            try
            {
                var settingValue = 0.0;
                Setting.getvalue(ref settingValue);
                SettingValue = settingValue;
                if ( _valueComboBox != null)
                     _valueComboBox.SelectedItem =  _valueComboBox.Items.Cast<ComboboxItem>().First(item => (int)item.Value == (int)SettingValue);
                 _valueLabel.Text = Setting.getvaluetext(SettingValue) + _units;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Update the setting value.
        /// </summary>
        internal void UpdateValue()
        {
            try
            {
                var settingValue = SettingValue;
                Setting.setgetvalue(ref settingValue);
                SettingValue = settingValue;
                if ( _valueComboBox != null)
                     _valueComboBox.SelectedItem =  _valueComboBox.Items.Cast<ComboboxItem>().First(item => (int)item.Value == (int)SettingValue);
                 _valueLabel.Text = Setting.getvaluetext(SettingValue) + _units;
                if (_isEffective)
                    EffectiveChangeOccurred?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Non integer numeric filter
        /// </summary>
        /// <param name="keyChar">Key char</param>
        /// <returns>False if character is digit or control char</returns>
        private static bool IsNonIntegerNumeric(char keyChar)
        {
            try
            {
                if (char.IsDigit(keyChar) || char.IsControl(keyChar))
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return false;
        }

        /// <summary>
        /// Non decimal numeric filter
        /// </summary>
        /// <param name="keyChar">key char</param>
        /// <returns>False if character is digit or control or "."</returns>
        private static bool IsNonDecimalNumeric(char keyChar)
        {
            try
            {
                if (char.IsDigit(keyChar) || char.IsControl(keyChar) || keyChar == '.')
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return false;
        }

        #endregion

        #region Custom Data Types

        /// <summary>
        /// Items in the <see cref="ComboBox"/> used to represent modal camera settings
        /// </summary>
        private sealed class ComboboxItem
        {
            public string Text { get; set; }
            public double Value { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }

        /// <summary>
        /// Key filtering options
        /// </summary>
        private enum KeyFilterType
        {
            None,
            Integer,
            Decimal
        }

        #endregion
    }
}
