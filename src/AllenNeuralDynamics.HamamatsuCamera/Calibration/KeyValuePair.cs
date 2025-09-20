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
    public partial class KeyValuePair : UserControl
    {

        #region Public Members

        public int Key;
        public int Value;

        public event EventHandler KeyChanged;
        public event EventHandler ValueChanged;
        public event EventHandler RowSelected;

        #endregion

        #region Private Members

        private Label Value_Label;

        #endregion

        #region Initialization

        public KeyValuePair(int key, int value)
        {
            try
            {
                InitializeComponent();
                CreateInstance(key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: NameValuePair(int {key}, int {value})\nMessage: {ex.Message}");
            }
        }

        public KeyValuePair()
        {
            try
            {
                InitializeComponent();
                CreateInstance();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: NameValuePair()\nMessage: {ex.Message}");
            }
        }

        public KeyValuePair(string col1, string col2)
        {
            try
            {
                InitializeComponent();
                CreateInstance(col1, col2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: NameValuePair(string {col1}, string {col2})\nMessage: {ex.Message}");
            }
        }

        private void CreateInstance(int key, int value)
        {
            try
            {
                Key = key;
                Value = Math.Min(Math.Max(value, 0), ushort.MaxValue);

                // Initialize Key
                Key_Label.Text = Key.ToString();

                // Initialize Value
                Value_Label = new Label();
                Value_Label.Text = Value.ToString();
                Value_Label.Font = new Font(Key_Label.Font, FontStyle.Regular);
                Value_Label.TextAlign = ContentAlignment.MiddleCenter;
                Value_Label.BackColor = Key_Label.BackColor;
                Value_Label.Dock = DockStyle.Fill;
                Value_Label.Margin = new Padding(3);
                Value_Label.Padding = new Padding(0);
                Value_Label.Click += ValueLabel_Click;

                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: CreateInstance(int {key}, int {value})\nMessage: {ex.Message}");
            }
        }

        private void CreateInstance(string col1, string col2)
        {
            try
            {
                Key_Label.Text = col1.ToString();
                Value_Label = new Label();
                Value_Label.Text = col2.ToString();
                Value_Label.Font = new Font(Key_Label.Font, FontStyle.Bold);
                Value_Label.TextAlign = ContentAlignment.MiddleCenter;
                Value_Label.BackColor = Key_Label.BackColor;
                Value_Label.Dock = DockStyle.Fill;
                Value_Label.Margin = new Padding(3);
                Value_Label.Padding = new Padding(0);


                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);

                Key = int.MinValue;
                Value = int.MinValue;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: CreateInstance(string {col1}, string {col2})\nMessage: {ex.Message}");
            }
        }

        private void CreateInstance()
        {
            try
            {
                // Initialize Key
                Key_Label.Click += ValueLabel_Click;

                // Initialize Value
                Value_Label = new Label();
                Value_Label.Font = new Font(Key_Label.Font, FontStyle.Regular);
                Value_Label.TextAlign = ContentAlignment.MiddleCenter;
                Value_Label.BackColor = Key_Label.BackColor;
                Value_Label.Dock = DockStyle.Fill;
                Value_Label.Margin = new Padding(3);
                Value_Label.Padding = new Padding(0);
                Value_Label.Click += UnClickLabel;

                Top_TableLayoutPanel.Controls.Add(Value_Label, 1, 0);

                Key = int.MaxValue;
                Value = int.MaxValue;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: CreateInstance()\nMessage: {ex.Message}");
            }
        }

        #endregion

        #region Parent Access

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
                Console.WriteLine($"Error: UpdateKey(bool {isUnique})\nMessage: {ex.Message}");
            }
        }

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
                Console.WriteLine($"Error: UpdateValue()\nMessage: {ex.Message}");
            }
            return wasEmpty;
        }

        #endregion

        #region Event Handling

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
                Console.WriteLine($"Error: UnClickLabel\nMessage: {ex.Message}");
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
                                valueLabel.Text = tb.Text;
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
                        if (RowSelected != null)
                            RowSelected.Invoke(this, EventArgs.Empty);
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

                // Filter 
                
                invalidCharEntered = IsNonIntegerNumeric(keyChar);

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
                        label = (Label)label;
                        double.TryParse(tb.Text, out double res);
                        if(label == Key_Label)
                        {
                            // TODO: Verify Key is Unique
                            Key = Math.Min(Math.Max((int)res, 0), ushort.MaxValue);
                            if (KeyChanged != null)
                                KeyChanged.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            Value = Math.Min(Math.Max((int)res, 0), ushort.MaxValue);
                            if (ValueChanged != null)
                                ValueChanged.Invoke(this, EventArgs.Empty);
                        }
                        tb.Hide();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: TextBox_LostFocus\nMessage: {ex.Message}");
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

        #endregion
    }
}
