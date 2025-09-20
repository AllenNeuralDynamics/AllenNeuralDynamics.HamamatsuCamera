
namespace AllenNeuralDynamics.HamamatsuCamera.Calibration
{
    partial class CalibrationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Top_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Settings_GroupBox = new System.Windows.Forms.GroupBox();
            this.Props_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Load_Button = new System.Windows.Forms.Button();
            this.Save_Button = new System.Windows.Forms.Button();
            this.Settings_Panel = new System.Windows.Forms.Panel();
            this.Image_Visualizer = new AllenNeuralDynamics.HamamatsuCamera.Visualizers.ImageVisualizer();
            this.LUTControl = new AllenNeuralDynamics.HamamatsuCamera.Calibration.LUTEditor();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.Settings_GroupBox.SuspendLayout();
            this.Props_TableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.ColumnCount = 2;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 539F));
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.Controls.Add(this.Settings_GroupBox, 0, 0);
            this.Top_TableLayoutPanel.Controls.Add(this.Image_Visualizer, 1, 0);
            this.Top_TableLayoutPanel.Controls.Add(this.LUTControl, 0, 1);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 2;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(1032, 713);
            this.Top_TableLayoutPanel.TabIndex = 0;
            this.Top_TableLayoutPanel.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // Settings_GroupBox
            // 
            this.Settings_GroupBox.Controls.Add(this.Props_TableLayoutPanel);
            this.Settings_GroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Settings_GroupBox.Location = new System.Drawing.Point(3, 3);
            this.Settings_GroupBox.Name = "Settings_GroupBox";
            this.Settings_GroupBox.Size = new System.Drawing.Size(533, 407);
            this.Settings_GroupBox.TabIndex = 0;
            this.Settings_GroupBox.TabStop = false;
            this.Settings_GroupBox.Text = "Camera Settings";
            // 
            // Props_TableLayoutPanel
            // 
            this.Props_TableLayoutPanel.ColumnCount = 2;
            this.Props_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Props_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Props_TableLayoutPanel.Controls.Add(this.Load_Button, 1, 0);
            this.Props_TableLayoutPanel.Controls.Add(this.Save_Button, 0, 0);
            this.Props_TableLayoutPanel.Controls.Add(this.Settings_Panel, 0, 1);
            this.Props_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Props_TableLayoutPanel.Location = new System.Drawing.Point(3, 22);
            this.Props_TableLayoutPanel.Name = "Props_TableLayoutPanel";
            this.Props_TableLayoutPanel.RowCount = 2;
            this.Props_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.Props_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Props_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Props_TableLayoutPanel.Size = new System.Drawing.Size(527, 382);
            this.Props_TableLayoutPanel.TabIndex = 0;
            this.Props_TableLayoutPanel.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // Load_Button
            // 
            this.Load_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Load_Button.Location = new System.Drawing.Point(266, 3);
            this.Load_Button.Name = "Load_Button";
            this.Load_Button.Size = new System.Drawing.Size(258, 34);
            this.Load_Button.TabIndex = 1;
            this.Load_Button.Text = "Load Settings";
            this.Load_Button.UseVisualStyleBackColor = true;
            this.Load_Button.Click += new System.EventHandler(this.Load_Button_Click);
            // 
            // Save_Button
            // 
            this.Save_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Save_Button.Location = new System.Drawing.Point(3, 3);
            this.Save_Button.Name = "Save_Button";
            this.Save_Button.Size = new System.Drawing.Size(257, 34);
            this.Save_Button.TabIndex = 0;
            this.Save_Button.Text = "Save Settings";
            this.Save_Button.UseVisualStyleBackColor = true;
            this.Save_Button.Click += new System.EventHandler(this.Save_Button_Click);
            // 
            // Settings_Panel
            // 
            this.Settings_Panel.AutoScroll = true;
            this.Settings_Panel.BackColor = System.Drawing.SystemColors.Window;
            this.Props_TableLayoutPanel.SetColumnSpan(this.Settings_Panel, 2);
            this.Settings_Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Settings_Panel.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Settings_Panel.Location = new System.Drawing.Point(3, 43);
            this.Settings_Panel.Name = "Settings_Panel";
            this.Settings_Panel.Size = new System.Drawing.Size(521, 336);
            this.Settings_Panel.TabIndex = 2;
            this.Settings_Panel.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // Image_Visualizer
            // 
            this.Image_Visualizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Image_Visualizer.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Image_Visualizer.Location = new System.Drawing.Point(542, 3);
            this.Image_Visualizer.Name = "Image_Visualizer";
            this.Image_Visualizer.Size = new System.Drawing.Size(487, 407);
            this.Image_Visualizer.TabIndex = 1;
            this.Image_Visualizer.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // LUTControl
            // 
            this.Top_TableLayoutPanel.SetColumnSpan(this.LUTControl, 2);
            this.LUTControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LUTControl.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LUTControl.Location = new System.Drawing.Point(3, 416);
            this.LUTControl.Name = "LUTControl";
            this.LUTControl.Size = new System.Drawing.Size(1026, 294);
            this.LUTControl.TabIndex = 2;
            this.LUTControl.LUTChanged += new System.EventHandler(this.LUTControl_LUTChanged);
            // 
            // CalibrationForm
            // 
            this.ClientSize = new System.Drawing.Size(1032, 713);
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.Name = "CalibrationForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "C13440 Calibration";
            this.Click += new System.EventHandler(this.UnClickLabel);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.Settings_GroupBox.ResumeLayout(false);
            this.Props_TableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel Props_TableLayoutPanel;
        private System.Windows.Forms.Button Save_Button;
        private System.Windows.Forms.Button Load_Button;
        private System.Windows.Forms.GroupBox Settings_GroupBox;
        private Visualizers.ImageVisualizer Image_Visualizer;
        private System.Windows.Forms.Panel Settings_Panel;
        private LUTEditor LUTControl;
    }
}
