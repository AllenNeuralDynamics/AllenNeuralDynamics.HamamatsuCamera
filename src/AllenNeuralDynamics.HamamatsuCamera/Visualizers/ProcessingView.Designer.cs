
namespace AllenNeuralDynamics.HamamatsuCamera.Visualizers
{
    partial class ProcessingView
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
            this.Title_Label = new System.Windows.Forms.Label();
            this.Plots_Table = new System.Windows.Forms.TableLayoutPanel();
            this.UI_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.CapVal_Label = new System.Windows.Forms.Label();
            this.Cap_Label = new System.Windows.Forms.Label();
            this.Deinterleave_Button = new System.Windows.Forms.Button();
            this.ROI_Label = new System.Windows.Forms.Label();
            this.ROIVal_Label = new System.Windows.Forms.Label();
            this.FPS_Label = new System.Windows.Forms.Label();
            this.FPSVal_Label = new System.Windows.Forms.Label();
            this.VisualizerRendererControl = new AllenNeuralDynamics.HamamatsuCamera.Visualizers.VisualizerRendererControl();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.UI_TableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.ColumnCount = 1;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Top_TableLayoutPanel.Controls.Add(this.Title_Label, 0, 0);
            this.Top_TableLayoutPanel.Controls.Add(this.Plots_Table, 0, 2);
            this.Top_TableLayoutPanel.Controls.Add(this.UI_TableLayoutPanel, 0, 3);
            this.Top_TableLayoutPanel.Controls.Add(this.VisualizerRendererControl, 0, 1);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 4;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(1024, 690);
            this.Top_TableLayoutPanel.TabIndex = 0;
            // 
            // Title_Label
            // 
            this.Title_Label.AutoSize = true;
            this.Title_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Title_Label.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title_Label.Location = new System.Drawing.Point(3, 0);
            this.Title_Label.Name = "Title_Label";
            this.Title_Label.Size = new System.Drawing.Size(1018, 50);
            this.Title_Label.TabIndex = 3;
            this.Title_Label.Text = "Image Processing";
            this.Title_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Plots_Table
            // 
            this.Plots_Table.ColumnCount = 1;
            this.Plots_Table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Plots_Table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Plots_Table.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Plots_Table.Location = new System.Drawing.Point(3, 353);
            this.Plots_Table.Name = "Plots_Table";
            this.Plots_Table.RowCount = 1;
            this.Plots_Table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Plots_Table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Plots_Table.Size = new System.Drawing.Size(1018, 294);
            this.Plots_Table.TabIndex = 5;
            // 
            // UI_TableLayoutPanel
            // 
            this.UI_TableLayoutPanel.ColumnCount = 8;
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.UI_TableLayoutPanel.Controls.Add(this.CapVal_Label, 3, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.Cap_Label, 2, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.Deinterleave_Button, 1, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.ROI_Label, 4, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.ROIVal_Label, 5, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.FPS_Label, 6, 0);
            this.UI_TableLayoutPanel.Controls.Add(this.FPSVal_Label, 7, 0);
            this.UI_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UI_TableLayoutPanel.Location = new System.Drawing.Point(3, 653);
            this.UI_TableLayoutPanel.Name = "UI_TableLayoutPanel";
            this.UI_TableLayoutPanel.RowCount = 1;
            this.UI_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.UI_TableLayoutPanel.Size = new System.Drawing.Size(1018, 34);
            this.UI_TableLayoutPanel.TabIndex = 4;
            // 
            // CapVal_Label
            // 
            this.CapVal_Label.AutoSize = true;
            this.CapVal_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CapVal_Label.Location = new System.Drawing.Point(762, 0);
            this.CapVal_Label.Name = "CapVal_Label";
            this.CapVal_Label.Size = new System.Drawing.Size(36, 34);
            this.CapVal_Label.TabIndex = 1;
            this.CapVal_Label.Text = "300";
            this.CapVal_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Cap_Label
            // 
            this.Cap_Label.AutoSize = true;
            this.Cap_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Cap_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Cap_Label.Location = new System.Drawing.Point(673, 0);
            this.Cap_Label.Name = "Cap_Label";
            this.Cap_Label.Size = new System.Drawing.Size(83, 34);
            this.Cap_Label.TabIndex = 0;
            this.Cap_Label.Text = "Capacity:";
            this.Cap_Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Deinterleave_Button
            // 
            this.Deinterleave_Button.AutoSize = true;
            this.Deinterleave_Button.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Deinterleave_Button.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Deinterleave_Button.Location = new System.Drawing.Point(551, 3);
            this.Deinterleave_Button.Name = "Deinterleave_Button";
            this.Deinterleave_Button.Size = new System.Drawing.Size(116, 28);
            this.Deinterleave_Button.TabIndex = 2;
            this.Deinterleave_Button.Text = "Deinterleave";
            this.Deinterleave_Button.UseVisualStyleBackColor = true;
            this.Deinterleave_Button.Click += new System.EventHandler(this.Deinterleave_Button_Click);
            // 
            // ROI_Label
            // 
            this.ROI_Label.AutoSize = true;
            this.ROI_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ROI_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ROI_Label.Location = new System.Drawing.Point(804, 0);
            this.ROI_Label.Name = "ROI_Label";
            this.ROI_Label.Size = new System.Drawing.Size(97, 34);
            this.ROI_Label.TabIndex = 3;
            this.ROI_Label.Text = "ROI Count:";
            this.ROI_Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ROIVal_Label
            // 
            this.ROIVal_Label.AutoSize = true;
            this.ROIVal_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ROIVal_Label.Location = new System.Drawing.Point(907, 0);
            this.ROIVal_Label.Name = "ROIVal_Label";
            this.ROIVal_Label.Size = new System.Drawing.Size(18, 34);
            this.ROIVal_Label.TabIndex = 4;
            this.ROIVal_Label.Text = "0";
            this.ROIVal_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FPS_Label
            // 
            this.FPS_Label.AutoSize = true;
            this.FPS_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FPS_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FPS_Label.Location = new System.Drawing.Point(931, 0);
            this.FPS_Label.Name = "FPS_Label";
            this.FPS_Label.Size = new System.Drawing.Size(47, 34);
            this.FPS_Label.TabIndex = 5;
            this.FPS_Label.Text = "FPS:";
            this.FPS_Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // FPSVal_Label
            // 
            this.FPSVal_Label.AutoSize = true;
            this.FPSVal_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FPSVal_Label.Location = new System.Drawing.Point(984, 0);
            this.FPSVal_Label.Name = "FPSVal_Label";
            this.FPSVal_Label.Size = new System.Drawing.Size(31, 34);
            this.FPSVal_Label.TabIndex = 6;
            this.FPSVal_Label.Text = "NA";
            this.FPSVal_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // VisualizerRendererControl
            // 
            this.VisualizerRendererControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.VisualizerRendererControl.Location = new System.Drawing.Point(3, 53);
            this.VisualizerRendererControl.Name = "VisualizerRendererControl";
            this.VisualizerRendererControl.Size = new System.Drawing.Size(1018, 294);
            this.VisualizerRendererControl.TabIndex = 6;
            // 
            // ProcessingView
            // 
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.DoubleBuffered = true;
            this.Name = "ProcessingView";
            this.Size = new System.Drawing.Size(1024, 690);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.Top_TableLayoutPanel.PerformLayout();
            this.UI_TableLayoutPanel.ResumeLayout(false);
            this.UI_TableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private System.Windows.Forms.Label Title_Label;
        private System.Windows.Forms.TableLayoutPanel UI_TableLayoutPanel;
        private System.Windows.Forms.Label CapVal_Label;
        private System.Windows.Forms.Label Cap_Label;
        private System.Windows.Forms.Button Deinterleave_Button;
        private System.Windows.Forms.Label ROI_Label;
        private System.Windows.Forms.Label ROIVal_Label;
        private System.Windows.Forms.Label FPS_Label;
        private System.Windows.Forms.Label FPSVal_Label;
        private System.Windows.Forms.TableLayoutPanel Plots_Table;
        private VisualizerRendererControl VisualizerRendererControl;
    }
}
