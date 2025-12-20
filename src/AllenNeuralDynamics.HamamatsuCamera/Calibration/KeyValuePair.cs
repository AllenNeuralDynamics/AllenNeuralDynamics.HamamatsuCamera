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
    /// A <see cref="UserControl"/> for displaying and configuring
    /// a KeyValuePair. Used to populate the <see cref="TableLayoutPanel"/> in the
    /// <see cref="LUTEditor"/>.
    /// </summary>
    public partial class KeyValuePair : UserControl
    {

        #region Public Members

        public ushort Key { get; set; }
        public ushort Value { get; set; }

        public event EventHandler KeyChanged;
        public event EventHandler ValueChanged;
        public event EventHandler RowSelected;

        #endregion

        #region Private Members

        private Label Value_Label;

        #endregion

        #region Initialization
        /// <summary>
        /// Default constructor for creating a KeyValuePair with no key or value specified.
        /// </summary>
        public KeyValuePair()
        {
            try
            {
                InitializeComponent();
                CreateInstance();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Constructs an instance with specified <see cref="ushort"/> key and value.
        /// </summary>
        /// <param name="pair"></param>
        public KeyValuePair(KeyValuePair<ushort, ushort> pair)
        {
            try
            {
                InitializeComponent();
                CreateInstance(pair.Key, pair.Value);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Constructs an instance with specified <see cref="string"/> key and value.
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        public KeyValuePair(string col1, string col2)
        {
            try
            {
                InitializeComponent();
                CreateInstance(col1, col2);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Adds a <see cref="Label"/> for the key and value. Adds a click handler to the value label.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void CreateInstance(ushort key, ushort value)
        {
            try
            {
                Key = key;
                Value = Math.Min(Math.Max(value, (ushort)0), ushort.MaxValue);

                // Initialize Key
                Key_Label.Text = Key.ToString();

                // Initialize Value
                Value_Label = new Label
                {
                    Text = Value.ToString(),
                    Font = new Font(Key_Label.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Key_Label.BackColor,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    Padding = new Padding(0)
                };
                Value_Label.Click += ValueLabel_Click;

                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Adds a <see cref="Label"/> for the key and value. Currently used in the <see cref="LUTEditor"/> to store
        /// column headers.
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        private void CreateInstance(string col1, string col2)
        {
            try
            {
                Key_Label.Text = col1.ToString();
                Value_Label = new Label
                {
                    Text = col2.ToString(),
                    Font = new Font(Key_Label.Font, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Key_Label.BackColor,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    Padding = new Padding(0)
                };


                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Create a <see cref="Label"/> for an empty instance. Used to add new items in the <see cref="LUTEditor"/>.
        /// </summary>
        private void CreateInstance()
        {
            try
            {
                // Initialize Key
                Key_Label.Click += ValueLabel_Click;

                // Initialize Value
                Value_Label = new Label
                {
                    Font = new Font(Key_Label.Font, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Key_Label.BackColor,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    Padding = new Padding(0)
                };
                Value_Label.Click += UnClickLabel;

                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);

            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        #endregion

        #region Parent Access
        /// <summary>
        /// Updates the key if it is unique.
        /// </summary>
        /// <param name="isUnique"></param>
        public void UpdateKey(bool isUnique)
        {
            try
            {
                if (isUnique)
                {
                    Key_Label.Text = Key.ToString();
                    Key_Label.Click -= ValueLabel_Click;
                    Key_Label.Click += UnClickLabel;
                    Value_Label.Click -= UnClickLabel;
                    Value_Label.Click += ValueLabel_Click;
                }
                else
                {
                    MessageBox.Show(Resources.MsgBox_Warning_UniqueKey, "Warning:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Key = 0;
                }
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        /// <returns>True if the value was empty.</returns>
        public bool UpdateValue()
        {
            bool wasEmpty = false;
            try
            {
                wasEmpty = Value_Label.Text == "";
                Value_Label.Text = Value.ToString();
            }
            catch(Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
            return wasEmpty;
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Puts focus on the key label to commit a change to the <see cref="TextBox"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnClickLabel(object sender, EventArgs e)
        {
            try
            {
                Key_Label.Focus();
                if(RowSelected != null)
                    RowSelected.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Swaps the value label with a <see cref="TextBox"/> for editing the value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueLabel_Click(object sender, EventArgs e)
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
                            valueLabel.Text = tb.Text;
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
                    tb.Text = valueLabel.Text;
                    tb.Show();
                    tb.Focus();
                    if (RowSelected != null)
                        RowSelected.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Filters out key presses that are non integer numeric.
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

                // Filter

                invalidCharEntered = IsNonIntegerNumeric(keyChar);

                // If an invalid char was entered:
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
        /// Submit a new value for the key or label.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (sender is TextBox tb && tb.Parent is Label label)
                {
                    ushort.TryParse(tb.Text, out ushort res);
                    if (label == Key_Label)
                    {
                        // TODO: Verify Key is Unique
                        Key = Math.Min(Math.Max(res, (ushort)0), ushort.MaxValue);
                        if (KeyChanged != null)
                            KeyChanged.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Value = Math.Min(Math.Max(res, (ushort)0), ushort.MaxValue);
                        if (ValueChanged != null)
                            ValueChanged.Invoke(this, EventArgs.Empty);
                    }
                    tb.Hide();
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
        /// Check if the key char is a digit or is control.
        /// </summary>
        /// <param name="keyChar"></param>
        /// <returns>True if is a digit or control key.</returns>
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

        #endregion
    }
}
