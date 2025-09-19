
namespace HamamatsuCamera.Calibration
{
    partial class KeyValuePair
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
            this.Key_Label = new System.Windows.Forms.Label();
            this.Top_TableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Top_TableLayoutPanel
            // 
            this.Top_TableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.Top_TableLayoutPanel.ColumnCount = 2;
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.Top_TableLayoutPanel.Controls.Add(this.Key_Label, 0, 0);
            this.Top_TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Top_TableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.Top_TableLayoutPanel.Name = "Top_TableLayoutPanel";
            this.Top_TableLayoutPanel.RowCount = 1;
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.Top_TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.Top_TableLayoutPanel.Size = new System.Drawing.Size(324, 30);
            this.Top_TableLayoutPanel.TabIndex = 0;
            this.Top_TableLayoutPanel.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // Key_Label
            // 
            this.Key_Label.AutoSize = true;
            this.Key_Label.BackColor = System.Drawing.Color.Transparent;
            this.Key_Label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Key_Label.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Key_Label.Location = new System.Drawing.Point(3, 3);
            this.Key_Label.Margin = new System.Windows.Forms.Padding(3);
            this.Key_Label.Name = "Key_Label";
            this.Key_Label.Size = new System.Drawing.Size(156, 24);
            this.Key_Label.TabIndex = 1;
            this.Key_Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Key_Label.Click += new System.EventHandler(this.UnClickLabel);
            // 
            // KeyValuePair
            // 
            this.Controls.Add(this.Top_TableLayoutPanel);
            this.Name = "KeyValuePair";
            this.Size = new System.Drawing.Size(324, 30);
            this.Click += new System.EventHandler(this.UnClickLabel);
            this.Top_TableLayoutPanel.ResumeLayout(false);
            this.Top_TableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel Top_TableLayoutPanel;
        private System.Windows.Forms.Label Key_Label;
    }
}
