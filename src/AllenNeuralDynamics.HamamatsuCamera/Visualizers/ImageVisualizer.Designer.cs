
namespace HamamatsuCamera.Visualizers
{
    partial class ImageVisualizer
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
            if (disposing && MaskImg != null)
            {
                MaskImg.Dispose();
                MaskImg = null;
            }
            if (disposing && NextInsideMask != null)
            {
                NextInsideMask.Dispose();
                NextInsideMask = null;
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
            this.FrameData_TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.Count_Label = new System.Windows.Forms.Label();
            this.CountVal_Label = new System.Windows.Forms.Label();
            this.FPS_Label = new System.Windows.Forms.Label();
            this.FPSVal_Label = new System.Windows.Forms.Label();
            this.Image_PictureBox = new System.Windows.Forms.PictureBox();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.FrameData_TableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Image_PictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.ColumnCount = 1;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.Controls.Add(this.FrameData_TableLayoutPanel, 0, 1);
            this.Top_TableLayoutPanel.Controls.Add(this.Image_PictureBox, 0, 0);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 2;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(471, 401);
            this.Top_TableLayoutPanel.TabIndex = 0;
            // 
            // FrameData_TableLayoutPanel
            // 
            this.FrameData_TableLayoutPanel.ColumnCount = 5;
            this.FrameData_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FrameData_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.FrameData_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.FrameData_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.FrameData_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.FrameData_TableLayoutPanel.Controls.Add(this.Count_Label, 1, 0);
            this.FrameData_TableLayoutPanel.Controls.Add(this.CountVal_Label, 2, 0);
            this.FrameData_TableLayoutPanel.Controls.Add(this.FPS_Label, 3, 0);
            this.FrameData_TableLayoutPanel.Controls.Add(this.FPSVal_Label, 4, 0);
            this.FrameData_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FrameData_TableLayoutPanel.Location = new System.Drawing.Point(3, 374);
            this.FrameData_TableLayoutPanel.Name = "FrameData_TableLayoutPanel";
            this.FrameData_TableLayoutPanel.RowCount = 1;
            this.FrameData_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.FrameData_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.FrameData_TableLayoutPanel.Size = new System.Drawing.Size(465, 24);
            this.FrameData_TableLayoutPanel.TabIndex = 0;
            // 
            // Count_Label
            // 
            this.Count_Label.AutoSize = true;
            this.Count_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Count_Label.Location = new System.Drawing.Point(228, 0);
            this.Count_Label.Name = "Count_Label";
            this.Count_Label.Size = new System.Drawing.Size(69, 24);
            this.Count_Label.TabIndex = 0;
            this.Count_Label.Text = "ROI Count:";
            this.Count_Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // CountVal_Label
            // 
            this.CountVal_Label.AutoSize = true;
            this.CountVal_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountVal_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CountVal_Label.Location = new System.Drawing.Point(303, 0);
            this.CountVal_Label.Name = "CountVal_Label";
            this.CountVal_Label.Size = new System.Drawing.Size(34, 24);
            this.CountVal_Label.TabIndex = 1;
            this.CountVal_Label.Text = "0";
            this.CountVal_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FPS_Label
            // 
            this.FPS_Label.AutoSize = true;
            this.FPS_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FPS_Label.Location = new System.Drawing.Point(343, 0);
            this.FPS_Label.Name = "FPS_Label";
            this.FPS_Label.Size = new System.Drawing.Size(44, 24);
            this.FPS_Label.TabIndex = 2;
            this.FPS_Label.Text = "FPS:";
            this.FPS_Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // FPSVal_Label
            // 
            this.FPSVal_Label.AutoSize = true;
            this.FPSVal_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FPSVal_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FPSVal_Label.Location = new System.Drawing.Point(393, 0);
            this.FPSVal_Label.Name = "FPSVal_Label";
            this.FPSVal_Label.Size = new System.Drawing.Size(69, 24);
            this.FPSVal_Label.TabIndex = 3;
            this.FPSVal_Label.Text = "NA";
            this.FPSVal_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Image_PictureBox
            // 
            this.Image_PictureBox.BackColor = System.Drawing.SystemColors.Control;
            this.Image_PictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Image_PictureBox.Location = new System.Drawing.Point(3, 3);
            this.Image_PictureBox.Name = "Image_PictureBox";
            this.Image_PictureBox.Size = new System.Drawing.Size(465, 365);
            this.Image_PictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.Image_PictureBox.TabIndex = 1;
            this.Image_PictureBox.TabStop = false;
            this.Image_PictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Image_PictureBox_MouseDown);
            this.Image_PictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Image_PictureBox_MouseMove);
            this.Image_PictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Image_PictureBox_MouseUp);
            this.Image_PictureBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Image_PictureBox_PreviewKeyDown);
            // 
            // ImageVisualizer
            // 
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ImageVisualizer";
            this.Size = new System.Drawing.Size(471, 401);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.FrameData_TableLayoutPanel.ResumeLayout(false);
            this.FrameData_TableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Image_PictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel FrameData_TableLayoutPanel;
        private System.Windows.Forms.Label Count_Label;
        private System.Windows.Forms.Label CountVal_Label;
        private System.Windows.Forms.Label FPS_Label;
        private System.Windows.Forms.Label FPSVal_Label;
        private System.Windows.Forms.PictureBox Image_PictureBox;
    }
}
