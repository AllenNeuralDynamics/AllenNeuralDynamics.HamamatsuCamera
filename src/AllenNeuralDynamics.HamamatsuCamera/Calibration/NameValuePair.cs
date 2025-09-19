using HamamatsuCamera.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HamamatsuCamera.Calibration
{
    public partial class NameValuePair : UserControl
    {

        #region Public Members

        public event EventHandler ReleaseBuffer;
        public event EventHandler StartAcquisition;
        public event EventHandler RefreshProps;

        public string SettingName;
        public double SettingValue;
        public bool ReadOnly;
        public DCAM_PROP_MANAGER Setting;

        #endregion

        #region Private Members

        private ComboBox ValueComboBox;
        private Label ValueLabel;
        private KeyFilterType KeyFilter;
        private string Units;
        private bool DataStream;
        private bool IsModal;
        private static Dictionary<int, string> UnitDictionary = new Dictionary<int, string>()
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

        public NameValuePair(DCAM_PROP_MANAGER setting)
        {
            try
            {
                InitializeComponent();
                Setting = setting;
                CreateInstance();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: NameValuePair\nMessage: {ex.Message}");
            }
        }

        public NameValuePair()
        {

        }

        private void CreateInstance()
        {
            try
            {
                // Get relevant Attribute Information
                ReadOnly = Setting.is_attr_readonly();
                IsModal = Setting.is_attrtype_mode();
                var attr = new DCAMPROPATTRIBUTE(Setting.m_attr.attribute);
                DataStream = attr.has_attr(DCAMPROPATTRIBUTE.DATASTREAM);

                // Initialize Name
                SettingName = Setting.getname();
                Name_Label.Text = string.Join(" ", SettingName.Split(' ').Select(str => str[0].ToString().ToUpper() + str.Substring(1).ToLower()));
                Name_Label.BackColor = ReadOnly ? Color.LightGray : Color.Transparent;

                // Initialize ValueControl
                Setting.getvalue(ref SettingValue);
                // If Modal, create ValueComboBox
                if (IsModal)
                {
                    // Make ComboBox Representation
                    ValueComboBox = new ComboBox();
                    ValueComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    ValueComboBox.Font = new Font(Name_Label.Font, FontStyle.Regular);
                    ValueComboBox.Dock = DockStyle.Fill;
                    ValueComboBox.Margin = new Padding(3);
                    ValueComboBox.BackColor = ReadOnly ? Color.LightGray : Color.White;
                    // Loop through each possible value of property
                    double tempVal;
                    tempVal = Setting.m_attr.valuemin;
                    while (tempVal <= Setting.m_attr.valuemax)
                    {
                        // Find text associated with possible value
                        var text = Setting.getvaluetext(tempVal);

                        // Add text/value pair as ComboboxItem
                        ValueComboBox.Items.Add(new ComboboxItem() { Text = text, Value = tempVal });

                        // Break out when no next possible value
                        if (!Setting.queryvalue_next(ref tempVal))
                            break;
                    }

                    ValueComboBox.SelectedItem = ValueComboBox.Items.Cast<ComboboxItem>().Where(item => item.Value == SettingValue).First();
                    ValueComboBox.SelectionChangeCommitted += ValueComboBox_SelectionChangeCommitted;

                }

                Units = UnitDictionary[Setting.m_attr.iUnit];
                KeyFilter = Setting.m_attr.iUnit == 0 ? KeyFilterType.Integer : KeyFilterType.Decimal;
                ValueLabel = new Label();
                ValueLabel.Text = Setting.getvaluetext((double)SettingValue) + Units;
                ValueLabel.Font = new Font(Name_Label.Font, FontStyle.Regular);
                ValueLabel.TextAlign = ContentAlignment.MiddleLeft;
                ValueLabel.Dock = DockStyle.Fill;
                ValueLabel.Margin = new Padding(3);
                ValueLabel.Padding = new Padding(0);
                ValueLabel.BackColor = ReadOnly ? Color.LightGray : Color.Transparent;
                if (!ReadOnly) ValueLabel.Click += ValueLabel_Click;
                else ValueLabel.Click += UnClickLabel;

                // Initialize Name_Label
                if (ValueComboBox != null && !ReadOnly)
                    Top_TableLayoutPanel.Controls.Add(ValueComboBox, 1, 0);
                else
                    Top_TableLayoutPanel.Controls.Add(ValueLabel, 1, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateInstance\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Event Handling

        private void UnClickLabel(object sender, EventArgs e)
        {
            try
            {
                Name_Label.Focus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UnClickLabel\nMessage: {ex.Message}");
            }
        }

        private void ValueComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                if (sender is Control)
                {
                    var valueControl = (Control)sender;
                    if (valueControl is ComboBox)
                    {
                        var valueComboBox = (ComboBox)valueControl;

                        var selectedItem = (ComboboxItem)valueComboBox.SelectedItem;
                        SettingValue = selectedItem.Value;

                        if (DataStream)
                            ReleaseBuffer.Invoke(this, EventArgs.Empty);
                        else
                        {
                            var val = (double)SettingValue;
                            Setting.setgetvalue(ref val);
                            SettingValue = val;
                            valueComboBox.SelectedItem = valueComboBox.Items.Cast<ComboboxItem>().Where(item => item.Value == (double)SettingValue).First();
                            RefreshProps.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ValueComboBox_SelectionChangeCommitted\nMessage: {ex.Message}");
            }
        }

        private void ValueLabel_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is Control)
                {
                    var valueControl = (Control)sender;
                    if (valueControl is Label)
                    {
                        var valueLabel = (Label)valueControl;
                        TextBox tb = null;
                        // If a TextBox is already embedded:
                        if (valueLabel.Controls.Count > 0)
                        {
                            // Reference it
                            tb = ((TextBox)valueLabel.Controls[0]);
                            // If it is already visible, it was clicked from outside, so hide it
                            if (tb.Visible)
                            {
                                valueLabel.Text = tb.Text + Units;
                                tb.Hide();
                                return;
                            }
                        }
                        else
                        {
                            tb = new TextBox();
                            tb.Parent = valueLabel;
                            tb.Size = valueLabel.Size;
                            tb.LostFocus += TextBox_LostFocus;
                            tb.KeyPress += TextBox_KeyPress;
                        }
                        tb.Text = valueLabel.Text;
                        tb.Show();
                        tb.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: ValueLabel_Click\nMessage: {ex.Message}");
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                // Get the char of the key pressed
                var keyChar = e.KeyChar;
                bool invalidCharEntered;

                // Filter based on the specified type of key filter, tracking if an invalid char was entered.
                switch (KeyFilter)
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
                if (invalidCharEntered == true)
                {
                    // Stop the character from being entered into the Control.
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: TextBox_KeyPress\nMessage: {ex.Message}");
            }
        }

        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (sender is TextBox)
                {
                    var tb = (TextBox)sender;
                    var label = tb.Parent;
                    if (label != null && label is Label)
                    {
                        // TODO: Check if in range
                        label = (Label)label;
                        double.TryParse(tb.Text, out double res);
                        SettingValue = res;

                        if (DataStream)
                        {
                            tb.Hide();
                            ReleaseBuffer.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            var val = (double)SettingValue;
                            Setting.setgetvalue(ref val);
                            SettingValue = val;
                            label.Text = Setting.getvaluetext((double)SettingValue);
                            tb.Hide();
                            RefreshProps.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: TextBox_LostFocus\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Form Access

        internal void Disable()
        {
            try
            {
                ReadOnly = true;
                if (ValueComboBox != null)
                {
                    Top_TableLayoutPanel.Controls.RemoveAt(1);
                    Top_TableLayoutPanel.Controls.Add(ValueLabel, 1, 0);
                }
                ValueLabel.BackColor = Color.LightGray;
                ValueLabel.Click -= ValueLabel_Click;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Disable\nMessage: {ex.Message}");
            }
        }

        internal void Enable()
        {
            try
            {
                ReadOnly = false;
                if (ValueComboBox != null)
                {
                    Top_TableLayoutPanel.Controls.RemoveAt(1);
                    Top_TableLayoutPanel.Controls.Add(ValueComboBox, 1, 0);
                }
                ValueLabel.BackColor = Color.Transparent;

                ValueLabel.Click += ValueLabel_Click;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Enable\nMessage: {ex.Message}");
            }
        }

        internal void RefreshValue()
        {
            try
            {
                Setting.getvalue(ref SettingValue);
                if (ValueComboBox != null)
                    ValueComboBox.SelectedItem = ValueComboBox.Items.Cast<ComboboxItem>().Where(item => item.Value == (double)SettingValue).First();
                ValueLabel.Text = Setting.getvaluetext(SettingValue) + Units;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: RefreshValue\nMessage: {ex.Message}");
            }
        }

        internal void UpdateValue()
        {
            try
            {
                Setting.setgetvalue(ref SettingValue);
                if (ValueComboBox != null)
                    ValueComboBox.SelectedItem = ValueComboBox.Items.Cast<ComboboxItem>().Where(item => item.Value == SettingValue).First();
                ValueLabel.Text = Setting.getvaluetext(SettingValue) + Units;
                StartAcquisition.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: UpdateValue\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Helper Functions

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
                Console.WriteLine($"Error: IsNonIntegerNumeric\nMessage: {ex.Message}");
            }
            return false;
        }

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
                Console.WriteLine($"Error: IsNonDecimalNumeric\nMessage: {ex.Message}");
            }
            return false;
        }

        #endregion

        #region Custom Data Types

        private class ComboboxItem
        {
            public string Text { get; set; }
            public double Value { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }

        private enum KeyFilterType
        {
            None,
            Integer,
            Decimal
        }

        #endregion
    }
}
